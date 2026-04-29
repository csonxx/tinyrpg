using System;
using UnityEngine;

namespace Core.Settings
{
    /// <summary>
    /// Text display speed affecting DialogueEngine/DialogueBox character animation.
    /// </summary>
    public enum TextSpeed
    {
        Slow = 150,
        Medium = 30,
        Fast = 10
    }

    /// <summary>
    /// Serializable settings data with default values.
    /// All volume values are floats in range 0.0 to 1.0.
    ///
    /// Implements S3-1 per design/gdd/settings-system.md.
    /// </summary>
    [Serializable]
    public sealed class SettingsData
    {
        private const float DEFAULT_MUSIC_VOLUME = 0.8f;
        private const float DEFAULT_SFX_VOLUME = 0.8f;
        private const float DEFAULT_VOICE_VOLUME = 1.0f;
        private const TextSpeed DEFAULT_TEXT_SPEED = TextSpeed.Medium;
        private const bool DEFAULT_HAPTIC_ENABLED = true;
        private const bool DEFAULT_AUTO_ADVANCE_ENABLED = false;

        [SerializeField, Range(0f, 1f)]
        private float _musicVolume = DEFAULT_MUSIC_VOLUME;

        [SerializeField, Range(0f, 1f)]
        private float _sfxVolume = DEFAULT_SFX_VOLUME;

        [SerializeField, Range(0f, 1f)]
        private float _voiceVolume = DEFAULT_VOICE_VOLUME;

        [SerializeField]
        private TextSpeed _textSpeed = DEFAULT_TEXT_SPEED;

        [SerializeField]
        private bool _hapticEnabled = DEFAULT_HAPTIC_ENABLED;

        [SerializeField]
        private bool _autoAdvanceEnabled = DEFAULT_AUTO_ADVANCE_ENABLED;

        #region Properties

        /// <summary>
        /// Music volume (0.0 to 1.0). Default: 0.8.
        /// </summary>
        public float MusicVolume
        {
            get => _musicVolume;
            set => _musicVolume = Mathf.Clamp01(value);
        }

        /// <summary>
        /// SFX volume (0.0 to 1.0). Default: 0.8.
        /// </summary>
        public float SfxVolume
        {
            get => _sfxVolume;
            set => _sfxVolume = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Voice volume (0.0 to 1.0). Default: 1.0.
        /// </summary>
        public float VoiceVolume
        {
            get => _voiceVolume;
            set => _voiceVolume = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Text display speed affecting character animation timing. Default: Medium (30ms).
        /// </summary>
        public TextSpeed TextSpeed
        {
            get => _textSpeed;
            set => _textSpeed = value;
        }

        /// <summary>
        /// Enable haptic feedback on touch gestures. Default: true.
        /// </summary>
        public bool HapticEnabled
        {
            get => _hapticEnabled;
            set => _hapticEnabled = value;
        }

        /// <summary>
        /// Auto-advance dialogue when text animation completes. Default: false.
        /// </summary>
        public bool AutoAdvanceEnabled
        {
            get => _autoAdvanceEnabled;
            set => _autoAdvanceEnabled = value;
        }

        #endregion

        /// <summary>
        /// Returns a new SettingsData with all default values.
        /// </summary>
        public static SettingsData CreateDefault()
        {
            return new SettingsData();
        }

        /// <summary>
        /// Creates a copy of this settings data.
        /// </summary>
        public SettingsData Clone()
        {
            return new SettingsData
            {
                _musicVolume = _musicVolume,
                _sfxVolume = _sfxVolume,
                _voiceVolume = _voiceVolume,
                _textSpeed = _textSpeed,
                _hapticEnabled = _hapticEnabled,
                _autoAdvanceEnabled = _autoAdvanceEnabled
            };
        }
    }
}
