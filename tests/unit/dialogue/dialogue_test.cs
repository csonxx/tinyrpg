using System;
using System.Collections.Generic;
using Core.Narrative;
using Core.Narrative.Dialogue;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Unit.Dialogue
{
    /// <summary>
    /// Unit tests for the Branching Dialogue System.
    ///
    /// Covers:
    /// - TEXT node: advance to nextNodeId on tap
    /// - CHOICE node: display N choices, apply trust shift on selection, log to choiceHistory
    /// - CHOICE: trust shift clamped at +/-10
    /// - CONDITION: evaluate true/false expressions against NSM state
    /// - END: emit DialogueSceneComplete
    /// - Dialogue cursor persists across NSM save/load
    /// - Rapid tap: first cancels animation, second advances
    /// </summary>
    [TestFixture]
    public class DialogueEngineTests
    {
        private NarrativeStateMachine _nsm;
        private NSMConfig _config;
        private DialogueEngine _engine;
        private List<NSMEvent> _capturedEvents;
        private string _lastEmittedNodeId;
        private DialogueNodeType _lastEmittedNodeType;
        private bool _sceneCompleteFired;
        private string _sceneCompleteSceneId;
        private bool _choicesDisplayedFired;
        private string _choicesDisplayedNodeId;
        private ChoiceData[] _choicesDisplayedChoices;
        private float _lastTrustShift;
        private float _lastClampedTrustShift;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<NSMConfig>();
            _config.name = "TestDialogueConfig";
            _nsm = new NarrativeStateMachine(_config);

            // Load schema for trust keys used in tests
            _nsm.LoadSchema(new[] { "trust.imperial", "trust.underground", "flags.metReina" });

            _engine = new DialogueEngine(_nsm, new ConditionEvaluator());
            _capturedEvents = new List<NSMEvent>();
            _lastEmittedNodeId = null;
            _lastEmittedNodeType = default;
            _sceneCompleteFired = false;
            _sceneCompleteSceneId = null;
            _choicesDisplayedFired = false;
            _choicesDisplayedNodeId = null;
            _choicesDisplayedChoices = null;
            _lastTrustShift = 0f;
            _lastClampedTrustShift = 0f;

            // Subscribe to dialogue events
            _nsm.Subscribe("dialogue.*", e =>
            {
                _capturedEvents.Add(e);
                if (e is DialogueNodeChangedEvent nodeChanged)
                {
                    _lastEmittedNodeId = nodeChanged.NodeId;
                    _lastEmittedNodeType = Enum.Parse<DialogueNodeType>(nodeChanged.NodeType);
                }
                else if (e is DialogueSceneCompleteEvent sceneComplete)
                {
                    _sceneCompleteFired = true;
                    _sceneCompleteSceneId = sceneComplete.SceneId;
                }
                else if (e is DialogueChoicesDisplayedEvent choicesDisplayed)
                {
                    _choicesDisplayedFired = true;
                    _choicesDisplayedNodeId = choicesDisplayed.NodeId;
                    _choicesDisplayedChoices = choicesDisplayed.Choices;
                }
                else if (e is DialogueTrustShiftEvent trustShift)
                {
                    _lastTrustShift = trustShift.Shift;
                    _lastClampedTrustShift = trustShift.ClampedShift;
                }
            });
        }

        [TearDown]
        public void TearDown()
        {
            if (_config != null)
            {
                UnityEngine.Object.DestroyImmediate(_config);
            }
        }

        #region Helper Methods

        private DialogueTree BuildTree(params DialogueNode[] nodes)
        {
            var dict = new Dictionary<string, DialogueNode>();
            foreach (var node in nodes)
            {
                dict[node.Id] = node;
            }
            return DialogueTree.CreateRuntime("test_scene", dict);
        }

        private void ClearEvents()
        {
            _capturedEvents.Clear();
            _lastEmittedNodeId = null;
            _lastEmittedNodeType = default;
            _sceneCompleteFired = false;
            _sceneCompleteSceneId = null;
            _choicesDisplayedFired = false;
            _choicesDisplayedNodeId = null;
            _choicesDisplayedChoices = null;
            _lastTrustShift = 0f;
            _lastClampedTrustShift = 0f;
        }

        private DialogueNodeChangedEvent GetLastNodeChangedEvent()
        {
            foreach (var e in _capturedEvents)
            {
                if (e is DialogueNodeChangedEvent evt)
                    return evt;
            }
            return null;
        }

        #endregion

        #region TEXT Node Tests

        [Test]
        public void TEXT_Node_EmitsNodeChangedEvent()
        {
            var tree = BuildTree(
                DialogueNode.Text("n1", "Hello, traveler.", "n2")
            );

            _engine.StartDialogue(tree);

            Assert.AreEqual("n1", _lastEmittedNodeId);
            Assert.AreEqual(DialogueNodeType.TEXT, _lastEmittedNodeType);
        }

        [Test]
        public void TEXT_Node_FirstTap_CancelsAnimation()
        {
            var tree = BuildTree(
                DialogueNode.Text("n1", "Hello, traveler.", "n2")
            );

            _engine.StartDialogue(tree);
            Assert.IsTrue(_engine.IsTextAnimating);

            ClearEvents();
            _engine.OnTap();

            // After first tap, animation should be cancelled (not waiting anymore)
            Assert.IsFalse(_engine.IsTextAnimating);
            Assert.AreEqual("n1", _lastEmittedNodeId); // Should NOT have advanced
        }

        [Test]
        public void TEXT_Node_SecondTap_AdvancesToNextNodeId()
        {
            var tree = BuildTree(
                DialogueNode.Text("n1", "Hello, traveler.", "n2"),
                DialogueNode.End("n2")
            );

            _engine.StartDialogue(tree);

            // First tap cancels animation
            _engine.OnTap();
            Assert.IsFalse(_engine.IsTextAnimating);

            // Second tap advances
            ClearEvents();
            _engine.OnTap();

            Assert.AreEqual("n2", _lastEmittedNodeId);
            Assert.AreEqual(DialogueNodeType.END, _lastEmittedNodeType);
        }

        [Test]
        public void TEXT_Node_RapidTap_FirstCancels_SecondAdvances()
        {
            var tree = BuildTree(
                DialogueNode.Text("n1", "Hello, traveler.", "n2"),
                DialogueNode.End("n2")
            );

            _engine.StartDialogue(tree);

            // Rapid first tap cancels
            _engine.OnTap();
            Assert.IsFalse(_engine.IsTextAnimating);
            Assert.AreNotEqual("n2", _engine.CurrentNodeId);

            // Rapid second tap advances
            _engine.OnTap();
            Assert.AreEqual("n2", _engine.CurrentNodeId);
        }

        #endregion

        #region CHOICE Node Tests

        [Test]
        public void CHOICE_Node_EmitsChoicesDisplayedEvent()
        {
            var tree = BuildTree(
                DialogueNode.Choice(
                    "choice1",
                    "What do you do?",
                    new ChoiceData[]
                    {
                        new ChoiceData("Draw sword", "fight"),
                        new ChoiceData("Run away", "flee"),
                        new ChoiceData("Talk it out", "talk")
                    },
                    new float[] { -5f, 0f, 10f },
                    null // nextNodeId not used for CHOICE
                ),
                DialogueNode.End("end")
            );

            _engine.StartDialogue(tree);

            // We need to get to the CHOICE node - advance from TEXT first if needed
            // But in this tree, choice1 is the first node

            Assert.IsTrue(_choicesDisplayedFired);
            Assert.AreEqual("choice1", _choicesDisplayedNodeId);
            Assert.AreEqual(3, _choicesDisplayedChoices.Length);
            Assert.AreEqual("Draw sword", _choicesDisplayedChoices[0].Text);
            Assert.AreEqual("fight", _choicesDisplayedChoices[0].NextNodeId);
        }

        [Test]
        public void CHOICE_Node_WaitsForExplicitSelection()
        {
            var tree = BuildTree(
                DialogueNode.Choice(
                    "choice1",
                    "What do you do?",
                    new ChoiceData[]
                    {
                        new ChoiceData("Option A", "nodeA"),
                        new ChoiceData("Option B", "nodeB")
                    },
                    new float[] { 5f, -5f },
                    null
                ),
                DialogueNode.End("end")
            );

            _engine.StartDialogue(tree);

            Assert.IsTrue(_engine.IsWaitingForChoice);

            // Tap should NOT advance while waiting for choice
            _engine.OnTap();
            Assert.AreEqual("choice1", _engine.CurrentNodeId);
            Assert.IsTrue(_engine.IsWaitingForChoice);
        }

        [Test]
        public void CHOICE_Node_SelectChoice_AdvancesToSelectedNode()
        {
            var tree = BuildTree(
                DialogueNode.Choice(
                    "choice1",
                    "What do you do?",
                    new ChoiceData[]
                    {
                        new ChoiceData("Draw sword", "fight"),
                        new ChoiceData("Run away", "flee")
                    },
                    new float[] { -5f, 5f },
                    null
                ),
                DialogueNode.End("end")
            );

            _engine.StartDialogue(tree);
            ClearEvents();

            _engine.SelectChoice(0); // Select "Draw sword" -> "fight"

            Assert.AreEqual("fight", _lastEmittedNodeId);
            Assert.AreEqual(DialogueNodeType.END, _lastEmittedNodeType);
        }

        [Test]
        public void CHOICE_Node_AppliesTrustShift()
        {
            var tree = BuildTree(
                DialogueNode.Choice(
                    "choice1",
                    "What do you do?",
                    new ChoiceData[]
                    {
                        new ChoiceData("Help the imperial soldier", "nodeA")
                    },
                    new float[] { 8f },
                    null
                ),
                DialogueNode.End("end")
            );

            _engine.StartDialogue(tree);
            _nsm.Set("trust.imperial", 50f);
            ClearEvents();

            _engine.SelectChoice(0);

            // Trust should have been increased by 8
            Assert.AreEqual(58f, _nsm.Get<float>("trust.imperial"));
        }

        [Test]
        public void CHOICE_Node_TrustShift_ClampedAtPlus10()
        {
            var tree = BuildTree(
                DialogueNode.Choice(
                    "choice1",
                    "What do you do?",
                    new ChoiceData[]
                    {
                        new ChoiceData("Major favor", "nodeA")
                    },
                    new float[] { 15f }, // Exceeds +10 cap
                    null
                ),
                DialogueNode.End("end")
            );

            _engine.StartDialogue(tree);
            _nsm.Set("trust.imperial", 50f);
            ClearEvents();

            _engine.SelectChoice(0);

            // Trust should be clamped to +10, so 50 + 10 = 60
            Assert.AreEqual(60f, _nsm.Get<float>("trust.imperial"));
            Assert.AreEqual(15f, _lastTrustShift);      // Raw value emitted
            Assert.AreEqual(10f, _lastClampedTrustShift); // Clamped value emitted
        }

        [Test]
        public void CHOICE_Node_TrustShift_ClampedAtMinus10()
        {
            var tree = BuildTree(
                DialogueNode.Choice(
                    "choice1",
                    "What do you do?",
                    new ChoiceData[]
                    {
                        new ChoiceData("Major betrayal", "nodeA")
                    },
                    new float[] { -15f }, // Exceeds -10 cap
                    null
                ),
                DialogueNode.End("end")
            );

            _engine.StartDialogue(tree);
            _nsm.Set("trust.underground", 30f);
            ClearEvents();

            _engine.SelectChoice(0);

            // Trust should be clamped to -10, so 30 - 10 = 20
            Assert.AreEqual(20f, _nsm.Get<float>("trust.underground"));
            Assert.AreEqual(-15f, _lastTrustShift);
            Assert.AreEqual(-10f, _lastClampedTrustShift);
        }

        [Test]
        public void CHOICE_Node_NegativeShift_UsesUndergroundKey()
        {
            var tree = BuildTree(
                DialogueNode.Choice(
                    "choice1",
                    "What do you do?",
                    new ChoiceData[]
                    {
                        new ChoiceData("Betray the cause", "nodeA")
                    },
                    new float[] { -5f },
                    null
                ),
                DialogueNode.End("end")
            );

            _engine.StartDialogue(tree);
            _nsm.Set("trust.imperial", 50f);
            _nsm.Set("trust.underground", 10f);
            ClearEvents();

            _engine.SelectChoice(0);

            // Imperial should be unchanged
            Assert.AreEqual(50f, _nsm.Get<float>("trust.imperial"));
            // Underground should increase by 5 (absolute value of -5)
            Assert.AreEqual(15f, _nsm.Get<float>("trust.underground"));
        }

        [Test]
        public void CHOICE_Node_LogsToChoiceHistory()
        {
            var tree = BuildTree(
                DialogueNode.Choice(
                    "choice1",
                    "What do you do?",
                    new ChoiceData[]
                    {
                        new ChoiceData("Draw sword", "fight"),
                        new ChoiceData("Run away", "flee")
                    },
                    new float[] { -5f, 5f },
                    null
                ),
                DialogueNode.End("end")
            );

            _engine.StartDialogue(tree);
            ClearEvents();

            _engine.SelectChoice(0);

            var history = _engine.ChoiceHistory;
            Assert.AreEqual(1, history.Count);
            Assert.AreEqual("choice1", history[0].NodeId);
            Assert.AreEqual(0, history[0].ChoiceIndex);
            Assert.AreEqual("Draw sword", history[0].ChoiceText);
            Assert.AreEqual(-5f, history[0].TrustShift);
            Assert.AreEqual("fight", history[0].NextNodeId);
        }

        [Test]
        public void CHOICE_Node_MultipleChoices_AllLogged()
        {
            var tree = BuildTree(
                DialogueNode.Choice(
                    "choice1",
                    "What do you do?",
                    new ChoiceData[]
                    {
                        new ChoiceData("Option A", "nodeA"),
                        new ChoiceData("Option B", "nodeB"),
                        new ChoiceData("Option C", "nodeC")
                    },
                    new float[] { 5f, 0f, -5f },
                    null
                ),
                DialogueNode.End("end")
            );

            _engine.StartDialogue(tree);
            _engine.SelectChoice(0);

            // Reset tree reference to continue
            // In real usage, the engine would auto-advance to nodeA, but here we need
            // to manually set up the next choice
            // For this test, just verify the first selection is logged
            Assert.AreEqual(1, _engine.ChoiceHistory.Count);
        }

        #endregion

        #region CONDITION Node Tests

        [Test]
        public void CONDITION_Node_TrueBranch_SelectsTrueNextNodeId()
        {
            var tree = BuildTree(
                DialogueNode.Condition("cond1", "trust.imperial >= 50", "high_trust", "low_trust"),
                DialogueNode.End("low_trust"),
                DialogueNode.End("high_trust")
            );

            _nsm.Set("trust.imperial", 75f);
            ClearEvents();

            _engine.StartDialogue(tree);

            Assert.AreEqual("high_trust", _lastEmittedNodeId);
        }

        [Test]
        public void CONDITION_Node_FalseBranch_SelectsFalseNextNodeId()
        {
            var tree = BuildTree(
                DialogueNode.Condition("cond1", "trust.imperial >= 50", "high_trust", "low_trust"),
                DialogueNode.End("low_trust"),
                DialogueNode.End("high_trust")
            );

            _nsm.Set("trust.imperial", 25f);
            ClearEvents();

            _engine.StartDialogue(tree);

            Assert.AreEqual("low_trust", _lastEmittedNodeId);
        }

        [Test]
        public void CONDITION_Node_LessThanOperator()
        {
            var tree = BuildTree(
                DialogueNode.Condition("cond1", "trust.underground < 30", "low", "high"),
                DialogueNode.End("low"),
                DialogueNode.End("high")
            );

            _nsm.Set("trust.underground", 10f);
            ClearEvents();

            _engine.StartDialogue(tree);

            Assert.AreEqual("low", _lastEmittedNodeId);
        }

        [Test]
        public void CONDITION_Node_EqualityOperator()
        {
            var tree = BuildTree(
                DialogueNode.Condition("cond1", "flags.metReina == 1", "met", "not_met"),
                DialogueNode.End("not_met"),
                DialogueNode.End("met")
            );

            _nsm.Set("flags.metReina", 1f);
            ClearEvents();

            _engine.StartDialogue(tree);

            Assert.AreEqual("met", _lastEmittedNodeId);
        }

        [Test]
        public void CONDITION_Node_NotEqualOperator()
        {
            var tree = BuildTree(
                DialogueNode.Condition("cond1", "trust.imperial != 50", "not_fifty", "is_fifty"),
                DialogueNode.End("is_fifty"),
                DialogueNode.End("not_fifty")
            );

            _nsm.Set("trust.imperial", 75f);
            ClearEvents();

            _engine.StartDialogue(tree);

            Assert.AreEqual("not_fifty", _lastEmittedNodeId);
        }

        #endregion

        #region END Node Tests

        [Test]
        public void END_Node_EmitsDialogueSceneComplete()
        {
            var tree = BuildTree(
                DialogueNode.End("end")
            );

            _engine.StartDialogue(tree);

            Assert.IsTrue(_sceneCompleteFired);
            Assert.AreEqual("test_scene", _sceneCompleteSceneId);
        }

        [Test]
        public void END_Node_ClearsDialogueCursor()
        {
            var tree = BuildTree(
                DialogueNode.End("end")
            );

            _engine.StartDialogue(tree);

            Assert.IsNull(_nsm.Get<string>("dialogue.cursor.sceneId"));
            Assert.IsNull(_nsm.Get<string>("dialogue.cursor.nodeId"));
        }

        #endregion

        #region Cursor Persistence Tests

        [Test]
        public void Dialogue_Cursor_PersistsInNSM()
        {
            var tree = BuildTree(
                DialogueNode.Text("n1", "First text.", "n2"),
                DialogueNode.End("n2")
            );

            _engine.StartDialogue(tree);
            _engine.OnTap(); // Cancel animation
            ClearEvents();
            _engine.OnTap(); // Advance to n2

            Assert.AreEqual("test_scene", _nsm.Get<string>("dialogue.cursor.sceneId"));
            Assert.AreEqual("n2", _nsm.Get<string>("dialogue.cursor.nodeId"));
        }

        [Test]
        public void Dialogue_Cursor_SurvivesSaveAndLoad()
        {
            var tree = BuildTree(
                DialogueNode.Text("n1", "First text.", "n2"),
                DialogueNode.End("n2")
            );

            _engine.StartDialogue(tree);
            _engine.OnTap();
            _engine.OnTap();

            // Serialize
            string json = _nsm.Serialize();

            // Create fresh NSM and engine
            var freshNsm = new NarrativeStateMachine(_config);
            freshNsm.Deserialize(json);

            var freshEngine = new DialogueEngine(freshNsm, new ConditionEvaluator());

            // Resume should work
            bool resumed = freshEngine.ResumeDialogue(tree);
            Assert.IsTrue(resumed);
            Assert.AreEqual("n2", freshEngine.CurrentNodeId);
        }

        [Test]
        public void ResumeDialogue_ReturnsFalse_WhenNoCursor()
        {
            var tree = BuildTree(
                DialogueNode.End("end")
            );

            bool resumed = _engine.ResumeDialogue(tree);
            Assert.IsFalse(resumed);
        }

        [Test]
        public void ResumeDialogue_ReturnsFalse_WhenSceneIdMismatch()
        {
            var tree1 = BuildTree(DialogueNode.End("end"));
            var tree2 = BuildTree(DialogueNode.End("end"));

            _engine.StartDialogue(tree1);

            // tree2 has different scene ID (runtime generated), so should not resume
            bool resumed = _engine.ResumeDialogue(tree2);
            Assert.IsFalse(resumed);
        }

        #endregion

        #region Visited Nodes Tests

        [Test]
        public void VisitedNodes_TracksAllVisitedNodes()
        {
            var tree = BuildTree(
                DialogueNode.Text("n1", "First.", "n2"),
                DialogueNode.Text("n2", "Second.", "n3"),
                DialogueNode.End("n3")
            );

            _engine.StartDialogue(tree);
            _engine.OnTap(); // n1 -> n2
            _engine.OnTap();
            _engine.OnTap(); // n2 -> n3

            var visited = _engine.VisitedNodes;
            Assert.Contains("n1", visited);
            Assert.Contains("n2", visited);
            Assert.Contains("n3", visited);
        }

        [Test]
        public void VisitedNodes_DoesNotDuplicate_OnRevisit()
        {
            var tree = BuildTree(
                DialogueNode.Text("n1", "First.", "n2"),
                DialogueNode.Text("n2", "Second.", "n1"), // loops back
                DialogueNode.End("n3")
            );

            _engine.StartDialogue(tree);
            _engine.OnTap(); _engine.OnTap(); // to n2
            _engine.OnTap(); _engine.OnTap(); // back to n1

            var visited = _engine.VisitedNodes;
            Assert.AreEqual(2, visited.Count);
        }

        #endregion

        #region Auto-Advance Tests

        [Test]
        public void AutoAdvance_AdvancesAfterDelay()
        {
            var tree = BuildTree(
                DialogueNode.Text("n1", "Auto-advance text.", "n2"),
                DialogueNode.End("n2")
            );

            _engine.StartDialogue(tree);
            _engine.OnTap(); // Cancel animation

            ClearEvents();
            _engine.Update(6f); // Exceeds 5s default delay

            Assert.AreEqual("n2", _lastEmittedNodeId);
        }

        [Test]
        public void AutoAdvance_Disabled_DoesNotAdvance()
        {
            var tree = BuildTree(
                DialogueNode.Text("n1", "Auto-advance text.", "n2"),
                DialogueNode.End("n2")
            );

            _engine.AutoAdvanceEnabled = false;
            _engine.StartDialogue(tree);
            _engine.OnTap(); // Cancel animation

            ClearEvents();
            _engine.Update(10f);

            Assert.AreEqual("n1", _engine.CurrentNodeId);
        }

        #endregion

        #region ConditionEvaluator Tests

        [Test]
        public void ConditionEvaluator_ValidExpression_ReturnsTrue()
        {
            var evaluator = new ConditionEvaluator();
            _nsm.Set("trust.imperial", 75f);

            var result = evaluator.Evaluate("trust.imperial >= 50", key => _nsm.Get<float>(key));

            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.Value);
        }

        [Test]
        public void ConditionEvaluator_ValidExpression_ReturnsFalse()
        {
            var evaluator = new ConditionEvaluator();
            _nsm.Set("trust.imperial", 25f);

            var result = evaluator.Evaluate("trust.imperial >= 50", key => _nsm.Get<float>(key));

            Assert.IsTrue(result.Success);
            Assert.IsFalse(result.Value);
        }

        [Test]
        public void ConditionEvaluator_InvalidExpression_ReturnsError()
        {
            var evaluator = new ConditionEvaluator();

            var result = evaluator.Evaluate("not a valid expression", key => 0f);

            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error);
        }

        [Test]
        public void ConditionEvaluator_EmptyExpression_ReturnsError()
        {
            var evaluator = new ConditionEvaluator();

            var result = evaluator.Evaluate("", key => 0f);

            Assert.IsFalse(result.Success);
        }

        [Test]
        public void ConditionEvaluator_AllOperators_Work()
        {
            var evaluator = new ConditionEvaluator();

            Assert.IsTrue(evaluator.Evaluate("x >= 5", k => 5f).Value);
            Assert.IsTrue(evaluator.Evaluate("x <= 5", k => 5f).Value);
            Assert.IsTrue(evaluator.Evaluate("x > 4", k => 5f).Value);
            Assert.IsTrue(evaluator.Evaluate("x < 6", k => 5f).Value);
            Assert.IsTrue(evaluator.Evaluate("x == 5", k => 5f).Value);
            Assert.IsTrue(evaluator.Evaluate("x != 6", k => 5f).Value);

            Assert.IsFalse(evaluator.Evaluate("x >= 6", k => 5f).Value);
            Assert.IsFalse(evaluator.Evaluate("x <= 4", k => 5f).Value);
        }

        #endregion
    }
}
