using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Narrative.Dialogue
{
    /// <summary>
    /// Enumeration of dialogue node types used in the branching dialogue system.
    /// </summary>
    public enum DialogueNodeType
    {
        /// <summary>
        /// Display text, wait for tap, advance to nextNodeId.
        /// </summary>
        TEXT,

        /// <summary>
        /// Display choices, wait for selection, advance to selected nextNodeId, apply trustShift.
        /// </summary>
        CHOICE,

        /// <summary>
        /// Evaluate conditionExpr against NSM, branch to trueNextNodeId or falseNextNodeId.
        /// </summary>
        CONDITION,

        /// <summary>
        /// Emit DialogueSceneComplete, end the dialogue scene.
        /// </summary>
        END
    }

    /// <summary>
    /// Represents a single node within a dialogue tree.
    ///
    /// Each node carries the data needed by DialogueEngine to advance the conversation.
    /// Nodes are plain data containers — no behavior lives here.
    /// </summary>
    [Serializable]
    public sealed class DialogueNode
    {
        [SerializeField] private string _id;
        [SerializeField] private DialogueNodeType _type;
        [SerializeField][TextArea] private string _text;
        [SerializeField] private string _speakerId;
        [SerializeField] private string _nextNodeId;
        [SerializeField] private ChoiceData[] _choices;
        [SerializeField] private float[] _trustShifts;
        [SerializeField] private string _conditionExpr;
        [SerializeField] private string _trueNextNodeId;
        [SerializeField] private string _falseNextNodeId;
        [SerializeField] private string _registerClue;
        [SerializeField] private string _clueCategory;

        /// <summary>
        /// Unique identifier for this node within its dialogue tree.
        /// </summary>
        public string Id => _id;

        /// <summary>
        /// The type of this node, determining its behavior in DialogueEngine.
        /// </summary>
        public DialogueNodeType Type => _type;

        /// <summary>
        /// The text to display for TEXT and CHOICE nodes.
        /// </summary>
        public string Text => _text;

        /// <summary>
        /// The character ID of the speaker for this node. null for narration.
        /// "PLAYER" indicates the player character; all other IDs are NPCs.
        /// </summary>
        public string SpeakerId => _speakerId;

        /// <summary>
        /// The node to advance to after a TEXT node is confirmed.
        /// </summary>
        public string NextNodeId => _nextNodeId;

        /// <summary>
        /// Available choices for a CHOICE node. Returns an empty list if none.
        /// </summary>
        public IReadOnlyList<ChoiceData> Choices => _choices ?? Array.Empty<ChoiceData>();

        /// <summary>
        /// Trust shifts corresponding to each choice, applied when that choice is selected.
        /// </summary>
        public IReadOnlyList<float> TrustShifts => _trustShifts ?? Array.Empty<float>();

        /// <summary>
        /// The expression evaluated by CONDITION nodes (e.g. "trust.imperial >= 50").
        /// </summary>
        public string ConditionExpr => _conditionExpr;

        /// <summary>
        /// The node to advance to when the CONDITION expression evaluates to true.
        /// </summary>
        public string TrueNextNodeId => _trueNextNodeId;

        /// <summary>
        /// The node to advance to when the CONDITION expression evaluates to false.
        /// </summary>
        public string FalseNextNodeId => _falseNextNodeId;

        /// <summary>
        /// Clue ID to register when this node is completed (after advancing past it).
        /// Null or empty means no clue is registered.
        /// </summary>
        public string RegisterClue => _registerClue;

        /// <summary>
        /// Category of the clue registered by this node (e.g. "documents", "conversations", "evidence").
        /// Null or empty is treated as uncategorized.
        /// </summary>
        public string ClueCategory => _clueCategory;

        /// <summary>
        /// Constructs a DialogueNode with all fields.
        /// </summary>
        public DialogueNode(
            string id,
            DialogueNodeType type,
            string text = null,
            string speakerId = null,
            string nextNodeId = null,
            ChoiceData[] choices = null,
            float[] trustShifts = null,
            string conditionExpr = null,
            string trueNextNodeId = null,
            string falseNextNodeId = null,
            string registerClue = null,
            string clueCategory = null)
        {
            _id = id;
            _type = type;
            _text = text;
            _speakerId = speakerId;
            _nextNodeId = nextNodeId;
            _choices = choices;
            _trustShifts = trustShifts;
            _conditionExpr = conditionExpr;
            _trueNextNodeId = trueNextNodeId;
            _falseNextNodeId = falseNextNodeId;
            _registerClue = registerClue;
            _clueCategory = clueCategory;
        }

        /// <summary>
        /// Creates a TEXT node that advances to the specified node.
        /// </summary>
        public static DialogueNode Text(string id, string text, string nextNodeId, string speakerId = null)
        {
            return new DialogueNode(id, DialogueNodeType.TEXT, text, speakerId, nextNodeId);
        }

        /// <summary>
        /// Creates a CHOICE node with the given choices and corresponding trust shifts.
        /// </summary>
        public static DialogueNode Choice(
            string id,
            string text,
            ChoiceData[] choices,
            float[] trustShifts,
            string nextNodeId,
            string speakerId = null)
        {
            return new DialogueNode(id, DialogueNodeType.CHOICE, text, speakerId, nextNodeId, choices, trustShifts);
        }

        /// <summary>
        /// Creates a CONDITION node evaluating the given expression.
        /// </summary>
        public static DialogueNode Condition(
            string id,
            string conditionExpr,
            string trueNextNodeId,
            string falseNextNodeId)
        {
            return new DialogueNode(
                id,
                DialogueNodeType.CONDITION,
                conditionExpr: conditionExpr,
                trueNextNodeId: trueNextNodeId,
                falseNextNodeId: falseNextNodeId);
        }

        /// <summary>
        /// Creates an END node.
        /// </summary>
        public static DialogueNode End(string id)
        {
            return new DialogueNode(id, DialogueNodeType.END);
        }
    }

    /// <summary>
    /// Represents a single choice option within a CHOICE node.
    /// </summary>
    [Serializable]
    public sealed class ChoiceData
    {
        [SerializeField] private string _text;
        [SerializeField] private string _nextNodeId;
        [SerializeField] private string _clueId;

        public string Text => _text;
        public string NextNodeId => _nextNodeId;

        /// <summary>
        /// Clue ID to register when this choice is selected.
        /// Null or empty means no clue is registered.
        /// </summary>
        public string ClueId => _clueId;

        public ChoiceData(string text, string nextNodeId, string clueId = null)
        {
            _text = text;
            _nextNodeId = nextNodeId;
            _clueId = clueId;
        }
    }
}
