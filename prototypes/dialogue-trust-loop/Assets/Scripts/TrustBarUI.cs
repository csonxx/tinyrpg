// PROTOTYPE - NOT FOR PRODUCTION
// Date: 2026-04-29

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Visual trust bar for prototype.
/// Displays fill amount, danger zone coloring, and label.
/// </summary>
public class TrustBarUI : MonoBehaviour
{
    public enum MeterType { Imperial, Underground }

    [Header("Config")]
    public MeterType meterType;
    public float dangerThreshold = 25f;
    public float crisisThreshold = 15f;

    [Header("UI References (prototype - assign in inspector)")]
    public Image fillImage;
    public TextMeshProUGUI labelText;
    public TextMeshProUGUI valueText;
    public GameObject dangerIndicator;  // pulsing amber
    public GameObject crisisIndicator;  // flashing red

    private const string IMPERIAL_LABEL = "Imperial";
    private const string UNDERGROUND_LABEL = "Underground";

    private float _currentFill = 0.4f;
    private Color _normalColor;
    private Color _dangerColor = new Color(0.8f, 0.4f, 0.1f);  // amber-ish

    private void Start()
    {
        // Set label
        labelText.text = meterType == MeterType.Imperial ? IMPERIAL_LABEL : UNDERGROUND_LABEL;

        // Colors from art bible
        if (meterType == MeterType.Imperial)
            _normalColor = new Color(0.72f, 0.57f, 0.35f);  // Dusty Ochre #B8925A
        else
            _normalColor = new Color(0.37f, 0.55f, 0.49f);  // Muted Jade #5E8B7E

        fillImage.color = _normalColor;
        UpdateVisuals();
    }

    public void SetValue(float normalizedValue)  // 0.0 to 1.0
    {
        _currentFill = Mathf.Clamp01(normalizedValue);
        UpdateVisuals();
    }

    public void AnimateShift(float delta)
    {
        // For prototype: simple immediate update with debug
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        fillImage.fillAmount = _currentFill;
        valueText.text = Mathf.RoundToInt(_currentFill * 100).ToString();

        // Danger/crisis coloring
        float value = _currentFill * 100f;

        if (value <= crisisThreshold)
        {
            fillImage.color = Color.red;
            crisisIndicator?.SetActive(true);
            dangerIndicator?.SetActive(false);
        }
        else if (value <= dangerThreshold)
        {
            fillImage.color = Color.Lerp(_normalColor, _dangerColor, 0.7f);
            dangerIndicator?.SetActive(true);
            crisisIndicator?.SetActive(false);
        }
        else
        {
            fillImage.color = _normalColor;
            dangerIndicator?.SetActive(false);
            crisisIndicator?.SetActive(false);
        }
    }
}
