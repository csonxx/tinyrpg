using System;
using Core.Narrative;
using Core.Scene;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Core.Accessibility
{
    /// <summary>
    /// Central coordinator for the Accessibility System.
    ///
    /// Owns:
    /// - Text size scaling via USS custom property "--text-scale" on the root VisualElement
    /// - Colorblind simulation via a fullscreen overlay Canvas with a color matrix shader
    /// - Reduce Motion via TransitionBeganEvent interception, overriding FADE_GREY/FADE_BLACK with CROSSFADE
    ///
    /// Implements S3-3 per design/gdd/accessibility-system.md.
    ///
    /// <para>Usage:</para>
    /// <list type="number">
    ///   <item>Call <see cref="Initialize"/> once from a bootstrapping script (e.g., App bootstrapper).</item>
    ///   <item>Call <see cref="SetRootVisualElement"/> with the UIDocument root element to enable text scaling.</item>
    ///   <item>Set properties directly (e.g., <c>AccessibilitySystem.Instance.TextSize = TextSizeMode.Large</c>).</item>
    /// </list>
    /// </summary>
    public sealed class AccessibilitySystem : MonoBehaviour
    {
        #region Singleton

        private static AccessibilitySystem _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// Global singleton instance. Throws if not yet initialized.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if accessed before Initialize is called.</exception>
        public static AccessibilitySystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            throw new InvalidOperationException(
                                "[AccessibilitySystem] Instance accessed before Initialize was called.");
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Settings Backend

        private IAccessibilitySettingsBackend _backend;

        /// <summary>
        /// The settings backend used for persistence. Defaults to PlayerPrefsAccessibilityBackend.
        /// Can be replaced with a different backend (e.g., S3-1 SettingsSystem) before or after initialization.
        /// </summary>
        public IAccessibilitySettingsBackend Backend
        {
            get => _backend;
            set
            {
                if (value == null) return;
                _backend = value;
                _backend.Load();
                ApplyAllSettings();
            }
        }

        #endregion

        #region USS Text Scale

        private VisualElement _rootVisualElement;
        private TextSizeMode _currentTextSize = TextSizeMode.Normal;

        /// <summary>
        /// Sets the root VisualElement used for USS text scaling.
        /// Call this once after the UIDocument root element is available.
        /// </summary>
        /// <param name="root">The root VisualElement of the active UIDocument.</param>
        public void SetRootVisualElement(VisualElement root)
        {
            _rootVisualElement = root;
            ApplyTextSize();
        }

        #endregion

        #region Colorblind Overlay

        [Header("Colorblind Overlay")]
        [SerializeField] private Canvas _colorblindOverlayCanvas;
        [SerializeField] private RawImage _colorblindOverlayImage;

        private Material _colorblindMaterial;
        private ColorblindMode _currentColorblindMode = ColorblindMode.None;

        #endregion

        #region Properties (mirror backend)

        /// <summary>
        /// Current text size mode. Setting this immediately applies the USS scale.
        /// Persists to backend automatically.
        /// </summary>
        public TextSizeMode TextSize
        {
            get => _currentTextSize;
            set
            {
                _currentTextSize = value;
                _backend.TextSize = value;
                ApplyTextSize();
                _backend.Save();
                OnSettingsChanged?.Invoke();
            }
        }

        /// <summary>
        /// Current colorblind simulation mode. Setting this immediately updates the shader.
        /// Persists to backend automatically.
        /// </summary>
        public ColorblindMode ColorblindMode
        {
            get => _currentColorblindMode;
            set
            {
                _currentColorblindMode = value;
                _backend.ColorblindMode = value;
                ApplyColorblindMode();
                _backend.Save();
                OnSettingsChanged?.Invoke();
            }
        }

        /// <summary>
        /// Whether reduce motion is enabled. Setting this immediately affects future transitions.
        /// Persists to backend automatically.
        /// </summary>
        public bool ReduceMotionEnabled
        {
            get => _backend.ReduceMotionEnabled;
            set
            {
                _backend.ReduceMotionEnabled = value;
                _backend.Save();
                OnSettingsChanged?.Invoke();
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Fired whenever any accessibility setting changes.
        /// Systems that need to react (e.g., re-apply colorblind shader) can subscribe.
        /// </summary>
        public event Action OnSettingsChanged;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            _backend = new PlayerPrefsAccessibilityBackend();
            _backend.Load();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Start()
        {
            ApplyAllSettings();
            SetupColorblindOverlay();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Must be called once from a bootstrapper before any accessibility features are used.
        /// If using the singleton GameObject approach (auto AddComponent), Start() handles this.
        /// </summary>
        public void Initialize()
        {
            _backend = new PlayerPrefsAccessibilityBackend();
            _backend.Load();
            ApplyAllSettings();
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<TransitionBeganEvent>(HandleTransitionBegan);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<TransitionBeganEvent>(HandleTransitionBegan);
        }

        private void SetupColorblindOverlay()
        {
            if (_colorblindOverlayCanvas != null)
            {
                _colorblindOverlayCanvas.gameObject.SetActive(_currentColorblindMode != ColorblindMode.None);
            }
        }

        #endregion

        #region Reduce Motion

        private void HandleTransitionBegan(TransitionBeganEvent evt)
        {
            if (!_backend.ReduceMotionEnabled) return;
            if (evt.TransitionType != TransitionType.FADE_GREY &&
                evt.TransitionType != TransitionType.FADE_BLACK)
                return;

            // Override: SceneManagement will check AccessibilitySystem.ReduceMotionEnabled
            // when selecting the actual transition to use. This event is informational only.
            // The actual override happens in SceneManagement.LoadScene.
        }

        /// <summary>
        /// Returns the effective transition type for a requested transition,
        /// accounting for reduce motion preference.
        /// Called by SceneManagement when selecting which transition to play.
        ///
        /// <para>Example:</para>
        /// <code>var type = AccessibilitySystem.GetEffectiveTransitionType(requestedType);</code>
        /// </summary>
        /// <param name="requested">The transition type that would normally be used.</param>
        /// <returns>The transition type to actually use (may be CROSSFADE if reduce motion is on).</returns>
        public static TransitionType GetEffectiveTransitionType(TransitionType requested)
        {
            if (_instance == null) return requested;
            if (!_instance.ReduceMotionEnabled) return requested;

            return requested switch
            {
                TransitionType.FADE_GREY => TransitionType.CROSSFADE,
                TransitionType.FADE_BLACK => TransitionType.CROSSFADE,
                _ => requested
            };
        }

        #endregion

        #region Apply Settings

        private void ApplyAllSettings()
        {
            _currentTextSize = _backend.TextSize;
            _currentColorblindMode = _backend.ColorblindMode;

            ApplyTextSize();
            ApplyColorblindMode();
        }

        private void ApplyTextSize()
        {
            if (_rootVisualElement == null) return;

            string scaleValue = TextSizeScales.GetScaleValue(_currentTextSize);
            _rootVisualElement.style.setProperty("--text-scale", scaleValue);
        }

        private void ApplyColorblindMode()
        {
            if (_colorblindOverlayCanvas == null) return;

            _colorblindOverlayCanvas.gameObject.SetActive(_currentColorblindMode != ColorblindMode.None);

            if (_colorblindMaterial == null && _colorblindOverlayImage != null)
            {
                // Shader is applied via material on the RawImage
                // The material will be created from the ColorblindColorMatrix shader
                _colorblindMaterial = new Material(Shader.Find("TinyRPG/Accessibility/ColorblindColorMatrix"));
                _colorblindOverlayImage.material = _colorblindMaterial;
            }

            if (_colorblindMaterial != null)
            {
                float[] matrix = ColorblindMatrices.GetMatrix(_currentColorblindMode);
                _colorblindMaterial.SetFloatArray("_ColorMatrix", matrix);
            }
        }

        #endregion

        #region Settings UI Helpers

        /// <summary>
        /// Applies text size scaling to a specific VisualElement's children
        /// by inheriting the --text-scale variable from root.
        /// This is called automatically on the root element; additional elements
        /// can call this if they have a different root hierarchy.
        /// </summary>
        /// <param name="element">The VisualElement to apply scale-aware styling to.</param>
        public static void ApplyTextScaleToElement(VisualElement element)
        {
            if (element == null) return;

            // USS variable inheritance handles propagation automatically.
            // Just ensure the element is a child of the scaled root.
            // No explicit action needed if element is in the visual tree.
        }

        #endregion
    }
}
