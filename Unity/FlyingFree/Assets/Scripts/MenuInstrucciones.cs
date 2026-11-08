using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FlyingFree.UI;

/// <summary>
/// Añade INSTRUCCIONES al menú principal, con el mismo estilo de los demás botones.
/// </summary>
public static class MenuInstrucciones
{
    const string NombreBoton = "INSTRUCCIONES";
    const string NombrePanel = "PanelInstrucciones";

    public static readonly string Texto =
        "1. Enfoca la marca del logo y mantenla a la vista.\n\n" +
        "2. Para lanzar el avión, da un golpe seco hacia delante con el móvil.\n\n" +
        "3. El avión vuela solo: no se puede dirigir después de lanzarlo.\n\n" +
        "4. Si quieres otro avión, pulsa SELECCIONAR AVIÓN y escanea la marca del avión para elegirlo.\n\n" +
        "5. Revienta los globos antes de que se acabe el tiempo.\n\n" +
        "6. Arriba ves los globos que te quedan y el reloj. La pausa y la ayuda están arriba a la derecha.";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Arrancar()
    {
        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;
        CrearSiHayMenu();
    }

    static void AlCargar(Scene escena, LoadSceneMode modo) => CrearSiHayMenu();

    static void CrearSiHayMenu()
    {
        var menu = Object.FindFirstObjectByType<FlyingFreeMenu>();
        if (menu == null) return;

        MenuSeleccionAvion.Asegurar();

        Transform jugar = Buscar(menu.transform, "JUGAR");
        Transform seleccionar = Buscar(menu.transform, "SELECCIONAR AVIÓN");
        Transform trailer = Buscar(menu.transform, "TRÁILER");
        Transform config = Buscar(menu.transform, "CONFIGURACIÓN");
        Transform salir = Buscar(menu.transform, "SALIR");
        if (jugar == null || config == null) return;

        Transform botonExistente = Buscar(menu.transform, NombreBoton);
        Button boton;
        if (botonExistente != null)
        {
            boton = botonExistente.GetComponent<Button>();
        }
        else
        {
            GameObject copia = Object.Instantiate(config.gameObject, config.parent);
            copia.name = NombreBoton;
            int indice = seleccionar != null ? seleccionar.GetSiblingIndex() + 1 : jugar.GetSiblingIndex() + 1;
            copia.transform.SetSiblingIndex(indice);

            Text etiqueta = copia.GetComponentInChildren<Text>();
            if (etiqueta != null) etiqueta.text = NombreBoton;

            MenuIconGraphic icono = copia.GetComponentInChildren<MenuIconGraphic>();
            if (icono != null)
            {
                icono.symbol = MenuIconGraphic.Symbol.Check;
                icono.color = new Color(0.09019608f, 0.24313726f, 0.33333334f, 1f);
            }

            boton = copia.GetComponent<Button>();
            botonExistente = copia.transform;
        }

        if (boton != null)
        {
            boton.onClick.RemoveAllListeners();
            boton.onClick.AddListener(() => Mostrar(menu.transform));
        }

        Colocar(jugar, 370f, 780f, 145f);
        if (seleccionar != null) Colocar(seleccionar, 220f, 740f, 135f);
        Colocar(botonExistente, 75f, 740f, 135f);
        if (trailer != null) Colocar(trailer, -70f, 740f, 135f);
        Colocar(config, -215f, 740f, 135f);
        if (salir != null) Colocar(salir, -360f, 740f, 135f);

        AsegurarPanel(menu.transform);
    }

