// PROTOTYPE - NOT FOR PRODUCTION
// Question: Dialogue UI implementation
// Date: 2026-04-29

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Core.Narrative.Dialogue;
using Input.Touch;

namespace UI.Dialogue
{
    /// <summary>
    /// Main controller for the Dialogue UI canvas.
    /// Subscribes to DialogueEngine and TouchInputSystem events,
    /// manages UI state machine: HIDDEN / SHOWING_TEXT / SHOWING_CHOICES / HISTORY_OPEN.
    /// </summary>
    public class DialogueCanvas : MonoBehaviour
    {
        #region UI State

        public enum UIState
        {
            Hidden,
            ShowingText,
            ShowingChoices,
            HistoryOpen
        }

        #endregion

        #region Events

        public event Action OnAdvanceDialogue;
        public event Action OnCancelTextAnimation;
        public event Action<int> OnChoiceSelected;
        public event Action<int> OnNavigateChoices; // -1 = left, +1 = right

        #endregion

        #region Inspector References

        [Header("Dialogue Box")]
        [SerializeField] private DialogueBox _dialogueBox;
        [SerializeField] private PortraitDisplay _portraitDisplay;

        [Header("Choice Container")]
        [SerializeField] private Transform _choiceContainer;
        [SerializeField] private ChoiceButton _choiceButtonPrefab;

        [Header("History Panel")]
        [SerializeField] private DialogueHistoryPanel _historyPanel;

        [Header("Root")]
        [SerializeField] private CanvasGroup _canvasGroup;

        #endregion

        #region Private Fields

        private UIState _currentState = UIState.Hidden;
        private ChoiceButton[] _choiceButtons;
        private int _focusedChoiceIndex;
        private const int MAX_VISIBLE_CHOICES = 6;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _choiceButtons = new ChoiceButton[MAX_VISIBLE_CHOICES];
            for (int i = 0; i < MAX_VISIBLE_CHOICES; i++)
            {
                var btn = Instantiate(_choiceButtonPrefab, _choiceContainer);
                btn.gameObject.SetActive(false);
                _choiceButtons[i] = btn;
            }

            _dialogueBox.OnTapCompleted += HandleTapCompleted;
            _historyPanel.gameObject.SetActive(false);
            HideAll();
        }

        private void OnEnable()
        {
            NarrativeStateMachine.Instance.EventBus.Subscribe(
                DialogueNodeChangedEvent.KEY,
                HandleDialogueNodeChanged);

            NarrativeStateMachine.Instance.EventBus.Subscribe(
                DialogueChoicesDisplayedEvent.KEY,
                HandleChoicesDisplayed);

            NarrativeStateMachine.Instance.EventBus.Subscribe(
                DialogueSceneCompleteEvent.KEY,
                HandleSceneComplete);

            TouchInputSystem.Instance.OnAdvanceDialogue += HandleAdvanceDialogue;
            TouchInputSystem.Instance.OnCancelTextAnimation += HandleCancelTextAnimation;
            TouchInputSystem.Instance.OnChoiceSelected += HandleChoiceSelectedFromInput;
            TouchInputSystem.Instance.OnNavigateChoices += HandleNavigateChoices;
            TouchInputSystem.Instance.OnShowDialogueHistory += HandleShowHistory;
        }

        private void OnDisable()
        {
            if (!NarrativeStateMachine.InstanceExists)
                return;

            NarrativeStateMachine.Instance.EventBus.Unsubscribe(
                DialogueNodeChangedEvent.KEY,
                HandleDialogueNodeChanged);

            NarrativeStateMachine.Instance.EventBus.Unsubscribe(
                DialogueChoicesDisplayedEvent.KEY,
                HandleChoicesDisplayed);

            NarrativeStateMachine.Instance.EventBus.Unsubscribe(
                DialogueSceneCompleteEvent.KEY,
                HandleSceneComplete);

            TouchInputSystem.Instance.OnAdvanceDialogue -= HandleAdvanceDialogue;
            TouchInputSystem.Instance.OnCancelTextAnimation -= HandleCancelTextAnimation;
            TouchInputSystem.Instance.OnChoiceSelected -= HandleChoiceSelectedFromInput;
            TouchInputSystem.Instance.OnNavigateChoices -= HandleNavigateChoices;
            TouchInputSystem.Instance.OnShowDialogueHistory -= HandleShowHistory;
        }

        #endregion

        #region Event Handlers

