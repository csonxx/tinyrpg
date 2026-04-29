using System;
using System.Collections.Generic;
using Core.Narrative;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace Core.Scene
{
    /// <summary>
    /// Manages scene loading, transitions, and the overlay scene stack.
    ///
    /// Implements S2-2: Scene Management from the sprint-2-scene-management story.
    ///
    /// Key responsibilities:
    /// - Async scene loading via Unity's SceneManager with AsyncOperation
    /// - Transition animations: FADE_GREY (400ms out, 100ms hold, 400ms in), FADE_BLACK, CROSSFADE
    /// - Scene stack for overlays via PushOverlay/PopOverlay (max depth 3)
    /// - Addressables preload trigger when choicesRemaining <= 3 (via OnChoicesRemainingChanged)
    /// - Emits SceneReady(sceneId) event when scene is ready
    ///
    /// All timing values come from SceneManagementConfig and are data-driven.
    /// </summary>
    public class SceneManagement : MonoBehaviour
    {
        /// <summary>
        /// Exception thrown when a programming error is detected in scene stack operations.
        /// </summary>
        public class SceneStackException : Exception
        {
            public SceneStackException(string message) : base(message) { }
        }

        #region Configuration

        [Tooltip("Data-driven configuration for transition timing and stack limits.")]
        [SerializeField] private SceneManagementConfig _config;

        [Tooltip("EventBus instance for emitting scene events. If null, uses global EventBus.")]
        [SerializeField] private EventBus _eventBus;

        #endregion

        #region State

        private string _currentSceneId;
        private readonly List<string> _sceneStack = new List<string>();
        private readonly Dictionary<string, Texture> _preloadedBackgrounds = new Dictionary<string, Texture>();
        private AsyncOperation _pendingLoadOperation;
        private bool _isTransitioning;
        private float _overlayPushTime;

        /// <summary>
        /// Currently active scene identifier.
        /// </summary>
        public string CurrentSceneId => _currentSceneId;

        /// <summary>
        /// Current overlay stack (readonly). First element is bottom of stack.
        /// </summary>
        public IReadOnlyList<string> SceneStack => _sceneStack.AsReadOnly();

        /// <summary>
        /// True if a transition is currently in progress.
        /// </summary>
        public bool IsTransitioning => _isTransitioning;

        #endregion

        #region EventBus

        private EventBus EventBus => _eventBus ?? Core.Narrative.EventBus.Global;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_config == null)
            {
                Debug.LogError("[SceneManagement] No config assigned. Using defaults.");
            }
        }

        private void Update()
        {
            // Check if pending async load has completed
            if (_pendingLoadOperation != null && _pendingLoadOperation.isDone)
            {
                var operation = _pendingLoadOperation;
                _pendingLoadOperation = null;

                // Emit SceneReady after the async operation completes
                EmitSceneReady(_currentSceneId);
            }
        }

        #endregion

        #region Public API — Scene Loading

        /// <summary>
        /// Load a scene asynchronously with the specified transition animation.
        /// Uses LoadSceneMode.Single (replaces current scene).
        /// Emits TransitionBeganEvent at start and TransitionCompleteEvent when done.
        /// Emits SceneReadyEvent when the async load operation completes.
        /// </summary>
        /// <param name="sceneId">Unique identifier of the scene to load.</param>
        /// <param name="transitionType">Visual transition to use.</param>
        public void LoadScene(string sceneId, TransitionType transitionType = TransitionType.FADE_GREY)
        {
            if (string.IsNullOrEmpty(sceneId))
                throw new ArgumentException("sceneId cannot be null or empty", nameof(sceneId));

            if (_isTransitioning)
            {
                Debug.LogWarning($"[SceneManagement] LoadScene({sceneId}) ignored — transition already in progress.");
                return;
            }

            StartCoroutine(LoadSceneCoroutine(sceneId, transitionType));
        }

        private System.Collections.IEnumerator LoadSceneCoroutine(string sceneId, TransitionType transitionType)
        {
            _isTransitioning = true;
            _currentSceneId = sceneId;

            // Emit transition began event
            EventBus.Emit(new TransitionBeganEvent(sceneId, transitionType));

            // Phase 1: Fade out
            float fadeOutDuration = GetConfigValue(c => c.FadeOutDurationSec);
            yield return new WaitForSeconds(fadeOutDuration);

            // Phase 2: Load scene asynchronously (unless CROSSFADE which doesn't fade out first)
            if (transitionType != TransitionType.CROSSFADE)
            {
                // Hold at grey/black screen while loading
                if (transitionType == TransitionType.FADE_GREY)
                {
                    float holdDuration = GetConfigValue(c => c.FadeGreyHoldDurationSec);
                    yield return new WaitForSeconds(holdDuration);
                }
            }

            // Begin async load — does NOT block the main thread
            _pendingLoadOperation = SceneManager.LoadSceneAsync(sceneId, LoadSceneMode.Single);

            // Wait for async load to complete (or nearly complete for crossfade)
            if (transitionType == TransitionType.CROSSFADE)
            {
                float crossfadeDuration = GetConfigValue(c => c.CrossfadeDurationSec);
                yield return new WaitForSeconds(crossfadeDuration);
            }
            else
            {
                // For fade transitions, wait for scene to actually load
                while (!_pendingLoadOperation.isDone)
                {
                    yield return null;
                }
            }

            // Phase 3: Fade in
            if (transitionType != TransitionType.CROSSFADE)
            {
                float fadeInDuration = GetConfigValue(c => c.FadeInDurationSec);
                yield return new WaitForSeconds(fadeInDuration);
            }

            _isTransitioning = false;

            // Emit transition complete event
            EventBus.Emit(new TransitionCompleteEvent(sceneId, transitionType));

            // Note: SceneReadyEvent is emitted in Update() when _pendingLoadOperation.isDone becomes true.
            // If the load already completed during the fade-in phase, it fires immediately.
        }

        #endregion

        #region Public API — Overlay Stack

        /// <summary>
        /// Push a cutscene overlay onto the scene stack.
        /// Loads the overlay scene additively without affecting the main scene stack depth count.
        /// Throws SceneStackException if the overlay stack would exceed MaxSceneStackDepth.
        /// </summary>
        /// <param name="cutsceneId">Unique identifier of the cutscene overlay to push.</param>
        public void PushOverlay(string cutsceneId)
        {
            if (string.IsNullOrEmpty(cutsceneId))
                throw new ArgumentException("cutsceneId cannot be null or empty", nameof(cutsceneId));

            int maxDepth = GetConfigValue(c => c.MaxSceneStackDepth);
            if (_sceneStack.Count >= maxDepth)
            {
                throw new SceneStackException(
                    $"[SceneManagement] Cannot push overlay '{cutsceneId}' — stack depth would exceed maximum of {maxDepth}. " +
                    $"Current stack: [{string.Join(", ", _sceneStack)}]");
            }

            _sceneStack.Add(cutsceneId);
            _overlayPushTime = Time.time;

            // Load overlay additively
            SceneManager.LoadScene(cutsceneId, LoadSceneMode.Additive);

            EmitSceneStackChanged();
        }

        /// <summary>
        /// Pop the topmost cutscene overlay from the scene stack.
        /// Unloads the overlay scene and restores the previous scene.
        /// Throws SceneStackException if the stack is empty.
        /// </summary>
        public void PopOverlay()
        {
            if (_sceneStack.Count == 0)
            {
                throw new SceneStackException("[SceneManagement] Cannot pop overlay — stack is empty.");
            }

            float minDuration = GetConfigValue(c => c.MinOverlayDurationSec);
            float elapsed = Time.time - _overlayPushTime;
            if (elapsed < minDuration)
            {
                Debug.LogWarning($"[SceneManagement] PopOverlay called before MinOverlayDuration ({minDuration:F2}s). Elapsed: {elapsed:F2}s");
                // Still pop — this is just a warning, not a hard block
            }

            string cutsceneId = _sceneStack[_sceneStack.Count - 1];
            _sceneStack.RemoveAt(_sceneStack.Count - 1);

            // Unload the overlay scene
            SceneManager.UnloadSceneAsync(cutsceneId);

            EmitSceneStackChanged();
        }

        /// <summary>
        /// Peek at the topmost overlay without popping it.
        /// Returns null if the stack is empty.
        /// </summary>
        public string PeekOverlay()
        {
            if (_sceneStack.Count == 0)
                return null;
            return _sceneStack[_sceneStack.Count - 1];
        }

        #endregion

        #region Public API — Addressables Preload

        /// <summary>
        /// Called by the DialogueSystem when the number of remaining choices changes.
        /// When choicesRemaining <= PreloadLookaheadChoices, triggers a preload of
        /// the next logical scene's assets.
        /// </summary>
        /// <param name="choicesRemaining">Number of dialogue choices still available.</param>
        /// <param name="nextSceneId">Identifier of the next scene to preload (optional, for guidance).</param>
        public void OnChoicesRemainingChanged(int choicesRemaining, string nextSceneId = null)
        {
            int threshold = GetConfigValue(c => c.PreloadLookaheadChoices);
            if (choicesRemaining <= threshold && !string.IsNullOrEmpty(nextSceneId))
            {
                PreloadScene(nextSceneId);
            }
        }

        /// <summary>
        /// Preload a scene's background art via Addressables.
        /// Caches the loaded texture internally for fast retrieval.
        /// Emits ScenePreloadRequestedEvent to trigger the Addressables load.
        /// </summary>
        /// <param name="sceneId">Identifier of the scene whose background should be preloaded.</param>
        public void PreloadScene(string sceneId)
        {
            if (string.IsNullOrEmpty(sceneId))
                return;

            if (_preloadedBackgrounds.ContainsKey(sceneId))
            {
                // Already preloaded
                return;
            }

            // Emit event to trigger Addressables system to load the background
            EventBus.Emit(new ScenePreloadRequestedEvent(sceneId));

            // Begin Addressables load — in a real implementation, the Addressables system
            // would respond to the event and call RegisterPreloadedBackground(sceneId, texture).
            // Here we initiate the load directly as a fallback/demo.
            StartPreloadAddressables(sceneId);
        }

        /// <summary>
        /// Register a preloaded background texture. Called by the Addressables system
        /// after it finishes loading the asset.
        /// </summary>
        /// <param name="sceneId">Identifier of the scene.</param>
        /// <param name="background">The loaded texture, or null if load failed.</param>
        public void RegisterPreloadedBackground(string sceneId, Texture background)
        {
            if (string.IsNullOrEmpty(sceneId))
                return;

            if (background != null)
            {
                _preloadedBackgrounds[sceneId] = background;
            }
            else
            {
                Debug.LogWarning($"[SceneManagement] Failed to preload background for scene '{sceneId}'.");
            }
        }

        /// <summary>
        /// Retrieve a preloaded background texture.
        /// Returns null if the scene has not been preloaded.
        /// </summary>
        /// <param name="sceneId">Identifier of the scene.</param>
        /// <returns>The preloaded Texture, or null if not preloaded.</returns>
        public Texture GetPreloadedBackground(string sceneId)
        {
            return _preloadedBackgrounds.TryGetValue(sceneId, out var texture) ? texture : null;
        }

        private void StartPreloadAddressables(string sceneId)
        {
            // Addressables address format: "Backgrounds/{sceneId}"
            var handle = Addressables.LoadAssetAsync<Texture>($"Backgrounds/{sceneId}");
            handle.Completed += operation =>
            {
                if (operation.Status == AsyncOperationStatus.Succeeded)
                {
                    RegisterPreloadedBackground(sceneId, operation.Result);
                }
                else
                {
                    RegisterPreloadedBackground(sceneId, null);
                }
            };
        }

        #endregion

        #region Event Emission

        private void EmitSceneReady(string sceneId)
        {
            EventBus.Emit(new SceneReadyEvent(sceneId));
        }

        private void EmitSceneStackChanged()
        {
            EventBus.Emit(new SceneStackChangedEvent(_sceneStack.ToArray()));
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Get a config value, returning a safe default if config is null.
        /// </summary>
        private T GetConfigValue<T>(Func<SceneManagementConfig, T> accessor)
        {
            if (_config != null)
                return accessor(_config);

            // Return sensible defaults if no config is assigned
            if (accessor == c => c.FadeOutDurationSec) return (T)(object)0.4f;
            if (accessor == c => c.FadeGreyHoldDurationSec) return (T)(object)0.1f;
            if (accessor == c => c.FadeInDurationSec) return (T)(object)0.4f;
            if (accessor == c => c.CrossfadeDurationSec) return (T)(object)0.5f;
            if (accessor == c => c.MaxSceneStackDepth) return (T)(object)3;
            if (accessor == c => c.MinOverlayDurationSec) return (T)(object)0.4f;
            if (accessor == c => c.PreloadLookaheadChoices) return (T)(object)3;

            return default;
        }

        #endregion
    }
}
