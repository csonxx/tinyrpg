using UnityEngine;

namespace Core.Scene
{
    /// <summary>
    /// ScriptableObject configuration for SceneManagement tuning knobs.
    /// All values are data-driven and editable by designers without code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "SceneManagementConfig", menuName = "TinyRPG/Scene/SceneManagementConfig")]
    public class SceneManagementConfig : ScriptableObject
    {
        #region Transition Timing

        [Header("Transition Timing (milliseconds)")]

        [Tooltip("Duration of the fade-out phase in FADE_GREY and FADE_BLACK transitions.")]
        [SerializeField] private int _fadeOutDurationMs = 400;

        [Tooltip("Duration of the hold phase in FADE_GREY transition (grey screen visible).")]
        [SerializeField] private int _fadeGreyHoldDurationMs = 100;

        [Tooltip("Duration of the fade-in phase in FADE_GREY and FADE_BLACK transitions.")]
        [SerializeField] private int _fadeInDurationMs = 400;

        [Tooltip("Duration of a crossfade transition in milliseconds.")]
        [SerializeField] private int _crossfadeDurationMs = 500;

        #endregion

        #region Scene Stack

        [Header("Scene Stack (Overlay)")]

        [Tooltip("Maximum depth of the overlay scene stack. PushOverlay will throw if exceeded.")]
        [SerializeField] private int _maxSceneStackDepth = 3;

        [Tooltip("Minimum duration an overlay must remain active before it can be popped (milliseconds).")]
        [SerializeField] private int _minOverlayDurationMs = 400;

        #endregion

        #region Addressables Preload

        [Header("Addressables Preload")]

        [Tooltip("When dialogue choices remaining falls to or below this value, trigger a scene preload.")]
        [SerializeField] private int _preloadLookaheadChoices = 3;

        #endregion

        #region Public Accessors

        /// <summary>
        /// Duration of fade-out phase in seconds.
        /// </summary>
        public float FadeOutDurationSec => _fadeOutDurationMs / 1000f;

        /// <summary>
        /// Duration of grey hold phase in seconds.
        /// </summary>
        public float FadeGreyHoldDurationSec => _fadeGreyHoldDurationMs / 1000f;

        /// <summary>
        /// Duration of fade-in phase in seconds.
        /// </summary>
        public float FadeInDurationSec => _fadeInDurationMs / 1000f;

        /// <summary>
        /// Duration of crossfade transition in seconds.
        /// </summary>
        public float CrossfadeDurationSec => _crossfadeDurationMs / 1000f;

        /// <summary>
        /// Maximum depth of the overlay scene stack.
        /// </summary>
        public int MaxSceneStackDepth => _maxSceneStackDepth;

        /// <summary>
        /// Minimum duration an overlay must remain active before it can be popped (seconds).
        /// </summary>
        public float MinOverlayDurationSec => _minOverlayDurationMs / 1000f;

        /// <summary>
        /// When dialogue choices remaining falls to or below this value, trigger preload.
        /// </summary>
        public int PreloadLookaheadChoices => _preloadLookaheadChoices;

        #endregion
    }
}
