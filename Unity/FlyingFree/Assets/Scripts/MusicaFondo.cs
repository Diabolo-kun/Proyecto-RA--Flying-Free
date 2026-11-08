using UnityEngine;
using UnityEngine.SceneManagement;
using FlyingFree.UI;

/// <summary>
/// Música de fondo en bucle. Arranca sola, sobrevive al cambio de escena
/// y respeta el volumen/silencio de Ajustes.
/// </summary>
[DefaultExecutionOrder(-80)]
public sealed class MusicaFondo : MonoBehaviour
{
    const string RutaClip = "Audio/flying_free_pont_aeri";
    const string PrefMusic = "FlyingFree.UI.Music";
    const string PrefMute = "FlyingFree.UI.Muted";

    static MusicaFondo instancia;
    AudioSource fuente;
    FlyingFreeMenu menuEnlazado;
    bool silenciadaPorEscena;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Arrancar()
    {
        if (instancia != null) return;

        AudioClip clip = Resources.Load<AudioClip>(RutaClip);
        if (clip == null)
        {
            Debug.LogWarning("[MusicaFondo] No se encontró Resources/Audio/flying_free_pont_aeri");
            return;
        }

        var go = new GameObject("MusicaFondo");
        DontDestroyOnLoad(go);
        instancia = go.AddComponent<MusicaFondo>();
        instancia.fuente = go.AddComponent<AudioSource>();
        instancia.fuente.clip = clip;
        instancia.fuente.loop = true;
        instancia.fuente.playOnAwake = false;
        instancia.fuente.spatialBlend = 0f;
        instancia.fuente.volume = VolumenActual();
        instancia.fuente.Play();

        SceneManager.sceneLoaded += instancia.AlCargarEscena;
        instancia.AlCargarEscena(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    public static void Pausar()
    {
        if (instancia == null || instancia.fuente == null) return;
        instancia.fuente.Pause();
    }

    public static void Reanudar()
    {
        if (instancia == null || instancia.fuente == null) return;
        if (instancia.silenciadaPorEscena) return;
        if (!instancia.fuente.isPlaying)
            instancia.fuente.UnPause();
        if (!instancia.fuente.isPlaying)
            instancia.fuente.Play();
        instancia.AplicarVolumen(VolumenActual());
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        DesenlazarMenu();
        if (instancia == this)
            instancia = null;
    }

    void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        DesenlazarMenu();
        FlyingFreeMenu menu = FindFirstObjectByType<FlyingFreeMenu>();
        if (menu != null)
            EnlazarMenu(menu);

        // En el tráiler solo se oye el vídeo, no la música de fondo.
        silenciadaPorEscena = escena.name == "TrailerAR";
        if (silenciadaPorEscena)
            Pausar();
        else
            Reanudar();

        AplicarVolumen(silenciadaPorEscena ? 0f : VolumenActual());
    }

    void EnlazarMenu(FlyingFreeMenu menu)
    {
        menuEnlazado = menu;
        menu.onMusicVolumeChanged.AddListener(AplicarVolumen);
        menu.onMuteChanged.AddListener(AlSilenciar);
        menu.PublishAudio();
    }

    void DesenlazarMenu()
    {
        if (menuEnlazado == null) return;
        menuEnlazado.onMusicVolumeChanged.RemoveListener(AplicarVolumen);
        menuEnlazado.onMuteChanged.RemoveListener(AlSilenciar);
        menuEnlazado = null;
    }

    void AlSilenciar(bool mute)
    {
        AplicarVolumen(mute ? 0f : PlayerPrefs.GetFloat(PrefMusic, 0.6f));
    }

    void AplicarVolumen(float volumen)
    {
        if (fuente == null) return;
        if (silenciadaPorEscena)
        {
            fuente.volume = 0f;
            return;
        }
        fuente.volume = Mathf.Clamp01(volumen);
    }

    static float VolumenActual()
    {
        bool mute = PlayerPrefs.GetInt(PrefMute, 0) != 0;
        if (mute) return 0f;
        return Mathf.Clamp01(PlayerPrefs.GetFloat(PrefMusic, 0.6f));
    }
}
