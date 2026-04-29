using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Narrative.Dialogue
{
    /// <summary>
    /// Event emitted when the current dialogue node changes.
    /// </summary>
    public sealed class DialogueNodeChangedEvent : NSMEvent
    {
        public const string KEY = "dialogue.node_changed";
        public string NodeId { get; }
        public string NodeType { get; }
        public string SpeakerId { get; }
        public string Text { get; }

        public DialogueNodeChangedEvent(string nodeId, DialogueNodeType nodeType, string speakerId, string text)
        {
            NodeId = nodeId;
            NodeType = nodeType.ToString();
            SpeakerId = speakerId;
            Text = text;
        }

        public override string Key => KEY;
    }

    /// <summary>
    /// Event emitted when the dialogue scene reaches an END node.
    /// </summary>
    public sealed class DialogueSceneCompleteEvent : NSMEvent
    {
        public const string KEY = "dialogue.scene_complete";
        public string SceneId { get; }

        public DialogueSceneCompleteEvent(string sceneId)
        {
            SceneId = sceneId;
        }

        public override string Key => KEY;
    }

    /// <summary>
    /// Event emitted when a CHOICE node is reached, signaling the UI to render choices.
    /// </summary>
    public sealed class DialogueChoicesDisplayedEvent : NSMEvent
    {
        public const string KEY = "dialogue.choices_displayed";
        public string NodeId { get; }
        public ChoiceData[] Choices { get; }

        public DialogueChoicesDisplayedEvent(string nodeId, ChoiceData[] choices)
        {
            NodeId = nodeId;
            Choices = choices;
        }

        public override string Key => KEY;
    }

    /// <summary>
    /// Event emitted when a trust shift is applied from a CHOICE selection.
    /// </summary>
    public sealed class DialogueTrustShiftEvent : NSMEvent
    {
        public const string KEY = "dialogue.trust_shift";
        public float Shift { get; }
        public float ClampedShift { get; }

        public DialogueTrustShiftEvent(float rawShift, float clampedShift)
        {
            Shift = rawShift;
            ClampedShift = clampedShift;
        }

        public override string Key => KEY;
    }

    /// <summary>
    /// Core dialogue traversal engine for the branching dialogue system.
    ///
    /// Manages the dialogue cursor, processes TEXT/CHOICE/CONDITION/END nodes,
    /// handles tap input for text advancement, and emits dialogue events.
    ///
    /// The cursor is persisted in NSM so it survives save/load cycles.
    /// </summary>
    public sealed class DialogueEngine
    {
        #region Singleton

        private static readonly Lazy<DialogueEngine> _instance = new Lazy<DialogueEngine>(() => new DialogueEngine());

        /// <summary>
        /// The singleton instance of the DialogueEngine.
        /// </summary>
        public static DialogueEngine Instance => _instance.Value;

        #endregion

        #region Constants

        /// <summary>
        /// The character ID used for the player character.
        /// </summary>
        public const string PlayerCharacterId = "PLAYER";

        /// <summary>
        /// Maximum trust shift magnitude applied per CHOICE selection.
        /// </summary>
        public const float MAX_TRUST_SHIFT = 10f;

        /// <summary>
        /// Default auto-advance delay in seconds for TEXT nodes.
        /// </summary>
        public const float DEFAULT_AUTO_ADVANCE_DELAY = 5f;

        #endregion

        #region NSM Keys

        private const string KEY_CURSOR_SCENE = "dialogue.cursor.sceneId";
        private const string KEY_CURSOR_NODE = "dialogue.cursor.nodeId";
        private const string KEY_CHOICE_HISTORY = "dialogue.choiceHistory";
        private const string KEY_VISITED_NODES = "dialogue.visitedNodes";

        #endregion

        #region Fields

        private readonly NarrativeStateMachine _nsm;
        private readonly ConditionEvaluator _conditionEvaluator;

        private DialogueTree _currentTree;
        private bool _isTextAnimating;
        private bool _isWaitingForChoiceSelection;
        private float _autoAdvanceTimer;
        private bool _autoAdvanceEnabled;

        #endregion

        #region Properties

        /// <summary>
        /// The currently loaded dialogue tree.
        /// </summary>
        public DialogueTree CurrentTree => _currentTree;

        /// <summary>
        /// Whether the text animation is currently playing (tap will cancel it).
        /// </summary>
        public bool IsTextAnimating => _isTextAnimating;

        /// <summary>
        /// Whether the engine is waiting for the player to select a choice.
        /// </summary>
        public bool IsWaitingForChoice => _isWaitingForChoiceSelection;

        /// <summary>
        /// The current scene ID from the cursor.
        /// </summary>
        public string CurrentSceneId => _nsm.Get<string>(KEY_CURSOR_SCENE);

        /// <summary>
        /// The current node ID from the cursor.
        /// </summary>
        public string CurrentNodeId => _nsm.Get<string>(KEY_CURSOR_NODE);

        /// <summary>
        /// All node IDs visited in this dialogue session.
        /// </summary>
        public IReadOnlyList<string> VisitedNodes => _nsm.Get<List<string>>(KEY_VISITED_NODES)
            ?? new List<string>();

        /// <summary>
        /// All choices made in this dialogue session.
        /// </summary>
        public IReadOnlyList<ChoiceRecord> ChoiceHistory => _nsm.Get<List<ChoiceRecord>>(KEY_CHOICE_HISTORY)
            ?? new List<ChoiceRecord>();

        /// <summary>
        /// Whether auto-advance is enabled for TEXT nodes.
        /// </summary>
        public bool AutoAdvanceEnabled
        {
            get => _autoAdvanceEnabled;
            set => _autoAdvanceEnabled = value;
        }

        #endregion

        #region Choice Record

        /// <summary>
        /// A record of a choice made by the player.
        /// </summary>
        [Serializable]
        public struct ChoiceRecord
        {
            public string NodeId;
            public int ChoiceIndex;
            public string ChoiceText;
            public float TrustShift;
            public float ClampedTrustShift;
            public string NextNodeId;

            public ChoiceRecord(string nodeId, int choiceIndex, string choiceText,
                float trustShift, float clampedTrustShift, string nextNodeId)
            {
                NodeId = nodeId;
                ChoiceIndex = choiceIndex;
                ChoiceText = choiceText;
                TrustShift = trustShift;
                ClampedTrustShift = clampedTrustShift;
                NextNodeId = nextNodeId;
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a DialogueEngine with the default NSM instance and a fresh condition evaluator.
        /// </summary>
        public DialogueEngine() : this(NarrativeStateMachine.Instance, new ConditionEvaluator())
        {
        }

        /// <summary>
        /// Creates a DialogueEngine with an injected NSM and condition evaluator.
        /// </summary>
        /// <param name="nsm">The NarrativeStateMachine to use for state and events.</param>
        /// <param name="conditionEvaluator">The ConditionEvaluator instance for CONDITION nodes.</param>
        public DialogueEngine(NarrativeStateMachine nsm, ConditionEvaluator conditionEvaluator)
        {
            _nsm = nsm ?? throw new ArgumentNullException(nameof(nsm));
            _conditionEvaluator = conditionEvaluator ?? throw new ArgumentNullException(nameof(conditionEvaluator));
            _autoAdvanceEnabled = true;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Begins a new dialogue scene from the given tree.
        /// Resets the cursor to the first node and clears the visited/choice history.
        /// </summary>
        /// <param name="tree">The DialogueTree to start.</param>
        public void StartDialogue(DialogueTree tree)
        {
            if (tree == null)
                throw new ArgumentNullException(nameof(tree));

            _currentTree = tree;
            string firstNodeId = tree.GetFirstNodeId();

            _nsm.Set(KEY_CURSOR_SCENE, tree.SceneId);
            _nsm.Set(KEY_CURSOR_NODE, firstNodeId);
            _nsm.Set(KEY_CHOICE_HISTORY, new List<ChoiceRecord>());
            _nsm.Set(KEY_VISITED_NODES, new List<string>());

            _isTextAnimating = false;
            _isWaitingForChoiceSelection = false;
            _autoAdvanceTimer = 0f;

            AdvanceToNode(firstNodeId);
        }

        /// <summary>
        /// Resumes a dialogue scene from the saved cursor in NSM.
        /// If no cursor exists, returns false and does nothing.
        /// </summary>
        /// <param name="tree">The DialogueTree to resume (must match the saved sceneId).</param>
        /// <returns>True if resume succeeded, false if no cursor was found.</returns>
        public bool ResumeDialogue(DialogueTree tree)
        {
            if (tree == null)
                throw new ArgumentNullException(nameof(tree));

            string savedSceneId = _nsm.Get<string>(KEY_CURSOR_SCENE);
            string savedNodeId = _nsm.Get<string>(KEY_CURSOR_NODE);

            if (string.IsNullOrEmpty(savedSceneId) || string.IsNullOrEmpty(savedNodeId))
                return false;

            if (savedSceneId != tree.SceneId)
                return false;

            _currentTree = tree;
            _isTextAnimating = false;
            _isWaitingForChoiceSelection = false;
            _autoAdvanceTimer = 0f;

            AdvanceToNode(savedNodeId);
            return true;
        }

        /// <summary>
        /// Called when a tap input is received.
        /// Behavior depends on current state:
        /// - If text is animating: cancel animation (first tap)
        /// - If text is done animating and node has NextNodeId: advance (second tap)
        /// - If waiting for choice: does nothing (choice uses SelectChoice)
        /// </summary>
        public void OnTap()
        {
            if (_currentTree == null) return;

            if (_isTextAnimating)
            {
                // First tap cancels text animation
                _isTextAnimating = false;
                return;
            }

            if (_isWaitingForChoiceSelection)
            {
                // Waiting for explicit choice selection, ignore tap
                return;
            }

            // Second tap (or if animation already done) advances
            string nodeId = _nsm.Get<string>(KEY_CURSOR_NODE);
            var node = _currentTree.GetNode(nodeId);

            if (node != null && !string.IsNullOrEmpty(node.NextNodeId))
            {
                AdvanceToNode(node.NextNodeId);
            }
        }

        /// <summary>
        /// Called when the player selects a choice at the given index.
        /// </summary>
        /// <param name="choiceIndex">Zero-based index of the selected choice.</param>
        public void SelectChoice(int choiceIndex)
        {
            if (_currentTree == null) return;
            if (!_isWaitingForChoiceSelection) return;

            string nodeId = _nsm.Get<string>(KEY_CURSOR_NODE);
            var node = _currentTree.GetNode(nodeId);

            if (node == null || node.Type != DialogueNodeType.CHOICE) return;

            var choices = node.Choices;
            var trustShifts = node.TrustShifts;

            if (choiceIndex < 0 || choiceIndex >= choices.Count) return;

            string selectedNextNodeId = choices[choiceIndex].NextNodeId;
            float rawTrustShift = choiceIndex < trustShifts.Count ? trustShifts[choiceIndex] : 0f;
            float clampedTrustShift = Mathf.Clamp(rawTrustShift, -MAX_TRUST_SHIFT, MAX_TRUST_SHIFT);

            // Apply trust shift via NSM
            if (Mathf.Abs(clampedTrustShift) > 0f)
            {
                // Emit trust shift event first
                _nsm.EventBus.Emit(new DialogueTrustShiftEvent(rawTrustShift, clampedTrustShift));

                // Determine which trust key to update based on sign of shift
                // Positive shift -> imperial, negative shift -> underground
                string trustKey = clampedTrustShift >= 0 ? "trust.imperial" : "trust.underground";
                float delta = Mathf.Abs(clampedTrustShift);
                _nsm.Mutate(trustKey, delta);
            }

            // Log to choice history
            var history = _nsm.Get<List<ChoiceRecord>>(KEY_CHOICE_HISTORY) ?? new List<ChoiceRecord>();
            history.Add(new ChoiceRecord(
                nodeId,
                choiceIndex,
                choices[choiceIndex].Text,
                rawTrustShift,
                clampedTrustShift,
                selectedNextNodeId));
            _nsm.Set(KEY_CHOICE_HISTORY, history);

            _isWaitingForChoiceSelection = false;
            AdvanceToNode(selectedNextNodeId);
        }

        /// <summary>
        /// Updates the auto-advance timer. Call this from a MonoBehaviour's Update().
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last frame.</param>
        public void Update(float deltaTime)
        {
            if (!_autoAdvanceEnabled) return;
            if (_currentTree == null) return;
            if (_isTextAnimating) return;
            if (_isWaitingForChoiceSelection) return;

            var nodeId = _nsm.Get<string>(KEY_CURSOR_NODE);
            var node = _currentTree.GetNode(nodeId);
            if (node == null || node.Type != DialogueNodeType.TEXT) return;

            _autoAdvanceTimer += deltaTime;
            if (_autoAdvanceTimer >= DEFAULT_AUTO_ADVANCE_DELAY)
            {
                _autoAdvanceTimer = 0f;
                if (!string.IsNullOrEmpty(node.NextNodeId))
                {
                    AdvanceToNode(node.NextNodeId);
                }
            }
        }

        /// <summary>
        /// Immediately ends the current dialogue scene and emits DialogueSceneComplete.
        /// </summary>
        public void ForceEnd()
        {
            if (_currentTree == null) return;

            string sceneId = _currentTree.SceneId;
            _currentTree = null;
            _isTextAnimating = false;
            _isWaitingForChoiceSelection = false;

            _nsm.EventBus.Emit(new DialogueSceneCompleteEvent(sceneId));
        }

        #endregion

        #region Private Helpers

        private void AdvanceToNode(string nodeId)
        {
            if (_currentTree == null) return;

            var node = _currentTree.GetNode(nodeId);
            if (node == null)
            {
                Debug.LogError($"[DialogueEngine] Node '{nodeId}' not found in tree '{_currentTree.SceneId}'.");
                return;
            }

            // Persist cursor
            _nsm.Set(KEY_CURSOR_SCENE, _currentTree.SceneId);
            _nsm.Set(KEY_CURSOR_NODE, nodeId);

            // Track visited nodes
            var visited = _nsm.Get<List<string>>(KEY_VISITED_NODES) ?? new List<string>();
            if (!visited.Contains(nodeId))
                visited.Add(nodeId);
            _nsm.Set(KEY_VISITED_NODES, visited);

            // Reset auto-advance timer
            _autoAdvanceTimer = 0f;

            // Emit node changed event
            _nsm.EventBus.Emit(new DialogueNodeChangedEvent(nodeId, node.Type, node.SpeakerId, node.Text));

            // Handle node type
            switch (node.Type)
            {
                case DialogueNodeType.TEXT:
                    _isTextAnimating = true;
                    _isWaitingForChoiceSelection = false;
                    break;

                case DialogueNodeType.CHOICE:
                    _isTextAnimating = false;
                    _isWaitingForChoiceSelection = true;
                    var choices = node.Choices;
                    _nsm.EventBus.Emit(new DialogueChoicesDisplayedEvent(nodeId, choices as ChoiceData[] ?? new ChoiceData[0]));
                    break;

                case DialogueNodeType.CONDITION:
                    EvaluateCondition(node);
                    break;

                case DialogueNodeType.END:
                    HandleEndNode();
                    break;
            }
        }

        private void EvaluateCondition(DialogueNode node)
        {
            var result = _conditionEvaluator.Evaluate(
                node.ConditionExpr,
                key => _nsm.Get<float>(key));

            if (!result.Success)
            {
                Debug.LogWarning($"[DialogueEngine] Condition evaluation failed for '{node.ConditionExpr}': {result.Error}");
                // Default to false branch on error
                AdvanceToNode(node.FalseNextNodeId);
                return;
            }

            string nextNodeId = result.Value ? node.TrueNextNodeId : node.FalseNextNodeId;
            AdvanceToNode(nextNodeId);
        }

        private void HandleEndNode()
        {
            if (_currentTree == null) return;

            string sceneId = _currentTree.SceneId;

            // Clear the dialogue cursor from NSM
            _nsm.Set(KEY_CURSOR_SCENE, null);
            _nsm.Set(KEY_CURSOR_NODE, null);

            _currentTree = null;
            _isTextAnimating = false;
            _isWaitingForChoiceSelection = false;

            _nsm.EventBus.Emit(new DialogueSceneCompleteEvent(sceneId));
        }

        #endregion

        #region Math Helpers

        private static class Mathf
        {
            public static float Clamp(float value, float min, float max)
            {
                if (value < min) return min;
                if (value > max) return max;
                return value;
            }

            public static float Abs(float value) => value >= 0 ? value : -value;
        }

        #endregion
    }
}
