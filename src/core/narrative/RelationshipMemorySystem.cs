using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core.Narrative
{
    /// <summary>
    /// Manages per-character relationship values and memory flags.
    ///
    /// Stores state in NSM:
    /// - relationships.{characterId} = float (0-100 relationship value)
    /// - relationships.{characterId}.{flagName} = bool (memory flag)
    /// - relationships.{characterId}.lastInteraction = float (Time.time of last interaction)
    ///
    /// Applies shifts from dialogue choices and performs passive decay after grace period.
    ///
    /// Implements S2-3: Relationship Memory.
    /// </summary>
    public sealed class RelationshipMemorySystem : MonoBehaviour
    {
        #region Singleton

        private static RelationshipMemorySystem _instance;
        private static readonly object _lock = new object();

        public static RelationshipMemorySystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            var go = new GameObject("RelationshipMemorySystem");
                            _instance = go.AddComponent<RelationshipMemorySystem>();
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Constants

        /// <summary>
        /// NSM key prefix for all relationship data.
        /// </summary>
        public const string KEY_PREFIX = "relationships";

        /// <summary>
        /// NSM key suffix for memory flags.
        /// </summary>
        public const string FLAG_SUFFIX = "flag";

        /// <summary>
        /// NSM key suffix for last interaction time.
        /// </summary>
        public const string LAST_INTERACTION_SUFFIX = "lastInteraction";

        #endregion

        #region SerializeField

        [Header("Configuration")]

        [Tooltip("Configuration asset for relationship decay and limits.")]
        [SerializeField]
        private RelationshipMemoryConfig _config;

        [Tooltip("If true, decay coroutine starts automatically on Initialize. Set false for testing.")]
        [SerializeField]
        private bool _autoStartDecay = true;

        #endregion

        #region Private Fields

        private NarrativeStateMachine _nsm;
        private Dictionary<string, float> _lastInteractionTimes;
        private Coroutine _decayCoroutine;
        private bool _isInitialized;
        private bool _decayStarted;

        #endregion

        #region NSM Key Helpers

        /// <summary>
        /// Returns the NSM key for a character's relationship value.
        /// </summary>
        public static string RelationshipKey(string characterId)
        {
            return $"{KEY_PREFIX}.{characterId}";
        }

        /// <summary>
        /// Returns the NSM key for a character's memory flag.
        /// </summary>
        public static string MemoryFlagKey(string characterId, string flagName)
        {
            return $"{KEY_PREFIX}.{characterId}.{flagName}";
        }

        /// <summary>
        /// Returns the NSM key for a character's last interaction time.
        /// </summary>
        public static string LastInteractionKey(string characterId)
        {
            return $"{KEY_PREFIX}.{characterId}.{LAST_INTERACTION_SUFFIX}";
        }

        #endregion

        #region Properties

        /// <summary>
        /// The configuration asset.
        /// </summary>
        public RelationshipMemoryConfig Config => _config;

        /// <summary>
        /// Whether the decay coroutine is currently running.
        /// </summary>
        public bool IsDecayActive => _decayCoroutine != null;

        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            _lastInteractionTimes = new Dictionary<string, float>();
        }

        private void OnDestroy()
        {
            StopDecayCoroutine();
            UnsubscribeFromDialogueEvents();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the system with the given NSM instance.
        /// Sets default values for any new characters.
        /// </summary>
        public void Initialize(NarrativeStateMachine nsm)
        {
            if (nsm == null)
                throw new ArgumentNullException(nameof(nsm));

            _nsm = nsm;

            SubscribeToDialogueEvents();

            if (_autoStartDecay)
            {
                StartDecayCoroutine();
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Initializes with a custom config (useful for testing).
        /// </summary>
        public void Initialize(NarrativeStateMachine nsm, RelationshipMemoryConfig config)
        {
            _config = config;
            Initialize(nsm);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets the current relationship value for a character.
        /// Returns the default value if no relationship exists.
        /// </summary>
        public float GetRelationshipValue(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                return 0f;

            string key = RelationshipKey(characterId);
            if (_nsm.TryGet<float>(key, out var value))
                return value;

            return _config != null ? _config.DefaultRelationshipValue : 50f;
        }

        /// <summary>
        /// Gets a memory flag for a character.
        /// Returns false if the flag does not exist.
        /// </summary>
        public bool GetMemoryFlag(string characterId, string flagName)
        {
            if (string.IsNullOrEmpty(characterId) || string.IsNullOrEmpty(flagName))
                return false;

            string key = MemoryFlagKey(characterId, flagName);
            return _nsm.TryGet<bool>(key, out var value) && value;
        }

        /// <summary>
        /// Sets a memory flag for a character.
        /// </summary>
        public void SetMemoryFlag(string characterId, string flagName, bool value)
        {
            if (string.IsNullOrEmpty(characterId) || string.IsNullOrEmpty(flagName))
                return;

            string key = MemoryFlagKey(characterId, flagName);
            bool oldValue = _nsm.TryGet<bool>(key, out var ov) && ov;

            _nsm.Set(key, value);

            if (oldValue != value)
            {
                _nsm.EventBus.Emit(new MemoryFlagChangedEvent(characterId, flagName, value));
            }
        }

        /// <summary>
        /// Applies a relationship shift to one or more characters.
        /// Clamps values to 0-100 range.
        /// </summary>
        public void ApplyShift(RelationshipShift shift)
        {
            if (shift.IsEmpty || _nsm == null)
                return;

            // Emit shift applied event
            _nsm.EventBus.Emit(new RelationshipShiftAppliedEvent(shift));

            // Apply each character's delta
            foreach (var kvp in shift.Shifts)
            {
                string characterId = kvp.Key;
                float rawDelta = kvp.Value;
                float clampedDelta = _config != null ? _config.ClampShift(rawDelta) : rawDelta;

                ApplySingleShift(characterId, clampedDelta);
            }
        }

        /// <summary>
        /// Forces a single decay tick for all characters with active relationships.
        /// Used for testing and manual decay triggers.
        /// </summary>
        public void ForceDecayTick()
        {
            if (_nsm == null)
                return;

            // Find all relationship keys
            var allKeys = _nsm.GetAllKeys();
            var relationshipKeys = allKeys.Where(k => k.StartsWith(KEY_PREFIX + ".") && !k.Contains("." + FLAG_SUFFIX) && !k.EndsWith("." + LAST_INTERACTION_SUFFIX));

            foreach (var key in relationshipKeys)
            {
                if (_nsm.TryGet<float>(key, out var currentValue))
                {
                    float newValue = _config != null
                        ? _config.ClampValue(currentValue - (_config.DecayAmountPerTick))
                        : Mathf.Clamp(currentValue - 1f, 0f, 100f);

                    if (Math.Abs(newValue - currentValue) > 0.001f)
                    {
                        _nsm.Set(key, newValue);
                        string characterId = ExtractCharacterIdFromKey(key);
                        _nsm.EventBus.Emit(new RelationshipValueChangedEvent(characterId, currentValue, newValue, -(_config?.DecayAmountPerTick ?? 1f)));
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        private void ApplySingleShift(string characterId, float delta)
        {
            string key = RelationshipKey(characterId);
            float currentValue = _nsm.TryGet<float>(key, out var cv) ? cv : (_config?.DefaultRelationshipValue ?? 50f);

            float clampedDelta = _config != null ? _config.ClampShift(delta) : delta;
            float newValue = _config != null
                ? _config.ClampValue(currentValue + clampedDelta)
                : Mathf.Clamp(currentValue + clampedDelta, 0f, 100f);

            _nsm.Set(key, newValue);
            _nsm.Set(LastInteractionKey(characterId), Time.time);
            _lastInteractionTimes[characterId] = Time.time;

            if (Math.Abs(newValue - currentValue) > 0.001f)
            {
                _nsm.EventBus.Emit(new RelationshipValueChangedEvent(characterId, currentValue, newValue, clampedDelta));
            }
        }

        private string ExtractCharacterIdFromKey(string key)
        {
            // Key format: relationships.{characterId} or relationships.{characterId}.{suffix}
            if (!key.StartsWith(KEY_PREFIX + "."))
                return key;

            string remainder = key.Substring(KEY_PREFIX.Length + 1);
            int dotIndex = remainder.IndexOf('.');
            return dotIndex > 0 ? remainder.Substring(0, dotIndex) : remainder;
        }

        #endregion

        #region Dialogue Event Subscription

        private void SubscribeToDialogueEvents()
        {
            if (_nsm == null)
                return;

            _nsm.EventBus.Subscribe(DialogueRelationshipShiftEvent.KEY, OnDialogueRelationshipShift);
        }

        private void UnsubscribeFromDialogueEvents()
        {
            if (_nsm == null)
                return;

            _nsm.EventBus.Unsubscribe(DialogueRelationshipShiftEvent.KEY, OnDialogueRelationshipShift);
        }

        private void OnDialogueRelationshipShift(NSMEvent e)
        {
            if (e is DialogueRelationshipShiftEvent shiftEvent)
            {
                // Only apply if there's a meaningful delta
                if (Math.Abs(shiftEvent.ClampedDelta) > 0.001f)
                {
                    var shift = new RelationshipShift(shiftEvent.CharacterId, shiftEvent.ClampedDelta);
                    ApplyShift(shift);
                }
            }
        }

        #endregion

        #region Decay Coroutine

        private void StartDecayCoroutine()
        {
            if (_decayCoroutine != null)
                return;

            _decayStarted = true;
            _decayCoroutine = StartCoroutine(DecayCoroutine());
        }

        private void StopDecayCoroutine()
        {
            if (_decayCoroutine != null)
            {
                StopCoroutine(_decayCoroutine);
                _decayCoroutine = null;
            }
        }

        private IEnumerator DecayCoroutine()
        {
            if (_config == null)
            {
                Debug.LogWarning("[RelationshipMemorySystem] No config, decay disabled.");
                yield break;
            }

            // Wait for grace period
            yield return new WaitForSeconds(_config.DecayGracePeriodSeconds);

            while (true)
            {
                yield return new WaitForSeconds(_config.DecayIntervalSeconds);

                PerformDecayTick();
            }
        }

        private void PerformDecayTick()
        {
            if (_nsm == null || _config == null)
                return;

            // Get all relationship keys and their last interaction times
            var allKeys = _nsm.GetAllKeys();
            var relationshipKeys = allKeys
                .Where(k => k.StartsWith(KEY_PREFIX + ".") && !k.Contains("." + FLAG_SUFFIX) && !k.EndsWith("." + LAST_INTERACTION_SUFFIX))
                .ToList();

            foreach (var key in relationshipKeys)
            {
                string characterId = ExtractCharacterIdFromKey(key);
                string lastInteractionKey = LastInteractionKey(characterId);

                // Check if enough time has passed since last interaction
                float lastInteraction = _nsm.TryGet<float>(lastInteractionKey, out var lit) ? lit : 0f;
                float timeSinceInteraction = Time.time - lastInteraction;

                // Only decay if past grace period since last interaction
                if (timeSinceInteraction >= _config.DecayGracePeriodSeconds)
                {
                    if (_nsm.TryGet<float>(key, out var currentValue) && currentValue > _config.MinRelationshipValue)
                    {
                        float newValue = _config.ClampValue(currentValue - _config.DecayAmountPerTick);

                        if (Math.Abs(newValue - currentValue) > 0.001f)
                        {
                            _nsm.Set(key, newValue);
                            _nsm.EventBus.Emit(new RelationshipValueChangedEvent(characterId, currentValue, newValue, -_config.DecayAmountPerTick));
                        }
                    }
                }
            }
        }

        #endregion

        #region Force Decay (for testing without Time.time dependency)

        /// <summary>
        /// Forces decay to start immediately (bypasses grace period). For testing.
        /// </summary>
        public void StartDecayForTesting()
        {
            _decayStarted = true;
            if (_decayCoroutine != null)
                StopCoroutine(_decayCoroutine);
            _decayCoroutine = StartCoroutine(DecayCoroutineForTesting());
        }

        private IEnumerator DecayCoroutineForTesting()
        {
            // Skip grace period for testing
            while (true)
            {
                yield return null; // Single frame tick for testing
                PerformDecayTick();
            }
        }

        #endregion
    }
}
