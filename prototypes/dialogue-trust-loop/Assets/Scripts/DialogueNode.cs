// PROTOTYPE - NOT FOR PRODUCTION
// Date: 2026-04-29

using System;
using System.Collections.Generic;

/// <summary>
/// Simplified dialogue tree node types for prototype testing.
/// </summary>
public enum DialogueNodeType
{
    TEXT,      // Display text, advance on tap
    CHOICE,    // Show choices, wait for selection
    CONDITION, // Branch based on NSM state (simplified: skip for prototype)
    END        // Scene complete
}

[Serializable]
public class TrustShift
{
    public float imperial = 0f;
    public float underground = 0f;
}

[Serializable]
public class DialogueChoice
{
    public string text;
    public string nextNodeId;
    public TrustShift trustShift;
}

[Serializable]
public class DialogueNode
{
    public string nodeId;
    public DialogueNodeType type;
    public string speakerId;  // null = narration
    public string content;
    public List<DialogueChoice> choices;  // for CHOICE type
    public string nextNodeId;  // for TEXT type

    public bool IsNarration => string.IsNullOrEmpty(speakerId);
}
