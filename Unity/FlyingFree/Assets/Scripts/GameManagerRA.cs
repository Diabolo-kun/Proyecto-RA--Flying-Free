using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Gestor principal del juego en la escena GameRA:
/// - Controla el tiempo límite de la partida (ej. 30 segundos, configurable en Inspector).
/// - Establece las condiciones de Victoria y Derrota proporcionales al tiempo (ej. 5 o más globos en 30s).
/// - Expone referencias preparadas en el Inspector para arrastrar los Canvases de Victoria y Derrota.
/// - Proporciona los métodos para los botones de "Volver a Jugar" y "Volver al Menú Principal".
/// </summary>
public class GameManagerRA : MonoBehaviour
{
    public static GameManagerRA Instance { get; private set; }

    [Header("Configuración de Tiempo")]
    [Tooltip("Tiempo límite de la partida en segundos (modificable libremente desde el Inspector)")]
    [SerializeField] private float tiempoDeJuego = 30f;

    [Header("Condiciones de Victoria")]
    [Tooltip("Objetivo base de globos explotados para ganar en una partida estándar de 30 segundos")]
    [SerializeField] private int globosObjetivoBase = 5;

    [Tooltip("Si está activo, escala automáticamente la cantidad de globos necesarios en función del tiempo configurado")]
    [SerializeField] private bool objetivoProporcionalAlTiempo = true;

    [Tooltip("Globos requeridos para ganar la partida (se calcula automáticamente o se puede fijar si desactivas la casilla anterior)")]
    [SerializeField] private int globosParaGanar = 5;

    [Header("Canvases de Fin de Partida (Arrastrar en Inspector)")]
    [Tooltip("Canvas o panel que se activará cuando el jugador gane la partida")]
    [SerializeField] private GameObject canvasVictoria;

    [Tooltip("Canvas o panel que se activará cuando el jugador pierda la partida")]
    [SerializeField] private GameObject canvasDerrota;

    [Header("Textos UI de Partida (Opcional)")]
    [Tooltip("Texto para mostrar la cuenta atrás de tiempo restante")]
    [SerializeField] private Text textoTiempo;

    [Tooltip("Texto para mostrar la puntuación y objetivo durante la partida (ej. '3 / 5')")]
    [SerializeField] private Text textoPuntuacion;

    [Tooltip("Texto dentro del Canvas de Victoria para mostrar los globos conseguidos")]
    [SerializeField] private Text textoResultadoVictoria;

    [Tooltip("Texto dentro del Canvas de Derrota para mostrar los globos conseguidos")]
    [SerializeField] private Text textoResultadoDerrota;

    // Estado interno
    private float tiempoRestante;
    private bool partidaEnCurso = false;
    private bool partidaFinalizada = false;
    private bool haGanado = false;

    // Propiedades públicas
    public float TiempoRestante => tiempoRestante;
    public float TiempoTotal => tiempoDeJuego;
    public int GlobosParaGanar => globosParaGanar;
    public bool PartidaEnCurso => partidaEnCurso;
    public bool HaGanado => haGanado;

    // Eventos
    public event Action<bool, int> OnPartidaFinalizada; // bool haGanado, int puntuacionFinal

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Asegurar ScoreManager en la escena si no existiese
        if (ScoreManager.Instance == null)
        {
            gameObject.AddComponent<ScoreManager>();
        }

