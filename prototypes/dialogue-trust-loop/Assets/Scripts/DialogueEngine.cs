// PROTOTYPE - NOT FOR PRODUCTION
// Date: 2026-04-29

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simplified dialogue engine for prototype.
/// Traverses a dialogue tree, emits events for UI, applies trust shifts.
/// </summary>
public class DialogueEngine : MonoBehaviour
{
    // Events
    public event Action<DialogueNode> OnNodeChanged;  // New node ready to display
    public event Action OnDialogueComplete;

    private Dictionary<string, DialogueNode> _nodes;
    private DialogueNode _currentNode;
    private string _currentNodeId;

    public DialogueNode currentNode => _currentNode;
    public bool isChoiceActive => _currentNode?.type == DialogueNodeType.CHOICE;

    public void StartDialogue(Dictionary<string, DialogueNode> nodes, string startNodeId)
    {
        _nodes = nodes;
        _currentNodeId = startNodeId;
        AdvanceToNode(_currentNodeId);
    }

    public void OnChoiceSelected(int choiceIndex)
    {
        if (_currentNode?.type != DialogueNodeType.CHOICE)
            return;

        var choices = _currentNode.choices;
        if (choiceIndex < 0 || choiceIndex >= choices.Count)
        {
            Debug.LogError($"[DialogueEngine] Invalid choice index {choiceIndex}");
            return;
        }

        var choice = choices[choiceIndex];
        Debug.Log($"[DialogueEngine] Choice selected: \"{choice.text}\"");

        // Apply trust shift
        if (choice.trustShift != null)
        {
            var trustManager = FindObjectOfType<TrustManager>();
            trustManager.ApplyShift(choice.trustShift);
        }

        // Advance to next node
        AdvanceToNode(choice.nextNodeId);
    }

    public void OnTapToAdvance()
    {
        if (_currentNode == null) return;

        if (_currentNode.type == DialogueNodeType.TEXT)
        {
            AdvanceToNode(_currentNode.nextNodeId);
        }
        else if (_currentNode.type == DialogueNodeType.END)
        {
            Debug.Log("[DialogueEngine] Dialogue complete.");
            OnDialogueComplete?.Invoke();
        }
    }

    private void AdvanceToNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            Debug.LogError("[DialogueEngine] Attempted to advance to null/empty nodeId!");
            OnDialogueComplete?.Invoke();
            return;
        }

        if (!_nodes.TryGetValue(nodeId, out var node))
        {
            Debug.LogError($"[DialogueEngine] Node not found: {nodeId}");
            OnDialogueComplete?.Invoke();
            return;
        }

        _currentNodeId = nodeId;
        _currentNode = node;
        Debug.Log($"[DialogueEngine] → Node: [{node.type}] {node.nodeId}");
        OnNodeChanged?.Invoke(node);
    }
}
