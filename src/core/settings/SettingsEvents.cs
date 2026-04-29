using System;
using Core.Narrative;

namespace Core.Settings
{
    /// <summary>
    /// Event emitted when music volume setting changes.
    /// </summary>
    public sealed class MusicVolumeChangedEvent : NSMEvent
    {
        public const string KEY = "settings.volume.music";

        public float Volume { get; }

        public MusicVolumeChangedEvent(float volume)
        {
            Volume = volume;
        }

        public override string Key => KEY;
    }

    /// <summary>
    /// Event emitted when SFX volume setting changes.
    /// </summary>
    public sealed class SFXVolumeChangedEvent : NSMEvent
    {
        public const string KEY = "settings.volume.sfx";

        public float Volume { get; }

        public SFXVolumeChangedEvent(float volume)
        {
            Volume = volume;
        }

        public override string Key => KEY;
    }

    /// <summary>
    /// Event emitted when voice volume setting changes.
    /// </summary>
    public sealed class VoiceVolumeChangedEvent : NSMEvent
    {
        public const string KEY = "settings.volume.voice";

        public float Volume { get; }

        public VoiceVolumeChangedEvent(float volume)
        {
            Volume = volume;
        }

        public override string Key => KEY;
    }

    /// <summary>
    /// Event emitted when text speed setting changes.
    /// </summary>
    public sealed class TextSpeedChangedEvent : NSMEvent
    {
        public const string KEY = "settings.text.speed";

        public TextSpeed Speed { get; }

        public TextSpeedChangedEvent(TextSpeed speed)
        {
            Speed = speed;
        }

        public override string Key => KEY;
    }

    /// <summary>
    /// Event emitted when haptic feedback enabled setting changes.
    /// </summary>
    public sealed class HapticEnabledChangedEvent : NSMEvent
    {
        public const string KEY = "settings.haptic.enabled";

        public bool Enabled { get; }

        public HapticEnabledChangedEvent(bool enabled)
        {
            Enabled = enabled;
        }

        public override string Key => KEY;
    }

    /// <summary>
    /// Event emitted when auto-advance setting changes.
    /// </summary>
    public sealed class AutoAdvanceChangedEvent : NSMEvent
    {
        public const string KEY = "settings.auto_advance";

        public bool Enabled { get; }

        public AutoAdvanceChangedEvent(bool enabled)
        {
            Enabled = enabled;
        }

        public override string Key => KEY;
    }
}
