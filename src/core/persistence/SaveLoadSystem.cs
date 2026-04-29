using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Core.Narrative;
using UnityEngine;

namespace Core.Persistence
{
    /// <summary>
    /// Main save/load service. Manages save slots, autosave triggers, and integrity verification.
    /// Implements a singleton MonoBehaviour attached to a persistent scene object.
    /// </summary>
    public sealed class SaveLoadSystem : MonoBehaviour
    {
        private const string SAVE_DIRECTORY = "saves";
        private const string SAVE_FILE_PREFIX = "save_";
        private const string AUTOSAVE_SLOT = "autosave";
        private const int SLOT_COUNT = 3;

        /// <summary>
        /// Raised when a save operation completes successfully.
        /// </summary>
        public event Action<int> OnSaveComplete;

        /// <summary>
        /// Raised when a load operation completes successfully.
        /// </summary>
        public event Action<int, SaveFile> OnLoadComplete;

        /// <summary>
        /// Raised when a load operation fails (corrupt file, hash mismatch, etc.).
        /// Passes the slot index and error message.
        /// </summary>
        public event Action<int, string> OnLoadFailed;

        /// <summary>
        /// Raised when an autosave is triggered.
        /// </summary>
        public event Action OnAutosaveTriggered;

        /// <summary>
        /// Metadata about a save slot, derived from the save file on disk.
        /// </summary>
        public struct SlotInfo
        {
            public bool Exists { get; set; }
            public int ChapterIndex { get; set; }
            public string SceneId { get; set; }
            public DateTime Timestamp { get; set; }
            public int PlayTimeSeconds { get; set; }
            public int ChoiceCount { get; set; }
            public string FormattedPlayTime => TimeSpan.FromSeconds(PlayTimeSeconds).ToString(@"hh\:mm\:ss");
        }

