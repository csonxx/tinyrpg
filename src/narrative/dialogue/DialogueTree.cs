using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Narrative.Dialogue
{
    /// <summary>
    /// Contains all DialogueNodes for a single dialogue scene.
    ///
    /// The DialogueTree is a dictionary mapping node IDs to their DialogueNode data.
    /// Loaded from a Chapter Content Data asset (ScriptableObject or JSON) at scene start.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDialogueTree", menuName = "Narrative/Dialogue Tree")]
    public sealed class DialogueTree : ScriptableObject
    {
        [SerializeField] private string _sceneId;
        [SerializeField] private DialogueNode[] _nodes;
        [SerializeField] private Sprite[] _portraitSprites;
        [SerializeField] private string[] _portraitCharacterIds;

        private Dictionary<string, DialogueNode> _nodeMap;
        private Dictionary<string, Sprite> _portraitMap;

        /// <summary>
        /// The unique identifier for this dialogue scene.
        /// </summary>
        public string SceneId => _sceneId;

        /// <summary>
        /// All nodes defined in this dialogue tree.
        /// </summary>
        public IReadOnlyList<DialogueNode> Nodes => _nodes ?? Array.Empty<DialogueNode>();

        /// <summary>
        /// Looks up a node by its ID. Returns null if not found.
        /// </summary>
        public DialogueNode GetNode(string nodeId)
        {
            if (_nodeMap == null)
                BuildNodeMap();

            return _nodeMap.TryGetValue(nodeId, out var node) ? node : null;
        }

        /// <summary>
        /// Returns the ID of the first node in this dialogue tree.
        /// Throws InvalidOperationException if the tree has no nodes.
        /// </summary>
        public string GetFirstNodeId()
        {
            if (_nodes == null || _nodes.Length == 0)
                throw new InvalidOperationException($"DialogueTree '{_sceneId}' has no nodes.");
            return _nodes[0].Id;
        }

        private void BuildNodeMap()
        {
            _nodeMap = new Dictionary<string, DialogueNode>();
            if (_nodes == null) return;
            foreach (var node in _nodes)
            {
                if (!string.IsNullOrEmpty(node.Id))
                    _nodeMap[node.Id] = node;
            }
        }

        private void BuildPortraitMap()
        {
            _portraitMap = new Dictionary<string, Sprite>();
            if (_portraitSprites == null || _portraitCharacterIds == null) return;
            int count = Mathf.Min(_portraitSprites.Length, _portraitCharacterIds.Length);
            for (int i = 0; i < count; i++)
            {
                if (!string.IsNullOrEmpty(_portraitCharacterIds[i]) && _portraitSprites[i] != null)
                    _portraitMap[_portraitCharacterIds[i]] = _portraitSprites[i];
            }
        }

        /// <summary>
        /// Returns the portrait Sprite for the given character ID.
        /// Returns null if no portrait is found.
        /// </summary>
        public Sprite GetPortrait(string characterId)
        {
            if (_portraitMap == null)
                BuildPortraitMap();

            return _portraitMap.TryGetValue(characterId ?? string.Empty, out var sprite) ? sprite : null;
        }

        /// <summary>
        /// Creates a DialogueTree at runtime from a dictionary of nodes.
        /// Useful for tests and runtime-generated dialogue.
        /// </summary>
        public static DialogueTree CreateRuntime(string sceneId, Dictionary<string, DialogueNode> nodes)
        {
            var tree = CreateInstance<DialogueTree>();
            tree._sceneId = sceneId;
            tree._nodes = nodes.Values.ToArray();
            tree.BuildNodeMap();
            return tree;
        }

        #region Unity Lifecycle

        private void OnEnable()
        {
            BuildNodeMap();
            BuildPortraitMap();
        }

        #endregion
    }
}
