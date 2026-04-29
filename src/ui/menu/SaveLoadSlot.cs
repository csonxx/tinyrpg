using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>
    /// A single save/load slot UI element.
    ///
    /// Displays slot metadata (chapter, scene, timestamp, playtime) or "Empty" state.
    /// Fires OnSlotClicked when tapped.
    ///
    /// Attach to a prefab with a Button component.
    /// </summary>
    public sealed class SaveLoadSlot : MonoBehaviour
    {
        #region UI Elements

        [Header("Slot UI Elements")]
        [SerializeField] private Text _slotNameText;
        [SerializeField] private Text _chapterText;
        [SerializeField] private Text _sceneText;
        [SerializeField] private Text _timestampText;
        [SerializeField] private Text _playTimeText;
        [SerializeField] private Text _emptyText;
        [SerializeField] private Image _autosaveIcon; // Shown for autosave slot
        [SerializeField] private GameObject _occupiedContent; // Group shown when slot has data
        [SerializeField] private Button _slotButton;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the slot is tapped.
        /// </summary>
        public Action OnSlotClicked { get; set; }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_slotButton != null)
            {
                _slotButton.onClick.AddListener(HandleClicked);
            }
        }

        private void OnDestroy()
        {
            if (_slotButton != null)
            {
                _slotButton.onClick.RemoveListener(HandleClicked);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Configures the slot with metadata.
        /// </summary>
        /// <param name="slotName">Display name (e.g., "Autosave", "Slot 1").</param>
        /// <param name="chapterName">Chapter display name (e.g., "Chapter 1").</param>
        /// <param name="sceneName">Scene identifier.</param>
        /// <param name="timestamp">Formatted timestamp string.</param>
        /// <param name="playTime">Formatted play time string.</param>
        /// <param name="isEmpty">True if this slot is empty (no save data).</param>
        /// <param name="isAutosave">True if this is the autosave slot.</param>
        public void Setup(
            string slotName,
            string chapterName,
            string sceneName,
            string timestamp,
            string playTime,
            bool isEmpty,
            bool isAutosave)
        {
            if (_slotNameText != null) _slotNameText.text = slotName;

            if (_autosaveIcon != null) _autosaveIcon.gameObject.SetActive(isAutosave);

            if (isEmpty)
            {
                ShowEmpty();
            }
            else
            {
                ShowOccupied(chapterName, sceneName, timestamp, playTime);
            }
        }

        #endregion

        #region Private Methods

        private void ShowEmpty()
        {
            if (_emptyText != null) _emptyText.gameObject.SetActive(true);
            if (_occupiedContent != null) _occupiedContent.SetActive(false);
        }

        private void ShowOccupied(string chapterName, string sceneName, string timestamp, string playTime)
        {
            if (_emptyText != null) _emptyText.gameObject.SetActive(false);
            if (_occupiedContent != null) _occupiedContent.SetActive(true);

            if (_chapterText != null) _chapterText.text = chapterName ?? string.Empty;
            if (_sceneText != null) _sceneText.text = sceneName ?? string.Empty;
            if (_timestampText != null) _timestampText.text = timestamp ?? string.Empty;
            if (_playTimeText != null) _playTimeText.text = playTime ?? string.Empty;
        }

        private void HandleClicked()
        {
            OnSlotClicked?.Invoke();
        }

        #endregion
    }
}
