using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FlyingFree.UI
{
    [Serializable] public sealed class VolumeEvent : UnityEvent<float> { }
    [Serializable] public sealed class MuteEvent : UnityEvent<bool> { }

    /// <summary>UI-only controller. Connect the events in the Inspector or through code.</summary>
    public sealed class FlyingFreeMenu : MonoBehaviour
    {
        const string Prefix = "FlyingFree.UI.";
        public GameObject mainPage;
        public GameObject settingsPage;
        public Button playButton, settingsButton, exitButton, backButton;
        [Tooltip("Botón para reiniciar o seleccionar el avión estándar")]
        public Button resetPlaneButton;
        public Slider soundSlider, musicSlider;
        public Toggle muteToggle;
        public Text soundValue, musicValue;
        [Tooltip("Texto descriptivo que muestra el estado o tipo de avión")]
        public Text planeStatusText;
        public bool quitApplicationOnExit = true;
        public UnityEvent onPlayRequested = new UnityEvent();
        public UnityEvent onExitRequested = new UnityEvent();
        public UnityEvent onSettingsOpened = new UnityEvent();
        public UnityEvent onSettingsClosed = new UnityEvent();
        [Tooltip("Se dispara al pulsar el botón Volver / Menú Principal en la pantalla de ajustes")]
        public UnityEvent onBackRequested = new UnityEvent();
        public UnityEvent onResetPlaneRequested = new UnityEvent();
        [Tooltip("Effective 0..1 volume; emits 0 while muted. Bind to an audio adapter.")]
        public VolumeEvent onSoundVolumeChanged = new VolumeEvent();
        public VolumeEvent onMusicVolumeChanged = new VolumeEvent();
        public MuteEvent onMuteChanged = new MuteEvent();

        public float SoundVolume { get; private set; }
        public float MusicVolume { get; private set; }
        public bool Muted { get; private set; }
        public bool IsSettingsOpen => settingsPage.activeSelf;

        void Awake()
        {
            playButton.onClick.AddListener(Play);
            settingsButton.onClick.AddListener(OpenSettings);
            exitButton.onClick.AddListener(Exit);
            backButton.onClick.AddListener(CloseSettings);
            if (resetPlaneButton != null) resetPlaneButton.onClick.AddListener(ResetPlane);
            soundSlider.onValueChanged.AddListener(SetSoundVolume);
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
            muteToggle.onValueChanged.AddListener(SetMuted);
            SoundVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "Sound", .8f));
            MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "Music", .6f));
            Muted = PlayerPrefs.GetInt(Prefix + "Muted", 0) != 0;
            soundSlider.SetValueWithoutNotify(SoundVolume);
            musicSlider.SetValueWithoutNotify(MusicVolume);
            muteToggle.SetIsOnWithoutNotify(Muted);
            RefreshLabels();
            mainPage.SetActive(true);
            settingsPage.SetActive(false);
        }

        // Start allows listeners to subscribe from their own Awake/OnEnable first.
        void Start() 
        { 
            PublishAudio(); 
            Select(playButton); 
            ActualizarTextoAvion();
        }

        void OnEnable()
        {
            PlaneSkinManager.OnSkinChanged += HandleSkinChanged;
            ActualizarTextoAvion();
        }

        void OnDisable()
        {
            PlaneSkinManager.OnSkinChanged -= HandleSkinChanged;
        }

        private void HandleSkinChanged(PlaneSkinEntry skin)
        {
            ActualizarTextoAvion();
        }

        private void ActualizarTextoAvion()
        {
            string nombre = PlaneSkinManager.Instance != null 
                ? PlaneSkinManager.Instance.GetActiveSkinName() 
                : "Estándar";
            SetPlaneStatusText(nombre);
        }

        public void Play() { onPlayRequested.Invoke(); }
        public void Exit()
        {
            Save();
            onExitRequested.Invoke();
            if (!quitApplicationOnExit) return;
#if UNITY_EDITOR
            Debug.Log("Flying Free: solicitud SALIR recibida. En el dispositivo se cerrará la aplicación.", this);
#elif !UNITY_IOS && !UNITY_WEBGL
            Application.Quit();
#endif
        }
        public void OpenSettings()
        {
            if (IsSettingsOpen) return;
            mainPage.SetActive(false);
            settingsPage.SetActive(true);
            ActualizarTextoAvion();
            Select(soundSlider);
            onSettingsOpened.Invoke();
        }
        public void CloseSettings()
        {
            if (!IsSettingsOpen) return;
            Save();
            onBackRequested.Invoke();
            settingsPage.SetActive(false);
            mainPage.SetActive(true);
            Select(settingsButton);
            onSettingsClosed.Invoke();
        }
        public void ResetPlane()
        {
            Debug.Log("[FlyingFreeMenu] Restableciendo avión a estándar...");
            if (PlaneSkinManager.Instance != null)
            {
                PlaneSkinManager.Instance.ResetToDefault();
            }
            else
            {
                SetPlaneStatusText("Estándar");
            }
            onResetPlaneRequested.Invoke();
        }
        /// <summary>Modifica dinámicamente el texto que acompaña al botón de configuración del avión.</summary>
        public void SetPlaneStatusText(string newText)
        {
            if (planeStatusText != null)
                planeStatusText.text = newText;
        }
        public void SetSoundVolume(float value)
        {
            SoundVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(Prefix + "Sound", SoundVolume);
            RefreshLabels();
            onSoundVolumeChanged.Invoke(Muted ? 0 : SoundVolume);
        }
        public void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(Prefix + "Music", MusicVolume);
            RefreshLabels();
            onMusicVolumeChanged.Invoke(Muted ? 0 : MusicVolume);
        }
        public void SetMuted(bool value)
        {
            Muted = value;
            PlayerPrefs.SetInt(Prefix + "Muted", value ? 1 : 0);
            PublishAudio();
        }
        public void PublishAudio()
        {
            onMuteChanged.Invoke(Muted);
            onSoundVolumeChanged.Invoke(Muted ? 0 : SoundVolume);
            onMusicVolumeChanged.Invoke(Muted ? 0 : MusicVolume);
        }
        void RefreshLabels()
        {
            soundValue.text = Mathf.RoundToInt(SoundVolume * 100) + "%";
            musicValue.text = Mathf.RoundToInt(MusicVolume * 100) + "%";
        }
        static void Select(Selectable selectable)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }
        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame && IsSettingsOpen)
                CloseSettings();
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape) && IsSettingsOpen) CloseSettings();
#endif
        }
        public void Save() { PlayerPrefs.Save(); }
        void OnApplicationPause(bool paused) { if (paused) Save(); }
        void OnApplicationQuit() { Save(); }
        void OnDestroy()
        {
            playButton.onClick.RemoveListener(Play);
            settingsButton.onClick.RemoveListener(OpenSettings);
            exitButton.onClick.RemoveListener(Exit);
            backButton.onClick.RemoveListener(CloseSettings);
            if (resetPlaneButton != null) resetPlaneButton.onClick.RemoveListener(ResetPlane);
            soundSlider.onValueChanged.RemoveListener(SetSoundVolume);
            musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
            muteToggle.onValueChanged.RemoveListener(SetMuted);
        }
    }
}