        private void HandleDialogueNodeChanged(NSMEvent e)
        {
            var evt = (DialogueNodeChangedEvent)e;
            bool isNarration = string.IsNullOrEmpty(evt.SpeakerId);
            bool isPlayer = evt.SpeakerId == "PLAYER";

            // Show canvas if hidden
            if (_currentState == UIState.Hidden)
                ShowCanvas();

            // Portrait
            _portraitDisplay.SetPortrait(evt.SpeakerId, isPlayer);

            // Speaker label
            _dialogueBox.SetSpeakerLabel(isNarration ? null : evt.SpeakerId);

            // Text
            _dialogueBox.DisplayText(evt.Text);
            _dialogueBox.SetSpeakerAnchored(isPlayer ? DialogueBox.AnchorSide.Left : DialogueBox.AnchorSide.Right);

            // Hide choices
            SetChoicesVisible(false);

            _currentState = UIState.ShowingText;
        }

        private void HandleChoicesDisplayed(NSMEvent e)
        {
            var evt = (DialogueChoicesDisplayedEvent)e;
            var choices = evt.Choices;

            _dialogueBox.HideTapIndicator();
            SetChoices(choices);
            SetChoicesVisible(true);

            _focusedChoiceIndex = 0;
            UpdateChoiceFocus();

            _currentState = UIState.ShowingChoices;
        }

        private void HandleSceneComplete(NSMEvent e)
        {
            HideCanvas();
        }

        private void HandleTapCompleted()
        {
            OnAdvanceDialogue?.Invoke();
        }

        private void HandleAdvanceDialogue()
        {
            if (_currentState == UIState.Hidden || _currentState == UIState.HistoryOpen)
                return;

            if (_currentState == UIState.ShowingText)
            {
                OnAdvanceDialogue?.Invoke();
            }
        }

        private void HandleCancelTextAnimation()
        {
            if (_currentState == UIState.ShowingText)
            {
                _dialogueBox.SkipAnimation();
                OnCancelTextAnimation?.Invoke();
            }
        }

        private void HandleChoiceSelectedFromInput(int index)
        {
            if (_currentState == UIState.ShowingChoices)
            {
                OnChoiceSelected?.Invoke(index);
            }
        }

        private void HandleNavigateChoices(int direction)
        {
            if (_currentState != UIState.ShowingChoices)
                return;

            int newIndex = _focusedChoiceIndex + direction;
            if (newIndex >= 0 && newIndex < _choiceButtons.Length)
            {
                _focusedChoiceIndex = newIndex;
                UpdateChoiceFocus();
                OnNavigateChoices?.Invoke(direction);
            }
        }

        private void HandleShowHistory()
        {
            if (_currentState == UIState.ShowingText)
            {
                ShowHistory();
            }
        }

        #endregion

        #region Choice Management

        private void SetChoices(ChoiceData[] choices)
        {
            // Deactivate all first
            for (int i = 0; i < MAX_VISIBLE_CHOICES; i++)
            {
                _choiceButtons[i].gameObject.SetActive(i < choices.Length);
                if (i < choices.Length)
                {
                    int idx = i;
                    _choiceButtons[i].Setup(choices[i].Text, () => SelectChoice(idx));
                }
            }
        }

        private void SetChoicesVisible(bool visible)
        {
            _choiceContainer.gameObject.SetActive(visible);
        }

        private void UpdateChoiceFocus()
        {
            for (int i = 0; i < _choiceButtons.Length; i++)
            {
                _choiceButtons[i].SetFocus(i == _focusedChoiceIndex);
            }
        }

        private void SelectChoice(int index)
        {
            SetChoicesVisible(false);
            _currentState = UIState.ShowingText;
            OnChoiceSelected?.Invoke(index);
        }

        #endregion

        #region History

        private void ShowHistory()
        {
            var history = DialogueEngine.Instance.ChoiceHistory;
            _historyPanel.Populate(history);
            _historyPanel.gameObject.SetActive(true);
            _historyPanel.OnClose += CloseHistory;
            _currentState = UIState.HistoryOpen;
        }

        private void CloseHistory()
        {
            _historyPanel.OnClose -= CloseHistory;
            _historyPanel.gameObject.SetActive(false);
            _currentState = UIState.ShowingText;
        }

        #endregion

        #region Show/Hide

        private void ShowCanvas()
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }

        private void HideCanvas()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _currentState = UIState.Hidden;
        }

        private void HideAll()
        {
            _dialogueBox.HideAll();
            SetChoicesVisible(false);
            if (_historyPanel != null)
                _historyPanel.gameObject.SetActive(false);
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
        }

        #endregion
    }
}
