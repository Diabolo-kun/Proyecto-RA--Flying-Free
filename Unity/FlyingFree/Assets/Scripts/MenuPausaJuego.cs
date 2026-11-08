using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FlyingFree.UI;

/// <summary>
/// Menú de pausa y ayuda en GameRA, con el mismo estilo visual que el menú principal.
/// </summary>
public sealed class MenuPausaJuego : MonoBehaviour
{
    GameObject panelPausa;
    GameObject panelAyuda;
    Button botonPausa;
    Button botonAyuda;
    GameManagerRA juego;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Arrancar()
    {
        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;
        AlCargar(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void AlCargar(Scene escena, LoadSceneMode modo)
    {
        if (escena.name != "GameRA") return;
        if (Object.FindFirstObjectByType<MenuPausaJuego>() != null) return;

        var go = new GameObject("MenuPausaJuego");
        go.AddComponent<MenuPausaJuego>();
        go.AddComponent<EstiloHudGameRA>();
    }

    void Start()
    {
        juego = GameManagerRA.Instance != null
            ? GameManagerRA.Instance
            : FindFirstObjectByType<GameManagerRA>();
        if (juego == null)
        {
            Destroy(gameObject);
            return;
        }

        CrearUi();
    }

    void Update()
    {
        if (juego == null || panelPausa == null || panelAyuda == null) return;
        if (juego.HaIniciado && !juego.PartidaEnCurso)
        {
            botonPausa.gameObject.SetActive(false);
            botonAyuda.gameObject.SetActive(false);
            if (panelPausa.activeSelf) panelPausa.SetActive(false);
            if (panelAyuda.activeSelf) panelAyuda.SetActive(false);
            return;
        }

        bool overlayAbierto = panelPausa.activeSelf || panelAyuda.activeSelf;
        botonPausa.gameObject.SetActive(!overlayAbierto);
        botonAyuda.gameObject.SetActive(!overlayAbierto);

        bool escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        if (!escape) return;

        if (panelAyuda.activeSelf) CerrarAyuda();
        else if (panelPausa.activeSelf) Seguir();
        else Pausar();
    }

    void CrearUi()
    {
        var root = new GameObject("HUD_Pausa", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            es.transform.SetParent(transform, false);
        }

        // Izquierda: pausa. Derecha: ayuda (?).
        botonPausa = EstiloMenuUI.CrearBotonIcono(
            root.transform,
            "BotonPausa",
            MenuIconGraphic.Symbol.Pause,
            new Vector2(1f, 1f),
            new Vector2(-250f, -96f),
            new Vector2(140f, 140f),
            Pausar);

        botonAyuda = EstiloMenuUI.CrearBotonConTexto(
            root.transform,
            "BotonAyuda",
            "?",
            new Vector2(1f, 1f),
            new Vector2(-96f, -96f),
            new Vector2(140f, 140f),
            AbrirAyuda,
            72);

        panelPausa = CrearOverlay(root.transform, "PanelPausa");
        var tarjetaPausa = CrearTarjeta(panelPausa.transform, 860f, 780f);

        var marco = EstiloMenuUI.CrearPanel(tarjetaPausa.transform, "Marco", EstiloMenuUI.Azul, false);
        var marcoRt = marco.rectTransform;
        marcoRt.anchorMin = new Vector2(0.06f, 0.78f);
        marcoRt.anchorMax = new Vector2(0.94f, 0.94f);
        marcoRt.offsetMin = marcoRt.offsetMax = Vector2.zero;
        var titulo = EstiloMenuUI.CrearTexto(marco.transform, "Titulo", "PAUSA", 52, Color.white);
        EstiloMenuUI.Estirar(titulo.rectTransform);

        var centro = new Vector2(0.5f, 0.5f);
        EstiloMenuUI.CrearBotonArte(tarjetaPausa.transform, "Seguir", "SEGUIR JUGANDO", true, MenuIconGraphic.Symbol.Play, centro, new Vector2(0f, 70f), new Vector2(680f, 130f), Seguir, 36);
        EstiloMenuUI.CrearBotonArte(tarjetaPausa.transform, "Reiniciar", "EMPEZAR DE NUEVO", false, MenuIconGraphic.Symbol.Back, centro, new Vector2(0f, -80f), new Vector2(680f, 120f), EmpezarDeNuevo, 34);
        EstiloMenuUI.CrearBotonArte(tarjetaPausa.transform, "Menu", "SALIR AL MENÚ", false, MenuIconGraphic.Symbol.Exit, centro, new Vector2(0f, -220f), new Vector2(680f, 120f), SalirAlMenu, 34);

        panelAyuda = CrearOverlay(root.transform, "PanelAyuda");
        var tarjetaAyuda = CrearTarjeta(panelAyuda.transform, 880f, 1120f);

        var cabecera = EstiloMenuUI.CrearPanel(tarjetaAyuda.transform, "Cabecera", EstiloMenuUI.Azul, false);
        var cabRt = cabecera.rectTransform;
        cabRt.anchorMin = new Vector2(0.06f, 0.86f);
        cabRt.anchorMax = new Vector2(0.94f, 0.96f);
        cabRt.offsetMin = cabRt.offsetMax = Vector2.zero;
        var tituloAyuda = EstiloMenuUI.CrearTexto(cabecera.transform, "Titulo", "INSTRUCCIONES", 46, Color.white);
        EstiloMenuUI.Estirar(tituloAyuda.rectTransform);

        var cuerpo = EstiloMenuUI.CrearTexto(tarjetaAyuda.transform, "Texto", MenuInstrucciones.Texto, 28, EstiloMenuUI.Tinta);
        cuerpo.alignment = TextAnchor.UpperLeft;
        var cuerpoRt = cuerpo.rectTransform;
        cuerpoRt.anchorMin = new Vector2(0.08f, 0.22f);
        cuerpoRt.anchorMax = new Vector2(0.92f, 0.84f);
        cuerpoRt.offsetMin = cuerpoRt.offsetMax = Vector2.zero;

        EstiloMenuUI.CrearBotonArte(
            tarjetaAyuda.transform,
            "VolverJuego",
            "VOLVER AL JUEGO",
            true,
            MenuIconGraphic.Symbol.Play,
            centro,
            new Vector2(0f, -430f),
            new Vector2(640f, 130f),
            CerrarAyuda,
            34);
    }

    static GameObject CrearOverlay(Transform padre, string nombre)
    {
        var panel = EstiloMenuUI.CrearPanel(padre, nombre, EstiloMenuUI.Overlay).gameObject;
        EstiloMenuUI.Estirar(panel.GetComponent<RectTransform>());
        panel.GetComponent<Image>().sprite = null;
        panel.GetComponent<Image>().type = Image.Type.Simple;
        panel.SetActive(false);
        return panel;
    }

    static Image CrearTarjeta(Transform padre, float ancho, float alto)
    {
        var tarjeta = EstiloMenuUI.CrearPanel(padre, "Tarjeta", Color.white);
        EstiloMenuUI.AplicarPanelCrema(tarjeta, 1f);
        var rt = tarjeta.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(ancho, alto);
        return tarjeta;
    }

    void Pausar()
    {
        if (juego == null || !juego.PartidaEnCurso) return;
        if (panelAyuda.activeSelf) panelAyuda.SetActive(false);
        juego.PausarPartida();
        panelPausa.SetActive(true);
        botonPausa.gameObject.SetActive(false);
        botonAyuda.gameObject.SetActive(false);
    }

    void AbrirAyuda()
    {
        if (juego == null || !juego.PartidaEnCurso) return;
        if (panelPausa.activeSelf) panelPausa.SetActive(false);
        juego.PausarPartida();
        panelAyuda.SetActive(true);
        botonPausa.gameObject.SetActive(false);
        botonAyuda.gameObject.SetActive(false);
    }

    void CerrarAyuda()
    {
        if (juego == null) return;
        juego.ReanudarPartida();
        panelAyuda.SetActive(false);
        botonPausa.gameObject.SetActive(true);
        botonAyuda.gameObject.SetActive(true);
    }

    void Seguir()
    {
        if (juego == null) return;
        juego.ReanudarPartida();
        panelPausa.SetActive(false);
        botonPausa.gameObject.SetActive(true);
        botonAyuda.gameObject.SetActive(true);
    }

    void EmpezarDeNuevo()
    {
        if (juego == null) return;
        panelPausa.SetActive(false);
        panelAyuda.SetActive(false);
        botonPausa.gameObject.SetActive(true);
        botonAyuda.gameObject.SetActive(true);
        juego.EmpezarDeNuevo();
    }

    void SalirAlMenu()
    {
        if (juego != null)
            juego.ReanudarPartida();
        SceneManager.LoadScene("FlyingFreeMenu");
    }
}
