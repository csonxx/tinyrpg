using System;
using Core.Narrative;

namespace Core.Audio
{
    /// <summary>
    /// Raised when a scene is ready and should trigger scene-linked BGM.
    /// </summary>
    public sealed class SceneReadyEvent : NSMEvent
    {
        public const string KEY = "scene.ready";
        public string SceneId { get; }
        public string SceneMusic { get; }

        public SceneReadyEvent(string sceneId, string sceneMusic = null)
        {
            SceneId = sceneId ?? throw new ArgumentNullException(nameof(sceneId));
            SceneMusic = sceneMusic;
        }

        public override string Key => KEY;
    }

    /// <summary>
    /// Raised when BGM fade-out completes.
    /// </summary>
    public sealed class BGMFadeCompleteEvent : NSMEvent
    {
        public const string KEY = "audio.bgm_fade_complete";
        public string BGMKey { get; }

        public BGMFadeCompleteEvent(string bgmKey)
        {
            BGMKey = bgmKey;
        }

        public override string Key => KEY;
    }

    /// <summary>
    /// Request to play a sound effect.
    /// </summary>
    public sealed class SFXPlayEvent : NSMEvent
    {
        public const string KEY = "audio.sfx_play";
        public string SFXKey { get; }

        public SFXPlayEvent(string sfxKey)
        {
            SFXKey = sfxKey ?? throw new ArgumentNullException(nameof(sfxKey));
        }

        public override string Key => KEY;
    }

    /// <summary>
    /// Request to play a voice line.
    /// </summary>
    public sealed class VoicePlayEvent : NSMEvent
    {
        public const string KEY = "audio.voice_play";
        public string VoiceKey { get; }

        public VoicePlayEvent(string voiceKey)
        {
            VoiceKey = voiceKey ?? throw new ArgumentNullException(nameof(voiceKey));
        }

        public override string Key => KEY;
    }
}
