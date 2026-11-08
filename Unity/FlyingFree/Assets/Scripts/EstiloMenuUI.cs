using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using FlyingFree.UI;
using TMPro;

/// <summary>
/// Piezas visuales del menú (colores, sprites, botones) reutilizables en GameRA.
/// </summary>
public static class EstiloMenuUI
{
    public static readonly Color Tinta = new Color(0.09019608f, 0.24313726f, 0.33333334f, 1f);
    public static readonly Color Azul = new Color(0.12941177f, 0.47843137f, 0.7058824f, 1f);
    public static readonly Color Crema = new Color(1f, 0.9647059f, 0.8980392f, 1f);
    public static readonly Color Amarillo = new Color(1f, 0.85490197f, 0.4117647f, 1f);
    public static readonly Color Overlay = new Color(0.045f, 0.16f, 0.23f, 0.72f);

    static Sprite botonAzul;
    static Sprite botonCrema;
    static Sprite panelRedondo;
    static Font fuente;
    static bool cargado;

    public static Sprite BotonAzul { get { Cargar(); return botonAzul; } }
    public static Sprite BotonCrema { get { Cargar(); return botonCrema; } }
    public static Sprite PanelRedondo { get { Cargar(); return panelRedondo; } }
    public static Font Fuente { get { Cargar(); return fuente; } }

    static void Cargar()
    {
        if (cargado) return;
        botonAzul = Resources.Load<Sprite>("FlyingFree/ButtonBlue");
        botonCrema = Resources.Load<Sprite>("FlyingFree/ButtonCream");
        panelRedondo = Resources.Load<Sprite>("FlyingFree/RoundedPanel");
        fuente = Resources.Load<Font>("FlyingFree/Kalam-Bold");
        if (fuente == null)
            fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (panelRedondo == null)
            panelRedondo = CrearPanelRedondo();
        cargado = true;
    }

