using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Narrative
{
    /// <summary>
    /// Represents a relationship shift to apply to one or more characters.
    /// Immutable struct containing per-character deltas.
    /// </summary>
    [Serializable]
    public struct RelationshipShift : IEquatable<RelationshipShift>
    {
        /// <summary>
        /// Character ID to delta mapping.
        /// </summary>
        public readonly Dictionary<string, float> Shifts;

        /// <summary>
        /// Creates a relationship shift with a single character delta.
        /// </summary>
        public RelationshipShift(string characterId, float delta)
        {
            Shifts = new Dictionary<string, float> { { characterId, delta } };
        }

        /// <summary>
        /// Creates a relationship shift with multiple character deltas.
        /// </summary>
        public RelationshipShift(Dictionary<string, float> shifts)
        {
            Shifts = shifts ?? new Dictionary<string, float>();
        }

        /// <summary>
        /// Returns the delta for a given character, or 0 if not present.
        /// </summary>
        public float GetDelta(string characterId)
        {
            return Shifts.TryGetValue(characterId, out var delta) ? delta : 0f;
        }

        /// <summary>
        /// Returns all character IDs affected by this shift.
        /// </summary>
        public IEnumerable<string> AffectedCharacters => Shifts.Keys;

        /// <summary>
        /// Returns true if this shift affects the given character.
        /// </summary>
        public bool AffectsCharacter(string characterId) => Shifts.ContainsKey(characterId);

        /// <summary>
        /// Returns true if no characters are affected.
        /// </summary>
        public bool IsEmpty => Shifts == null || Shifts.Count == 0;

        public bool Equals(RelationshipShift other)
        {
            if (Shifts == null && other.Shifts == null) return true;
            if (Shifts == null || other.Shifts == null) return false;
            if (Shifts.Count != other.Shifts.Count) return false;
            return Shifts.All(kvp => other.Shifts.TryGetValue(kvp.Key, out var v) && Mathf.Approximately(v, kvp.Value));
        }

        public override bool Equals(object obj) => obj is RelationshipShift other && Equals(other);

        public override int GetHashCode()
        {
            if (Shifts == null || Shifts.Count == 0) return 0;
            unchecked
            {
                int hash = 17;
                foreach (var kvp in Shifts.OrderBy(x => x.Key))
                {
                    hash = hash * 31 + kvp.Key.GetHashCode();
                    hash = hash * 31 + kvp.Value.GetHashCode();
                }
                return hash;
            }
        }

        public static bool operator ==(RelationshipShift left, RelationshipShift right) => left.Equals(right);
        public static bool operator !=(RelationshipShift left, RelationshipShift right) => !left.Equals(right);

        public override string ToString()
        {
            if (IsEmpty) return "RelationshipShift(empty)";
            var parts = Shifts.Select(kvp => $"{kvp.Key}:{kvp.Value:+0.0;-0.0}").ToArray();
            return $"RelationshipShift({string.Join(", ", parts)})";
        }
    }
}
