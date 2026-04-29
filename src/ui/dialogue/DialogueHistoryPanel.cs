// PROTOTYPE - NOT FOR PRODUCTION
// Question: Dialogue UI implementation
// Date: 2026-04-29

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Core.Narrative.Dialogue;
using TMPro;

namespace UI.Dialogue
{
    /// <summary>
    /// Dialogue history panel — slides in from right on swipe,
    /// shows last 20 dialogue entries, dismisses on tap outside.
    /// </summary>
    public class DialogueHistoryPanel : MonoBehaviour
    {
        #region Inspector References

        [SerializeField] private RectTransform _panelRect;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Transform _entryContainer;
        [SerializeField] private GameObject _entryPrefab;

        [Header("Animation")]
        [SerializeField] private float _slideInDuration = 0.3f;
        [SerializeField] private float _slideOutDuration = 0.2f;

        #endregion

        #region Configuration

        private const int MAX_HISTORY_ENTRIES = 20;

        #endregion

        #region Events

        public event Action OnClose;

        #endregion

        #region Private Fields

        private bool _isAnimating;
        private float _animationTimer;
        private float _animationDuration;
        private bool _isSlidingIn;
        private Vector2 _hiddenPosition = new Vector2(2000f, 0f);
        private Vector2 _visiblePosition = Vector2.zero;
        private Vector2 _startPosition;
        private Vector2 _targetPosition;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _panelRect.anchoredPosition = _hiddenPosition;
            _canvasGroup.alpha = 0f;
            _closeButton.onClick.AddListener(() => OnClose?.Invoke());
        }

        private void Update()
        {
            if (!_isAnimating)
                return;

            _animationTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_animationTimer / _animationDuration);
            float eased = _isSlidingIn ? EaseOut(t) : EaseIn(t);

            _panelRect.anchoredPosition = Vector2.Lerp(_startPosition, _targetPosition, eased);
            _canvasGroup.alpha = _isSlidingIn ? t : (1f - t);

            if (t >= 1f)
            {
                _isAnimating = false;
                if (!_isSlidingIn)
                {
                    _panelRect.anchoredPosition = _hiddenPosition;
                    _canvasGroup.alpha = 0f;
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Populates the history panel with the given choice history.
        /// </summary>
        public void Populate(IReadOnlyList<DialogueEngine.ChoiceRecord> history)
        {
            // Clear existing entries
            foreach (Transform child in _entryContainer)
                Destroy(child.gameObject);

            int count = Mathf.Min(history.Count, MAX_HISTORY_ENTRIES);
            for (int i = 0; i < count; i++)
            {
                var record = history[i];
                var entry = Instantiate(_entryPrefab, _entryContainer);
                var text = entry.GetComponent<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = $"[{record.ChoiceIndex + 1}] {record.ChoiceText}";
                }
            }
        }

        /// <summary>
        /// Triggers the slide-in animation.
        /// </summary>
        public void SlideIn()
        {
            _isSlidingIn = true;
            _isAnimating = true;
            _animationTimer = 0f;
            _animationDuration = _slideInDuration;
            _startPosition = _hiddenPosition;
            _targetPosition = _visiblePosition;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Triggers the slide-out animation.
        /// </summary>
        public void SlideOut()
        {
            _isSlidingIn = false;
            _isAnimating = true;
            _animationTimer = 0f;
            _animationDuration = _slideOutDuration;
            _startPosition = _panelRect.anchoredPosition;
            _targetPosition = _hiddenPosition;
        }

        #endregion

        #region Math Helpers

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
        private static float EaseIn(float t) => t * t;
        private static int Min(int a, int b) => a < b ? a : b;

        private static class Mathf
        {
            public static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
            public static float Lerp(float a, float b, float t) => a + (b - a) * t;
            public static int Min(int a, int b) => Min(a, b);
        }

        #endregion
    }
}
