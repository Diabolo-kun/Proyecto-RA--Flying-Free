using System.Collections.Generic;
using UnityEngine;
using Vuforia;

/// <summary>
/// Genera un plano configurable en el Inspector asociado a un Image Target de Vuforia.
/// Genera prefabs aleatoriamente dentro del plano respetando:
/// - Las esquinas y márgenes de la pared/plano.
/// - La distancia mínima entre objetos para evitar superposiciones.
/// - Animación de subida desde el suelo hasta altura aleatoria.
/// </summary>
public class TargetPlaneSpawner : MonoBehaviour
{
    public enum AreaShape
    {
        [Tooltip("Plano rectangular de pared delimitado por sus 4 esquinas (traslado a pared)")]
        RectangularPlane = 0,
        [Tooltip("Círculo de radio X con algoritmo de Trello: rayo desde el origen + distancia Y tras el cruce")]
        Circle_OriginRay = 1,
        [Tooltip("Círculo con distribución uniforme de área")]
        Circle_Uniform = 2,
        [Tooltip("Pared delimitada por 4 Transforms manuales de esquinas")]
        CustomCorners = 3
    }

    [Header("--- REFERENCIAS ---")]
    [Tooltip("Target de Vuforia. Si está vacío se busca en este GameObject automáticamente")]
    public ObserverBehaviour imageTargetObserver;

    [Tooltip("Prefab asignado que se generará aleatoriamente (ej. Globo)")]
    public GameObject prefabToSpawn;

    [Header("--- DIMENSIONES DEL PLANO / PARED ---")]
    [Tooltip("Forma y algoritmo del área de generación")]
    public AreaShape areaShape = AreaShape.RectangularPlane;

    [Tooltip("Tamaño del plano: X = Ancho horizontal, Y = Alto/Largo vertical en el plano")]
    public Vector2 planeSize = new Vector2(2.0f, 1.5f);

    [Tooltip("Radio X del círculo invisible con centro en la marca")]
    public float circleRadius = 1.0f;

    [Tooltip("Distancia Y a partir del punto de cruce del círculo de radio X (tarea de Trello)")]
    public float circleCrossDistanceY = 0.3f;

    [Tooltip("Variación aleatoria (+/-) en la distancia Y")]
    public float distanceYJitter = 0.1f;

    [Tooltip("Desplazamiento del centro del plano con respecto a la marca")]
    public Vector3 planeOffset = Vector3.zero;

    [Tooltip("Margen de seguridad hacia adentro respecto a las esquinas y bordes de la pared")]
    [Range(0f, 1f)]
    public float cornerMargin = 0.15f;

    [Header("--- ESQUINAS FÍSICAS DE LA PARED (OPCIONAL) ---")]
    [Tooltip("Si se asignan, definen las 4 esquinas físicas de la pared para que no dé errores de salida")]
    public Transform cornerBottomLeft;
    public Transform cornerBottomRight;
    public Transform cornerTopRight;
    public Transform cornerTopLeft;

    [Header("--- VISUALIZACIÓN DEL PLANO ---")]
    [Tooltip("Crear/mostrar un plano visual que represente la pared/superficie")]
    public bool showVisualPlane = true;

    [Tooltip("Material para el plano visual (opcional)")]
    public Material planeMaterial;

    [Tooltip("Color del plano visual si no se asigna material específico")]
    public Color planeColor = new Color(0.15f, 0.55f, 0.95f, 0.25f);

    [Tooltip("Dibujar líneas de rayos desde el origen hasta los globos en el Scene view")]
    public bool drawRayGizmos = true;

    [Header("--- REGLAS DE GENERACIÓN ALEATORIA ---")]
    [Tooltip("Cantidad de prefabs a generar")]
    [Range(1, 50)]
    public int numberOfObjects = 6;

    [Tooltip("Distancia mínima obligatoria entre cada prefab generado (para que no se solapen)")]
    [Range(0.05f, 2f)]
    public float minDistanceBetweenObjects = 0.35f;

    [Tooltip("Intentos máximos de muestreo por objeto antes de descartarlo si el plano está saturado")]
    public int maxPlacementAttempts = 100;

