using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Core.Narrative;

namespace UI.Notifications
{
    /// <summary>
    /// Individual toast notification UI component.
    /// Displays trust changes, danger warnings, and crisis alerts with appropriate styling.
    /// Supports tap-to-dismiss and auto-dismiss animations.
    /// Implements S3-2: Notification System.
    /// </summary>
    public class TrustToast : MonoBehaviour
    {
        #region Toast Types

        /// <summary>
        /// Categorizes toast urgency level for styling and animation.
        /// </summary>
        public enum ToastType
        {
            /// <summary>Positive or negative trust change from choices.</summary>
            Trust,

            /// <summary>Trust meter entered danger zone (25 or below).</summary>
            Danger,

            /// <summary>Trust meter entered crisis zone (15 or below).</summary>
            Crisis
        }

        #endregion

        #region Inspector References

        [Header("Root")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Trust Toast Content")]
        [SerializeField] private TextMeshProUGUI _trustLabel;
        [SerializeField] private Image _iconBackground;

        [Header("Danger/Crisis Content")]
        [SerializeField] private TextMeshProUGUI _warningLabel;
        [SerializeField] private GameObject _trustContentRoot;
        [SerializeField] private GameObject _warningContentRoot;

        [Header("Colors - Trust")]
        [SerializeField] private Color _imperialColor = new Color(0.82f, 0.60f, 0.35f); // Dusty Ochre
        [SerializeField] private Color _undergroundColor = new Color(0.35f, 0.58f, 0.53f); // Muted Jade

        [Header("Colors - Warning")]
        [SerializeField] private Color _dangerColor = new Color(0.85f, 0.55f, 0.2f); // Amber
        [SerializeField] private Color _crisisColor = new Color(0.75f, 0.2f, 0.2f); // Red

        [Header("Animation")]
        [SerializeField] private float _slideInDuration = 0.3f;
        [SerializeField] private float _slideOutDuration = 0.2f;

        #endregion

        #region Private Fields

        private ToastType _currentType;
        private bool _isVisible;
        private bool _isAnimating;
        private Action<TrustToast> _onDismissed;

        #endregion

        #region Properties

        /// <summary>
        /// Whether this toast is currently visible to the user.
        /// </summary>
        public bool IsVisible => _isVisible;

        /// <summary>
        /// Whether this toast is mid-animation.
        /// </summary>
        public bool IsAnimating => _isAnimating;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Start hidden
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _trustContentRoot.SetActive(false);
            _warningContentRoot.SetActive(false);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Configure this toast as a trust change notification.
        /// </summary>
        /// <param name="deltaImperial">Change in Imperial trust (positive = gain).</param>
        /// <param name="deltaUnderground">Change in Underground trust (positive = gain).</param>
        /// <param name="onDismissed">Callback when toast is fully dismissed.</param>
        public void ConfigureAsTrust(float deltaImperial, float deltaUnderground, Action<TrustToast> onDismissed)
        {
            _currentType = ToastType.Trust;
            _onDismissed = onDismissed;

            // Determine dominant color based on larger delta
            Color toastColor;
            string labelText;
            if (Mathf.Abs(deltaImperial) >= Mathf.Abs(deltaUnderground))
            {
                toastColor = deltaImperial >= 0 ? _imperialColor : _imperialColor * 0.7f;
                labelText = FormatTrustDelta(deltaImperial, "Imperial!");
            }
            else
            {
                toastColor = deltaUnderground >= 0 ? _undergroundColor : _undergroundColor * 0.7f;
                labelText = FormatTrustDelta(deltaUnderground, "Underground!");
            }

            _iconBackground.color = toastColor;
            _trustLabel.text = labelText;
            _trustLabel.color = deltaImperial >= 0 || deltaUnderground >= 0
                ? Color.white
                : new Color(1f, 0.8f, 0.8f); // Slightly red tint for negative

            _trustContentRoot.SetActive(true);
            _warningContentRoot.SetActive(false);
        }

        /// <summary>
        /// Configure this toast as a danger zone warning.
        /// </summary>
        /// <param name="meterName">Which trust meter triggered (Imperial or Underground).</param>
        /// <param name="currentValue">Current trust value.</param>
        /// <param name="onDismissed">Callback when toast is fully dismissed.</param>
        public void ConfigureAsDanger(string meterName, float currentValue, Action<TrustToast> onDismissed)
        {
            _currentType = ToastType.Danger;
            _onDismissed = onDismissed;

            _iconBackground.color = _dangerColor;
            _warningLabel.text = $"DANGER: {meterName} Trust Critical ({currentValue:F0})";
            _warningLabel.color = Color.white;

            _trustContentRoot.SetActive(false);
            _warningContentRoot.SetActive(true);
        }

        /// <summary>
        /// Configure this toast as a crisis zone alert.
        /// </summary>
        /// <param name="meterName">Which trust meter triggered.</param>
        /// <param name="currentValue">Current trust value.</param>
        /// <param name="onDismissed">Callback when toast is fully dismissed.</param>
        public void ConfigureAsCrisis(string meterName, float currentValue, Action<TrustToast> onDismissed)
        {
            _currentType = ToastType.Crisis;
            _onDismissed = onDismissed;

            _iconBackground.color = _crisisColor;
            _warningLabel.text = $"CRISIS: {meterName} Trust Critical ({currentValue:F0})";
            _warningLabel.color = Color.white;

            _trustContentRoot.SetActive(false);
            _warningContentRoot.SetActive(true);
        }

        /// <summary>
        /// Show the toast with slide-in animation.
        /// </summary>
        public void Show()
        {
            if (_isVisible || _isAnimating)
                return;

            gameObject.SetActive(true);
            _isAnimating = true;
            StartCoroutine(AnimateSlideIn());
        }

        /// <summary>
        /// Dismiss the toast with slide-out animation.
        /// </summary>
        public void Dismiss()
        {
            if (!_isVisible || _isAnimating)
                return;

            _isAnimating = true;
            StartCoroutine(AnimateSlideOut());
        }

        /// <summary>
        /// Immediately hide without animation (for queue overflow).
        /// </summary>
        public void HideImmediately()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _isVisible = false;
            _isAnimating = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Handle tap input - dismisses the toast.
        /// </summary>
        public void OnToastTapped()
        {
            if (_isVisible && !_isAnimating)
            {
                Dismiss();
            }
        }

        #endregion

        #region Animation

        private System.Collections.IEnumerator AnimateSlideIn()
        {
            // Slide in from top
            var rectTransform = (RectTransform)transform;
            Vector2 startPos = new Vector2(0, rectTransform.anchoredPosition.y);
            Vector2 endPos = Vector2.zero;

            // Start position above view
            rectTransform.anchoredPosition = new Vector2(0, 200f);

            float elapsed = 0f;
            while (elapsed < _slideInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / _slideInDuration);

                rectTransform.anchoredPosition = Vector2.Lerp(new Vector2(0, 200f), endPos, t);
                _canvasGroup.alpha = t;

                yield return null;
            }

            rectTransform.anchoredPosition = endPos;
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _isVisible = true;
            _isAnimating = false;
        }

        private System.Collections.IEnumerator AnimateSlideOut()
        {
            _canvasGroup.blocksRaycasts = false;

            var rectTransform = (RectTransform)transform;
            Vector2 startPos = rectTransform.anchoredPosition;
            Vector2 endPos = new Vector2(0, 200f); // Slide up and out

            float elapsed = 0f;
            while (elapsed < _slideOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / _slideOutDuration);

                rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                _canvasGroup.alpha = 1f - t;

                yield return null;
            }

            HideImmediately();
            _onDismissed?.Invoke(this);
        }

        #endregion

        #region Helpers

        private static string FormatTrustDelta(float delta, string faction)
        {
            string sign = delta >= 0 ? "+" : "";
            return $"{sign}{delta:F0} {faction}";
        }

        #endregion
    }
}
