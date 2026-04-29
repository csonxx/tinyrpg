namespace Core.Accessibility
{
    /// <summary>
    /// Interface for accessibility settings persistence backends.
    /// Allows different implementations (PlayerPrefs, ScriptableObject, remote config)
    /// to be swapped without changing the rest of the system.
    ///
    /// All properties are explicit implementation — call the interface members directly.
    /// </summary>
    public interface IAccessibilitySettingsBackend
    {
        /// <summary>
        /// Current text size mode.
        /// </summary>
        TextSizeMode TextSize { get; set; }

        /// <summary>
        /// Current colorblind simulation mode.
        /// </summary>
        ColorblindMode ColorblindMode { get; set; }

        /// <summary>
        /// Whether reduce motion is enabled.
        /// When true, scene transitions use CROSSFADE instead of FADE_GREY/FADE_BLACK.
        /// </summary>
        bool ReduceMotionEnabled { get; set; }

        /// <summary>
        /// Persists current settings to the underlying storage.
        /// Called automatically by AccessibilitySystem on change; can be called manually.
        /// </summary>
        void Save();

        /// <summary>
        /// Loads settings from underlying storage and applies to the backend.
        /// Called automatically by AccessibilitySystem on startup.
        /// </summary>
        void Load();
    }
}
