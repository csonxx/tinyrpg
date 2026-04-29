// PROTOTYPE - NOT FOR PRODUCTION
// Question: Dialogue UI implementation
// Date: 2026-04-29

using UnityEngine;
using UnityEngine.UI;

namespace UI.Dialogue
{
    /// <summary>
    /// Manages character portrait display: anchors left for player, right for NPC,
    /// with 200ms ease-in-out fade-in on scene entry.
    /// </summary>
    public class PortraitDisplay : MonoBehaviour
    {
        #region Inspector References

        [SerializeField] private Image _portraitImage;
        [SerializeField] private RectTransform _portraitRect;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Anchors")]
        [SerializeField] private bool _playerAnchoredLeft = true;

        #endregion

        #region Configuration

        [Header("Animation")]
        [SerializeField] private float _fadeInDuration = 0.2f;

        #endregion

        #region Private Fields

        private float _fadeTimer;
        private bool _isAnimating;
        private float _targetAlpha;
        private string _currentCharacterId;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _canvasGroup.alpha = 0f;
        }

        private void Update()
        {
            if (!_isAnimating)
                return;

            _fadeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_fadeTimer / _fadeInDuration);
            float eased = EaseOut(t);
            _canvasGroup.alpha = Mathf.Lerp(0f, _targetAlpha, eased);

            if (t >= 1f)
            {
                _isAnimating = false;
                _canvasGroup.alpha = _targetAlpha;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Sets the portrait for the given characterId.
        /// Anchors left for PLAYER, right for all other IDs.
        /// </summary>
        public void SetPortrait(string characterId, bool isPlayer)
        {
            _currentCharacterId = characterId;

            // Anchor: player = left, NPC = right
            if (isPlayer && _playerAnchoredLeft)
            {
                _portraitRect.anchorMin = new Vector2(0f, 0.35f);
                _portraitRect.anchorMax = new Vector2(0.35f, 0.95f);
            }
            else
            {
                _portraitRect.anchorMin = new Vector2(0.65f, 0.35f);
                _portraitRect.anchorMax = new Vector2(1f, 0.95f);
            }

            _portraitRect.pivot = new Vector2(0.5f, 0.5f);
            _portraitRect.anchoredPosition = Vector2.zero;
            _portraitRect.sizeDelta = Vector2.zero;

            // Placeholder: colored rectangle. Portrait sprite lookup deferred to character asset system.
            _portraitImage.color = GetPlaceholderColor(characterId);

            // Fade in
            _targetAlpha = 1f;
            _fadeTimer = 0f;
            _isAnimating = true;
        }

        /// <summary>
        /// Hides the portrait.
        /// </summary>
        public void Hide()
        {
            _targetAlpha = 0f;
            _fadeTimer = 0f;
            _isAnimating = true;
        }

        #endregion

        #region Private Helpers

        private Color GetPlaceholderColor(string characterId)
        {
            // Distinct placeholder colors per character for prototyping
            if (characterId == "PLAYER")
                return new Color(0.4f, 0.5f, 0.6f, 1f);
            if (characterId == "YAMAMOTO")
                return new Color(0.6f, 0.3f, 0.3f, 1f);
            if (characterId == "LIU")
                return new Color(0.3f, 0.5f, 0.4f, 1f);
            if (characterId == "ZHANG")
                return new Color(0.5f, 0.4f, 0.3f, 1f);
            return new Color(0.5f, 0.5f, 0.5f, 1f);
        }

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);

        private static class Mathf
        {
            public static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
            public static float Lerp(float a, float b, float t) => a + (b - a) * t;
        }

        #endregion
    }
}
