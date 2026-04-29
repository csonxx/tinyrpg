using System;
using System.Collections.Generic;
using Core.Narrative.Dialogue;
using Core.Scene;
using UnityEngine;

namespace Core.Narrative
{
    /// <summary>
    /// Manages the linear progression of episodes, chapters, and scenes.
    ///
    /// This system implements the Episode Structure defined in the GDD:
    /// - Loads EpisodeData containing ordered chapters and scenes
    /// - Subscribes to DialogueSceneComplete events to advance to the next scene
    /// - Handles chapter and episode completion
    /// - Delegates scene loading to IEpisodeSceneManagement (Scene Management is parallel Sprint 2 work)
    ///
    /// Sprints: S2-1
    /// Design Doc: design/gdd/episode-structure.md
    /// </summary>
    public sealed class EpisodeStructure : MonoBehaviour
    {
        /// <summary>
        /// Default fade duration in seconds for scene transitions.
        /// Tune this via the inspector or a config file in production.
        /// </summary>
        private const float k_DefaultTransitionDuration = 0.5f;

        [SerializeField] private EpisodeData _episodeData;
        [SerializeField] private IEpisodeSceneManagement _sceneManagement;

        private EpisodeState _currentState;
        private int _currentChapterIndex;
        private int _currentSceneIndex;
        private bool _isRunning;

        /// <summary>
        /// The currently active episode data.
        /// </summary>
        public EpisodeData EpisodeData => _episodeData;

        /// <summary>
        /// The current runtime state of the episode.
        /// </summary>
        public EpisodeState CurrentState => _currentState;

        /// <summary>
        /// The index of the currently active chapter (0-based).
        /// </summary>
        public int CurrentChapterIndex => _currentChapterIndex;

        /// <summary>
        /// The index of the currently active scene within its chapter (0-based).
        /// </summary>
        public int CurrentSceneIndex => _currentSceneIndex;

        /// <summary>
        /// Whether an episode is currently running.
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Event fired when the episode state changes.
        /// Payload: (EpisodeState previousState, EpisodeState newState).
        /// </summary>
        public event Action<EpisodeState, EpisodeState> OnStateChanged;

        private void Awake()
        {
            if (_sceneManagement == null)
            {
                _sceneManagement = new DefaultEpisodeSceneManagement();
            }
        }

        private void OnEnable()
        {
            NarrativeStateMachine.Instance.Subscribe(
                DialogueSceneCompleteEvent.KEY,
                OnDialogueSceneComplete);
        }

        private void OnDisable()
        {
            NarrativeStateMachine.Instance.Unsubscribe(
                DialogueSceneCompleteEvent.KEY,
                OnDialogueSceneComplete);
        }

        /// <summary>
        /// Starts the episode from the beginning.
        /// </summary>
        public void StartEpisode()
        {
            if (_episodeData == null)
            {
                Debug.LogError("[EpisodeStructure] Cannot start episode: EpisodeData is null.");
                return;
            }

            _currentChapterIndex = 0;
            _currentSceneIndex = 0;
            _isRunning = true;

            SetState(EpisodeState.EpisodeLoading);

            // Emit EpisodeStarted event
            EventBusEmitter.Emit(EpisodeEvents.EpisodeStartedKey, _episodeData.EpisodeId);

            // Update NSM keys
            NarrativeStateMachine.Instance.Set(EpisodeKeys.CurrentEpisode, _episodeData.EpisodeId);
            NarrativeStateMachine.Instance.Set(EpisodeKeys.EpisodeComplete, "false");
            NarrativeStateMachine.Instance.Set(EpisodeKeys.CurrentChapter, _currentChapterIndex.ToString());

            // Load first scene
            LoadCurrentScene();
        }

        /// <summary>
        /// Starts the episode resuming from a specific chapter and scene.
        /// </summary>
        /// <param name="chapterIndex">The chapter index to resume from.</param>
        /// <param name="sceneIndex">The scene index to resume from.</param>
        public void ResumeEpisode(int chapterIndex, int sceneIndex)
        {
            if (_episodeData == null)
            {
                Debug.LogError("[EpisodeStructure] Cannot resume episode: EpisodeData is null.");
                return;
            }

            if (chapterIndex < 0 || chapterIndex >= _episodeData.Chapters.Count)
            {
                Debug.LogError($"[EpisodeStructure] Invalid chapter index: {chapterIndex}");
                return;
            }

            _currentChapterIndex = chapterIndex;
            _currentSceneIndex = sceneIndex;
            _isRunning = true;

            SetState(EpisodeState.EpisodeLoading);

            EventBusEmitter.Emit(EpisodeEvents.EpisodeStartedKey, _episodeData.EpisodeId);

            NarrativeStateMachine.Instance.Set(EpisodeKeys.CurrentEpisode, _episodeData.EpisodeId);
            NarrativeStateMachine.Instance.Set(EpisodeKeys.EpisodeComplete, "false");
            NarrativeStateMachine.Instance.Set(EpisodeKeys.CurrentChapter, _currentChapterIndex.ToString());

            LoadCurrentScene();
        }

        /// <summary>
        /// Forces a transition to a specific scene, bypassing normal linear progression.
        /// Uses the forceTransitionOverride from ChapterData if set, otherwise uses default.
        /// </summary>
        /// <param name="sceneId">The target scene ID to transition to.</param>
        public void ForceTransition(string sceneId)
        {
            if (!_isRunning)
            {
                Debug.LogWarning("[EpisodeStructure] Cannot force transition: episode is not running.");
                return;
            }

            SetState(EpisodeState.SceneTransitioning);
            _sceneManagement.LoadScene(sceneId, TransitionType.FADE_GREY);
        }

