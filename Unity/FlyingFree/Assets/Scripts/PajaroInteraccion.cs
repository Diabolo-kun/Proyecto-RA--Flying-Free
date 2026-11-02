using UnityEngine;

/// <summary>
/// Gestiona las colisiones e interacciones entre el pájaro y el avión de papel:
/// - Caso 1 (Pájaro atrapa avión): El pájaro agarra el avión con el pico y se lo lleva volando.
/// - Caso 2 (Avión embiste al pájaro): El avión explota en un choque cómico.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PajaroInteraccion : MonoBehaviour
{
    [Header("Pico / Garra de Agarre")]
    [Tooltip("Transform del pico o socket donde se sujetará el avión al atraparlo")]
    public Transform beakSocket;

    [Header("Animación")]
    [SerializeField] private Animator animator;
    [SerializeField] private string captureAnimState = "Capture";

    [Header("Vuelo tras captura")]
    [SerializeField] private float carryFlySpeed = 2.5f;

    private bool hasCapturedPlane = false;
    public bool HasCapturedPlane => hasCapturedPlane;
    private PaperPlane capturedPlane = null;
    private Collider birdCollider;

    private void Awake()
    {
        birdCollider = GetComponent<Collider>();
        birdCollider.isTrigger = true;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

#if UNITY_EDITOR
        if (animator != null && animator.runtimeAnimatorController == null)
        {
            animator.runtimeAnimatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Pajaro.controller");
        }
#endif

        if (beakSocket == null)
        {
            // Buscar hijo llamado gripSocket o Pico en cualquier nivel de la jerarquía
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name.Equals("gripSocket", System.StringComparison.OrdinalIgnoreCase) ||
                    t.name.Equals("Pico", System.StringComparison.OrdinalIgnoreCase))
                {
                    beakSocket = t;
                    break;
                }
            }
            if (beakSocket == null) beakSocket = transform;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasCapturedPlane) return;

        PaperPlane plane = other.GetComponentInParent<PaperPlane>();
        if (plane != null)
        {
            HandlePlaneCollision(plane);
        }
    }

    /// <summary>
    /// Gestiona la colisión con el avión desde cualquier lado del trigger.
    /// </summary>
    public void HandlePlaneCollision(PaperPlane plane)
    {
        if (hasCapturedPlane || plane == null || plane.IsCaptured) return;

        // Determinar si el pájaro caza al avión o el avión embiste al pájaro:
        Vector3 haciaAvion = (plane.transform.position - transform.position).normalized;
        float alineacionPajaro = Vector3.Dot(transform.forward, haciaAvion);

        // Si el pájaro encara al avión de frente (caza intencional):
        if (alineacionPajaro > 0.2f)
        {
            PajaroAtrapaAvion(plane);
        }
        else
        {
            // Si el avión lo embiste de lado/atrás: el avión explota
            AvionEmbistePajaro(plane);
        }
    }

    [Header("Efectos Choque")]
    [Tooltip("Prefab opcional para futuras partículas o animación de plumas al chocar")]
    public GameObject plumasEffectPrefab;

    /// <summary>
    /// El pájaro intercepta el avión, lo atrapa con el pico y se lo lleva volando.
    /// </summary>
    private void PajaroAtrapaAvion(PaperPlane plane)
    {
        hasCapturedPlane = true;
        capturedPlane = plane;

        Debug.Log("[PajaroInteraccion] ¡El pájaro ha atrapado el avión con el pico!");

        if (animator != null)
        {
            animator.CrossFadeInFixedTime(captureAnimState, 0.08f);
        }

        // Sujetar el avión en el pico del pájaro y desactivar su vuelo libre
        plane.CaptureByBird(beakSocket != null ? beakSocket : transform);
    }

    /// <summary>
    /// El avión choca contra el pájaro y estalla en pedazos cómicos, y el pájaro también desaparece.
    /// </summary>
    private void AvionEmbistePajaro(PaperPlane plane)
    {
        Debug.Log("[PajaroInteraccion] ¡El avión ha chocado contra el pájaro y ha explotado!");

        // 1. Punto de impacto exacto entre avión y pájaro
        Vector3 puntoImpacto = plane != null 
            ? (plane.transform.position + transform.position) * 0.5f 
            : transform.position;

        // 2. Hace explotar el avión con la animación Explosion_Choque directamente en el punto de choque
        if (plane != null)
        {
            plane.ExplodeInAir(puntoImpacto);
        }

        // 3. Preparado para la futura animación de plumas o partículas del pájaro
        if (plumasEffectPrefab != null)
        {
            Instantiate(plumasEffectPrefab, transform.position, Quaternion.identity);
        }
        OnBirdHit();

        // 4. El pájaro también desaparece de inmediato tras el choque
        DesaparecerPajaro();
    }

    /// <summary>
    /// Oculta y destruye el pájaro tras ser embestido por el avión.
    /// </summary>
    public void DesaparecerPajaro()
    {
        // Desactivar colliders para no generar más eventos
        foreach (var c in GetComponentsInChildren<Collider>())
        {
            c.enabled = false;
        }

        // Ocultar mallas visuales de inmediato
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            r.enabled = false;
        }

        Transform visual = transform.Find("Visual");
        if (visual != null)
        {
            visual.gameObject.SetActive(false);
        }

        Destroy(gameObject, 0.15f);
    }

    private void OnBirdHit()
    {
        // Más adelante aquí se disparará la animación de plumas
        // Por ahora, el pájaro realiza un pequeño sobresalto si no se destruye de inmediato
        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }
    }

    private void Update()
    {
        // Si lleva el avión atrapado, elevarse suavemente hacia el cielo llevándoselo
        if (hasCapturedPlane && capturedPlane != null)
        {
            transform.position += (transform.forward + (Vector3.up * 0.4f)).normalized * carryFlySpeed * Time.deltaTime;
        }
    }
}
