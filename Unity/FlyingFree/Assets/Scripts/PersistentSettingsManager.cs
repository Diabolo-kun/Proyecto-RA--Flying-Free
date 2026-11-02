using System;
using UnityEngine;

namespace FlyingFree
{
    /// <summary>
    /// Administrador persistente de configuración (Audio, Volumen y Mute).
    /// Sobrevive al cambio de escenas gracias a DontDestroyOnLoad.
    /// Garantiza que la configuración ajustada en el Menú Principal se herede
    /// automáticamente en GameRA y en cualquier otra escena del juego.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PersistentSettingsManager : MonoBehaviour
    {
        public static PersistentSettingsManager Instance { get; private set; }

        private const string Prefix = "FlyingFree.UI.";
        private const string SoundKey = Prefix + "Sound";
        private const string MusicKey = Prefix + "Music";
        private const string MuteKey = Prefix + "Muted";

        [Header("--- ESTADO ACTUAL (0 a 1) ---")]
        [Range(0f, 1f)]
        [SerializeField] private float soundVolume = 0.8f;
        [Range(0f, 1f)]
        [SerializeField] private float musicVolume = 0.6f;
        [SerializeField] private bool isMuted = false;

        [Header("--- PANEL DE CONFIGURACIÓN OVERLAY (OPCIONAL) ---")]
        [Tooltip("Si se asigna un Canvas o panel de ajustes, se conservará en todas las escenas para poder abrirlo como menú de pausa")]
        [SerializeField] private GameObject settingsOverlayPanel;

#pragma warning disable CS0414
        [Tooltip("Tecla rápida para abrir/cerrar ajustes en cualquier escena (Escape o botón Atrás en Android)")]
        [SerializeField] private KeyCode toggleSettingsKey = KeyCode.Escape;
#pragma warning restore CS0414

        // Eventos para que cualquier script de cualquier escena se entere de los cambios
        public static event Action<float> OnSoundVolumeChanged;
        public static event Action<float> OnMusicVolumeChanged;
        public static event Action<bool> OnMuteChanged;

        public float SoundVolume => soundVolume;
        public float MusicVolume => musicVolume;
        public bool IsMuted => isMuted;

        public float EffectiveSoundVolume => isMuted ? 0f : soundVolume;
        public float EffectiveMusicVolume => isMuted ? 0f : musicVolume;

        private void Awake()
        {
            // Patrón Singleton Persistente
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadSettingsFromPlayerPrefs();
        }

        private void Start()
        {
            BroadcastAllSettings();
        }

        private void Update()
        {
            // Permite abrir el panel de configuración en cualquier escena con la tecla Escape
            if (settingsOverlayPanel != null)
            {
#if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Keyboard.current != null &&
                    UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    ToggleSettingsOverlay();
                }
#elif ENABLE_LEGACY_INPUT_MANAGER
                if (Input.GetKeyDown(toggleSettingsKey))
                {
                    ToggleSettingsOverlay();
                }
#endif
            }
        }

        /// <summary>
        /// Carga los valores guardados en PlayerPrefs con los mismos nombres que usa FlyingFreeMenu.
        /// </summary>
        public void LoadSettingsFromPlayerPrefs()
        {
            soundVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SoundKey, 0.8f));
            musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, 0.6f));
            isMuted = PlayerPrefs.GetInt(MuteKey, 0) != 0;
        }

        public void SetSoundVolume(float value)
        {
            soundVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SoundKey, soundVolume);
            PlayerPrefs.Save();
            OnSoundVolumeChanged?.Invoke(EffectiveSoundVolume);
        }

        public void SetMusicVolume(float value)
        {
            musicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicKey, musicVolume);
            PlayerPrefs.Save();
            OnMusicVolumeChanged?.Invoke(EffectiveMusicVolume);
        }

        public void SetMuted(bool mute)
        {
            isMuted = mute;
            PlayerPrefs.SetInt(MuteKey, isMuted ? 1 : 0);
            PlayerPrefs.Save();
            OnMuteChanged?.Invoke(isMuted);
            OnSoundVolumeChanged?.Invoke(EffectiveSoundVolume);
            OnMusicVolumeChanged?.Invoke(EffectiveMusicVolume);
        }

        public void BroadcastAllSettings()
        {
            OnMuteChanged?.Invoke(isMuted);
            OnSoundVolumeChanged?.Invoke(EffectiveSoundVolume);
            OnMusicVolumeChanged?.Invoke(EffectiveMusicVolume);
        }

        public void ToggleSettingsOverlay()
        {
            if (settingsOverlayPanel != null)
            {
                bool newState = !settingsOverlayPanel.activeSelf;
                settingsOverlayPanel.SetActive(newState);
            }
        }

        public void OpenSettingsOverlay()
        {
            if (settingsOverlayPanel != null)
                settingsOverlayPanel.SetActive(true);
        }

        public void CloseSettingsOverlay()
        {
            if (settingsOverlayPanel != null)
                settingsOverlayPanel.SetActive(false);
        }
    }
}
