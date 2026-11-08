using UnityEngine;
using UnityEngine.InputSystem;
using Gyroscope = UnityEngine.InputSystem.Gyroscope;

/// <summary>
/// Lanza el avión con un movimiento brusco del móvil.
/// No usa toques. Se integra con GameManagerRA y el avión elegido.
/// </summary>
public class LanzarAvionMovimiento : MonoBehaviour
{
    [Header("Referencias")]
    public Camera arCamera;
    public GameObject avionPrefab;
    public GameObject[] avionesElegibles;
    public Transform arReference;

    [Header("Detección del impulso")]
    [Tooltip("Aceleración mínima (g). Más bajo = más fácil.")]
    public float umbralLanzamiento = 0.12f;
    public float tiempoEntreLanzamientos = 0.8f;

    [Header("Avión")]
    public float velocidadAvion = 5f;
    public float distanciaSpawn = 0.35f;
    public float tamañoAvion = 3f;
    public bool scaleWithReference = true;

    [Header("Ayuda")]
    public bool mostrarAyuda = false;
    public bool permitirPruebaEnEditor = true;

    const float GravedadMs2 = 9.80665f;
    const string ClaveAvion = "FlyingFree.AvionSeleccionado";
    const string AvionDefecto = "Avion_Cuaderno";

    float ultimoLanzamiento = -10f;
    Vector3 muestraAnterior;
    bool hayMuestraAnterior;
    bool gestoDisponible = true;
    float ultimoImpulso;
    float picoImpulso;
    string fuenteSensor = "buscando sensores...";
    string ultimoMotivo = "Con la marca a la vista, da un golpe hacia delante con el móvil.";
    PaperPlane avionActual;
    System.Action<bool> alTerminar;

    void OnEnable()
    {
        ActivarSensores();
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
        SoltarAvionActual();
    }

    void Start()
    {
        if (arCamera == null)
            arCamera = Camera.main;

        EnsureGameSpaceReference();
        ActivarSensores();
        InvokeRepeating(nameof(ActivarSensores), 1f, 2f);
    }

