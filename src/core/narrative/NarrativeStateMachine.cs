using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Core.Narrative
{
    /// <summary>
    /// Narrative State Machine — single source of truth for all game state.
    ///
    /// Provides a flat key-value store with event-driven mutations, event subscription,
    /// undo queue, JSON serialization with SHA256 integrity hash, and trust boundary detection.
    ///
    /// This is a plain C# class (not a MonoBehaviour). Expose to Unity scene via
    /// a MonoBehaviour wrapper if needed, or use static Instance directly.
    /// </summary>
    public sealed class NarrativeStateMachine
    {
        #region Singleton

        private static readonly Lazy<NarrativeStateMachine> _lazyInstance =
            new Lazy<NarrativeStateMachine>(() => new NarrativeStateMachine(null));

        /// <summary>
        /// Global singleton instance. Thread-safe via Lazy[T].
        /// </summary>
        public static NarrativeStateMachine Instance => _lazyInstance.Value;

        #endregion

        #region Constants

        /// <summary>
        /// Maximum number of undo steps. Config can override at construction.
        /// </summary>
        public const int MAX_UNDO = 20;

        private const string TRUST_PREFIX = "trust.";
        private const string HASH_KEY = "__nsm_hash__";

        #endregion

        #region Fields

        private readonly Dictionary<string, object> _data = new Dictionary<string, object>();
        private readonly List<(string key, object value)> _undoStack = new List<(string, object)>();
        private readonly EventBus _eventBus = new EventBus();
        private readonly HashSet<string> _schemaKeys = new HashSet<string>();
        private readonly NSMConfig _config;

        private NSMState _currentState = NSMState.TITLE;

        #endregion

        #region Properties

        /// <summary>
        /// The current NSM state enum value.
        /// </summary>
        public NSMState CurrentState => _currentState;

        /// <summary>
        /// The event bus used for subscribing to NSM events.
        /// </summary>
        public EventBus EventBus => _eventBus;

        /// <summary>
        /// Number of items currently in the undo queue.
        /// </summary>
        public int UndoCount => _undoStack.Count;

        /// <summary>
        /// Maximum undo steps allowed.
        /// </summary>
        public int MaxUndo => _config?.MAX_UNDO ?? MAX_UNDO;

        /// <summary>
        /// The trust minimum value (e.g., 0).
        /// </summary>
        public float TrustMin => _config?.TRUST_MIN ?? 0f;

        /// <summary>
        /// The trust maximum value (e.g., 100).
        /// </summary>
        public float TrustMax => _config?.TRUST_MAX ?? 100f;

        #endregion

        #region Constructor

        /// <summary>
        /// Create an NSM instance with optional configuration.
        /// Prefer using the static Instance singleton unless you need separate instances.
        /// </summary>
        /// <param name="config">Optional NSMConfig ScriptableObject. Uses default config if null.</param>
        public NarrativeStateMachine(NSMConfig config)
        {
            _config = config ?? NSMConfig.Default ?? CreateDefaultConfig();
        }

        private static NSMConfig CreateDefaultConfig()
        {
            var defaultConfig = ScriptableObject.CreateInstance<NSMConfig>();
            defaultConfig.name = "DefaultNSMConfig";
            return defaultConfig;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Mutate: apply a named delta to a numeric key.
        /// For trust keys (trust.*), clamps to [TRUST_MIN, TRUST_MAX] and emits TrustBoundaryReached on boundary cross.
        /// </summary>
        /// <param name="key">The key to mutate</param>
        /// <param name="delta">The float delta to add (can be negative)</param>
        public void Mutate(string key, float delta)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            // Validate against schema in strict mode
            if (_config.STRICT_SCHEMA_MODE && !_schemaKeys.Contains(key))
                throw new NSMSchemaException($"Key '{key}' is not defined in the current schema.");

            object oldValueRaw;
            if (!_data.TryGetValue(key, out oldValueRaw))
            {
                oldValueRaw = 0f;
            }

            // Ensure current value is numeric
            if (!(oldValueRaw is float) && !(oldValueRaw is int))
                throw new NSMSchemaException($"Key '{key}' does not hold a numeric value. Cannot Mutate.");

            float oldValue = Convert.ToSingle(oldValueRaw);
            float newValue;

            bool isTrustKey = IsTrustKey(key);

            if (isTrustKey)
            {
                newValue = _config.ClampTrust(oldValue + delta);
            }
            else
            {
                newValue = oldValue + delta;
            }

            // Detect trust boundary crossing before mutation
            TrustBoundary boundary = TrustBoundary.None;
            if (isTrustKey)
            {
                if (oldValue >= TrustMin && newValue <= TrustMin)
                    boundary = TrustBoundary.CrossedZero;
                else if (oldValue <= TrustMax && newValue >= TrustMax)
                    boundary = TrustBoundary.CrossedHundred;
            }

            // Push to undo stack before mutating
            PushUndo(key, oldValueRaw);

            // Apply mutation
            SetInternal(key, newValue, oldValueRaw, delta, isTrustKey);

            // Emit trust boundary event
            if (boundary != TrustBoundary.None)
            {
                var trustEvent = new TrustBoundaryReachedEvent(key, newValue, boundary);
                _eventBus.Emit(trustEvent);
            }
        }

        /// <summary>
        /// Set: directly assign a value to a key. Used for non-numeric or complex state.
        /// </summary>
        /// <param name="key">The key to set</param>
        /// <param name="value">The value to assign</param>
        public void Set(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            if (_config.STRICT_SCHEMA_MODE && !_schemaKeys.Contains(key))
                throw new NSMSchemaException($"Key '{key}' is not defined in the current schema.");

            object oldValue = null;
            _data.TryGetValue(key, out oldValue);

            PushUndo(key, oldValue);
            SetInternal(key, value, oldValue, null, IsTrustKey(key));
        }

        /// <summary>
        /// Get: retrieve the current value for a key.
        /// </summary>
        /// <typeparam name="T">Expected type of the value</typeparam>
        /// <param name="key">The key to look up</param>
        /// <returns>The value cast to T, or default(T) if not found</returns>
        public T Get<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            if (_data.TryGetValue(key, out var value))
            {
                if (value is T typed)
                    return typed;

                // Handle int/float conversions
                if (typeof(T) == typeof(float) && value is int intVal)
                    return (T)(object)(float)intVal;
                if (typeof(T) == typeof(int) && value is float floatVal)
                    return (T)(object)(int)floatVal;

                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch (InvalidCastException)
                {
                    throw new NSMSchemaException($"Key '{key}' holds value of type '{value.GetType().Name}' which cannot be cast to '{typeof(T).Name}'.");
                }
            }

            return default(T);
        }

        /// <summary>
        /// Check if a key exists in the state store.
        /// </summary>
        public bool HasKey(string key)
        {
            return _data.ContainsKey(key);
        }

        /// <summary>
        /// Subscribe to events matching a glob pattern. See EventBus for pattern syntax.
        /// </summary>
        /// <param name="pattern">Glob pattern (e.g. "trust.*" or "nsm.*")</param>
        /// <param name="callback">Action invoked on matching events</param>
        public void Subscribe(string pattern, Action<NSMEvent> callback)
        {
            _eventBus.Subscribe(pattern, callback);
        }

        /// <summary>
        /// Unsubscribe from a pattern. If callback is null, removes all listeners for that pattern.
        /// </summary>
        public void Unsubscribe(string pattern, Action<NSMEvent> callback = null)
        {
            _eventBus.Unsubscribe(pattern, callback);
        }

        /// <summary>
        /// Undo the last mutation. Restores the previous value of the mutated key.
        /// Silently no-ops when queue is empty.
        /// Blocked during CUTSCENE and CHAPTER_COMPLETE states.
        /// </summary>
        public void Undo()
        {
            // Undo blocked in certain states
            if (_currentState == NSMState.CUTSCENE || _currentState == NSMState.CHAPTER_COMPLETE)
                return;

            if (_undoStack.Count == 0)
                return;

            var (key, oldValue) = _undoStack[_undoStack.Count - 1];
            _undoStack.RemoveAt(_undoStack.Count - 1);

            object currentValue = null;
            _data.TryGetValue(key, out currentValue);

            SetInternal(key, oldValue, currentValue, null, IsTrustKey(key));

            var undoEvent = new UndoPerformedEvent(key, oldValue);
            _eventBus.Emit(undoEvent);
        }

        /// <summary>
        /// Clear the entire undo queue. Called on chapter load.
        /// </summary>
        public void ClearUndoQueue()
        {
            _undoStack.Clear();
        }

        /// <summary>
        /// Serialize the entire state store to a JSON string with SHA256 integrity hash.
        /// </summary>
        public string Serialize()
        {
            var container = new SerializationContainer
            {
                State = _currentState,
                Data = new Dictionary<string, object>(_data)
            };

            string json = JsonUtility.ToJson(container, true);
            string hash = ComputeSha256(json);

            // Append hash as a separate field
            var hashWrapper = new SerializationContainer
            {
                State = _currentState,
                Data = new Dictionary<string, object>(_data)
            };
            hashWrapper.Data[HASH_KEY] = hash;

            return JsonUtility.ToJson(hashWrapper, false);
        }

        /// <summary>
        /// Deserialize a JSON string previously created by Serialize().
        /// Validates SHA256 hash and schema before restoring state.
        /// Emits SchemaValidationFailed on error.
        /// </summary>
        /// <param name="json">JSON string from Serialize()</param>
        /// <returns>True if deserialization succeeded, false on hash mismatch or schema error</returns>
        public bool Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                EmitSchemaError("JSON string is null or empty.");
                return false;
            }

            try
            {
                var container = JsonUtility.FromJson<SerializationContainer>(json);
                if (container == null || container.Data == null)
                {
                    EmitSchemaError("Failed to parse JSON or container data is null.");
                    return false;
                }

                // Validate hash
                if (container.Data.TryGetValue(HASH_KEY, out var storedHashObj) && storedHashObj is string storedHash)
                {
                    container.Data.Remove(HASH_KEY);

                    // Re-serialize data portion and compare
                    string recomputedHash = ComputeSha256(JsonUtility.ToJson(container));

                    if (!string.Equals(storedHash, recomputedHash, StringComparison.OrdinalIgnoreCase))
                    {
                        EmitSchemaError($"Hash mismatch: stored={storedHash}, computed={recomputedHash}. Data may be tampered.");
                        return false;
                    }
                }

                // Schema validation on deserialize (always validates)
                if (_config.STRICT_SCHEMA_MODE && _schemaKeys.Count > 0)
                {
                    var errors = new List<string>();
                    foreach (var kvp in container.Data)
                    {
                        if (!IsReservedKey(kvp.Key) && !_schemaKeys.Contains(kvp.Key))
                        {
                            errors.Add($"Unexpected key '{kvp.Key}' not defined in schema.");
                        }
                    }
                    if (errors.Count > 0)
                    {
                        EmitSchemaError(string.Join("; ", errors));
                        return false;
                    }
                }

                // Clear undo queue on load
                ClearUndoQueue();

                // Restore state
                _data.Clear();
                foreach (var kvp in container.Data)
                {
                    _data[kvp.Key] = kvp.Value;
                }
                _currentState = container.State;

                return true;
            }
            catch (Exception ex)
            {
                EmitSchemaError($"Deserialization exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Set the NSM state enum. Emits StateChanged event.
        /// </summary>
        public void SetState(NSMState newState)
        {
            if (_currentState == newState)
                return;

            NSMState oldState = _currentState;
            _currentState = newState;

            var stateEvent = new StateChangedEvent(oldState, newState);
            _eventBus.Emit(stateEvent);
        }

        /// <summary>
        /// Load a schema — a set of known keys. Clears any previous schema.
        /// </summary>
        /// <param name="keys">Enumerable of valid key names</param>
        public void LoadSchema(IEnumerable<string> keys)
        {
            _schemaKeys.Clear();
            if (keys != null)
            {
                foreach (var key in keys)
                {
                    if (!string.IsNullOrEmpty(key))
                        _schemaKeys.Add(key);
                }
            }
        }

        /// <summary>
        /// Check if a key is part of the current schema.
        /// </summary>
        public bool IsSchemaKey(string key)
        {
            return _schemaKeys.Contains(key);
        }

        /// <summary>
        /// Get a snapshot of all current keys (for debugging/inspection).
        /// </summary>
        public IReadOnlyDictionary<string, object> GetAllData()
        {
            return _data;
        }

        #endregion

        #region Private Helpers

        private void SetInternal(string key, object newValue, object oldValue, float? delta, bool isTrustKey)
        {
            float? newValueFloat = TryConvertToFloat(newValue);
            float? clampedValue = isTrustKey && newValueFloat.HasValue
                ? _config.ClampTrust(newValueFloat.Value)
                : newValueFloat;

            object finalValue = clampedValue ?? newValue;

            _data[key] = finalValue;

            var keyEvent = new KeyChangedEvent(key, oldValue, finalValue, delta);
            _eventBus.Emit(keyEvent);
        }

        private void PushUndo(string key, object oldValue)
        {
            if (_undoStack.Count >= MaxUndo)
            {
                _undoStack.RemoveAt(0);
            }
            _undoStack.Add((key, oldValue));
        }

        private static float? TryConvertToFloat(object value)
        {
            if (value == null) return null;
            if (value is float f) return f;
            if (value is int i) return i;
            if (value is double d) return (float)d;
            if (value is long l) return l;
            if (value is short s) return s;
            if (value is byte b) return b;
            return null;
        }

        private static bool IsReservedKey(string key)
        {
            return key == HASH_KEY;
        }

        private void EmitSchemaError(string message)
        {
            Debug.LogError($"[NSM] Schema validation failed: {message}");
            _eventBus.Emit(new SchemaValidationFailedEvent(new[] { message }));
        }

        private static string ComputeSha256(string input)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        #endregion

        #region Serialization Container

        [Serializable]
        private sealed class SerializationContainer
        {
            [SerializeField] public NSMState State;
            [SerializeField] public Dictionary<string, object> Data;
        }

        #endregion

        #region Schema Exception

        /// <summary>
        /// Exception thrown when schema validation fails in strict mode.
        /// </summary>
        public sealed class NSMSchemaException : Exception
        {
            public NSMSchemaException(string message) : base(message) { }
            public NSMSchemaException(string message, Exception inner) : base(message, inner) { }
        }

        #endregion
    }
}
