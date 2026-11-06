using UnityEngine;

/// <summary>Intercepción lateral, agarre exclusivo y huida. Único dueño del movimiento durante el robo.</summary>
[DefaultExecutionOrder(200)]
[RequireComponent(typeof(Collider))]
public class PajaroInteraccion : MonoBehaviour
{
    public enum EstadoRobo { Patrulla, Aproximacion, Transportando, Saliendo }
    [Header("Socket lateral del ala (PaperGrip_SOCKET)")]
    public Transform beakSocket;
    [SerializeField] Animator animator;
    [SerializeField] string captureAnimState = "Capture";
    [Header("Intercepción")]
    [Min(.1f)] public float radioDeteccion = .9f;
    [Min(.1f)] public float velocidadAproximacion = 3.5f;
    [Min(.1f)] public float tiempoMaximoAproximacion = 1.5f;
    [Header("Huida")]
    [SerializeField, Min(.1f)] float carryFlySpeed = 2.5f;
    [Min(.1f)] public float tiempoMinimoHuida = 1.5f;
    [Min(1)] public float tiempoMaximoHuida = 5f;
    [Tooltip("Conservado por compatibilidad. Los pájaros roban; no hacen explotar el avión.")]
    public GameObject plumasEffectPrefab;
    public EstadoRobo Estado { get; private set; }
    public bool HasCapturedPlane => Estado == EstadoRobo.Transportando;
    public bool OcupaMovimiento => Estado != EstadoRobo.Patrulla;
    public PaperPlane AvionRobado => llevado;
    ControlBandada gestor;
    Camera camara;
    PaperPlane objetivo, llevado;
    float reloj;
    Vector3 escapeLocal, previousLocal;
    bool previousValid;
    AnimatorCullingMode originalCulling;
    float originalAnimatorSpeed = 1;

