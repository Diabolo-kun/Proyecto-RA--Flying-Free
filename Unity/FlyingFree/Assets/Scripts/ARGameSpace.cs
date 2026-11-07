using UnityEngine;
using Vuforia;

/// <summary>
/// Nodo de referencia espacial maestro (GameSpace) para la experiencia AR.
/// Se alinea con la marca principal (ImageTarget de la pared de globos) de Vuforia,
/// asegurando que:
/// 1. Su origen espacial coincide con la posición de la marca en la pared.
/// 2. Su eje 'Up' (+Y) está siempre alineado con la gravedad del mundo real (Vector3.up),
///    evitando que la rotación de la pared invierta el cabeceo o la guiñada del avión.
/// 3. Su eje 'Forward' (+Z) apunta directamente hacia la pared (hacia los globos),
///    de forma que un avión lanzado hacia adelante vuela en la dirección natural +Z.
/// 4. Sirve como origen coordinado compartido tanto para la generación de globos,
///    como para el vuelo del avión de papel y el pasillo de órbita de los pájaros.
/// </summary>
public class ARGameSpace : MonoBehaviour
{
    public static ARGameSpace Instance { get; private set; }

    [Header("--- REFERENCIAS VUFORIA ---")]
    [Tooltip("Transform del Image Target principal (ej. ImageTarget-PlanoGlobos)")]
    [SerializeField] private Transform targetMarker;

    [Tooltip("ObserverBehaviour de Vuforia asociado a la marca")]
    [SerializeField] private ObserverBehaviour targetObserver;

    [Tooltip("Cámara AR del jugador (si está vacía toma Camera.main)")]
    [SerializeField] private Camera camaraAR;

    [Header("--- ALINEACIÓN Y ORIENTACIÓN ---")]
    [Tooltip("Si está activo, fija el eje 'Up' en Vector3.up (gravedad real), independientemente de la inclinación del target")]
    [SerializeField] private bool lockUpToGravity = true;

    [Tooltip("Suavizado de posición y rotación ante temblores leves de tracking (0 = instantáneo)")]
    [Range(0f, 25f)]
    [SerializeField] private float trackingSmoothing = 0f;

    [Header("--- SIMULACIÓN / TEST SIN VUFORIA ---")]
    [Tooltip("Si la marca no se ha detectado aún o estás en el Editor, sitúa el GameSpace frente a la cámara")]
    [SerializeField] private bool fallbackInFrontOfCamera = true;

    [Tooltip("Distancia en metros frente a la cámara para la simulación")]
    [SerializeField] private float fallbackDistance = 2.2f;

    [Header("--- DEBUG VISUAL ---")]
    [Tooltip("Dibuja los ejes de coordenadas y el pasillo de vuelo en la Scene View")]
    [SerializeField] private bool drawGizmos = true;

    // Estado interno
    private bool isCurrentlyTracked = false;
    private bool hasEverBeenTracked = false;
    private bool hasInitializedFallback = false;
    private float gameplayScale = 1;
    public float GameplayScale => gameplayScale;
    public void SetGameplayScale(float value)
    {
        gameplayScale = Mathf.Clamp(value, .002f, 1);
        transform.localScale = Vector3.one * gameplayScale;
    }
    private Vector3 lastTrackedPosition = Vector3.zero;
    private Quaternion lastTrackedRotation = Quaternion.identity;

    // Propiedades públicas
    public Transform TargetMarker => targetMarker;
    public bool IsTracked => isCurrentlyTracked;
    public bool HasEverBeenTracked => hasEverBeenTracked;
    public Camera CameraAR => camaraAR;

    /// <summary>
    /// Obtiene la instancia activa o crea una automáticamente en tiempo de ejecución si no existe.
    /// </summary>
    public static ARGameSpace GetOrCreateInstance()
    {
        if (Instance != null) return Instance;

        ARGameSpace existing = FindObjectOfType<ARGameSpace>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        GameObject go = new GameObject("AR_GameSpace");
        Instance = go.AddComponent<ARGameSpace>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        AutoResolveReferences();
    }

