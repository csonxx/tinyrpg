using System;
using System.Collections.Generic;
using Core.Scene;
using UnityEngine;

namespace Core.Narrative
{
    /// <summary>
    /// Represents a single scene within a chapter, including its scene ID, transition style,
    /// and optional branching configuration for Rule 5: Branching Path Selection.
    /// </summary>
    [Serializable]
    public sealed class SceneData
    {
        [SerializeField] private string _sceneId;
        [SerializeField] private bool _isMemoirOrFlashback;

        // Branching fields (S3-4)
        [SerializeField] private string _conditionExpression;
        [SerializeField] private List<BranchTarget> _branchTargets;

        public string SceneId => _sceneId;
        public bool IsMemoirOrFlashback => _isMemoirOrFlashback;

        /// <summary>
        /// Returns true if this scene has a branching condition defined.
        /// </summary>
        public bool HasCondition => !string.IsNullOrEmpty(_conditionExpression);

        /// <summary>
        /// The condition expression to evaluate for branch selection.
        /// Example: "trust.imperial >= 50 && clues.foundKey == 1"
        /// </summary>
        public string ConditionExpression => _conditionExpression ?? string.Empty;

        /// <summary>
        /// Gets branch targets as a dictionary for efficient lookup.
        /// Returns null if no branch targets are defined.
        /// </summary>
        public Dictionary<string, string> BranchTargets
        {
            get
            {
                if (_branchTargets == null || _branchTargets.Count == 0)
                    return null;

                var dict = new Dictionary<string, string>();
                foreach (var target in _branchTargets)
                {
                    if (!string.IsNullOrEmpty(target.BranchId) && !string.IsNullOrEmpty(target.SceneId))
                    {
                        dict[target.BranchId] = target.SceneId;
                    }
                }
                return dict.Count > 0 ? dict : null;
            }
        }

        public SceneData(string sceneId, bool isMemoirOrFlashback = false)
        {
            _sceneId = sceneId;
            _isMemoirOrFlashback = isMemoirOrFlashback;
        }

        /// <summary>
        /// Creates a scene with branching configuration.
        /// </summary>
        /// <param name="sceneId">The scene identifier.</param>
        /// <param name="isMemoirOrFlashback">Whether this is a memoir/flashback scene.</param>
        /// <param name="conditionExpression">Optional condition expression for branch selection.</param>
        /// <param name="branchTargets">Optional list of branch targets.</param>
        public SceneData(
            string sceneId,
            bool isMemoirOrFlashback,
            string conditionExpression,
            List<BranchTarget> branchTargets)
        {
            _sceneId = sceneId;
            _isMemoirOrFlashback = isMemoirOrFlashback;
            _conditionExpression = conditionExpression;
            _branchTargets = branchTargets;
        }
    }

    /// <summary>
    /// Represents a single branch target for a branching scene.
    /// </summary>
    [Serializable]
    public sealed class BranchTarget
    {
        [SerializeField] private string _branchId;
        [SerializeField] private string _sceneId;

        public string BranchId => _branchId;
        public string SceneId => _sceneId;

        public BranchTarget() { }

        public BranchTarget(string branchId, string sceneId)
        {
            _branchId = branchId;
            _sceneId = sceneId;
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
