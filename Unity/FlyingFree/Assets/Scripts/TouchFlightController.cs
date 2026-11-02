using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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
    [SerializeField] private PaperPlane planePrefab;
    [Tooltip("Posición de salida en viewport (0-1). (0.5, 0.15) = abajo al centro.")]
    [SerializeField] private Vector2 spawnViewport = new Vector2(0.5f, 0.15f);
    [SerializeField] private float spawnDistance = 1.5f;
    [Tooltip("Si está activo, el avión sale apuntando hacia el punto tocado. Si no, hacia delante de la cámara.")]
    [SerializeField] private bool aimAtTouch = true;
    [SerializeField] private float aimDistance = 20f;

    [Header("Realidad Aumentada (Vuforia)")]
    [Tooltip("Image Target (o su contenido). Define el 'arriba' del vuelo y la escala. Puede dejarse vacío.")]
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
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        var pointer = Pointer.current;
        if (pointer == null) return;

        Vector2 pointerPos = pointer.position.ReadValue();

        if (pointer.press.wasPressedThisFrame && !IsPointerOverUI())
        {
            BeginTouch(pointerPos);
        }

        if (!holding) return;

        if (pointer.press.isPressed)
        {
            UpdateJoystick(pointerPos);
        }
        else
        {
            EndTouch();
        }
#else
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
            BeginTouch(Input.mousePosition);

        if (!holding) return;

        if (Input.GetMouseButton(0))
            UpdateJoystick(Input.mousePosition);
        else
            EndTouch();
#endif
    }

    // ───────────── Toque ─────────────

    private void BeginTouch(Vector2 screenPos)
    {
        // Solo un avión a la vez
        if (currentPlane != null)
            Destroy(currentPlane.gameObject);

        SpawnPlane(screenPos);

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

    private void SpawnPlane(Vector2 screenPos)
    {
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

        // Sin parent: así no se oculta si Vuforia pierde el target
        currentPlane = Instantiate(planePrefab, spawnPos, Quaternion.LookRotation(dir));
        currentPlane.transform.localScale *= scale;
        currentPlane.Initialize(dir, arReference);
        currentPlane.OnFinished += HandlePlaneFinished;
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

#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            int touchId = Touchscreen.current.primaryTouch.touchId.ReadValue();
            return EventSystem.current.IsPointerOverGameObject(touchId);
        }
        return EventSystem.current.IsPointerOverGameObject();
#else
        if (Input.touchCount > 0)
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);

        return EventSystem.current.IsPointerOverGameObject();
#endif
    }
}
