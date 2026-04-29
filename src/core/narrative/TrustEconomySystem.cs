using System;
using System.Collections;
using UnityEngine;

namespace Core.Narrative
{
    /// <summary>
    /// Core system for managing dual trust economy with Imperial and Underground factions.
    /// Integrates with NSM for state persistence and emits events for HUD/Notification integration.
    /// </summary>
    public class TrustEconomySystem : MonoBehaviour
    {
        #region Singleton

        private static TrustEconomySystem _instance;
        private static readonly object _lock = new object();

        public static TrustEconomySystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            var go = new GameObject("TrustEconomySystem");
                            _instance = go.AddComponent<TrustEconomySystem>();
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return _instance;
            }
        }

        #endregion

        /// <summary>
        /// NSM key for Imperial trust.
        /// </summary>
        public const string IMPERIAL_KEY = "trust.imperial";

        /// <summary>
        /// NSM key for Underground trust.
        /// </summary>
        public const string UNDERGROUND_KEY = "trust.underground";

        [Tooltip("Configuration for trust economy tuning values")]
        [SerializeField] private TrustEconomyConfig _config;

        private NarrativeStateMachine _nsm;
        private float _gameStartTime;
        private bool _decayActive;
        private Coroutine _decayCoroutine;

        // Track thresholds to avoid duplicate events on same threshold
        private bool _imperialInDangerZone;
        private bool _imperialInCrisisZone;
        private bool _undergroundInDangerZone;
        private bool _undergroundInCrisisZone;

        /// <summary>
        /// Current Imperial trust value.
        /// </summary>
        public float ImperialTrust => _nsm.Get<float>(IMPERIAL_KEY);

        /// <summary>
        /// Current Underground trust value.
        /// </summary>
        public float UndergroundTrust => _nsm.Get<float>(UNDERGROUND_KEY);

        /// <summary>
        /// Whether passive decay is currently active.
        /// </summary>
        public bool IsDecayActive => _decayActive;

        /// <summary>
        /// Configuration used by this system (may be null if not set).
        /// </summary>
        public TrustEconomyConfig Config => _config;

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            if (_config == null)
            {
                Debug.LogWarning("[TrustEconomySystem] No config assigned, using hardcoded defaults.");
            }
        }

        private void OnEnable()
        {
            SubscribeToNSM();
        }

        private void OnDisable()
        {
            UnsubscribeFromNSM();
            StopDecay();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Initializes the trust economy system with the provided NSM instance.
        /// Sets initial values if not already present in NSM.
        /// </summary>
        /// <param name="nsm">The NarrativeStateMachine instance</param>
        public void Initialize(NarrativeStateMachine nsm)
        {
            _nsm = nsm;

            // Initialize schema keys if they don't exist
            if (!_nsm.IsSchemaKey(IMPERIAL_KEY))
            {
                _nsm.LoadSchema(new[] { IMPERIAL_KEY, UNDERGROUND_KEY });
            }

            // Set initial values if defaults are needed
            var config = GetConfig();
            if (!_nsm.HasKey(IMPERIAL_KEY))
            {
                _nsm.Set(IMPERIAL_KEY, config.ImperialStartValue);
            }
            if (!_nsm.HasKey(UNDERGROUND_KEY))
            {
                _nsm.Set(UNDERGROUND_KEY, config.UndergroundStartValue);
            }

            // Reset threshold tracking
            ResetThresholdTracking();

            // Start decay timer
            _gameStartTime = Time.time;
            StartDecay();
        }

        /// <summary>
        /// Applies a trust shift to both factions, clamped to max shift per choice.
        /// Emits TrustShiftAppliedEvent after applying.
        /// </summary>
        /// <param name="shift">The trust shift to apply</param>
        public void ApplyShift(TrustShift shift)
        {
            if (_nsm == null) return;

            var config = GetConfig();
            var clampedShift = shift.Clamped(config.MaxShiftPerChoice);

            float oldImperial = ImperialTrust;
            float oldUnderground = UndergroundTrust;

            // Apply to NSM - this will trigger subscription callback
            _nsm.Mutate(IMPERIAL_KEY, clampedShift.DeltaImperial);
            _nsm.Mutate(UNDERGROUND_KEY, clampedShift.DeltaUnderground);

            // Emit TrustShiftAppliedEvent
            _nsm.EventBus.Emit(new TrustShiftAppliedEvent(clampedShift.DeltaImperial, clampedShift.DeltaUnderground));

            CheckDangerCrisisThresholds(oldImperial, oldUnderground);
            CheckParityCrisis();
        }

        /// <summary>
        /// Forces a decay tick, reducing trust by decay amount for both factions.
        /// Does not emit TrustShiftApplied (internal decay, not player choice).
        /// </summary>
        public void ForceDecayTick()
        {
            if (_nsm == null) return;

            var config = GetConfig();
            float oldImperial = ImperialTrust;
            float oldUnderground = UndergroundTrust;

            _nsm.Mutate(IMPERIAL_KEY, -config.DecayAmountPerInterval);
            _nsm.Mutate(UNDERGROUND_KEY, -config.DecayAmountPerInterval);

            CheckDangerCrisisThresholds(oldImperial, oldUnderground);
            CheckParityCrisis();
        }

        #endregion

        #region NSM Integration

        private void SubscribeToNSM()
        {
            if (_nsm == null) return;
            _nsm.Subscribe(IMPERIAL_KEY, OnTrustChanged);
            _nsm.Subscribe(UNDERGROUND_KEY, OnTrustChanged);
        }

        private void UnsubscribeFromNSM()
        {
            if (_nsm == null) return;
            _nsm.Unsubscribe(IMPERIAL_KEY, OnTrustChanged);
            _nsm.Unsubscribe(UNDERGROUND_KEY, OnTrustChanged);
        }

        private void OnTrustChanged(NSMEvent e)
        {
            if (e is KeyChangedEvent keyEvent)
            {
                EmitTrustValueChanged();
            }
        }

        private void EmitTrustValueChanged()
        {
            if (_nsm == null) return;
            _nsm.EventBus.Emit(new TrustValueChangedEvent(ImperialTrust, UndergroundTrust));
        }

        #endregion

        #region Decay

        private void StartDecay()
        {
            if (_decayCoroutine != null)
            {
                StopCoroutine(_decayCoroutine);
            }
            _decayCoroutine = StartCoroutine(DecayLoop());
        }

        private void StopDecay()
        {
            if (_decayCoroutine != null)
            {
                StopCoroutine(_decayCoroutine);
                _decayCoroutine = null;
            }

            if (_decayActive)
            {
                _decayActive = false;
                _nsm.EventBus.Emit(new PassiveDecayActiveEvent(false));
            }
        }

        private IEnumerator DecayLoop()
        {
            var config = GetConfig();

            // Wait for grace period
            yield return new WaitForSeconds(config.DecayGracePeriodSeconds);

            _decayActive = true;
            _nsm.EventBus.Emit(new PassiveDecayActiveEvent(true));

            // Decay loop
            while (true)
            {
                yield return new WaitForSeconds(config.DecayIntervalSeconds);

                // Only decay if game is in scene active state
                if (_nsm != null && _nsm.CurrentState == NSMState.SCENE_ACTIVE)
                {
                    ForceDecayTick();
                }
            }
        }

        #endregion

        #region Threshold Detection

        private void CheckDangerCrisisThresholds(float oldImperial, float oldUnderground)
        {
            var config = GetConfig();
            float imperial = ImperialTrust;
            float underground = UndergroundTrust;

            // Check Imperial danger zone
            if (oldImperial > config.DangerThreshold && imperial <= config.DangerThreshold)
            {
                _imperialInDangerZone = true;
                _nsm.EventBus.Emit(new DangerZoneEnteredEvent(IMPERIAL_KEY));
            }

            // Check Underground danger zone
            if (oldUnderground > config.DangerThreshold && underground <= config.DangerThreshold)
            {
                _undergroundInDangerZone = true;
                _nsm.EventBus.Emit(new DangerZoneEnteredEvent(UNDERGROUND_KEY));
            }

            // Check Imperial crisis zone
            if (oldImperial > config.CrisisThreshold && imperial <= config.CrisisThreshold)
            {
                _imperialInCrisisZone = true;
                _nsm.EventBus.Emit(new CrisisEnteredEvent(IMPERIAL_KEY));
            }

            // Check Underground crisis zone
            if (oldUnderground > config.CrisisThreshold && underground <= config.CrisisThreshold)
            {
                _undergroundInCrisisZone = true;
                _nsm.EventBus.Emit(new CrisisEnteredEvent(UNDERGROUND_KEY));
            }
        }

        private void CheckParityCrisis()
        {
            var config = GetConfig();
            float imperial = ImperialTrust;
            float underground = UndergroundTrust;

            float difference = Math.Abs(imperial - underground);
            bool bothInDanger = imperial <= config.DangerThreshold
                && underground <= config.DangerThreshold;

            // Parity crisis: within 10 points AND both <= 25
            // This is informational - the story doesn't specify what event to emit
            // For now we emit a debug log. If needed, a ParityCrisisEvent could be added.
            if (difference <= config.ParityDifferenceThreshold && bothInDanger)
            {
                Debug.Log($"[TrustEconomySystem] Parity crisis detected: Imperial={imperial}, Underground={underground}, Diff={difference}");
            }
        }

        private void ResetThresholdTracking()
        {
            _imperialInDangerZone = false;
            _imperialInCrisisZone = false;
            _undergroundInDangerZone = false;
            _undergroundInCrisisZone = false;
        }

        #endregion

        #region Configuration Access

        private TrustEconomyConfig GetConfig()
        {
            if (_config != null) return _config;

            // Return a static default instance if no config is set
            return GetDefaultConfig();
        }

        private static TrustEconomyConfig _defaultConfig;

        private static TrustEconomyConfig GetDefaultConfig()
        {
            if (_defaultConfig == null)
            {
                _defaultConfig = ScriptableObject.CreateInstance<TrustEconomyConfig>();
            }
            return _defaultConfig;
        }

        #endregion
    }
}
