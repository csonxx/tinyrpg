namespace Core.Narrative
{
    /// <summary>
    /// Event key constants for the Episode Structure system.
    /// All episode-related events use these keys with the EventBus.
    /// </summary>
    public static class EpisodeEvents
    {
        /// <summary>
        /// Key for EpisodeStartedEvent.
        /// Payload: string episodeId.
        /// </summary>
        public const string EpisodeStartedKey = "episode.started";

        /// <summary>
        /// Key for ChapterCompleteEvent.
        /// Payload: string episodeId, int chapterIndex.
        /// </summary>
        public const string ChapterCompleteKey = "episode.chapter_complete";

        /// <summary>
        /// Key for EpisodeCompleteEvent.
        /// Payload: string episodeId, bool isFinalEpisode.
        /// </summary>
        public const string EpisodeCompleteKey = "episode.complete";
    }
}
