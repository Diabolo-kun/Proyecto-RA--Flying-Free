using UnityEngine;

/// <summary>
/// Componente para los globos objetivos.
/// Al ser tocado por un avión de papel:
/// 1. Reproduce la animación o efecto de estallar del globo.
/// 2. Suma un punto al marcador (ScoreManager).
/// 3. Hace desaparecer el avión de papel.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GloboTarget : MonoBehaviour
{
    [Header("Puntuación")]
    [SerializeField] private int points = 1;

    [Header("Animación y Efectos")]
    [Tooltip("Animator del globo (si usa el prefab Globo_Explosion)")]
    [SerializeField] private Animator animator;

    [Tooltip("Nombre del estado de animación de estallido")]
    [SerializeField] private string explodeAnimationState = "Globo_Explosion_Explotar_Globo";

    [Tooltip("Prefab opcional de partículas de explosión")]
    [SerializeField] private GameObject explosionEffectPrefab;

    [Tooltip("Tiempo de retardo antes de destruir el GameObject tras la explosión")]
    [SerializeField] private float destroyDelay = 0.8f;

    private Collider col;
    private bool isPopped = false;

    private void Awake()
    {
        // Compensar el offset horneado en Maya (-1.45m) si no está aplicado en el prefab
        Transform origen = transform.Find("Origen_Modelo");
        if (origen != null && Mathf.Abs(origen.localPosition.y) < 0.1f)
        {
            origen.localPosition = new Vector3(origen.localPosition.x, -1.45f, origen.localPosition.z);
        }

        // Si existe un BoxCollider residual gigante (ej. 1x1x1m), sustituirlo por un SphereCollider ajustado al globo
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null && box.size.sqrMagnitude > 0.4f)
        {
            Destroy(box);
            SphereCollider sc = gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.22f;
            sc.center = Vector3.zero;
            col = sc;
        }
        else
        {
            col = GetComponent<Collider>();
            if (col == null)
            {
                SphereCollider sc = gameObject.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = 0.22f;
                sc.center = Vector3.zero;
                col = sc;
            }
            else
            {
                col.isTrigger = true;
            }
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
            animator = gameObject.AddComponent<Animator>();

#if UNITY_EDITOR
        if (animator.runtimeAnimatorController == null)
        {
            animator.runtimeAnimatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Controllers/Globo_Explosion.controller");
        }
#endif
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isPopped) return;

        // Comprobar si el que colisionó es un avión de papel
        PaperPlane plane = other.GetComponentInParent<PaperPlane>();
        if (plane != null)
        {
            PopBalloon(plane);
        }
    }

    /// <summary>
    /// Ejecuta el estallido del globo, suma el punto y hace desaparecer el avión.
    /// </summary>
    public void PopBalloon(PaperPlane plane)
    {
        if (isPopped) return;
        isPopped = true;

        // Desactivar collider para que no sume doble
        if (col != null) col.enabled = false;

        // 1. Sumar punto al marcador
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(points);
        }
        else
        {
            Debug.Log($"[GloboTarget] ¡Globo estallado! +{points} punto(s).");
        }

        // 2. Reproducir animación de explosión del globo
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.Play(explodeAnimationState, 0, 0f);
        }
        else
        {
            // Si no tiene esa animación (ej. globo procedural), ocultamos el modelo visual
            MeshRenderer rend = GetComponentInChildren<MeshRenderer>();
            if (rend != null) rend.enabled = false;
        }

        // Instanciar efecto si está asignado
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        // 3. Hacer desaparecer el avión de papel
        if (plane != null)
        {
            plane.OnHitBalloon();
        }

        // Destruir el globo tras la animación
        Destroy(gameObject, destroyDelay);
    }
}
