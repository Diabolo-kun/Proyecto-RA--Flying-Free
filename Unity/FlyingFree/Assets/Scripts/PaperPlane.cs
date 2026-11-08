using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Avión de papel para AR.
///
/// El vuelo se calcula COMPLETAMENTE en el espacio local de un Transform de referencia
/// (GameSpace / Image Target): posición, giro y cabeceo. Cada frame se convierte a mundo.
/// Así el avión queda anclado a la escena AR: si el tracking se mueve, tiembla o el
/// dispositivo se desplaza, el avión se mueve con el target en lugar de quedarse
/// "pegado" al mundo de la cámara.
///
/// Se mueve en LateUpdate (con Time.deltaTime), después de que Vuforia actualice la
/// pose del target, para que no haya desfase entre la cámara y el avión.
///
/// Termina al tocar un PlaneTarget (éxito) o cualquier otro collider sólido (choque).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[DefaultExecutionOrder(100)]
public class PaperPlane : MonoBehaviour
{
    [Header("Vuelo (unidades locales de la referencia por segundo)")]
    [SerializeField] public float forwardSpeed = 6f;
    [SerializeField] private float turnSpeed = 80f;        // grados por segundo
    [SerializeField] private float inputSmoothing = 8f;    // más alto = responde más rápido
    [SerializeField] private float maxPitch = 60f;         // límite de cabeceo (grados)
    [SerializeField] private float maxLifetime = 20f;      // segundos antes de autodestruirse

    [Header("Visual (opcional)")]
    [Tooltip("Hijo con el modelo del avión. Se inclina (roll) al girar.")]
    [SerializeField] private Transform visual;
    [SerializeField] private float maxBankAngle = 35f;

    [Header("Final y Efectos")]
    [SerializeField] private float destroyDelay = 1.5f;
    [Tooltip("Prefab de explosión de choque (ej. Explosion_Choque)")]
    [SerializeField] public GameObject explosionPrefab;

    public bool IsCaptured { get; private set; }
    static readonly List<PaperPlane> activePlanes = new List<PaperPlane>();
    public static IReadOnlyList<PaperPlane> ActivePlanes => activePlanes;
    public bool IsFlying => initialized && !finished && !IsCaptured && isActiveAndEnabled;
    /// <summary>Entrada común para triggers, barridos y robo; la pausa AR bloquea todas las interacciones.</summary>
    public bool CanInteract => IsFlying && (GameManagerRA.Instance == null ||
        (GameManagerRA.Instance.PartidaEnCurso && !GameManagerRA.Instance.EsperandoSeguimiento && !GameManagerRA.Instance.EstaPausada));
    public Vector3 CaptureGripOffset { get; private set; }
    public Vector3 PreviousWorldPosition { get; private set; }
    public Vector3 WorldVelocity => reference != null
        ? reference.TransformVector(Quaternion.Euler(pitch, yaw, 0) * Vector3.forward * forwardSpeed)
        : Quaternion.Euler(pitch, yaw, 0) * Vector3.forward * forwardSpeed;
    bool initialized;
    UnityEngine.Object reservedByBird;
    void OnEnable() { if (!activePlanes.Contains(this)) activePlanes.Add(this); }
    void OnDisable()
    {
        activePlanes.Remove(this); reservedByBird = null;
        // Destrucción externa o desactivación: también debe resolverse el lanzamiento.
        if (initialized && !finished)
        { finished = true; OnFinished?.Invoke(false); }
    }
    public bool IsReservedByOtherBird(UnityEngine.Object bird) => reservedByBird != null && reservedByBird != bird;
    public bool TryReserveForBird(UnityEngine.Object bird)
    {
        if (!IsFlying || bird == null || IsReservedByOtherBird(bird)) return false;
        reservedByBird = bird; return true;
    }
    public void ReleaseBirdReservation(UnityEngine.Object bird) { if (reservedByBird == bird) reservedByBird = null; }

    /// <summary>Se dispara una vez: true = tocó el objetivo, false = chocó o se acabó el tiempo.</summary>
    public event Action<bool> OnFinished;

    private PlaneCollisionDetector contacts;
    private Rigidbody rb;
    private Transform reference;
    private Vector3 localPosition;      // posición en el espacio de la referencia
    private Vector2 steerInput;
    private Vector2 smoothedInput;
    private float yaw;
    private float pitch;
    private float lifeTimer;
    private bool finished;

    private Quaternion ReferenceRotation =>
        reference != null ? reference.rotation : Quaternion.identity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        contacts = new PlaneCollisionDetector(this);
        var originalMotor = GetComponent<FlyingFree.Sprint2.PaperPlaneMotor>();
        if (originalMotor != null && originalMotor.definition != null) CaptureGripOffset = originalMotor.definition.gripOffset;
        rb.isKinematic = true;
        rb.useGravity = false;
        // El movimiento se hace por Transform en LateUpdate, no por física
        rb.interpolation = RigidbodyInterpolation.None;

