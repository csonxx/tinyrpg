using System;
using System.Collections.Generic;

namespace Core.Narrative
{
    /// <summary>
    /// Base class for all NSM events. Contains the event key used for routing.
    /// </summary>
    public abstract class NSMEvent
    {
        /// <summary>
        /// The event key/topic, used for glob pattern matching.
        /// </summary>
        public string Key { get; }

        protected NSMEvent(string key)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
        }
    }

    /// <summary>
    /// Raised when a state value changes via Mutate or Set.
    /// </summary>
    public sealed class KeyChangedEvent : NSMEvent
    {
        public object OldValue { get; }
        public object NewValue { get; }
        public float? Delta { get; }

        public KeyChangedEvent(string key, object oldValue, object newValue, float? delta = null)
            : base(key)
        {
            OldValue = oldValue;
            NewValue = newValue;
            Delta = delta;
        }
    }

    /// <summary>
    /// Raised when the NSM's global state enum changes.
    /// </summary>
    public sealed class StateChangedEvent : NSMEvent
    {
        public NSMState OldState { get; }
        public NSMState NewState { get; }

        public StateChangedEvent(NSMState oldState, NSMState newState)
            : base("nsm.state")
        {
            OldState = oldState;
            NewState = newState;
        }
    }

    /// <summary>
    /// Raised when an Undo operation restores a previous value.
    /// </summary>
    public sealed class UndoPerformedEvent : NSMEvent
    {
        public string Key { get; }
        public object RestoredValue { get; }

        public UndoPerformedEvent(string key, object restoredValue)
            : base("nsm.undo")
        {
            Key = key;
            RestoredValue = restoredValue;
        }
    }

    /// <summary>
    /// Raised when schema validation fails during deserialization.
    /// </summary>
    public sealed class SchemaValidationFailedEvent : NSMEvent
    {
        public IReadOnlyList<string> Errors { get; }

        public SchemaValidationFailedEvent(IReadOnlyList<string> errors)
            : base("nsm.schema_validation_failed")
        {
            Errors = errors ?? throw new ArgumentNullException(nameof(errors));
        }
    }

    /// <summary>
    /// Raised when a trust meter crosses 0 or 100 boundary.
    /// </summary>
    public sealed class TrustBoundaryReachedEvent : NSMEvent
    {
        public string MeterName { get; }
        public float Value { get; }
        public TrustBoundary Boundary { get; }

        public TrustBoundaryReachedEvent(string meterName, float value, TrustBoundary boundary)
            : base("trust.boundary")
        {
            MeterName = meterName ?? throw new ArgumentNullException(nameof(meterName));
            Value = value;
            Boundary = boundary;
        }
    }

    /// <summary>
    /// Indicates which boundary a trust value crossed.
    /// </summary>
    public enum TrustBoundary
    {
        None,
        CrossedZero,
        CrossedHundred
    }
}
