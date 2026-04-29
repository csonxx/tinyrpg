namespace Core.Accessibility
{
    /// <summary>
    /// Text size scaling modes for accessibility.
    /// Each mode specifies a multiplier applied to the base font size.
    /// </summary>
    public enum TextSizeMode
    {
        Small = 0,
        Normal = 1,
        Large = 2
    }

    /// <summary>
    /// Provides scale multipliers for text size modes.
    /// </summary>
    public static class TextSizeScales
    {
        /// <summary>
        /// Returns the USS custom property scale value for the given text size mode.
        /// Values are strings suitable for USS custom property values (e.g., "0.8").
        /// </summary>
        public static string GetScaleValue(TextSizeMode mode)
        {
            return mode switch
            {
                TextSizeMode.Small => "0.8",
                TextSizeMode.Normal => "1.0",
                TextSizeMode.Large => "1.4",
                _ => "1.0"
            };
        }

        /// <summary>
        /// Returns the numeric scale multiplier for the given text size mode.
        /// </summary>
        public static float GetScaleMultiplier(TextSizeMode mode)
        {
            return mode switch
            {
                TextSizeMode.Small => 0.8f,
                TextSizeMode.Normal => 1.0f,
                TextSizeMode.Large => 1.4f,
                _ => 1.0f
            };
        }
    }
}
