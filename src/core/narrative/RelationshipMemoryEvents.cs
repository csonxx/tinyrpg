using System;
using System.Collections.Generic;

namespace Core.Narrative
{
    /// <summary>
    /// Raised when a relationship shift is applied to one or more characters.
    /// </summary>
    public sealed class RelationshipShiftAppliedEvent : NSMEvent
    {
        public const string KEY = "relationship.shift_applied";

        public RelationshipShift Shift { get; }

        public RelationshipShiftAppliedEvent(RelationshipShift shift)
        {
            Shift = shift;
        }

        public override string Key => KEY;
    }

    /// <summary>
    /// Raised when a relationship value changes for a character.
    /// </summary>
    public sealed class RelationshipValueChangedEvent : NSMEvent
    {
        public const string KEY = "relationship.value_changed";

        public string CharacterId { get; }
        public float OldValue { get; }
        public float NewValue { get; }
        public float Delta { get; }

        public RelationshipValueChangedEvent(string characterId, float oldValue, float newValue, float delta)
        {
            CharacterId = characterId ?? throw new ArgumentNullException(nameof(characterId));
            OldValue = oldValue;
            NewValue = newValue;
            Delta = delta;
        }

        public override string Key => KEY;
    }

    /// <summary>
    /// Raised when a memory flag is set or cleared.
    /// </summary>
    public sealed class MemoryFlagChangedEvent : NSMEvent
    {
        public const string KEY = "relationship.flag_changed";

        public string CharacterId { get; }
        public string FlagName { get; }
        public bool Value { get; }

        public MemoryFlagChangedEvent(string characterId, string flagName, bool value)
        {
            CharacterId = characterId ?? throw new ArgumentNullException(nameof(characterId));
            FlagName = flagName ?? throw new ArgumentNullException(nameof(flagName));
            Value = value;
        }

        public override string Key => KEY;
    }

    /// <summary>
    /// Emitted by DialogueEngine when a choice modifies relationship with the current speaker.
    /// </summary>
    public sealed class DialogueRelationshipShiftEvent : NSMEvent
    {
        public const string KEY = "dialogue.relationship_shift";

        /// <summary>
        /// The character whose relationship is being affected (typically the speaker).
        /// </summary>
        public string CharacterId { get; }

        /// <summary>
        /// The relationship delta to apply.
        /// </summary>
        public float Delta { get; }

        /// <summary>
        /// The clamped delta (within max shift magnitude).
        /// </summary>
        public float ClampedDelta { get; }

        public DialogueRelationshipShiftEvent(string characterId, float delta, float clampedDelta)
        {
            CharacterId = characterId ?? throw new ArgumentNullException(nameof(characterId));
            Delta = delta;
            ClampedDelta = clampedDelta;
        }

        public override string Key => KEY;
    }
}
