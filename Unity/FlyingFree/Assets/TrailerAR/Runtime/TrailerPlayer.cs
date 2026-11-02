using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace FlyingFree.Trailer
{
    /// <summary>Playback is independent from tracking: a lost target never overrides a user pause.</summary>
    public sealed class TrailerPlayer : MonoBehaviour
    {
        public VideoPlayer video;
        public AudioSource audioSource;
        public Transform surfaceRoot;
        public Renderer screenRenderer;
        public GameObject scanPrompt;
        public Text statusText, timeText, pauseText;
        public Button pauseButton, rewindButton, forwardButton, menuButton, retryButton;
        public Slider progress;
        public string menuScene = "FlyingFreeMenu";
        public bool simulation;
        public float screenWidthMeters = .30f;

        public bool MarkerVisible { get; private set; }
        public bool Ready => video != null && video.isPrepared;
        public bool WantsPlayback { get; private set; } = true;
        public bool Ended { get; private set; }
        public bool Seeking { get; private set; }
        public double Duration => Ready && !double.IsInfinity(video.length) && !double.IsNaN(video.length) ? video.length : 0;
        public double CurrentTime => Ready ? Math.Max(0, video.time) : 0;

        RenderTexture ownedTexture;
        bool scrubbing, foreground = true, exiting;
        double? queuedSeek;
        string error, trackingError;
        float prepareStarted, seekStarted;

        void Awake()
        {
            video.playOnAwake = false;
            video.isLooping = false;
            video.renderMode = VideoRenderMode.RenderTexture;
            ownedTexture = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32) { name = "Trailer video (runtime)" };
            ownedTexture.Create();
            video.targetTexture = ownedTexture;
            screenRenderer.material.mainTexture = ownedTexture;
            video.audioOutputMode = VideoAudioOutputMode.AudioSource;
            video.controlledAudioTrackCount = 1;
            video.EnableAudioTrack(0, true);
            video.SetTargetAudioSource(0, audioSource);
            video.prepareCompleted += Prepared;
            video.seekCompleted += SeekCompleted;
            video.loopPointReached += Finished;
            video.errorReceived += VideoError;
            pauseButton.onClick.AddListener(TogglePlayback);
            rewindButton.onClick.AddListener(Rewind);
            forwardButton.onClick.AddListener(Forward);
            menuButton.onClick.AddListener(ReturnToMenu);
            retryButton.onClick.AddListener(Prepare);
            progress.onValueChanged.AddListener(SliderChanged);
            progress.GetComponent<TrailerSeekBar>().player = this;
            surfaceRoot.gameObject.SetActive(false);
        }
        void OnEnable()
        {
            PersistentSettingsManager.OnSoundVolumeChanged += SetVolume;
            SetVolume(PlayerPrefs.GetInt("FlyingFree.UI.Muted", 0) != 0 ? 0 : PlayerPrefs.GetFloat("FlyingFree.UI.Sound", .8f));
        }
        void OnDisable()
        {
            PersistentSettingsManager.OnSoundVolumeChanged -= SetVolume;
            if (video != null) video.Pause();
        }
        void Start() { Prepare(); }
        void SetVolume(float value) { if (audioSource != null) audioSource.volume = Mathf.Clamp01(value); }
        public void Prepare()
        {
            error = null; Ended = false; Seeking = false; queuedSeek = null;
            prepareStarted = Time.unscaledTime;
            video.Stop(); video.Prepare();
            Refresh();
        }
        void Prepared(VideoPlayer source)
        {
            if (exiting) return;
            float aspect = source.height > 0 ? (float)source.width / source.height : 16f / 9;
            screenRenderer.transform.localScale = new Vector3(screenWidthMeters, screenWidthMeters / aspect, 1);
            Reconcile(); Refresh();
        }
        public void SetMarkerVisible(bool visible, Transform anchor = null)
        {
            if (exiting) return;
            if (anchor != null && surfaceRoot.parent != anchor)
            {
                surfaceRoot.SetParent(anchor, false);
                surfaceRoot.localPosition = Vector3.zero;
                surfaceRoot.localRotation = Quaternion.identity;
            }
            MarkerVisible = visible;
            surfaceRoot.gameObject.SetActive(visible);
            Reconcile(); Refresh();
        }
        public void SetTrackingError(string message) { trackingError = message; SetMarkerVisible(false); }
        public void ClearTrackingError() { trackingError = null; Refresh(); }
        public void TogglePlayback()
        {
            if (!Ready || !MarkerVisible || error != null) return;
            if (Ended) { Ended = false; WantsPlayback = true; SeekTo(0); }
            else { WantsPlayback = !WantsPlayback; Reconcile(); }
            Refresh();
        }
        public void Rewind() { SeekTo(CurrentTime - 10); }
        public void Forward() { SeekTo(CurrentTime + 10); }
        public void BeginScrub() { if (Ready && video.canSetTime) scrubbing = true; }
        public void EndScrub()
        {
            if (!scrubbing) return;
            scrubbing = false;
            SeekTo(progress.value * Duration);
        }
        void SliderChanged(float value) { if (!scrubbing) SeekTo(value * Duration); }
        public void SeekTo(double seconds)
        {
            if (!Ready || !video.canSetTime || Duration <= 0 || exiting) return;
            seconds = Math.Max(0, Math.Min(seconds, Math.Max(0, Duration - .10)));
            if (Seeking) { queuedSeek = seconds; return; }
            if (Math.Abs(CurrentTime - seconds) < .03) return;
            Ended = false;
            Seeking = true; seekStarted = Time.unscaledTime;
            video.Pause(); video.time = seconds;
            Refresh();
        }
        void SeekCompleted(VideoPlayer source)
        {
            Seeking = false;
            if (exiting) return;
            if (queuedSeek.HasValue)
            {
                double next = queuedSeek.Value; queuedSeek = null;
                SeekTo(next);
            }
            Reconcile(); Refresh();
        }
        void Reconcile()
        {
            if (!Ready || exiting) return;
            if (foreground && MarkerVisible && WantsPlayback && !Ended && !Seeking && error == null)
            { if (!video.isPlaying) video.Play(); }
            else if (video.isPlaying) video.Pause();
        }
        void Finished(VideoPlayer source) { Ended = true; WantsPlayback = false; video.Pause(); Refresh(); }
        void VideoError(VideoPlayer source, string message)
        {
            error = "No se ha podido abrir el vídeo. Pulsa REINTENTAR.";
            Debug.LogWarning("Tráiler: " + message, this); video.Pause(); Refresh();
        }
        void Update()
        {
            if (exiting) return;
            if (!Ready && error == null && Time.unscaledTime - prepareStarted > 30)
            { error = "El vídeo no responde. Pulsa REINTENTAR."; Refresh(); }
            if (Seeking && Time.unscaledTime - seekStarted > 10)
            { Seeking = false; queuedSeek = null; error = "No se ha podido mover el vídeo. Pulsa REINTENTAR."; Refresh(); }
            if (!scrubbing && !Seeking) progress.SetValueWithoutNotify(Duration > 0 ? (float)(CurrentTime / Duration) : 0);
            timeText.text = Stamp(scrubbing ? progress.value * Duration : CurrentTime) + " / " + Stamp(Duration);
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame) ReturnToMenu();
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape)) ReturnToMenu();
#endif
        }
        void Refresh()
        {
            if (statusText == null) return;
            string state = trackingError ?? error;
            if (state == null)
                state = !MarkerVisible ? "Enfoca la nueva marca TRÁILER para continuar" : !Ready ? "Preparando vídeo…" : Seeking ? "Buscando…" : Ended ? "Tráiler terminado. Puedes volver a verlo." : WantsPlayback ? "Reproduciendo sobre la marca" : "En pausa";
            statusText.text = (simulation ? "SIMULACIÓN · " : "") + state;
            scanPrompt.SetActive(!MarkerVisible && error == null && trackingError == null);
            pauseText.text = Ended ? "VER OTRA VEZ" : WantsPlayback ? "PAUSAR" : "REANUDAR";
            pauseButton.interactable = Ready && MarkerVisible && error == null;
            rewindButton.interactable = forwardButton.interactable = progress.interactable = Ready && video.canSetTime && error == null;
            retryButton.gameObject.SetActive(error != null);
        }
        void OnApplicationPause(bool paused) { foreground = !paused; Reconcile(); }
        void OnApplicationFocus(bool focused) { foreground = focused; Reconcile(); }
        public void ReturnToMenu()
        {
            if (exiting) return;
            exiting = true; video.Stop(); audioSource.Stop();
            SceneManager.LoadSceneAsync(menuScene);
        }
        static string Stamp(double time)
        {
            int total = double.IsNaN(time) || double.IsInfinity(time) ? 0 : Math.Max(0, (int)time);
            return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }
        void OnDestroy()
        {
            if (video != null)
            {
                video.prepareCompleted -= Prepared; video.seekCompleted -= SeekCompleted;
                video.loopPointReached -= Finished; video.errorReceived -= VideoError;
                video.Stop(); video.targetTexture = null;
            }
            if (ownedTexture != null) { ownedTexture.Release(); Destroy(ownedTexture); }
            if (screenRenderer != null) Destroy(screenRenderer.material);
        }
    }
}
