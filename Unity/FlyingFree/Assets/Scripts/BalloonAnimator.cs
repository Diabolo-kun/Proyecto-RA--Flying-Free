using System.Collections;
using UnityEngine;

/// <summary>
/// Controla la animación del globo/prefab generado:
/// 1. Sube desde el suelo (altura inicial) hasta una altura aleatoria con aceleración y frenado suave.
/// 2. Una vez alcanzada la altura, realiza una suave flotación y oscilación (vaivén) natural.
/// 3. Opcionalmente asigna una variación de color aleatoria al material.
/// </summary>
public class BalloonAnimator : MonoBehaviour
{
    public enum MovementAxis
    {
        [Tooltip("Sube en el eje vertical Z del plano del target (ideal para targets en pared)")]
        Local_Z_WallUp,
        [Tooltip("Sube en el eje normal Y del target (perpendicular al plano)")]
        Local_Y_Normal,
        [Tooltip("Sube en el eje vertical absoluto del mundo (Y mundial)")]
        World_Y
    }

    [Header("Eje de Subida")]
    [Tooltip("Eje a lo largo del cual se realiza el movimiento de subida")]
    public MovementAxis movementAxis = MovementAxis.Local_Z_WallUp;

    [Header("Parámetros de Altura")]
    [Tooltip("Altura mínima a la que puede subir el globo")]
    public float minHeight = 0.5f;

    [Tooltip("Altura máxima a la que puede subir el globo")]
    public float maxHeight = 1.5f;

    [Tooltip("Tiempo en segundos que tarda en subir")]
    public float riseDuration = 2.5f;

    [Tooltip("Curva de suavizado para la subida (por defecto Ease-Out)")]
    public AnimationCurve riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Efecto de Flotación (Idle)")]
    [Tooltip("Activar suave vaivén y balanceo cuando alcance la altura")]
    public bool enableIdleFloat = true;

    [Tooltip("Amplitud de la oscilación vertical")]
    public float bobbingAmplitude = 0.04f;

    [Tooltip("Frecuencia de la oscilación vertical")]
    public float bobbingFrequency = 1.5f;

    [Tooltip("Ángulo máximo de inclinación/balanceo en grados")]
    public float swayMaxAngle = 4.0f;

    [Tooltip("Frecuencia del balanceo lateral")]
    public float swayFrequency = 1.0f;

    [Header("Variación Visual")]
    [Tooltip("Si está activo, asigna un color aleatorio vibrante al material al nacer")]
    public bool randomizeColor = true;

    private Vector3 initialBasePos;
    private Vector3 targetLocalPos;
    private Quaternion initialLocalRot;
    private float chosenHeight = 1.0f;
    private float elapsedTime = 0f;
    private bool isRising = false;
    private float randomPhaseOffset;

    private void Awake()
    {
        initialLocalRot = transform.localRotation;
        randomPhaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Start()
    {
        if (randomizeColor)
        {
            ApplyRandomColor();
        }

        // Si no se inició externamente por el Spawner, arrancar automáticamente
        if (!isRising)
        {
            StartAnimation(transform.localPosition);
        }
    }

    /// <summary>
    /// Inicia la animación calculando la altura aleatoria y la posición final.
    /// </summary>
    public void StartAnimation(Vector3 spawnGroundPosition, float customTargetHeight = -1f)
    {
        initialBasePos = spawnGroundPosition;
        transform.localPosition = initialBasePos;

        chosenHeight = (customTargetHeight > 0f) ? customTargetHeight : Random.Range(minHeight, maxHeight);

        // Calcular posición final según el eje seleccionado
        Vector3 offset = Vector3.zero;
        switch (movementAxis)
        {
            case MovementAxis.Local_Z_WallUp:
                offset = new Vector3(0, 0, chosenHeight);
                break;
            case MovementAxis.Local_Y_Normal:
                offset = new Vector3(0, chosenHeight, 0);
                break;
            case MovementAxis.World_Y:
                // Convertido a espacio local del padre
                Vector3 worldTarget = transform.TransformPoint(Vector3.zero) + Vector3.up * chosenHeight;
                if (transform.parent != null)
                    targetLocalPos = transform.parent.InverseTransformPoint(worldTarget);
                else
                    targetLocalPos = worldTarget;
                elapsedTime = 0f;
                isRising = true;
                return;
        }

        targetLocalPos = initialBasePos + offset;
        elapsedTime = 0f;
        isRising = true;
    }

    private void Update()
    {
        if (isRising)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / Mathf.Max(0.01f, riseDuration));
            float curveValue = riseCurve != null ? riseCurve.Evaluate(t) : Mathf.SmoothStep(0, 1, t);

            transform.localPosition = Vector3.Lerp(initialBasePos, targetLocalPos, curveValue);

            // Ligera rotación mientras sube
            float spin = Mathf.Sin(t * Mathf.PI) * 10f;
            transform.localRotation = initialLocalRot * Quaternion.Euler(0, spin, 0);

            if (t >= 1.0f)
            {
                isRising = false;
                transform.localPosition = targetLocalPos;
            }
        }
        else if (enableIdleFloat)
        {
            // Efecto suave de flotación y oscilación
            float time = Time.time + randomPhaseOffset;
            float bob = Mathf.Sin(time * bobbingFrequency) * bobbingAmplitude;
            float swayX = Mathf.Sin(time * swayFrequency) * swayMaxAngle;
            float swayZ = Mathf.Cos(time * swayFrequency * 0.8f) * swayMaxAngle;

            Vector3 bobVector = Vector3.zero;
            switch (movementAxis)
            {
                case MovementAxis.Local_Z_WallUp:
                    bobVector = new Vector3(0, 0, bob);
                    break;
                case MovementAxis.Local_Y_Normal:
                case MovementAxis.World_Y:
                    bobVector = new Vector3(0, bob, 0);
                    break;
            }

            transform.localPosition = targetLocalPos + bobVector;
            transform.localRotation = initialLocalRot * Quaternion.Euler(swayX, 0, swayZ);
        }
    }

    /// <summary>
    /// Aplica un color vibrante aleatorio a los renderers del objeto.
    /// </summary>
    private void ApplyRandomColor()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0) return;

        Color randomColor = Color.HSVToRGB(Random.Range(0f, 1f), Random.Range(0.7f, 0.95f), Random.Range(0.85f, 1f));

        foreach (Renderer rend in renderers)
        {
            // Instanciar material para no alterar el asset original compartido
            Material mat = rend.material;
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", randomColor);
            }
            else if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", randomColor);
            }
        }
    }
}
