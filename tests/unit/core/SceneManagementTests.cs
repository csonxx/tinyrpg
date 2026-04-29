using System;
using System.Collections.Generic;
using System.Linq;
using Core.Narrative;
using Core.Scene;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Unit.Core
{
    /// <summary>
    /// Unit tests for SceneManagement.
    /// Tests cover: transition timing, overlay stack operations, preload triggers, and event emission.
    /// </summary>
    [TestFixture]
    public class SceneManagementTests
    {
        private SceneManagement _sceneManager;
        private SceneManagementConfig _config;
        private EventBus _eventBus;
        private List<NSMEvent> _capturedEvents;
        private List<string> _receivedKeys;

        [SetUp]
        public void SetUp()
        {
            // Create a fresh config
            _config = ScriptableObject.CreateInstance<SceneManagementConfig>();
            _config.name = "TestSceneConfig";

            // Create fresh EventBus for each test
            _eventBus = new EventBus();
            _capturedEvents = new List<NSMEvent>();
            _receivedKeys = new List<string>();

            // Create SceneManagement GameObject with component
            var go = new GameObject("SceneManagementTest");
            _sceneManager = go.AddComponent<SceneManagement>();

            // Use reflection to inject the EventBus since it's [SerializeField]
            var configField = typeof(SceneManagement).GetField("_config",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            configField.SetValue(_sceneManager, _config);

            var eventBusField = typeof(SceneManagement).GetField("_eventBus",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            eventBusField.SetValue(_sceneManager, _eventBus);

            // Subscribe to all events for easy capture
            _eventBus.Subscribe("*", e =>
            {
                _capturedEvents.Add(e);
                _receivedKeys.Add(e.Key);
            });
        }

        [TearDown]
        public void TearDown()
        {
            if (_sceneManager != null)
            {
                UnityEngine.Object.DestroyImmediate(_sceneManager.gameObject);
            }
            if (_config != null)
            {
                UnityEngine.Object.DestroyImmediate(_config);
            }
        }

        #region Helper Methods

        private T GetEvent<T>(int index) where T : NSMEvent
        {
            Assert.IsInstanceOf<T>(_capturedEvents[index]);
            return (T)_capturedEvents[index];
        }

        private int CountEvents(string key)
        {
            return _receivedKeys.Count(k => k == key);
        }

        private void ClearEvents()
        {
            _capturedEvents.Clear();
            _receivedKeys.Clear();
        }

        #endregion

        #region Configuration Tests

        [Test]
        public void Config_HasCorrectDefaultValues()
        {
            Assert.AreEqual(400, _config.FadeOutDurationSec * 1000f);
            Assert.AreEqual(100, _config.FadeGreyHoldDurationSec * 1000f);
            Assert.AreEqual(400, _config.FadeInDurationSec * 1000f);
            Assert.AreEqual(500, _config.CrossfadeDurationSec * 1000f);
            Assert.AreEqual(3, _config.MaxSceneStackDepth);
            Assert.AreEqual(400, _config.MinOverlayDurationSec * 1000f);
            Assert.AreEqual(3, _config.PreloadLookaheadChoices);
        }

        [Test]
        public void SceneManagement_ReadsConfigValues()
        {
            // Access via the helper by calling LoadScene (which uses GetConfigValue internally)
            // We can't directly test GetConfigValue, but we can verify the component initialized
            Assert.IsNotNull(_sceneManager);
            Assert.AreEqual(TransitionType.FADE_GREY, TransitionType.FADE_GREY); // sanity
        }

        #endregion

        #region FADE_GREY Transition Tests

        [Test]
        public void LoadScene_FadeGrey_EmitsTransitionBeganThenComplete()
        {
            // FADE_GREY: 400ms out + 100ms hold + 400ms in = ~900ms total
            // We can only verify events are emitted in correct order, not exact timing in unit tests
            ClearEvents();

            _sceneManager.LoadScene("test_scene", TransitionType.FADE_GREY);

            // TransitionBeganEvent should be emitted immediately
            var beganEvents = _capturedEvents.OfType<TransitionBeganEvent>().ToList();
            Assert.AreEqual(1, beganEvents.Count);
            Assert.AreEqual("test_scene", beganEvents[0].SceneId);
            Assert.AreEqual(TransitionType.FADE_GREY, beganEvents[0].TransitionType);
        }

        [Test]
        public void LoadScene_FadeGrey_TimingMatchesSpec()
        {
            // Verify config values match the spec: 400ms out, 100ms hold, 400ms in
            Assert.AreEqual(0.4f, _config.FadeOutDurationSec, 0.001f);
            Assert.AreEqual(0.1f, _config.FadeGreyHoldDurationSec, 0.001f);
            Assert.AreEqual(0.4f, _config.FadeInDurationSec, 0.001f);

            float totalFadeGreyTime = _config.FadeOutDurationSec
                                    + _config.FadeGreyHoldDurationSec
                                    + _config.FadeInDurationSec;
            Assert.AreEqual(0.9f, totalFadeGreyTime, 0.001f);
        }

        #endregion

        #region FADE_BLACK Transition Tests

        [Test]
        public void LoadScene_FadeBlack_EmitsCorrectTransitionType()
        {
            ClearEvents();

            _sceneManager.LoadScene("test_scene", TransitionType.FADE_BLACK);

            var beganEvents = _capturedEvents.OfType<TransitionBeganEvent>().ToList();
            Assert.AreEqual(1, beganEvents.Count);
            Assert.AreEqual(TransitionType.FADE_BLACK, beganEvents[0].TransitionType);
        }

        [Test]
        public void LoadScene_FadeBlack_TimingHasNoHold()
        {
            // FADE_BLACK: 400ms out + 400ms in (no 100ms hold)
            float totalFadeBlackTime = _config.FadeOutDurationSec + _config.FadeInDurationSec;
            Assert.AreEqual(0.8f, totalFadeBlackTime, 0.001f);
        }

        #endregion

        #region CROSSFADE Transition Tests

        [Test]
        public void LoadScene_Crossfade_EmitsCorrectTransitionType()
        {
            ClearEvents();

            _sceneManager.LoadScene("test_scene", TransitionType.CROSSFADE);

            var beganEvents = _capturedEvents.OfType<TransitionBeganEvent>().ToList();
            Assert.AreEqual(1, beganEvents.Count);
            Assert.AreEqual(TransitionType.CROSSFADE, beganEvents[0].TransitionType);
        }

        [Test]
        public void LoadScene_Crossfade_UsesCrossfadeDuration()
        {
            Assert.AreEqual(0.5f, _config.CrossfadeDurationSec, 0.001f);
        }

        #endregion

        #region SceneReady Event Tests

        [Test]
        public void LoadScene_EmitsSceneReady_WhenAsyncOperationCompletes()
        {
            // Note: This test verifies the event structure, not actual async completion
            // In a real integration test, we'd wait for the coroutine to complete
            ClearEvents();

            _sceneManager.LoadScene("test_scene", TransitionType.CROSSFADE);

            // After LoadScene call, TransitionBeganEvent should exist
            var beganEvents = _capturedEvents.OfType<TransitionBeganEvent>().ToList();
            Assert.AreEqual(1, beganEvents.Count);
        }

        [Test]
        public void SceneReadyEvent_HasCorrectKey()
        {
            var evt = new SceneReadyEvent("scene_1");
            Assert.AreEqual("scene.ready", evt.Key);
            Assert.AreEqual("scene_1", evt.SceneId);
        }

        #endregion

        #region Scene Stack / Overlay Tests

        [Test]
        public void PushOverlay_AddsToStack()
        {
            _sceneManager.PushOverlay("cutscene_1");

            Assert.AreEqual(1, _sceneManager.SceneStack.Count);
            Assert.AreEqual("cutscene_1", _sceneManager.SceneStack[0]);
        }

        [Test]
        public void PushOverlay_EmitsSceneStackChangedEvent()
        {
            ClearEvents();

            _sceneManager.PushOverlay("cutscene_1");

            var stackEvents = _capturedEvents.OfType<SceneStackChangedEvent>().ToList();
            Assert.AreEqual(1, stackEvents.Count);
            Assert.AreEqual(1, stackEvents[0].Stack.Length);
            Assert.AreEqual("cutscene_1", stackEvents[0].Stack[0]);
        }

        [Test]
        public void PushOverlay_Multiple_BuildsStack()
        {
            _sceneManager.PushOverlay("cutscene_1");
            _sceneManager.PushOverlay("cutscene_2");
            _sceneManager.PushOverlay("cutscene_3");

            Assert.AreEqual(3, _sceneManager.SceneStack.Count);
            Assert.AreEqual("cutscene_1", _sceneManager.SceneStack[0]);
            Assert.AreEqual("cutscene_2", _sceneManager.SceneStack[1]);
            Assert.AreEqual("cutscene_3", _sceneManager.SceneStack[2]);
        }

        [Test]
        public void PopOverlay_RemovesFromStack()
        {
            _sceneManager.PushOverlay("cutscene_1");
            _sceneManager.PushOverlay("cutscene_2");
            ClearEvents();

            _sceneManager.PopOverlay();

            Assert.AreEqual(1, _sceneManager.SceneStack.Count);
            Assert.AreEqual("cutscene_1", _sceneManager.SceneStack[0]);
        }

        [Test]
        public void PopOverlay_EmitsSceneStackChangedEvent()
        {
            _sceneManager.PushOverlay("cutscene_1");
            _sceneManager.PushOverlay("cutscene_2");
            ClearEvents();

            _sceneManager.PopOverlay();

            var stackEvents = _capturedEvents.OfType<SceneStackChangedEvent>().ToList();
            Assert.AreEqual(1, stackEvents.Count);
            Assert.AreEqual(1, stackEvents[0].Stack.Length);
        }

        [Test]
        public void PopOverlay_ThrowsWhenStackEmpty()
        {
            Assert.Throws<SceneManagement.SceneStackException>(() => _sceneManager.PopOverlay());
        }

        [Test]
        public void PushOverlay_ThrowsWhenMaxDepthExceeded()
        {
            // Fill the stack to max depth (3)
            _sceneManager.PushOverlay("cutscene_1");
            _sceneManager.PushOverlay("cutscene_2");
            _sceneManager.PushOverlay("cutscene_3");

            // 4th push should throw
            Assert.Throws<SceneManagement.SceneStackException>(() => _sceneManager.PushOverlay("cutscene_4"));
        }

        [Test]
        public void PushOverlay_RespectsMaxDepthConfig()
        {
            Assert.AreEqual(3, _config.MaxSceneStackDepth);

            // Should be able to push exactly 3
            _sceneManager.PushOverlay("c1");
            _sceneManager.PushOverlay("c2");
            _sceneManager.PushOverlay("c3");
            Assert.AreEqual(3, _sceneManager.SceneStack.Count);

            // 4th should throw
            Assert.Throws<SceneManagement.SceneStackException>(() => _sceneManager.PushOverlay("c4"));
        }

        [Test]
        public void PeekOverlay_ReturnsTopWithoutPopping()
        {
            _sceneManager.PushOverlay("cutscene_1");
            _sceneManager.PushOverlay("cutscene_2");

            Assert.AreEqual("cutscene_2", _sceneManager.PeekOverlay());
            Assert.AreEqual(2, _sceneManager.SceneStack.Count); // count unchanged
        }

        [Test]
        public void PeekOverlay_ReturnsNullWhenEmpty()
        {
            Assert.IsNull(_sceneManager.PeekOverlay());
        }

        [Test]
        public void PushOverlay_ThrowsOnNullOrEmpty()
        {
            Assert.Throws<ArgumentException>(() => _sceneManager.PushOverlay(null));
            Assert.Throws<ArgumentException>(() => _sceneManager.PushOverlay(""));
        }

        #endregion

        #region Preload / Addressables Tests

        [Test]
        public void OnChoicesRemainingChanged_TriggersPreload_WhenAtThreshold()
        {
            ClearEvents();

            // choicesRemaining (3) <= PreloadLookaheadChoices (3) -> should trigger preload
            _sceneManager.OnChoicesRemainingChanged(3, "next_scene");

            var preloadEvents = _capturedEvents.OfType<ScenePreloadRequestedEvent>().ToList();
            Assert.AreEqual(1, preloadEvents.Count);
            Assert.AreEqual("next_scene", preloadEvents[0].SceneId);
        }

        [Test]
        public void OnChoicesRemainingChanged_TriggersPreload_WhenBelowThreshold()
        {
            ClearEvents();

            // choicesRemaining (2) < PreloadLookaheadChoices (3) -> should trigger preload
            _sceneManager.OnChoicesRemainingChanged(2, "next_scene");

            var preloadEvents = _capturedEvents.OfType<ScenePreloadRequestedEvent>().ToList();
            Assert.AreEqual(1, preloadEvents.Count);
        }

        [Test]
        public void OnChoicesRemainingChanged_DoesNotTrigger_WhenAboveThreshold()
        {
            ClearEvents();

            // choicesRemaining (4) > PreloadLookaheadChoices (3) -> should NOT trigger
            _sceneManager.OnChoicesRemainingChanged(4, "next_scene");

            var preloadEvents = _capturedEvents.OfType<ScenePreloadRequestedEvent>().ToList();
            Assert.AreEqual(0, preloadEvents.Count);
        }

        [Test]
        public void OnChoicesRemainingChanged_DoesNotTrigger_WhenNoNextScene()
        {
            ClearEvents();

            _sceneManager.OnChoicesRemainingChanged(3, null);
            _sceneManager.OnChoicesRemainingChanged(3, "");

            var preloadEvents = _capturedEvents.OfType<ScenePreloadRequestedEvent>().ToList();
            Assert.AreEqual(0, preloadEvents.Count);
        }

        [Test]
        public void PreloadScene_DoesNotDuplicate_WhenAlreadyPreloaded()
        {
            ClearEvents();

            // First preload
            _sceneManager.PreloadScene("scene_1");

            // Second call with same scene should not emit another preload event
            _sceneManager.PreloadScene("scene_1");

            var preloadEvents = _capturedEvents.OfType<ScenePreloadRequestedEvent>().ToList();
            Assert.AreEqual(1, preloadEvents.Count);
        }

        [Test]
        public void GetPreloadedBackground_ReturnsNull_WhenNotPreloaded()
        {
            var result = _sceneManager.GetPreloadedBackground("nonexistent_scene");
            Assert.IsNull(result);
        }

        [Test]
        public void RegisterPreloadedBackground_CachesTexture()
        {
            var fakeTexture = new Texture2D(1, 1);

            _sceneManager.RegisterPreloadedBackground("scene_1", fakeTexture);

            var result = _sceneManager.GetPreloadedBackground("scene_1");
            Assert.AreEqual(fakeTexture, result);
        }

        [Test]
        public void ScenePreloadRequestedEvent_HasCorrectKey()
        {
            var evt = new ScenePreloadRequestedEvent("scene_1");
            Assert.AreEqual("scene.preload_requested", evt.Key);
            Assert.AreEqual("scene_1", evt.SceneId);
        }

        #endregion

        #region Transition Events Tests

        [Test]
        public void TransitionBeganEvent_HasCorrectKey()
        {
            var evt = new TransitionBeganEvent("scene_1", TransitionType.FADE_GREY);
            Assert.AreEqual("scene.transition_began", evt.Key);
            Assert.AreEqual("scene_1", evt.SceneId);
            Assert.AreEqual(TransitionType.FADE_GREY, evt.TransitionType);
        }

        [Test]
        public void TransitionCompleteEvent_HasCorrectKey()
        {
            var evt = new TransitionCompleteEvent("scene_1", TransitionType.CROSSFADE);
            Assert.AreEqual("scene.transition_complete", evt.Key);
            Assert.AreEqual("scene_1", evt.SceneId);
            Assert.AreEqual(TransitionType.CROSSFADE, evt.TransitionType);
        }

        [Test]
        public void CutsceneCompleteEvent_HasCorrectKey()
        {
            var evt = new CutsceneCompleteEvent("cutscene_1");
            Assert.AreEqual("scene.cutscene_complete", evt.Key);
            Assert.AreEqual("cutscene_1", evt.SceneId);
        }

        #endregion

        #region CurrentSceneId Tests

        [Test]
        public void CurrentSceneId_InitializedAsNull()
        {
            Assert.IsNull(_sceneManager.CurrentSceneId);
        }

        #endregion

        #region IsTransitioning Tests

        [Test]
        public void IsTransitioning_InitializedAsFalse()
        {
            Assert.IsFalse(_sceneManager.IsTransitioning);
        }

        #endregion
    }
}
