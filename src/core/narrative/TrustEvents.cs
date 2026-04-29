using System;

namespace Core.Narrative
{
    /// <summary>
    /// Event emitted when either trust value changes.
    /// </summary>
    public sealed class TrustValueChangedEvent : NSMEvent
    {
        public const string KEY = "trust.changed";

        public readonly float Imperial;
        public readonly float Underground;

        public TrustValueChangedEvent(float imperial, float underground)
        {
            Key = KEY;
            Imperial = imperial;
            Underground = underground;
        }
    }

    /// <summary>
    /// Event emitted after a player choice applies a trust shift.
    /// </summary>
    public sealed class TrustShiftAppliedEvent : NSMEvent
    {
        public const string KEY = "trust.shift_applied";

        public readonly float DeltaImperial;
        public readonly float DeltaUnderground;
        public readonly bool IsSecret;

        public TrustShiftAppliedEvent(float deltaImperial, float deltaUnderground, bool isSecret = false)
        {
            Key = KEY;
            DeltaImperial = deltaImperial;
            DeltaUnderground = deltaUnderground;
            IsSecret = isSecret;
        }
    }

    /// <summary>
    /// Event emitted when trust crosses below the danger threshold.
    /// </summary>
    public sealed class DangerZoneEnteredEvent : NSMEvent
    {
        public const string KEY = "trust.danger_zone";

        public readonly string MeterName;

        public DangerZoneEnteredEvent(string meterName)
        {
            Key = KEY;
            MeterName = meterName;
        }
    }

    /// <summary>
    /// Event emitted when trust crosses below the crisis threshold.
    /// </summary>
    public sealed class CrisisEnteredEvent : NSMEvent
    {
        public const string KEY = "trust.crisis";

        public readonly string MeterName;

        public CrisisEnteredEvent(string meterName)
        {
            Key = KEY;
            MeterName = meterName;
        }
    }

    /// <summary>
    /// Event emitted when passive decay starts or stops.
    /// </summary>
    public sealed class PassiveDecayActiveEvent : NSMEvent
    {
        public const string KEY = "trust.decay_active";

        public readonly bool IsActive;

        public PassiveDecayActiveEvent(bool isActive)
        {
            Key = KEY;
            IsActive = isActive;
        }
    }
}
