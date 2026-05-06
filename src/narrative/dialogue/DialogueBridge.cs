using System;
using Core.Narrative;
using UnityEngine;

namespace Core.Narrative.Dialogue
{
    /// <summary>
    /// MonoBehaviour that bridges scene loading to the DialogueEngine.
    ///
    /// Lives on the dialogue scene. When the scene finishes loading (via SceneReadyEvent),
    /// this component retrieves the DialogueTree for the current scene from EpisodeStructure
    /// and calls DialogueEngine.StartDialogue(tree) to begin the dialogue.
    ///
    /// This component replaces the comment in EpisodeStructure.OnSceneReady() that said
    /// "DialogueEngine.StartDialogue will be called by the scene's DialogueBridge component".
    /// </summary>
    public sealed class DialogueBridge : MonoBehaviour
    {
        [Tooltip("Reference to the DialogueEngine. If null, uses the singleton instance.")]
        [SerializeField] private DialogueEngine _dialogueEngine;

        private NarrativeStateMachine _nsm;
        private bool _isSubscribed;

        private void Awake()
        {
            _nsm = NarrativeStateMachine.Instance;
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            if (_isSubscribed) return;
            _isSubscribed = true;

            // Listen for scene ready events to start dialogue
            EventBus.Instance.Subscribe(Core.Scene.SceneReadyEvent.KEY, OnSceneReady);

            // Listen for episode complete to show completion UI
            _nsm.Subscribe(EpisodeEvents.EpisodeCompleteKey, OnEpisodeComplete);

            // Listen for settings changes to forward to DialogueEngine
            EventBus.Instance.Subscribe(Core.Settings.AutoAdvanceChangedEvent.KEY, OnAutoAdvanceChanged);
        }

        private void UnsubscribeFromEvents()
        {
            if (!_isSubscribed) return;
            _isSubscribed = false;

            EventBus.Instance.Unsubscribe(Core.Scene.SceneReadyEvent.KEY, OnSceneReady);
            _nsm.Unsubscribe(EpisodeEvents.EpisodeCompleteKey, OnEpisodeComplete);
            EventBus.Instance.Unsubscribe(Core.Settings.AutoAdvanceChangedEvent.KEY, OnAutoAdvanceChanged);
        }

        private void OnSceneReady(NSMEvent e)
        {
            if (e is not Core.Scene.SceneReadyEvent) return;

            // Read the current scene ID from NSM (set by EpisodeStructure before loading)
            var sceneId = _nsm.Get<string>(EpisodeKeys.CurrentScene);
            if (string.IsNullOrEmpty(sceneId))
            {
                Debug.LogWarning("[DialogueBridge] No current scene ID in NSM. Dialogue will not start.");
                return;
            }

            // Find the DialogueTree for this scene from the current episode
            var tree = GetDialogueTreeForScene(sceneId);
            if (tree == null)
            {
                Debug.LogWarning($"[DialogueBridge] No DialogueTree found for scene: {sceneId}. Dialogue will not start.");
                return;
            }

            // Get or create the DialogueEngine
            var engine = _dialogueEngine ?? DialogueEngine.Instance;

            try
            {
                engine.StartDialogue(tree);
                Debug.Log($"[DialogueBridge] Started dialogue for scene: {sceneId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DialogueBridge] Failed to start dialogue: {ex.Message}");
            }
        }

        private void OnEpisodeComplete(NSMEvent e)
        {
            Debug.Log("[DialogueBridge] Episode complete. Showing completion UI.");
            // TODO: Show episode complete screen / UI transition
            // For now, log the event. UI team will hook up the completion screen.
        }

        private void OnAutoAdvanceChanged(NSMEvent e)
        {
            if (e is Core.Settings.AutoAdvanceChangedEvent evt)
            {
                DialogueEngine.Instance.AutoAdvanceEnabled = evt.Enabled;
            }
        }

        private DialogueTree GetDialogueTreeForScene(string sceneId)
        {
            // Walk the current episode data to find the scene's DialogueTree
            // We stored the tree in SceneData when the episode was initialized
            var episodeData = GetCurrentEpisodeData();
            if (episodeData == null) return null;

            foreach (var chapter in episodeData.Chapters)
            {
                foreach (var scene in chapter.Scenes)
                {
                    if (scene.SceneId == sceneId)
                    {
                        return scene.DialogueTree;
                    }
                }
            }

            return null;
        }

        private EpisodeData GetCurrentEpisodeData()
        {
            // Get from EpisodeStructure directly - it persists as a singleton
            var episodeStructure = FindObjectOfType<EpisodeStructure>();
            if (episodeStructure != null)
            {
                return episodeStructure.EpisodeData;
            }
            return null;
        }
    }
}
