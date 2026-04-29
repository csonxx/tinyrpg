using System;
using System.Collections.Generic;
using Core.Audio;
using Core.Narrative;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Integration.Audio
{
    /// <summary>
    /// Integration tests for AudioManagement.
    /// Tests event wiring, pause/resume, and BGM scene linking.
    /// </summary>
    [TestFixture]
    public class AudioManagementTests
    {
        private AudioManagement _audio;
        private NarrativeStateMachine _nsm;
        private NSMConfig _nsmConfig;
        private List<string> _logMessages;

        [SetUp]
        public void SetUp()
        {
            _logMessages = new List<string>();
            Application.logMessageReceived += OnLogMessage;

            // Create NSM first (dependency)
            _nsmConfig = ScriptableObject.CreateInstance<NSMConfig>();
            _nsmConfig.name = "TestNSMConfig";
            _nsm = new NarrativeStateMachine(_nsmConfig);

            // Access AudioManagement singleton (creates it)
            _audio = AudioManagement.Instance;
            Assert.IsNotNull(_audio, "AudioManagement singleton should be created");
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= OnLogMessage;

            // Clean up singleton
            var go = _audio.gameObject;
            if (go != null)
            {
                DestroyImmediate(go);
            }

            if (_nsmConfig != null)
            {
                DestroyImmediate(_nsmConfig);
            }
        }

        private void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            _logMessages.Add(condition);
        }

        private string GetLastLog()
        {
            return _logMessages.Count > 0 ? _logMessages[_logMessages.Count - 1] : null;
        }

        #region SceneReady BGM Tests

        [Test]
        public void SceneReady_WithSceneMusic_PlaysBGM()
        {
            // Arrange
            var evt = new SceneReadyEvent("test_scene", "bgm_test");

            // Act
            EventBus.Instance.Emit(evt);

            // Assert
            Assert.IsTrue(GetLastLog().Contains("PlayBGM"));
            Assert.IsTrue(GetLastLog().Contains("bgm_test"));
        }

        [Test]
        public void SceneReady_WithNullMusic_DoesNotCrash()
        {
            // Arrange
            var evt = new SceneReadyEvent("test_scene", null);

            // Act & Assert (should not throw)
            EventBus.Instance.Emit(evt);

            // Assert - should log about null key
            Assert.IsTrue(GetLastLog().Contains("null/empty key"));
        }

        #endregion

        #region NSM Pause/Resume Tests

        [Test]
        public void NSM_MenuOpen_PausesAudio()
        {
            // Arrange - start with some BGM playing
            EventBus.Instance.Emit(new SceneReadyEvent("scene", "bgm_test"));
            ClearLogs();

            // Act - open menu
            _nsm.SetState(NSMState.MENU_OPEN);

            // Assert
            Assert.IsTrue(GetLastLog().Contains("Pause"));
        }

        [Test]
        public void NSM_DialogueActive_ResumesAudio()
        {
            // Arrange - start BGM and pause
            EventBus.Instance.Emit(new SceneReadyEvent("scene", "bgm_test"));
            _nsm.SetState(NSMState.MENU_OPEN);
            ClearLogs();

            // Act - resume to dialogue
            _nsm.SetState(NSMState.DIALOGUE_ACTIVE);

            // Assert
            Assert.IsTrue(GetLastLog().Contains("Resume"));
        }

        [Test]
        public void NSM_Cutscene_PausesAudio()
        {
            // Arrange
            EventBus.Instance.Emit(new SceneReadyEvent("scene", "bgm_test"));
            ClearLogs();

            // Act
            _nsm.SetState(NSMState.CUTSCENE);

            // Assert
            Assert.IsTrue(GetLastLog().Contains("Pause"));
        }

        [Test]
        public void NSM_SceneActive_ResumesAudio()
        {
            // Arrange - pause with cutscene
            EventBus.Instance.Emit(new SceneReadyEvent("scene", "bgm_test"));
            _nsm.SetState(NSMState.CUTSCENE);
            ClearLogs();

            // Act
            _nsm.SetState(NSMState.SCENE_ACTIVE);

            // Assert
            Assert.IsTrue(GetLastLog().Contains("Resume"));
        }

        #endregion

        #region BGM Fade Tests

        [Test]
        public void StopBGM_EmitsBGMFadeCompleteEvent()
        {
            // Arrange
            var capturedEvents = new List<NSMEvent>();
            EventBus.Instance.Subscribe(BGMFadeCompleteEvent.KEY, e => capturedEvents.Add(e));
            EventBus.Instance.Emit(new SceneReadyEvent("scene", "bgm_test"));
            ClearLogs();

            // Act
            _audio.StopBGM();

            // Assert
            Assert.AreEqual(1, capturedEvents.Count);
            var fadeEvent = capturedEvents[0] as BGMFadeCompleteEvent;
            Assert.IsNotNull(fadeEvent);
            Assert.AreEqual("bgm_test", fadeEvent.BGMKey);
        }

        #endregion

        #region Volume Tests

        [Test]
        public void PlaySFX_LogsVolume()
        {
            // Arrange
            EventBus.Instance.Emit(new SceneReadyEvent("scene", "bgm_test"));
            ClearLogs();

            // Act
            EventBus.Instance.Emit(new SFXPlayEvent("sfx_click"));

            // Assert
            Assert.IsTrue(GetLastLog().Contains("PlaySFX"));
            Assert.IsTrue(GetLastLog().Contains("sfx_click"));
            Assert.IsTrue(GetLastLog().Contains("volume=1.00"));
        }

        [Test]
        public void PlayVoice_LogsVolume()
        {
            // Arrange
            ClearLogs();

            // Act
            EventBus.Instance.Emit(new VoicePlayEvent("voice_hello"));

            // Assert
            Assert.IsTrue(GetLastLog().Contains("PlayVoice"));
            Assert.IsTrue(GetLastLog().Contains("voice_hello"));
            Assert.IsTrue(GetLastLog().Contains("volume=1.00"));
        }

        #endregion

        private void ClearLogs()
        {
            _logMessages.Clear();
        }
    }
}
