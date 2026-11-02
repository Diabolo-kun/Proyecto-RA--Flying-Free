using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Al tocar la pantalla: lanza un avión de papel desde la cámara AR.
/// Mientras se mantiene el dedo: muestra un joystick flotante en el punto del toque
/// y envía su valor al avión. Al soltar, el joystick se oculta y el avión sigue recto.
///
/// La cámara NO sigue al avión: es la ARCamera de Vuforia y se mueve con el dispositivo.
/// El avión se instancia en el mundo (sin parent) para que no desaparezca
/// si Vuforia pierde el tracking del target.
///
/// Usa Input.GetMouseButton*, que en Android equivale al primer dedo
/// y permite probar con ratón en el editor.
/// </summary>
public class TouchFlightController : MonoBehaviour
{
    [Header("Avión")]
    [Tooltip("La ARCamera de Vuforia. Si se deja vacío usa Camera.main.")]
    [SerializeField] private Camera cam;
    [Tooltip("Prefab del avión. El script PaperPlane debe estar en el objeto RAÍZ del prefab.")]
    [SerializeField] private GameObject planePrefab;
    [Tooltip("Posición de salida en viewport (0-1). (0.5, 0.15) = abajo al centro.")]
    [SerializeField] private Vector2 spawnViewport = new Vector2(0.5f, 0.15f);
    [SerializeField] private float spawnDistance = 1.5f;
    [Tooltip("Si está activo, el avión sale apuntando hacia el punto tocado. Si no, hacia delante de la cámara.")]
    [SerializeField] private bool aimAtTouch = true;
    [SerializeField] private float aimDistance = 20f;

    [Header("Realidad Aumentada (Vuforia)")]
    [Tooltip("GameSpace (hijo del Image Target). Define el 'arriba' del vuelo y la escala. Puede dejarse vacío.")]
    [SerializeField] private Transform arReference;
    [Tooltip("Multiplica distancias, velocidad y tamaño del avión por la escala de la referencia.")]
    [SerializeField] private bool scaleWithReference = true;

    [Header("Joystick (Canvas en Screen Space - Overlay)")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform joystickBase;
    [SerializeField] private RectTransform joystickHandle;
    [Range(0f, 0.5f)]
    [SerializeField] private float deadZone = 0.1f;

    private PaperPlane currentPlane;
    private Vector2 touchOrigin;
    private bool holding;

    private void Start()
    {
        if (cam == null) cam = Camera.main;
        joystickBase.gameObject.SetActive(false);

        EnsureGameSpaceReference();
    }

    private void EnsureGameSpaceReference()
    {
        ARGameSpace gs = ARGameSpace.GetOrCreateInstance();
        if (gs != null)
        {
            // Si no tiene referencia asignada o apuntaba al ImageTarget crudo, redirigir a GameSpace
            if (arReference == null || (gs.TargetMarker != null && arReference == gs.TargetMarker))
            {
                arReference = gs.transform;
            }
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
            BeginTouch(Input.mousePosition);

        if (!holding) return;

        if (Input.GetMouseButton(0))
            UpdateJoystick(Input.mousePosition);
        else
            EndTouch();
    }

    private void OnDisable()
    {
        // Si otro script (p. ej. GameManagerRA al terminar la partida) desactiva este
        // controlador con el dedo pulsado, Update deja de ejecutarse y el joystick
        // se quedaría visible en pantalla. Lo limpiamos aquí.
        holding = false;

        if (joystickBase != null)
            joystickBase.gameObject.SetActive(false);

        if (currentPlane != null)
            currentPlane.SetSteering(Vector2.zero);
    }

    // ───────────── Toque ─────────────

    private void BeginTouch(Vector2 screenPos)
    {
        // Solo un avión a la vez
        if (currentPlane != null)
            Destroy(currentPlane.gameObject);

        // Si no se pudo crear el avión (prefab mal asignado), no mostramos joystick
        if (!SpawnPlane(screenPos))
            return;

        touchOrigin = screenPos;
        holding = true;

        joystickBase.gameObject.SetActive(true);
        joystickBase.position = screenPos;          // válido en Canvas Overlay
        joystickHandle.anchoredPosition = Vector2.zero;
    }

    private void EndTouch()
    {
        holding = false;
        joystickBase.gameObject.SetActive(false);

        if (currentPlane != null)
            currentPlane.SetSteering(Vector2.zero);
    }

    // ───────────── Avión ─────────────

    private bool SpawnPlane(Vector2 screenPos)
    {
        EnsureGameSpaceReference();

        // Si no está asignado directamente, obtener del PlaneSkinManager
        if (planePrefab == null && PlaneSkinManager.Instance != null)
        {
            PaperPlane active = PlaneSkinManager.Instance.GetActivePrefab();
            if (active != null) planePrefab = active.gameObject;
        }

        if (planePrefab == null)
        {
            Debug.LogError("[TouchFlightController] 'Plane Prefab' está vacío. Asigna el prefab del avión.", this);
            return false;
        }

        float scale = (arReference != null && scaleWithReference)
            ? arReference.lossyScale.x
            : 1f;

        Vector3 spawnPos = cam.ViewportToWorldPoint(
            new Vector3(spawnViewport.x, spawnViewport.y, spawnDistance * scale));

        Vector3 dir = cam.transform.forward;
        if (aimAtTouch)
        {
            Ray ray = cam.ScreenPointToRay(screenPos);
            Vector3 aimPoint = ray.GetPoint(aimDistance * scale);
            dir = (aimPoint - spawnPos).normalized;
        }

        // Sin parent: así no se oculta si Vuforia pierde el target.
        // Se instancia como GameObject y se busca el componente después, en lugar de
        // Instantiate<PaperPlane>, para evitar el InvalidCastException de las builds.
        GameObject planeObject = Instantiate(planePrefab, spawnPos, Quaternion.LookRotation(dir));

        PaperPlane plane = planeObject.GetComponent<PaperPlane>();
        if (plane == null)
        {
            Debug.LogError("[TouchFlightController] El prefab asignado no tiene el script PaperPlane en el objeto raíz.", planePrefab);
            Destroy(planeObject);
            return false;
        }

        planeObject.transform.localScale *= scale;
        plane.Initialize(dir, arReference);
        plane.OnFinished += HandlePlaneFinished;
        currentPlane = plane;
        return true;
    }

    private void HandlePlaneFinished(bool hitTarget)
    {
        currentPlane = null;

        // Si el dedo seguía pulsado, ocultamos el joystick;
        // para lanzar otro avión hay que soltar y volver a tocar.
        if (holding)
        {
            holding = false;
            joystickBase.gameObject.SetActive(false);
        }

        Debug.Log(hitTarget ? "¡Objetivo alcanzado!" : "El avión se estrelló");
    }

    // ───────────── Joystick ─────────────

    private void UpdateJoystick(Vector2 screenPos)
    {
        float scale = canvas.scaleFactor;
        float radiusPx = joystickBase.rect.width * 0.5f * scale;

        Vector2 delta = Vector2.ClampMagnitude(screenPos - touchOrigin, radiusPx);

        // Posición del stick en unidades del Canvas
        joystickHandle.anchoredPosition = delta / scale;

        Vector2 input = delta / radiusPx;
        if (input.magnitude < deadZone) input = Vector2.zero;

        if (currentPlane != null)
            currentPlane.SetSteering(input);
    }

    // ───────────── Utilidades ─────────────

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        if (Input.touchCount > 0)
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);

        return EventSystem.current.IsPointerOverGameObject();
    }
}
