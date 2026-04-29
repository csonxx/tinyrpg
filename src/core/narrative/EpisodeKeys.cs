namespace Core.Narrative
{
    /// <summary>
    /// NSM key constants used by the Episode Structure system.
    /// These keys persist episode/chapter state for save/resume functionality.
    /// </summary>
    public static class EpisodeKeys
    {
        /// <summary>
        /// NSM key storing the currently active episode ID.
        /// </summary>
        public const string CurrentEpisode = "episode.current";

        /// <summary>
        /// NSM key storing whether the current episode is complete ("true"/"false").
        /// </summary>
        public const string EpisodeComplete = "episode.complete";

        /// <summary>
        /// NSM key storing the currently active chapter index (string representation of int).
        /// </summary>
        public const string CurrentChapter = "chapter.current";
    }
}
