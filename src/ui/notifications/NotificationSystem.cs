using System;
using System.Collections.Generic;
using UnityEngine;
using Core.Narrative;

namespace UI.Notifications
{
    /// <summary>
    /// Manages the notification toast queue and display.
    /// Subscribes to trust events and displays appropriate toasts.
    /// Implements S3-2: Notification System.
    ///
    /// AC1: Trust change toast appears on every CHOICE selection showing delta
    /// AC2: Danger zone (25 or below) triggers amber pulse warning toast
    /// AC3: Crisis zone (15 or below) triggers red flash alert toast
    /// AC4: Notifications queue and display sequentially (max 3 visible at once)
    /// AC5: Toast auto-dismisses after 2 seconds or on tap
    /// </summary>
    public class NotificationSystem : MonoBehaviour
    {
        #region Constants

        /// <summary>
        /// Maximum number of toasts visible at once. Enforces queue overflow.
        /// </summary>
        private const int MAX_VISIBLE_TOASTS = 3;

        /// <summary>
        /// Auto-dismiss delay in seconds.
        /// </summary>
        private const float AUTO_DISMISS_DELAY = 2f;

        #endregion

        #region Inspector References

        [Header("Toast Pool")]
        [SerializeField] private TrustToast _toastPrefab;
        [SerializeField] private Transform _toastContainer;

        [Header("Layout")]
        [SerializeField] private float _toastSpacing = 10f;
        [SerializeField] private float _slideOffset = 200f;

        #endregion

        #region Private Fields

        private readonly Queue<TrustToast> _pendingToasts = new Queue<TrustToast>();
        private readonly List<TrustToast> _visibleToasts = new List<TrustToast>();
        private readonly List<TrustToast> _toastPool = new List<TrustToast>();

        private bool _isShowingToast;
        private Coroutine _autoDismissCoroutine;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Start()
        {
            // Pre-warm the pool
            WarmToastPool(MAX_VISIBLE_TOASTS);
        }

        #endregion

        #region Event Subscription

        private void SubscribeToEvents()
        {
            NarrativeStateMachine.Instance.Subscribe(TrustShiftAppliedEvent.KEY, HandleTrustShiftApplied);
            NarrativeStateMachine.Instance.Subscribe(DangerZoneEnteredEvent.KEY, HandleDangerZoneEntered);
            NarrativeStateMachine.Instance.Subscribe(CrisisEnteredEvent.KEY, HandleCrisisEntered);
        }

        private void UnsubscribeFromEvents()
        {
            if (!NarrativeStateMachine.InstanceExists)
                return;

            NarrativeStateMachine.Instance.Unsubscribe(TrustShiftAppliedEvent.KEY, HandleTrustShiftApplied);
            NarrativeStateMachine.Instance.Unsubscribe(DangerZoneEnteredEvent.KEY, HandleDangerZoneEntered);
            NarrativeStateMachine.Instance.Unsubscribe(CrisisEnteredEvent.KEY, HandleCrisisEntered);
        }

        #endregion

        #region Event Handlers

        private void HandleTrustShiftApplied(NSMEvent e)
        {
            var evt = (TrustShiftAppliedEvent)e;

            // Only show non-secret trust shifts (secret shifts are not shown to player per design)
            if (evt.IsSecret)
                return;

            // Create toast for trust change
            var toast = GetPooledToast();
            toast.ConfigureAsTrust(evt.DeltaImperial, evt.DeltaUnderground, OnToastDismissed);
            EnqueueToast(toast);
        }

        private void HandleDangerZoneEntered(NSMEvent e)
        {
            var evt = (DangerZoneEnteredEvent)e;

            var toast = GetPooledToast();
            toast.ConfigureAsDanger(evt.MeterName, evt.CurrentValue, OnToastDismissed);
            EnqueueToast(toast);
        }

        private void HandleCrisisEntered(NSMEvent e)
        {
            var evt = (CrisisEnteredEvent)e;

            var toast = GetPooledToast();
            toast.ConfigureAsCrisis(evt.MeterName, evt.CurrentValue, OnToastDismissed);
            EnqueueToast(toast);
        }

        #endregion

        #region Queue Management

