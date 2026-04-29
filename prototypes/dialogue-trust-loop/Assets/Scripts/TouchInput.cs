// PROTOTYPE - NOT FOR PRODUCTION
// Date: 2026-04-29

using UnityEngine;

/// <summary>
/// Simplified touch input for prototype.
/// Detects tap on screen and routes to current UI handler.
/// </summary>
public class TouchInput : MonoBehaviour
{
    private float _tapMaxDuration = 0.3f;
    private float _tapMaxMovement = 20f;

    private Touch _touch;
    private Vector2 _touchDownPos;
    private float _touchDownTime;
    private bool _isTouchDown = false;

    private void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        // Editor/stub: use mouse as proxy for tap
        if (Input.GetMouseButtonDown(0))
        {
            _touchDownPos = Input.mousePosition;
            _touchDownTime = Time.time;
            _isTouchDown = true;
        }
        else if (Input.GetMouseButtonUp(0) && _isTouchDown)
        {
            _isTouchDown = false;
            float duration = Time.time - _touchDownTime;
            float movement = Vector2.Distance(_touchDownPos, (Vector2)Input.mousePosition);

            if (duration <= _tapMaxDuration && movement <= _tapMaxMovement)
            {
                OnTap(Input.mousePosition);
            }
        }
#else
        // Mobile: real touch
        if (Input.touchCount > 0)
        {
            _touch = Input.GetTouch(0);

            if (_touch.phase == TouchPhase.Began)
            {
                _touchDownPos = _touch.position;
                _touchDownTime = Time.time;
                _isTouchDown = true;
            }
            else if (_touch.phase == TouchPhase.Ended && _isTouchDown)
            {
                _isTouchDown = false;
                float duration = Time.time - _touchDownTime;
                float movement = Vector2.Distance(_touchDownPos, _touch.position);

                if (duration <= _tapMaxDuration && movement <= _tapMaxMovement)
                {
                    OnTap(_touch.position);
                }
            }
        }
#endif
    }

    private void OnTap(Vector2 screenPos)
    {
        Debug.Log($"[TouchInput] Tap at {screenPos}");
        FindObjectOfType<PrototypeScene>().OnScreenTapped();
    }
}
