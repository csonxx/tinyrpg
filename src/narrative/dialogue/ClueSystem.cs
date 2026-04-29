using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Narrative.Dialogue
{
    /// <summary>
    /// Categories for organizing clues in the Intelligence Journal.
    /// </summary>
    public static class ClueCategory
    {
        public const string Documents = "documents";
        public const string Conversations = "conversations";
        public const string Evidence = "evidence";
        public const string Uncategorized = "";
    }

    /// <summary>
    /// Static utility class for registering and querying clues discovered by the player.
    ///
    /// Clues are stored as boolean flags in NSM under keys "clues.{clueId}" (value 1.0 = discovered).
    /// A parallel list of discovered clue metadata is maintained in NSM for journal retrieval.
    ///
    /// Clue registration triggers a ClueRegisteredEvent on the NSM event bus.
    /// </summary>
    public static class ClueSystem
    {
        #region Constants

        private const string KEY_PREFIX = "clues.";
        private const string KEY_DISCOVERED_LIST = "clues.discoveredList";

        #endregion

        #region Events

        /// <summary>
        /// Event emitted when a new clue is registered.
        /// </summary>
        public sealed class ClueRegisteredEvent : NSMEvent
        {
            public const string KEY = "clue.registered";
            public string ClueId { get; }
            public string Category { get; }

            public ClueRegisteredEvent(string clueId, string category)
            {
                ClueId = clueId;
                Category = category;
            }

            public override string Key => KEY;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Registers a clue as discovered in NSM.
        /// Idempotent — registering an already-discovered clue is a no-op.
        /// </summary>
        /// <param name="clueId">Unique identifier for the clue.</param>
        /// <param name="category">Optional category string (e.g. ClueCategory.Documents).</param>
        public static void RegisterClue(string clueId, string category = null)
        {
            if (string.IsNullOrEmpty(clueId))
                return;

            var nsm = NarrativeStateMachine.Instance;
            string key = KEY_PREFIX + clueId;

            // Already discovered — skip
            if (nsm.Get<float>(key) == 1.0f)
                return;

            // Set the boolean flag (stored as float 1.0)
            nsm.Set(key, (float)1.0f);

            // Append to discovered list for journal retrieval
            var clueMeta = new ClueMetadata { Id = clueId, Category = category ?? ClueCategory.Uncategorized };
            AddToDiscoveredList(nsm, clueMeta);

            // Emit event
            nsm.EventBus.Emit(new ClueRegisteredEvent(clueId, category));
        }

        /// <summary>
        /// Returns true if the specified clue has been discovered.
        /// </summary>
        /// <param name="clueId">The clue ID to check.</param>
        public static bool IsClueDiscovered(string clueId)
        {
            if (string.IsNullOrEmpty(clueId))
                return false;

            var nsm = NarrativeStateMachine.Instance;
            return nsm.Get<float>(KEY_PREFIX + clueId) == 1.0f;
        }

        /// <summary>
        /// Returns all discovered clues, grouped by category.
        /// </summary>
        public static Dictionary<string, List<ClueMetadata>> GetDiscoveredCluesByCategory()
        {
            var nsm = NarrativeStateMachine.Instance;
            var list = GetDiscoveredList(nsm);
            var grouped = new Dictionary<string, List<ClueMetadata>>
            {
                { ClueCategory.Documents, new List<ClueMetadata>() },
                { ClueCategory.Conversations, new List<ClueMetadata>() },
                { ClueCategory.Evidence, new List<ClueMetadata>() },
                { ClueCategory.Uncategorized, new List<ClueMetadata>() }
            };

            foreach (var meta in list)
            {
                string cat = string.IsNullOrEmpty(meta.Category) ? ClueCategory.Uncategorized : meta.Category;
                if (!grouped.ContainsKey(cat))
                    grouped[cat] = new List<ClueMetadata>();
                grouped[cat].Add(meta);
            }

            return grouped;
        }

        /// <summary>
        /// Returns all discovered clue metadata entries.
        /// </summary>
        public static List<ClueMetadata> GetAllDiscoveredClues()
        {
            return GetDiscoveredList(NarrativeStateMachine.Instance);
        }

        /// <summary>
        /// Returns the total count of discovered clues.
        /// </summary>
        public static int GetDiscoveredCount()
        {
            return GetDiscoveredList(NarrativeStateMachine.Instance).Count;
        }

        #endregion

        #region Private Helpers

        private static List<ClueMetadata> GetDiscoveredList(NarrativeStateMachine nsm)
        {
            string json = nsm.Get<string>(KEY_DISCOVERED_LIST);
            if (string.IsNullOrEmpty(json))
                return new List<ClueMetadata>();

            try
            {
                var wrapper = JsonUtility.FromJson<ClueListWrapper>("{\"clues\":" + json + "}");
                return wrapper?.Clues ?? new List<ClueMetadata>();
            }
            catch
            {
                return new List<ClueMetadata>();
            }
        }

        private static void AddToDiscoveredList(NarrativeStateMachine nsm, ClueMetadata meta)
        {
            var list = GetDiscoveredList(nsm);
            list.Add(meta);
            string json = JsonUtility.ToJson(new ClueListWrapper { Clues = list });
            // Strip the wrapper wrapper since we store just the array
            try
            {
                var wrapper = JsonUtility.FromJson<ClueListWrapper>(json);
                string arrayJson = JsonUtility.ToJson(wrapper.Clues);
                nsm.Set(KEY_DISCOVERED_LIST, arrayJson);
            }
            catch
            {
                // Fallback: store empty
                nsm.Set(KEY_DISCOVERED_LIST, "[]");
            }
        }

        #endregion

        #region Serialization Types

        /// <summary>
        /// Metadata for a single discovered clue.
        /// </summary>
        [Serializable]
        public sealed class ClueMetadata
        {
            [SerializeField] private string _id;
            [SerializeField] private string _category;

            public string Id => _id;
            public string Category => _category;

            public ClueMetadata() { }

            public ClueMetadata(string id, string category)
            {
                _id = id;
                _category = category ?? ClueCategory.Uncategorized;
            }
        }

        [Serializable]
        private sealed class ClueListWrapper
        {
            [SerializeField] private List<ClueMetadata> _clues;

            public List<ClueMetadata> Clues => _clues;

            public ClueListWrapper() { }

            public ClueListWrapper(List<ClueMetadata> clues)
            {
                _clues = clues;
            }
        }

        #endregion
    }
}