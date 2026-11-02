using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

/// <summary>
/// Controla la bandada de pájaros que orbitan alrededor de la cámara.
/// Admite tanto el modo Vuforia AR real (mediante Anchor espacial de Vuforia)
/// como un MODO DE PRUEBAS sin Vuforia para probar cómodamente en PC con cualquier cámara.
/// </summary>
public class ControlBandada : MonoBehaviour
{
    [Header("MODO DE PRUEBAS (SIN VUFORIA)")]
    [Tooltip("Si está activo, simula el ancla espacial y permite probar en el editor sin esperar a ARCore/ARKit/Vuforia")]
    public bool testModeWithoutVuforia = true;

    [Tooltip("Dibuja la órbita de vuelo y el área trasera de spawn en la Scene View")]
    public bool dibujarGizmos = true;

    [Header("REFERENCIAS")]
    public Camera camaraAR;
    public GameObject prefabPajaro;

    [Header("PAJAROS ACTIVOS")]
    [Min(0)] public int minimoActivos = 2;
    [Min(1)] public int maximoActivos = 4;
    [Min(0.1f)] public float intervaloAparicion = 1.2f;
    [Min(1f)] public float cambiarCantidadCada = 12f;

    [Header("VUELO")]
    [Min(0.2f)] public float radioVuelo = 1.6f;
    [Min(0.05f)] public float velocidad = 1.2f;
    public float alturaRespectoCamara = -0.35f;
    [Range(0f, 0.15f)]
    public float oscilacionVertical = 0.04f;

    [Header("VARIACIONES NATURALES")]
    [Range(0f, 0.5f)]
    public float variacionVelocidad = 0.20f;

    [Range(0.05f, 2f)]
    public float frecuenciaCambioVelocidad = 0.30f;

    [Range(0.05f, 2f)]
    public float frecuenciaCambioAltura = 0.35f;

    [Header("VUELTAS ALEATORIAS")]
    [Min(1)] public int vueltasMinimas = 1;
    [Min(1)] public int vueltasMaximas = 3;

    [Header("APARICION OCULTA")]
    [Range(0f, 70f)]
    public float dispersionDetras = 50f;

    private AnchorBehaviour ancla;
    private Transform anclaTransform;
    private List<PajaroOrbital> activos = new List<PajaroOrbital>();

    private int cantidadObjetivo;
    private float siguienteAparicion;
    private float siguienteCambio;
    private bool preparado;

    public int PajarosActivosCount => activos.Count;

    private void OnValidate()
    {
        maximoActivos = Mathf.Max(1, maximoActivos);
        minimoActivos = Mathf.Clamp(minimoActivos, 0, maximoActivos);

        vueltasMinimas = Mathf.Max(1, vueltasMinimas);
        vueltasMaximas = Mathf.Max(vueltasMinimas, vueltasMaximas);
    }

    private void Start()
    {
        if (camaraAR == null)
            camaraAR = Camera.main;

        if (camaraAR == null)
        {
            Debug.LogError("[ControlBandada] No se ha encontrado ninguna cámara (asigna camaraAR o pon Tag MainCamera).");
            enabled = false;
            return;
        }

        if (prefabPajaro == null)
        {
            Debug.LogWarning("[ControlBandada] No has asignado el prefabPajaro. Se creará uno temporal procedural para pruebas.");
        }

        StartCoroutine(PrepararSistema());
    }

    private IEnumerator PrepararSistema()
    {
        // -------------------------------------------------------------
        // MODO 1: SIMULACIÓN DE PRUEBAS (SIN VUFORIA)
        // -------------------------------------------------------------
        if (testModeWithoutVuforia)
        {
            GameObject anclaSimulada = new GameObject("AnclaBandada_Simulada");
            anclaSimulada.transform.position = camaraAR.transform.position;
            anclaSimulada.transform.rotation = Quaternion.identity;
            anclaTransform = anclaSimulada.transform;

            cantidadObjetivo = Random.Range(minimoActivos, maximoActivos + 1);
            siguienteAparicion = Time.time + 0.5f;
            siguienteCambio = Time.time + cambiarCantidadCada;

            preparado = true;
            Debug.Log($"[ControlBandada] MODO TEST ACTIVO: Sistema listo alrededor de '{camaraAR.name}' sin requerir Vuforia.");
            yield break;
        }

        // -------------------------------------------------------------
        // MODO 2: VUFORIA DEVICE TRACKING REAL
        // -------------------------------------------------------------
        float inicio = Time.time;
        bool avisoMostrado = false;

        while (true)
        {
            VuforiaBehaviour v = VuforiaBehaviour.Instance;

            if (v != null &&
                v.DevicePoseBehaviour != null &&
                v.DevicePoseBehaviour.TargetStatus.Status == Status.TRACKED)
            {
                ancla = v.ObserverFactory.CreateAnchorBehaviour(
                    "AnclaBandada_" + Time.frameCount,
                    camaraAR.transform.position,
                    Quaternion.identity
                );
                anclaTransform = ancla.transform;
                break;
            }

            if (!avisoMostrado && Time.time - inicio > 8f)
            {
                Debug.LogWarning("[ControlBandada] Esperando Device Tracking de Vuforia. Si estás en PC puedes activar 'Test Mode Without Vuforia' en el Inspector.");
                avisoMostrado = true;
            }

            yield return null;
        }

        if (ancla == null)
        {
            Debug.LogError("[ControlBandada] No se ha podido crear el ancla espacial de Vuforia.");
            yield break;
        }

        cantidadObjetivo = Random.Range(minimoActivos, maximoActivos + 1);
        siguienteAparicion = Time.time + 1f;
        siguienteCambio = Time.time + cambiarCantidadCada;

        preparado = true;
    }