    static void AsegurarPanel(Transform menu)
    {
        if (Buscar(menu, NombrePanel) != null) return;

        var overlay = new GameObject(NombrePanel, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        overlay.transform.SetParent(menu, false);
        var overlayRt = overlay.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = overlayRt.offsetMax = Vector2.zero;
        var overlayImg = overlay.GetComponent<Image>();
        overlayImg.color = new Color(0.045f, 0.16f, 0.23f, 0.72f);
        overlay.GetComponent<Button>().onClick.AddListener(() => overlay.SetActive(false));

        var tarjeta = new GameObject("Tarjeta", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        tarjeta.transform.SetParent(overlay.transform, false);
        var tarjetaRt = tarjeta.GetComponent<RectTransform>();
        tarjetaRt.anchorMin = tarjetaRt.anchorMax = new Vector2(0.5f, 0.5f);
        tarjetaRt.sizeDelta = new Vector2(880f, 1180f);
        var tarjetaImg = tarjeta.GetComponent<Image>();
        Sprite crema = Resources.Load<Sprite>("FlyingFree/ButtonCream");
        Sprite redondo = Resources.Load<Sprite>("FlyingFree/RoundedPanel");
        if (crema != null)
        {
            tarjetaImg.sprite = crema;
            tarjetaImg.type = Image.Type.Sliced;
            tarjetaImg.color = Color.white;
        }
        else
        {
            tarjetaImg.sprite = redondo;
            tarjetaImg.type = redondo != null ? Image.Type.Sliced : Image.Type.Simple;
            tarjetaImg.color = new Color(1f, 0.9647059f, 0.8980392f, 1f);
        }

        var cabecera = new GameObject("Cabecera", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        cabecera.transform.SetParent(tarjeta.transform, false);
        var cabRt = cabecera.GetComponent<RectTransform>();
        cabRt.anchorMin = new Vector2(0.06f, 0.86f);
        cabRt.anchorMax = new Vector2(0.94f, 0.96f);
        cabRt.offsetMin = cabRt.offsetMax = Vector2.zero;
        var cabImg = cabecera.GetComponent<Image>();
        cabImg.sprite = redondo;
        cabImg.type = redondo != null ? Image.Type.Sliced : Image.Type.Simple;
        cabImg.color = new Color(0.12941177f, 0.47843137f, 0.7058824f, 1f);
        cabImg.raycastTarget = false;

        Font fuente = Resources.Load<Font>("FlyingFree/Kalam-Bold");
        if (fuente == null) fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var titulo = CrearTexto(cabecera.transform, "Título", "INSTRUCCIONES", 46, Color.white, fuente);
        Estirar(titulo.rectTransform);

        var cuerpo = CrearTexto(tarjeta.transform, "Texto", Texto, 30, new Color(0.09019608f, 0.24313726f, 0.33333334f, 1f), fuente);
        cuerpo.alignment = TextAnchor.UpperLeft;
        var cuerpoRt = cuerpo.rectTransform;
        cuerpoRt.anchorMin = new Vector2(0.08f, 0.18f);
        cuerpoRt.anchorMax = new Vector2(0.92f, 0.84f);
        cuerpoRt.offsetMin = cuerpoRt.offsetMax = Vector2.zero;

        var volverGo = new GameObject("Volver", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        volverGo.transform.SetParent(tarjeta.transform, false);
        var volverRt = volverGo.GetComponent<RectTransform>();
        volverRt.anchorMin = volverRt.anchorMax = new Vector2(0.5f, 0.5f);
        volverRt.anchoredPosition = new Vector2(0f, -470f);
        volverRt.sizeDelta = new Vector2(520f, 120f);
        var volverImg = volverGo.GetComponent<Image>();
        Sprite azul = Resources.Load<Sprite>("FlyingFree/ButtonBlue");
        if (azul != null)
        {
            volverImg.sprite = azul;
            volverImg.type = Image.Type.Sliced;
            volverImg.color = Color.white;
        }
        else
        {
            volverImg.sprite = redondo;
            volverImg.type = redondo != null ? Image.Type.Sliced : Image.Type.Simple;
            volverImg.color = new Color(0.12941177f, 0.47843137f, 0.7058824f, 1f);
        }
        var volverBtn = volverGo.GetComponent<Button>();
        volverBtn.targetGraphic = volverImg;
        volverBtn.onClick.AddListener(() => overlay.SetActive(false));
        var volverTxt = CrearTexto(volverGo.transform, "Texto", "VOLVER", 38, Color.white, fuente);
        Estirar(volverTxt.rectTransform);

        // Evita que un toque en la tarjeta cierre el panel.
        var bloqueo = tarjeta.AddComponent<Button>();
        bloqueo.transition = Selectable.Transition.None;
        bloqueo.targetGraphic = tarjetaImg;

        overlay.SetActive(false);
    }

    static void Mostrar(Transform menu)
    {
        Transform panel = Buscar(menu, NombrePanel);
        if (panel != null) panel.gameObject.SetActive(true);
    }

    static Text CrearTexto(Transform padre, string nombre, string valor, int tamano, Color color, Font fuente)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(padre, false);
        var label = go.GetComponent<Text>();
        label.font = fuente;
        label.text = valor;
        label.fontSize = tamano;
        label.color = color;
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.raycastTarget = false;
        return label;
    }

    static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static Transform Buscar(Transform raiz, string nombre)
    {
        Transform[] todos = raiz.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < todos.Length; i++)
            if (todos[i].name == nombre)
                return todos[i];
        return null;
    }

    static void Colocar(Transform objetivo, float y, float ancho, float alto)
    {
        RectTransform rect = objetivo as RectTransform;
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(ancho, alto);
    }
}
