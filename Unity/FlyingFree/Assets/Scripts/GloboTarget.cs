using UnityEngine;
using UnityEngine.Events;

/// <summary>Globo de gameplay: reserva única del impacto, explosión y notificación a la partida.</summary>
[DisallowMultipleComponent]
public class GloboTarget : MonoBehaviour
{
    [Header("Puntuación")]
    [SerializeField, Min(1)] private int points = 1;
    [Header("Animación y Efectos")]
    [SerializeField] private Animator animator;
    [SerializeField] private string explodeAnimationState = "Globo_Explosion_Explotar_Globo";
    [SerializeField] private GameObject explosionEffectPrefab;
    [Tooltip("Tiempo mínimo de permanencia. Si el clip dura más, se deja terminar.")]
    [SerializeField, Min(.1f)] private float destroyDelay = .8f;
    [Header("Interacción")]
    [SerializeField] private UnityEvent onPopped = new UnityEvent();
    public bool IsPopped => isPopped;
    private bool isPopped;

    private void Awake()
    {
        Transform origin = transform.Find("Origen_Modelo");
        if (origin != null && Mathf.Abs(origin.localPosition.y) < .1f)
            origin.localPosition = new Vector3(origin.localPosition.x, -1.45f, origin.localPosition.z);

        // Desactivar el collider residual inmediatamente: Destroy lo retira al final del frame.
        var box = GetComponent<BoxCollider>();
        if (box != null && box.size.sqrMagnitude > .4f)
        {
            box.enabled = false;
            Destroy(box);
            var sphere = gameObject.AddComponent<SphereCollider>();
            sphere.radius = .22f;
        }
        if (GetComponent<Collider>() == null)
        {
            var sphere = gameObject.AddComponent<SphereCollider>();
            sphere.radius = .22f;
        }
        foreach (var collider in GetComponentsInChildren<Collider>(true)) collider.isTrigger = true;
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void OnTriggerEnter(Collider other) { Contact(other); }
    private void OnTriggerStay(Collider other) { Contact(other); }
    private void Contact(Collider other)
    {
        if (other == null || !other.enabled || !other.gameObject.activeInHierarchy) return;
        var plane = other.GetComponentInParent<PaperPlane>();
        if (plane != null) PopBalloon(plane);
    }

    /// <summary>Entrada conservada para los otros sistemas y los eventos existentes.</summary>
    public void PopBalloon(PaperPlane plane) { TryPopBalloon(plane); }

    public bool TryPopBalloon(PaperPlane plane)
    {
        if (!isActiveAndEnabled || isPopped || plane == null || !plane.CanInteract) return false;
        // Marcar ANTES de avisar a puntuación, fin de avión y listeners externos.
        isPopped = true;
        var game = GameManagerRA.Instance;
        if (game != null && !game.TryPopBalloon(this, plane, points))
        { isPopped = false; return false; }

        foreach (var collider in GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        var movement = GetComponent<BalloonAnimator>();
        if (movement != null) movement.enabled = false;

        if (game == null)
        {
            plane.OnHitBalloon();
            if (ScoreManager.Instance != null) ScoreManager.Instance.AddScore(Mathf.Max(1, points));
        }

        float lifetime = Mathf.Max(.1f, destroyDelay);
        int state = Animator.StringToHash(explodeAnimationState);
        if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null
            && animator.HasState(0, state))
        {
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.speed = 1;
            animator.Play(state, 0, 0);
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
                if (clip.name == explodeAnimationState) lifetime = Mathf.Max(lifetime, clip.length);
        }
        else
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        }
        if (explosionEffectPrefab != null)
        {
            var effect = Instantiate(explosionEffectPrefab, transform.position, transform.rotation, transform.parent);
            Destroy(effect, lifetime);
        }
        Destroy(gameObject, lifetime);
        onPopped?.Invoke();
        return true;
    }
}
