using UnityEngine;

namespace Core.Narrative
{
    /// <summary>
    /// ScriptableObject configuration for Narrative State Machine tuning knobs.
    /// Place in Resources or Addressables for runtime access.
    /// </summary>
    [CreateAssetMenu(fileName = "NSMConfig", menuName = "FogBoundOath/NSM Config")]
    public class NSMConfig : ScriptableObject
    {
        [Header("Undo Settings")]
        [Tooltip("Maximum number of undo steps in the queue.")]
        [SerializeField] private int _maxUndo = 20;

        [Header("Trust Meter Clamping")]
        [Tooltip("Minimum value for trust meters.")]
        [SerializeField] private float _trustMin = 0f;

        [Tooltip("Maximum value for trust meters.")]
        [SerializeField] private float _trustMax = 100f;

        [Header("Schema Validation")]
        [Tooltip("If true, Mutate will throw on unknown keys. If false, allows any key.")]
        [SerializeField] private bool _strictSchemaMode = true;

        /// <summary>
        /// Maximum number of undo steps stored in the queue.
        /// </summary>
        public int MAX_UNDO => _maxUndo;

        /// <summary>
        /// Minimum clamped value for trust meters (typically 0).
        /// </summary>
        public float TRUST_MIN => _trustMin;

        /// <summary>
        /// Maximum clamped value for trust meters (typically 100).
        /// </summary>
        public float TRUST_MAX => _trustMax;

        /// <summary>
        /// If true, Mutate will throw exceptions on unknown keys.
        /// If false, any key is allowed.
        /// </summary>
        public bool STRICT_SCHEMA_MODE => _strictSchemaMode;

        /// <summary>
        /// Default config instance used when no explicit config is provided.
        /// </summary>
        public static NSMConfig Default { get; private set; }

        private void OnEnable()
        {
            // Auto-register as default if none set
            if (Default == null)
                Default = this;
        }

        /// <summary>
        /// Check if a given key is a trust-related key (starts with "trust.").
        /// </summary>
        public static bool IsTrustKey(string key)
        {
            return !string.IsNullOrEmpty(key) && key.StartsWith("trust.", System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Clamp a trust value to the configured min/max range.
        /// </summary>
        public float ClampTrust(float value)
        {
            return Mathf.Clamp(value, _trustMin, _trustMax);
        }
    }
}
