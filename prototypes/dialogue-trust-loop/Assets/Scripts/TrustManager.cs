// PROTOTYPE - NOT FOR PRODUCTION
// Date: 2026-04-29

using System;
using UnityEngine;
using事件 = UnityEngine.Events;

/// <summary>
/// Simplified dual trust economy for prototype.
/// Owns imperial and underground trust values (0-100).
/// Emits events on trust changes.
/// </summary>
public class TrustManager : MonoBehaviour
{
    // Events
    public event Action<float, float> OnTrustChanged;  // (imperial, underground)
    public event Action<float, float> OnTrustShiftApplied;  // (deltaImperial, deltaUnderground)
    public event Action<string> OnDangerZoneEntered;  // "imperial" or "underground"
    public event Action<string> OnCrisisEntered;

    // Trust values
    private float _imperial = 40f;
    private float _underground = 40f;

    public float imperial => _imperial;
    public float underground => _underground;

    // Thresholds
    private const float DANGER_THRESHOLD = 25f;
    private const float CRISIS_THRESHOLD = 15f;

    private void Start()
    {
        // Initialize at 40/40 per design doc
        _imperial = 40f;
        _underground = 40f;
        OnTrustChanged?.Invoke(_imperial, _underground);
    }

    /// <summary>
    /// Apply a trust shift from a dialogue choice.
    /// </summary>
    public void ApplyShift(TrustShift shift)
    {
        float prevImperial = _imperial;
        float prevUnderground = _underground;

        _imperial = Mathf.Clamp(_imperial + shift.imperial, 0f, 100f);
        _underground = Mathf.Clamp(_underground + shift.underground, 0f, 100f);

        Debug.Log($"[Trust] Shift applied: Imperial {shift.imperial:+0;-0} ({prevImperial:F1}→{_imperial:F1}), " +
                  $"Underground {shift.underground:+0;-0} ({prevUnderground:F1}→{_underground:F1})");

        OnTrustShiftApplied?.Invoke(shift.imperial, shift.underground);
        OnTrustChanged?.Invoke(_imperial, _underground);

        // Check danger/crisis thresholds
        CheckThreshold("imperial", prevImperial, _imperial);
        CheckThreshold("underground", prevUnderground, _underground);
    }

    private void CheckThreshold(string meter, float prev, float current)
    {
        bool prevWasSafe = prev > DANGER_THRESHOLD;
        bool currentIsDanger = current <= DANGER_THRESHOLD && current > CRISIS_THRESHOLD;
        bool currentIsCrisis = current <= CRISIS_THRESHOLD;

        if (currentIsCrisis && prev > CRISIS_THRESHOLD)
        {
            Debug.LogWarning($"[Trust] {meter} entered CRISIS zone ({current:F1})!");
            OnCrisisEntered?.Invoke(meter);
        }
        else if (currentIsDanger && prevWasSafe)
        {
            Debug.Log($"[Trust] {meter} entered danger zone ({current:F1})!");
            OnDangerZoneEntered?.Invoke(meter);
        }
    }
}
