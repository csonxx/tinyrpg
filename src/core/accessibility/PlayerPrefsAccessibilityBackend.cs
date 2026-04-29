using UnityEngine;

namespace Core.Accessibility
{
    /// <summary>
    /// PlayerPrefs-based backend for accessibility settings.
    /// Used as the default/temporary implementation until a proper Settings System (S3-1) exists.
    ///
    /// Keys follow the convention "Accessibility_[SettingName]".
    /// </summary>
    public sealed class PlayerPrefsAccessibilityBackend : IAccessibilitySettingsBackend
    {
        #region Constants

        private const string KEY_TEXT_SIZE = "Accessibility_TextSize";
        private const string KEY_COLORBLIND_MODE = "Accessibility_ColorblindMode";
        private const string KEY_REDUCE_MOTION = "Accessibility_ReduceMotion";

        #endregion

        #region IAccessibilitySettingsBackend

        public TextSizeMode TextSize
        {
            get => (TextSizeMode)PlayerPrefs.GetInt(KEY_TEXT_SIZE, (int)TextSizeMode.Normal);
            set => PlayerPrefs.SetInt(KEY_TEXT_SIZE, (int)value);
        }

        public ColorblindMode ColorblindMode
        {
            get => (ColorblindMode)PlayerPrefs.GetInt(KEY_COLORBLIND_MODE, (int)ColorblindMode.None);
            set => PlayerPrefs.SetInt(KEY_COLORBLIND_MODE, (int)value);
        }

        public bool ReduceMotionEnabled
        {
            get => PlayerPrefs.GetInt(KEY_REDUCE_MOTION, 0) == 1;
            set => PlayerPrefs.SetInt(KEY_REDUCE_MOTION, value ? 1 : 0);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }

        public void Load()
        {
            // PlayerPrefs.Load() is called automatically by Unity when needed.
            // No-op here, but keeping for interface consistency.
        }

        #endregion
    }
}