    [Header("--- ANIMACIÓN DE SUBIDA (GLOBO) ---")]
    [Tooltip("Aplica la animación desde el suelo hasta altura aleatoria")]
    public bool applyRiseAnimation = true;

    [Tooltip("Eje en el que suben los globos")]
    public BalloonAnimator.MovementAxis riseAxis = BalloonAnimator.MovementAxis.Local_Z_WallUp;

    [Tooltip("Altura mínima a la que sube el globo")]
    public float minRiseHeight = 0.4f;

    [Tooltip("Altura máxima a la que sube el globo")]
    public float maxRiseHeight = 1.2f;

    [Tooltip("Tiempo en segundos que tarda en subir cada globo")]
    public float riseDuration = 2.5f;

    [Tooltip("Nivel del 'suelo' relativo. Por defecto se calcula en el borde inferior del plano")]
    public bool useBottomCornerAsFloor = true;

    [Tooltip("Offset personalizado de suelo si no se usa el borde inferior")]
    public float customFloorOffset = -0.75f;

    [Header("--- COMPORTAMIENTO VUFORIA ---")]
    [Tooltip("Generar automáticamente al detectar la marca")]
    public bool autoSpawnOnTracking = true;

    [Tooltip("Si está activo, sólo se muestran cuando el target esté en estado TRACKED")]
    public bool onlyWhenTracked = true;

    [Tooltip("Volver a generar objetos cada vez que se detecte de nuevo la marca")]
    public bool regenerateOnEachDetection = false;

    [Tooltip("Ocultar los objetos cuando se pierda el seguimiento")]
    public bool hideOnTrackingLost = true;

    [Header("--- MODO DE PRUEBAS (SIN VUFORIA) ---")]
    [Tooltip("Si está activo, simula que la marca está detectada permanentemente para probar en Play sin cámara ni Vuforia")]
    public bool testModeWithoutVuforia = true;

    // Referencias internas
    private GameObject planeVisualObject;
    private GameObject spawnedObjectsContainer;
    private readonly List<GameObject> spawnedInstances = new List<GameObject>();
    private readonly List<Vector3> spawnedPositions = new List<Vector3>();
    private readonly List<Vector3> spawnedCrossPoints = new List<Vector3>();
    private Vector3 lastCalculatedCrossPoint = Vector3.zero;
    private bool isCurrentlyTracked = false;
    private bool hasGeneratedOnce = false;

    private void Awake()
    {
        if (imageTargetObserver == null)
            imageTargetObserver = GetComponent<ObserverBehaviour>();

        EnsureContainers();
    }

    private void OnEnable()
    {
        if (imageTargetObserver != null)
            imageTargetObserver.OnTargetStatusChanged += HandleTargetStatusChanged;
    }

    private void OnDisable()
    {
        if (imageTargetObserver != null)
            imageTargetObserver.OnTargetStatusChanged -= HandleTargetStatusChanged;
    }

    private void Start()
    {
        UpdateVisualPlane();

        // En modo pruebas, simular detección directa al iniciar
        if (testModeWithoutVuforia)
        {
            OnTargetFound();
        }
        else
        {
            // Ocultar al inicio hasta que Vuforia detecte el ImageTarget
            if (spawnedObjectsContainer != null)
                spawnedObjectsContainer.SetActive(false);

            if (planeVisualObject != null)
                planeVisualObject.SetActive(false);
        }
    }

    private void OnValidate()
    {
        // Actualización dinámica en el Inspector de Unity
        if (cornerMargin > planeSize.x * 0.45f) cornerMargin = planeSize.x * 0.45f;
        if (cornerMargin > planeSize.y * 0.45f) cornerMargin = planeSize.y * 0.45f;

        if (Application.isPlaying)
        {
            if (testModeWithoutVuforia && !isCurrentlyTracked)
            {
                OnTargetFound();
            }
            else if (!testModeWithoutVuforia && isCurrentlyTracked && imageTargetObserver != null)
            {
                OnTargetLost();
            }

            UpdateVisualPlane();
        }
    }

