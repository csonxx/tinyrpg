// PROTOTYPE - NOT FOR PRODUCTION
// Question: Dialogue UI implementation
// Date: 2026-04-29

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI.Dialogue
{
    /// <summary>
    /// Individual choice button with focus highlight and minimum 60px touch target.
    /// </summary>
    public class ChoiceButton : MonoBehaviour
    {
        #region Inspector References

        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _choiceText;
        [SerializeField] private RectTransform _buttonRect;
        [SerializeField] private Image _focusBorder;

        [Header("Focus State")]
        [SerializeField] private float _focusedScale = 1.05f;
        [SerializeField] private Color _normalBorderColor = new Color(0.8f, 0.75f, 0.7f, 0.5f);
        [SerializeField] private Color _focusedBorderColor = new Color(0.96f, 0.87f, 0.7f, 1f);

        #endregion

        #region Private Fields

        private Action _onSelected;
        private bool _isFocused;
        private Vector3 _normalScale;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _normalScale = _buttonRect.localScale;
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => _onSelected?.Invoke());
            SetFocus(false);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Configures the button with choice text and selection callback.
        /// </summary>
        public void Setup(string text, Action onSelected)
        {
            _choiceText.text = text;
            _onSelected = onSelected;
        }

        /// <summary>
        /// Sets the focus state. Focused buttons are slightly larger with highlighted border.
        /// </summary>
        public void SetFocus(bool focused)
        {
            _isFocused = focused;
            _buttonRect.localScale = focused ? _normalScale * _focusedScale : _normalScale;
            _focusBorder.color = focused ? _focusedBorderColor : _normalBorderColor;
        }

        #endregion
    }
}
