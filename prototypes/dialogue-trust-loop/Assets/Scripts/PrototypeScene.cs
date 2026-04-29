// PROTOTYPE - NOT FOR PRODUCTION
// Date: 2026-04-29

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main prototype scene orchestrator.
/// Wires TrustManager, DialogueEngine, and DialogueUI together.
/// </summary>
public class PrototypeScene : MonoBehaviour
{
    [Header("Component References")]
    public TrustManager trustManager;
    public DialogueEngine dialogueEngine;
    public DialogueUI dialogueUI;

    [Header("Trust Bar References")]
    public TrustBarUI imperialBar;
    public TrustBarUI undergroundBar;

    [Header("Debug UI")]
    public GameObject debugPanel;
    public UnityEngine.UI.Text debugLog;

    private string _currentState = "idle";
    private int _choiceCount = 0;
    private float _sessionStartTime;

    private void Start()
    {
        _sessionStartTime = Time.time;

        // Wire up events
        trustManager.OnTrustChanged += OnTrustChanged;
        trustManager.OnTrustShiftApplied += OnTrustShiftApplied;
        trustManager.OnDangerZoneEntered += OnDangerZone;
        trustManager.OnCrisisEntered += OnCrisis;

        dialogueEngine.OnNodeChanged += OnNodeChanged;
        dialogueEngine.OnDialogueComplete += OnDialogueComplete;

        // Start dialogue
        var tree = SampleDialogue.BuildTestTree();
        dialogueEngine.StartDialogue(tree, "start");
        _currentState = "playing";

        Log("Prototype started. Tap to advance dialogue.");
    }

    private void OnTrustChanged(float imperial, float underground)
    {
        imperialBar.SetValue(imperial / 100f);
        undergroundBar.SetValue(underground / 100f);

        Log($"Trust: Imperial {imperial:F0} | Underground {underground:F0}");
    }

    private void OnTrustShiftApplied(float deltaImperial, float deltaUnderground)
    {
        string deltaStr = $"({deltaImperial:+0;-0}, {deltaUnderground:+0;-0})";
        Log($"Trust shifted {deltaStr}");
    }

    private void OnDangerZone(string meter)
    {
        Log($"⚠️ {meter.ToUpper()} entered DANGER zone!");
    }

    private void OnCrisis(string meter)
    {
        Log($"🚨 {meter.ToUpper()} entered CRISIS zone!");
    }

    private void OnNodeChanged(DialogueNode node)
    {
        if (node.type == DialogueNodeType.TEXT)
        {
            if (node.IsNarration)
                dialogueUI.ShowNarrationOnly(node.content);
            else
                dialogueUI.ShowText(node.speakerId, node.content);
            _currentState = "waiting_tap";
        }
        else if (node.type == DialogueNodeType.CHOICE)
        {
            var choiceTexts = new List<string>();
            foreach (var c in node.choices)
                choiceTexts.Add(c.text);

            dialogueUI.ShowChoices(node.speakerId, node.content, choiceTexts);
            _currentState = "waiting_choice";
        }
        else if (node.type == DialogueNodeType.END)
        {
            OnDialogueComplete();
        }
    }

    private void OnDialogueComplete()
    {
        _currentState = "complete";
        dialogueUI.ClearUI();
        Log($"=== SCENE COMPLETE ===");
        Log($"Choices made: {_choiceCount}");
        Log($"Session time: {Time.time - _sessionStartTime:F1}s");
        Log("Prototype test complete. Check trust values above.");
    }

    public void OnChoiceSelected(int index)
    {
        if (_currentState != "waiting_choice") return;
        _choiceCount++;
        dialogueEngine.OnChoiceSelected(index);
        _currentState = "playing";
    }

    public void OnTapToAdvance()
    {
        if (_currentState != "waiting_tap") return;
        dialogueEngine.OnTapToAdvance();
        _currentState = "playing";
    }

    public void OnScreenTapped()
    {
        if (_currentState == "waiting_tap")
        {
            OnTapToAdvance();
        }
        else if (_currentState == "waiting_choice")
        {
            // Ignore taps in choice mode unless they hit a button
        }
    }

    private void Log(string msg)
    {
        Debug.Log($"[Prototype] {msg}");
        if (debugLog != null)
            debugLog.text += $"\n{msg}";
    }
}
