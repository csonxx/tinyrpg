using System;
using Core.Narrative;
using Input.Touch;
using UnityEngine;

namespace Core.Settings
{
    /// <summary>
    /// Core settings manager that loads, persists, and broadcasts user preferences.
    ///
    /// Loads settings from JSON on game start. Saves automatically on every change.
    /// Emits events for downstream systems (AudioManagement, TouchInputSystem, DialogueBox)
    /// to subscribe to.
    ///
    /// Implements S3-1 per design/gdd/settings-system.md.
    /// </summary>
    public sealed class SettingsSystem : MonoBehaviour
    {
        #region Singleton

        private static SettingsSystem _instance;
        private static readonly object _lock = new object();

        public static SettingsSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            var go = new GameObject("SettingsSystem");
                            _instance = go.AddComponent<SettingsSystem>();
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Constants

        private const string SETTINGS_FILE_NAME = "settings.json";

        #endregion

        #region State

        private SettingsData _settings;
        private string SettingsFilePath => Application.persistentDataPath + "/" + SETTINGS_FILE_NAME;

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
            DontDestroyOnLoad(gameObject);

            Load();
            ApplySettingsToSystems();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Current settings data. Never returns null.
        /// </summary>
        public SettingsData Settings => _settings ?? SettingsData.CreateDefault();

        /// <summary>
        /// Set music volume and persist.
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            if (Mathf.Approximately(Settings.MusicVolume, volume))
                return;

            Settings.MusicVolume = volume;
            SaveAndEmit();
            EventBus.Instance.Emit(new MusicVolumeChangedEvent(Settings.MusicVolume));
        }

        /// <summary>
        /// Set SFX volume and persist.
        /// </summary>
        public void SetSfxVolume(float volume)
        {
            if (Mathf.Approximately(Settings.SfxVolume, volume))
                return;

            Settings.SfxVolume = volume;
            SaveAndEmit();
            EventBus.Instance.Emit(new SFXVolumeChangedEvent(Settings.SfxVolume));
        }

        /// <summary>
        /// Set voice volume and persist.
        /// </summary>
        public void SetVoiceVolume(float volume)
        {
            if (Mathf.Approximately(Settings.VoiceVolume, volume))
                return;

            Settings.VoiceVolume = volume;
            SaveAndEmit();
            EventBus.Instance.Emit(new VoiceVolumeChangedEvent(Settings.VoiceVolume));
        }

        /// <summary>
        /// Set text speed and persist.
        /// </summary>
        public void SetTextSpeed(TextSpeed speed)
        {
            if (Settings.TextSpeed == speed)
                return;

            Settings.TextSpeed = speed;
            SaveAndEmit();
            EventBus.Instance.Emit(new TextSpeedChangedEvent(Settings.TextSpeed));
        }

        /// <summary>
        /// Set haptic feedback enabled and persist.
        /// </summary>
        public void SetHapticEnabled(bool enabled)
        {
            if (Settings.HapticEnabled == enabled)
                return;

            Settings.HapticEnabled = enabled;
            SaveAndEmit();
            EventBus.Instance.Emit(new HapticEnabledChangedEvent(Settings.HapticEnabled));
        }

        /// <summary>
        /// Set auto-advance enabled and persist.
        /// </summary>
        public void SetAutoAdvanceEnabled(bool enabled)
        {
            if (Settings.AutoAdvanceEnabled == enabled)
                return;

            Settings.AutoAdvanceEnabled = enabled;
            SaveAndEmit();
            EventBus.Instance.Emit(new AutoAdvanceChangedEvent(Settings.AutoAdvanceEnabled));
        }

        /// <summary>
        /// Force save current settings to disk immediately.
        /// </summary>
        public void ForceSave()
        {
            Save();
        }

        /// <summary>
        /// Reload settings from disk, discarding current changes.
        /// </summary>
        public void Reload()
        {
            Load();
            ApplySettingsToSystems();
        }

        #endregion

        #region Private Methods

        private void Load()
        {
            try
            {
                if (!System.IO.File.Exists(SettingsFilePath))
                {
                    Debug.Log($"[SettingsSystem] No settings file found at {SettingsFilePath}, using defaults.");
                    _settings = SettingsData.CreateDefault();
                    return;
                }

                string json = System.IO.File.ReadAllText(SettingsFilePath);
                var loaded = JsonUtility.FromJson<SettingsData>(json);

                if (loaded == null)
                {
                    Debug.LogWarning("[SettingsSystem] Failed to parse settings file, using defaults.");
                    _settings = SettingsData.CreateDefault();
                    return;
                }

                _settings = loaded;
                Debug.Log($"[SettingsSystem] Loaded settings from {SettingsFilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SettingsSystem] Error loading settings: {ex.Message}, using defaults.");
                _settings = SettingsData.CreateDefault();
            }
        }

        private void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(_settings, true);
                System.IO.File.WriteAllText(SettingsFilePath, json);
                Debug.Log($"[SettingsSystem] Saved settings to {SettingsFilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SettingsSystem] Failed to save settings: {ex.Message}");
            }
        }

        private void SaveAndEmit()
        {
            Save();
        }

        private void ApplySettingsToSystems()
        {
            // Apply haptic setting directly to TouchInputSystem (Option A per design decision)
            if (Input.Touch.TouchInputSystem.Instance != null)
            {
                Input.Touch.TouchInputSystem.Instance.HapticFeedbackEnabled = Settings.HapticEnabled;
            }

            // Emit initial events so subscribed systems get current values
            EventBus.Instance.Emit(new MusicVolumeChangedEvent(Settings.MusicVolume));
            EventBus.Instance.Emit(new SFXVolumeChangedEvent(Settings.SfxVolume));
            EventBus.Instance.Emit(new VoiceVolumeChangedEvent(Settings.VoiceVolume));
            EventBus.Instance.Emit(new TextSpeedChangedEvent(Settings.TextSpeed));
            EventBus.Instance.Emit(new HapticEnabledChangedEvent(Settings.HapticEnabled));
            EventBus.Instance.Emit(new AutoAdvanceChangedEvent(Settings.AutoAdvanceEnabled));
        }

        #endregion
    }
}
