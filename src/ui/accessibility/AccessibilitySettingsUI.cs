using System;
using Core.Accessibility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Accessibility
{
    /// <summary>
    /// Extension to MenuManager's settings panel that adds accessibility controls.
    ///
    /// Adds to the existing SettingsScreen:
    /// - Text Size dropdown (Small / Normal / Large)
    /// - Colorblind Mode dropdown (None / Deuteranopia / Protanopia)
    /// - Reduce Motion toggle
    ///
    /// Reads/writes settings via AccessibilitySystem.
    /// Requires MenuManager to expose the settings panel GameObject and existing settings controls.
    ///
    /// Attach to the same GameObject as MenuManager.
    /// </summary>
    [RequireComponent(typeof(MenuManager))]
    public sealed class AccessibilitySettingsUI : MonoBehaviour
    {
        #region Inspector References (added to MenuManager's Settings panel)

        [Header("Accessibility Controls")]
        [SerializeField] private GameObject _accessibilitySection; // Parent GO for accessibility controls
        [SerializeField] private TMP_Dropdown _textSizeDropdown;
        [SerializeField] private TMP_Dropdown _colorblindModeDropdown;
        [SerializeField] private Toggle _reduceMotionToggle;

        [Header("Accessibility Section Layout")]
        [SerializeField] private float _sectionSpacing = 16f;
        [SerializeField] private float _labelWidth = 160f;

        #endregion

        #region Private Fields

        private MenuManager _menuManager;
        private bool _isInitialized;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _menuManager = GetComponent<MenuManager>();
        }

        private void OnEnable()
        {
            if (AccessibilitySystem.Instance != null)
            {
                AccessibilitySystem.Instance.OnSettingsChanged += RefreshUIFromSettings;
            }
        }

        private void OnDisable()
        {
            if (AccessibilitySystem.Instance != null)
            {
                AccessibilitySystem.Instance.OnSettingsChanged -= RefreshUIFromSettings;
            }
        }

        private void Start()
        {
            InitializeAccessibilityControls();
        }

        #endregion

        #region Initialization

        private void InitializeAccessibilityControls()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            SetupTextSizeDropdown();
            SetupColorblindModeDropdown();
            SetupReduceMotionToggle();

            // Set default values from current system state
            RefreshUIFromSettings();
        }

        private void SetupTextSizeDropdown()
        {
            if (_textSizeDropdown == null) return;

            _textSizeDropdown.ClearOptions();
            _textSizeDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "Small",
                "Normal",
                "Large"
            });

            _textSizeDropdown.onValueChanged.RemoveAllListeners();
            _textSizeDropdown.onValueChanged.AddListener(OnTextSizeChanged);
        }

        private void SetupColorblindModeDropdown()
        {
            if (_colorblindModeDropdown == null) return;

            _colorblindModeDropdown.ClearOptions();
            _colorblindModeDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "None",
                "Deuteranopia (Green-Blind)",
                "Protanopia (Red-Blind)"
            });

            _colorblindModeDropdown.onValueChanged.RemoveAllListeners();
            _colorblindModeDropdown.onValueChanged.AddListener(OnColorblindModeChanged);
        }

        private void SetupReduceMotionToggle()
        {
            if (_reduceMotionToggle == null) return;

            _reduceMotionToggle.onValueChanged.RemoveAllListeners();
            _reduceMotionToggle.onValueChanged.AddListener(OnReduceMotionChanged);
        }

        #endregion

        #region Event Handlers

        private void OnTextSizeChanged(int index)
        {
            if (AccessibilitySystem.Instance == null) return;

            // Index matches TextSizeMode enum ordinal
            AccessibilitySystem.Instance.TextSize = (TextSizeMode)index;
        }

        private void OnColorblindModeChanged(int index)
        {
            if (AccessibilitySystem.Instance == null) return;

            AccessibilitySystem.Instance.ColorblindMode = (ColorblindMode)index;
        }

        private void OnReduceMotionChanged(bool value)
        {
            if (AccessibilitySystem.Instance == null) return;

            AccessibilitySystem.Instance.ReduceMotionEnabled = value;
        }

        #endregion

        #region UI Refresh

        /// <summary>
        /// Called when settings change externally (e.g., loaded from save, reset to defaults).
        /// Refreshes all UI controls to match current settings.
        /// </summary>
        private void RefreshUIFromSettings()
        {
            if (AccessibilitySystem.Instance == null) return;
            if (_textSizeDropdown == null || _colorblindModeDropdown == null || _reduceMotionToggle == null) return;

            // Prevent recursive events
            _textSizeDropdown.onValueChanged.RemoveAllListeners();
            _colorblindModeDropdown.onValueChanged.RemoveAllListeners();
            _reduceMotionToggle.onValueChanged.RemoveAllListeners();

            _textSizeDropdown.value = (int)AccessibilitySystem.Instance.TextSize;
            _colorblindModeDropdown.value = (int)AccessibilitySystem.Instance.ColorblindMode;
            _reduceMotionToggle.isOn = AccessibilitySystem.Instance.ReduceMotionEnabled;

            // Re-attach listeners
            _textSizeDropdown.onValueChanged.AddListener(OnTextSizeChanged);
            _colorblindModeDropdown.onValueChanged.AddListener(OnColorblindModeChanged);
            _reduceMotionToggle.onValueChanged.AddListener(OnReduceMotionChanged);
        }

        #endregion
    }
}
