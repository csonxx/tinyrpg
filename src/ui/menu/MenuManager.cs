using System;
using Core.Audio;
using Core.Narrative;
using Core.Persistence;
using Input.Touch;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>
    /// Central coordinator for the Menu System.
    ///
    /// Owns the menu state machine (CLOSED, PAUSE_OPEN, SAVE_OPEN, LOAD_OPEN, SETTINGS_OPEN,
    /// CONFIRM_OVERWRITE, SAVING), integrates with NSM, TouchInputSystem, AudioManagement,
    /// and SaveLoadSystem.
    ///
    /// Implements S2-6 per design/gdd/menu-system.md.
    /// </summary>
    public sealed class MenuManager : MonoBehaviour
    {
        #region Menu State

        /// <summary>
        /// Menu state machine states.
        /// </summary>
        public enum MenuState
        {
            Closed,
            PauseOpen,
            SaveOpen,
            LoadOpen,
            SettingsOpen,
            ConfirmOverwrite,
            Saving
        }

        #endregion

        #region Singleton

        private static MenuManager _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// Global singleton instance.
        /// </summary>
        public static MenuManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            var go = new GameObject("MenuManager");
                            _instance = go.AddComponent<MenuManager>();
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Constants

        /// <summary>
        /// Menu fade-in/out duration in milliseconds. GDD tuning knob.
        /// </summary>
        public const int MENU_FADE_DURATION_MS = 200;

        /// <summary>
        /// Whether overwrite confirmation is required before saving to an occupied slot.
        /// GDD tuning knob.
        /// </summary>
        public const bool CONFIRM_OVERWRITE_ENABLED = true;

        #endregion

        #region Inspector References

        [Header("Canvas")]
        [SerializeField] private Canvas _menuCanvas;
        [SerializeField] private CanvasGroup _menuCanvasGroup;

        [Header("Pause Menu")]
        [SerializeField] private GameObject _pauseMenuPanel;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _loadButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;

        [Header("Pause Menu Title")]
        [SerializeField] private Text _pauseMenuTitle;

        [Header("Save/Load Screen")]
        [SerializeField] private GameObject _saveLoadPanel;
        [SerializeField] private SaveLoadSlot[] _saveSlots;
        [SerializeField] private Button _saveLoadBackButton;
        [SerializeField] private Text _saveLoadTitleText;

        [Header("Confirm Overwrite Dialog")]
        [SerializeField] private GameObject _confirmDialog;
        [SerializeField] private Button _confirmOverwriteButton;
        [SerializeField] private Button _cancelOverwriteButton;

        [Header("Settings Screen")]
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;
        [SerializeField] private Slider _voiceVolumeSlider;
        [SerializeField] private Toggle _hapticToggle;
        [SerializeField] private Toggle _autoAdvanceToggle;
        [SerializeField] private Button _settingsBackButton;

        [Header("Pause Button (HUD)")]
        [SerializeField] private Button _pauseButton;

        #endregion

        #region Private Fields

        private MenuState _currentState = MenuState.Closed;
        private MenuState _previousMenuState;
        private NSMState _previousNsmState = NSMState.SCENE_ACTIVE;
        private bool _isOpening;
        private bool _isClosing;
        private float _fadeProgress;
        private int _targetSlotForOverwrite = -1;
        private bool _isSaveMode; // true = save screen, false = load screen

        // Cached event delegates
        private event Action<int> OnSlotSelectedHandler;

        #endregion

        #region Properties

        /// <summary>
        /// The current menu state.
        /// </summary>
        public MenuState CurrentState => _currentState;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            ValidateInspectorReferences();
            InitializeUI();
            HideAllPanels();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            ProcessFade();
        }

        #endregion

        #region Initialization

        private void ValidateInspectorReferences()
        {
            if (_menuCanvas == null)
                Debug.LogError("[MenuManager] MenuCanvas is not assigned.");
            if (_menuCanvasGroup == null)
                Debug.LogError("[MenuManager] MenuCanvasGroup is not assigned.");
            if (_pauseMenuPanel == null)
                Debug.LogError("[MenuManager] PauseMenuPanel is not assigned.");
            if (_pauseButton == null)
                Debug.LogError("[MenuManager] PauseButton is not assigned.");
            if (_saveLoadPanel == null)
                Debug.LogError("[MenuManager] SaveLoadPanel is not assigned.");
            if (_settingsPanel == null)
                Debug.LogError("[MenuManager] SettingsPanel is not assigned.");
            if (_confirmDialog == null)
                Debug.LogError("[MenuManager] ConfirmDialog is not assigned.");
        }

        private void InitializeUI()
        {
            // Pause menu buttons
            _continueButton?.onClick.AddListener(OnContinueClicked);
            _saveButton?.onClick.AddListener(OnSaveClicked);
            _loadButton?.onClick.AddListener(OnLoadClicked);
            _settingsButton?.onClick.AddListener(OnSettingsClicked);
            _quitButton?.onClick.AddListener(OnQuitClicked);

            // Pause button (HUD)
            _pauseButton?.onClick.AddListener(OnPauseButtonClicked);

            // Save/Load screen
            _saveLoadBackButton?.onClick.AddListener(OnSaveLoadBackClicked);
            for (int i = 0; i < _saveSlots.Length; i++)
            {
                int slotIndex = GetSlotIndex(i);
                _saveSlots[i].OnSlotClicked = () => OnSlotClicked(slotIndex);
            }

            // Confirm overwrite dialog
            _confirmOverwriteButton?.onClick.AddListener(OnConfirmOverwriteClicked);
            _cancelOverwriteButton?.onClick.AddListener(OnCancelOverwriteClicked);

            // Settings screen
            _settingsBackButton?.onClick.AddListener(OnSettingsBackClicked);
            _musicVolumeSlider?.onValueChanged.AddListener(OnMusicVolumeChanged);
            _sfxVolumeSlider?.onValueChanged.AddListener(OnSfxVolumeChanged);
            _voiceVolumeSlider?.onValueChanged.AddListener(OnVoiceVolumeChanged);
            _hapticToggle?.onValueChanged.AddListener(OnHapticChanged);
            _autoAdvanceToggle?.onValueChanged.AddListener(OnAutoAdvanceChanged);

            // Initialize settings UI from current values
            InitializeSettingsUI();

            // Initially hide menu canvas
            _menuCanvasGroup.alpha = 0f;
            _menuCanvasGroup.blocksRaycasts = false;
        }

        private void InitializeSettingsUI()
        {
            // Hardcoded MVP values. Settings System (Sprint 3) will replace this.
            float musicVol = 1.0f;
            float sfxVol = 1.0f;
            float voiceVol = 1.0f;
            bool haptic = true;
            bool autoAdvance = false;

            if (_musicVolumeSlider != null) _musicVolumeSlider.value = musicVol;
            if (_sfxVolumeSlider != null) _sfxVolumeSlider.value = sfxVol;
            if (_voiceVolumeSlider != null) _voiceVolumeSlider.value = voiceVol;
            if (_hapticToggle != null) _hapticToggle.isOn = haptic;
            if (_autoAdvanceToggle != null) _autoAdvanceToggle.isOn = autoAdvance;
        }

        private void HideAllPanels()
        {
            if (_pauseMenuPanel != null) _pauseMenuPanel.SetActive(false);
            if (_saveLoadPanel != null) _saveLoadPanel.SetActive(false);
            if (_settingsPanel != null) _settingsPanel.SetActive(false);
            if (_confirmDialog != null) _confirmDialog.SetActive(false);
        }

        /// <summary>
        /// Maps UI slot index (0-3) to SaveLoadSystem slot (-1 for autosave, 0-2 for manual).
        /// UI displays: Slot 0 = Autosave, Slot 1 = Slot 1, Slot 2 = Slot 2, Slot 3 = Slot 3
        /// </summary>
        private int GetSlotIndex(int uiSlotIndex)
        {
            return uiSlotIndex - 1; // UI slot 0 (autosave) -> system slot -1
        }

        #endregion

        #region Event Subscription

        private void SubscribeToEvents()
        {
            // Subscribe to SaveLoadSystem events
            if (SaveLoadSystem.Instance != null)
            {
                SaveLoadSystem.Instance.OnSaveComplete += HandleSaveComplete;
                SaveLoadSystem.Instance.OnLoadComplete += HandleLoadComplete;
                SaveLoadSystem.Instance.OnLoadFailed += HandleLoadFailed;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (SaveLoadSystem.Instance != null)
            {
                SaveLoadSystem.Instance.OnSaveComplete -= HandleSaveComplete;
                SaveLoadSystem.Instance.OnLoadComplete -= HandleLoadComplete;
                SaveLoadSystem.Instance.OnLoadFailed -= HandleLoadFailed;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Opens the pause menu. Called by HUD pause button tap.
        /// </summary>
        public void OpenPauseMenu()
        {
            if (_currentState != MenuState.Closed)
                return;

            _previousNsmState = NarrativeStateMachine.Instance.CurrentState;

            // Enter MENU_OPEN state in NSM
            NarrativeStateMachine.Instance.SetState(NSMState.MENU_OPEN);

            // Disable touch input
            TouchInputSystem.Instance.SetInputState(InputState.DISABLED);
            TouchInputSystem.Instance.SetContext(SceneContext.MENU_OPEN);

            // Pause audio
            AudioManagement.Instance.Pause();

            // Save play time before opening menu
            SaveLoadSystem.Instance.PausePlayTimeTracking();

            // Transition to PAUSE_OPEN state
            TransitionToState(MenuState.PauseOpen);
            BeginFadeIn();
        }

        /// <summary>
        /// Closes the menu and resumes the game.
        /// </summary>
        public void CloseMenu()
        {
            if (_currentState == MenuState.Closed)
                return;

            if (_isClosing)
                return;

            _isClosing = true;

            // Restore previous NSM state
            NarrativeStateMachine.Instance.SetState(_previousNsmState);

            // Re-enable touch input
            TouchInputSystem.Instance.SetInputState(InputState.ENABLED);
            TouchInputSystem.Instance.SetContext(SceneContext.SCENE_ACTIVE);

            // Resume audio
            AudioManagement.Instance.Resume();

            // Resume play time tracking
            SaveLoadSystem.Instance.ResumePlayTimeTracking();

            BeginFadeOut();
        }

        /// <summary>
        /// Registers a callback for when a save/load slot is selected.
        /// </summary>
        /// <param name="handler">Callback with slot index.</param>
        public void RegisterSlotSelectedHandler(Action<int> handler)
        {
            OnSlotSelectedHandler += handler;
        }

        /// <summary>
        /// Unregisters a slot selected callback.
        /// </summary>
        public void UnregisterSlotSelectedHandler(Action<int> handler)
        {
            OnSlotSelectedHandler -= handler;
        }

        #endregion

        #region Button Handlers

        private void OnPauseButtonClicked()
        {
            OpenPauseMenu();
        }

        private void OnContinueClicked()
        {
            CloseMenu();
        }

        private void OnSaveClicked()
        {
            _isSaveMode = true;
            RefreshSaveLoadScreen(isSave: true);
            TransitionToState(MenuState.SaveOpen);
        }

        private void OnLoadClicked()
        {
            _isSaveMode = false;
            RefreshSaveLoadScreen(isSave: false);
            TransitionToState(MenuState.LoadOpen);
        }

        private void OnSettingsClicked()
        {
            TransitionToState(MenuState.SettingsOpen);
        }

        private void OnQuitClicked()
        {
            // Transition to TITLE state. Scene loading is handled by Scene Management.
            NarrativeStateMachine.Instance.SetState(NSMState.TITLE);
            CloseMenu();
        }

        private void OnSaveLoadBackClicked()
        {
            TransitionToState(MenuState.PauseOpen);
        }

        private void OnSettingsBackClicked()
        {
            TransitionToState(MenuState.PauseOpen);
        }

        private void OnSlotClicked(int slot)
        {
            if (_isSaveMode)
            {
                HandleSaveSlotClicked(slot);
            }
            else
            {
                HandleLoadSlotClicked(slot);
            }
        }

        private void HandleSaveSlotClicked(int slot)
        {
            var slotInfo = SaveLoadSystem.Instance.GetSlotInfo(slot);

            if (slotInfo.Exists && CONFIRM_OVERWRITE_ENABLED)
            {
                // Show confirmation dialog
                _targetSlotForOverwrite = slot;
                TransitionToState(MenuState.ConfirmOverwrite);
            }
            else
            {
                // Save directly (empty slot or overwrite disabled)
                PerformSave(slot);
            }
        }

        private void HandleLoadSlotClicked(int slot)
        {
            var slotInfo = SaveLoadSystem.Instance.GetSlotInfo(slot);

            if (!slotInfo.Exists)
            {
                // Slot is empty, cannot load
                Debug.Log($"[MenuManager] Cannot load from empty slot {slot}");
                return;
            }

            // Close menu first, then load
            // Loading will restore NSM state via SaveLoadSystem
            _previousNsmState = NSMState.SCENE_ACTIVE; // Will be restored from save
            CloseMenu();

            // Load after menu closes
            SaveLoadSystem.Instance.Load(slot);
        }

        private void OnConfirmOverwriteClicked()
        {
            if (_targetSlotForOverwrite >= 0)
            {
                PerformSave(_targetSlotForOverwrite);
            }
            _targetSlotForOverwrite = -1;
            TransitionToState(MenuState.SaveOpen);
        }

        private void OnCancelOverwriteClicked()
        {
            _targetSlotForOverwrite = -1;
            TransitionToState(MenuState.SaveOpen);
        }

        private void PerformSave(int slot)
        {
            TransitionToState(MenuState.Saving);
            SaveLoadSystem.Instance.Save(slot);
        }

        #endregion

        #region Settings Handlers

        private void OnMusicVolumeChanged(float value)
        {
            // Hardcoded MVP — Settings System (Sprint 3) will handle actual persistence
            Debug.Log($"[MenuManager] Music volume changed to {value}");
        }

        private void OnSfxVolumeChanged(float value)
        {
            Debug.Log($"[MenuManager] SFX volume changed to {value}");
        }

        private void OnVoiceVolumeChanged(float value)
        {
            Debug.Log($"[MenuManager] Voice volume changed to {value}");
        }

        private void OnHapticChanged(bool value)
        {
            Debug.Log($"[MenuManager] Haptic feedback changed to {value}");
        }

        private void OnAutoAdvanceChanged(bool value)
        {
            Debug.Log($"[MenuManager] Auto-advance changed to {value}");
        }

        #endregion

        #region SaveLoadSystem Event Handlers

        private void HandleSaveComplete(int slot)
        {
            Debug.Log($"[MenuManager] Save completed for slot {slot}");
            TransitionToState(MenuState.SaveOpen);
            RefreshSaveLoadScreen(isSave: true);
        }

        private void HandleLoadComplete(int slot, SaveFile saveFile)
        {
            Debug.Log($"[MenuManager] Load completed from slot {slot}");
            // State has been restored by SaveLoadSystem
        }

        private void HandleLoadFailed(int slot, string errorMessage)
        {
            Debug.LogError($"[MenuManager] Load failed for slot {slot}: {errorMessage}");
            // Remain on Load screen — player can try another slot or go back
            TransitionToState(MenuState.LoadOpen);
        }

        #endregion

        #region State Management

        private void TransitionToState(MenuState newState)
        {
            MenuState oldState = _currentState;
            _currentState = newState;
            _previousMenuState = oldState;

            UpdateUIForState(newState);
        }

        private void UpdateUIForState(MenuState state)
        {
            HideAllPanels();

            switch (state)
            {
                case MenuState.Closed:
                    // All panels hidden
                    break;

                case MenuState.PauseOpen:
                    if (_pauseMenuPanel != null) _pauseMenuPanel.SetActive(true);
                    break;

                case MenuState.SaveOpen:
                    if (_saveLoadPanel != null) _saveLoadPanel.SetActive(true);
                    if (_saveLoadTitleText != null) _saveLoadTitleText.text = "Save Game";
                    RefreshSaveLoadScreen(isSave: true);
                    break;

                case MenuState.LoadOpen:
                    if (_saveLoadPanel != null) _saveLoadPanel.SetActive(true);
                    if (_saveLoadTitleText != null) _saveLoadTitleText.text = "Load Game";
                    RefreshSaveLoadScreen(isSave: false);
                    break;

                case MenuState.SettingsOpen:
                    if (_settingsPanel != null) _settingsPanel.SetActive(true);
                    InitializeSettingsUI(); // Refresh settings from current values
                    break;

                case MenuState.ConfirmOverwrite:
                    if (_confirmDialog != null) _confirmDialog.SetActive(true);
                    break;

                case MenuState.Saving:
                    // No additional UI — save is in progress
                    break;
            }
        }

        private void RefreshSaveLoadScreen(bool isSave)
        {
            for (int i = 0; i < _saveSlots.Length; i++)
            {
                int systemSlot = GetSlotIndex(i);
                var slotInfo = SaveLoadSystem.Instance.GetSlotInfo(systemSlot);

                string slotName;
                if (i == 0)
                    slotName = "Autosave";
                else
                    slotName = $"Slot {i}";

                _saveSlots[i].Setup(
                    slotName: slotName,
                    chapterName: slotInfo.Exists ? $"Chapter {slotInfo.ChapterIndex}" : null,
                    sceneName: slotInfo.Exists ? slotInfo.SceneId : null,
                    timestamp: slotInfo.Exists ? slotInfo.Timestamp.ToString("yyyy-MM-dd HH:mm") : null,
                    playTime: slotInfo.Exists ? slotInfo.FormattedPlayTime : null,
                    isEmpty: !slotInfo.Exists,
                    isAutosave: i == 0
                );
            }
        }

        #endregion

        #region Fade Animation

        private void BeginFadeIn()
        {
            _isOpening = true;
            _isClosing = false;
            _fadeProgress = 0f;
            _menuCanvasGroup.blocksRaycasts = true;
        }

        private void BeginFadeOut()
        {
            _isOpening = false;
            _isClosing = true;
            _fadeProgress = 1f;
            _menuCanvasGroup.blocksRaycasts = false;
        }

        private void ProcessFade()
        {
            float fadeSpeed = 1000f / MENU_FADE_DURATION_MS; // Progress per second

            if (_isOpening)
            {
                _fadeProgress += Time.unscaledDeltaTime * fadeSpeed;
                if (_fadeProgress >= 1f)
                {
                    _fadeProgress = 1f;
                    _isOpening = false;
                }
                _menuCanvasGroup.alpha = _fadeProgress;
            }
            else if (_isClosing)
            {
                _fadeProgress -= Time.unscaledDeltaTime * fadeSpeed;
                if (_fadeProgress <= 0f)
                {
                    _fadeProgress = 0f;
                    _isClosing = false;
                    TransitionToState(MenuState.Closed);
                }
                _menuCanvasGroup.alpha = _fadeProgress;
            }
        }

        #endregion
    }
}
