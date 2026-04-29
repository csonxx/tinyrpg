using System;
using System.Collections.Generic;
using System.Linq;
using Core.Narrative;
using NUnit.Framework;

namespace Tests.Unit.NSM
{
    /// <summary>
    /// Unit tests for NarrativeStateMachine.
    /// Covers Mutate, Set, Get, Subscribe, Undo, Serialize, Deserialize, and TrustBoundaryReached.
    /// </summary>
    [TestFixture]
    public class NarrativeStateMachineTests
    {
        private NarrativeStateMachine _nsm;
        private NSMConfig _config;
        private List<NSMEvent> _capturedEvents;
        private List<string> _receivedKeys;

        [SetUp]
        public void SetUp()
        {
            // Create a fresh config with defaults
            _config = ScriptableObject.CreateInstance<NSMConfig>();
            _config.name = "TestNSMConfig";

            // Create fresh NSM instance for each test
            _nsm = new NarrativeStateMachine(_config);
            _capturedEvents = new List<NSMEvent>();
            _receivedKeys = new List<string>();

            // Subscribe to all events for easy capture
            _nsm.Subscribe("*", e =>
            {
                _capturedEvents.Add(e);
                _receivedKeys.Add(e.Key);
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

        private KeyChangedEvent GetKeyChangedEvent(NSMEvent e)
        {
            Assert.IsInstanceOf<KeyChangedEvent>(e);
            return (KeyChangedEvent)e;
        }

        private void ClearEvents()
        {
            _capturedEvents.Clear();
            _receivedKeys.Clear();
        }

        private string[] GetTrustKeys()
        {
            return new[] { "trust.imperial", "trust.underground" };
        }

        #endregion

        #region Mutate Tests

        [Test]
        public void Mutate_IncreasesValueCorrectly()
        {
            // Load schema so strict mode doesn't block
            _nsm.LoadSchema(new[] { "trust.imperial" });

            _nsm.Mutate("trust.imperial", 5f);

            float value = _nsm.Get<float>("trust.imperial");
            Assert.AreEqual(5f, value);
        }

        [Test]
        public void Mutate_DecreasesValueCorrectly()
        {
            _nsm.LoadSchema(new[] { "score" });

            _nsm.Set("score", 50f);
            ClearEvents();

            _nsm.Mutate("score", -10f);

            Assert.AreEqual(40f, _nsm.Get<float>("score"));
        }

        [Test]
        public void Mutate_AccumulatesDeltas()
        {
            _nsm.LoadSchema(new[] { "trust.imperial" });

            _nsm.Mutate("trust.imperial", 5f);
            _nsm.Mutate("trust.imperial", 10f);
            _nsm.Mutate("trust.imperial", -3f);

            Assert.AreEqual(12f, _nsm.Get<float>("trust.imperial"));
        }

        [Test]
        public void Mutate_ClampsTrustToZero()
        {
            _nsm.LoadSchema(GetTrustKeys());

            _nsm.Set("trust.imperial", 5f);
            ClearEvents();

            _nsm.Mutate("trust.imperial", -10f);

            Assert.AreEqual(0f, _nsm.Get<float>("trust.imperial"));
        }

        [Test]
        public void Mutate_ClampsTrustTo100()
        {
            _nsm.LoadSchema(GetTrustKeys());

            _nsm.Set("trust.imperial", 95f);
            ClearEvents();

            _nsm.Mutate("trust.imperial", 10f);

            Assert.AreEqual(100f, _nsm.Get<float>("trust.imperial"));
        }

        [Test]
        public void Mutate_EmitsKeyChangedEvent()
        {
            _nsm.LoadSchema(new[] { "trust.imperial" });

            ClearEvents();
            _nsm.Mutate("trust.imperial", 5f);

            Assert.GreaterOrEqual(_capturedEvents.Count, 1);
            var keyEvent = GetKeyChangedEvent(_capturedEvents[0]);
            Assert.AreEqual("trust.imperial", keyEvent.Key);
            Assert.AreEqual(0f, keyEvent.OldValue); // default
            Assert.AreEqual(5f, keyEvent.NewValue);
            Assert.AreEqual(5f, keyEvent.Delta);
        }

        [Test]
        public void Mutate_ThrowsOnUnknownKeyInStrictMode()
        {
            // Schema is empty, so trust.imperial is unknown
            Assert.Throws<NarrativeStateMachine.NSMSchemaException>(() =>
                _nsm.Mutate("trust.imperial", 5f));
        }

        [Test]
        public void Mutate_AllowsUnknownKeyInNonStrictMode()
        {
            var nonStrictConfig = ScriptableObject.CreateInstance<NSMConfig>();
            nonStrictConfig.name = "NonStrictConfig";
            var nonStrictNsm = new NarrativeStateMachine(nonStrictConfig);

            // Should not throw
            nonStrictNsm.Mutate("arbitrary.key", 99f);
            Assert.AreEqual(99f, nonStrictNsm.Get<float>("arbitrary.key"));

            UnityEngine.Object.DestroyImmediate(nonStrictConfig);
        }

        #endregion

        #region Set Tests

        [Test]
        public void Set_SetsNonNumericValues()
        {
            _nsm.LoadSchema(new[] { "player.name", "player.class" });

            _nsm.Set("player.name", "Lin");
            _nsm.Set("player.class", "Rogue");

            Assert.AreEqual("Lin", _nsm.Get<string>("player.name"));
            Assert.AreEqual("Rogue", _nsm.Get<string>("player.class"));
        }

        [Test]
        public void Set_OverwritesExistingValue()
        {
            _nsm.LoadSchema(new[] { "player.name" });

            _nsm.Set("player.name", "Lin");
            _nsm.Set("player.name", "Mei");

            Assert.AreEqual("Mei", _nsm.Get<string>("player.name"));
        }

        [Test]
        public void Set_EmitsKeyChangedEvent()
        {
            _nsm.LoadSchema(new[] { "player.name" });

            ClearEvents();
            _nsm.Set("player.name", "Lin");

            var keyEvent = GetKeyChangedEvent(_capturedEvents[0]);
            Assert.AreEqual("player.name", keyEvent.Key);
            Assert.IsNull(keyEvent.OldValue);
            Assert.AreEqual("Lin", keyEvent.NewValue);
        }

        #endregion

        #region Get Tests

        [Test]
        public void Get_ReturnsCorrectType_Int()
        {
            _nsm.LoadSchema(new[] { "count" });
            _nsm.Set("count", 42);

            int value = _nsm.Get<int>("count");
            Assert.AreEqual(42, value);
        }

        [Test]
        public void Get_ReturnsCorrectType_Float()
        {
            _nsm.LoadSchema(new[] { "score" });
            _nsm.Set("score", 99.5f);

            float value = _nsm.Get<float>("score");
            Assert.AreEqual(99.5f, value);
        }

        [Test]
        public void Get_ReturnsCorrectType_String()
        {
            _nsm.LoadSchema(new[] { "player.name" });
            _nsm.Set("player.name", "TestPlayer");

            string value = _nsm.Get<string>("player.name");
            Assert.AreEqual("TestPlayer", value);
        }

        [Test]
        public void Get_ReturnsDefaultWhenKeyMissing()
        {
            int intVal = _nsm.Get<int>("nonexistent");
            Assert.AreEqual(0, intVal);

            string strVal = _nsm.Get<string>("nonexistent");
            Assert.IsNull(strVal);

            float floatVal = _nsm.Get<float>("nonexistent");
            Assert.AreEqual(0f, floatVal);
        }

        [Test]
        public void Get_ConvertsIntToFloat()
        {
            _nsm.LoadSchema(new[] { "score" });
            _nsm.Set("score", 10);

            float value = _nsm.Get<float>("score");
            Assert.AreEqual(10f, value);
        }

        #endregion

        #region Subscribe Tests

        [Test]
        public void Subscribe_FiresCallbackOnMatchingKeyChange()
        {
            var callCount = 0;
            _nsm.Subscribe("trust.imperial", e => callCount++);

            _nsm.LoadSchema(GetTrustKeys());
            _nsm.Mutate("trust.imperial", 5f);
            _nsm.Mutate("trust.imperial", 3f);

            Assert.AreEqual(2, callCount);
        }

        [Test]
        public void Subscribe_DoesNotFireOnNonMatchingKey()
        {
            var callCount = 0;
            _nsm.Subscribe("trust.imperial", e => callCount++);

            _nsm.LoadSchema(new[] { "score" });
            _nsm.Mutate("score", 10f);

            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void Subscribe_GlobStarMatchesAllKeys()
        {
            var callCount = 0;
            _nsm.Subscribe("*", e => callCount++);

            _nsm.LoadSchema(new[] { "any.key" });
            _nsm.Mutate("any.key", 1f);
            _nsm.Set("another.key", "value");

            Assert.GreaterOrEqual(callCount, 2);
        }

        [Test]
        public void Subscribe_GlobPatternTrustStar_MatchesTrustImperial()
        {
            var receivedKeys = new List<string>();
            _nsm.Subscribe("trust.*", e => receivedKeys.Add(e.Key));

            _nsm.LoadSchema(GetTrustKeys());
            _nsm.Mutate("trust.imperial", 5f);

            Assert.Contains("trust.imperial", receivedKeys);
        }

        [Test]
        public void Subscribe_GlobPatternTrustStar_MatchesTrustUnderground()
        {
            var receivedKeys = new List<string>();
            _nsm.Subscribe("trust.*", e => receivedKeys.Add(e.Key));

            _nsm.LoadSchema(GetTrustKeys());
            _nsm.Mutate("trust.underground", 10f);

            Assert.Contains("trust.underground", receivedKeys);
        }

        [Test]
        public void Subscribe_GlobPatternTrustStar_MatchesBothTrustKeys()
        {
            var receivedKeys = new List<string>();
            _nsm.Subscribe("trust.*", e => receivedKeys.Add(e.Key));

            _nsm.LoadSchema(GetTrustKeys());
            _nsm.Mutate("trust.imperial", 5f);
            _nsm.Mutate("trust.underground", 10f);

            Assert.AreEqual(2, receivedKeys.Count);
            Assert.Contains("trust.imperial", receivedKeys);
            Assert.Contains("trust.underground", receivedKeys);
        }

        [Test]
        public void Subscribe_Unsubscribe_RemovesCallback()
        {
            var callCount = 0;
            Action<NSMEvent> handler = e => callCount++;
            _nsm.Subscribe("trust.*", handler);

            _nsm.LoadSchema(GetTrustKeys());
            _nsm.Mutate("trust.imperial", 5f);

            _nsm.Unsubscribe("trust.*", handler);

            _nsm.Mutate("trust.imperial", 3f);

            Assert.AreEqual(1, callCount); // Only first mutation counted
        }

        [Test]
        public void Subscribe_UnsubscribeWithNull_RemovesAllListeners()
        {
            _nsm.Subscribe("trust.*", e => { });
            _nsm.Subscribe("trust.*", e => { });

            _nsm.LoadSchema(GetTrustKeys());
            _nsm.Unsubscribe("trust.*");

            ClearEvents();
            _nsm.Mutate("trust.imperial", 5f);

            // No events should be received for trust.* pattern
            Assert.AreEqual(0, _receivedKeys.Count);
        }

        #endregion

        #region Undo Tests

        [Test]
        public void Undo_RestoresPreviousValue()
        {
            _nsm.LoadSchema(new[] { "score" });

            _nsm.Set("score", 100f);
            ClearEvents();
            _nsm.Mutate("score", 50f); // score = 150
            Assert.AreEqual(150f, _nsm.Get<float>("score"));

            _nsm.Undo();

            Assert.AreEqual(100f, _nsm.Get<float>("score"));
        }

        [Test]
        public void Undo_EmitsUndoPerformedEvent()
        {
            _nsm.LoadSchema(new[] { "score" });

            _nsm.Set("score", 100f);
            ClearEvents();
            _nsm.Mutate("score", 50f);

            _nsm.Undo();

            Assert.IsTrue(_capturedEvents.Any(e => e is UndoPerformedEvent));
        }

        [Test]
        public void Undo_SilentFailWhenQueueEmpty()
        {
            _nsm.LoadSchema(new[] { "score" });

            // Undo on empty queue should not throw
            Assert.DoesNotThrow(() => _nsm.Undo());
        }

        [Test]
        public void Undo_Called21Times_DoesNotCrash()
        {
            _nsm.LoadSchema(new[] { "counter" });

            // Set initial value
            _nsm.Set("counter", 0f);

            // Perform 21 mutations (exceeds MAX_UNDO of 20)
            for (int i = 0; i < 21; i++)
            {
                _nsm.Mutate("counter", 1f);
            }

            // Should not crash when undoing beyond cap
            for (int i = 0; i < 21; i++)
            {
                Assert.DoesNotThrow(() => _nsm.Undo());
            }
        }

        [Test]
        public void Undo_QueueCapsAtMaxUndo()
        {
            _nsm.LoadSchema(new[] { "counter" });
            _nsm.Set("counter", 0f);

            // Fill beyond max
            for (int i = 0; i < 25; i++)
            {
                _nsm.Mutate("counter", 1f);
            }

            // Undo queue should be capped at MAX_UNDO (20)
            Assert.LessOrEqual(_nsm.UndoCount, _nsm.MaxUndo);
        }

        [Test]
        public void Undo_BlockedDuringCutscene()
        {
            _nsm.LoadSchema(GetTrustKeys());

            _nsm.SetState(NSMState.CUTSCENE);
            _nsm.Mutate("trust.imperial", 5f);
            Assert.AreEqual(5f, _nsm.Get<float>("trust.imperial"));

            _nsm.Undo();

            // Should not undo because CUTSCENE blocks it
            Assert.AreEqual(5f, _nsm.Get<float>("trust.imperial"));
        }

        [Test]
        public void Undo_BlockedDuringChapterComplete()
        {
            _nsm.LoadSchema(GetTrustKeys());

            _nsm.SetState(NSMState.CHAPTER_COMPLETE);
            _nsm.Mutate("trust.imperial", 5f);
            Assert.AreEqual(5f, _nsm.Get<float>("trust.imperial"));

            _nsm.Undo();

            Assert.AreEqual(5f, _nsm.Get<float>("trust.imperial"));
        }

        [Test]
        public void Undo_AllowedDuringSceneActive()
        {
            _nsm.LoadSchema(GetTrustKeys());

            _nsm.SetState(NSMState.SCENE_ACTIVE);
            _nsm.Set("trust.imperial", 10f);
            ClearEvents();
            _nsm.Mutate("trust.imperial", 5f);
            Assert.AreEqual(15f, _nsm.Get<float>("trust.imperial"));

            _nsm.Undo();

            Assert.AreEqual(10f, _nsm.Get<float>("trust.imperial"));
        }

        #endregion

        #region Trust Boundary Tests

        [Test]
        public void TrustBoundaryReached_FiresWhenTrustCrossesZero()
        {
            TrustBoundaryReachedEvent receivedEvent = null;
            _nsm.Subscribe("trust.boundary", e =>
            {
                if (e is TrustBoundaryReachedEvent te)
                    receivedEvent = te;
            });

            _nsm.LoadSchema(GetTrustKeys());
            _nsm.Set("trust.imperial", 5f);
            ClearEvents();

            _nsm.Mutate("trust.imperial", -10f);

            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual("trust.imperial", receivedEvent.MeterName);
            Assert.AreEqual(0f, receivedEvent.Value);
            Assert.AreEqual(TrustBoundary.CrossedZero, receivedEvent.Boundary);
        }

        [Test]
        public void TrustBoundaryReached_FiresWhenTrustCrosses100()
        {
            TrustBoundaryReachedEvent receivedEvent = null;
            _nsm.Subscribe("trust.boundary", e =>
            {
                if (e is TrustBoundaryReachedEvent te)
                    receivedEvent = te;
            });

            _nsm.LoadSchema(GetTrustKeys());
            _nsm.Set("trust.underground", 95f);
            ClearEvents();

            _nsm.Mutate("trust.underground", 10f);

            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual("trust.underground", receivedEvent.MeterName);
            Assert.AreEqual(100f, receivedEvent.Value);
            Assert.AreEqual(TrustBoundary.CrossedHundred, receivedEvent.Boundary);
        }

        [Test]
        public void TrustBoundaryReached_NotFiredWhenTrustStaysWithinBounds()
        {
            var boundaryEvents = 0;
            _nsm.Subscribe("trust.boundary", e =>
            {
                if (e is TrustBoundaryReachedEvent)
                    boundaryEvents++;
            });

            _nsm.LoadSchema(GetTrustKeys());
            _nsm.Set("trust.imperial", 50f);
            ClearEvents();

            _nsm.Mutate("trust.imperial", 10f);
            _nsm.Mutate("trust.imperial", -5f);

            Assert.AreEqual(0, boundaryEvents);
        }

        [Test]
        public void TrustBoundaryReached_FiredForBothMetersIndependently()
        {
            var events = new List<TrustBoundaryReachedEvent>();
            _nsm.Subscribe("trust.boundary", e =>
            {
                if (e is TrustBoundaryReachedEvent te)
                    events.Add(te);
            });

            _nsm.LoadSchema(GetTrustKeys());

            _nsm.Set("trust.imperial", 5f);
            _nsm.Mutate("trust.imperial", -10f); // imperial crosses 0

            _nsm.Set("trust.underground", 95f);
            _nsm.Mutate("trust.underground", 10f); // underground crosses 100

            Assert.AreEqual(2, events.Count);
        }

        #endregion

        #region Serialization Tests

        [Test]
        public void Serialize_ProducesValidJSON()
        {
            _nsm.LoadSchema(new[] { "player.name", "score", "trust.imperial" });
            _nsm.Set("player.name", "TestPlayer");
            _nsm.Set("score", 123.45f);
            _nsm.Set("trust.imperial", 50f);

            string json = _nsm.Serialize();

            Assert.IsNotNull(json);
            Assert.IsNotEmpty(json);
            Assert.IsTrue(json.Contains("\"player.name\""));
            Assert.IsTrue(json.Contains("\"score\""));
            Assert.IsTrue(json.Contains("TestPlayer"));
        }

        [Test]
        public void Deserialize_RestoresExactState()
        {
            _nsm.LoadSchema(new[] { "player.name", "score", "trust.imperial", "trust.underground" });
            _nsm.Set("player.name", "Mei");
            _nsm.Set("score", 999.5f);
            _nsm.Set("trust.imperial", 75f);
            _nsm.Set("trust.underground", 25f);
            _nsm.SetState(NSMState.SCENE_ACTIVE);

            string json = _nsm.Serialize();

            // Create a fresh NSM and deserialize
            var freshNsm = new NarrativeStateMachine(_config);
            bool success = freshNsm.Deserialize(json);

            Assert.IsTrue(success);
            Assert.AreEqual("Mei", freshNsm.Get<string>("player.name"));
            Assert.AreEqual(999.5f, freshNsm.Get<float>("score"));
            Assert.AreEqual(75f, freshNsm.Get<float>("trust.imperial"));
            Assert.AreEqual(25f, freshNsm.Get<float>("trust.underground"));
            Assert.AreEqual(NSMState.SCENE_ACTIVE, freshNsm.CurrentState);
        }

        [Test]
        public void Deserialize_RestoresTrustValuesAfterModification()
        {
            _nsm.LoadSchema(GetTrustKeys());
            _nsm.Set("trust.imperial", 80f);
            string json = _nsm.Serialize();

            _nsm.Mutate("trust.imperial", -30f);
            Assert.AreEqual(50f, _nsm.Get<float>("trust.imperial"));

            var freshNsm = new NarrativeStateMachine(_config);
            freshNsm.Deserialize(json);

            Assert.AreEqual(80f, freshNsm.Get<float>("trust.imperial"));
        }

        [Test]
        public void Serialize_IncludesHash()
        {
            _nsm.LoadSchema(new[] { "key" });
            _nsm.Set("key", "value");

            string json = _nsm.Serialize();

            // Hash is included in the serialized data
            Assert.IsTrue(json.Contains("__nsm_hash__"));
        }

        [Test]
        public void HashMismatch_LogsErrorAndEmitsSchemaValidationFailed()
        {
            _nsm.LoadSchema(new[] { "key" });
            _nsm.Set("key", "value");
            string json = _nsm.Serialize();

            // Tamper with the JSON
            json = json.Replace("value", "tampered");

            SchemaValidationFailedEvent receivedEvent = null;
            _nsm.Subscribe("nsm.schema_validation_failed", e =>
            {
                if (e is SchemaValidationFailedEvent se)
                    receivedEvent = se;
            });

            bool success = _nsm.Deserialize(json);

            Assert.IsFalse(success);
            Assert.IsNotNull(receivedEvent);
            Assert.IsTrue(receivedEvent.Errors.Count > 0);
        }

        [Test]
        public void Deserialize_InvalidJSON_EmitsSchemaValidationFailed()
        {
            SchemaValidationFailedEvent receivedEvent = null;
            _nsm.Subscribe("nsm.schema_validation_failed", e =>
            {
                if (e is SchemaValidationFailedEvent se)
                    receivedEvent = se;
            });

            bool success = _nsm.Deserialize("not valid json at all");

            Assert.IsFalse(success);
            Assert.IsNotNull(receivedEvent);
        }

        #endregion

        #region Load / Clear Tests

        [Test]
        public void Load_ClearsUndoQueue()
        {
            _nsm.LoadSchema(new[] { "score" });
            _nsm.Set("score", 0f);
            _nsm.Mutate("score", 10f);
            Assert.AreEqual(1, _nsm.UndoCount);

            _nsm.LoadSchema(new[] { "score", "other" });

            Assert.AreEqual(0, _nsm.UndoCount);
        }

        [Test]
        public void Load_LoadsSchemaKeys()
        {
            _nsm.LoadSchema(new[] { "a", "b", "c" });

            Assert.IsTrue(_nsm.IsSchemaKey("a"));
            Assert.IsTrue(_nsm.IsSchemaKey("b"));
            Assert.IsTrue(_nsm.IsSchemaKey("c"));
            Assert.IsFalse(_nsm.IsSchemaKey("d"));
        }

        #endregion

        #region State Transition Tests

        [Test]
        public void SetState_EmitsStateChangedEvent()
        {
            StateChangedEvent receivedEvent = null;
            _nsm.Subscribe("nsm.state", e =>
            {
                if (e is StateChangedEvent se)
                    receivedEvent = se;
            });

            _nsm.SetState(NSMState.CHAPTER_LOADING);

            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual(NSMState.TITLE, receivedEvent.OldState);
            Assert.AreEqual(NSMState.CHAPTER_LOADING, receivedEvent.NewState);
        }

        [Test]
        public void SetState_DoesNotEmitWhenSameState()
        {
            int eventCount = 0;
            _nsm.Subscribe("nsm.state", e => eventCount++);

            _nsm.SetState(NSMState.SCENE_ACTIVE);
            _nsm.SetState(NSMState.SCENE_ACTIVE);

            Assert.AreEqual(1, eventCount);
        }

        #endregion
    }
}
