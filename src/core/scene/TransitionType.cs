namespace Core.Scene
{
    /// <summary>
    /// Defines the visual transition type when loading a new scene.
    /// </summary>
    public enum TransitionType
    {
        /// <summary>
        /// Grey fade: 400ms fade to grey, 100ms hold, 400ms fade in (900ms total).
        /// Default transition for chapter/scene changes.
        /// </summary>
        FADE_GREY,

        /// <summary>
        /// Full black fade: fade to black and back.
        /// Used for dramatic scene transitions.
        /// </summary>
        FADE_BLACK,

        /// <summary>
        /// Crossfade: blend directly between scenes without a fade overlay.
        /// Used for fast or subtle scene changes.
        /// </summary>
        CROSSFADE
    }
}