        // Asegurar collider calibrado en la punta del avión
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            SphereCollider sc = gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.08f;
            sc.center = new Vector3(0f, 0f, 0.12f);
        }

#if UNITY_EDITOR
        if (explosionPrefab == null)
        {
            explosionPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Explosion_Choque.prefab");
        }
#endif

        // Los colliders del avión son trigger para detectar contacto con paredes
        // y objetivos sin que la física empuje al avión.
        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.isTrigger = true;
    }

    /// <summary>
    /// Llamar justo después de instanciar.
    /// worldDirection: dirección inicial en mundo.
    /// referenceFrame: Transform que define "arriba" y el espacio de vuelo (GameSpace). Puede ser null.
    /// La escala de la referencia se aplica sola al convertir local → mundo.
    /// </summary>
    public void Initialize(Vector3 worldDirection, Transform referenceFrame)
    {
        initialized = true;
        reference = referenceFrame != null
            ? referenceFrame
            : (ARGameSpace.Instance != null ? ARGameSpace.Instance.transform : null);

        Vector3 localDir = reference != null
            ? reference.InverseTransformDirection(worldDirection)
            : worldDirection;
        localDir.Normalize();

        // Yaw y pitch con trigonometría. LookRotation falla (orientación arbitraria)
        // cuando la dirección es paralela al eje "arriba" de la referencia.
        yaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
        pitch = Mathf.Clamp(
            -Mathf.Asin(Mathf.Clamp(localDir.y, -1f, 1f)) * Mathf.Rad2Deg,
            -maxPitch, maxPitch);

        localPosition = reference != null
            ? reference.InverseTransformPoint(transform.position)
            : transform.position;

        ApplyPose();
        PreviousWorldPosition = transform.position;
    }

    /// <summary>Modelo visible del avión elegido en la escena de la marca.</summary>
    public void SetVisual(Transform modelo)
    {
        if (modelo != null)
            visual = modelo;
    }

    /// <summary>Entrada del joystick, valores entre -1 y 1 en cada eje.</summary>
    public void SetSteering(Vector2 input)
    {
        steerInput = Vector2.ClampMagnitude(input, 1f);
    }

    private void LateUpdate()
    {
        if (!initialized || IsCaptured) return;
        var game = GameManagerRA.Instance;
        if (game != null && game.IsPlaneRegistered(this) && (game.EsperandoSeguimiento || game.EstaPausada)) return;
        // Reproyectar la posición anterior con la pose ACTUAL del marcador evita falsos barridos al relocalizar AR.
        PreviousWorldPosition = reference != null ? reference.TransformPoint(localPosition) : localPosition;
        Quaternion previousRotation = ReferenceRotation * Quaternion.Euler(pitch, yaw, 0f);
        // Limitamos el dt para que un tirón de frame no atraviese una pared
        float dt = Mathf.Min(Time.deltaTime, 0.05f);

        if (!finished)
        {
            lifeTimer += dt;
            if (lifeTimer >= maxLifetime)
            {
                Finish(false);
            }
            else
            {
                // Suavizado de la entrada para que el vuelo sea fluido
                float t = 1f - Mathf.Exp(-inputSmoothing * dt);
                smoothedInput = Vector2.Lerp(smoothedInput, steerInput, t);

                yaw += smoothedInput.x * turnSpeed * dt;
                // En Unity, X positivo = morro hacia abajo; joystick arriba = subir
                pitch = Mathf.Clamp(pitch - smoothedInput.y * turnSpeed * dt, -maxPitch, maxPitch);

                Quaternion localRot = Quaternion.Euler(pitch, yaw, 0f);
                localPosition += localRot * Vector3.forward * (forwardSpeed * dt);
            }
        }

        if (IsCaptured) return;

        // Se aplica siempre (también tras terminar) para que el avión siga anclado al target
        ApplyPose();

        // Inclinación lateral solo visual
        if (visual != null)
            visual.localRotation = Quaternion.Euler(0f, 0f, -smoothedInput.x * maxBankAngle);

        if (CanInteract) contacts.CheckMovement(PreviousWorldPosition, previousRotation);
    }

    private void ApplyPose()
    {
        if (IsCaptured) return;

        Quaternion worldRot = ReferenceRotation * Quaternion.Euler(pitch, yaw, 0f);
        Vector3 worldPos = reference != null
            ? reference.TransformPoint(localPosition)
            : localPosition;

        transform.SetPositionAndRotation(worldPos, worldRot);
    }

    private void OnTriggerEnter(Collider other) { HandleContact(other); }
    private void OnTriggerStay(Collider other) { HandleContact(other); }

    /// <summary>Único resolutor para callbacks físicos y barridos de movimiento.</summary>
    public void HandleContact(Collider other) { HandleContact(other, null); }

    internal void HandleContact(Collider other, Vector3? sweepPosition)
    {
        if (!CanInteract || other == null || !other.enabled || !other.gameObject.activeInHierarchy
            || other.GetComponentInParent<PaperPlane>() != null) return;
        var balloon = other.GetComponentInParent<GloboTarget>();
        if (balloon != null) { balloon.PopBalloon(this); return; }
        var bird = other.GetComponentInParent<PajaroInteraccion>();
        if (bird != null)
        {
            if (sweepPosition.HasValue) bird.HandleSweptPlaneCollision(this);
            else bird.HandlePlaneCollision(this);
            return;
        }
        var target = other.GetComponentInParent<PlaneTarget>();
        if (target != null) { target.NotifyHit(this); return; }
        if (!other.isTrigger) ExplodeInAir(sweepPosition);
    }

    internal void CompleteTargetHit() { if (CanInteract) Finish(true); }

    /// <summary>
    /// El pájaro ha atrapado el avión con el pico y se lo lleva volando.
    /// </summary>
    public void CaptureByBird(Transform birdBeak)
    {
        TryCaptureByBird(birdBeak, null);
    }

    public bool TryCaptureByBird(Transform birdBeak, UnityEngine.Object bird)
    {
        if (!CanInteract || birdBeak == null || IsReservedByOtherBird(bird)) return false;
        IsCaptured = true;
        finished = true;
        reservedByBird = null;
        steerInput = Vector2.zero;

        // Desactivar físicas
        if (rb != null) { rb.isKinematic = true; rb.detectCollisions = false; }
        foreach (Collider col in GetComponentsInChildren<Collider>()) col.enabled = false;

        // Emparentar al pico del pájaro
        // Conserva posición, giro y escala mundiales: el cambio de padre no provoca ningún salto.
        transform.SetParent(birdBeak, true);

        OnFinished?.Invoke(false);
        return true;
    }

    /// <summary>
    /// El avión choca contra un obstáculo y explota en pedazos; los pájaros usan el robo.
    /// </summary>
    public void ExplodeInAir(Vector3? customImpactPos = null)
    {
        if (!CanInteract) return;
        finished = true;
        steerInput = Vector2.zero;
        DisableContacts();

        Vector3 spawnPos = customImpactPos.HasValue ? customImpactPos.Value : transform.position;

        // Instanciar la animación de choque / explosión
        if (explosionPrefab != null)
        {
            GameObject exp = Instantiate(explosionPrefab, spawnPos, Quaternion.identity);
            if (reference != null) exp.transform.localScale *= Mathf.Abs(reference.lossyScale.x);

            // Asegurar que Origen_Modelo compense la altura horneada de Maya (-1.52f)
            Transform origen = exp.transform.Find("Origen_Modelo");
            if (origen != null && Mathf.Abs(origen.localPosition.y) < 0.1f)
            {
                origen.localPosition = new Vector3(origen.localPosition.x, -1.52f, origen.localPosition.z);
            }

            Animator anim = exp.GetComponentInChildren<Animator>();
            if (anim == null)
            {
                anim = exp.AddComponent<Animator>();
            }

#if UNITY_EDITOR
            if (anim.runtimeAnimatorController == null)
            {
                anim.runtimeAnimatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Controllers/Explosion_Choque.controller");
            }
#endif
            if (anim != null && anim.runtimeAnimatorController != null)
            {
                anim.Play("Explosion_Choque_Completa", 0, 0f);
            }

            Destroy(exp, 1.8f);
        }

        // Ocultar modelo visual inmediatamente
        if (visual != null) visual.gameObject.SetActive(false);
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;

        OnFinished?.Invoke(false);
        Destroy(gameObject, 0.5f);
    }

    /// <summary>
    /// Al tocar un globo, el globo estalla y el avión desaparece.
    /// </summary>
    public void OnHitBalloon()
    {
        if (!CanInteract) return;
        finished = true;
        steerInput = Vector2.zero;
        DisableContacts();

        // Ocultar modelo visual del avión
        if (visual != null) visual.gameObject.SetActive(false);
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;

        OnFinished?.Invoke(true);
        Destroy(gameObject, 0.2f);
    }

    private void Finish(bool hitTarget)
    {
        if (finished) return;
        finished = true;
        steerInput = Vector2.zero;
        DisableContacts();
        OnFinished?.Invoke(hitTarget);
        Destroy(gameObject, destroyDelay);
    }

    private void DisableContacts()
    {
        reservedByBird = null;
        if (rb != null) rb.detectCollisions = false;
        foreach (var collider in GetComponentsInChildren<Collider>(true)) collider.enabled = false;
    }

    /// <summary>La ronda ha terminado o se reinicia: retirar sin generar otra pérdida.</summary>
    public void CancelForRoundEnd()
    {
        finished = true; initialized = false; reservedByBird = null;
        foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;
        gameObject.SetActive(false); Destroy(gameObject);
    }
    public void ExpireFlight()
    {
        if (finished) return;
        foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;
        foreach (var renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = false;
        Finish(false);
    }
}
