using UnityEngine;
using UnityEngine.InputSystem;
using Gyroscope = UnityEngine.InputSystem.Gyroscope;

/// <summary>
/// Detecta un impulso corto hacia delante del móvil (no un agitado) y lanza
/// un avión de papel en la dirección de la ARCamera de Vuforia.
///
/// Lee acelerómetro y giroscopio con el Input System (el proyecto no usa
/// Input.acceleration clásico, que en Android se queda siempre a cero).
/// </summary>
public class LanzarAvionMovimiento : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("ARCamera de Vuforia. Si se deja vacío usa Camera.main.")]
    public Camera arCamera;
    [Tooltip("Prefab del avión. Preferible Avion_Estandar con PaperPlane en la raíz.")]
    public GameObject avionPrefab;
    [Tooltip("GameSpace / Image Target. Define el espacio de vuelo anclado a AR.")]
    public Transform arReference;

    [Header("Configuración")]
    [Tooltip("Aceleración mínima hacia delante (g) para considerar un lanzamiento.")]
    public float umbralLanzamiento = 1.3f;
    [Tooltip("Si el movimiento lateral supera esto, se trata como agitado y se ignora.")]
    public float umbralLateral = 1.8f;
    [Tooltip("Velocidad angular máxima (rad/s) para no lanzar al girar el móvil.")]
    public float umbralGiro = 6f;
    [Tooltip("Velocidad de vuelo que se aplica al PaperPlane al nacer.")]
    public float velocidadAvion = 5f;
    [Tooltip("Segundos mínimos entre un lanzamiento y el siguiente.")]
    public float tiempoEntreLanzamientos = 0.5f;
    [Tooltip("Distancia delante de la cámara a la que aparece el avión (metros).")]
    public float distanciaSpawn = 0.4f;
    public bool scaleWithReference = true;
    public bool usarGiroscopio = true;
    public bool mostrarDebug = true;
    [Tooltip("En el Editor, Espacio o clic lanza un avión (el acelerómetro no existe en PC).")]
    public bool permitirPruebaEnEditor = true;

    private const float GravedadMs2 = 9.80665f;

    private float ultimoLanzamiento = -10f;
    private Vector3 gravedadFiltrada;
    private bool gestoDisponible = true;
    private float siguienteLog;
    private float minimoZ;
    private float maximoZ;
    private float ultimoImpulso;
    private float ultimoLateral;
    private float ultimoGiro;
    private Vector3 ultimaAceleracionG;
    private string fuenteSensor = "buscando sensores...";
    private string ultimoMotivo = "Apunta a los globos e impulsa el móvil hacia delante.";

    private void OnEnable()
    {
        ActivarSensores();
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void Start()
    {
        if (arCamera == null)
            arCamera = Camera.main;

        EnsureGameSpaceReference();
        ActivarSensores();
        gravedadFiltrada = LeerAceleracionEnG();
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected ||
            change == InputDeviceChange.Enabled)
        {
            ActivarSensores();
        }
    }

    private static void EnableIfPresent(InputDevice device)
    {
        if (device != null && !device.enabled)
            InputSystem.EnableDevice(device);
    }

    private void ActivarSensores()
    {
        EnableIfPresent(Accelerometer.current);
        EnableIfPresent(LinearAccelerationSensor.current);
        EnableIfPresent(GravitySensor.current);
        EnableIfPresent(Gyroscope.current);
        EnableIfPresent(AttitudeSensor.current);

        if (Accelerometer.current != null)
            Accelerometer.current.samplingFrequency = 60f;
        if (LinearAccelerationSensor.current != null)
            LinearAccelerationSensor.current.samplingFrequency = 60f;
        if (Gyroscope.current != null)
            Gyroscope.current.samplingFrequency = 60f;
    }

    private void EnsureGameSpaceReference()
    {
        ARGameSpace gs = ARGameSpace.GetOrCreateInstance();
        if (gs == null) return;

        if (arReference == null || (gs.TargetMarker != null && arReference == gs.TargetMarker))
            arReference = gs.transform;
    }

    private void Update()
    {
        if (permitirPruebaEnEditor && Application.isEditor)
        {
            bool espacio = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            bool clic = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            if (espacio || clic)
            {
                IntentarLanzar();
                return;
            }
        }

        Vector3 aceleracionG = LeerAceleracionEnG();
        ultimaAceleracionG = aceleracionG;

        Vector3 movimientoG;
        if (LinearAccelerationSensor.current != null && LinearAccelerationSensor.current.enabled)
        {
            movimientoG = LinearAccelerationSensor.current.acceleration.ReadValue() / GravedadMs2;
            fuenteSensor = "LinearAcceleration";
        }
        else
        {
            gravedadFiltrada = Vector3.Lerp(
                gravedadFiltrada,
                aceleracionG,
                Mathf.Clamp01(Time.deltaTime * 2f)
            );
            movimientoG = aceleracionG - gravedadFiltrada;
            fuenteSensor = Accelerometer.current != null && Accelerometer.current.enabled ? "Accelerometer" : "SIN SENSOR";
        }

        // En el móvil, Z positivo sale de la pantalla hacia el usuario.
        // Empujar el teléfono hacia la escena (lanzar el avión) es Z negativo.
        float impulso = -movimientoG.z;
        float lateral = new Vector2(movimientoG.x, movimientoG.y).magnitude;
        float giro = LeerGiro();

        ultimoImpulso = impulso;
        ultimoLateral = lateral;
        ultimoGiro = giro;

        if (mostrarDebug && !Application.isEditor)
        {
            minimoZ = Mathf.Min(minimoZ, movimientoG.z);
            maximoZ = Mathf.Max(maximoZ, movimientoG.z);

            if (Time.unscaledTime >= siguienteLog)
            {
                Debug.Log($"[LanzarAvionMovimiento] Sensor={fuenteSensor} Accel={aceleracionG:F2} " +
                          $"Z min/max={minimoZ:F2}/{maximoZ:F2} Umbral={umbralLanzamiento:F2} " +
                          $"Lateral={lateral:F2} Giro={giro:F2} Estado={ultimoMotivo}", this);
                siguienteLog = Time.unscaledTime + 1f;
                minimoZ = maximoZ = 0f;
            }
        }

        if (impulso <= umbralLanzamiento * 0.5f)
            gestoDisponible = true;

        if (!gestoDisponible || impulso <= umbralLanzamiento)
            return;

        if (lateral > umbralLateral && lateral > impulso)
        {
            ultimoMotivo = "Agitado detectado (movimiento lateral). Impulsa hacia delante.";
            return;
        }

        if (usarGiroscopio && giro > umbralGiro)
        {
            ultimoMotivo = "Giro detectado. Mantén el móvil estable y empuja hacia delante.";
            return;
        }

        gestoDisponible = false;
        IntentarLanzar();
    }

    private Vector3 LeerAceleracionEnG()
    {
        if (Accelerometer.current != null)
        {
            if (!Accelerometer.current.enabled)
                InputSystem.EnableDevice(Accelerometer.current);
            return Accelerometer.current.acceleration.ReadValue() / GravedadMs2;
        }

        return Vector3.zero;
    }

    private float LeerGiro()
    {
        if (!usarGiroscopio)
            return 0f;

        if (Gyroscope.current == null)
            return 0f;

        if (!Gyroscope.current.enabled)
            InputSystem.EnableDevice(Gyroscope.current);

        return Gyroscope.current.angularVelocity.ReadValue().magnitude;
    }

    private void IntentarLanzar()
    {
        if (Time.time - ultimoLanzamiento < tiempoEntreLanzamientos)
            return;

        if (LanzarAvion())
        {
            ultimoLanzamiento = Time.time;
            ultimoMotivo = "¡Avión lanzado!";
            Debug.Log("[LanzarAvionMovimiento] ¡Avión lanzado!", this);
        }
    }

    private bool LanzarAvion()
    {
        EnsureGameSpaceReference();

        if (arCamera == null)
            arCamera = Camera.main;

        if (avionPrefab == null && PlaneSkinManager.Instance != null)
        {
            PaperPlane activo = PlaneSkinManager.Instance.GetActivePrefab();
            if (activo != null) avionPrefab = activo.gameObject;
        }

        if (arCamera == null || avionPrefab == null)
        {
            Debug.LogError("[LanzarAvionMovimiento] Falta ARCamera o Avion Prefab.", this);
            ultimoMotivo = "Falta asignar ARCamera o el prefab del avión.";
            return false;
        }

        float escala = (arReference != null && scaleWithReference)
            ? arReference.lossyScale.x
            : 1f;

        Vector3 direccion = arCamera.transform.forward;
        Vector3 posicion = arCamera.transform.position + direccion * (distanciaSpawn * Mathf.Max(escala, 0.01f));

        GameObject avion = Instantiate(avionPrefab, posicion, Quaternion.LookRotation(direccion));
        avion.transform.localScale *= escala;

        PaperPlane plane = avion.GetComponent<PaperPlane>();
        if (plane != null)
        {
            plane.forwardSpeed = velocidadAvion;
            plane.Initialize(direccion, arReference);
            return true;
        }

        Rigidbody rb = avion.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.linearVelocity = direccion * velocidadAvion;
            Destroy(avion, 12f);
            return true;
        }

        Debug.LogError("[LanzarAvionMovimiento] El prefab no tiene PaperPlane ni Rigidbody.", avionPrefab);
        Destroy(avion);
        return false;
    }

    private void OnGUI()
    {
        if (!mostrarDebug) return;

        const int pad = 16;
        GUI.Box(new Rect(pad, pad, 460, 188), "");
        GUILayout.BeginArea(new Rect(pad + 10, pad + 8, 440, 172));
        GUILayout.Label("PRUEBA · LANZAMIENTO POR IMPULSO");
        GUILayout.Label(ultimoMotivo);
        if (!Application.isEditor)
        {
            GUILayout.Label($"Sensor: {fuenteSensor}");
            GUILayout.Label($"Accel g: {ultimaAceleracionG.x:0.00}, {ultimaAceleracionG.y:0.00}, {ultimaAceleracionG.z:0.00}");
            GUILayout.Label($"Impulso Z: {ultimoImpulso:0.00}  (umbral {umbralLanzamiento:0.00})");
            GUILayout.Label($"Lateral: {ultimoLateral:0.00}   Giro: {ultimoGiro:0.00}");
        }
        else
        {
            GUILayout.Label("Editor: pulsa Espacio o clic para lanzar.");
        }
        GUILayout.EndArea();
    }
}