        CalcularObjetivo();
    }

    private void OnValidate()
    {
        CalcularObjetivo();
    }

    private void CalcularObjetivo()
    {
        if (objetivoProporcionalAlTiempo && tiempoDeJuego > 0f)
        {
            // Calcula de forma proporcional a los 30 segundos (mínimo 1 globo)
            globosParaGanar = Mathf.Max(1, Mathf.RoundToInt((tiempoDeJuego / 30f) * globosObjetivoBase));
        }
    }

    private void Start()
    {
        // Ocultar los canvases de fin de partida al comenzar
        if (canvasVictoria != null) canvasVictoria.SetActive(false);
        if (canvasDerrota != null) canvasDerrota.SetActive(false);

        IniciarPartida();
    }

    /// <summary>
    /// Comienza o reinicia la cuenta atrás de la partida.
    /// </summary>
    public void IniciarPartida()
    {
        CalcularObjetivo();
        tiempoRestante = tiempoDeJuego;
        partidaEnCurso = true;
        partidaFinalizada = false;
        haGanado = false;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetScore();
        }

        ActualizarTextosUI();
        Debug.Log($"[GameManagerRA] Partida iniciada. Tiempo: {tiempoDeJuego}s | Objetivo: {globosParaGanar} globos.");
    }

    private void Update()
    {
        if (!partidaEnCurso) return;

        tiempoRestante -= Time.deltaTime;

        if (tiempoRestante <= 0f)
        {
            tiempoRestante = 0f;
            FinalizarPartida();
        }

        ActualizarTextosUI();
    }

    private void ActualizarTextosUI()
    {
        int scoreActual = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;

        if (textoTiempo != null)
        {
            int segundos = Mathf.CeilToInt(tiempoRestante);
            textoTiempo.text = $"Tiempo: {segundos:00}s";
        }

        if (textoPuntuacion != null)
        {
            textoPuntuacion.text = $"Globos: {scoreActual} / {globosParaGanar}";
        }
    }

    /// <summary>
    /// Ejecuta las condiciones de fin de partida (Victoria o Derrota).
    /// </summary>
    private void FinalizarPartida()
    {
        partidaEnCurso = false;
        partidaFinalizada = true;

        int scoreFinal = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
        haGanado = scoreFinal >= globosParaGanar;

        // Desactivar temporalmente el controlador de vuelo para que no se sigan tirando aviones
        TouchFlightController flight = FindFirstObjectByType<TouchFlightController>();
        if (flight != null)
        {
            flight.enabled = false;
        }

        if (haGanado)
        {
            Debug.Log($"[GameManagerRA] ¡VICTORIA! Globos explotados: {scoreFinal} / {globosParaGanar}");

            if (canvasVictoria != null)
            {
                canvasVictoria.SetActive(true);
            }
            if (canvasDerrota != null)
            {
                canvasDerrota.SetActive(false);
            }

            if (textoResultadoVictoria != null)
            {
                textoResultadoVictoria.text = $"¡Has Ganado!\nGlobos explotados: {scoreFinal} / {globosParaGanar}";
            }
        }
        else
        {
            Debug.Log($"[GameManagerRA] DERROTA. Globos explotados: {scoreFinal} / {globosParaGanar}");

            if (canvasDerrota != null)
            {
                canvasDerrota.SetActive(true);
            }
            if (canvasVictoria != null)
            {
                canvasVictoria.SetActive(false);
            }

            if (textoResultadoDerrota != null)
            {
                textoResultadoDerrota.text = $"¡Has Perdido!\nGlobos explotados: {scoreFinal} / {globosParaGanar}";
            }
        }

        OnPartidaFinalizada?.Invoke(haGanado, scoreFinal);
    }

    // =========================================================================
    // MÉTODOS PÚBLICOS PARA LOS BOTONES DE LOS CANVASES
    // =========================================================================

    /// <summary>
    /// Reinicia la partida en la escena actual de Realidad Aumentada.
    /// Conectar a los botones de "Volver a Jugar" de los Canvases de Victoria y Derrota.
    /// </summary>
    public void VolverAJugar()
    {
        Debug.Log("[GameManagerRA] Reiniciando partida...");
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetScore();
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Vuelve a la escena del Menú Principal.
    /// Conectar a los botones de "Volver al Menú Principal" de los Canvases de Victoria y Derrota.
    /// </summary>
    public void VolverAlMenuPrincipal()
    {
        Debug.Log("[GameManagerRA] Volviendo al Menú Principal...");
        SceneManager.LoadScene("FlyingFreeMenu");
    }

    // =========================================================================
    // HUD / MENÚ ONGUI DE RESPALDO (Funciona mientras no arrastres los Canvases)
    // =========================================================================
    private void OnGUI()
    {
        // Si no has asignado todavía los Canvases en el inspector, muestra este HUD para que puedas probarlo ya mismo
        if (canvasVictoria == null && canvasDerrota == null)
        {
            int score = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;

            // Panel superior con tiempo y objetivo
            GUI.Box(new Rect(10, 10, 260, 65), "");
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };

            int seg = Mathf.CeilToInt(tiempoRestante);
            labelStyle.normal.textColor = seg <= 5 ? Color.red : Color.white;
            GUI.Label(new Rect(20, 15, 240, 24), $"⏱ Tiempo: {seg:00}s / {Mathf.CeilToInt(tiempoDeJuego)}s", labelStyle);

            labelStyle.normal.textColor = score >= globosParaGanar ? Color.green : Color.yellow;
            GUI.Label(new Rect(20, 42, 240, 24), $"🎈 Globos: {score} / {globosParaGanar}", labelStyle);

            // Ventana modal de fin de partida cuando se agota el tiempo
            if (partidaFinalizada)
            {
                float w = 340;
                float h = 210;
                float x = (Screen.width - w) / 2;
                float y = (Screen.height - h) / 2;

                GUI.Box(new Rect(x, y, w, h), "");

                GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                titleStyle.normal.textColor = haGanado ? new Color(0.2f, 0.9f, 0.3f) : new Color(0.95f, 0.25f, 0.2f);

                string titulo = haGanado ? "¡VICTORIA!" : "¡HAS PERDIDO!";
                GUI.Label(new Rect(x + 10, y + 15, w - 20, 30), titulo, titleStyle);

                GUIStyle infoStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter
                };
                string msg = haGanado
                    ? $"¡Has conseguido {score} globos!\n(Objetivo: {globosParaGanar} en {tiempoDeJuego}s)"
                    : $"Solo conseguiste {score} de los {globosParaGanar} globos necesarios.";
                GUI.Label(new Rect(x + 10, y + 55, w - 20, 45), msg, infoStyle);

                if (GUI.Button(new Rect(x + 30, y + 110, w - 60, 38), "🔄 Volver a Jugar"))
                {
                    VolverAJugar();
                }

                if (GUI.Button(new Rect(x + 30, y + 155, w - 60, 38), "🏠 Menú Principal"))
                {
                    VolverAlMenuPrincipal();
                }
            }
        }
    }
}
