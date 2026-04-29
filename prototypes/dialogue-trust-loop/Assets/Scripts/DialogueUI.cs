// PROTOTYPE - NOT FOR PRODUCTION
// Date: 2026-04-29

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple dialogue UI for prototype.
/// Shows speaker name, text, and choice buttons.
/// </summary>
public class DialogueUI : MonoBehaviour
{
    [Header("UI References (prototype - assign in inspector)")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI bodyText;
    public GameObject tapIndicator;  // "Tap to continue" chevron
    public GameObject choicesPanel;
    public Button choiceButtonPrefab;
    public Transform choiceContainer;

    [Header("Prototype Config")]
    [TextArea(2, 4)]
    public string placeholderText = "Dialogue text will appear here...";

    private List<Button> _activeChoiceButtons = new List<Button>();

    private void Start()
    {
        ClearUI();
    }

    public void ShowText(string speakerId, string text)
    {
        dialoguePanel.SetActive(true);

        if (string.IsNullOrEmpty(speakerId))
        {
            // Narration
            speakerText.text = "";
            speakerText.gameObject.SetActive(false);
        }
        else
        {
            speakerText.text = speakerId;
            speakerText.gameObject.SetActive(true);
        }

        bodyText.text = text;
        tapIndicator.SetActive(true);
        choicesPanel.SetActive(false);
    }

    public void ShowChoices(string speakerId, string text, List<string> choiceTexts)
    {
        dialoguePanel.SetActive(true);

        if (string.IsNullOrEmpty(speakerId))
        {
            speakerText.text = "";
            speakerText.gameObject.SetActive(false);
        }
        else
        {
            speakerText.text = speakerId;
            speakerText.gameObject.SetActive(true);
        }

        bodyText.text = text;
        tapIndicator.SetActive(false);
        choicesPanel.SetActive(true);

        // Clear old buttons
        foreach (var btn in _activeChoiceButtons)
            Destroy(btn.gameObject);
        _activeChoiceButtons.Clear();

        // Create new choice buttons
        for (int i = 0; i < choiceTexts.Count; i++)
        {
            var btn = Instantiate(choiceButtonPrefab, choiceContainer);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = $"{i + 1}. {choiceTexts[i]}";
            var index = i;  // capture
            btn.onClick.AddListener(() => OnChoiceClicked(index));
            _activeChoiceButtons.Add(btn);
        }
    }

    public void ShowNarrationOnly(string text)
    {
        dialoguePanel.SetActive(true);
        speakerText.text = "";
        speakerText.gameObject.SetActive(false);
        bodyText.text = text;
        tapIndicator.SetActive(true);
        choicesPanel.SetActive(false);
    }

    public void ClearUI()
    {
        dialoguePanel.SetActive(false);
        tapIndicator.SetActive(false);
        choicesPanel.SetActive(false);
    }

    private void OnChoiceClicked(int index)
    {
        Debug.Log($"[DialogueUI] Choice clicked: index {index}");
        FindObjectOfType<PrototypeScene>().OnChoiceSelected(index);
    }

    public void OnTapToAdvance()
    {
        FindObjectOfType<PrototypeScene>().OnTapToAdvance();
    }
}
