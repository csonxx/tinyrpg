using System;
using System.Collections.Generic;
using Core.Scene;
using UnityEngine;

namespace Core.Narrative
{
    /// <summary>
    /// Represents a single scene within a chapter, including its scene ID and transition style.
    /// </summary>
    [Serializable]
    public sealed class SceneData
    {
        [SerializeField] private string _sceneId;
        [SerializeField] private bool _isMemoirOrFlashback;

        public string SceneId => _sceneId;
        public bool IsMemoirOrFlashback => _isMemoirOrFlashback;

        public SceneData(string sceneId, bool isMemoirOrFlashback = false)
        {
            _sceneId = sceneId;
            _isMemoirOrFlashback = isMemoirOrFlashback;
        }
    }

    /// <summary>
    /// Represents a single chapter within an episode, containing an ordered list of scenes.
    /// </summary>
    [Serializable]
    public sealed class ChapterData
    {
        [SerializeField] private int _chapterIndex;
        [SerializeField] private List<SceneData> _scenes;
        [SerializeField] private string _forceTransitionOverride;

        public int ChapterIndex => _chapterIndex;
        public IReadOnlyList<SceneData> Scenes => _scenes;
        public string ForceTransitionOverride => _forceTransitionOverride;

        public ChapterData(int chapterIndex, List<SceneData> scenes, string forceTransitionOverride = null)
        {
            _chapterIndex = chapterIndex;
            _scenes = scenes;
            _forceTransitionOverride = forceTransitionOverride;
        }
    }

    /// <summary>
    /// Represents an episode containing ordered chapters and scenes.
    /// </summary>
    [Serializable]
    public sealed class EpisodeData
    {
        [SerializeField] private string _episodeId;
        [SerializeField] private List<ChapterData> _chapters;
        [SerializeField] private bool _isLastEpisode;

        public string EpisodeId => _episodeId;
        public IReadOnlyList<ChapterData> Chapters => _chapters;
        public bool IsLastEpisode => _isLastEpisode;

        public EpisodeData(string episodeId, List<ChapterData> chapters, bool isLastEpisode = false)
        {
            _episodeId = episodeId;
            _chapters = chapters;
            _isLastEpisode = isLastEpisode;
        }
    }

    /// <summary>
    /// Runtime state of the Episode Structure system.
    /// </summary>
    public enum EpisodeState
    {
        /// <summary>
        /// No episode is currently active or loaded.
        /// </summary>
        EpisodeIdle,

        /// <summary>
        /// Episode data is loaded; waiting for scene to initialize.
        /// </summary>
        EpisodeLoading,

        /// <summary>
        /// A chapter is actively running (dialogue in progress).
        /// </summary>
        ChapterActive,

        /// <summary>
        /// Transitioning between scenes or chapters.
        /// </summary>
        SceneTransitioning,

        /// <summary>
        /// Current chapter has finished all scenes.
        /// </summary>
        ChapterComplete,

        /// <summary>
        /// Episode has finished all chapters.
        /// </summary>
        EpisodeComplete
    }

}
