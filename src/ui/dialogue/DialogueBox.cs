// PROTOTYPE - NOT FOR PRODUCTION
// Question: Dialogue UI implementation
// Date: 2026-04-29

using System;
using Core.Narrative;
using Core.Settings;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI.Dialogue
{
    /// <summary>
    /// Handles the dialogue box: speaker label, animated text reveal,
    /// tap indicator, and text animation state.
    /// </summary>
    public class DialogueBox : MonoBehaviour
    {
        public enum AnchorSide { Left, Right, Center }

        #region Inspector References

        [Header("Box Background")]
        [SerializeField] private RectTransform _boxRect;
        [SerializeField] private Image _boxBackground;

        [Header("Speaker Name")]
        [SerializeField] private TextMeshProUGUI _speakerLabel;
        [SerializeField] private RectTransform _speakerLabelRect;

        [Header("Dialogue Text")]
        [SerializeField] private TextMeshProUGUI _dialogueText;

        [Header("Tap Indicator")]
        [SerializeField] private RectTransform _tapIndicator;
        [SerializeField] private CanvasGroup _tapIndicatorCanvasGroup;

        #endregion

        #region Configuration

        [Header("Animation Settings")]
        [SerializeField] private float _charDisplayTimeMs = 30f;
        [SerializeField] private float _tapIndicatorPulseSpeed = 0.8f;

        #endregion

        #region Events

        public event Action OnTapCompleted;

        #endregion

        #region Private Fields

        private string _fullText;
        private int _displayedCharCount;
        private bool _isAnimating;
        private float _animationTimer;
        private bool _isSpeakerAnchoredRight;
        private AnchorSide _currentAnchor = AnchorSide.Center;

        private float _tapIndicatorBaseY;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _speakerLabel.gameObject.SetActive(false);
            _tapIndicatorCanvasGroup.alpha = 0f;
            _fullText = string.Empty;
            _displayedCharCount = 0;
            _isAnimating = false;
            _tapIndicatorBaseY = _tapIndicator.anchoredPosition.y;
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
            if (!_isAnimating)
            {
                AnimateTapIndicator();
                return;
            }

            _animationTimer += Time.deltaTime * 1000f; // ms
            int targetChars = Mathf.FloorToInt(_animationTimer / _charDisplayTimeMs);
            targetChars = Mathf.Min(targetChars, _fullText.Length);

            if (targetChars != _displayedCharCount)
            {
                _displayedCharCount = targetChars;
                _dialogueText.text = _fullText.Substring(0, _displayedCharCount);

                if (_displayedCharCount >= _fullText.Length)
                {
                    _isAnimating = false;
                    _dialogueText.text = _fullText;
                    ShowTapIndicator();
                }
            }
        }

        #endregion

        #region Event Subscription

        private void SubscribeToEvents()
        {
            EventBus.Instance.Subscribe(TextSpeedChangedEvent.KEY, OnTextSpeedChanged);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Instance.Unsubscribe(TextSpeedChangedEvent.KEY, OnTextSpeedChanged);
        }

        private void OnTextSpeedChanged(NSMEvent e)
        {
            if (e is TextSpeedChangedEvent evt)
            {
                _charDisplayTimeMs = (int)evt.Speed;
                Debug.Log($"[DialogueBox] Text speed changed to {evt.Speed} ({_charDisplayTimeMs}ms per char)");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Displays the given text with character-by-character animation.
        /// </summary>
        public void DisplayText(string text)
        {
            _fullText = text ?? string.Empty;
            _displayedCharCount = 0;
            _dialogueText.text = string.Empty;
            _animationTimer = 0f;
            _isAnimating = true;
            HideTapIndicator();
        }

        /// <summary>
        /// Immediately completes the text animation and shows full text.
        /// </summary>
        public void SkipAnimation()
        {
            if (!_isAnimating)
                return;

            _isAnimating = false;
            _displayedCharCount = _fullText.Length;
            _dialogueText.text = _fullText;
            ShowTapIndicator();
        }

        /// <summary>
        /// Sets the speaker label. Pass null to hide (narration mode).
        /// </summary>
        public void SetSpeakerLabel(string speakerId)
        {
            if (string.IsNullOrEmpty(speakerId))
            {
                _speakerLabel.gameObject.SetActive(false);
                return;
            }

            // speakerId is used as localization key — return as-is for now
            _speakerLabel.text = speakerId;
            _speakerLabel.gameObject.SetActive(true);
        }

        /// <summary>
        /// Sets the anchor side for the dialogue box: Left (player), Right (NPC), Center (narration).
        /// </summary>
        public void SetSpeakerAnchored(AnchorSide side)
        {
            _currentAnchor = side;
            UpdateBoxAnchor();
        }

        /// <summary>
        /// Hides the tap indicator chevron.
        /// </summary>
        public void HideTapIndicator()
        {
            _tapIndicatorCanvasGroup.alpha = 0f;
        }

        /// <summary>
        /// Shows the tap indicator with pulsing animation.
        /// Fires OnTapCompleted when shown (player can now advance).
        /// </summary>
        public void ShowTapIndicator()
        {
            _tapIndicatorCanvasGroup.alpha = 1f;
            OnTapCompleted?.Invoke();
        }

        /// <summary>
        /// Hides all dialogue box elements.
        /// </summary>
        public void HideAll()
        {
            _speakerLabel.gameObject.SetActive(false);
            _dialogueText.text = string.Empty;
            _tapIndicatorCanvasGroup.alpha = 0f;
            _isAnimating = false;
        }

        #endregion

        #region Private Helpers

        private void UpdateBoxAnchor()
        {
            switch (_currentAnchor)
            {
                case AnchorSide.Left:
                    _boxRect.anchorMin = new Vector2(0f, 0f);
                    _boxRect.anchorMax = new Vector2(0.65f, 0.35f);
                    _boxRect.pivot = new Vector2(0f, 0f);
                    _speakerLabelRect.anchorMin = new Vector2(0f, 0f);
                    _speakerLabelRect.anchorMax = new Vector2(0f, 1f);
                    _speakerLabelRect.pivot = new Vector2(0f, 0.5f);
                    break;

                case AnchorSide.Right:
                    _boxRect.anchorMin = new Vector2(0.35f, 0f);
                    _boxRect.anchorMax = new Vector2(1f, 0.35f);
                    _boxRect.pivot = new Vector2(1f, 0f);
                    _speakerLabelRect.anchorMin = new Vector2(1f, 0f);
                    _speakerLabelRect.anchorMax = new Vector2(1f, 1f);
                    _speakerLabelRect.pivot = new Vector2(1f, 0.5f);
                    break;

                case AnchorSide.Center:
                    _boxRect.anchorMin = new Vector2(0.1f, 0f);
                    _boxRect.anchorMax = new Vector2(0.9f, 0.35f);
                    _boxRect.pivot = new Vector2(0.5f, 0f);
                    _speakerLabel.gameObject.SetActive(false);
                    break;
            }

            _boxRect.anchoredPosition = Vector2.zero;
            _boxRect.sizeDelta = Vector2.zero;
        }

        private void AnimateTapIndicator()
        {
            if (_tapIndicatorCanvasGroup.alpha < 0.5f)
                return;

            float pulse = (Mathf.Sin(Time.time * _tapIndicatorPulseSpeed * Mathf.PI) + 1f) * 0.5f;
            float targetAlpha = 0.5f + (pulse * 0.5f);
            _tapIndicatorCanvasGroup.alpha = targetAlpha;
        }

        #endregion
    }
}
