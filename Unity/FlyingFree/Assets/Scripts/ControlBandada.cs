using System.Collections.Generic;
using UnityEngine;

/// <summary>Bandada en el mismo espacio AR que los aviones.</summary>
public class ControlBandada : MonoBehaviour
{
    [Header("Pruebas sin marcador (desactivar en GameRA)")]
    public bool testModeWithoutVuforia = true;
    public bool dibujarGizmos = true;
    [Header("Referencias")]
    public Camera camaraAR;
    public GameObject prefabPajaro;
    public ARGameSpace gameSpace;
    [Range(.2f, .8f)] public float distanciaPasillo = .5f;
    [Header("Aparición aleatoria")]
    [Min(0)] public int minimoActivos = 2;
    [Min(1)] public int maximoActivos = 4;
    [Min(.1f)] public float intervaloAparicion = 1.2f;
    [Min(1)] public float cambiarCantidadCada = 12f;
    [Header("Vuelo (metros y segundos del GameSpace)")]
    [Min(.2f)] public float radioVuelo = 1.6f;
    [Min(.05f)] public float velocidad = 1.2f;
    public float alturaRespectoCamara = -.35f;
    [Range(0, .3f)] public float oscilacionVertical = .12f;
    [Range(0, .5f)] public float variacionVelocidad = .2f;
    [Range(.05f, 2)] public float frecuenciaCambioVelocidad = .3f;
    [Range(.05f, 2)] public float frecuenciaCambioAltura = .35f;
    [Min(1)] public int vueltasMinimas = 1;
    [Min(1)] public int vueltasMaximas = 3;
    [Range(0, 70)] public float dispersionDetras = 50;
    [Min(.1f)] public float separacionEntrePajaros = .65f;
    readonly List<PajaroOrbital> activos = new List<PajaroOrbital>();
    int cantidadObjetivo;
    float siguienteAparicion, siguienteCambio;
    bool estabaJugando;
    public int PajarosActivosCount { get { activos.RemoveAll(p => p == null); return activos.Count; } }
    public bool PuedeSimular => isActiveAndEnabled &&
        (GameManagerRA.Instance == null || GameManagerRA.Instance.PartidaEnCurso) &&
        (testModeWithoutVuforia || (gameSpace != null && gameSpace.IsTracked));
    void OnValidate()
    {
        maximoActivos = Mathf.Max(1, maximoActivos);
        minimoActivos = Mathf.Clamp(minimoActivos, 0, maximoActivos);
        vueltasMinimas = Mathf.Max(1, vueltasMinimas);
        vueltasMaximas = Mathf.Max(vueltasMinimas, vueltasMaximas);
        intervaloAparicion = Mathf.Max(.1f, intervaloAparicion);
    }
    void Start()
    {
        if (camaraAR == null) camaraAR = Camera.main;
        if (gameSpace == null) gameSpace = ARGameSpace.GetOrCreateInstance();
        ElegirCantidad(); siguienteAparicion = Time.time + .5f;
    }
    void ElegirCantidad()
    {
        cantidadObjetivo = Random.Range(Mathf.Clamp(minimoActivos, 0, maximoActivos), maximoActivos + 1);
        siguienteCambio = Time.time + Mathf.Max(1, cambiarCantidadCada);
    }
    void Update()
    {
        activos.RemoveAll(p => p == null);
        bool jugando = GameManagerRA.Instance == null || GameManagerRA.Instance.PartidaEnCurso;
        if (!jugando && estabaJugando) LimpiarPajaros();
        estabaJugando = jugando;
        if (!PuedeSimular || camaraAR == null) return;
        if (Time.time >= siguienteCambio) ElegirCantidad();
        if (Time.time < siguienteAparicion || activos.Count >= cantidadObjetivo) return;
        CrearPajaro();
        siguienteAparicion = Time.time + intervaloAparicion * Random.Range(.8f, 1.25f);
    }
    public Vector3 GetCorridorCenterLocal()
    {
        if (gameSpace == null || camaraAR == null) return Vector3.zero;
        Vector3 cameraLocal = gameSpace.transform.InverseTransformPoint(camaraAR.transform.position);
        float back = Mathf.Min(-.8f, cameraLocal.z - .8f);
        return new Vector3(cameraLocal.x * distanciaPasillo,
            cameraLocal.y + alturaRespectoCamara, (back - .4f) * .5f);
    }
    public Vector2 GetOrbitRadii()
    {
        Vector3 cam = gameSpace != null && camaraAR != null
            ? gameSpace.transform.InverseTransformPoint(camaraAR.transform.position) : new Vector3(0, 0, -2.2f);
        float back = Mathf.Min(-.8f, cam.z - .8f);
        return new Vector2(Mathf.Max(.4f, radioVuelo), (-.4f - back) * .5f);
    }
    [ContextMenu("Forzar Generar Pájaro")]
    public void CrearPajaro()
    {
        if (!Application.isPlaying || !PuedeSimular || prefabPajaro == null || gameSpace == null || camaraAR == null
            || PajarosActivosCount >= maximoActivos) return;
        Vector3 center = GetCorridorCenterLocal(); Vector2 radii = GetOrbitRadii();
        float initial = Random.Range(0, 360f);
        for (int i = 0; i < 48; i++)
        {
            float angle = initial + i * 7.5f; float a = angle * Mathf.Deg2Rad;
            Vector3 local = center + new Vector3(Mathf.Cos(a) * radii.x, 0, Mathf.Sin(a) * radii.y);
            Vector3 world = gameSpace.transform.TransformPoint(local);
            float scale = Mathf.Abs(gameSpace.transform.lossyScale.x);
            if (!PajaroOrbital.OcultoDetras(camaraAR, world, .5f * scale)) continue;
            bool libre = true;
            foreach (var bird in activos)
                if (bird != null && Vector3.Distance(world, bird.transform.position) < separacionEntrePajaros * scale) { libre = false; break; }
            if (!libre) continue;
            GameObject obj = Instantiate(prefabPajaro, world, Quaternion.identity, gameSpace.transform);
            var interaction = obj.GetComponent<PajaroInteraccion>();
            if (interaction == null) interaction = obj.AddComponent<PajaroInteraccion>();
            interaction.Configurar(this, camaraAR);
            var orbit = obj.GetComponent<PajaroOrbital>();
            if (orbit == null) orbit = obj.AddComponent<PajaroOrbital>();
            orbit.ConfigurarVariaciones(variacionVelocidad, frecuenciaCambioVelocidad, frecuenciaCambioAltura);
            orbit.Iniciar(camaraAR, center, radii.x, velocidad, 0, oscilacionVertical, angle,
                Random.Range(vueltasMinimas, vueltasMaximas + 1), Random.value < .5f ? -1 : 1, this);
            activos.Add(orbit); return;
        }
    }
    [ContextMenu("Limpiar Todos los Pájaros")]
    public void LimpiarPajaros()
    {
        foreach (var bird in activos) if (bird != null)
        {
            bird.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(bird.gameObject); else DestroyImmediate(bird.gameObject);
        }
        activos.Clear();
    }
    void OnDisable() { LimpiarPajaros(); }
    void OnDrawGizmosSelected()
    {
        if (!dibujarGizmos || gameSpace == null || camaraAR == null) return;
        Vector3 center = GetCorridorCenterLocal(); Vector2 radii = GetOrbitRadii(); Gizmos.color = Color.cyan;
        Vector3 previous = gameSpace.transform.TransformPoint(center + Vector3.right * radii.x);
        for (int i = 1; i <= 48; i++)
        {
            float a = i * Mathf.PI / 24;
            Vector3 next = gameSpace.transform.TransformPoint(center + new Vector3(Mathf.Cos(a) * radii.x, 0, Mathf.Sin(a) * radii.y));
            Gizmos.DrawLine(previous, next); previous = next;
        }
    }
}