        // Singleton instance
        private static SaveLoadSystem _instance;
        public static SaveLoadSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException(
                        "SaveLoadSystem has not been initialized. " +
                        "Ensure it exists in the scene or call Awake() to create a default instance.");
                }
                return _instance;
            }
        }

        // Internal state
        private float _sessionStartTime;
        private int _sessionChoiceCount;
        private bool _isPlayTimeTracking;
        private bool _isPaused;

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureSaveDirectoryExists();
            SubscribeToNsmEvents();
            ResumePlayTimeTracking();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                UnsubscribeFromNsmEvents();
                _instance = null;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            _isPaused = pauseStatus;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Saves the current NSM state to the specified slot (0-2).
        /// </summary>
        /// <param name="slot">Slot index from 0 to 2</param>
        /// <exception cref="ArgumentOutOfRangeException">If slot is invalid</exception>
        public void Save(int slot)
        {
            ValidateSlot(slot);
            string path = GetSavePath(slot);
            InternalSave(path, slot);
        }

        /// <summary>
        /// Loads NSM state from the specified slot (0-2).
        /// </summary>
        /// <param name="slot">Slot index from 0 to 2</param>
        /// <exception cref="ArgumentOutOfRangeException">If slot is invalid</exception>
        /// <exception cref="FileNotFoundException">If save file does not exist</exception>
        public void Load(int slot)
        {
            ValidateSlot(slot);
            string path = GetSavePath(slot);
            InternalLoad(path, slot);
        }

        /// <summary>
        /// Saves to the autosave slot. Overwrites any existing autosave.
        /// </summary>
        public void SaveAutosave()
        {
            string path = GetSavePath(AUTOSAVE_SLOT);
            InternalSave(path, -1);
            OnAutosaveTriggered?.Invoke();
        }

        /// <summary>
        /// Loads from the autosave slot.
        /// </summary>
        /// <exception cref="FileNotFoundException">If autosave does not exist</exception>
        public void LoadAutosave()
        {
            string path = GetSavePath(AUTOSAVE_SLOT);
            InternalLoad(path, -1);
        }

        /// <summary>
        /// Returns metadata for the given slot. Does not load the full save.
        /// Returns a SlotInfo with Exists=false if the slot is empty.
        /// </summary>
        /// <param name="slot">Slot index 0-2, or -1 for autosave</param>
        /// <returns>SlotInfo struct with metadata</returns>
        public SlotInfo GetSlotInfo(int slot)
        {
            string path = GetSavePath(slot);

            if (!File.Exists(path))
            {
                return new SlotInfo { Exists = false };
            }

            try
            {
                string json = File.ReadAllText(path);
                var saveFile = SaveFile.FromJson(json);

                return new SlotInfo
                {
                    Exists = true,
                    ChapterIndex = saveFile.ChapterIndex,
                    SceneId = saveFile.SceneId,
                    Timestamp = saveFile.TimestampAsDateTime(),
                    PlayTimeSeconds = saveFile.PlayTimeSeconds,
                    ChoiceCount = saveFile.ChoiceCount
                };
            }
            catch
            {
                return new SlotInfo { Exists = false };
            }
        }

        /// <summary>
        /// Called by the dialogue system when dialogue node 0 completes
        /// and the state returns to SCENE_ACTIVE, triggering an autosave.
        /// </summary>
        public void RequestAutosaveForDialogueNode0()
        {
            SaveAutosave();
        }

        /// <summary>
        /// Call when a choice is made by the player to track choice count.
        /// </summary>
        public void RecordChoice()
        {
            _sessionChoiceCount++;
        }

        /// <summary>
        /// Pauses play time tracking (e.g., when entering a menu).
        /// </summary>
        public void PausePlayTimeTracking()
        {
            if (_isPlayTimeTracking && !_isPaused)
            {
                _sessionStartTime += Time.time;
            }
            _isPlayTimeTracking = false;
        }

        /// <summary>
        /// Resumes play time tracking (e.g., when returning to gameplay).
        /// </summary>
        public void ResumePlayTimeTracking()
        {
            if (!_isPlayTimeTracking)
            {
                _sessionStartTime = -Time.time;
                _isPlayTimeTracking = true;
            }
        }

        /// <summary>
        /// Returns the number of available manual save slots.
        /// </summary>
        public int SlotCount => SLOT_COUNT;

        #endregion

        #region Private Methods

        private void InternalSave(string path, int slot)
        {
            PausePlayTimeTracking();

            try
            {
                var nsmState = NarrativeStateMachine.Instance.Serialize();
                string nsmJson = nsmState.ToJson();
                string hash = ComputeHash(nsmJson);

                // Read chapterIndex and sceneId from NSM state
                int chapterIndex = GetIntFromNsmState(nsmState, "chapterIndex");
                string sceneId = GetStringFromNsmState(nsmState, "sceneId") ?? string.Empty;

                var saveFile = new SaveFile
                {
                    ChapterIndex = chapterIndex,
                    SceneId = sceneId,
                    NsmState = nsmJson,
                    NsmHash = hash,
                    PlayTimeSeconds = GetSessionPlayTimeSeconds(),
                    ChoiceCount = _sessionChoiceCount
                };

                string json = saveFile.ToJson();

                // Ensure directory exists
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(path, json);
                OnSaveComplete?.Invoke(slot);
            }
            finally
            {
                ResumePlayTimeTracking();
            }
        }

        private void InternalLoad(string path, int slot)
        {
            if (!File.Exists(path))
            {
                string msg = $"Save file not found at {path}";
                OnLoadFailed?.Invoke(slot, msg);
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                var saveFile = SaveFile.FromJson(json);

                // Integrity check: recompute hash
                string computedHash = ComputeHash(saveFile.NsmState);
                if (!string.Equals(computedHash, saveFile.NsmHash, StringComparison.OrdinalIgnoreCase))
                {
                    OnLoadFailed?.Invoke(slot, "Save file is corrupted (hash mismatch).");
                    return;
                }

                // Restore NSM state
                NarrativeStateMachine.Instance.Deserialize(saveFile.NsmState);

                // Restore session state
                _sessionChoiceCount = saveFile.ChoiceCount;
                _sessionStartTime = -Time.time;
                _isPlayTimeTracking = true;

                OnLoadComplete?.Invoke(slot, saveFile);
            }
            catch (Exception ex)
            {
                OnLoadFailed?.Invoke(slot, $"Failed to load save: {ex.Message}");
            }
        }

        private string ComputeHash(string input)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private string GetSavePath(int slot)
        {
            string basePath = Application.persistentDataPath;
            string slotName = slot == -1 ? AUTOSAVE_SLOT : slot.ToString();
            return Path.Combine(basePath, SAVE_DIRECTORY, $"{SAVE_FILE_PREFIX}{slotName}.json");
        }

        private void ValidateSlot(int slot)
        {
            if (slot < 0 || slot >= SLOT_COUNT)
            {
                throw new ArgumentOutOfRangeException(nameof(slot),
                    $"Slot must be between 0 and {SLOT_COUNT - 1}, or -1 for autosave.");
            }
        }

        private void EnsureSaveDirectoryExists()
        {
            string directory = Path.Combine(Application.persistentDataPath, SAVE_DIRECTORY);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private int GetSessionPlayTimeSeconds()
        {
            return Mathf.FloorToInt(GetSessionPlayTimeSecondsFloat());
        }

        private float GetSessionPlayTimeSecondsFloat()
        {
            if (_isPlayTimeTracking)
            {
                return _sessionStartTime + Time.time;
            }
            return _sessionStartTime;
        }

        private static int GetIntFromNsmState(object nsmState, string key)
        {
            // NSM serialization returns a dictionary-like structure
            // We use reflection or a getter to extract values
            // The NSM.Serialize() returns NarrativeStateData which has an indexer
            if (nsmState is System.Collections.IDictionary dict && dict.Contains(key))
            {
                var value = dict[key];
                if (value is int intVal) return intVal;
                if (value is long longVal) return (int)longVal;
                if (value is float floatVal) return (int)floatVal;
                if (value is double doubleVal) return (int)doubleVal;
                if (value is string strVal && int.TryParse(strVal, out int parsed)) return parsed;
            }
            return 0;
        }

        private static string GetStringFromNsmState(object nsmState, string key)
        {
            if (nsmState is System.Collections.IDictionary dict && dict.Contains(key))
            {
                return dict[key]?.ToString();
            }
            return null;
        }

        #endregion

        #region NSM Event Subscriptions

        private void SubscribeToNsmEvents()
        {
            EventBus.Instance.Subscribe("nsm.state.*", HandleNsmStateChanged);
            EventBus.Instance.Subscribe("trust.boundary", HandleTrustBoundaryReached);
        }

        private void UnsubscribeFromNsmEvents()
        {
            EventBus.Instance.Unsubscribe("nsm.state.*", HandleNsmStateChanged);
            EventBus.Instance.Unsubscribe("trust.boundary", HandleTrustBoundaryReached);
        }

        private void HandleNsmStateChanged(NSMEvent e)
        {
            if (e is StateChangedEvent stateChanged)
            {
                // CHAPTER_COMPLETE is a state the NSM enters when a chapter ends
                // The state enum value itself is the indicator
                string stateName = stateChanged.NewState?.ToString() ?? string.Empty;
                if (stateName.Contains("CHAPTER_COMPLETE", StringComparison.OrdinalIgnoreCase))
                {
                    SaveAutosave();
                }
            }
        }

        private void HandleTrustBoundaryReached(NSMEvent e)
        {
            if (e is TrustBoundaryReachedEvent)
            {
                SaveAutosave();
            }
        }

        #endregion
    }
}
