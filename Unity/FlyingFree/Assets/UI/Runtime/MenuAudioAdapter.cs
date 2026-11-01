using UnityEngine;

namespace FlyingFree.UI
{
    /// <summary>Optional adapter: assign dedicated menu/game AudioSources, never global volume.</summary>
    public sealed class MenuAudioAdapter : MonoBehaviour
    {
        public FlyingFreeMenu menu;
        public AudioSource[] soundSources = new AudioSource[0];
        public AudioSource[] musicSources = new AudioSource[0];
        void OnEnable()
        {
            if (menu == null) return;
            menu.onSoundVolumeChanged.AddListener(ApplySound);
            menu.onMusicVolumeChanged.AddListener(ApplyMusic);
        }
        void Start() { if (menu != null) menu.PublishAudio(); }
        void OnDisable()
        {
            if (menu == null) return;
            menu.onSoundVolumeChanged.RemoveListener(ApplySound);
            menu.onMusicVolumeChanged.RemoveListener(ApplyMusic);
        }
        public void ApplySound(float value) { foreach (var source in soundSources) if (source != null) source.volume = value; }
        public void ApplyMusic(float value) { foreach (var source in musicSources) if (source != null) source.volume = value; }
    }
}
