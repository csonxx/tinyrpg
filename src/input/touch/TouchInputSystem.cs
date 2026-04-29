#if UNITY_2022_3_OR_NEWER
#define UNITY_2022_3_PLUS
#endif

using System;
using UnityEngine;
#if UNITY_2022_3_PLUS
using UnityEngine.InputSystem;
using Touch = UnityEngine.InputSystem.InputAction.Touchphase;
#else
using Touch = UnityEngine.Touch;
#endif

namespace Input.Touch
{
    /// <summary>
    /// Gesture type recognized by the touch input system.
    /// </summary>
    public enum Gesture
    {
        None,
        Tap,
        SwipeLeft,
        SwipeRight,
        LongPress
    }

    /// <summary>
    /// Scene context used to route touch gestures to appropriate handlers.
    /// </summary>
    public enum SceneContext
    {
        DIALOGUE_ACTIVE,
        CHOICE_ACTIVE,
        CUTSCENE,
        MENU_OPEN,
        HISTORY_OVERLAY,
        NONE
    }

    /// <summary>
    /// Input state that determines how the touch system responds to touches.
    /// </summary>
    public enum InputState
    {
        ENABLED,
        DISABLED,
        BLOCKED
    }

    /// <summary>
    /// Direction for navigation gestures.
    /// </summary>
    public enum NavigationDirection
    {
        Left,
        Right
    }

    /// <summary>
    /// Touch input gateway for 雾中誓言 (Fog-bound Oath).
    ///
    /// Recognizes tap, swipe, and long-press gestures using Unity's Input.touches API.
    /// Routes gestures to appropriate callbacks based on current SceneContext.
    ///
    /// Usage:
    ///   TouchInputSystem.Instance.SetContext(SceneContext.DIALOGUE_ACTIVE, "character_01");
    ///   TouchInputSystem.Instance.SetInputState(InputState.ENABLED);
    ///
    /// Subscribe to events:
    ///   TouchInputSystem.Instance.OnAdvanceDialogue += HandleAdvance;
    ///   TouchInputSystem.Instance.OnCancelTextAnimation += HandleCancel;
    /// </summary>
    public sealed class TouchInputSystem : MonoBehaviour
    {
        #region Gesture Thresholds

        private const float TAP_MAX_DURATION_MS = 300f;
        private const float TAP_MAX_MOVEMENT_PX = 20f;
        private const float SWIPE_MAX_DURATION_MS = 500f;
        private const float SWIPE_MIN_DISTANCE_PX = 50f;
        private const float LONG_PRESS_MIN_DURATION_MS = 600f;
        private const float LONG_PRESS_MAX_DRIFT_PX = 10f;
        private const float DOUBLE_TAP_WINDOW_MS = 300f;

        #endregion

        #region Singleton

        private static TouchInputSystem _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// Global singleton instance. Thread-safe via double-checked locking.
        /// </summary>
        public static TouchInputSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            var go = new GameObject("TouchInputSystem");
                            _instance = go.AddComponent<TouchInputSystem>();
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Fired on TAP during DIALOGUE_ACTIVE when text animation is NOT running.
        /// </summary>
        public event Action OnAdvanceDialogue;

        /// <summary>
        /// Fired on the first TAP during DIALOGUE_ACTIVE when text animation IS running.
        /// DialogueEngine subscribes to cancel the animation.
        /// </summary>
        public event Action OnCancelTextAnimation;

        /// <summary>
        /// Fired on SWIPE_LEFT or SWIPE_RIGHT during DIALOGUE_ACTIVE.
        /// </summary>
        public event Action OnShowDialogueHistory;

        /// <summary>
        /// Fired on LONG_PRESS during DIALOGUE_ACTIVE. Passes the current character ID.
        /// </summary>
        public event Action<string> OnShowCharacterInfo;

        /// <summary>
        /// Fired on TAP during CHOICE_ACTIVE. Passes the focused choice index.
        /// </summary>
        public event Action<int> OnChoiceSelected;

        /// <summary>
        /// Fired on SWIPE during CHOICE_ACTIVE. Passes navigation direction.
        /// </summary>
        public event Action<NavigationDirection> OnNavigateChoices;

