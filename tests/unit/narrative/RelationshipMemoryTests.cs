using System;
using System.Collections.Generic;
using System.Linq;
using Core.Narrative;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Unit.Narrative
{
    /// <summary>
    /// Unit tests for RelationshipMemorySystem.
    /// Tests: relationship value storage, memory flags, shift application,
    /// clamping, passive decay grace period, and event emission.
    /// </summary>
    [TestFixture]
    public class RelationshipMemoryTests
    {
        #region Test Subject & Mocks

        private RelationshipMemorySystem _system;
        private RelationshipMemoryConfig _config;
        private NarrativeStateMachine _nsm;
        private List<NSMEvent> _capturedEvents;
        private List<string> _receivedKeys;

        #endregion

        #region Test Lifecycle

        [SetUp]
        public void SetUp()
        {
            // Create config
            _config = ScriptableObject.CreateInstance<RelationshipMemoryConfig>();
            _config.name = "TestRelationshipConfig";
            _config.DefaultRelationshipValue = 50f;
            _config.DecayGracePeriodSeconds = 120f;
            _config.DecayIntervalSeconds = 60f;
            _config.DecayAmountPerTick = 1f;
            _config.MaxShiftPerChoice = 10f;
            _config.MinRelationshipValue = 0f;
            _config.MaxRelationshipValue = 100f;

            // Create NSM
            var nsmConfig = ScriptableObject.CreateInstance<NSMConfig>();
            nsmConfig.name = "TestNSMConfig";
            _nsm = new NarrativeStateMachine(nsmConfig);

            // Create RelationshipMemorySystem GameObject
            var go = new GameObject("RelationshipMemorySystemUnderTest");
            _system = go.AddComponent<RelationshipMemorySystem>();

            // Inject config via reflection (SerializeField injection)
            var configField = typeof(RelationshipMemorySystem)
                .GetField("_config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            configField.SetValue(_system, _config);

            // Initialize system with autoStartDecay = false for deterministic testing
            var autoStartField = typeof(RelationshipMemorySystem)
                .GetField("_autoStartDecay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            autoStartField.SetValue(_system, false);

            _system.Initialize(_nsm);

            // Setup event capture
            _capturedEvents = new List<NSMEvent>();
            _receivedKeys = new List<string>();
            _nsm.Subscribe("*", e =>
            {
                _capturedEvents.Add(e);
                _receivedKeys.Add(e.Key);
            });
        }

        [TearDown]
        public void TearDown()
        {
            if (_system != null)
            {
                UnityEngine.Object.DestroyImmediate(_system.gameObject);
            }
            if (_config != null)
            {
                UnityEngine.Object.DestroyImmediate(_config);
            }
        }

        private void ClearEvents()
        {
            _capturedEvents.Clear();
            _receivedKeys.Clear();
        }

        #endregion

        #region RelationshipShift Struct Tests

        [Test]
        public void RelationshipShift_SingleCharacter_CreatesCorrectly()
        {
            var shift = new RelationshipShift("ZHANG", 5f);

            Assert.AreEqual(1, shift.Shifts.Count);
            Assert.AreEqual(5f, shift.GetDelta("ZHANG"));
            Assert.IsTrue(shift.AffectsCharacter("ZHANG"));
            Assert.IsFalse(shift.AffectsCharacter("MARCUS"));
        }

        [Test]
        public void RelationshipShift_MultipleCharacters_CreatesCorrectly()
        {
            var shifts = new Dictionary<string, float>
            {
                { "ZHANG", 5f },
                { "MARCUS", -3f }
            };
            var shift = new RelationshipShift(shifts);

            Assert.AreEqual(2, shift.Shifts.Count);
            Assert.AreEqual(5f, shift.GetDelta("ZHANG"));
            Assert.AreEqual(-3f, shift.GetDelta("MARCUS"));
            Assert.AreEqual(0f, shift.GetDelta("UNKNOWN"));
        }

        [Test]
        public void RelationshipShift_Equality_Works()
        {
            var shift1 = new RelationshipShift("ZHANG", 5f);
            var shift2 = new RelationshipShift("ZHANG", 5f);
            var shift3 = new RelationshipShift("ZHANG", 3f);
            var shift4 = new RelationshipShift("MARCUS", 5f);

            Assert.AreEqual(shift1, shift2);
            Assert.AreNotEqual(shift1, shift3);
            Assert.AreNotEqual(shift1, shift4);
        }

        [Test]
        public void RelationshipShift_IsEmpty_TrueWhenEmpty()
        {
            var emptyShift = new RelationshipShift(new Dictionary<string, float>());
            Assert.IsTrue(emptyShift.IsEmpty);

            var validShift = new RelationshipShift("ZHANG", 5f);
            Assert.IsFalse(validShift.IsEmpty);
        }

        [Test]
        public void RelationshipShift_GetDelta_ReturnsZeroForMissingCharacter()
        {
            var shift = new RelationshipShift("ZHANG", 5f);
            Assert.AreEqual(0f, shift.GetDelta("MARCUS"));
        }

        #endregion

        #region NSM Key Helper Tests

        [Test]
        public void RelationshipKey_FormatsCorrectly()
        {
            Assert.AreEqual("relationships.ZHANG", RelationshipMemorySystem.RelationshipKey("ZHANG"));
        }

        [Test]
        public void MemoryFlagKey_FormatsCorrectly()
        {
            Assert.AreEqual("relationships.ZHANG.sawThrough_lie",
                RelationshipMemorySystem.MemoryFlagKey("ZHANG", "sawThrough_lie"));
        }

        [Test]
        public void LastInteractionKey_FormatsCorrectly()
        {
            Assert.AreEqual("relationships.ZHANG.lastInteraction",
                RelationshipMemorySystem.LastInteractionKey("ZHANG"));
        }

        #endregion

        #region GetRelationshipValue Tests

        [Test]
        public void GetRelationshipValue_DefaultsToConfigValue_WhenNoRelationshipExists()
        {
            float value = _system.GetRelationshipValue("ZHANG");
            Assert.AreEqual(50f, value);
        }

        [Test]
        public void GetRelationshipValue_ReturnsStoredValue()
        {
            _nsm.Set(RelationshipMemorySystem.RelationshipKey("ZHANG"), 75f);

            float value = _system.GetRelationshipValue("ZHANG");
            Assert.AreEqual(75f, value);
        }

        [Test]
        public void GetRelationshipValue_ReturnsZeroForEmptyCharacterId()
        {
            Assert.AreEqual(0f, _system.GetRelationshipValue(null));
            Assert.AreEqual(0f, _system.GetRelationshipValue(""));
        }

        #endregion

        #region Memory Flag Tests

        [Test]
        public void SetMemoryFlag_StoresValueInNSM()
        {
            _system.SetMemoryFlag("ZHANG", "sawThrough_lie", true);

            Assert.IsTrue(_nsm.Get<bool>(RelationshipMemorySystem.MemoryFlagKey("ZHANG", "sawThrough_lie")));
        }

        [Test]
        public void GetMemoryFlag_ReturnsStoredValue()
        {
            _nsm.Set(RelationshipMemorySystem.MemoryFlagKey("ZHANG", "knows_secret"), true);

            Assert.IsTrue(_system.GetMemoryFlag("ZHANG", "knows_secret"));
        }

        [Test]
        public void GetMemoryFlag_ReturnsFalseForMissingFlag()
        {
            Assert.IsFalse(_system.GetMemoryFlag("ZHANG", "nonexistent_flag"));
        }

        [Test]
        public void SetMemoryFlag_EmitsEventOnChange()
        {
            ClearEvents();

            _system.SetMemoryFlag("ZHANG", "new_flag", true);

            var flagEvent = _capturedEvents.Find(e => e is MemoryFlagChangedEvent) as MemoryFlagChangedEvent;
            Assert.IsNotNull(flagEvent);
            Assert.AreEqual("ZHANG", flagEvent.CharacterId);
            Assert.AreEqual("new_flag", flagEvent.FlagName);
            Assert.IsTrue(flagEvent.Value);
        }

        [Test]
        public void SetMemoryFlag_NoEventWhenValueUnchanged()
        {
            _nsm.Set(RelationshipMemorySystem.MemoryFlagKey("ZHANG", "existing_flag"), true);
            ClearEvents();

            _system.SetMemoryFlag("ZHANG", "existing_flag", true);

            var flagEvent = _capturedEvents.Find(e => e is MemoryFlagChangedEvent);
            Assert.IsNull(flagEvent);
        }

        [Test]
        public void SetMemoryFlag_IgnoresNullOrEmptyInputs()
        {
            // Should not throw
            Assert.DoesNotThrow(() => _system.SetMemoryFlag(null, "flag", true));
            Assert.DoesNotThrow(() => _system.SetMemoryFlag("", "flag", true));
            Assert.DoesNotThrow(() => _system.SetMemoryFlag("ZHANG", null, true));
            Assert.DoesNotThrow(() => _system.SetMemoryFlag("ZHANG", "", true));
        }

        #endregion

        #region ApplyShift Tests

        [Test]
        public void ApplyShift_AppliesDeltaToCharacter()
        {
            _nsm.Set(RelationshipMemorySystem.RelationshipKey("ZHANG"), 50f);
            var shift = new RelationshipShift("ZHANG", 10f);

            _system.ApplyShift(shift);

            Assert.AreEqual(60f, _system.GetRelationshipValue("ZHANG"));
        }

        [Test]
        public void ApplyShift_NegativeDelta_DecreasesValue()
        {
            _nsm.Set(RelationshipMemorySystem.RelationshipKey("ZHANG"), 50f);
            var shift = new RelationshipShift("ZHANG", -10f);

            _system.ApplyShift(shift);

            Assert.AreEqual(40f, _system.GetRelationshipValue("ZHANG"));
        }

        [Test]
        public void ApplyShift_ClampsToMaxValue()
        {
            _nsm.Set(RelationshipMemorySystem.RelationshipKey("ZHANG"), 95f);
            var shift = new RelationshipShift("ZHANG", 10f);

            _system.ApplyShift(shift);

            Assert.AreEqual(100f, _system.GetRelationshipValue("ZHANG"));
        }

        [Test]
        public void ApplyShift_ClampsToMinValue()
        {
            _nsm.Set(RelationshipMemorySystem.RelationshipKey("ZHANG"), 5f);
            var shift = new RelationshipShift("ZHANG", -10f);

            _system.ApplyShift(shift);

            Assert.AreEqual(0f, _system.GetRelationshipValue("ZHANG"));
        }

        [Test]
        public void ApplyShift_RespectsMaxShiftPerChoice()
        {
            _nsm.Set(RelationshipMemorySystem.RelationshipKey("ZHANG"), 50f);
            var shift = new RelationshipShift("ZHANG", 20f); // exceeds max of 10

            _system.ApplyShift(shift);

            Assert.AreEqual(60f, _system.GetRelationshipValue("ZHANG")); // only +10 applied
        }

        [Test]
        public void ApplyShift_EmitsRelationshipShiftAppliedEvent()
        {
            ClearEvents();
            var shift = new RelationshipShift("ZHANG", 5f);

            _system.ApplyShift(shift);

            var appliedEvent = _capturedEvents.Find(e => e is RelationshipShiftAppliedEvent) as RelationshipShiftAppliedEvent;
            Assert.IsNotNull(appliedEvent);
            Assert.AreEqual(5f, appliedEvent.Shift.GetDelta("ZHANG"));
        }

        [Test]
        public void ApplyShift_EmitsRelationshipValueChangedEvent()
        {
            _nsm.Set(RelationshipMemorySystem.RelationshipKey("ZHANG"), 50f);
            ClearEvents();
            var shift = new RelationshipShift("ZHANG", 5f);

            _system.ApplyShift(shift);

            var valueEvent = _capturedEvents.Find(e => e is RelationshipValueChangedEvent) as RelationshipValueChangedEvent;
            Assert.IsNotNull(valueEvent);
            Assert.AreEqual("ZHANG", valueEvent.CharacterId);
            Assert.AreEqual(50f, valueEvent.OldValue);
            Assert.AreEqual(55f, valueEvent.NewValue);
            Assert.AreEqual(5f, valueEvent.Delta);
        }

        [Test]
        public void ApplyShift_UpdatesLastInteractionTime()
        {
            var shift = new RelationshipShift("ZHANG", 5f);

            _system.ApplyShift(shift);

            float lastInteraction = _nsm.Get<float>(RelationshipMemorySystem.LastInteractionKey("ZHANG"));
            Assert.Greater(lastInteraction, 0f);
        }

        [Test]
        public void ApplyShift_MultipleCharacters_AppliesAll()
        {
            var shifts = new Dictionary<string, float>
            {
                { "ZHANG", 5f },
                { "MARCUS", 10f }
            };
            var shift = new RelationshipShift(shifts);

            _system.ApplyShift(shift);

            Assert.AreEqual(55f, _system.GetRelationshipValue("ZHANG"));
            Assert.AreEqual(60f, _system.GetRelationshipValue("MARCUS"));
        }

        [Test]
        public void ApplyShift_IgnoresEmptyShift()
        {
            _nsm.Set(RelationshipMemorySystem.RelationshipKey("ZHANG"), 50f);
            ClearEvents();
            var emptyShift = new RelationshipShift(new Dictionary<string, float>());

            _system.ApplyShift(emptyShift);

            // No events should be emitted
            Assert.AreEqual(0, _capturedEvents.Count);
            Assert.AreEqual(50f, _system.GetRelationshipValue("ZHANG")); // unchanged
        }

        #endregion

        #region ForceDecayTick Tests

        [Test]
        public void ForceDecayTick_ReducesRelationshipValue()
        {
            _nsm.Set(RelationshipMemorySystem.RelationshipKey("ZHANG"), 50f);
            ClearEvents();

            _system.ForceDecayTick();

            Assert.AreEqual(49f, _system.GetRelationshipValue("ZHANG"));
        }

        [Test]
        public void ForceDecayTick_DoesNotGoBelowMin()
        {
            _nsm.Set(RelationshipMemorySystem.RelationshipKey("ZHANG"), 0.5f);

            _system.ForceDecayTick();

            Assert.AreEqual(0f, _system.GetRelationshipValue("ZHANG"));
        }

        [Test]
        public void ForceDecayTick_EmitsRelationshipValueChangedEvent()
        {
            _nsm.Set(RelationshipMemorySystem.RelationshipKey("ZHANG"), 50f);
            ClearEvents();

            _system.ForceDecayTick();

            var valueEvent = _capturedEvents.Find(e => e is RelationshipValueChangedEvent) as RelationshipValueChangedEvent;
            Assert.IsNotNull(valueEvent);
            Assert.AreEqual("ZHANG", valueEvent.CharacterId);
            Assert.AreEqual(50f, valueEvent.OldValue);
            Assert.AreEqual(49f, valueEvent.NewValue);
            Assert.AreEqual(-1f, valueEvent.Delta);
        }

        [Test]
        public void ForceDecayTick_OnlyAffectsExistingRelationships()
        {
            _nsm.Set(RelationshipMemorySystem.RelationshipKey("ZHANG"), 50f);

            // MARCUS has no relationship stored
            _system.ForceDecayTick();

            // ZHANG should be decayed, MARCUS should still return default
            Assert.AreEqual(49f, _system.GetRelationshipValue("ZHANG"));
            Assert.AreEqual(50f, _system.GetRelationshipValue("MARCUS")); // default
        }

        #endregion

        #region DialogueRelationshipShiftEvent Integration Tests

        [Test]
        public void OnDialogueRelationshipShift_AppliesRelationshipShift()
        {
            _nsm.Set(RelationshipMemorySystem.RelationshipKey("ZHANG"), 50f);
            ClearEvents();

            // Simulate DialogueEngine emitting the event
            _nsm.EventBus.Emit(new DialogueRelationshipShiftEvent("ZHANG", 15f, 10f));

            // Should be clamped to 10 (max shift per choice)
            Assert.AreEqual(60f, _system.GetRelationshipValue("ZHANG"));
        }

        [Test]
        public void OnDialogueRelationshipShift_IgnoresTinyDeltas()
        {
            _nsm.Set(RelationshipMemorySystem.RelationshipKey("ZHANG"), 50f);
            ClearEvents();

            // Simulate event with very small delta
            _nsm.EventBus.Emit(new DialogueRelationshipShiftEvent("ZHANG", 0.0001f, 0.0001f));

            // Value should be unchanged (delta too small)
            Assert.AreEqual(50f, _system.GetRelationshipValue("ZHANG"));
        }

        [Test]
        public void OnDialogueRelationshipShift_HandlesPlayerCharacter()
        {
            _nsm.Set(RelationshipMemorySystem.RelationshipKey("PLAYER"), 50f);
            ClearEvents();

            // PLAYER is the player character, should still apply (S2-3 doesn't exclude it)
            _nsm.EventBus.Emit(new DialogueRelationshipShiftEvent("PLAYER", 5f, 5f));

            Assert.AreEqual(55f, _system.GetRelationshipValue("PLAYER"));
        }

        #endregion

        #region Config Tests

        [Test]
        public void Config_ClampShift_RespectsMax()
        {
            Assert.AreEqual(10f, _config.ClampShift(20f));
            Assert.AreEqual(-10f, _config.ClampShift(-20f));
            Assert.AreEqual(5f, _config.ClampShift(5f));
        }

        [Test]
        public void Config_ClampShift_NoLimitWhenZero()
        {
            _config.MaxShiftPerChoice = 0f;
            Assert.AreEqual(20f, _config.ClampShift(20f));
            Assert.AreEqual(-20f, _config.ClampShift(-20f));
        }

        [Test]
        public void Config_ClampValue_ClampsToRange()
        {
            Assert.AreEqual(0f, _config.ClampValue(-5f));
            Assert.AreEqual(100f, _config.ClampValue(105f));
            Assert.AreEqual(50f, _config.ClampValue(50f));
        }

        #endregion

        #region Initialize Tests

        [Test]
        public void Initialize_WithNullNSM_Throws()
        {
            var go = new GameObject();
            var system = go.AddComponent<RelationshipMemorySystem>();

            Assert.Throws<ArgumentNullException>(() => system.Initialize(null));

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void IsDecayActive_FalseBeforeStart()
        {
            Assert.IsFalse(_system.IsDecayActive);
        }

        #endregion
    }
}
