using UnityEngine;

namespace FlyingFree
{
    /// <summary>
    /// Sincroniza automáticamente cualquier AudioSource de cualquier escena (GameRA, Pruebas, etc.)
    /// con el volumen global configurado en el Menú de Ajustes.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class GlobalAudioSync : MonoBehaviour
    {
        public enum AudioChannel
        {
            SoundEffects, // Efectos de sonido (globos, choques, motores)
            Music         // Música de fondo o ambiental
        }

        [Header("Tipo de Canal")]
        [Tooltip("Indica si este AudioSource reproduce música o efectos de sonido")]
        public AudioChannel channel = AudioChannel.SoundEffects;

        [Header("Volumen Base")]
        [Range(0f, 1f)]
        [Tooltip("Volumen original del audio relativo (ej. 1.0 = volumen completo de este clip)")]
        public float baseVolume = 1.0f;

        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            if (channel == AudioChannel.SoundEffects)
            {
                PersistentSettingsManager.OnSoundVolumeChanged += UpdateVolume;
            }
            else
            {
                PersistentSettingsManager.OnMusicVolumeChanged += UpdateVolume;
            }
        }

        private void OnDisable()
        {
            if (channel == AudioChannel.SoundEffects)
            {
                PersistentSettingsManager.OnSoundVolumeChanged -= UpdateVolume;
            }
            else
            {
                PersistentSettingsManager.OnMusicVolumeChanged -= UpdateVolume;
            }
        }

        private void Start()
        {
            // Sincronizar inmediatamente al iniciar la escena
            float effectiveVolume = 1.0f;
            if (PersistentSettingsManager.Instance != null)
            {
                effectiveVolume = (channel == AudioChannel.SoundEffects) ?
                    PersistentSettingsManager.Instance.EffectiveSoundVolume :
                    PersistentSettingsManager.Instance.EffectiveMusicVolume;
            }
            else
            {
                // Fallback leyendo directamente de PlayerPrefs si no existe el manager en la escena
                float sound = PlayerPrefs.GetFloat("FlyingFree.UI.Sound", 0.8f);
                float music = PlayerPrefs.GetFloat("FlyingFree.UI.Music", 0.6f);
                bool muted = PlayerPrefs.GetInt("FlyingFree.UI.Muted", 0) != 0;

                effectiveVolume = muted ? 0f : (channel == AudioChannel.SoundEffects ? sound : music);
            }

            UpdateVolume(effectiveVolume);
        }

        private void UpdateVolume(float globalVolume)
        {
            if (audioSource != null)
            {
                audioSource.volume = baseVolume * globalVolume;
            }
        }
    }
}