        /// <summary>
        /// Manually advances to the next scene or chapter.
        /// Used for skippable content or when DialogueEngine does not emit DialogueSceneComplete.
        /// </summary>
        public void AdvanceToNextScene()
        {
            if (!_isRunning) return;
            AdvanceSceneOrChapter();
        }

        /// <summary>
        /// Called by scene setup when the scene is ready (dialogue can begin).
        /// This replaces an explicit SceneReady event that doesn't exist yet.
        /// </summary>
        public void OnSceneReady()
        {
            if (!_isRunning) return;

            // If we were waiting for the scene to load, activate the chapter now
            if (_currentState == EpisodeState.EpisodeLoading)
            {
                SetState(EpisodeState.ChapterActive);
                // DialogueEngine.StartDialogue will be called by the scene's DialogueBridge component
            }
        }

        private void LoadCurrentScene()
        {
            if (_currentChapterIndex >= _episodeData.Chapters.Count)
            {
                CompleteEpisode();
                return;
            }

            var chapter = _episodeData.Chapters[_currentChapterIndex];
            if (_currentSceneIndex >= chapter.Scenes.Count)
            {
                CompleteChapter();
                return;
            }

            var scene = chapter.Scenes[_currentSceneIndex];
            var transitionType = ResolveTransitionType(chapter, scene);
            _sceneManagement.LoadScene(scene.SceneId, transitionType);
        }

        private TransitionType ResolveTransitionType(ChapterData chapter, SceneData scene)
        {
            // Force transition override takes precedence
            if (!string.IsNullOrEmpty(chapter.ForceTransitionOverride))
            {
                if (Enum.TryParse<TransitionType>(chapter.ForceTransitionOverride, true, out var overrideType))
                {
                    return overrideType;
                }
            }

            // Scene-level memoir/flashback flag
            if (scene.IsMemoirOrFlashback)
            {
                return TransitionType.FADE_BLACK;
            }

            return TransitionType.FADE_GREY;
        }

        private void OnDialogueSceneComplete(NSMEvent e)
        {
            if (!_isRunning) return;
            if (e is not DialogueSceneCompleteEvent sceneComplete) return;

            AdvanceSceneOrChapter();
        }

        private void AdvanceSceneOrChapter()
        {
            var chapter = _episodeData.Chapters[_currentChapterIndex];
            _currentSceneIndex++;

            if (_currentSceneIndex >= chapter.Scenes.Count)
            {
                CompleteChapter();
            }
            else
            {
                // Transition to next scene
                SetState(EpisodeState.SceneTransitioning);
                var scene = chapter.Scenes[_currentSceneIndex];
                var transitionType = ResolveTransitionType(chapter, scene);
                _sceneManagement.LoadScene(scene.SceneId, transitionType);
            }
        }

        private void CompleteChapter()
        {
            SetState(EpisodeState.ChapterComplete);

            EventBusEmitter.Emit(EpisodeEvents.ChapterCompleteKey, _episodeData.EpisodeId, _currentChapterIndex);

            _currentChapterIndex++;
            _currentSceneIndex = 0;

            NarrativeStateMachine.Instance.Set(EpisodeKeys.CurrentChapter, _currentChapterIndex.ToString());

            if (_currentChapterIndex >= _episodeData.Chapters.Count)
            {
                CompleteEpisode();
            }
            else
            {
                // Load first scene of next chapter
                SetState(EpisodeState.EpisodeLoading);
                LoadCurrentScene();
            }
        }

        private void CompleteEpisode()
        {
            _isRunning = false;
            SetState(EpisodeState.EpisodeComplete);

            NarrativeStateMachine.Instance.Set(EpisodeKeys.EpisodeComplete, "true");

            EventBusEmitter.Emit(EpisodeEvents.EpisodeCompleteKey, _episodeData.EpisodeId, _episodeData.IsLastEpisode);
        }

        private void SetState(EpisodeState newState)
        {
            if (_currentState == newState) return;

            var previousState = _currentState;
            _currentState = newState;
            OnStateChanged?.Invoke(previousState, newState);
        }
    }

    /// <summary>
    /// Emits events through the global EventBus.
    /// </summary>
    internal static class EventBusEmitter
    {
        public static void Emit(string key, params object[] args)
        {
            // Events with no payload
            if (args == null || args.Length == 0)
            {
                NarrativeStateMachine.Instance.EventBus.Emit(new GenericEvent(key));
            }
            else
            {
                NarrativeStateMachine.Instance.EventBus.Emit(new GenericEvent(key, args));
            }
        }
    }

    /// <summary>
    /// Generic event for carrying simple payloads through the EventBus.
    /// </summary>
    internal sealed class GenericEvent : NSMEvent
    {
        public object[] Args { get; }

        public GenericEvent(string key, object[] args = null) : base(key)
        {
            Args = args;
        }
    }

    /// <summary>
    /// Default stub implementation of IEpisodeSceneManagement.
    /// Logs scene load requests; replace with real Scene Management system before production.
    /// </summary>
    public sealed class DefaultEpisodeSceneManagement : IEpisodeSceneManagement
    {
        public void LoadScene(string sceneId, TransitionType transitionType)
        {
            Debug.Log($"[DefaultEpisodeSceneManagement] LoadScene(\"{sceneId}\", {transitionType})");
            // TODO: Replace with actual Unity scene loading:
            // SceneManager.LoadScene(sceneId);
        }
    }
}
