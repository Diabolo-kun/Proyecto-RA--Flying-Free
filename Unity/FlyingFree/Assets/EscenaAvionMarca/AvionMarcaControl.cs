using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Vuforia;
using FaseToque = UnityEngine.InputSystem.TouchPhase;
using Toque = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Escena aparte: al enfocar la marca del avión aparece el modelo.
/// Pellizco cambia el tamaño, deslizar a los lados lo gira
/// y deslizar arriba o abajo lo voltea. Las flechas cambian de modelo
/// y, al llegar al último, vuelven al primero.
/// </summary>
public class AvionMarcaControl : MonoBehaviour
{
    [Tooltip("Prefabs de avión, en el orden de las flechas. Se puede ampliar sin cambiar el código.")]
    [SerializeField] GameObject[] modelos;
    [SerializeField] Sprite botonAzul;
    [SerializeField] Sprite botonCrema;
    [SerializeField] Font fuenteMenu;
    [SerializeField] Vector3 desplazamiento = new Vector3(0f, 0.02f, 0f);
    [SerializeField] Vector3 rotacionInicial = Vector3.zero;
    [SerializeField] float escalaInicial = 0.55f;
    [SerializeField] float factorEscala = 1.08f;
    [SerializeField] float escalaMinima = 0.2f;
    [SerializeField] float escalaMaxima = 3f;

    const string ClaveAvion = "FlyingFree.AvionSeleccionado";
    const string AvionDefecto = "Avion_Cuaderno";

    static readonly Color Tinta = new Color(0.09019608f, 0.24313726f, 0.33333334f, 1f);
    static readonly Color Azul = new Color(0.12941177f, 0.47843137f, 0.7058824f, 1f);

    Transform anclaje;
    Text etiqueta;
    readonly List<GameObject> instancias = new List<GameObject>();
    readonly List<string> nombres = new List<string>();
    RectTransform flechaIzquierda;
    RectTransform flechaDerecha;
    RectTransform botonVolver;
    ObserverBehaviour observador;
    int indice;
    bool marcadoVisible;

    bool pellizcoActivo;
    float distanciaAnterior = 1f;
    bool arrastrando;
    bool girando;
    bool gestoEnFlecha;
    int sentidoFlecha;
    Vector2 inicioGesto;
    Vector2 posicionAnterior;

