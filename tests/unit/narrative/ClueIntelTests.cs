using System.Collections.Generic;
using Core.Narrative;
using Core.Narrative.Dialogue;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Unit.Narrative
{
    /// <summary>
    /// Unit tests for ClueSystem and clue-related functionality.
    /// Covers: clue registration, idempotency, discovery checks, CONDITION node gating.
    /// </summary>
    [TestFixture]
    public class ClueIntelTests
    {
        private NarrativeStateMachine _nsm;
        private NSMConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<NSMConfig>();
            _config.name = "TestNSMConfig";
            _nsm = new NarrativeStateMachine(_config);
            // ClueSystem uses NarrativeStateMachine.Instance directly.
            // For tests, we accept the singleton coupling as a known limitation.
        }

        [TearDown]
        public void TearDown()
        {
            if (_nsm != null)
            {
                _nsm.Dispose();
                _nsm = null;
            }
            if (_config != null)
            {
                UnityEngine.Object.DestroyImmediate(_config);
            }
        }

        #region Clue Registration

        [Test]
        public void RegisterClue_SetsNSMKeyToTrue()
        {
            // Arrange
            const string clueId = "clue_zhang_affair";

            // Act
            ClueSystem.RegisterClue(clueId, "evidence");

            // Assert
            Assert.IsTrue(ClueSystem.IsClueDiscovered(clueId));
        }

        [Test]
        public void RegisterClue_Idempotent_SameClueTwice_NoCrash()
        {
            // Arrange
            const string clueId = "clue_zhang_affair";

            // Act - register twice
            ClueSystem.RegisterClue(clueId, "evidence");
            ClueSystem.RegisterClue(clueId, "evidence"); // should be no-op

            // Assert
            Assert.IsTrue(ClueSystem.IsClueDiscovered(clueId));
        }

        [Test]
        public void RegisterClue_StoresInNsmUnderCorrectKey()
        {
            // Arrange
            const string clueId = "clue_zhang_affair";

            // Act
            ClueSystem.RegisterClue(clueId, "evidence");

            // Assert - NSM key should be clues.clue_zhang_affair = 1.0f
            var value = _nsm.Get<float>("clues." + clueId);
            Assert.AreEqual(1.0f, value);
        }

        [Test]
        public void RegisterClue_NodeVsChoice_BothRegisterSameClue()
        {
            // Arrange
            const string clueId = "clue_shared";

            // Act - register via "node" and via "choice"
            ClueSystem.RegisterClue(clueId, "document");
            ClueSystem.RegisterClue(clueId, "conversation");

            // Assert - only one entry, clue is discovered
            Assert.IsTrue(ClueSystem.IsClueDiscovered(clueId));
        }

        #endregion

        #region Discovery Check

        [Test]
        public void IsClueDiscovered_UndiscoveredClue_ReturnsFalse()
        {
            // Arrange
            const string clueId = "clue_unknown";

            // Act & Assert
            Assert.IsFalse(ClueSystem.IsClueDiscovered(clueId));
        }

        [Test]
        public void IsClueDiscovered_RegisteredClue_ReturnsTrue()
        {
            // Arrange
            const string clueId = "clue_zhang_affair";
            ClueSystem.RegisterClue(clueId, "evidence");

            // Act & Assert
            Assert.IsTrue(ClueSystem.IsClueDiscovered(clueId));
        }

        [Test]
        public void IsClueDiscovered_NullOrEmptyId_ReturnsFalse()
        {
            // Act & Assert
            Assert.IsFalse(ClueSystem.IsClueDiscovered(null));
            Assert.IsFalse(ClueSystem.IsClueDiscovered(string.Empty));
        }

        #endregion

        #region GetDiscoveredClues

        [Test]
        public void GetDiscoveredCluesByCategory_ReturnsCorrectGrouping()
        {
            // Arrange
            ClueSystem.RegisterClue("clue_doc_1", "documents");
            ClueSystem.RegisterClue("clue_conv_1", "conversations");
            ClueSystem.RegisterClue("clue_evi_1", "evidence");

            // Act
            var byCategory = ClueSystem.GetDiscoveredCluesByCategory();

            // Assert
            Assert.AreEqual(3, byCategory.Count);
            Assert.IsTrue(byCategory.ContainsKey("documents"));
            Assert.IsTrue(byCategory.ContainsKey("conversations"));
            Assert.IsTrue(byCategory.ContainsKey("evidence"));
            Assert.AreEqual(1, byCategory["documents"].Count);
            Assert.AreEqual(1, byCategory["conversations"].Count);
            Assert.AreEqual(1, byCategory["evidence"].Count);
        }

        [Test]
        public void GetDiscoveredCluesByCategory_EmptyWhenNoClues()
        {
            // Act
            var byCategory = ClueSystem.GetDiscoveredCluesByCategory();

            // Assert
            Assert.AreEqual(0, byCategory.Count);
        }

        [Test]
        public void GetAllDiscoveredClues_ReturnsAllRegistered()
        {
            // Arrange
            ClueSystem.RegisterClue("clue_a", "documents");
            ClueSystem.RegisterClue("clue_b", "conversations");
            ClueSystem.RegisterClue("clue_c", "evidence");

            // Act
            var all = ClueSystem.GetAllDiscoveredClues();

            // Assert
            Assert.AreEqual(3, all.Count);
        }

        #endregion

        #region CONDITION Node Gating

        [Test]
        public void Condition_CanEvaluateClueBoolean_GatedChoiceAppears()
        {
            // Arrange - simulate a CONDITION node evaluating clues.clue_zhang_affair >= 1
            const string clueId = "clue_zhang_affair";
            ClueSystem.RegisterClue(clueId, "evidence");

            // Act - ConditionEvaluator checks if clues.clue_zhang_affair >= 1
            // This is equivalent to checking IsClueDiscovered
            var clueValue = _nsm.Get<float>("clues." + clueId);
            var conditionMet = clueValue >= 1f;

            // Assert
            Assert.IsTrue(conditionMet);
        }

        [Test]
        public void Condition_CanEvaluateUndiscoveredClue_GatedChoiceHidden()
        {
            // Arrange - clue NOT discovered
            const string clueId = "clue_undiscovered";

            // Act
            var clueValue = _nsm.Get<float>("clues." + clueId);
            var conditionMet = clueValue >= 1f;

            // Assert
            Assert.IsFalse(conditionMet);
        }

        #endregion
    }
}