    void Awake()
    {
        foreach (var col in GetComponentsInChildren<Collider>()) col.isTrigger = true;
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            originalCulling = animator.cullingMode;
            originalAnimatorSpeed = animator.speed;
            // El socket animado debe evaluarse incluso cuando nace detrás del usuario.
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
            if (child.name == "PaperGrip_SOCKET" || child.name == "gripSocket") { beakSocket = child; break; }
        if (beakSocket == null) beakSocket = transform;
    }
    public void Configurar(ControlBandada manager, Camera camera)
    {
        gestor = manager; camara = camera;
        // El controlador de la demo antigua solo trabaja con PaperPlaneMotor.
        var legacy = GetComponent<FlyingFree.Sprint2.BirdCarryController>();
        if (legacy != null) legacy.enabled = false;
    }
    void OnTriggerEnter(Collider other)
    {
        var plane = other.GetComponentInParent<PaperPlane>();
        if (plane != null) HandlePlaneCollision(plane);
    }
    public void HandlePlaneCollision(PaperPlane plane) { IntentarInterceptar(plane); }
    public bool IntentarInterceptar(PaperPlane plane)
    {
        if (!isActiveAndEnabled || OcupaMovimiento || plane == null || !plane.IsFlying
            || (gestor != null && !gestor.PuedeSimular)) return false;
        if (Vector3.Distance(transform.position, plane.transform.position) > radioDeteccion + .5f) return false;
        if (!plane.TryReserveForBird(this)) return false;
        objetivo = plane; reloj = 0; Estado = EstadoRobo.Aproximacion;
        var legacy = GetComponent<FlyingFree.Sprint2.BirdCarryController>();
        if (legacy != null) legacy.enabled = false;
        if (animator != null && animator.runtimeAnimatorController != null && animator.HasState(0, Animator.StringToHash(captureAnimState)))
        {
            // La maniobra es breve en gameplay; la pose lateral de agarre queda al contacto.
            animator.CrossFadeInFixedTime(captureAnimState, .08f, 0, .3f);
        }
        return true;
    }
    void LateUpdate()
    {
        bool paused = gestor != null && !gestor.PuedeSimular;
        if (animator != null) animator.speed = paused ? 0 : originalAnimatorSpeed;
        if (paused) { previousValid = false; return; }
        float dt = Time.deltaTime;
        reloj += dt;
        if (Estado == EstadoRobo.Patrulla) BuscarAvion();
        else if (Estado == EstadoRobo.Aproximacion) Aproximar(dt);
        else Escapar(dt);
        previousLocal = transform.localPosition; previousValid = true;
    }
    void BuscarAvion()
    {
        PaperPlane best = null; float bestDistance = radioDeteccion * radioDeteccion;
        Vector3 previousWorld = previousValid && transform.parent != null
            ? transform.parent.TransformPoint(previousLocal) : transform.position;
        foreach (var plane in PaperPlane.ActivePlanes)
        {
            if (plane == null || !plane.IsFlying || plane.IsReservedByOtherBird(this)) continue;
            Vector3 relative = plane.transform.position - transform.position;
            Vector3 oldRelative = plane.PreviousWorldPosition - previousWorld;
            Vector3 delta = relative - oldRelative;
            float t = delta.sqrMagnitude > .000001f ? Mathf.Clamp01(-Vector3.Dot(oldRelative, delta) / delta.sqrMagnitude) : 1;
            float distance = (oldRelative + delta * t).sqrMagnitude;
            // Solo intentos desde el costado del avión. Contactos físicos también entran por el trigger.
            float lateral = relative.sqrMagnitude > .001f ? Mathf.Abs(Vector3.Dot(relative.normalized, plane.transform.forward)) : 0;
            if (distance < bestDistance && (lateral < .8f || distance < .08f)) { best = plane; bestDistance = distance; }
        }
        if (best != null) IntentarInterceptar(best);
    }
    void Aproximar(float dt)
    {
        if (objetivo == null || !objetivo.IsFlying || reloj > tiempoMaximoAproximacion)
        { SalirSinAvion(); return; }
        // El socket está girado 90 grados respecto al pico: al alinearlo se llega de lado.
        Quaternion desired = objetivo.transform.rotation * Quaternion.Inverse(beakSocket.rotation) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, desired, 1 - Mathf.Exp(-18 * dt));
        Vector3 grip = beakSocket.TransformPoint(objetivo.CaptureGripOffset);
        Vector3 movement = objetivo.transform.position - grip;
        float speed = velocidadAproximacion + objetivo.WorldVelocity.magnitude;
        transform.position += Vector3.ClampMagnitude(movement, speed * dt);
        float gap = Vector3.Distance(beakSocket.TransformPoint(objetivo.CaptureGripOffset), objetivo.transform.position);
        float angle = Quaternion.Angle(beakSocket.rotation, objetivo.transform.rotation);
        if (gap > .045f || angle > 12f) return;
        // La corrección máxima son 4.5 cm; es el pájaro el que termina la aproximación, nunca el avión.
        transform.rotation = desired;
        transform.position += objetivo.transform.position - beakSocket.TransformPoint(objetivo.CaptureGripOffset);
        if (!objetivo.TryCaptureByBird(beakSocket, this)) { SalirSinAvion(); return; }
        llevado = objetivo; objetivo = null; Estado = EstadoRobo.Transportando; reloj = 0;
        PrepararHuida();
    }
    void PrepararHuida()
    {
        Vector3 up = transform.parent != null ? transform.parent.up : Vector3.up;
        Vector3 direction = (transform.forward + up * .65f).normalized;
        escapeLocal = transform.parent != null ? transform.parent.InverseTransformDirection(direction) : direction;
    }
    void SalirSinAvion()
    {
        if (objetivo != null) objetivo.ReleaseBirdReservation(this);
        objetivo = null; Estado = EstadoRobo.Saliendo; reloj = 0; PrepararHuida();
    }
    void Escapar(float dt)
    {
        Vector3 world = transform.parent != null ? transform.parent.TransformDirection(escapeLocal) : escapeLocal;
        transform.position += world * carryFlySpeed * dt;
        Camera camera = camara != null ? camara : Camera.main;
        bool fuera = camera == null;
        if (camera != null)
        {
            Vector3 p = camera.WorldToViewportPoint(transform.position);
            fuera = p.z <= 0 || p.x < -.2f || p.x > 1.2f || p.y < -.2f || p.y > 1.2f;
        }
        if ((reloj >= tiempoMinimoHuida && fuera) || reloj >= tiempoMaximoHuida) DesaparecerPajaro();
    }
    public void DesaparecerPajaro()
    {
        if (objetivo != null) objetivo.ReleaseBirdReservation(this);
        objetivo = null;
        gameObject.SetActive(false); Destroy(gameObject);
    }
    void OnDisable()
    {
        if (objetivo != null) objetivo.ReleaseBirdReservation(this);
        if (animator != null) { animator.speed = originalAnimatorSpeed; animator.cullingMode = originalCulling; }
    }
}
