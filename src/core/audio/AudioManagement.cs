using System;
using Core.Narrative;
using UnityEngine;

namespace Core.Audio
{
    /// <summary>
    /// Core audio manager that handles BGM, SFX, and Voice playback.
    /// Implements event-driven audio with pause/resume on NSM state changes.
    ///
    /// Implements S2-4 per design/gdd/audio-management.md.
    /// </summary>
    public sealed class AudioManagement : MonoBehaviour
    {
        #region Singleton

        private static AudioManagement _instance;
        private static readonly object _lock = new object();

        public static AudioManagement Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            var go = new GameObject("AudioManagement");
                            _instance = go.AddComponent<AudioManagement>();
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Constants

        /// <summary>
        /// Fade-out duration in milliseconds. GDD tuning knob.
        /// </summary>
        public const int BGM_FADE_DURATION_MS = 500;

        #endregion

        #region State

        private bool _isPaused;
        private string _currentBGM;
        private float _targetBGMVolume = 1.0f;

        // Volume multipliers hardcoded for MVP (Settings System is separate story)
        private float _masterVolume = 1.0f;
        private float _bgmVolume = 1.0f;
        private float _sfxVolume = 1.0f;
        private float _voiceVolume = 1.0f;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Event Subscription

        private void SubscribeToEvents()
        {
            // Subscribe to global EventBus for audio events
            EventBus.Instance.Subscribe(SceneReadyEvent.KEY, OnSceneReady);
            EventBus.Instance.Subscribe(SFXPlayEvent.KEY, OnSFXPlay);
            EventBus.Instance.Subscribe(VoicePlayEvent.KEY, OnVoicePlay);

            // Subscribe to NSM state changes via NSM's EventBus
            NarrativeStateMachine.Instance.EventBus.Subscribe("nsm.state", OnNSMStateChanged);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Instance.Unsubscribe(SceneReadyEvent.KEY, OnSceneReady);
            EventBus.Instance.Unsubscribe(SFXPlayEvent.KEY, OnSFXPlay);
            EventBus.Instance.Unsubscribe(VoicePlayEvent.KEY, OnVoicePlay);

            if (NarrativeStateMachine.Instance != null)
            {
                NarrativeStateMachine.Instance.EventBus.Unsubscribe("nsm.state", OnNSMStateChanged);
            }
        }

        #endregion

        #region Event Handlers

        private void OnSceneReady(NSMEvent e)
        {
            if (e is SceneReadyEvent evt)
            {
                PlayBGM(evt.SceneMusic);
            }
        }

        private void OnSFXPlay(NSMEvent e)
        {
            if (e is SFXPlayEvent evt)
            {
                PlaySFX(evt.SFXKey);
            }
        }

        private void OnVoicePlay(NSMEvent e)
        {
            if (e is VoicePlayEvent evt)
            {
                PlayVoice(evt.VoiceKey);
            }
        }

        private void OnNSMStateChanged(NSMEvent e)
        {
            if (e is StateChangedEvent stateEvent)
            {
                HandleStateChange(stateEvent.NewState);
            }
        }

        private void HandleStateChange(NSMState newState)
        {
            switch (newState)
            {
                case NSMState.MENU_OPEN:
                case NSMState.CUTSCENE:
                    Pause();
                    break;

                case NSMState.DIALOGUE_ACTIVE:
                case NSMState.SCENE_ACTIVE:
                    Resume();
                    break;

                // Other states: TITLE, CHAPTER_LOADING, CHAPTER_COMPLETE, ERROR
                // No explicit pause/resume action needed
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Play BGM by key. If same BGM is already playing, do nothing.
        /// If another BGM is playing, fade out current then fade in new.
        /// </summary>
        public void PlayBGM(string bgmKey)
        {
            if (string.IsNullOrEmpty(bgmKey))
            {
                Debug.Log("[AudioManagement] PlayBGM called with null/empty key, ignoring.");
                return;
            }

            if (_currentBGM == bgmKey && !_isPaused)
            {
                Debug.Log($"[AudioManagement] BGM '{bgmKey}' already playing, ignoring.");
                return;
            }

            StopBGM();
            _currentBGM = bgmKey;
            var volume = GetEffectiveBGMVolume();
            Debug.Log($"[AudioManagement] PlayBGM: key='{bgmKey}', volume={volume:F2}");
        }

        /// <summary>
        /// Stop current BGM with fade-out over BGM_FADE_DURATION_MS.
        /// </summary>
        public void StopBGM()
        {
            if (string.IsNullOrEmpty(_currentBGM))
            {
                return;
            }

            var fadingBGM = _currentBGM;
            Debug.Log($"[AudioManagement] StopBGM: fading out '{fadingBGM}' over {BGM_FADE_DURATION_MS}ms");

            // In a real implementation, this would trigger a coroutine for smooth fade
            // For MVP mock, we just log and clear immediately
            _currentBGM = null;

            // Emit fade complete event
            EventBus.Instance.Emit(new BGMFadeCompleteEvent(fadingBGM));
        }

        /// <summary>
        /// Play a sound effect by key.
        /// </summary>
        public void PlaySFX(string sfxKey)
        {
            if (string.IsNullOrEmpty(sfxKey))
            {
                return;
            }

            var volume = GetEffectiveSFXVolume();
            Debug.Log($"[AudioManagement] PlaySFX: key='{sfxKey}', volume={volume:F2}");
        }

        /// <summary>
        /// Play a voice line by key.
        /// </summary>
        public void PlayVoice(string voiceKey)
        {
            if (string.IsNullOrEmpty(voiceKey))
            {
                return;
            }

            var volume = GetEffectiveVoiceVolume();
            Debug.Log($"[AudioManagement] PlayVoice: key='{voiceKey}', volume={volume:F2}");
        }

        /// <summary>
        /// Stop current voice line.
        /// </summary>
        public void StopVoice()
        {
            Debug.Log("[AudioManagement] StopVoice");
        }

        /// <summary>
        /// Pause all audio playback.
        /// </summary>
        public void Pause()
        {
            if (_isPaused)
            {
                return;
            }

            _isPaused = true;
            Debug.Log($"[AudioManagement] Pause (BGM '{_currentBGM}' paused)");
        }

        /// <summary>
        /// Resume all audio playback.
        /// </summary>
        public void Resume()
        {
            if (!_isPaused)
            {
                return;
            }

            _isPaused = false;
            Debug.Log($"[AudioManagement] Resume (BGM '{_currentBGM}' resumed)");
        }

        /// <summary>
        /// Stop all audio immediately without fade.
        /// </summary>
        public void StopAll()
        {
            StopBGM();
            StopVoice();
            Debug.Log("[AudioManagement] StopAll");
        }

        #endregion

        #region Volume Helpers

        private float GetEffectiveBGMVolume() => _masterVolume * _bgmVolume;
        private float GetEffectiveSFXVolume() => _masterVolume * _sfxVolume;
        private float GetEffectiveVoiceVolume() => _masterVolume * _voiceVolume;

        #endregion
    }
}
