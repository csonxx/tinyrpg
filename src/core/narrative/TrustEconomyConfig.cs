using UnityEngine;

namespace Core.Narrative
{
    /// <summary>
    /// ScriptableObject containing all tunable parameters for the dual trust economy system.
    /// Located at trust.imperial and trust.underground meters.
    /// </summary>
    [CreateAssetMenu(fileName = "TrustEconomyConfig", menuName = "TinyRPG/TrustEconomyConfig")]
    public class TrustEconomyConfig : ScriptableObject
    {
        [Header("Starting Values")]
        [Tooltip("Initial trust value for Imperial faction")]
        public float ImperialStartValue = 40f;

        [Tooltip("Initial trust value for Underground faction")]
        public float UndergroundStartValue = 40f;

        [Header("Thresholds")]
        [Tooltip("Below this value, danger zone is entered")]
        public float DangerThreshold = 25f;

        [Tooltip("Below this value, crisis zone is entered")]
        public float CrisisThreshold = 15f;

        [Tooltip("Maximum possible trust value")]
        public float MaxValue = 100f;

        [Header("ApplyShift Limits")]
        [Tooltip("Maximum delta per single trust shift (clamped to this magnitude)")]
        public float MaxShiftPerChoice = 10f;

        [Header("Passive Decay")]
        [Tooltip("Time in seconds before decay begins after game start")]
        public float DecayGracePeriodSeconds = 120f;

        [Tooltip("Amount subtracted from trust per decay interval")]
        public float DecayAmountPerInterval = 0.5f;

        [Tooltip("Time in seconds between each decay tick")]
        public float DecayIntervalSeconds = 30f;

        [Header("Parity Crisis")]
        [Tooltip("Maximum difference between imperial and underground for parity crisis")]
        public float ParityDifferenceThreshold = 10f;
    }
}