        /// <summary>
        /// Fired on TAP during MENU_OPEN.
        /// </summary>
        public event Action OnActivateMenuItem;

        /// <summary>
        /// Fired on SWIPE during MENU_OPEN. Passes navigation direction.
        /// </summary>
        public event Action<NavigationDirection> OnNavigateMenu;

        #endregion

        #region Fields

        [SerializeField, Tooltip("Enable haptic feedback on tap gestures when true.")]
        private bool _hapticFeedbackEnabled = true;

        private SceneContext _currentContext = SceneContext.NONE;
        private InputState _inputState = InputState.ENABLED;
        private string _currentCharacterId;

        // Touch tracking
        private bool _isTrackingTouch;
        private float _touchStartTime;
        private Vector2 _touchStartPosition;
        private int _trackedTouchId = -1;
        private bool _longPressFired;
        private bool _wasInTextAnimationLastFrame;

        // Double-tap tracking
        private float _lastTapTime;
        private bool _consumedSecondTap;

        // Cached delegates for haptics (avoid allocation in hot path)
        private static readonly Action<object> _triggerHapticAction = TriggerHapticInternal;

        #endregion

        #region Properties

        /// <summary>
        /// Current scene context used for gesture routing.
        /// </summary>
        public SceneContext CurrentContext => _currentContext;

        /// <summary>
        /// Current input state (ENABLED, DISABLED, BLOCKED).
        /// </summary>
        public InputState CurrentInputState => _inputState;

        /// <summary>
        /// Enable or disable haptic feedback on tap.
        /// </summary>
        public bool HapticFeedbackEnabled
        {
            get => _hapticFeedbackEnabled;
            set => _hapticFeedbackEnabled = value;
        }

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
        }

