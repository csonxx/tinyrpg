using System;
using Core.Accessibility;
using Core.Narrative;
using Core.Narrative.Data;
using Core.Scene;
using Core.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bootstrap
{
    /// <summary>
    /// Bootstrap MonoBehaviour that initializes all core game systems in the correct order.
    ///
    /// Execution order:
    /// 1. Awake() - Initialize AccessibilitySystem and create runtime EpisodeData
    /// 2. Start() - Initialize other systems and wait for user input to start game
    ///
    /// This component lives on a GameObject in BootstrapScene and persists until
    /// the first dialogue scene is loaded via EpisodeStructure.StartEpisode().
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Core Systems (Auto-populated)")]

        [Tooltip("EpisodeStructure component that manages episode flow. Assign in Inspector after adding to scene.")]
        [SerializeField] private EpisodeStructure _episodeStructure;

        [Tooltip("SceneManagement component for scene transitions. Assign in Inspector.")]
        [SerializeField] private SceneManagement _sceneManagement;

        [Header("Episode Data")]

        [Tooltip("Runtime EpisodeData created from ChapterContentData. Set by InitializeEpisodeData().")]
        [SerializeField] private EpisodeData _runtimeEpisodeData;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Step 1: Initialize AccessibilitySystem (must be first)
            // AccessibilitySystem uses Lazy<T> singleton and Initialize() sets static state
            AccessibilitySystem.Instance.Initialize();
            Debug.Log("[GameBootstrap] AccessibilitySystem initialized.");

            // Step 2: Create runtime EpisodeData from ChapterContentData
            InitializeEpisodeData();
        }

        private void Start()
        {
            // Step 3: Initialize TouchInputSystem (auto-initializes via Awake, but we can verify)
            if (TouchInputSystem.Instance != null)
            {
                Debug.Log("[GameBootstrap] TouchInputSystem verified.");
            }

            // Step 4: Initialize TrustEconomySystem with NSM
            TrustEconomySystem.Instance.Initialize(NarrativeStateMachine.Instance);
            Debug.Log("[GameBootstrap] TrustEconomySystem initialized.");

            // Step 5: Initialize RelationshipMemorySystem with NSM
            RelationshipMemorySystem.Instance.Initialize(NarrativeStateMachine.Instance);
            Debug.Log("[GameBootstrap] RelationshipMemorySystem initialized.");

            // Step 6: Load SettingsSystem defaults (auto-initializes via Awake)
            if (SettingsSystem.Instance != null)
            {
                SettingsSystem.Instance.ForceSave();
                Debug.Log("[GameBootstrap] SettingsSystem verified.");
            }

            // Step 7: Load MainMenuScene
            LoadMainMenuScene();

            Debug.Log("[GameBootstrap] Bootstrap complete. Waiting for user to start game.");
        }

        #endregion

        #region Bootstrap Flow

        /// <summary>
        /// Creates a minimal runtime EpisodeData from ChapterContentData.
        /// This allows the game to boot without requiring a pre-built EpisodeData asset file.
        /// </summary>
        private void InitializeEpisodeData()
        {
            // Create dialogue tree from ChapterContentData
            var dialogueTree = ChapterContentData.CreateCh1Scene1();

            // Build minimal episode data at runtime
            _runtimeEpisodeData = new EpisodeData(
                episodeId: "ep_ch1",
                titleKey: "episode_1_title",
                scenes: new SceneData[]
                {
                    new SceneData(
                        sceneId: "ch1_scene1",
                        titleKey: "scene_1_title",
                        dialogueTree: dialogueTree
                    )
                }
            );

            Debug.Log($"[GameBootstrap] Created runtime EpisodeData for episode: {_runtimeEpisodeData.EpisodeId}");
        }

        /// <summary>
        /// Loads the MainMenuScene as the first interactive scene.
        /// Called from Start() after all systems are initialized.
        /// </summary>
        private void LoadMainMenuScene()
        {
            if (_sceneManagement != null)
            {
                _sceneManagement.LoadScene("MainMenuScene", TransitionType.FADE_BLACK);
            }
            else
            {
                // Fallback: direct scene load if SceneManagement not assigned
                SceneManager.LoadScene("MainMenuScene", LoadSceneMode.Single);
            }
        }

        /// <summary>
        /// Called by UI button or other start trigger to begin the episode.
        /// Initializes EpisodeStructure with runtime data and starts the episode.
        /// </summary>
        public void OnStartGame()
        {
            if (_episodeStructure == null)
            {
                Debug.LogError("[GameBootstrap] EpisodeStructure not assigned. Cannot start game.");
                return;
            }

            // Assign runtime episode data to EpisodeStructure
            _episodeStructure.Initialize(_runtimeEpisodeData);

            // Start the episode - this will load the first dialogue scene
            _episodeStructure.StartEpisode();

            Debug.Log("[GameBootstrap] Episode started.");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets the runtime EpisodeData created during bootstrap.
        /// Can be used by other systems that need to reference episode content.
        /// </summary>
        public EpisodeData RuntimeEpisodeData => _runtimeEpisodeData;

        #endregion
    }
}