    static Sprite CrearPanelRedondo()
    {
        const int n = 128;
        const float r = 30f;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float dx = Mathf.Max(Mathf.Abs(x + 0.5f - n * 0.5f) - (n * 0.5f - r), 0f);
            float dy = Mathf.Max(Mathf.Abs(y + 0.5f - n * 0.5f) - (n * 0.5f - r), 0f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f)));
        }
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(32, 32, 32, 32));
    }

    public static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    public static void Caja(RectTransform rt, Vector2 ancla, Vector2 pos, Vector2 tamano)
    {
        rt.anchorMin = rt.anchorMax = ancla;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tamano;
    }

    public static Image CrearPanel(Transform padre, string nombre, Color color, bool raycast = true)
    {
        Cargar();
        var go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(padre, false);
        var img = go.GetComponent<Image>();
        img.sprite = panelRedondo;
        img.type = panelRedondo != null ? Image.Type.Sliced : Image.Type.Simple;
        img.color = color;
        img.raycastTarget = raycast;
        return img;
    }

    public static Text CrearTexto(Transform padre, string nombre, string valor, int tamano, Color color)
    {
        Cargar();
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

    public static Button CrearBotonArte(
        Transform padre,
        string nombre,
        string etiqueta,
        bool primario,
        MenuIconGraphic.Symbol icono,
        Vector2 ancla,
        Vector2 pos,
        Vector2 tamano,
        UnityAction accion,
        int tamanoFuente = 34)
    {
        Cargar();
        var raiz = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(MenuButtonFeedback));
        raiz.transform.SetParent(padre, false);
        Caja(raiz.GetComponent<RectTransform>(), ancla, pos, tamano);

        raiz.GetComponent<Image>().color = Color.clear;

        var visualGo = new GameObject("Arte", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        visualGo.transform.SetParent(raiz.transform, false);
        var visual = visualGo.GetComponent<RectTransform>();
        Estirar(visual);

        var arte = visualGo.GetComponent<Image>();
        Sprite sprite = primario ? botonAzul : botonCrema;
        if (sprite != null)
        {
            arte.sprite = sprite;
            arte.type = Image.Type.Sliced;
            arte.color = Color.white;
        }
        else
        {
            arte.sprite = panelRedondo;
            arte.type = panelRedondo != null ? Image.Type.Sliced : Image.Type.Simple;
            arte.color = primario ? Azul : Crema;
        }
        arte.raycastTarget = false;

        var iconoGo = new GameObject("Icono", typeof(RectTransform), typeof(CanvasRenderer), typeof(MenuIconGraphic));
        iconoGo.transform.SetParent(visualGo.transform, false);
        var iconoRt = iconoGo.GetComponent<RectTransform>();
        iconoRt.anchorMin = iconoRt.anchorMax = new Vector2(0.145f, 0.55f);
        float lado = Mathf.Min(78f, tamano.y * 0.55f);
        iconoRt.sizeDelta = new Vector2(lado, lado);
        var graphic = iconoGo.GetComponent<MenuIconGraphic>();
        graphic.symbol = icono;
        graphic.color = primario ? Color.white : Tinta;
        graphic.raycastTarget = false;
        if (primario) Sombra(iconoGo);

        var texto = CrearTexto(visualGo.transform, "Texto", etiqueta, tamanoFuente, primario ? Color.white : Tinta);
        var textoRt = texto.rectTransform;
        textoRt.anchorMin = new Vector2(0.245f, 0.14f);
        textoRt.anchorMax = new Vector2(0.96f, 0.94f);
        textoRt.offsetMin = textoRt.offsetMax = Vector2.zero;
        if (primario) Sombra(texto.gameObject);

        var button = raiz.GetComponent<Button>();
        button.targetGraphic = arte;
        ColoresBoton(button);
        if (accion != null) button.onClick.AddListener(accion);
        raiz.GetComponent<MenuButtonFeedback>().visual = visual;
        return button;
    }

    public static Button CrearBotonIcono(
        Transform padre,
        string nombre,
        MenuIconGraphic.Symbol icono,
        Vector2 ancla,
        Vector2 pos,
        Vector2 tamano,
        UnityAction accion)
    {
        Cargar();
        var raiz = CrearRaizBotonRedondo(padre, nombre, ancla, pos, tamano, out RectTransform visual);
        var iconoGo = new GameObject("Icono", typeof(RectTransform), typeof(CanvasRenderer), typeof(MenuIconGraphic));
        iconoGo.transform.SetParent(visual, false);
        var iconoRt = iconoGo.GetComponent<RectTransform>();
        iconoRt.anchorMin = iconoRt.anchorMax = new Vector2(0.5f, 0.55f);
        iconoRt.sizeDelta = new Vector2(tamano.x * 0.42f, tamano.y * 0.42f);
        var graphic = iconoGo.GetComponent<MenuIconGraphic>();
        graphic.symbol = icono;
        graphic.color = Color.white;
        graphic.raycastTarget = false;
        Sombra(iconoGo);

        var button = raiz.GetComponent<Button>();
        if (accion != null) button.onClick.AddListener(accion);
        return button;
    }

    public static Button CrearBotonConTexto(
        Transform padre,
        string nombre,
        string etiqueta,
        Vector2 ancla,
        Vector2 pos,
        Vector2 tamano,
        UnityAction accion,
        int tamanoFuente = 48)
    {
        Cargar();
        var raiz = CrearRaizBotonRedondo(padre, nombre, ancla, pos, tamano, out RectTransform visual);
        var texto = CrearTexto(visual, "Texto", etiqueta, tamanoFuente, Color.white);
        Estirar(texto.rectTransform);
        Sombra(texto.gameObject);

        var button = raiz.GetComponent<Button>();
        if (accion != null) button.onClick.AddListener(accion);
        return button;
    }

    static GameObject CrearRaizBotonRedondo(
        Transform padre,
        string nombre,
        Vector2 ancla,
        Vector2 pos,
        Vector2 tamano,
        out RectTransform visual)
    {
        var raiz = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(MenuButtonFeedback));
        raiz.transform.SetParent(padre, false);
        Caja(raiz.GetComponent<RectTransform>(), ancla, pos, tamano);
        raiz.GetComponent<Image>().color = Color.clear;

        var visualGo = new GameObject("Arte", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        visualGo.transform.SetParent(raiz.transform, false);
        visual = visualGo.GetComponent<RectTransform>();
        Estirar(visual);

        var arte = visualGo.GetComponent<Image>();
        if (botonAzul != null)
        {
            arte.sprite = botonAzul;
            arte.type = Image.Type.Sliced;
            arte.color = Color.white;
        }
        else
        {
            arte.sprite = panelRedondo;
            arte.type = panelRedondo != null ? Image.Type.Sliced : Image.Type.Simple;
            arte.color = Azul;
        }
        arte.raycastTarget = false;

        var button = raiz.GetComponent<Button>();
        button.targetGraphic = arte;
        ColoresBoton(button);
        raiz.GetComponent<MenuButtonFeedback>().visual = visual;
        return raiz;
    }

    public static void AplicarPanelCrema(Image imagen, float alpha = 0.96f)
    {
        Cargar();
        if (imagen == null) return;
        imagen.sprite = botonCrema != null ? botonCrema : panelRedondo;
        imagen.type = Image.Type.Sliced;
        imagen.color = botonCrema != null ? new Color(1f, 1f, 1f, alpha) : new Color(Crema.r, Crema.g, Crema.b, alpha);
    }

    public static void PintarTextoTmp(TMP_Text texto, Color color)
    {
        if (texto == null) return;
        texto.color = color;
    }

    public static void AplicarBotonExistente(Button boton, bool primario, MenuIconGraphic.Symbol icono)
    {
        if (boton == null) return;
        Cargar();

        var imagen = boton.GetComponent<Image>();
        if (imagen != null)
        {
            Sprite sprite = primario ? botonAzul : botonCrema;
            if (sprite != null)
            {
                imagen.sprite = sprite;
                imagen.type = Image.Type.Sliced;
                imagen.color = Color.white;
            }
            else
            {
                imagen.sprite = panelRedondo;
                imagen.type = panelRedondo != null ? Image.Type.Sliced : Image.Type.Simple;
                imagen.color = primario ? Azul : Crema;
            }
            boton.targetGraphic = imagen;
        }

        foreach (var tmp in boton.GetComponentsInChildren<TMP_Text>(true))
            PintarTextoTmp(tmp, primario ? Color.white : Tinta);

        foreach (var uiText in boton.GetComponentsInChildren<Text>(true))
        {
            uiText.font = fuente;
            uiText.color = primario ? Color.white : Tinta;
        }

        if (boton.GetComponentInChildren<MenuIconGraphic>(true) == null)
        {
            var iconoGo = new GameObject("Icono", typeof(RectTransform), typeof(CanvasRenderer), typeof(MenuIconGraphic));
            iconoGo.transform.SetParent(boton.transform, false);
            var rt = iconoGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.12f, 0.5f);
            rt.sizeDelta = new Vector2(54f, 54f);
            var g = iconoGo.GetComponent<MenuIconGraphic>();
            g.symbol = icono;
            g.color = primario ? Color.white : Tinta;
            g.raycastTarget = false;
        }

        if (boton.GetComponent<MenuButtonFeedback>() == null)
        {
            var feedback = boton.gameObject.AddComponent<MenuButtonFeedback>();
            feedback.visual = boton.transform as RectTransform;
        }

        ColoresBoton(boton);
    }

    static void ColoresBoton(Button button)
    {
        var colores = button.colors;
        colores.normalColor = Color.white;
        colores.highlightedColor = new Color(1f, 1f, 0.96f, 1f);
        colores.pressedColor = new Color(0.8f, 0.86f, 0.9f, 1f);
        colores.selectedColor = Color.white;
        colores.fadeDuration = 0.08f;
        button.colors = colores;
    }

    static void Sombra(GameObject go)
    {
        var sombra = go.GetComponent<Shadow>();
        if (sombra == null) sombra = go.AddComponent<Shadow>();
        sombra.effectColor = new Color(0.06f, 0.22f, 0.30f, 0.35f);
        sombra.effectDistance = new Vector2(0f, -2f);
    }
}
