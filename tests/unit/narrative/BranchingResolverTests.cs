using System;
using System.Collections.Generic;
using Core.Narrative;
using Core.Narrative.Dialogue;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Unit.Narrative
{
    /// <summary>
    /// Unit tests for BranchingResolver and ConditionExpression systems.
    /// Tests branching path resolution, condition evaluation, and dead-end detection.
    /// </summary>
    [TestFixture]
    public sealed class BranchingResolverTests
    {
        private NarrativeStateMachine _nsm;
        private const string TestEpisodeId = "test_ep";

        [SetUp]
        public void SetUp()
        {
            // Create a fresh NSM for each test
            _nsm = NarrativeStateMachine.Instance;
            ResetNSM();
        }

        private void ResetNSM()
        {
            _nsm.Set(EpisodeKeys.CurrentEpisode, null);
            _nsm.Set(EpisodeKeys.EpisodeComplete, null);
            _nsm.Set(EpisodeKeys.CurrentChapter, null);
            BranchingResolver.ClearBranchHistory(_nsm);

            // Reset trust values
            _nsm.Set("trust.imperial", 50f);
            _nsm.Set("trust.underground", 50f);

            // Reset relationship values
            _nsm.Set("relationships.reina", 0f);
            _nsm.Set("relationships.theron", 0f);

            // Reset clue flags
            _nsm.Set("clues.foundKey", 0f);
            _nsm.Set("clues.hasEvidence", 0f);
        }

        #region ConditionExpression Evaluation Tests

        [Test]
        public void ConditionExpression_SimpleEquality_ReturnsCorrectResult()
        {
            _nsm.Set("trust.imperial", 60f);

            var result = ConditionExpression.Evaluate("trust.imperial >= 50", _nsm);

            Assert.IsTrue(result.IsValid);
            Assert.IsTrue(result.Value);
        }

        [Test]
        public void ConditionExpression_SimpleInequality_ReturnsCorrectResult()
        {
            _nsm.Set("trust.imperial", 40f);

            var result = ConditionExpression.Evaluate("trust.imperial >= 50", _nsm);

            Assert.IsTrue(result.IsValid);
            Assert.IsFalse(result.Value);
        }

        [Test]
        public void ConditionExpression_EqualityOperator_Works()
        {
            _nsm.Set("clues.foundKey", 1f);

            var result = ConditionExpression.Evaluate("clues.foundKey == 1", _nsm);

            Assert.IsTrue(result.IsValid);
            Assert.IsTrue(result.Value);
        }

        [Test]
        public void ConditionExpression_NotEqualOperator_Works()
        {
            _nsm.Set("trust.imperial", 40f);

            var result = ConditionExpression.Evaluate("trust.imperial != 50", _nsm);

            Assert.IsTrue(result.IsValid);
            Assert.IsTrue(result.Value);
        }

        [Test]
        public void ConditionExpression_LessThanOperator_Works()
        {
            _nsm.Set("trust.imperial", 30f);

            var result = ConditionExpression.Evaluate("trust.imperial < 50", _nsm);

            Assert.IsTrue(result.IsValid);
            Assert.IsTrue(result.Value);
        }

        [Test]
        public void ConditionExpression_GreaterThanOperator_Works()
        {
            _nsm.Set("trust.imperial", 70f);

            var result = ConditionExpression.Evaluate("trust.imperial > 50", _nsm);

            Assert.IsTrue(result.IsValid);
            Assert.IsTrue(result.Value);
        }

        [Test]
        public void ConditionExpression_LessThanOrEqual_Works()
        {
            _nsm.Set("trust.imperial", 50f);

            var result = ConditionExpression.Evaluate("trust.imperial <= 50", _nsm);

            Assert.IsTrue(result.IsValid);
            Assert.IsTrue(result.Value);
        }

        [Test]
        public void ConditionExpression_GreaterThanOrEqual_Works()
        {
            _nsm.Set("trust.imperial", 50f);

            var result = ConditionExpression.Evaluate("trust.imperial >= 50", _nsm);

            Assert.IsTrue(result.IsValid);
            Assert.IsTrue(result.Value);
        }

        [Test]
        public void ConditionExpression_AND_BothTrue_ReturnsTrue()
        {
            _nsm.Set("trust.imperial", 60f);
            _nsm.Set("clues.foundKey", 1f);

            var result = ConditionExpression.Evaluate("trust.imperial >= 50 && clues.foundKey == 1", _nsm);

            Assert.IsTrue(result.IsValid);
            Assert.IsTrue(result.Value);
        }

        [Test]
        public void ConditionExpression_AND_OneFalse_ReturnsFalse()
        {
            _nsm.Set("trust.imperial", 40f);
            _nsm.Set("clues.foundKey", 1f);

            var result = ConditionExpression.Evaluate("trust.imperial >= 50 && clues.foundKey == 1", _nsm);

            Assert.IsTrue(result.IsValid);
            Assert.IsFalse(result.Value);
        }

        [Test]
        public void ConditionExpression_OR_OneTrue_ReturnsTrue()
        {
            _nsm.Set("trust.imperial", 40f);
            _nsm.Set("clues.foundKey", 1f);

            var result = ConditionExpression.Evaluate("trust.imperial >= 50 || clues.foundKey == 1", _nsm);

            Assert.IsTrue(result.IsValid);
            Assert.IsTrue(result.Value);
        }

        [Test]
        public void ConditionExpression_OR_BothFalse_ReturnsFalse()
        {
            _nsm.Set("trust.imperial", 40f);
            _nsm.Set("clues.foundKey", 0f);

            var result = ConditionExpression.Evaluate("trust.imperial >= 50 || clues.foundKey == 1", _nsm);

            Assert.IsTrue(result.IsValid);
            Assert.IsFalse(result.Value);
        }

        [Test]
        public void ConditionExpression_ComplexAND_OR_Precedence()
        {
            _nsm.Set("trust.imperial", 60f);
            _nsm.Set("clues.foundKey", 0f);
            _nsm.Set("relationships.reina", 40f);

            // (trust.imperial >= 50 && clues.foundKey == 1) || relationships.reina > 30
            // = (true && false) || true = false || true = true
            var result = ConditionExpression.Evaluate("trust.imperial >= 50 && clues.foundKey == 1 || relationships.reina > 30", _nsm);

            Assert.IsTrue(result.IsValid);
            Assert.IsTrue(result.Value);
        }

        [Test]
        public void ConditionExpression_AllAND_AllTrue_ReturnsTrue()
        {
            _nsm.Set("trust.imperial", 60f);
            _nsm.Set("trust.underground", 40f);

            var result = ConditionExpression.Evaluate("trust.imperial >= 50 && trust.underground <= 50", _nsm);

            Assert.IsTrue(result.IsValid);
            Assert.IsTrue(result.Value);
        }

        [Test]
        public void ConditionExpression_MissingKey_ReturnsFalse()
        {
            // Key doesn't exist, Get<float> returns 0
            var result = ConditionExpression.Evaluate("trust.nonexistent >= 50", _nsm);

            Assert.IsTrue(result.IsValid);
            Assert.IsFalse(result.Value);
        }

        [Test]
        public void ConditionExpression_InvalidSyntax_ReturnsFailure()
        {
            var result = ConditionExpression.Evaluate("not a valid expression", _nsm);

            Assert.IsFalse(result.IsValid);
            Assert.IsNotNull(result.ErrorMessage);
        }

        [Test]
        public void ConditionExpression_EmptyString_ReturnsFailure()
        {
            var result = ConditionExpression.Evaluate("", _nsm);

            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public void ConditionExpression_NullString_ReturnsFailure()
        {
            var result = ConditionExpression.Evaluate(null, _nsm);

            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public void ConditionExpression_IsValidSyntax_ValidExpressions()
        {
            Assert.IsTrue(ConditionExpression.IsValidSyntax("trust.imperial >= 50"));
            Assert.IsTrue(ConditionExpression.IsValidSyntax("clues.foundKey == 1"));
            Assert.IsTrue(ConditionExpression.IsValidSyntax("trust.imperial >= 50 && clues.foundKey == 1"));
            Assert.IsTrue(ConditionExpression.IsValidSyntax("trust.imperial >= 50 || clues.foundKey == 1"));
            Assert.IsTrue(ConditionExpression.IsValidSyntax("a >= 1 && b <= 2 || c > 3"));
        }

        [Test]
        public void ConditionExpression_IsValidSyntax_InvalidExpressions()
        {
            Assert.IsFalse(ConditionExpression.IsValidSyntax(""));
            Assert.IsFalse(ConditionExpression.IsValidSyntax(null));
            Assert.IsFalse(ConditionExpression.IsValidSyntax("not valid"));
            Assert.IsFalse(ConditionExpression.IsValidSyntax("trust.imperial = 50")); // single = is not valid
        }

        #endregion

        #region BranchingResolver Resolution Tests

        [Test]
        public void ResolveNextScene_NoCondition_ReturnsLinear()
        {
            var scene = new SceneData("scene1");

            var result = BranchingResolver.ResolveNextScene(scene, _nsm, hasMoreScenesInChapter: true);

            Assert.IsFalse(result.WasBranching);
            Assert.IsNull(result.NextSceneId);
            Assert.IsFalse(result.IsDeadEnd);
        }

        [Test]
        public void ResolveNextScene_ConditionTrue_ReturnsDefaultBranch()
        {
            _nsm.Set("trust.imperial", 60f);
            var branchTargets = new List<BranchTarget>
            {
                new BranchTarget("default", "branch_scene"),
                new BranchTarget("alt", "alt_scene")
            };
            var scene = new SceneData("scene1", false, "trust.imperial >= 50", branchTargets);

            var result = BranchingResolver.ResolveNextScene(scene, _nsm, hasMoreScenesInChapter: true);

            Assert.IsTrue(result.WasBranching);
            Assert.AreEqual("branch_scene", result.NextSceneId);
            Assert.AreEqual("default", result.ChosenBranchId);
            Assert.IsFalse(result.IsDeadEnd);
        }

        [Test]
        public void ResolveNextScene_ConditionFalse_NoBranchTargets_FallsBackToLinear()
        {
            _nsm.Set("trust.imperial", 40f);
            var scene = new SceneData("scene1", false, "trust.imperial >= 50", null);

            var result = BranchingResolver.ResolveNextScene(scene, _nsm, hasMoreScenesInChapter: true);

            Assert.IsTrue(result.WasBranching);
            Assert.IsNull(result.NextSceneId);
            Assert.IsFalse(result.IsDeadEnd);
        }

        [Test]
        public void ResolveNextScene_ConditionFalse_NoMoreScenes_ReturnsDeadEnd()
        {
            _nsm.Set("trust.imperial", 40f);
            var scene = new SceneData("scene1", false, "trust.imperial >= 50", null);

            var result = BranchingResolver.ResolveNextScene(scene, _nsm, hasMoreScenesInChapter: false);

            Assert.IsTrue(result.WasBranching);
            Assert.IsNull(result.NextSceneId);
            Assert.IsTrue(result.IsDeadEnd);
        }

        [Test]
        public void ResolveNextScene_FirstBranchMatches_ReturnsFirstBranch()
        {
            _nsm.Set("clues.foundKey", 0f);
            _nsm.Set("clues.hasEvidence", 1f);
            var branchTargets = new List<BranchTarget>
            {
                new BranchTarget("clues.foundKey:clues.foundKey == 1", "key_scene"),
                new BranchTarget("clues.hasEvidence:clues.hasEvidence == 1", "evidence_scene")
            };
            var scene = new SceneData("scene1", false, "clues.foundKey == 1", branchTargets);

            var result = BranchingResolver.ResolveNextScene(scene, _nsm, hasMoreScenesInChapter: true);

            Assert.IsTrue(result.WasBranching);
            Assert.AreEqual("evidence_scene", result.NextSceneId);
        }

        [Test]
        public void ResolveNextScene_NullScene_ReturnsDeadEnd()
        {
            var result = BranchingResolver.ResolveNextScene(null, _nsm, hasMoreScenesInChapter: false);

            Assert.IsTrue(result.IsDeadEnd);
        }

        [Test]
        public void ResolveNextScene_ConditionInvalid_FallsBackToLinear()
        {
            var scene = new SceneData("scene1", false, "invalid expression", null);

            var result = BranchingResolver.ResolveNextScene(scene, _nsm, hasMoreScenesInChapter: true);

            Assert.IsTrue(result.WasBranching);
            Assert.IsNull(result.NextSceneId);
            Assert.IsFalse(result.IsDeadEnd);
        }

        [Test]
        public void ResolveNextScene_RecordsBranchHistory()
        {
            _nsm.Set("trust.imperial", 60f);
            var branchTargets = new List<BranchTarget>
            {
                new BranchTarget("default", "branch_scene")
            };
            var scene = new SceneData("scene1", false, "trust.imperial >= 50", branchTargets);

            BranchingResolver.ResolveNextScene(scene, _nsm, hasMoreScenesInChapter: true);

            var history = BranchingResolver.GetBranchHistory(_nsm);
            Assert.AreEqual(1, history.Count);
            Assert.AreEqual("scene1", history[0].SceneId);
            Assert.AreEqual("trust.imperial >= 50", history[0].ConditionExpression);
            Assert.AreEqual("default", history[0].ChosenBranchId);
        }

        [Test]
        public void ResolveNextScene_RecordsLinearFallbackInHistory()
        {
            _nsm.Set("trust.imperial", 40f);
            var scene = new SceneData("scene1", false, "trust.imperial >= 50", null);

            BranchingResolver.ResolveNextScene(scene, _nsm, hasMoreScenesInChapter: true);

            var history = BranchingResolver.GetBranchHistory(_nsm);
            Assert.AreEqual(1, history.Count);
            Assert.AreEqual("scene1", history[0].SceneId);
            Assert.IsNull(history[0].ChosenBranchId);
        }

        [Test]
        public void ResolveNextScene_EmptyBranchTargets_FallsBackToLinear()
        {
            _nsm.Set("trust.imperial", 60f);
            var branchTargets = new List<BranchTarget>(); // Empty
            var scene = new SceneData("scene1", false, "trust.imperial >= 50", branchTargets);

            var result = BranchingResolver.ResolveNextScene(scene, _nsm, hasMoreScenesInChapter: true);

            Assert.IsTrue(result.WasBranching);
            Assert.IsNull(result.NextSceneId);
            Assert.IsFalse(result.IsDeadEnd);
        }

        #endregion

        #region Branch History Tests

        [Test]
        public void ClearBranchHistory_RemovesHistory()
        {
            _nsm.Set("trust.imperial", 60f);
            var branchTargets = new List<BranchTarget> { new BranchTarget("default", "branch_scene") };
            var scene = new SceneData("scene1", false, "trust.imperial >= 50", branchTargets);
            BranchingResolver.ResolveNextScene(scene, _nsm, hasMoreScenesInChapter: true);

            BranchingResolver.ClearBranchHistory(_nsm);

            var history = BranchingResolver.GetBranchHistory(_nsm);
            Assert.AreEqual(0, history.Count);
        }

        [Test]
        public void GetBranchHistory_EmptyHistory_ReturnsEmptyList()
        {
            var history = BranchingResolver.GetBranchHistory(_nsm);
            Assert.IsNotNull(history);
            Assert.AreEqual(0, history.Count);
        }

        #endregion

        #region SceneData Branching Constructor Tests

        [Test]
        public void SceneData_HasCondition_WhenConditionSet_ReturnsTrue()
        {
            var scene = new SceneData("scene1", false, "trust.imperial >= 50", null);

            Assert.IsTrue(scene.HasCondition);
        }

        [Test]
        public void SceneData_HasCondition_WhenNoCondition_ReturnsFalse()
        {
            var scene = new SceneData("scene1");

            Assert.IsFalse(scene.HasCondition);
        }

        [Test]
        public void SceneData_HasCondition_WhenEmptyCondition_ReturnsFalse()
        {
            var scene = new SceneData("scene1", false, "", null);

            Assert.IsFalse(scene.HasCondition);
        }

        [Test]
        public void SceneData_BranchTargets_ReturnsCorrectDictionary()
        {
            var branchTargets = new List<BranchTarget>
            {
                new BranchTarget("default", "scene_a"),
                new BranchTarget("alt", "scene_b")
            };
            var scene = new SceneData("scene1", false, "trust.imperial >= 50", branchTargets);

            var dict = scene.BranchTargets;

            Assert.IsNotNull(dict);
            Assert.AreEqual(2, dict.Count);
            Assert.AreEqual("scene_a", dict["default"]);
            Assert.AreEqual("scene_b", dict["alt"]);
        }

        [Test]
        public void SceneData_BranchTargets_WhenNull_ReturnsNull()
        {
            var scene = new SceneData("scene1");

            Assert.IsNull(scene.BranchTargets);
        }

        [Test]
        public void SceneData_BranchTargets_WhenEmpty_ReturnsNull()
        {
            var scene = new SceneData("scene1", false, null, new List<BranchTarget>());

            Assert.IsNull(scene.BranchTargets);
        }

        [Test]
        public void SceneData_ConditionExpression_ReturnsCorrectValue()
        {
            var scene = new SceneData("scene1", false, "trust.imperial >= 50", null);

            Assert.AreEqual("trust.imperial >= 50", scene.ConditionExpression);
        }

        #endregion

        #region Integration Tests with Trust and Relationship Systems

        [Test]
        public void ResolveNextScene_WithDualTrust_EvaluatesCorrectly()
        {
            _nsm.Set("trust.imperial", 70f);
            _nsm.Set("trust.underground", 30f);
            var branchTargets = new List<BranchTarget>
            {
                new BranchTarget("default", "pro_imperial")
            };
            var scene = new SceneData("scene1", false, "trust.imperial >= 50 && trust.underground <= 40", branchTargets);

            var result = BranchingResolver.ResolveNextScene(scene, _nsm, hasMoreScenesInChapter: true);

            Assert.IsTrue(result.WasBranching);
            Assert.AreEqual("pro_imperial", result.NextSceneId);
        }

        [Test]
        public void ResolveNextScene_WithRelationship_EvaluatesCorrectly()
        {
            _nsm.Set("relationships.reina", 60f);
            var branchTargets = new List<BranchTarget>
            {
                new BranchTarget("default", "friendly_path")
            };
            var scene = new SceneData("scene1", false, "relationships.reina >= 50", branchTargets);

            var result = BranchingResolver.ResolveNextScene(scene, _nsm, hasMoreScenesInChapter: true);

            Assert.IsTrue(result.WasBranching);
            Assert.AreEqual("friendly_path", result.NextSceneId);
        }

        [Test]
        public void ResolveNextScene_WithClueFlag_EvaluatesCorrectly()
        {
            _nsm.Set("clues.foundKey", 1f);
            var branchTargets = new List<BranchTarget>
            {
                new BranchTarget("default", "found_key_scene")
            };
            var scene = new SceneData("scene1", false, "clues.foundKey == 1", branchTargets);

            var result = BranchingResolver.ResolveNextScene(scene, _nsm, hasMoreScenesInChapter: true);

            Assert.IsTrue(result.WasBranching);
            Assert.AreEqual("found_key_scene", result.NextSceneId);
        }

        [Test]
        public void ResolveNextScene_ComplexCondition_EvaluatesCorrectly()
        {
            _nsm.Set("trust.imperial", 60f);
            _nsm.Set("clues.foundKey", 1f);
            _nsm.Set("relationships.theron", 20f);
            var branchTargets = new List<BranchTarget>
            {
                new BranchTarget("default", "best_path")
            };
            // (trust.imperial >= 50 && clues.foundKey == 1) || relationships.theron > 30
            var scene = new SceneData("scene1", false, "trust.imperial >= 50 && clues.foundKey == 1 || relationships.theron > 30", branchTargets);

            var result = BranchingResolver.ResolveNextScene(scene, _nsm, hasMoreScenesInChapter: true);

            Assert.IsTrue(result.WasBranching);
            Assert.AreEqual("best_path", result.NextSceneId);
        }

        #endregion
    }
}
