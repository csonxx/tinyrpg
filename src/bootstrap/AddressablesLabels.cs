namespace Core.Addressables
{
    /// <summary>
    /// Static class containing all Addressables label strings used for asset loading.
    ///
    /// These labels correspond to Addressables groups in the Unity project.
    /// Art team populates actual assets; this file defines the label contract.
    ///
    /// Usage: Addressables.LoadAssetAsync<Texture>($"{AddressablesLabels.Backgrounds}/scene_name");
    /// </summary>
    public static class AddressablesLabels
    {
        /// <summary>Background images for dialogue scenes (e.g., "Backgrounds/courtyard_day").</summary>
        public const string Backgrounds = "Backgrounds";

        /// <summary>Character portrait images for dialogue speakers (e.g., "Portraits/yamamoto_neutral").</summary>
        public const string Portraits = "Portraits";

        /// <summary>Background music tracks (e.g., "BGM/courtyard_tense").</summary>
        public const string BGM = "BGM";

        /// <summary>Sound effect files (e.g., "SFX/footsteps", "SFX/dialogue_advance").</summary>
        public const string SFX = "SFX";

        /// <summary>Voice acting audio files for dialogue lines (e.g., "Voice/yamamoto_line_001").</summary>
        public const string Voice = "Voice";
    }
}
