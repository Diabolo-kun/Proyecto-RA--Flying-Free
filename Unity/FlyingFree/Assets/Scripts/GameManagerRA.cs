using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Reglas de la ronda: puntuación, lanzamientos, objetivos y resultado único.</summary>
public class GameManagerRA : MonoBehaviour
{
    public static GameManagerRA Instance { get; private set; }
    [Header("Reglas de partida")]
    [SerializeField] bool avionesIlimitados = true;
    [SerializeField, Min(1)] int avionesIniciales = 5;
    [SerializeField, Min(1)] float tiempoDeJuego = 30;
    [SerializeField, Min(1)] int globosObjetivoBase = 5;
    [SerializeField] bool objetivoProporcionalAlTiempo;
    [SerializeField, Min(1)] int globosParaGanar = 5;
    [Tooltip("En AR espera a la marca y pausa el reloj y los lanzamientos si se pierde el seguimiento.")]
    [SerializeField] bool esperarSeguimientoAR;
    [Header("Aviones que fallan el objetivo")]
    [SerializeField, Min(.1f)] float duracionMaximaVuelo = 6f;
    [SerializeField, Min(.1f)] float margenPasadoLaMarca = 1f;
    [SerializeField, Min(.5f)] float radioZonaVuelo = 4f;
    [Header("Resultado")]
    [SerializeField] GameObject canvasVictoria;
    [SerializeField] GameObject canvasDerrota;
    [Header("HUD")]
    [SerializeField] TMP_Text textoTiempo;
    [SerializeField] TMP_Text textoPuntuacion;
    [SerializeField] TMP_Text textoAviones;
    [SerializeField] TMP_Text textoEstado;
    [SerializeField] TMP_Text textoResultadoVictoria;
    [SerializeField] TMP_Text textoResultadoDerrota;
    readonly Dictionary<PaperPlane, Action<bool>> enVuelo = new Dictionary<PaperPlane, Action<bool>>();
    readonly HashSet<PaperPlane> lanzados = new HashSet<PaperPlane>();
    readonly HashSet<GloboTarget> globosContados = new HashSet<GloboTarget>();
    readonly Dictionary<PaperPlane, float> edadVuelos = new Dictionary<PaperPlane, float>();
    readonly List<PaperPlane> vuelosFueraDeZona = new List<PaperPlane>();
    ScoreManager marcador;
    float tiempoRestante;
    bool partidaEnCurso, partidaFinalizada, haGanado, resolviendoImpacto, haIniciado;
    int avionesRestantes, lanzamientosRealizados, aciertos, fallos, robados;
    string motivoFinal = "";
    public float TiempoRestante => tiempoRestante;
    public float TiempoTotal => tiempoDeJuego;
    public int GlobosParaGanar => globosParaGanar;
    public bool PartidaEnCurso => partidaEnCurso;
    public bool HaGanado => haGanado;
    public bool HaIniciado => haIniciado;
    public bool AvionesIlimitados => avionesIlimitados;
    public int AvionesRestantes => avionesIlimitados ? int.MaxValue : avionesRestantes;
    public int AvionesIniciales => avionesIniciales;
    public int AvionesEnVuelo => enVuelo.Count;
    public int AvionesLanzados => lanzamientosRealizados;
    public int AvionesAcertados => aciertos;
    public int AvionesFallados => fallos;
    public int AvionesRobados => robados;
    public int AvionesPerdidos => fallos + robados;
    public string MotivoFinal => motivoFinal;
    public bool EsperandoSeguimiento => partidaEnCurso && esperarSeguimientoAR &&
        (ARGameSpace.Instance == null || !ARGameSpace.Instance.IsTracked);
    public bool PuedeLanzar => partidaEnCurso && !EsperandoSeguimiento && (avionesIlimitados || avionesRestantes > 0) && enVuelo.Count == 0;
    public event Action<bool, int> OnPartidaFinalizada;
    public event Action<int> OnAvionesActualizados;
    public event Action OnPartidaIniciada;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; }
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        marcador = ScoreManager.Instance != null ? ScoreManager.Instance : FindAnyObjectByType<ScoreManager>();
        if (marcador == null) marcador = gameObject.AddComponent<ScoreManager>();
        CalcularObjetivo();
    }
    void OnEnable() { ScoreManager.OnScoreUpdated += AlCambiarPuntuacion; }
    void OnDisable() { ScoreManager.OnScoreUpdated -= AlCambiarPuntuacion; }
    void OnValidate()
    {
        avionesIniciales = Mathf.Max(1, avionesIniciales); tiempoDeJuego = Mathf.Max(1, tiempoDeJuego);
        CalcularObjetivo();
    }
    void CalcularObjetivo()
    {
        int objetivo = objetivoProporcionalAlTiempo
            ? Mathf.RoundToInt(tiempoDeJuego / 30f * globosObjetivoBase) : globosParaGanar;
        globosParaGanar = avionesIlimitados ? Mathf.Max(1, objetivo) : Mathf.Clamp(objetivo, 1, Mathf.Max(1, avionesIniciales));
    }
    void Start() { if (!haIniciado) IniciarPartida(); }
    public void IniciarPartida()
    {
        bool reinicio = haIniciado;
        partidaEnCurso = false; LimpiarAviones();
        foreach (var flock in FindObjectsByType<ControlBandada>(FindObjectsSortMode.None)) flock.LimpiarPajaros();
        CalcularObjetivo(); avionesRestantes = avionesIniciales; lanzamientosRealizados = aciertos = fallos = robados = 0;
        globosContados.Clear(); tiempoRestante = tiempoDeJuego; motivoFinal = "";
        partidaFinalizada = haGanado = resolviendoImpacto = false; haIniciado = true;
        if (canvasVictoria != null) canvasVictoria.SetActive(false);
        if (canvasDerrota != null) canvasDerrota.SetActive(false);
        if (marcador != null) marcador.ResetScore();
        partidaEnCurso = true;
        if (reinicio)
            foreach (var spawner in FindObjectsByType<TargetPlaneSpawner>(FindObjectsSortMode.None)) spawner.ReiniciarRonda();
        foreach (var controller in FindObjectsByType<TouchFlightController>(FindObjectsSortMode.None)) controller.CancelarEntrada();
        ActualizarTextosUI(); OnAvionesActualizados?.Invoke(AvionesRestantes); OnPartidaIniciada?.Invoke();
    }
    public bool TryRegisterLaunch(PaperPlane plane)
    {
        if (!PuedeLanzar || plane == null || !plane.IsFlying || lanzados.Contains(plane)) return false;
        Action<bool> callback = hit => ResolverAvion(plane, hit);
        enVuelo.Add(plane, callback); lanzados.Add(plane); plane.OnFinished += callback;
        edadVuelos.Add(plane, 0);
        if (!avionesIlimitados) avionesRestantes--;
        lanzamientosRealizados++; ActualizarTextosUI(); OnAvionesActualizados?.Invoke(AvionesRestantes);
        return true;
    }
    public bool IsPlaneRegistered(PaperPlane plane) => plane != null && enVuelo.ContainsKey(plane);
    void ResolverAvion(PaperPlane plane, bool acierto)
    {
        if (!enVuelo.TryGetValue(plane, out var callback)) return;
        enVuelo.Remove(plane); plane.OnFinished -= callback;
        edadVuelos.Remove(plane);
        if (!partidaEnCurso) return;
        if (acierto) aciertos++;
        else if (plane.IsCaptured) robados++;
        else fallos++;
        if (!resolviendoImpacto) EvaluarResultado();
        ActualizarTextosUI();
    }
    /// <summary>Transacción única: globo, puntos y fin del avión se resuelven antes de decidir el resultado.</summary>
    public bool TryPopBalloon(GloboTarget balloon, PaperPlane plane, int points)
    {
        if (!partidaEnCurso || EsperandoSeguimiento || balloon == null || plane == null || !plane.IsFlying
            || !enVuelo.ContainsKey(plane) || globosContados.Contains(balloon)) return false;
        globosContados.Add(balloon); resolviendoImpacto = true;
        try
        {
            // Consumir el avión antes de notificar puntos: un listener de OnScoreUpdated
            // puede intentar resolver otro contacto en el mismo frame.
            plane.OnHitBalloon();
            if (marcador != null) marcador.AddScore(Mathf.Max(1, points));
        }
        finally { resolviendoImpacto = false; }
        EvaluarResultado(); ActualizarTextosUI(); return true;
    }
    void AlCambiarPuntuacion(int score)
    {
        if (partidaEnCurso && !resolviendoImpacto) EvaluarResultado();
        ActualizarTextosUI();
    }
    void Update()
    {
        if (!partidaEnCurso) return;
        if (!EsperandoSeguimiento)
        {
            tiempoRestante = Mathf.Max(0, tiempoRestante - Time.deltaTime);
            if (tiempoRestante <= 0) FinalizarPartida();
            if (partidaEnCurso) ComprobarVuelosPerdidos();
        }
        ActualizarTextosUI();
    }
    void ComprobarVuelosPerdidos()
    {
        vuelosFueraDeZona.Clear();
        var space = ARGameSpace.Instance;
        foreach (var entry in enVuelo)
        {
            var plane = entry.Key;
            float age = edadVuelos[plane] + Time.deltaTime; edadVuelos[plane] = age;
            bool fuera = age >= duracionMaximaVuelo;
            if (space != null && age > .15f)
            {
                Vector3 local = space.transform.InverseTransformPoint(plane.transform.position);
                Vector3 cameraLocal = space.GetCameraLocalPosition();
                fuera |= local.z > margenPasadoLaMarca || local.z < cameraLocal.z - 1f
                    || Mathf.Abs(local.x) > radioZonaVuelo || Mathf.Abs(local.y - cameraLocal.y) > radioZonaVuelo;
            }
            if (fuera) vuelosFueraDeZona.Add(plane);
        }
        foreach (var plane in vuelosFueraDeZona) if (plane != null) plane.ExpireFlight();
    }
    void EvaluarResultado()
    {
        if (!partidaEnCurso) return;
        if (marcador != null && marcador.CurrentScore >= globosParaGanar) Terminar(true, "Objetivo de globos completado");
        else if (!avionesIlimitados && avionesRestantes == 0 && enVuelo.Count == 0) Terminar(false, "No quedan aviones");
    }
    // Conserva la entrada de finalización existente, utilizada también por las pruebas de pájaros.
    void FinalizarPartida()
    { Terminar(marcador != null && marcador.CurrentScore >= globosParaGanar, "Se ha agotado el tiempo"); }
    void Terminar(bool victoria, string motivo)
    {
        if (!partidaEnCurso || partidaFinalizada) return;
        partidaEnCurso = false; partidaFinalizada = true; haGanado = victoria; motivoFinal = motivo;
        // Los lanzamientos que seguían pendientes al agotarse el tiempo también quedan resueltos.
        fallos += enVuelo.Count;
        foreach (var entry in enVuelo) if (entry.Key != null) entry.Key.OnFinished -= entry.Value;
        enVuelo.Clear(); edadVuelos.Clear();
        foreach (var plane in lanzados) if (plane != null && plane.IsFlying) plane.CancelForRoundEnd();
        foreach (var controller in FindObjectsByType<TouchFlightController>(FindObjectsSortMode.None)) controller.CancelarEntrada();
        if (canvasVictoria != null) canvasVictoria.SetActive(victoria);
        if (canvasDerrota != null) canvasDerrota.SetActive(!victoria);
        ActualizarTextosUI(); OnPartidaFinalizada?.Invoke(victoria, marcador != null ? marcador.CurrentScore : 0);
    }
    void ActualizarTextosUI()
    {
        int score = marcador != null ? marcador.CurrentScore : 0;
        int seconds = Mathf.CeilToInt(tiempoRestante);
        if (textoTiempo != null) textoTiempo.text = $"{seconds / 60:00}:{seconds % 60:00}";
        if (textoPuntuacion != null) textoPuntuacion.text = $"GLOBOS  {score} / {globosParaGanar}";
        if (textoAviones != null) textoAviones.text = avionesIlimitados ? $"AVIONES  ∞  ·  LANZADOS {lanzamientosRealizados}" : $"AVIONES  {avionesRestantes} / {avionesIniciales}";
        string status = !partidaEnCurso ? motivoFinal : EsperandoSeguimiento ? "APUNTA A LA MARCA PARA CONTINUAR"
            : enVuelo.Count > 0 ? (!avionesIlimitados && avionesRestantes == 0 ? "ÚLTIMO AVIÓN EN VUELO" : "CONTROLA TU AVIÓN") : "LANZA TU AVIÓN";
        if (textoEstado != null) textoEstado.text = status;
        string result = $"{score} de {globosParaGanar} globos\nAciertos: {aciertos} · Fallos: {fallos} · Robos: {robados}\n{motivoFinal}";
        if (textoResultadoVictoria != null) textoResultadoVictoria.text = result;
        if (textoResultadoDerrota != null) textoResultadoDerrota.text = result;
    }
    void LimpiarAviones()
    {
        foreach (var entry in enVuelo) if (entry.Key != null) entry.Key.OnFinished -= entry.Value;
        enVuelo.Clear(); edadVuelos.Clear();
        foreach (var plane in lanzados) if (plane != null) plane.CancelForRoundEnd();
        lanzados.Clear();
    }
    public void VolverAJugar() { SceneManager.LoadScene("GameRA"); }
    public void VolverAlMenuPrincipal() { SceneManager.LoadScene("FlyingFreeMenu"); }
    void OnDestroy() { LimpiarAviones(); if (Instance == this) Instance = null; }
    // Respaldo para las escenas de pruebas que no tienen el HUD de GameRA.
    void OnGUI()
    {
        if (canvasVictoria != null || canvasDerrota != null) return;
        float scale = Mathf.Max(.5f, Screen.width / 960f); var previous = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        string disponibles = avionesIlimitados ? "∞" : $"{avionesRestantes} / {avionesIniciales}";
        GUI.Box(new Rect(12, 12, 330, 95), $"GLOBOS  {(marcador != null ? marcador.CurrentScore : 0)} / {globosParaGanar}\nAVIONES  {disponibles}\nTIEMPO  {Mathf.CeilToInt(tiempoRestante)} s");
        if (partidaFinalizada)
        {
            float x = Screen.width / scale * .5f - 180, y = Screen.height / scale * .5f - 100;
            GUI.Box(new Rect(x, y, 360, 200), (haGanado ? "¡VICTORIA!" : "FIN DE PARTIDA") + "\n" + motivoFinal);
            if (GUI.Button(new Rect(x + 25, y + 85, 310, 42), "VOLVER A JUGAR")) VolverAJugar();
            if (GUI.Button(new Rect(x + 25, y + 138, 310, 42), "MENÚ PRINCIPAL")) VolverAlMenuPrincipal();
        }
        GUI.matrix = previous;
    }
}