    private bool SeguimientoValido()
    {
        if (testModeWithoutVuforia)
            return anclaTransform != null;

        VuforiaBehaviour v = VuforiaBehaviour.Instance;

        if (v == null || v.DevicePoseBehaviour == null || ancla == null)
            return false;

        Status estadoDevice = v.DevicePoseBehaviour.TargetStatus.Status;
        Status estadoAncla = ancla.TargetStatus.Status;

        bool deviceCorrecto = estadoDevice == Status.TRACKED || estadoDevice == Status.EXTENDED_TRACKED;
        bool anclaCorrecta = estadoAncla == Status.TRACKED || estadoAncla == Status.EXTENDED_TRACKED;

        return deviceCorrecto && anclaCorrecta;
    }

    private void Update()
    {
        if (!preparado)
            return;

        // Eliminar de la lista los pájaros destruidos
        activos.RemoveAll(p => p == null);

        // Si el seguimiento no es válido, no generar nuevos
        if (!SeguimientoValido())
            return;

        // Elegir una nueva cantidad objetivo periódicamente
        if (Time.time >= siguienteCambio)
        {
            cantidadObjetivo = Random.Range(minimoActivos, maximoActivos + 1);
            siguienteCambio = Time.time + cambiarCantidadCada;
        }

        if (Time.time >= siguienteAparicion &&
            activos.Count < cantidadObjetivo &&
            activos.Count < maximoActivos)
        {
            CrearPajaro();
            siguienteAparicion = Time.time + intervaloAparicion;
        }
    }

    /// <summary>
    /// Genera un pájaro en una posición oculta detrás de la vista de la cámara e inicia su vuelo orbital.
    /// </summary>
    [ContextMenu("Forzar Generar Pájaro")]
    public void CrearPajaro()
    {
        if (camaraAR == null) camaraAR = Camera.main;
        if (camaraAR == null) return;

        EnsureAnchor();

        // Dirección horizontal hacia donde mira la cámara
        Vector3 adelante = Vector3.ProjectOnPlane(camaraAR.transform.forward, Vector3.up);

        if (adelante.sqrMagnitude < 0.001f)
            adelante = Vector3.ProjectOnPlane(camaraAR.transform.up, Vector3.up);

        if (adelante.sqrMagnitude < 0.001f)
            adelante = Vector3.forward;

        adelante.Normalize();

        // Centro de vuelo expresado en coordenadas locales del ancla
        Vector3 centro = anclaTransform.InverseTransformPoint(camaraAR.transform.position);

        for (int intento = 0; intento < 12; intento++)
        {
            // Elegir una dirección aleatoria DETRÁS de la cámara
            float desviacion = Random.Range(-dispersionDetras, dispersionDetras);

            Vector3 direccionMundo = Quaternion.AngleAxis(desviacion, Vector3.up) * -adelante;
            Vector3 direccionLocal = anclaTransform.InverseTransformDirection(direccionMundo);

            direccionLocal.y = 0f;
            direccionLocal.Normalize();

            float angulo = Mathf.Atan2(direccionLocal.z, direccionLocal.x) * Mathf.Rad2Deg;
            float rad = angulo * Mathf.Deg2Rad;

            Vector3 posicionLocal = centro + new Vector3(
                Mathf.Cos(rad) * radioVuelo,
                alturaRespectoCamara,
                Mathf.Sin(rad) * radioVuelo
            );

            Vector3 posicionMundo = anclaTransform.TransformPoint(posicionLocal);

            // Verificar que verdaderamente nace fuera de la vista de la cámara
            if (!PajaroOrbital.OcultoDetras(camaraAR, posicionMundo))
                continue;

            GameObject objeto = null;
            if (prefabPajaro != null)
            {
                objeto = Instantiate(prefabPajaro, anclaTransform);
            }
            else
            {
                objeto = CreateFallbackPajaro();
                objeto.transform.SetParent(anclaTransform, false);
            }

            PajaroOrbital vuelo = objeto.GetComponent<PajaroOrbital>();
            if (vuelo == null)
                vuelo = objeto.AddComponent<PajaroOrbital>();

            // Asegurar componente de interacción de choque / captura
            PajaroInteraccion interaccion = objeto.GetComponent<PajaroInteraccion>();
            if (interaccion == null)
                interaccion = objeto.AddComponent<PajaroInteraccion>();

            // Asegurar collider trigger calibrado en el pájaro para detectar colisión con el avión
            SphereCollider sc = objeto.GetComponent<SphereCollider>();
            if (sc == null)
            {
                BoxCollider bc = objeto.GetComponent<BoxCollider>();
                if (bc != null) Destroy(bc);
                sc = objeto.AddComponent<SphereCollider>();
            }
            sc.isTrigger = true;
            sc.radius = 0.22f;
            sc.center = Vector3.zero;

            vuelo.ConfigurarVariaciones(
                variacionVelocidad,
                frecuenciaCambioVelocidad,
                frecuenciaCambioAltura
            );

            int vueltas = Random.Range(vueltasMinimas, vueltasMaximas + 1);
            int sentido = Random.value < 0.5f ? -1 : 1;

            vuelo.Iniciar(
                camaraAR,
                centro,
                radioVuelo,
                velocidad,
                alturaRespectoCamara,
                oscilacionVertical,
                angulo,
                vueltas,
                sentido
            );

            activos.Add(vuelo);
            return;
        }
    }

