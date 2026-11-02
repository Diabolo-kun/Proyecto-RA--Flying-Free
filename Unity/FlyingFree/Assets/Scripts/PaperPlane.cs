using System;
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

    /// <summary>Se dispara una vez: true = tocó el objetivo, false = chocó o se acabó el tiempo.</summary>
    public event Action<bool> OnFinished;

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
        reference = referenceFrame;

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
    }

    /// <summary>Entrada del joystick, valores entre -1 y 1 en cada eje.</summary>
    public void SetSteering(Vector2 input)
    {
        steerInput = Vector2.ClampMagnitude(input, 1f);
    }

    private void LateUpdate()
    {
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

    private void OnTriggerEnter(Collider other)
    {
        if (finished || IsCaptured) return;

        // 1. ¿Es un globo?
        GloboTarget globo = other.GetComponentInParent<GloboTarget>();
        if (globo != null)
        {
            globo.PopBalloon(this);
            return;
        }

        // 2. ¿Es un pájaro?
        PajaroInteraccion pajaro = other.GetComponentInParent<PajaroInteraccion>();
        if (pajaro != null)
        {
            pajaro.HandlePlaneCollision(this);
            return;
        }

        // 3. ¿Es un objetivo clásico?
        PlaneTarget target = other.GetComponentInParent<PlaneTarget>();
        if (target != null)
        {
            target.NotifyHit(this);
            Finish(true);
            return;
        }

        // Cualquier otro collider sólido (pared, suelo...) = choque con explosión
        if (!other.isTrigger)
        {
            ExplodeInAir();
        }
    }

    /// <summary>
    /// El pájaro ha atrapado el avión con el pico y se lo lleva volando.
    /// </summary>
    public void CaptureByBird(Transform birdBeak)
    {
        if (finished || IsCaptured) return;
        IsCaptured = true;
        finished = true;
        steerInput = Vector2.zero;

        // Desactivar físicas
        if (rb != null) rb.isKinematic = true;

        // Emparentar al pico del pájaro
        transform.SetParent(birdBeak, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        OnFinished?.Invoke(false);
        Destroy(gameObject, 6f);
    }

    /// <summary>
    /// El avión choca contra un pájaro u obstáculo y explota en pedazos.
    /// </summary>
    public void ExplodeInAir(Vector3? customImpactPos = null)
    {
        if (finished || IsCaptured) return;
        finished = true;
        steerInput = Vector2.zero;

        Vector3 spawnPos = customImpactPos.HasValue ? customImpactPos.Value : transform.position;

        // Instanciar la animación de choque / explosión
        if (explosionPrefab != null)
        {
            GameObject exp = Instantiate(explosionPrefab, spawnPos, Quaternion.identity);

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
        if (finished || IsCaptured) return;
        finished = true;
        steerInput = Vector2.zero;

        // Ocultar modelo visual del avión
        if (visual != null) visual.gameObject.SetActive(false);
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;

        OnFinished?.Invoke(true);
        Destroy(gameObject, 0.2f);
    }

    private void Finish(bool hitTarget)
    {
        finished = true;
        steerInput = Vector2.zero;
        OnFinished?.Invoke(hitTarget);
        Destroy(gameObject, destroyDelay);
    }
}
