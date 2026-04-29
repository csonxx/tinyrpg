using Core.Narrative;

namespace Core.Scene
{
    /// <summary>
    /// Base class for all scene-related events.
    /// All scene events use the "scene." prefix for EventBus glob matching.
    /// </summary>
    public abstract class SceneEvent : NSMEvent
    {
        public string SceneId { get; }

        protected SceneEvent(string key, string sceneId) : base(key)
        {
            SceneId = sceneId;
        }
    }

    /// <summary>
    /// Emitted when a scene has finished loading and is ready for display.
    /// The scene's background art and content are fully loaded.
    /// </summary>
    public sealed class SceneReadyEvent : SceneEvent
    {
        public string SceneMusic { get; }

        public SceneReadyEvent(string sceneId, string sceneMusic = null) : base("scene.ready", sceneId)
        {
            SceneMusic = sceneMusic;
        }
    }

    /// <summary>
    /// Emitted when a cutscene overlay has finished playing.
    /// Used to notify systems that overlay control can be returned.
    /// </summary>
    public sealed class CutsceneCompleteEvent : SceneEvent
    {
        public CutsceneCompleteEvent(string cutsceneId) : base("scene.cutscene_complete", cutsceneId) { }
    }

    /// <summary>
    /// Emitted when SceneManagement requests a preload of a scene's assets.
    /// Addressables system should respond by loading the scene's background art.
    /// </summary>
    public sealed class ScenePreloadRequestedEvent : SceneEvent
    {
        public ScenePreloadRequestedEvent(string sceneId) : base("scene.preload_requested", sceneId) { }
    }

    /// <summary>
    /// Emitted when a transition begins (fade out started).
    /// </summary>
    public sealed class TransitionBeganEvent : SceneEvent
    {
        public TransitionType TransitionType { get; }

        public TransitionBeganEvent(string sceneId, TransitionType transitionType)
            : base("scene.transition_began", sceneId)
        {
            TransitionType = transitionType;
        }
    }

    /// <summary>
    /// Emitted when a transition completes (fade in finished).
    /// </summary>
    public sealed class TransitionCompleteEvent : SceneEvent
    {
        public TransitionType TransitionType { get; }

        public TransitionCompleteEvent(string sceneId, TransitionType transitionType)
            : base("scene.transition_complete", sceneId)
        {
            TransitionType = transitionType;
        }
    }

    /// <summary>
    /// Emitted when the scene stack changes (overlay pushed or popped).
    /// </summary>
    public sealed class SceneStackChangedEvent : NSMEvent
    {
        public string[] Stack { get; }

        public SceneStackChangedEvent(string[] stack) : base("scene.stack_changed")
        {
            Stack = stack;
        }
    }
}
