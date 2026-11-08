using UnityEngine;

public class MotionFlightController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera cam;
    [SerializeField] private GameObject planePrefab;
    [SerializeField] private Transform arReference;

    [Header("Lanzamiento")]
    [SerializeField] private float umbralImpulso = 0.8f;
    [SerializeField] private float tiempoEntreLanzamientos = 0.5f;
    [SerializeField] private float distanciaSalida = 0.3f;
    [SerializeField] private bool scaleWithReference = true;

    private Vector3 gravedadFiltrada;
    private float ultimoLanzamiento = float.NegativeInfinity;
    private bool gestoDisponible = true;
#if UNITY_ANDROID && !UNITY_EDITOR
    private bool gravedadInicializada;
    private float siguienteDiagnostico;
    private float minimoZ;
    private float maximoZ;
#endif

    private void Start()
    {
        Debug.Log("[MotionFlight] Inicio v3 | escena=" + gameObject.scene.name, this);
        if (cam == null)
            cam = Camera.main;

        if (cam == null)
        {
            Debug.LogError("[MotionFlight] No se ha asignado la ARCamera.", this);
            enabled = false;
            return;
        }

        ARGameSpace gs = ARGameSpace.GetOrCreateInstance();

        if (gs != null)
            arReference = gs.transform;

#if UNITY_ANDROID && !UNITY_EDITOR
        gravedadInicializada = false;
#else
        gravedadFiltrada = Input.acceleration;
#endif
    }

    private void Update()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        var sensor = UnityEngine.InputSystem.Accelerometer.current;
        if (sensor == null)
        {
            if (Time.unscaledTime >= siguienteDiagnostico)
            {
                Debug.LogWarning("[MotionFlight] Input System: acelerómetro no encontrado.", this);
                siguienteDiagnostico = Time.unscaledTime + 1f;
            }
            return;
        }

        if (!sensor.enabled)
            UnityEngine.InputSystem.InputSystem.EnableDevice(sensor);

        // El control de Android ya entrega la aceleración en g.
        Vector3 aceleracion = sensor.acceleration.ReadValue();
        if (!gravedadInicializada && aceleracion.sqrMagnitude > 0.01f)
        {
            gravedadFiltrada = aceleracion;
            gravedadInicializada = true;
        }
#else
        Vector3 aceleracion = Input.acceleration;
#endif

        // Eliminar aproximadamente la gravedad.
        float suavizado = 1f - Mathf.Exp(-3f * Time.deltaTime);
        gravedadFiltrada = Vector3.Lerp(gravedadFiltrada, aceleracion, suavizado);

        Vector3 movimiento = aceleracion - gravedadFiltrada;
        float impulso = -movimiento.z;

#if UNITY_ANDROID && !UNITY_EDITOR
        minimoZ = Mathf.Min(minimoZ, movimiento.z);
        maximoZ = Mathf.Max(maximoZ, movimiento.z);

        if (Time.unscaledTime >= siguienteDiagnostico)
        {
            Debug.Log($"[MotionFlight] Input System Accel={aceleracion:F3} Z min/max={minimoZ:F3}/{maximoZ:F3} Umbral={umbralImpulso:F2}", this);
            siguienteDiagnostico = Time.unscaledTime + 1f;
            minimoZ = maximoZ = 0f;
        }
#endif

        // El impulso debe bajar antes de aceptar otro gesto.
        if (impulso <= umbralImpulso * 0.5f)
            gestoDisponible = true;

        if (!gestoDisponible || impulso < umbralImpulso)
            return;

        gestoDisponible = false;

        if (Time.time - ultimoLanzamiento >= tiempoEntreLanzamientos)
            LanzarAvion();
    }

    private void LanzarAvion()
    {
        if (planePrefab == null)
        {
            Debug.LogError("[MotionFlight] Falta asignar el prefab del avión.", this);
            return;
        }

        float escala = 1f;

        if (arReference != null && scaleWithReference)
            escala = arReference.lossyScale.x;

        Vector3 direccion = cam.transform.forward;
        Vector3 posicion = cam.transform.position +
                           direccion * distanciaSalida * escala;

        GameObject avion = Instantiate(
            planePrefab,
            posicion,
            Quaternion.LookRotation(direccion)
        );

        PaperPlane plane = avion.GetComponent<PaperPlane>();

        if (plane == null)
        {
            Debug.LogError("[MotionFlight] El prefab necesita PaperPlane en el objeto raíz.", avion);
            Destroy(avion);
            return;
        }

        avion.transform.localScale *= escala;

        plane.Initialize(direccion, arReference);
        plane.SetSteering(Vector2.zero);
        plane.OnFinished += AlTerminarVuelo;

        ultimoLanzamiento = Time.time;
        Debug.Log("[MotionFlight] ¡Avión lanzado con movimiento del móvil!", this);
    }

    private void AlTerminarVuelo(bool objetivoAlcanzado)
    {
        Debug.Log(objetivoAlcanzado
            ? "¡Objetivo alcanzado!"
            : "Vuelo terminado", this);
    }
}


