using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Narrative
{
    /// <summary>
    /// Resolves the next scene based on branching conditions defined in SceneData.
    ///
    /// This system implements Rule 5: Branching Path Selection from the Episode Structure GDD:
    /// - Evaluates CONDITION expressions against NSM state
    /// - Selects the first branch whose condition evaluates to true
    /// - Falls back to linear sequence if no branch matches
    /// - Detects dead ends (no valid branch) and signals EPISODE_COMPLETE
    /// - Records branch history in NSM for save/resume
    ///
    /// Sprints: S3-4
    /// Design Doc: design/gdd/episode-structure.md (Rule 5)
    /// </summary>
    public static class BranchingResolver
    {
        /// <summary>
        /// Result of a branch resolution operation.
        /// </summary>
        public struct ResolutionResult
        {
            /// <summary>
            /// The resolved next scene ID, or null if no valid branch.
            /// </summary>
            public readonly string NextSceneId;

            /// <summary>
            /// The branch ID that was chosen (for history recording), or null if linear fallback.
            /// </summary>
            public readonly string ChosenBranchId;

            /// <summary>
            /// True if this resolution used branching (condition was evaluated).
            /// </summary>
            public readonly bool WasBranching;

            /// <summary>
            /// True if this is a dead end (no valid branch found and no more linear scenes).
            /// </summary>
            public readonly bool IsDeadEnd;

            public ResolutionResult(string nextSceneId, string chosenBranchId, bool wasBranching, bool isDeadEnd)
            {
                NextSceneId = nextSceneId;
                ChosenBranchId = chosenBranchId;
                WasBranching = wasBranching;
                IsDeadEnd = isDeadEnd;
            }

            public static ResolutionResult Branch(string sceneId, string branchId) =>
                new ResolutionResult(sceneId, branchId, wasBranching: true, isDeadEnd: false);

            public static ResolutionResult Linear(string sceneId) =>
                new ResolutionResult(sceneId, chosenBranchId: null, wasBranching: false, isDeadEnd: false);

            public static ResolutionResult DeadEnd() =>
                new ResolutionResult(nextSceneId: null, chosenBranchId: null, wasBranching: false, isDeadEnd: true);
        }

        /// <summary>
        /// Branch decision record for save/resume history.
        /// </summary>
        [Serializable]
        public struct BranchDecision
        {
            [SerializeField] private string _sceneId;
            [SerializeField] private string _conditionExpression;
            [SerializeField] private string _chosenBranchId;

            public string SceneId => _sceneId;
            public string ConditionExpression => _conditionExpression;
            public string ChosenBranchId => _chosenBranchId;

            public BranchDecision(string sceneId, string conditionExpression, string chosenBranchId)
            {
                _sceneId = sceneId;
                _conditionExpression = conditionExpression;
                _chosenBranchId = chosenBranchId;
            }
        }

        private const string NSM_KEY_BRANCH_HISTORY = "episode.branchHistory";

        /// <summary>
        /// Resolves the next scene for a given scene with potential branching.
        ///
        /// Algorithm (MVP: max 2-level deep branching - no recursive tree traversal):
        /// 1. If scene has no condition expression, return linear fallback
        /// 2. Evaluate condition expression against NSM
        /// 3. If true, return the primary branch target
        /// 4. Otherwise, check remaining branches in order and return first match
        /// 5. If no branch matches and no more linear scenes, return DeadEnd
        /// 6. Otherwise, return linear fallback (next scene in sequence)
        /// </summary>
        /// <param name="currentScene">The current scene data with potential branching.</param>
        /// <param name="nsm">The Narrative State Machine instance.</param>
        /// <param name="hasMoreScenesInChapter">True if there are more scenes in the current chapter after currentScene.</param>
        /// <returns>ResolutionResult with the next scene ID or dead-end signal.</returns>
        public static ResolutionResult ResolveNextScene(
            SceneData currentScene,
            NarrativeStateMachine nsm,
            bool hasMoreScenesInChapter)
        {
            if (currentScene == null)
            {
                Debug.LogWarning("[BranchingResolver] CurrentScene is null, treating as dead end.");
                return ResolutionResult.DeadEnd();
            }

            // No branching data - use linear progression
            if (!currentScene.HasCondition || string.IsNullOrEmpty(currentScene.ConditionExpression))
            {
                return ResolutionResult.Linear(nextSceneId: null);
            }

            string conditionExpr = currentScene.ConditionExpression;

            // Evaluate the condition expression
            var evalResult = ConditionExpression.Evaluate(conditionExpr, nsm);
            if (!evalResult.IsValid)
            {
                Debug.LogWarning($"[BranchingResolver] Condition evaluation failed: {evalResult.ErrorMessage}. Falling back to linear.");
                RecordBranchDecision(currentScene.SceneId, conditionExpr, chosenBranchId: null, nsm);
                return ResolutionResult.Linear(nextSceneId: null);
            }

            // Condition is false - check branch targets
            if (!evalResult.Value)
            {
                // Try each branch in order
                if (currentScene.BranchTargets != null)
                {
                    foreach (var kvp in currentScene.BranchTargets)
                    {
                        string branchId = kvp.Key;
                        string branchSceneId = kvp.Value;

                        if (string.IsNullOrEmpty(branchSceneId))
                        {
                            continue;
                        }

                        // Branch targets can have their own conditions in key format: "branchId:condition"
                        // For MVP, we treat simple branch targets as always valid
                        if (branchId.Contains(":"))
                        {
                            // Nested condition format: "branchId:condition"
                            var nestedResult = TryEvaluateNestedCondition(branchId, nsm);
                            if (nestedResult.HasValue && nestedResult.Value)
                            {
                                RecordBranchDecision(currentScene.SceneId, conditionExpr, branchId, nsm);
                                return ResolutionResult.Branch(branchSceneId, branchId);
                            }
                        }
                        else
                        {
                            // Simple branch - no nested condition
                            RecordBranchDecision(currentScene.SceneId, conditionExpr, branchId, nsm);
                            return ResolutionResult.Branch(branchSceneId, branchId);
                        }
                    }
                }

                // No branch matched - dead end if no more linear scenes
                if (!hasMoreScenesInChapter)
                {
                    Debug.Log("[BranchingResolver] No valid branch and no more scenes. Treating as dead end (EPISODE_COMPLETE).");
                    RecordBranchDecision(currentScene.SceneId, conditionExpr, chosenBranchId: null, nsm);
                    return ResolutionResult.DeadEnd();
                }

                // Fall back to linear progression
                RecordBranchDecision(currentScene.SceneId, conditionExpr, chosenBranchId: null, nsm);
                return ResolutionResult.Linear(nextSceneId: null);
            }

            // Condition is true - return primary branch target (key "default" or first branch)
            if (currentScene.BranchTargets != null)
            {
                // First, try "default" branch
                if (currentScene.BranchTargets.TryGetValue("default", out string defaultSceneId) &&
                    !string.IsNullOrEmpty(defaultSceneId))
                {
                    RecordBranchDecision(currentScene.SceneId, conditionExpr, "default", nsm);
                    return ResolutionResult.Branch(defaultSceneId, "default");
                }

                // Otherwise, return first available branch
                foreach (var kvp in currentScene.BranchTargets)
                {
                    if (!string.IsNullOrEmpty(kvp.Value))
                    {
                        RecordBranchDecision(currentScene.SceneId, conditionExpr, kvp.Key, nsm);
                        return ResolutionResult.Branch(kvp.Value, kvp.Key);
                    }
                }
            }

            // No branch targets defined - dead end
            if (!hasMoreScenesInChapter)
            {
                Debug.Log("[BranchingResolver] Condition true but no branch targets and no more scenes. Treating as dead end.");
                return ResolutionResult.DeadEnd();
            }

            return ResolutionResult.Linear(nextSceneId: null);
        }

        /// <summary>
        /// Attempts to evaluate a nested condition in branch ID format "branchId:condition".
        /// </summary>
        private static bool? TryEvaluateNestedCondition(string branchId, NarrativeStateMachine nsm)
        {
            int colonIndex = branchId.LastIndexOf(':');
            if (colonIndex < 0 || colonIndex >= branchId.Length - 1)
            {
                return null;
            }

            string nestedCondition = branchId.Substring(colonIndex + 1).Trim();
            if (string.IsNullOrEmpty(nestedCondition))
            {
                return null;
            }

            // Check if it's a simple flag check
            if (nestedCondition.StartsWith("clues.", StringComparison.Ordinal))
            {
                string clueId = nestedCondition.Substring(6);
                float value = nsm.Get<float>("clues." + clueId);
                return Mathf.Approximately(value, 1.0f);
            }

            if (nestedCondition.StartsWith("trust.", StringComparison.Ordinal))
            {
                float value = nsm.Get<float>(nestedCondition);
                // Extract target from condition
                int opIndex = nestedCondition.IndexOfAny(new[] { '>', '<', '!' });
                if (opIndex > 0)
                {
                    string opAndValue = nestedCondition.Substring(opIndex);
                    int opStart = opIndex;
                    while (opStart < nestedCondition.Length && !char.IsDigit(nestedCondition[opStart]) && nestedCondition[opStart] != '-')
                    {
                        opStart++;
                    }
                    if (opStart < nestedCondition.Length && float.TryParse(nestedCondition.Substring(opStart), out float target))
                    {
                        string op = nestedCondition.Substring(opIndex, opStart - opIndex);
                        return op switch
                        {
                            ">=" => value >= target,
                            "<=" => value <= target,
                            ">" => value > target,
                            "<" => value < target,
                            "==" => Mathf.Approximately(value, target),
                            "!=" => !Mathf.Approximately(value, target),
                            _ => null
                        };
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Records a branch decision in NSM for save/resume.
        /// </summary>
        private static void RecordBranchDecision(
            string sceneId,
            string conditionExpression,
            string chosenBranchId,
            NarrativeStateMachine nsm)
        {
            var history = GetBranchHistory(nsm);
            history.Add(new BranchDecision(sceneId, conditionExpression, chosenBranchId));

            // Limit history size to prevent unbounded growth
            const int MaxHistorySize = 100;
            if (history.Count > MaxHistorySize)
            {
                history.RemoveAt(0);
            }

            SaveBranchHistory(history, nsm);
        }

        /// <summary>
        /// Gets the branch decision history from NSM.
        /// </summary>
        public static List<BranchDecision> GetBranchHistory(NarrativeStateMachine nsm)
        {
            string json = nsm.Get<string>(NSM_KEY_BRANCH_HISTORY);
            if (string.IsNullOrEmpty(json))
            {
                return new List<BranchDecision>();
            }

            try
            {
                var wrapper = JsonUtility.FromJson<BranchHistoryWrapper>("{\"decisions\":" + json + "}");
                return wrapper?.Decisions ?? new List<BranchDecision>();
            }
            catch
            {
                return new List<BranchDecision>();
            }
        }

        /// <summary>
        /// Clears the branch decision history (e.g., when starting a new episode).
        /// </summary>
        public static void ClearBranchHistory(NarrativeStateMachine nsm)
        {
            nsm.Set(NSM_KEY_BRANCH_HISTORY, "[]");
        }

        private static void SaveBranchHistory(List<BranchDecision> history, NarrativeStateMachine nsm)
        {
            try
            {
                var wrapper = new BranchHistoryWrapper { Decisions = history };
                string json = JsonUtility.ToJson(wrapper);
                // Extract just the array portion
                var fromJson = JsonUtility.FromJson<BranchHistoryWrapper>(json);
                string arrayJson = JsonUtility.ToJson(fromJson.Decisions);
                nsm.Set(NSM_KEY_BRANCH_HISTORY, arrayJson);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BranchingResolver] Failed to save branch history: {ex.Message}");
            }
        }

        [Serializable]
        private sealed class BranchHistoryWrapper
        {
            [SerializeField] private List<BranchDecision> _decisions;

            public List<BranchDecision> Decisions => _decisions;

            public BranchHistoryWrapper() { }

            public BranchHistoryWrapper(List<BranchDecision> decisions)
            {
                _decisions = decisions;
            }
        }
    }
}