        /// <summary>
        /// Add a toast to the queue. If space available, show immediately.
        /// If queue is full (3 visible), the oldest visible toast is dismissed to make room.
        /// </summary>
        private void EnqueueToast(TrustToast toast)
        {
            // If we have max visible, dismiss oldest to make room
            if (_visibleToasts.Count >= MAX_VISIBLE_TOASTS)
            {
                TrustToast oldest = _visibleToasts[0];
                oldest.Dismiss();
                // It will be moved to pool in OnToastDismissed callback
            }

            _pendingToasts.Enqueue(toast);
            ProcessQueue();
        }

        /// <summary>
        /// Process the pending queue, showing toasts one at a time.
        /// </summary>
        private void ProcessQueue()
        {
            if (_isShowingToast || _pendingToasts.Count == 0)
                return;

            TrustToast next = _pendingToasts.Dequeue();
            ShowToast(next);
        }

        /// <summary>
        /// Display a toast with proper positioning.
        /// </summary>
        private void ShowToast(TrustToast toast)
        {
            _isShowingToast = true;

            // Position toast based on current visible count
            int stackIndex = _visibleToasts.Count;
            Vector3 position = toast.transform.localPosition;
            position.y = -stackIndex * (_toastPrefab.GetComponent<RectTransform>().sizeDelta.y + _toastSpacing);
            toast.transform.localPosition = position;

            toast.Show();
            _visibleToasts.Add(toast);

            // Start auto-dismiss timer
            if (_autoDismissCoroutine != null)
                StopCoroutine(_autoDismissCoroutine);
            _autoDismissCoroutine = StartCoroutine(AutoDismissAfterDelay(toast));
        }

        /// <summary>
        /// Coroutine that auto-dismisses a toast after the configured delay.
        /// </summary>
        private System.Collections.IEnumerator AutoDismissAfterDelay(TrustToast toast)
        {
            yield return new WaitForSeconds(AUTO_DISMISS_DELAY);

            if (toast != null && toast.IsVisible && !toast.IsAnimating)
            {
                toast.Dismiss();
            }
        }

        /// <summary>
        /// Callback when a toast is dismissed (either by user tap or auto-dismiss).
        /// </summary>
        private void OnToastDismissed(TrustToast toast)
        {
            _visibleToasts.Remove(toast);
            ReturnToPool(toast);

            _isShowingToast = false;

            // Reposition remaining toasts
            RepositionVisibleToasts();

            // Process next in queue
            ProcessQueue();
        }

        /// <summary>
        /// Reposition visible toasts to fill gaps after dismissal.
        /// </summary>
        private void RepositionVisibleToasts()
        {
            for (int i = 0; i < _visibleToasts.Count; i++)
            {
                var toast = _visibleToasts[i];
                Vector3 position = toast.transform.localPosition;
                position.y = -i * (toast.GetComponent<RectTransform>().sizeDelta.y + _toastSpacing);
                toast.transform.localPosition = position;
            }
        }

        #endregion

        #region Object Pool

        /// <summary>
        /// Pre-warm the toast pool with the specified count.
        /// </summary>
        private void WarmToastPool(int count)
        {
            for (int i = 0; i < count; i++)
            {
                CreatePooledToast();
            }
        }

        /// <summary>
        /// Get a toast from the pool, or create a new one if pool is empty.
        /// </summary>
        private TrustToast GetPooledToast()
        {
            TrustToast toast;

            if (_toastPool.Count > 0)
            {
                toast = _toastPool[_toastPool.Count - 1];
                _toastPool.RemoveAt(_toastPool.Count - 1);
            }
            else
            {
                toast = CreatePooledToast();
            }

            toast.gameObject.SetActive(false);
            return toast;
        }

        /// <summary>
        /// Create a new pooled toast instance.
        /// </summary>
        private TrustToast CreatePooledToast()
        {
            var toast = Instantiate(_toastPrefab, _toastContainer);
            _toastPool.Add(toast);
            toast.gameObject.SetActive(false);
            return toast;
        }

        /// <summary>
        /// Return a toast to the pool after dismissal.
        /// </summary>
        private void ReturnToPool(TrustToast toast)
        {
            toast.HideImmediately();
            _toastPool.Add(toast);
        }

        #endregion

        #region Public API (for testing)

        /// <summary>
        /// Number of toasts currently visible.
        /// </summary>
        public int VisibleCount => _visibleToasts.Count;

        /// <summary>
        /// Number of toasts waiting in queue.
        /// </summary>
        public int QueuedCount => _pendingToasts.Count;

        /// <summary>
        /// Number of toasts in the pool.
        /// </summary>
        public int PooledCount => _toastPool.Count;

        #endregion
    }
}
