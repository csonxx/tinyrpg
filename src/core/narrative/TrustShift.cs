using System;

namespace Core.Narrative
{
    /// <summary>
    /// Immutable data class representing a trust shift to be applied to both factions.
    /// Used to pass trust change data between systems without direct NSM coupling.
    /// </summary>
    [Serializable]
    public struct TrustShift : IEquatable<TrustShift>
    {
        /// <summary>
        /// Change to apply to Imperial trust (positive = gain, negative = lose).
        /// </summary>
        public readonly float DeltaImperial;

        /// <summary>
        /// Change to apply to Underground trust (positive = gain, negative = lose).
        /// </summary>
        public readonly float DeltaUnderground;

        /// <summary>
        /// Whether this shift is secret (not displayed to player in history).
        /// </summary>
        public readonly bool IsSecret;

        public TrustShift(float deltaImperial, float deltaUnderground, bool isSecret = false)
        {
            DeltaImperial = deltaImperial;
            DeltaUnderground = deltaUnderground;
            IsSecret = isSecret;
        }

        /// <summary>
        /// Creates a new TrustShift with clamped delta values.
        /// </summary>
        /// <param name="maxMagnitude">Maximum absolute value for each delta</param>
        public TrustShift Clamped(float maxMagnitude)
        {
            return new TrustShift(
                ClampToMagnitude(DeltaImperial, maxMagnitude),
                ClampToMagnitude(DeltaUnderground, maxMagnitude),
                IsSecret
            );
        }

        private static float ClampToMagnitude(float value, float maxMagnitude)
        {
            if (value > maxMagnitude) return maxMagnitude;
            if (value < -maxMagnitude) return -maxMagnitude;
            return value;
        }

        public bool Equals(TrustShift other)
        {
            return DeltaImperial.Equals(other.DeltaImperial)
                && DeltaUnderground.Equals(other.DeltaUnderground)
                && IsSecret == other.IsSecret;
        }

        public override bool Equals(object obj)
        {
            return obj is TrustShift other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = DeltaImperial.GetHashCode();
                hashCode = (hashCode * 397) ^ DeltaUnderground.GetHashCode();
                hashCode = (hashCode * 397) ^ IsSecret.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"TrustShift(Imperial: {DeltaImperial:+0.0;-0.0;+0.0}, Underground: {DeltaUnderground:+0.0;-0.0;+0.0}, Secret: {IsSecret})";
        }

        public static bool operator ==(TrustShift left, TrustShift right) => left.Equals(right);
        public static bool operator !=(TrustShift left, TrustShift right) => !left.Equals(right);
    }
}