    private void HandleTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus targetStatus)
    {
        if (testModeWithoutVuforia) return;

        bool tracked = targetStatus.Status == Status.TRACKED ||
                       (!onlyWhenTracked && (targetStatus.Status == Status.EXTENDED_TRACKED ||
                                             targetStatus.Status == Status.LIMITED));

        if (tracked && !isCurrentlyTracked)
        {
            OnTargetFound();
        }
        else if (!tracked && isCurrentlyTracked)
        {
            OnTargetLost();
        }
    }

    [ContextMenu("Simular: Detectar Marca (Test Sin Vuforia)")]
    public void OnTargetFound()
    {
        isCurrentlyTracked = true;

        if (planeVisualObject != null && showVisualPlane)
            planeVisualObject.SetActive(true);

        if (spawnedObjectsContainer != null)
            spawnedObjectsContainer.SetActive(true);

        if (autoSpawnOnTracking)
        {
            if (!hasGeneratedOnce || regenerateOnEachDetection)
            {
                GenerateObjects();
                hasGeneratedOnce = true;
            }
        }
    }

    [ContextMenu("Simular: Perder Marca (Test Sin Vuforia)")]
    public void OnTargetLost()
    {
        isCurrentlyTracked = false;

        if (hideOnTrackingLost)
        {
            if (planeVisualObject != null)
                planeVisualObject.SetActive(false);

            if (spawnedObjectsContainer != null)
                spawnedObjectsContainer.SetActive(false);
        }
    }

    private void EnsureContainers()
    {
        if (spawnedObjectsContainer == null)
        {
            Transform existing = transform.Find("SpawnedObjectsContainer");
            if (existing != null)
            {
                spawnedObjectsContainer = existing.gameObject;
            }
            else
            {
                spawnedObjectsContainer = new GameObject("SpawnedObjectsContainer");
                spawnedObjectsContainer.transform.SetParent(transform, false);
            }
        }
    }

    /// <summary>
    /// Genera la cantidad de objetos solicitada dentro del plano respetando esquinas y distancia mínima.
    /// </summary>
    [ContextMenu("Generar Objetos")]
    public void GenerateObjects()
    {
        EnsureContainers();
        ClearSpawnedObjects();

        spawnedPositions.Clear();
        spawnedCrossPoints.Clear();
        int successfullySpawned = 0;

        // Calcular el nivel del suelo donde empezará la animación
        float floorLevel = useBottomCornerAsFloor ? (planeOffset.z - (planeSize.y * 0.5f)) : customFloorOffset;

        for (int i = 0; i < numberOfObjects; i++)
        {
            bool foundValidPoint = false;
            Vector3 candidatePoint = Vector3.zero;
            Vector3 candidateCrossPoint = Vector3.zero;

            for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
            {
                candidatePoint = GetRandomPointInArea();
                candidateCrossPoint = lastCalculatedCrossPoint;

                if (IsDistanceValid(candidatePoint, spawnedPositions, minDistanceBetweenObjects))
                {
                    foundValidPoint = true;
                    break;
                }
            }

            if (foundValidPoint)
            {
                spawnedPositions.Add(candidatePoint);
                spawnedCrossPoints.Add(candidateCrossPoint);
                GameObject instance = SpawnPrefabAt(candidatePoint, floorLevel);
                if (instance != null)
                {
                    spawnedInstances.Add(instance);
                    successfullySpawned++;
                }
            }
            else
            {
                Debug.LogWarning($"[TargetPlaneSpawner] No se pudo encontrar espacio libre para el objeto #{i + 1} " +
                                 $"(distancia mínima requerida: {minDistanceBetweenObjects}m). Considera agrandar el plano o reducir la distancia.");
            }
        }

        Debug.Log($"[TargetPlaneSpawner] Generación completada: {successfullySpawned}/{numberOfObjects} objetos colocados con éxito.");
    }

    /// <summary>
    /// Instancia el prefab o crea uno de prueba si no está asignado.
    /// </summary>
    private GameObject SpawnPrefabAt(Vector3 targetPoint, float floorLevel)
    {
        GameObject newObj = null;

        if (prefabToSpawn != null)
        {
            newObj = Instantiate(prefabToSpawn, spawnedObjectsContainer.transform);
        }
        else
        {
            // Fallback: crea un globo estético procedural con esfera y nudo
            newObj = CreateFallbackBalloonObject();
            newObj.transform.SetParent(spawnedObjectsContainer.transform, false);
        }

        newObj.name = $"Globo_{spawnedInstances.Count + 1}";

        // Calcular posición inicial desde el suelo
        Vector3 groundSpawnPos = targetPoint;
        switch (riseAxis)
        {
            case BalloonAnimator.MovementAxis.Local_Z_WallUp:
                groundSpawnPos.z = floorLevel;
                break;
            case BalloonAnimator.MovementAxis.Local_Y_Normal:
                groundSpawnPos.y = floorLevel;
                break;
            case BalloonAnimator.MovementAxis.World_Y:
                groundSpawnPos = targetPoint;
                break;
        }

        if (applyRiseAnimation && Application.isPlaying)
        {
            newObj.transform.localPosition = groundSpawnPos;

            BalloonAnimator animator = newObj.GetComponent<BalloonAnimator>();
            if (animator == null)
            {
                animator = newObj.AddComponent<BalloonAnimator>();
            }

            animator.movementAxis = riseAxis;
            animator.minHeight = minRiseHeight;
            animator.maxHeight = maxRiseHeight;
            animator.riseDuration = riseDuration;

            float targetHeight = (riseAxis == BalloonAnimator.MovementAxis.Local_Z_WallUp) ? 
                                 Mathf.Abs(targetPoint.z - floorLevel) : 
                                 Random.Range(minRiseHeight, maxRiseHeight);

            animator.StartAnimation(groundSpawnPos, targetHeight);
        }
        else
        {
            newObj.transform.localPosition = targetPoint;
        }

        return newObj;
    }

    /// <summary>
    /// Elimina todos los objetos instanciados actualmente.
    /// </summary>
    [ContextMenu("Limpiar Objetos")]
    public void ClearSpawnedObjects()
    {
        foreach (GameObject obj in spawnedInstances)
        {
            if (obj != null)
            {
                if (Application.isPlaying)
                    Destroy(obj);
                else
                    DestroyImmediate(obj);
            }
        }
        spawnedInstances.Clear();
        spawnedPositions.Clear();
        spawnedCrossPoints.Clear();
    }

    /// <summary>
    /// Retorna un punto aleatorio dentro del área configurada respetando esquinas, distancias y el modo de área.
    /// </summary>
    public Vector3 GetRandomPointInArea()
    {
        switch (areaShape)
        {
            case AreaShape.Circle_OriginRay:
            {
                // Algoritmo Trello:
                // 1. Círculo invisible de radio X centrado en la marca (planeOffset).
                // 2. Escoger dirección aleatoria (línea desde el origen hacia ese punto).
                float angle = Random.Range(0f, Mathf.PI * 2f);
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

                // 3. Punto de cruce en el perímetro del círculo de radio X:
                Vector3 crossPoint = planeOffset + (dir * Mathf.Max(0.01f, circleRadius));
                lastCalculatedCrossPoint = crossPoint;

                // 4. "y un poco más, en el punto de cruce, a Y distancia se elige un punto aleatorio":
                float actualDistY = Mathf.Max(0f, circleCrossDistanceY + Random.Range(-distanceYJitter, distanceYJitter));
                Vector3 finalPoint = crossPoint + (dir * actualDistY);

                return finalPoint;
            }

            case AreaShape.Circle_Uniform:
            {
                // Distribución homogénea en círculo
                float safeRadius = Mathf.Max(0.01f, circleRadius - cornerMargin);
                float r = Mathf.Sqrt(Random.Range(0f, 1f)) * safeRadius;
                float angle = Random.Range(0f, Mathf.PI * 2f);

                float randomX = r * Mathf.Cos(angle);
                float randomZ = r * Mathf.Sin(angle);

                lastCalculatedCrossPoint = planeOffset + new Vector3(Mathf.Cos(angle) * circleRadius, 0, Mathf.Sin(angle) * circleRadius);
                return planeOffset + new Vector3(randomX, 0, randomZ);
            }

            case AreaShape.CustomCorners:
            {
                // Si hay 4 esquinas asignadas manualmente, interpolar bilinealmente
                if (cornerBottomLeft != null && cornerBottomRight != null && cornerTopRight != null && cornerTopLeft != null)
                {
                    Vector3 bl = transform.InverseTransformPoint(cornerBottomLeft.position);
                    Vector3 br = transform.InverseTransformPoint(cornerBottomRight.position);
                    Vector3 tr = transform.InverseTransformPoint(cornerTopRight.position);
                    Vector3 tl = transform.InverseTransformPoint(cornerTopLeft.position);

                    float u = Random.Range(cornerMargin, 1f - cornerMargin);
                    float v = Random.Range(cornerMargin, 1f - cornerMargin);

                    Vector3 bottom = Vector3.Lerp(bl, br, u);
                    Vector3 top = Vector3.Lerp(tl, tr, u);
                    Vector3 point = Vector3.Lerp(bottom, top, v);

                    lastCalculatedCrossPoint = planeOffset;
                    return point;
                }
                // Si no están asignadas las 4, fallback al plano rectangular
                goto default;
            }

            case AreaShape.RectangularPlane:
            default:
            {
                // Traslado a la pared: rectángulo delimitado por las esquinas con margen de seguridad
                float safeHalfWidth = Mathf.Max(0.01f, (planeSize.x * 0.5f) - cornerMargin);
                float safeHalfHeight = Mathf.Max(0.01f, (planeSize.y * 0.5f) - cornerMargin);

                float randomX = Random.Range(-safeHalfWidth, safeHalfWidth);
                float randomZ = Random.Range(-safeHalfHeight, safeHalfHeight);

                Vector3 target = planeOffset + new Vector3(randomX, 0, randomZ);
                lastCalculatedCrossPoint = planeOffset;
                return target;
            }
        }
    }

    /// <summary>
    /// Valida que el punto candidato esté al menos a 'minDist' de todos los puntos ya aceptados.
    /// </summary>
    private bool IsDistanceValid(Vector3 candidate, List<Vector3> existingPoints, float minDist)
    {
        float sqrMinDist = minDist * minDist;
        for (int i = 0; i < existingPoints.Count; i++)
        {
            if ((candidate - existingPoints[i]).sqrMagnitude < sqrMinDist)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Calcula las 4 esquinas del plano en coordenadas locales del Image Target.
    /// Si se asignaron Transforms de esquinas, usa sus posiciones exactas.
    /// </summary>
    public Vector3[] GetPlaneCorners(bool withMargin = false)
    {
        if (areaShape == AreaShape.CustomCorners &&
            cornerBottomLeft != null && cornerBottomRight != null && cornerTopRight != null && cornerTopLeft != null)
        {
            Vector3 bl = transform.InverseTransformPoint(cornerBottomLeft.position);
            Vector3 br = transform.InverseTransformPoint(cornerBottomRight.position);
            Vector3 tr = transform.InverseTransformPoint(cornerTopRight.position);
            Vector3 tl = transform.InverseTransformPoint(cornerTopLeft.position);

            if (withMargin)
            {
                Vector3 center = (bl + br + tr + tl) * 0.25f;
                float shrink = Mathf.Clamp01(1f - (cornerMargin * 0.5f));
                bl = Vector3.Lerp(center, bl, shrink);
                br = Vector3.Lerp(center, br, shrink);
                tr = Vector3.Lerp(center, tr, shrink);
                tl = Vector3.Lerp(center, tl, shrink);
            }

            return new Vector3[4] { bl, br, tr, tl };
        }

        float margin = withMargin ? cornerMargin : 0f;
        float halfW = Mathf.Max(0.01f, (planeSize.x * 0.5f) - margin);
        float halfH = Mathf.Max(0.01f, (planeSize.y * 0.5f) - margin);

        return new Vector3[4]
        {
            planeOffset + new Vector3(-halfW, 0, -halfH), // 0: Esquina Inferior-Izquierda
            planeOffset + new Vector3( halfW, 0, -halfH), // 1: Esquina Inferior-Derecha
            planeOffset + new Vector3( halfW, 0,  halfH), // 2: Esquina Superior-Derecha
            planeOffset + new Vector3(-halfW, 0,  halfH)  // 3: Esquina Superior-Izquierda
        };
    }

    /// <summary>
    /// Crea 4 GameObjects hijos para manipular visualmente las esquinas de la pared en la Scene view.
    /// </summary>
    [ContextMenu("Crear / Calibrar Esquinas Físicas en la Escena")]
    public void AutoSetupCornerObjects()
    {
        Vector3[] corners = GetPlaneCorners(false);
        string[] names = new string[] { "Esquina_InferiorIzquierda", "Esquina_InferiorDerecha", "Esquina_SuperiorDerecha", "Esquina_SuperiorIzquierda" };

        Transform cornersParent = transform.Find("EsquinasPared");
        if (cornersParent == null)
        {
            GameObject parentObj = new GameObject("EsquinasPared");
            cornersParent = parentObj.transform;
            cornersParent.SetParent(transform, false);
        }

        Transform[] cornerRefs = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            Transform existing = cornersParent.Find(names[i]);
            if (existing == null)
            {
                GameObject cObj = new GameObject(names[i]);
                cObj.transform.SetParent(cornersParent, false);
                cObj.transform.localPosition = corners[i];
                cornerRefs[i] = cObj.transform;
            }
            else
            {
                cornerRefs[i] = existing;
            }
        }

        cornerBottomLeft = cornerRefs[0];
        cornerBottomRight = cornerRefs[1];
        cornerTopRight = cornerRefs[2];
        cornerTopLeft = cornerRefs[3];
        areaShape = AreaShape.CustomCorners;

        Debug.Log("[TargetPlaneSpawner] Esquinas de la pared generadas y asignadas con éxito.");
    }

    /// <summary>
    /// Crea o actualiza el plano visual para que se adapte exactamente a las dimensiones en el Inspector.
    /// </summary>
    [ContextMenu("Actualizar Plano Visual")]
    public void UpdateVisualPlane()
    {
        if (!showVisualPlane)
        {
            if (planeVisualObject != null)
                planeVisualObject.SetActive(false);
            return;
        }

        Transform existing = transform.Find("PlaneVisual");
        if (existing != null)
        {
            planeVisualObject = existing.gameObject;
        }
        else
        {
            planeVisualObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            planeVisualObject.name = "PlaneVisual";
            planeVisualObject.transform.SetParent(transform, false);

            // Eliminar collider para que no interfiera con rayos o toques
            Collider col = planeVisualObject.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }
        }

        // Posicionar y escalar (un Quad en Unity mide 1x1 en X-Y, rotamos para alinearlo con el target local X-Z)
        planeVisualObject.transform.localPosition = planeOffset + new Vector3(0, 0.001f, 0); // ligeramente elevado para evitar Z-fighting
        planeVisualObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        planeVisualObject.transform.localScale = new Vector3(planeSize.x, planeSize.y, 1f);

        // Configurar Material
        MeshRenderer meshRenderer = planeVisualObject.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            if (planeMaterial != null)
            {
                meshRenderer.sharedMaterial = planeMaterial;
            }
            else
            {
                // Material por defecto elegante y semitransparente
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Standard");

                Material tempMat = new Material(shader);
                if (tempMat.HasProperty("_BaseColor")) tempMat.SetColor("_BaseColor", planeColor);
                else if (tempMat.HasProperty("_Color")) tempMat.SetColor("_Color", planeColor);

                meshRenderer.sharedMaterial = tempMat;
            }
        }

        planeVisualObject.SetActive(showVisualPlane && (!Application.isPlaying || isCurrentlyTracked));
    }

    /// <summary>
    /// Genera un modelo de globo 3D procedural listo para usarse si no hay prefab asignado.
    /// </summary>
    private GameObject CreateFallbackBalloonObject()
    {
        GameObject balloonRoot = new GameObject("GloboProcedural");

        // Cuerpo del globo (esfera ligeramente estirada)
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "Body";
        body.transform.SetParent(balloonRoot.transform, false);
        body.transform.localScale = new Vector3(0.2f, 0.26f, 0.2f);
        body.transform.localPosition = new Vector3(0, 0.13f, 0);

        // Nudo del globo (pequeño cono/cilindro abajo)
        GameObject knot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        knot.name = "Knot";
        knot.transform.SetParent(balloonRoot.transform, false);
        knot.transform.localScale = new Vector3(0.04f, 0.02f, 0.04f);
        knot.transform.localPosition = new Vector3(0, -0.01f, 0);

        // Quitar colliders por defecto del fallback
        Collider c1 = body.GetComponent<Collider>();
        if (c1 != null) { if (Application.isPlaying) Destroy(c1); else DestroyImmediate(c1); }
        Collider c2 = knot.GetComponent<Collider>();
        if (c2 != null) { if (Application.isPlaying) Destroy(c2); else DestroyImmediate(c2); }

        // Agregar SphereCollider en la raíz para posibles toques/interacciones
        SphereCollider rootCol = balloonRoot.AddComponent<SphereCollider>();
        rootCol.center = new Vector3(0, 0.13f, 0);
        rootCol.radius = 0.15f;

        return balloonRoot;
    }

    private void OnDrawGizmosSelected()
    {
        DrawGizmoVisuals(true);
    }

    private void OnDrawGizmos()
    {
        DrawGizmoVisuals(false);
    }

    private void DrawGizmoVisuals(bool isSelected)
    {
        Gizmos.matrix = transform.localToWorldMatrix;

        if (areaShape == AreaShape.RectangularPlane || areaShape == AreaShape.CustomCorners)
        {
            // Dibujar el plano completo (borde exterior)
            Vector3[] outerCorners = GetPlaneCorners(false);
            Gizmos.color = isSelected ? new Color(0f, 0.8f, 1f, 0.9f) : new Color(0f, 0.5f, 0.8f, 0.4f);
            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(outerCorners[i], outerCorners[(i + 1) % 4]);
            }

            // Dibujar esferas en las 4 esquinas de la pared
            Gizmos.color = Color.magenta;
            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawWireSphere(outerCorners[i], 0.04f);
            }

            // Dibujar el área segura interna (respetando el cornerMargin)
            Vector3[] safeCorners = GetPlaneCorners(true);
            Gizmos.color = Color.yellow;
            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(safeCorners[i], safeCorners[(i + 1) % 4]);
            }
        }
        else
        {
            // Círculo invisible de radio X
            Gizmos.color = isSelected ? Color.cyan : new Color(0f, 0.8f, 1f, 0.4f);
            DrawWireCircle(planeOffset, circleRadius);

            if (areaShape == AreaShape.Circle_OriginRay)
            {
                // Círculo exterior a distancia Y
                Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.35f);
                DrawWireCircle(planeOffset, circleRadius + circleCrossDistanceY);
            }
            else
            {
                Gizmos.color = Color.yellow;
                DrawWireCircle(planeOffset, Mathf.Max(0.01f, circleRadius - cornerMargin));
            }
        }

        // Dibujar rayos desde el origen hasta los globos generados
        if (drawRayGizmos && spawnedPositions != null && spawnedPositions.Count > 0)
        {
            for (int i = 0; i < spawnedPositions.Count; i++)
            {
                Vector3 targetPos = spawnedPositions[i];

                if (areaShape == AreaShape.Circle_OriginRay && i < spawnedCrossPoints.Count)
                {
                    Vector3 crossPos = spawnedCrossPoints[i];
                    // Línea desde el origen hasta el círculo de radio X (Cyan)
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(planeOffset, crossPos);

                    // Punto de cruce en el círculo (Esfera amarilla)
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(crossPos, 0.025f);

                    // Línea desde el cruce hasta el punto final a distancia Y (Verde)
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(crossPos, targetPos);
                }
                else
                {
                    // Línea directa desde el origen
                    Gizmos.color = new Color(0f, 0.9f, 0.9f, 0.4f);
                    Gizmos.DrawLine(planeOffset, targetPos);
                }

                // Radio de distancia mínima alrededor de cada globo
                Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.5f);
                Gizmos.DrawWireSphere(targetPos, minDistanceBetweenObjects * 0.5f);
            }
        }
    }

    private void DrawWireCircle(Vector3 center, float radius, int segments = 32)
    {
        float angleStep = (Mathf.PI * 2f) / segments;
        Vector3 prevPoint = center + new Vector3(Mathf.Cos(0) * radius, 0, Mathf.Sin(0) * radius);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }
}