    private void OnEnable()
    {
        if (targetObserver != null)
        {
            targetObserver.OnTargetStatusChanged += HandleTargetStatusChanged;
        }
    }

    private void OnDisable()
    {
        if (targetObserver != null)
        {
            targetObserver.OnTargetStatusChanged -= HandleTargetStatusChanged;
        }
    }

    private void Start()
    {
        AutoResolveReferences();

        // En caso de que se use TargetPlaneSpawner con testModeWithoutVuforia:
        TargetPlaneSpawner spawner = FindObjectOfType<TargetPlaneSpawner>();
        if (spawner != null && spawner.testModeWithoutVuforia)
        {
            isCurrentlyTracked = true;
            hasEverBeenTracked = true;
        }

        UpdateTransformPose(true);
    }

    private void AutoResolveReferences()
    {
        if (camaraAR == null)
            camaraAR = Camera.main;

        if (targetMarker == null)
        {
            // 1. Buscar ImageTarget con nombre PlanoGlobos
            GameObject targetObj = GameObject.Find("ImageTarget-PlanoGlobos");
            if (targetObj != null)
            {
                targetMarker = targetObj.transform;
            }
            else
            {
                // 2. Buscar por TargetPlaneSpawner
                TargetPlaneSpawner spawner = FindObjectOfType<TargetPlaneSpawner>();
                if (spawner != null)
                {
                    targetMarker = spawner.transform;
                }
            }
        }

        if (targetObserver == null && targetMarker != null)
        {
            targetObserver = targetMarker.GetComponent<ObserverBehaviour>();
        }
    }

