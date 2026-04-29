using System.Collections.Generic;
using Core.Narrative;
using Core.Settings;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Unit.Core
{
    /// <summary>
    /// Unit tests for SettingsData and SettingsSystem.
    /// Tests cover: default values, volume clamping, JSON serialization,
    /// event emission, and setting changes.
    /// </summary>
    [TestFixture]
    public class SettingsSystemTests
    {
        #region SettingsData Tests

        [Test]
        public void SettingsData_DefaultValues_AreCorrect()
        {
            var data = new SettingsData();

            Assert.AreEqual(0.8f, data.MusicVolume, 0.001f);
            Assert.AreEqual(0.8f, data.SfxVolume, 0.001f);
            Assert.AreEqual(1.0f, data.VoiceVolume, 0.001f);
            Assert.AreEqual(TextSpeed.Medium, data.TextSpeed);
            Assert.IsTrue(data.HapticEnabled);
            Assert.IsFalse(data.AutoAdvanceEnabled);
        }

        [Test]
        public void SettingsData_VolumeClamping_ClampsToZero()
        {
            var data = new SettingsData();

            data.MusicVolume = -0.5f;
            Assert.AreEqual(0f, data.MusicVolume);

            data.SfxVolume = -1.0f;
            Assert.AreEqual(0f, data.SfxVolume);

            data.VoiceVolume = -0.1f;
            Assert.AreEqual(0f, data.VoiceVolume);
        }

        [Test]
        public void SettingsData_VolumeClamping_ClampsToOne()
        {
            var data = new SettingsData();

            data.MusicVolume = 1.5f;
            Assert.AreEqual(1f, data.MusicVolume);

            data.SfxVolume = 2.0f;
            Assert.AreEqual(1f, data.SfxVolume);

            data.VoiceVolume = 1.1f;
            Assert.AreEqual(1f, data.VoiceVolume);
        }

        [Test]
        public void SettingsData_VolumeClamping_AcceptsValidValues()
        {
            var data = new SettingsData();

            data.MusicVolume = 0.5f;
            Assert.AreEqual(0.5f, data.MusicVolume);

            data.SfxVolume = 0.0f;
            Assert.AreEqual(0.0f, data.SfxVolume);

            data.VoiceVolume = 1.0f;
            Assert.AreEqual(1.0f, data.VoiceVolume);
        }

        [Test]
        public void SettingsData_TextSpeed_StoresAllValues()
        {
            var data = new SettingsData();

            data.TextSpeed = TextSpeed.Slow;
            Assert.AreEqual(TextSpeed.Slow, data.TextSpeed);

            data.TextSpeed = TextSpeed.Medium;
            Assert.AreEqual(TextSpeed.Medium, data.TextSpeed);

            data.TextSpeed = TextSpeed.Fast;
            Assert.AreEqual(TextSpeed.Fast, data.TextSpeed);
        }

        [Test]
        public void SettingsData_TextSpeed_HasCorrectMsPerChar()
        {
            Assert.AreEqual(150, (int)TextSpeed.Slow);
            Assert.AreEqual(30, (int)TextSpeed.Medium);
            Assert.AreEqual(10, (int)TextSpeed.Fast);
        }

        [Test]
        public void SettingsData_HapticEnabled_StoresBool()
        {
            var data = new SettingsData();

            data.HapticEnabled = false;
            Assert.IsFalse(data.HapticEnabled);

            data.HapticEnabled = true;
            Assert.IsTrue(data.HapticEnabled);
        }

        [Test]
        public void SettingsData_AutoAdvanceEnabled_StoresBool()
        {
            var data = new SettingsData();

            data.AutoAdvanceEnabled = true;
            Assert.IsTrue(data.AutoAdvanceEnabled);

            data.AutoAdvanceEnabled = false;
            Assert.IsFalse(data.AutoAdvanceEnabled);
        }

        [Test]
        public void SettingsData_Clone_CreatesIndependentCopy()
        {
            var original = new SettingsData
            {
                MusicVolume = 0.5f,
                SfxVolume = 0.6f,
                VoiceVolume = 0.7f,
                TextSpeed = TextSpeed.Fast,
                HapticEnabled = false,
                AutoAdvanceEnabled = true
            };

            var clone = original.Clone();

            // Verify values match
            Assert.AreEqual(original.MusicVolume, clone.MusicVolume);
            Assert.AreEqual(original.SfxVolume, clone.SfxVolume);
            Assert.AreEqual(original.VoiceVolume, clone.VoiceVolume);
            Assert.AreEqual(original.TextSpeed, clone.TextSpeed);
            Assert.AreEqual(original.HapticEnabled, clone.HapticEnabled);
            Assert.AreEqual(original.AutoAdvanceEnabled, clone.AutoAdvanceEnabled);

            // Verify it's a different object
            Assert.AreNotSame(original, clone);
        }

        [Test]
        public void SettingsData_CreateDefault_ReturnsNonNull()
        {
            var data = SettingsData.CreateDefault();
            Assert.IsNotNull(data);
        }

        #endregion

        #region JSON Serialization Tests

        [Test]
        public void SettingsData_JsonRoundTrip_PreservesValues()
        {
            var original = new SettingsData
            {
                MusicVolume = 0.5f,
                SfxVolume = 0.6f,
                VoiceVolume = 0.7f,
                TextSpeed = TextSpeed.Fast,
                HapticEnabled = false,
                AutoAdvanceEnabled = true
            };

            string json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<SettingsData>(json);

            Assert.IsNotNull(restored);
            Assert.AreEqual(original.MusicVolume, restored.MusicVolume, 0.001f);
            Assert.AreEqual(original.SfxVolume, restored.SfxVolume, 0.001f);
            Assert.AreEqual(original.VoiceVolume, restored.VoiceVolume, 0.001f);
            Assert.AreEqual(original.TextSpeed, restored.TextSpeed);
            Assert.AreEqual(original.HapticEnabled, restored.HapticEnabled);
            Assert.AreEqual(original.AutoAdvanceEnabled, restored.AutoAdvanceEnabled);
        }

        [Test]
        public void SettingsData_JsonRoundTrip_PreservesDefaultValues()
        {
            var original = SettingsData.CreateDefault();

            string json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<SettingsData>(json);

            Assert.IsNotNull(restored);
            Assert.AreEqual(original.MusicVolume, restored.MusicVolume, 0.001f);
            Assert.AreEqual(original.SfxVolume, restored.SfxVolume, 0.001f);
            Assert.AreEqual(original.VoiceVolume, restored.VoiceVolume, 0.001f);
            Assert.AreEqual(original.TextSpeed, restored.TextSpeed);
            Assert.AreEqual(original.HapticEnabled, restored.HapticEnabled);
            Assert.AreEqual(original.AutoAdvanceEnabled, restored.AutoAdvanceEnabled);
        }

        [Test]
        public void SettingsData_JsonIncludesAllFields()
        {
            var data = new SettingsData();
            string json = JsonUtility.ToJson(data);

            Assert.IsTrue(json.Contains("_musicVolume"));
            Assert.IsTrue(json.Contains("_sfxVolume"));
            Assert.IsTrue(json.Contains("_voiceVolume"));
            Assert.IsTrue(json.Contains("_textSpeed"));
            Assert.IsTrue(json.Contains("_hapticEnabled"));
            Assert.IsTrue(json.Contains("_autoAdvanceEnabled"));
        }

        #endregion

        #region SettingsSystem Instance Tests

        [Test]
        public void SettingsSystem_Instance_ReturnsNonNull()
        {
            // Note: In a real test environment with Unity, Instance would be set by Awake
            // This tests the static property access pattern
            var instance = SettingsSystem.Instance;
            Assert.IsNotNull(instance);
        }

        [Test]
        public void SettingsSystem_Instance_ReturnsSameInstance()
        {
            var instance1 = SettingsSystem.Instance;
            var instance2 = SettingsSystem.Instance;
            Assert.AreSame(instance1, instance2);
        }

        #endregion

        #region SettingsEvents Tests

        [Test]
        public void MusicVolumeChangedEvent_HasCorrectKey()
        {
            var evt = new MusicVolumeChangedEvent(0.5f);
            Assert.AreEqual("settings.volume.music", evt.Key);
            Assert.AreEqual(0.5f, evt.Volume);
        }

        [Test]
        public void SFXVolumeChangedEvent_HasCorrectKey()
        {
            var evt = new SFXVolumeChangedEvent(0.6f);
            Assert.AreEqual("settings.volume.sfx", evt.Key);
            Assert.AreEqual(0.6f, evt.Volume);
        }

        [Test]
        public void VoiceVolumeChangedEvent_HasCorrectKey()
        {
            var evt = new VoiceVolumeChangedEvent(0.7f);
            Assert.AreEqual("settings.volume.voice", evt.Key);
            Assert.AreEqual(0.7f, evt.Volume);
        }

        [Test]
        public void TextSpeedChangedEvent_HasCorrectKey()
        {
            var evt = new TextSpeedChangedEvent(TextSpeed.Fast);
            Assert.AreEqual("settings.text.speed", evt.Key);
            Assert.AreEqual(TextSpeed.Fast, evt.Speed);
        }

        [Test]
        public void HapticEnabledChangedEvent_HasCorrectKey()
        {
            var evt = new HapticEnabledChangedEvent(false);
            Assert.AreEqual("settings.haptic.enabled", evt.Key);
            Assert.IsFalse(evt.Enabled);
        }

        [Test]
        public void AutoAdvanceChangedEvent_HasCorrectKey()
        {
            var evt = new AutoAdvanceChangedEvent(true);
            Assert.AreEqual("settings.auto_advance", evt.Key);
            Assert.IsTrue(evt.Enabled);
        }

        #endregion
    }
}