        private void Update()
        {
            ProcessTouches();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Set the current scene context and optionally the active character ID.
        /// Called by DialogueEngine and other scene systems when context changes.
        /// </summary>
        /// <param name="context">The new scene context.</param>
        /// <param name="characterId">The current character ID (for DIALOGUE_ACTIVE context).</param>
        public void SetContext(SceneContext context, string characterId = null)
        {
            _currentContext = context;
            _currentCharacterId = characterId;
        }

        /// <summary>
        /// Set the input state to control how touches are processed.
        /// </summary>
        /// <param name="state">The new input state.</param>
        public void SetInputState(InputState state)
        {
            _inputState = state;
        }

        /// <summary>
        /// Called by DialogueEngine when text animation state changes.
        /// Used to determine whether TAP should cancel animation or advance dialogue.
        /// </summary>
        /// <param name="isAnimating">True if text animation is currently running.</param>
        public void SetTextAnimationState(bool isAnimating)
        {
            _wasInTextAnimationLastFrame = isAnimating;
        }

        #endregion

        #region Touch Processing

        private void ProcessTouches()
        {
            if (_inputState == InputState.DISABLED)
                return;

            // Process each touch
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        HandleTouchBegan(touch);
                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        HandleTouchHeld(touch);
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        HandleTouchEnded(touch);
                        break;
                }
            }
        }

        private void HandleTouchBegan(Touch touch)
        {
            if (_inputState == InputState.BLOCKED)
                return;

            _isTrackingTouch = true;
            _trackedTouchId = touch.fingerId;
            _touchStartTime = Time.realtimeTimeSinceStartup;
            _touchStartPosition = touch.position;
            _longPressFired = false;
            _consumedSecondTap = false;
        }

        private void HandleTouchHeld(Touch touch)
        {
            if (!_isTrackingTouch || touch.fingerId != _trackedTouchId)
                return;

            if (_inputState == InputState.BLOCKED)
                return;

            float elapsed = (Time.realtimeTimeSinceStartup - _touchStartTime) * 1000f;
            float distance = Vector2.Distance(touch.position, _touchStartPosition);

            // Check for long press (only fire once)
            if (!_longPressFired && elapsed >= LONG_PRESS_MIN_DURATION_MS && distance <= LONG_PRESS_MAX_DRIFT_PX)
            {
                _longPressFired = true;
                Gesture gesture = RecognizeGesture(_touchStartTime, _touchStartPosition, touch.position, touch.deltaTime);
                if (gesture == Gesture.LongPress)
                {
                    RouteGesture(gesture);
                }
            }
        }

        private void HandleTouchEnded(Touch touch)
        {
            if (!_isTrackingTouch || touch.fingerId != _trackedTouchId)
                return;

            _isTrackingTouch = false;
            _trackedTouchId = -1;

            if (_inputState == InputState.BLOCKED)
                return;

            float duration = (Time.realtimeTimeSinceStartup - _touchStartTime) * 1000f;
            Gesture gesture = RecognizeGesture(_touchStartTime, _touchStartPosition, touch.position, duration);

            if (gesture != Gesture.None)
            {
                RouteGesture(gesture);
            }
        }

        private Gesture RecognizeGesture(float startTime, Vector2 startPos, Vector2 endPos, float durationMs)
        {
            float duration = durationMs * 1000f;
            float distance = Vector2.Distance(endPos, startPos);
            float deltaX = endPos.x - startPos.x;

            // TAP: <= 300ms, <= 20px movement
            if (duration <= TAP_MAX_DURATION_MS && distance <= TAP_MAX_MOVEMENT_PX)
            {
                return Gesture.Tap;
            }

            // SWIPE_LEFT: <= 500ms, >= 50px left
            if (duration <= SWIPE_MAX_DURATION_MS && deltaX <= -SWIPE_MIN_DISTANCE_PX)
            {
                return Gesture.SwipeLeft;
            }

            // SWIPE_RIGHT: <= 500ms, >= 50px right
            if (duration <= SWIPE_MAX_DURATION_MS && deltaX >= SWIPE_MIN_DISTANCE_PX)
            {
                return Gesture.SwipeRight;
            }

            // LONG_PRESS: >= 600ms, <= 10px drift (already handled in HandleTouchHeld)
            // but also check on release in case touch ended before 600ms was fully recognized
            if (duration >= LONG_PRESS_MIN_DURATION_MS && distance <= LONG_PRESS_MAX_DRIFT_PX)
            {
                return Gesture.LongPress;
            }

            return Gesture.None;
        }

        private void RouteGesture(Gesture gesture)
        {
            switch (_currentContext)
            {
                case SceneContext.DIALOGUE_ACTIVE:
                    RouteDialogueGesture(gesture);
                    break;

                case SceneContext.CHOICE_ACTIVE:
                    RouteChoiceGesture(gesture);
                    break;

                case SceneContext.CUTSCENE:
                    // All touches consumed silently
                    break;

                case SceneContext.MENU_OPEN:
                    RouteMenuGesture(gesture);
                    break;

                case SceneContext.HISTORY_OVERLAY:
                    // TAP or SWIPE closes history
                    if (gesture == Gesture.Tap || gesture == Gesture.SwipeLeft || gesture == Gesture.SwipeRight)
                    {
                        // Consume silently - close history overlay
                        SetContext(SceneContext.DIALOGUE_ACTIVE, _currentCharacterId);
                    }
                    break;

                case SceneContext.NONE:
                default:
                    // No context set, ignore
                    break;
            }
        }

        private void RouteDialogueGesture(Gesture gesture)
        {
            switch (gesture)
            {
                case Gesture.Tap:
                    HandleDialogueTap();
                    break;

                case Gesture.SwipeLeft:
                case Gesture.SwipeRight:
                    OnShowDialogueHistory?.Invoke();
                    TriggerHaptic();
                    break;

                case Gesture.LongPress:
                    OnShowCharacterInfo?.Invoke(_currentCharacterId);
                    TriggerHaptic();
                    break;
            }
        }

        private void HandleDialogueTap()
        {
            float now = Time.realtimeTimeSinceStartup;
            bool withinDoubleTapWindow = (now - _lastTapTime) * 1000f <= DOUBLE_TAP_WINDOW_MS;

            // Check if this is the second tap in a double-tap sequence
            if (withinDoubleTapWindow && !_consumedSecondTap)
            {
                // Second tap - advance dialogue (only if text animation is NOT running)
                if (!_wasInTextAnimationLastFrame)
                {
                    OnAdvanceDialogue?.Invoke();
                    TriggerHaptic();
                }
                _consumedSecondTap = true;
                _lastTapTime = 0f; // Reset to prevent triple-tap
            }
            else
            {
                // First tap
                if (_wasInTextAnimationLastFrame)
                {
                    // Cancel text animation
                    OnCancelTextAnimation?.Invoke();
                    TriggerHaptic();
                }
                else
                {
                    // Text animation not running - advance dialogue immediately
                    OnAdvanceDialogue?.Invoke();
                    TriggerHaptic();
                }
                _lastTapTime = now;
            }
        }

        private void RouteChoiceGesture(Gesture gesture)
        {
            switch (gesture)
            {
                case Gesture.Tap:
                    // OnChoiceSelected with focused index (0 = first choice, etc.)
                    // The actual focused index is managed by the UI system; default to 0
                    OnChoiceSelected?.Invoke(0);
                    TriggerHaptic();
                    break;

                case Gesture.SwipeLeft:
                    OnNavigateChoices?.Invoke(NavigationDirection.Left);
                    TriggerHaptic();
                    break;

                case Gesture.SwipeRight:
                    OnNavigateChoices?.Invoke(NavigationDirection.Right);
                    TriggerHaptic();
                    break;

                case Gesture.LongPress:
                    // LONG_PRESS not used in CHOICE_ACTIVE
                    break;
            }
        }

        private void RouteMenuGesture(Gesture gesture)
        {
            switch (gesture)
            {
                case Gesture.Tap:
                    OnActivateMenuItem?.Invoke();
                    TriggerHaptic();
                    break;

                case Gesture.SwipeLeft:
                    OnNavigateMenu?.Invoke(NavigationDirection.Left);
                    TriggerHaptic();
                    break;

                case Gesture.SwipeRight:
                    OnNavigateMenu?.Invoke(NavigationDirection.Right);
                    TriggerHaptic();
                    break;

                case Gesture.LongPress:
                    // LONG_PRESS not used in MENU_OPEN
                    break;
            }
        }

        #endregion

        #region Haptic Feedback

        private void TriggerHaptic()
        {
            if (!_hapticFeedbackEnabled)
                return;

            try
            {
                // Use the newer Input System haptic API if available, fall back to legacy
#if UNITY_2022_3_PLUS
                if (Touchscreen.current != null && Touchscreen.current.touchesCount.value > 0)
                {
                    // Trigger light haptic via Input System
                    var t = InputSystem.GetDevice<Touchscreen>();
                    if (t != null)
                    {
                        InputSystem.QueueHapticEvent(t, Time.realtimeSinceStartup, 0.25f, 0.1f, 0.1f);
                    }
                }
#else
                Handheld.Vibrate();
#endif
            }
            catch (Exception)
            {
                // Haptic not supported on this platform - silently ignore
            }
        }

        private static void TriggerHapticInternal(object _)
        {
            try
            {
#if UNITY_2022_3_PLUS
                var t = InputSystem.GetDevice<Touchscreen>();
                if (t != null)
                {
                    InputSystem.QueueHapticEvent(t, Time.realtimeSinceStartup, 0.25f, 0.1f, 0.1f);
                }
#else
                Handheld.Vibrate();
#endif
            }
            catch (Exception)
            {
                // Silently ignore
            }
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        [ContextMenu("Simulate Tap (Dialogue)")]
        private void SimulateTapDialogue()
        {
            var prevContext = _currentContext;
            SetContext(SceneContext.DIALOGUE_ACTIVE, "debug_character");
            _wasInTextAnimationLastFrame = false;
            RouteDialogueGesture(Gesture.Tap);
            SetContext(prevContext);
        }

        [ContextMenu("Simulate Swipe Left (Dialogue)")]
        private void SimulateSwipeLeftDialogue()
        {
            var prevContext = _currentContext;
            SetContext(SceneContext.DIALOGUE_ACTIVE, "debug_character");
            RouteDialogueGesture(Gesture.SwipeLeft);
            SetContext(prevContext);
        }

        [ContextMenu("Simulate Long Press (Dialogue)")]
        private void SimulateLongPressDialogue()
        {
            var prevContext = _currentContext;
            SetContext(SceneContext.DIALOGUE_ACTIVE, "debug_character");
            RouteDialogueGesture(Gesture.LongPress);
            SetContext(prevContext);
        }
#endif

        #endregion
    }
}