    private void HandleTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus targetStatus)
    {
        bool tracked = targetStatus.Status == Status.TRACKED ||
                       targetStatus.Status == Status.EXTENDED_TRACKED ||
                       targetStatus.Status == Status.LIMITED;

        isCurrentlyTracked = tracked;
        if (tracked)
        {
            hasEverBeenTracked = true;
        }
    }

    private void LateUpdate()
    {
        UpdateTransformPose(false);
    }

    /// <summary>Plano de la imagen: Z asciende por ella, Y es su normal hacia el jugador.</summary>
    public static Quaternion MarkerPlaneFrame(Transform marker, Camera camera)
    {
        Vector3 normal = marker.up;
        if (camera != null && Vector3.Dot(normal, camera.transform.position - marker.position) < 0) normal = -normal;
        // Priorizar la vertical de la gravedad real (Vector3.up del mundo) proyectada en el plano del marcador.
        // Si el marcador está en pared vertical, la proyección da la vertical exacta hacia el techo, inmune
        // a cómo se mueva o ladee el móvil. Si el marcador está horizontal (mesa), usar fallback.
        Vector3 gravityUp = Vector3.ProjectOnPlane(Vector3.up, normal);
        Vector3 imageUp = gravityUp.sqrMagnitude > .01f ? gravityUp.normalized
            : (camera != null ? Vector3.ProjectOnPlane(camera.transform.up, normal) : marker.forward);
        if (imageUp.sqrMagnitude < .001f) imageUp = marker.forward;
        return Quaternion.LookRotation(imageUp.normalized, normal);
    }

    /// <summary>Y sube por la imagen, Z vuela desde el jugador hacia su plano.</summary>
    public static Quaternion UprightMarkerRotation(Transform marker, Camera camera)
    {
        Quaternion image = MarkerPlaneFrame(marker, camera);
        return Quaternion.LookRotation(-(image * Vector3.up), image * Vector3.forward);
    }

    /// <summary>
    /// Actualiza la posición y orientación de GameSpace asegurando el eje vertical hacia el cielo
    /// y el eje hacia adelante (+Z) apuntando hacia la pared.
    /// </summary>
    public void UpdateTransformPose(bool instant)
    {
        if (camaraAR == null) camaraAR = Camera.main;

        Vector3 desiredPosition;
        Quaternion desiredRotation;

        if (targetMarker != null && isCurrentlyTracked)
        {
            desiredPosition = targetMarker.position;

            if (lockUpToGravity)
            {
                desiredRotation = UprightMarkerRotation(targetMarker, camaraAR);
            }
            else
            {
                desiredRotation = targetMarker.rotation;
            }

            lastTrackedPosition = desiredPosition;
            lastTrackedRotation = desiredRotation;
        }
        else if (hasEverBeenTracked)
        {
            // Conservar la última pose conocida mientras el tracking vuelve
            desiredPosition = lastTrackedPosition;
            desiredRotation = lastTrackedRotation;
        }
        else if (fallbackInFrontOfCamera && camaraAR != null)
        {
            // Modo simulación: posicionar a distancia frente a la cámara UNA SOLA VEZ
            // para que quede anclado en el espacio del mundo y NO rote con la cámara.
            if (!hasInitializedFallback)
            {
                Vector3 camFwd = Vector3.ProjectOnPlane(camaraAR.transform.forward, Vector3.up).normalized;
                if (camFwd.sqrMagnitude < 0.001f) camFwd = Vector3.forward;

                desiredPosition = camaraAR.transform.position + (camFwd * fallbackDistance);
                desiredRotation = Quaternion.LookRotation(camFwd, Vector3.up);

                lastTrackedPosition = desiredPosition;
                lastTrackedRotation = desiredRotation;
                hasInitializedFallback = true;
            }
            else
            {
                desiredPosition = lastTrackedPosition;
                desiredRotation = lastTrackedRotation;
            }
        }
        else
        {
            return;
        }

        if (instant || trackingSmoothing <= 0f || !Application.isPlaying)
        {
            transform.position = desiredPosition;
            transform.rotation = desiredRotation;
        }
        else
        {
            float t = 1f - Mathf.Exp(-trackingSmoothing * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, t);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, t);
        }

        // La escala de GameSpace es SIEMPRE estrictamente 1 para evitar distorsiones
        transform.localScale = Vector3.one * gameplayScale;
    }

    /// <summary>
    /// Calcula el punto del pasillo de juego frente a la pared, expresado en coordenadas locales de GameSpace.
    /// normalizedDistance: 0.0 = en la marca (pared), 1.0 = distancia completa del pasillo (hacia el jugador).
    /// </summary>
    public Vector3 GetCorridorPointLocal(float normalizedDistance = 0.5f)
    {
        // La pared está en Z = 0 local. El jugador/habitación está en Z < 0 local.
        // Un punto a medio camino en el pasillo es Z negativo fijo (ej. -1.2m frente a la pared).
        float distZ = -Mathf.Max(0.6f, fallbackDistance * normalizedDistance);
        return new Vector3(0f, 0f, distZ);
    }

    /// <summary>
    /// Retorna la posición de la cámara en el espacio local de GameSpace.
    /// </summary>
    public Vector3 GetCameraLocalPosition()
    {
        if (camaraAR == null) camaraAR = Camera.main;
        if (camaraAR == null) return new Vector3(0f, 0f, -2f);
        return transform.InverseTransformPoint(camaraAR.transform.position);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        // Origen en la marca
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, 0.12f);

        // Ejes coordenados del GameSpace
        Gizmos.color = Color.red;    // X: Derecha de la pared
        Gizmos.DrawRay(transform.position, transform.right * 0.4f);

        Gizmos.color = Color.green;  // Y: Techo (gravedad real)
        Gizmos.DrawRay(transform.position, transform.up * 0.4f);

        Gizmos.color = Color.blue;   // Z: Hacia la pared (dirección de impacto)
        Gizmos.DrawRay(transform.position, transform.forward * 0.4f);

        // Pasillo de juego hacia la cámara
        Camera cam = camaraAR != null ? camaraAR : Camera.main;
        if (cam != null)
        {
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.6f);
            Gizmos.DrawLine(transform.position, cam.transform.position);

            Vector3 mitadMundo = transform.TransformPoint(GetCorridorPointLocal(0.5f));
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.8f);
            Gizmos.DrawWireSphere(mitadMundo, 0.18f);
        }
    }
}