    /// <summary>
    /// Elimina todos los pájaros actualmente en vuelo.
    /// </summary>
    [ContextMenu("Limpiar Todos los Pájaros")]
    public void LimpiarPajaros()
    {
        foreach (var p in activos)
        {
            if (p != null)
            {
                if (Application.isPlaying) Destroy(p.gameObject);
                else DestroyImmediate(p.gameObject);
            }
        }
        activos.Clear();
    }

    private void EnsureAnchor()
    {
        if (anclaTransform == null)
        {
            GameObject anclaSimulada = new GameObject("AnclaBandada_Simulada");
            anclaSimulada.transform.position = camaraAR != null ? camaraAR.transform.position : transform.position;
            anclaSimulada.transform.rotation = Quaternion.identity;
            anclaTransform = anclaSimulada.transform;
            preparado = true;
        }
    }

    private GameObject CreateFallbackPajaro()
    {
        GameObject pajaro = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pajaro.name = $"Pajaro_Prueba_{activos.Count + 1}";
        pajaro.transform.localScale = new Vector3(0.18f, 0.08f, 0.28f);

        // Crear una pequeña ala visual para identificarlo
        GameObject ala = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ala.name = "Alas";
        ala.transform.SetParent(pajaro.transform, false);
        ala.transform.localScale = new Vector3(2.2f, 0.15f, 0.5f);

        Collider col = pajaro.GetComponent<Collider>();
        if (col != null) { if (Application.isPlaying) Destroy(col); else DestroyImmediate(col); }
        Collider colAla = ala.GetComponent<Collider>();
        if (colAla != null) { if (Application.isPlaying) Destroy(colAla); else DestroyImmediate(colAla); }

        return pajaro;
    }

    private void OnDestroy()
    {
        if (ancla != null)
            Destroy(ancla.gameObject);
        else if (anclaTransform != null && anclaTransform.name.Contains("Simulada"))
            Destroy(anclaTransform.gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        if (!dibujarGizmos) return;

        Camera cam = camaraAR != null ? camaraAR : Camera.main;
        if (cam == null) return;

        Vector3 centro = cam.transform.position + new Vector3(0, alturaRespectoCamara, 0);

        // Círculo de la órbita de vuelo en cyan
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.7f);
        int segmentos = 36;
        float paso = (Mathf.PI * 2f) / segmentos;
        Vector3 anterior = centro + new Vector3(Mathf.Cos(0) * radioVuelo, 0, Mathf.Sin(0) * radioVuelo);

        for (int i = 1; i <= segmentos; i++)
        {
            float a = i * paso;
            Vector3 siguiente = centro + new Vector3(Mathf.Cos(a) * radioVuelo, 0, Mathf.Sin(a) * radioVuelo);
            Gizmos.DrawLine(anterior, siguiente);
            anterior = siguiente;
        }

        // Cono de aparición detrás de la cámara en verde
        Vector3 adelante = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
        if (adelante.sqrMagnitude < 0.001f) adelante = Vector3.forward;

        Vector3 atras = -adelante;
        Vector3 izqAtras = Quaternion.AngleAxis(-dispersionDetras, Vector3.up) * atras;
        Vector3 derAtras = Quaternion.AngleAxis(dispersionDetras, Vector3.up) * atras;

        Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.6f);
        Gizmos.DrawLine(centro, centro + (izqAtras * radioVuelo));
        Gizmos.DrawLine(centro, centro + (derAtras * radioVuelo));
    }
}
