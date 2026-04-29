// PROTOTYPE - NOT FOR PRODUCTION
// Date: 2026-04-29

using System.Collections.Generic;

/// <summary>
/// Hardcoded sample dialogue tree for prototype testing.
/// This represents what would come from Chapter Content Data in production.
/// </summary>
public static class SampleDialogue
{
    public static Dictionary<string, DialogueNode> BuildTestTree()
    {
        var nodes = new Dictionary<string, DialogueNode>();

        // Node 1: Opening narration
        nodes["start"] = new DialogueNode
        {
            nodeId = "start",
            type = DialogueNodeType.TEXT,
            speakerId = null,  // narration
            content = "The rain hammers against the window. Captain YAMAMOTO watches you from across the table, his eyes unreadable.",
            nextNodeId = "yamamoto_1"
        };

        // Node 2: YAMAMOTO speaks
        nodes["yamamoto_1"] = new DialogueNode
        {
            nodeId = "yamamoto_1",
            type = DialogueNodeType.TEXT,
            speakerId = "YAMAMOTO",
            content = "The shipment arrives tomorrow. You will accompany Major KATO to the docks. Report anything... unusual.",
            nextNodeId = "choice_1"
        };

        // Node 3: First choice
        nodes["choice_1"] = new DialogueNode
        {
            nodeId = "choice_1",
            type = DialogueNodeType.CHOICE,
            speakerId = null,
            content = "How do you respond to YAMAMOTO's orders?",
            choices = new List<DialogueChoice>
            {
                new DialogueChoice
                {
                    text = "\"Understood, Captain. I won't let you down.\"",
                    nextNodeId = "choice_1_response_a",
                    trustShift = new TrustShift { imperial = +8, underground = -3 }
                },
                new DialogueChoice
                {
                    text = "\"Captain... there may be complications. The underground is watching the docks.\"",
                    nextNodeId = "choice_1_response_b",
                    trustShift = new TrustShift { imperial = -5, underground = +10 }
                },
                new DialogueChoice
                {
                    text = "[Remain silent. Nod curtly.]",
                    nextNodeId = "choice_1_response_c",
                    trustShift = new TrustShift { imperial = +2, underground = +2 }
                }
            }
        };

        // Choice 1-A: Loyal response
        nodes["choice_1_response_a"] = new DialogueNode
        {
            nodeId = "choice_1_response_a",
            type = DialogueNodeType.TEXT,
            speakerId = "YAMAMOTO",
            content = "Good. Your loyalty is noted. Major KATO will brief you further.",
            nextNodeId = "choice_2"
        };

        // Choice 1-B: Warn underground
        nodes["choice_1_response_b"] = new DialogueNode
        {
            nodeId = "choice_1_response_b",
            type = DialogueNodeType.TEXT,
            speakerId = "YAMAMOTO",
            content = "[His eyes narrow slightly] ...You know more than you let on. Perhaps too much.",
            nextNodeId = "choice_2"
        };

        // Choice 1-C: Silent
        nodes["choice_1_response_c"] = new DialogueNode
        {
            nodeId = "choice_1_response_c",
            type = DialogueNodeType.TEXT,
            speakerId = "YAMAMOTO",
            content = "[He studies you for a long moment] You are... careful. Good.",
            nextNodeId = "choice_2"
        };

        // Node 4: Second choice
        nodes["choice_2"] = new DialogueNode
        {
            nodeId = "choice_2",
            type = DialogueNodeType.CHOICE,
            speakerId = null,
            content = "Before you leave, YAMAMOTO adds one more thing...",
            choices = new List<DialogueChoice>
            {
                new DialogueChoice
                {
                    text = "\"I heard about the incident last week. A shame about LIU.\"",
                    nextNodeId = "ending_confrontation",
                    trustShift = new TrustShift { imperial = +12, underground = -8 }
                },
                new DialogueChoice
                {
                    text = "\"The shipment details — I'll need them in writing, for Major KATO.\"",
                    nextNodeId = "ending_clever",
                    trustShift = new TrustShift { imperial = +3, underground = +7 }
                },
                new DialogueChoice
                {
                    text = "\"Nothing, Captain. I'm ready.\"",
                    nextNodeId = "ending_quiet",
                    trustShift = new TrustShift { imperial = +5, underground = +3 }
                }
            }
        };

        // Ending: Confrontation path
        nodes["ending_confrontation"] = new DialogueNode
        {
            nodeId = "ending_confrontation",
            type = DialogueNodeType.TEXT,
            speakerId = "YAMAMOTO",
            content = "[His expression hardens] LIU was a traitor. You would do well to remember that... for your own sake.",
            nextNodeId = "end"
        };

        // Ending: Clever path
        nodes["ending_clever"] = new DialogueNode
        {
            nodeId = "ending_clever",
            type = DialogueNodeType.TEXT,
            speakerId = "YAMAMOTO",
            content = "[A thin smile] In writing. Of course. You think like a survivor. That is... useful.",
            nextNodeId = "end"
        };

        // Ending: Quiet path
        nodes["ending_quiet"] = new DialogueNode
        {
            nodeId = "ending_quiet",
            type = DialogueNodeType.TEXT,
            speakerId = "YAMAMOTO",
            content = "Good. Go.",
            nextNodeId = "end"
        };

        // End node
        nodes["end"] = new DialogueNode
        {
            nodeId = "end",
            type = DialogueNodeType.END,
            speakerId = null,
            content = "END OF SCENE"
        };

        return nodes;
    }
}