    void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected ||
            change == InputDeviceChange.Enabled)
            ActivarSensores();
    }

    static void EnableIfPresent(InputDevice device)
    {
        if (device != null && !device.enabled)
            InputSystem.EnableDevice(device);
    }

    void ActivarSensores()
    {
        EnableIfPresent(Accelerometer.current);
        EnableIfPresent(LinearAccelerationSensor.current);
        EnableIfPresent(GravitySensor.current);
        EnableIfPresent(Gyroscope.current);
        EnableIfPresent(AttitudeSensor.current);

        try
        {
            if (Accelerometer.current != null)
                Accelerometer.current.samplingFrequency = 50f;
            if (LinearAccelerationSensor.current != null)
                LinearAccelerationSensor.current.samplingFrequency = 50f;
        }
        catch
        {
            // Algunos Samsung no dejan subir la frecuencia.
        }
    }

    void EnsureGameSpaceReference()
    {
        ARGameSpace gs = ARGameSpace.GetOrCreateInstance();
        if (gs == null) return;

        if (arReference == null || (gs.TargetMarker != null && arReference == gs.TargetMarker))
            arReference = gs.transform;
    }

    void Update()
    {
        if (avionActual != null)
        {
            if (!avionActual.IsFlying)
                SoltarAvionActual();
            ultimoMotivo = "Avión en vuelo...";
            return;
        }

        if (permitirPruebaEnEditor && Application.isEditor)
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                IntentarLanzar();
                return;
            }
        }

        if (!LeerAceleracion(out Vector3 muestra))
        {
            ultimoMotivo = "El móvil no da acelerómetro. Mira Ajustes > Apps > FlyingFree.";
            return;
        }

        float impulso = 0f;
        if (hayMuestraAnterior)
            impulso = (muestra - muestraAnterior).magnitude;
        muestraAnterior = muestra;
        hayMuestraAnterior = true;

        // También mira aceleración lineal absoluta, por si el delta sale flojo.
        if (LinearAccelerationSensor.current != null && LinearAccelerationSensor.current.enabled)
        {
            float lineal = LinearAccelerationSensor.current.acceleration.ReadValue().magnitude / GravedadMs2;
            impulso = Mathf.Max(impulso, lineal);
        }

        ultimoImpulso = impulso;
        if (impulso > picoImpulso)
            picoImpulso = impulso;

        if (impulso <= umbralLanzamiento * 0.35f)
            gestoDisponible = true;

        ActualizarMotivoEspera();

        if (!gestoDisponible || impulso < umbralLanzamiento)
            return;

        gestoDisponible = false;
        IntentarLanzar();
    }

    void ActualizarMotivoEspera()
    {
        var game = GameManagerRA.Instance;
        if (game != null && !game.PuedeLanzar)
        {
            if (game.EsperandoSeguimiento)
                ultimoMotivo = "Enfoca la marca de los globos y manténla estable.";
            else if (game.AvionesEnVuelo > 0)
                ultimoMotivo = "Espera a que termine el avión en vuelo.";
            else if (!game.PartidaEnCurso)
                ultimoMotivo = "La partida no está en curso.";
            else
                ultimoMotivo = "Ahora no se puede lanzar.";
            return;
        }

        if (fuenteSensor.Contains("SIN"))
            ultimoMotivo = "Sin sensor de movimiento.";
        else
            ultimoMotivo = "Da un golpe seco hacia delante con el móvil.";
    }

    bool LeerAceleracion(out Vector3 muestraEnG)
    {
        if (Accelerometer.current != null)
        {
            if (!Accelerometer.current.enabled)
                InputSystem.EnableDevice(Accelerometer.current);

            muestraEnG = Accelerometer.current.acceleration.ReadValue() / GravedadMs2;
            fuenteSensor = "Accelerometer";
            return muestraEnG.sqrMagnitude > 0.0001f;
        }

        if (LinearAccelerationSensor.current != null)
        {
            if (!LinearAccelerationSensor.current.enabled)
                InputSystem.EnableDevice(LinearAccelerationSensor.current);

            muestraEnG = LinearAccelerationSensor.current.acceleration.ReadValue() / GravedadMs2;
            fuenteSensor = "LinearAcceleration";
            return true;
        }

        muestraEnG = Vector3.zero;
        fuenteSensor = "SIN SENSOR";
        return false;
    }

    void IntentarLanzar()
    {
        if (Time.time - ultimoLanzamiento < tiempoEntreLanzamientos)
            return;

        if (LanzarAvion())
        {
            ultimoLanzamiento = Time.time;
            ultimoMotivo = "¡Avión lanzado!";
            picoImpulso = 0f;
            Debug.Log("[LanzarAvionMovimiento] ¡Avión lanzado!", this);
        }
    }

    bool LanzarAvion()
    {
        EnsureGameSpaceReference();

        if (arCamera == null)
            arCamera = Camera.main;

        var game = GameManagerRA.Instance;
        if (game != null && !game.PuedeLanzar)
        {
            ActualizarMotivoEspera();
            return false;
        }

        GameObject origen = AvionElegido();
        if (origen == null)
            origen = avionPrefab;

        if (origen == null && PlaneSkinManager.Instance != null)
        {
            PaperPlane activo = PlaneSkinManager.Instance.GetActivePrefab();
            if (activo != null) origen = activo.gameObject;
        }

        if (arCamera == null || origen == null)
        {
            ultimoMotivo = "Falta la cámara AR o el prefab del avión.";
            return false;
        }

        float escalaRef = (arReference != null && scaleWithReference)
            ? Mathf.Max(arReference.lossyScale.x, 0.01f)
            : 1f;
        float escala = escalaRef * Mathf.Max(0.1f, tamañoAvion);

        Vector3 direccion = arCamera.transform.forward;
        Vector3 posicion = arCamera.transform.position + direccion * (distanciaSpawn * escalaRef);

        GameObject avion = Instantiate(origen, posicion, Quaternion.LookRotation(direccion));
        avion.transform.localScale = Vector3.Scale(avion.transform.localScale, Vector3.one * escala);

        PaperPlane plane = TouchFlightController.PrepararAvion(avion);
        if (plane == null)
        {
            Destroy(avion);
            ultimoMotivo = "El prefab del avión no es válido.";
            return false;
        }

        plane.forwardSpeed = velocidadAvion;
        plane.Initialize(direccion, arReference);

        if (game != null && !game.TryRegisterLaunch(plane))
        {
            plane.CancelForRoundEnd();
            ultimoMotivo = "La partida no aceptó el lanzamiento.";
            return false;
        }

        avionActual = plane;
        alTerminar = _ =>
        {
            if (avionActual == plane)
                SoltarAvionActual();
        };
        plane.OnFinished += alTerminar;
        return true;
    }

    GameObject AvionElegido()
    {
        string nombre = PlayerPrefs.GetString(ClaveAvion, "");
        if (string.IsNullOrEmpty(nombre))
            nombre = AvionDefecto;

        GameObject elegido = BuscarAvion(nombre);
        return elegido != null ? elegido : BuscarAvion(AvionDefecto);
    }

    GameObject BuscarAvion(string nombre)
    {
        if (avionesElegibles == null || string.IsNullOrEmpty(nombre))
            return null;

        for (int i = 0; i < avionesElegibles.Length; i++)
        {
            if (avionesElegibles[i] != null && avionesElegibles[i].name == nombre)
                return avionesElegibles[i];
        }

        return null;
    }

    void SoltarAvionActual()
    {
        if (avionActual != null && alTerminar != null)
            avionActual.OnFinished -= alTerminar;
        avionActual = null;
        alTerminar = null;
    }

}