    void Awake()
    {
        observador = GetComponent<ObserverBehaviour>();
        CrearAnclaje();
        CrearModelos();
        CrearAyuda();
        ActivarModelo(IndiceInicial());
        AplicarVisibilidad();
    }

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        if (observador != null)
            observador.OnTargetStatusChanged += AlCambiarEstado;
    }

    void OnDisable()
    {
        if (observador != null)
            observador.OnTargetStatusChanged -= AlCambiarEstado;
        EnhancedTouchSupport.Disable();
    }

    void Update()
    {
        var dedos = Toque.activeTouches;
        if (dedos.Count >= 2)
        {
            arrastrando = false;
            gestoEnFlecha = false;
            if (marcadoVisible && anclaje != null && !SobreFlechas(dedos[0].screenPosition) && !SobreFlechas(dedos[1].screenPosition))
                ProcesarPellizco(dedos[0], dedos[1]);
            else
                pellizcoActivo = false;
            return;
        }

        pellizcoActivo = false;
        if (dedos.Count == 1)
            ProcesarUnDedo(dedos[0]);
        else
            ProcesarRaton();
    }

    void AlCambiarEstado(ObserverBehaviour behaviour, TargetStatus status)
    {
        marcadoVisible = status.Status == Status.TRACKED;
        AplicarVisibilidad();
    }

    void CrearAnclaje()
    {
        var go = new GameObject("AvionAnclaje");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = desplazamiento;
        go.transform.localRotation = Quaternion.Euler(rotacionInicial);
        go.transform.localScale = Vector3.one * escalaInicial;
        anclaje = go.transform;
    }

    void CrearModelos()
    {
        instancias.Clear();
        nombres.Clear();
        if (modelos == null)
            return;

        for (int i = 0; i < modelos.Length; i++)
        {
            if (modelos[i] == null)
                continue;

            var instancia = Instantiate(modelos[i], anclaje);
            instancia.name = modelos[i].name;
            instancia.transform.localPosition = Vector3.zero;
            instancia.transform.localRotation = Quaternion.identity;
            instancia.transform.localScale = Vector3.one;

            var motor = instancia.GetComponent<FlyingFree.Sprint2.PaperPlaneMotor>();
            var perfil = motor != null ? motor.definition : null;
            QuitarFisica(instancia);

            string nombre = perfil != null && !string.IsNullOrEmpty(perfil.displayName)
                ? perfil.displayName
                : instancia.name.Replace("Avion_", "").Replace("_", " ");

            instancias.Add(instancia);
            nombres.Add(nombre);
        }
    }

    static void QuitarFisica(GameObject instancia)
    {
        var motor = instancia.GetComponent<FlyingFree.Sprint2.PaperPlaneMotor>();
        if (motor != null)
            DestroyImmediate(motor);

        var vuelo = instancia.GetComponent<PaperPlane>();
        if (vuelo != null)
            DestroyImmediate(vuelo);

        var cuerpo = instancia.GetComponent<Rigidbody>();
        if (cuerpo != null)
            DestroyImmediate(cuerpo);
    }

    void ProcesarPellizco(Toque a, Toque b)
    {
        if (a.phase == FaseToque.Ended || a.phase == FaseToque.Canceled ||
            b.phase == FaseToque.Ended || b.phase == FaseToque.Canceled)
        {
            pellizcoActivo = false;
            return;
        }

        float distancia = Vector2.Distance(a.screenPosition, b.screenPosition);
        if (!pellizcoActivo)
        {
            pellizcoActivo = true;
            distanciaAnterior = Mathf.Max(distancia, 1f);
            return;
        }

        AplicarFactor(distancia / Mathf.Max(distanciaAnterior, 1f));
        distanciaAnterior = Mathf.Max(distancia, 1f);
    }

    void ProcesarUnDedo(Toque dedo)
    {
        if (dedo.phase == FaseToque.Began)
        {
            EmpezarGesto(dedo.screenPosition);
            return;
        }

        if (!arrastrando)
            return;

        if (dedo.phase == FaseToque.Ended || dedo.phase == FaseToque.Canceled)
        {
            TerminarGesto(dedo.screenPosition);
            return;
        }

        SeguirGiro(dedo.screenPosition);
    }

    void ProcesarRaton()
    {
        var raton = UnityEngine.InputSystem.Mouse.current;
        if (raton == null)
            return;

        float rueda = raton.scroll.ReadValue().y;
        if (Mathf.Abs(rueda) > 0.1f)
            AplicarFactor(rueda > 0f ? factorEscala : 1f / factorEscala);

        if (raton.leftButton.wasPressedThisFrame)
            EmpezarGesto(raton.position.ReadValue());
        else if (raton.leftButton.isPressed && arrastrando)
            SeguirGiro(raton.position.ReadValue());
        else if (raton.leftButton.wasReleasedThisFrame && arrastrando)
            TerminarGesto(raton.position.ReadValue());
    }

    void EmpezarGesto(Vector2 posicion)
    {
        inicioGesto = posicion;
        posicionAnterior = posicion;
        arrastrando = true;
        girando = false;
        sentidoFlecha = FlechaEn(posicion);
        gestoEnFlecha = sentidoFlecha != 0;
    }

    void SeguirGiro(Vector2 posicion)
    {
        if (gestoEnFlecha || !marcadoVisible || anclaje == null)
        {
            posicionAnterior = posicion;
            return;
        }

        if ((posicion - inicioGesto).magnitude > 12f)
            girando = true;

        if (girando)
        {
            Transform referencia = anclaje.parent != null ? anclaje.parent : anclaje;
            Vector2 delta = posicion - posicionAnterior;
            float giro = delta.x * (360f / Mathf.Max(Screen.width, 1));
            float volteo = -delta.y * (360f / Mathf.Max(Screen.height, 1));
            anclaje.Rotate(referencia.up, giro, Space.World);
            anclaje.Rotate(referencia.right, volteo, Space.World);
        }

        posicionAnterior = posicion;
    }

    void TerminarGesto(Vector2 posicion)
    {
        bool eraFlecha = gestoEnFlecha;
        SeguirGiro(posicion);
        arrastrando = false;
        gestoEnFlecha = false;
        sentidoFlecha = 0;

        if (eraFlecha)
            return;
    }

    bool SobreFlechas(Vector2 pantalla)
    {
        return FlechaEn(pantalla) != 0;
    }

    int FlechaEn(Vector2 pantalla)
    {
        if (flechaIzquierda != null && RectTransformUtility.RectangleContainsScreenPoint(flechaIzquierda, pantalla, null))
            return -1;

        if (flechaDerecha != null && RectTransformUtility.RectangleContainsScreenPoint(flechaDerecha, pantalla, null))
            return 1;

        if (botonVolver != null && RectTransformUtility.RectangleContainsScreenPoint(botonVolver, pantalla, null))
            return 1;

        return 0;
    }

    void AplicarFactor(float factor)
    {
        if (anclaje == null || factor <= 0f)
            return;

        float nueva = Mathf.Clamp(anclaje.localScale.x * factor, escalaMinima, escalaMaxima);
        anclaje.localScale = Vector3.one * nueva;
    }

    void CambiarModelo(int sentido)
    {
        if (instancias.Count == 0 || sentido == 0)
            return;

        ActivarModelo(indice + sentido);
    }

    void ActivarModelo(int nuevoIndice)
    {
        if (instancias.Count == 0)
            return;

        indice = ((nuevoIndice % instancias.Count) + instancias.Count) % instancias.Count;
        for (int i = 0; i < instancias.Count; i++)
            instancias[i].SetActive(i == indice);

        if (marcadoVisible)
            ActivarRenderersDelModeloActual();

        RefrescarEtiqueta();
        if (instancias[indice] != null)
            GuardarAvion(instancias[indice].name);
    }

    int IndiceInicial()
    {
        int encontrado = IndiceDe(NombreAvionGuardado());
        if (encontrado >= 0)
            return encontrado;

        int cuaderno = IndiceDe(AvionDefecto);
        return cuaderno >= 0 ? cuaderno : 0;
    }

    static void GuardarAvion(string nombrePrefab)
    {
        if (string.IsNullOrEmpty(nombrePrefab))
            return;

        PlayerPrefs.SetString(ClaveAvion, nombrePrefab);
        PlayerPrefs.Save();
    }

    static string NombreAvionGuardado()
    {
        string nombre = PlayerPrefs.GetString(ClaveAvion, "");
        return string.IsNullOrEmpty(nombre) ? AvionDefecto : nombre;
    }

    int IndiceDe(string nombre)
    {
        for (int i = 0; i < instancias.Count; i++)
        {
            if (instancias[i] != null && instancias[i].name == nombre)
                return i;
        }

        return -1;
    }

    void AplicarVisibilidad()
    {
        if (anclaje != null)
            anclaje.gameObject.SetActive(marcadoVisible);

        if (marcadoVisible)
            ActivarRenderersDelModeloActual();

        RefrescarEtiqueta();
    }

    void ActivarRenderersDelModeloActual()
    {
        if (indice < 0 || indice >= instancias.Count || instancias[indice] == null)
            return;

        var renderers = instancias[indice].GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = true;
    }

    void RefrescarEtiqueta()
    {
        if (etiqueta != null)
            etiqueta.text = NombreModelo(indice);
    }

    string NombreModelo(int i)
    {
        if (i < 0 || i >= nombres.Count || string.IsNullOrEmpty(nombres[i]))
            return "Avión";

        return nombres[i];
    }

    void CrearAyuda()
    {
        var canvasGo = new GameObject("UI_AvionMarca");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        canvasGo.AddComponent<GraphicRaycaster>();

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        var fuente = fuenteMenu != null ? fuenteMenu : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var fondo = CrearPanel(canvasGo.transform, Color.white, false);
        if (botonCrema != null)
        {
            fondo.sprite = botonCrema;
            fondo.type = UnityEngine.UI.Image.Type.Sliced;
        }
        else
            fondo.color = new Color(1f, 0.9647059f, 0.8980392f, 1f);
        Colocar(fondo.rectTransform, 0.07f, 0.93f, 0.86f, 0.97f);

        var titulo = CrearTexto(fondo.transform, "Selecciona avión", fuente, 42, Tinta);
        QuitarSombra(titulo);
        var zonaTitulo = titulo.rectTransform;
        zonaTitulo.anchorMin = new Vector2(0.08f, 0.5f);
        zonaTitulo.anchorMax = new Vector2(0.92f, 0.94f);
        zonaTitulo.offsetMin = Vector2.zero;
        zonaTitulo.offsetMax = Vector2.zero;

        etiqueta = CrearTexto(fondo.transform, NombreModelo(0), fuente, 54, Azul);
        etiqueta.fontStyle = FontStyle.Bold;
        QuitarSombra(etiqueta);
        var zonaNombre = etiqueta.rectTransform;
        zonaNombre.anchorMin = new Vector2(0.08f, 0.06f);
        zonaNombre.anchorMax = new Vector2(0.92f, 0.52f);
        zonaNombre.offsetMin = Vector2.zero;
        zonaNombre.offsetMax = Vector2.zero;

        CrearFlechas(canvasGo.transform);
        CrearVolver(canvasGo.transform, fuente);
    }

    void CrearVolver(Transform canvas, Font fuente)
    {
        var raiz = new GameObject("Volver");
        raiz.transform.SetParent(canvas, false);
        var toque = raiz.AddComponent<UnityEngine.UI.Image>();
        toque.color = Color.clear;

        var arteGo = new GameObject("Arte");
        arteGo.transform.SetParent(raiz.transform, false);
        var arte = arteGo.AddComponent<UnityEngine.UI.Image>();
        arte.sprite = botonCrema != null ? botonCrema : botonAzul;
        arte.type = arte.sprite != null ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
        arte.color = arte.sprite != null ? Color.white : new Color(1f, 0.9647059f, 0.8980392f, 1f);
        arte.raycastTarget = false;
        Estirar(arte.rectTransform);

        var texto = CrearTexto(arteGo.transform, "SELECCIONAR AVIÓN", fuente, 32, Tinta);
        QuitarSombra(texto);
        Estirar(texto.rectTransform);

        var boton = raiz.AddComponent<Button>();
        boton.targetGraphic = arte;
        var colores = boton.colors;
        colores.normalColor = Color.white;
        colores.highlightedColor = new Color(1f, 1f, 0.96f, 1f);
        colores.pressedColor = new Color(0.8f, 0.86f, 0.9f, 1f);
        colores.selectedColor = Color.white;
        colores.fadeDuration = 0.08f;
        boton.colors = colores;
        boton.onClick.AddListener(VolverAlMenu);

        var animacion = raiz.AddComponent<FlyingFree.UI.MenuButtonFeedback>();
        animacion.visual = arte.rectTransform;

        botonVolver = raiz.GetComponent<RectTransform>();
        Caja(botonVolver, 0f, -680f, 460f, 156f);
    }

    void VolverAlMenu()
    {
        if (indice >= 0 && indice < instancias.Count && instancias[indice] != null)
            GuardarAvion(instancias[indice].name);

        SceneManager.LoadScene("FlyingFreeMenu");
    }

    void CrearFlechas(Transform canvas)
    {
        flechaIzquierda = CrearFlecha(canvas, true);
        Caja(flechaIzquierda, -390f, -680f, 220f, 156f);

        flechaDerecha = CrearFlecha(canvas, false);
        Caja(flechaDerecha, 390f, -680f, 220f, 156f);
    }

    RectTransform CrearFlecha(Transform canvas, bool izquierda)
    {
        var raiz = new GameObject(izquierda ? "Flecha izquierda" : "Flecha derecha");
        raiz.transform.SetParent(canvas, false);
        var toque = raiz.AddComponent<UnityEngine.UI.Image>();
        toque.color = Color.clear;

        var arteGo = new GameObject("Arte");
        arteGo.transform.SetParent(raiz.transform, false);
        var arte = arteGo.AddComponent<UnityEngine.UI.Image>();
        arte.sprite = botonAzul;
        arte.type = botonAzul != null ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
        arte.color = botonAzul != null ? Color.white : Azul;
        arte.raycastTarget = false;
        Estirar(arte.rectTransform);

        var iconoGo = new GameObject("Icono");
        iconoGo.transform.SetParent(arteGo.transform, false);
        var icono = iconoGo.AddComponent<FlyingFree.UI.MenuIconGraphic>();
        icono.symbol = FlyingFree.UI.MenuIconGraphic.Symbol.Back;
        icono.color = Color.white;
        icono.raycastTarget = false;
        var zonaIcono = icono.rectTransform;
        zonaIcono.anchorMin = zonaIcono.anchorMax = new Vector2(0.5f, 0.55f);
        zonaIcono.sizeDelta = new Vector2(78f, 78f);
        zonaIcono.anchoredPosition = Vector2.zero;
        if (!izquierda)
            zonaIcono.localScale = new Vector3(-1f, 1f, 1f);
        var sombraIcono = iconoGo.AddComponent<Shadow>();
        sombraIcono.effectColor = new Color(0.06f, 0.22f, 0.30f, 0.35f);
        sombraIcono.effectDistance = new Vector2(0f, -2f);

        var boton = raiz.AddComponent<Button>();
        boton.targetGraphic = arte;
        var colores = boton.colors;
        colores.normalColor = Color.white;
        colores.highlightedColor = new Color(1f, 1f, 0.96f, 1f);
        colores.pressedColor = new Color(0.8f, 0.86f, 0.9f, 1f);
        colores.selectedColor = Color.white;
        colores.fadeDuration = 0.08f;
        boton.colors = colores;
        int sentido = izquierda ? -1 : 1;
        boton.onClick.AddListener(() => CambiarModelo(sentido));

        var animacion = raiz.AddComponent<FlyingFree.UI.MenuButtonFeedback>();
        animacion.visual = arte.rectTransform;
        return raiz.GetComponent<RectTransform>();
    }

    static void Caja(RectTransform rect, float x, float y, float ancho, float alto)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(ancho, alto);
    }

    static void QuitarSombra(Text texto)
    {
        var sombra = texto.GetComponent<Shadow>();
        if (sombra != null)
            sombra.enabled = false;
    }

    static void Estirar(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static UnityEngine.UI.Image CrearPanel(Transform padre, Color color, bool recibeToque = false)
    {
        var go = new GameObject("Aviso");
        go.transform.SetParent(padre, false);
        var imagen = go.AddComponent<UnityEngine.UI.Image>();
        imagen.color = color;
        imagen.raycastTarget = recibeToque;
        return imagen;
    }

    static Text CrearTexto(Transform padre, string contenido, Font fuente, int tamano, Color color)
    {
        var go = new GameObject("Texto");
        go.transform.SetParent(padre, false);
        var texto = go.AddComponent<Text>();
        texto.font = fuente;
        texto.text = contenido;
        texto.fontSize = tamano;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = color;
        texto.horizontalOverflow = HorizontalWrapMode.Wrap;
        texto.verticalOverflow = VerticalWrapMode.Truncate;
        texto.raycastTarget = false;
        texto.resizeTextForBestFit = true;
        texto.resizeTextMinSize = 18;
        texto.resizeTextMaxSize = tamano;

        var sombra = go.AddComponent<Shadow>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.8f);
        sombra.effectDistance = new Vector2(2f, -2f);
        return texto;
    }

    static void Colocar(RectTransform rect, float xMin, float xMax, float yMin, float yMax)
    {
        rect.anchorMin = new Vector2(xMin, yMin);
        rect.anchorMax = new Vector2(xMax, yMax);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
