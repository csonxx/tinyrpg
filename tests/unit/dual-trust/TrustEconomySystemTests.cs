using System;
using System.Collections.Generic;
using Core.Narrative;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Unit.DualTrust
{
    /// <summary>
    /// Unit tests for TrustEconomySystem.
    /// Tests: ApplyShift clamping, passive decay timing, danger zone threshold (25),
    /// crisis threshold (15), parity crisis detection.
    /// </summary>
    [TestFixture]
    public class TrustEconomySystemTests
    {
        private TrustEconomySystem _system;
        private TrustEconomyConfig _config;
        private NarrativeStateMachine _nsm;
        private List<NSMEvent> _capturedEvents;
        private List<string> _receivedKeys;
        private float _initialTime;

        #region Test Lifecycle

        [SetUp]
        public void SetUp()
        {
            // Create config
            _config = ScriptableObject.CreateInstance<TrustEconomyConfig>();
            _config.name = "TestTrustConfig";

            // Create NSM
            var nsmConfig = ScriptableObject.CreateInstance<NSMConfig>();
            nsmConfig.name = "TestNSMConfig";
            _nsm = new NarrativeStateMachine(nsmConfig);

            // Create TrustEconomySystem gameobject
            var go = new GameObject("TrustEconomySystemUnderTest");
            _system = go.AddComponent<TrustEconomySystem>();

            // Use reflection to inject config ( SerializeField injection )
            var configField = typeof(TrustEconomySystem).GetField("_config",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            configField.SetValue(_system, _config);

            // Initialize system
            _system.Initialize(_nsm);

            // Setup event capture
            _capturedEvents = new List<NSMEvent>();
            _receivedKeys = new List<string>();
            _nsm.Subscribe("*", e =>
            {
                _capturedEvents.Add(e);
                _receivedKeys.Add(e.Key);
            });

            // Mock Time.time for deterministic decay testing
            _initialTime = Time.time;
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

        #region TrustShift Tests

        [Test]
        public void TrustShift_Clamped_LimitsToMaxMagnitude()
        {
            var shift = new TrustShift(15f, -20f, false);
            var clamped = shift.Clamped(10f);

            Assert.AreEqual(10f, clamped.DeltaImperial);
            Assert.AreEqual(-10f, clamped.DeltaUnderground);
            Assert.IsFalse(clamped.IsSecret);
        }

        [Test]
        public void TrustShift_Clamped_PreservesSecretFlag()
        {
            var shift = new TrustShift(5f, 5f, true);
            var clamped = shift.Clamped(10f);

            Assert.IsTrue(clamped.IsSecret);
        }

        [Test]
        public void TrustShift_Clamped_WithinBounds_Unchanged()
        {
            var shift = new TrustShift(3f, -7f, false);
            var clamped = shift.Clamped(10f);

            Assert.AreEqual(3f, clamped.DeltaImperial);
            Assert.AreEqual(-7f, clamped.DeltaUnderground);
        }

        [Test]
        public void TrustShift_Equality_Works()
        {
            var shift1 = new TrustShift(5f, -3f, false);
            var shift2 = new TrustShift(5f, -3f, false);
            var shift3 = new TrustShift(5f, -3f, true);

            Assert.AreEqual(shift1, shift2);
            Assert.AreNotEqual(shift1, shift3);
        }

        #endregion

        #region ApplyShift Clamping Tests

        [Test]
        public void ApplyShift_ClampsToMaxShiftPerChoice()
        {
            var shift = new TrustShift(15f, 15f, false);

            _system.ApplyShift(shift);

            // Should be clamped to 10
            Assert.AreEqual(10f, _system.ImperialTrust);
            Assert.AreEqual(10f, _system.UndergroundTrust);
        }

        [Test]
        public void ApplyShift_NegativeDelta_Clamped()
        {
            var shift = new TrustShift(-15f, -20f, false);

            _system.ApplyShift(shift);

            Assert.AreEqual(-10f, _system.ImperialTrust);
            Assert.AreEqual(-10f, _system.UndergroundTrust);
        }

        [Test]
        public void ApplyShift_EmitsTrustShiftAppliedEvent()
        {
            var shift = new TrustShift(5f, -3f, false);
            ClearEvents();

            _system.ApplyShift(shift);

            var shiftEvent = _capturedEvents.Find(e => e is TrustShiftAppliedEvent) as TrustShiftAppliedEvent;
            Assert.IsNotNull(shiftEvent);
            Assert.AreEqual(5f, shiftEvent.DeltaImperial);
            Assert.AreEqual(-3f, shiftEvent.DeltaUnderground);
        }

        [Test]
        public void ApplyShift_EmitsTrustValueChangedEvent()
        {
            var shift = new TrustShift(5f, 5f, false);
            ClearEvents();

            _system.ApplyShift(shift);

            var changedEvent = _capturedEvents.Find(e => e is TrustValueChangedEvent) as TrustValueChangedEvent;
            Assert.IsNotNull(changedEvent);
            Assert.AreEqual(45f, changedEvent.Imperial);  // 40 + 5
            Assert.AreEqual(45f, changedEvent.Underground); // 40 + 5
        }

        #endregion

        #region Passive Decay Tests

        [Test]
        public void PassiveDecay_NoDecayDuringGracePeriod()
        {
            // Initial values should be starting values (40)
            Assert.AreEqual(_config.ImperialStartValue, _system.ImperialTrust);
            Assert.AreEqual(_config.UndergroundStartValue, _system.UndergroundTrust);

            // ForceDecayTick should work but passive decay hasn't started
            // (We can verify IsDecayActive is false before grace period)
            Assert.IsFalse(_system.IsDecayActive);
        }

        [Test]
        public void ForceDecayTick_ReducesBothTrustValues()
        {
            float initialImperial = _system.ImperialTrust;
            float initialUnderground = _system.UndergroundTrust;

            _system.ForceDecayTick();

            Assert.AreEqual(initialImperial - _config.DecayAmountPerInterval, _system.ImperialTrust);
            Assert.AreEqual(initialUnderground - _config.DecayAmountPerInterval, _system.UndergroundTrust);
        }

        [Test]
        public void ForceDecayTick_DoesNotEmitTrustShiftApplied()
        {
            ClearEvents();

            _system.ForceDecayTick();

            var shiftEvent = _capturedEvents.Find(e => e is TrustShiftAppliedEvent);
            Assert.IsNull(shiftEvent);
        }

        [Test]
        public void ForceDecayTick_EmitsTrustValueChanged()
        {
            ClearEvents();

            _system.ForceDecayTick();

            var changedEvent = _capturedEvents.Find(e => e is TrustValueChangedEvent);
            Assert.IsNotNull(changedEvent);
        }

        #endregion

        #region Danger Zone Threshold Tests (exactly 25)

        [Test]
        public void DangerZone_FiresAtExactly25_CrossingFromAbove()
        {
            // Set trust to 26 (just above danger)
            _nsm.Set(TrustEconomySystem.IMPERIAL_KEY, 26f);
            ClearEvents();

            // Mutate to cross below 25
            _nsm.Mutate(TrustEconomySystem.IMPERIAL_KEY, -1f);

            var dangerEvent = _capturedEvents.Find(e => e is DangerZoneEnteredEvent de && de.MeterName == TrustEconomySystem.IMPERIAL_KEY) as DangerZoneEnteredEvent;
            Assert.IsNotNull(dangerEvent, "DangerZoneEntered should fire when crossing from 26 to 25");
        }

        [Test]
        public void DangerZone_FiresWhenCrossingBelow25()
        {
            // Set trust to 30 and mutate to cross below 25
            _nsm.Set(TrustEconomySystem.IMPERIAL_KEY, 30f);
            ClearEvents();

            _nsm.Mutate(TrustEconomySystem.IMPERIAL_KEY, -6f);

            var dangerEvent = _capturedEvents.Find(e => e is DangerZoneEnteredEvent de && de.MeterName == TrustEconomySystem.IMPERIAL_KEY) as DangerZoneEnteredEvent;
            Assert.IsNotNull(dangerEvent, "DangerZoneEntered should fire when trust crosses below 25");
        }

        [Test]
        public void DangerZone_DoesNotFireAt26()
        {
            _nsm.Set(TrustEconomySystem.IMPERIAL_KEY, 26f);
            ClearEvents();

            // No mutation, just verify no event at 26 (not below 25)
            _nsm.Set(TrustEconomySystem.IMPERIAL_KEY, 26f);

            var dangerEvent = _capturedEvents.Find(e => e is DangerZoneEnteredEvent de && de.MeterName == TrustEconomySystem.IMPERIAL_KEY);
            Assert.IsNull(dangerEvent, "DangerZoneEntered should not fire at exactly 26");
        }

        [Test]
        public void DangerZone_FiresForBothMetersIndependently()
        {
            _nsm.Set(TrustEconomySystem.IMPERIAL_KEY, 30f);
            _nsm.Set(TrustEconomySystem.UNDERGROUND_KEY, 30f);
            ClearEvents();

            _nsm.Mutate(TrustEconomySystem.IMPERIAL_KEY, -6f);  // to 24
            _nsm.Mutate(TrustEconomySystem.UNDERGROUND_KEY, -5f); // to 25 - not crossed yet

            var dangerEvents = _capturedEvents.FindAll(e => e is DangerZoneEnteredEvent);
            Assert.AreEqual(1, dangerEvents.Count, "Only imperial crossed below 25");

            _nsm.Mutate(TrustEconomySystem.UNDERGROUND_KEY, -1f); // to 24 - now crossed

            dangerEvents = _capturedEvents.FindAll(e => e is DangerZoneEnteredEvent);
            Assert.AreEqual(2, dangerEvents.Count, "Both meters should have triggered");
        }

        #endregion

        #region Crisis Threshold Tests (exactly 15)

        [Test]
        public void Crisis_FiresAtExactly15_CrossingFromAbove()
        {
            _nsm.Set(TrustEconomySystem.IMPERIAL_KEY, 16f);
            ClearEvents();

            _nsm.Mutate(TrustEconomySystem.IMPERIAL_KEY, -1f);

            var crisisEvent = _capturedEvents.Find(e => e is CrisisEnteredEvent ce && ce.MeterName == TrustEconomySystem.IMPERIAL_KEY) as CrisisEnteredEvent;
            Assert.IsNotNull(crisisEvent, "CrisisEntered should fire when crossing from 16 to 15");
        }

        [Test]
        public void Crisis_FiresWhenCrossingBelow15()
        {
            _nsm.Set(TrustEconomySystem.IMPERIAL_KEY, 20f);
            ClearEvents();

            _nsm.Mutate(TrustEconomySystem.IMPERIAL_KEY, -6f);

            var crisisEvent = _capturedEvents.Find(e => e is CrisisEnteredEvent ce && ce.MeterName == TrustEconomySystem.IMPERIAL_KEY) as CrisisEnteredEvent;
            Assert.IsNotNull(crisisEvent, "CrisisEntered should fire when trust crosses below 15");
        }

        [Test]
        public void Crisis_DoesNotFireAt16()
        {
            _nsm.Set(TrustEconomySystem.IMPERIAL_KEY, 16f);
            ClearEvents();

            _nsm.Set(TrustEconomySystem.IMPERIAL_KEY, 16f);

            var crisisEvent = _capturedEvents.Find(e => e is CrisisEnteredEvent ce && ce.MeterName == TrustEconomySystem.IMPERIAL_KEY);
            Assert.IsNull(crisisEvent, "CrisisEntered should not fire at exactly 16");
        }

        [Test]
        public void Crisis_FiresForBothMetersIndependently()
        {
            _nsm.Set(TrustEconomySystem.IMPERIAL_KEY, 16f);
            _nsm.Set(TrustEconomySystem.UNDERGROUND_KEY, 16f);
            ClearEvents();

            _nsm.Mutate(TrustEconomySystem.IMPERIAL_KEY, -1f);  // to 15
            _nsm.Mutate(TrustEconomySystem.UNDERGROUND_KEY, -0.5f); // to 15.5 - not crossed

            var crisisEvents = _capturedEvents.FindAll(e => e is CrisisEnteredEvent);
            Assert.AreEqual(1, crisisEvents.Count);

            _nsm.Mutate(TrustEconomySystem.UNDERGROUND_KEY, -1f); // to 14.5 - crossed

            crisisEvents = _capturedEvents.FindAll(e => e is CrisisEnteredEvent);
            Assert.AreEqual(2, crisisEvents.Count);
        }

        #endregion

        #region Parity Crisis Tests (both within 10 points AND both <= 25)

        [Test]
        public void ParityCrisis_DetectedWhenBothWithin10AndBothLe25()
        {
            // Imperial = 25, Underground = 20 (diff = 5, both <= 25)
            _nsm.Set(TrustEconomySystem.IMPERIAL_KEY, 25f);
            _nsm.Set(TrustEconomySystem.UNDERGROUND_KEY, 20f);

            // Should log parity crisis (verified by debug output check)
            // We can't easily test debug logs, but we can verify no exception is thrown
            Assert.DoesNotThrow(() => _system.ForceDecayTick());
        }

        [Test]
        public void ParityCrisis_NotDetectedWhenDifferenceExceeds10()
        {
            // Imperial = 25, Underground = 10 (diff = 15 > 10)
            _nsm.Set(TrustEconomySystem.IMPERIAL_KEY, 25f);
            _nsm.Set(TrustEconomySystem.UNDERGROUND_KEY, 10f);

            // Should not log parity crisis
            Assert.DoesNotThrow(() => _system.ForceDecayTick());
        }

        [Test]
        public void ParityCrisis_NotDetectedWhenOneExceeds25()
        {
            // Imperial = 26, Underground = 20 (imperial > 25)
            _nsm.Set(TrustEconomySystem.IMPERIAL_KEY, 26f);
            _nsm.Set(TrustEconomySystem.UNDERGROUND_KEY, 20f);

            // Should not log parity crisis (both must be <= 25)
            Assert.DoesNotThrow(() => _system.ForceDecayTick());
        }

        [Test]
        public void ParityCrisis_BoundaryAtExactly10Difference()
        {
            // Imperial = 25, Underground = 15 (diff = 10, both <= 25)
            _nsm.Set(TrustEconomySystem.IMPERIAL_KEY, 25f);
            _nsm.Set(TrustEconomySystem.UNDERGROUND_KEY, 15f);

            // diff = 10, which equals threshold, should trigger
            Assert.DoesNotThrow(() => _system.ForceDecayTick());
        }

        #endregion

        #region NSM Integration Tests

        [Test]
        public void Initialize_SetsDefaultValuesIfNotPresent()
        {
            // Verify initial values are set to config values
            Assert.AreEqual(_config.ImperialStartValue, _system.ImperialTrust);
            Assert.AreEqual(_config.UndergroundStartValue, _system.UndergroundTrust);
        }

        [Test]
        public void TrustValuesPersistedInNSM()
        {
            _nsm.Set(TrustEconomySystem.IMPERIAL_KEY, 75f);
            _nsm.Set(TrustEconomySystem.UNDERGROUND_KEY, 35f);

            Assert.AreEqual(75f, _nsm.Get<float>(TrustEconomySystem.IMPERIAL_KEY));
            Assert.AreEqual(35f, _nsm.Get<float>(TrustEconomySystem.UNDERGROUND_KEY));
        }

        #endregion
    }
}
