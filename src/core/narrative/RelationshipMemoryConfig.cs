using UnityEngine;

namespace Core.Narrative
{
    /// <summary>
    /// Configuration for the Relationship Memory system.
    /// ScriptableObject asset stored in assets/data/config/.
    /// </summary>
    [CreateAssetMenu(fileName = "RelationshipMemoryConfig", menuName = "TinyRPG/RelationshipMemoryConfig")]
    public sealed class RelationshipMemoryConfig : ScriptableObject
    {
        [Header("Starting Values")]

        [Tooltip("Default relationship value (0-100) when first meeting a character.")]
        [Range(0f, 100f)]
        public float DefaultRelationshipValue = 50f;

        [Header("Decay Settings")]

        [Tooltip("Seconds of no interaction before passive decay begins.")]
        public float DecayGracePeriodSeconds = 120f;

        [Tooltip("Seconds between each passive decay tick.")]
        public float DecayIntervalSeconds = 60f;

        [Tooltip("Amount to subtract from each relationship value per decay tick.")]
        [Range(0f, 10f)]
        public float DecayAmountPerTick = 1f;

        [Header("Limits")]

        [Tooltip("Maximum absolute shift magnitude per choice (0 = no limit).")]
        public float MaxShiftPerChoice = 0f;

        [Tooltip("Minimum relationship value (clamp floor).")]
        public float MinRelationshipValue = 0f;

        [Tooltip("Maximum relationship value (clamp ceiling).")]
        public float MaxRelationshipValue = 100f;

        /// <summary>
        /// Clamps a shift value to the configured maximum if set.
        /// </summary>
        public float ClampShift(float rawShift)
        {
            if (MaxShiftPerChoice <= 0f) return rawShift;
            return Mathf.Clamp(rawShift, -MaxShiftPerChoice, MaxShiftPerChoice);
        }

        /// <summary>
        /// Clamps a relationship value to the configured range.
        /// </summary>
        public float ClampValue(float value)
        {
            return Mathf.Clamp(value, MinRelationshipValue, MaxRelationshipValue);
        }
    }
}
