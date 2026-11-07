using System.Collections.Generic;
using UnityEngine;
using Vuforia;

/// <summary>
/// Controla la aparición, posicionamiento en el entorno AR y ciclo de vida de los globos.
/// Soporta área rectangular y el algoritmo de círculo (Trello: radio X + distancia Y).
/// Garantiza la distancia mínima entre globos, alineación con la gravedad real,
/// gestión de globos restantes y eliminación exclusiva al mostrarse el menú de fin de juego.
/// </summary>
public class TargetPlaneSpawner : MonoBehaviour
{
    public enum AreaShape
    {
        [Tooltip("Plano rectangular de pared delimitado por sus dimensiones y márgenes")]
        RectangularPlane = 0,
        [Tooltip("Círculo de radio X con algoritmo de Trello: rayo desde el origen + distancia Y tras el cruce")]
        Circle_OriginRay = 1,
        [Tooltip("Círculo con distribución uniforme de área")]
        Circle_Uniform = 2,
        [Tooltip("Modo de esquinas (retrocompatibilidad, redirige a RectangularPlane)")]
        CustomCorners = 3
    }

    [Header("--- REFERENCIAS ---")]
    [Tooltip("Target de Vuforia. Si está vacío se busca en este GameObject automáticamente")]
    public ObserverBehaviour imageTargetObserver;

    [Tooltip("Prefab asignado que se generará aleatoriamente (ej. Globo_Explosion)")]
    public GameObject prefabToSpawn;

    [Header("--- PLANO DE LA MARCA ---")]
    [Tooltip("Los globos se organizan paralelos a la imagen respecto a la ARCamera")]
    public bool uprightLayout = true;

    [Tooltip("Ajusta el conjunto al encuadre real de la cámara al generar la ronda")]
    public bool fitLayoutToCamera;

    [Range(.01f, 1)] public float maximumLayoutScale = .15f;
    private float layoutScale = 1;
    private Vector2 layoutShift;

    public float LayoutScale => layoutScale;
    public Quaternion LayoutRotation
    {
        get
        {
            if (!uprightLayout) return transform.rotation;
            Camera camera = ARGameSpace.Instance != null ? ARGameSpace.Instance.CameraAR : Camera.main;
            if (camera == null) camera = Camera.main;
            return ARGameSpace.MarkerPlaneFrame(transform, camera);
        }
    }
    private Vector3 LayoutOrigin => transform.position + LayoutRotation * new Vector3(layoutShift.x, 0, layoutShift.y);
    private Matrix4x4 LayoutMatrix => Matrix4x4.TRS(LayoutOrigin, LayoutRotation, transform.lossyScale * layoutScale);

    [Header("--- DIMENSIONES DEL PLANO / PARED ---")]
    [Tooltip("Forma y algoritmo del área de generación")]
    public AreaShape areaShape = AreaShape.RectangularPlane;

    [Tooltip("Tamaño del plano: X = Ancho horizontal, Y = Alto/Largo vertical")]
    public Vector2 planeSize = new Vector2(2.0f, 1.5f);

    [Tooltip("Radio X del círculo invisible con centro en la marca")]
    public float circleRadius = 1.0f;

    [Tooltip("Distancia Y a partir del punto de cruce del círculo de radio X (tarea de Trello)")]
    public float circleCrossDistanceY = 0.3f;

    [Tooltip("Variación aleatoria (+/-) en la distancia Y")]
    public float distanceYJitter = 0.1f;

    [Tooltip("Desplazamiento del centro del plano con respecto a la marca")]
    public Vector3 planeOffset = Vector3.zero;

    [Tooltip("Margen de seguridad hacia adentro respecto a los bordes")]
    [Range(0f, 1f)]
    public float cornerMargin = 0.15f;

    [Header("--- VISUALIZACIÓN DEL PLANO ---")]
    [Tooltip("Mostrar plano visual semitransparente que represente la pared")]
    public bool showVisualPlane = true;

    [Tooltip("Material para el plano visual (opcional)")]
    public Material planeMaterial;

    [Tooltip("Color del plano visual si no se asigna material")]
    public Color planeColor = new Color(0.15f, 0.55f, 0.95f, 0.25f);

    [Tooltip("Dibujar líneas guía y rayos en la vista de escena")]
    public bool drawRayGizmos = true;

    [Header("--- REGLAS DE GENERACIÓN ALEATORIA ---")]
    [Tooltip("Cantidad de globos a generar en la ronda")]
    [Range(1, 50)]
    public int numberOfObjects = 6;

    [Tooltip("Distancia mínima obligatoria entre globos para evitar solapes")]
    [Range(0.05f, 2f)]
    public float minDistanceBetweenObjects = 0.35f;

    [Tooltip("Intentos máximos de muestreo antes de descartar una posición saturada")]
    public int maxPlacementAttempts = 100;

    [Header("--- ORIENTACIÓN DEL GLOBO (GRAVEDAD REAL) ---")]
    [Tooltip("Garantiza que el nudo del globo apunte siempre hacia el suelo real con la gravedad, inmune a giros del móvil")]
    public bool nudoHaciaAbajoGravedad = true;

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

    [Tooltip("Nivel del suelo relativo calculado en el borde inferior del plano")]
    public bool useBottomCornerAsFloor = true;

    [Tooltip("Offset personalizado de suelo si no se usa el borde inferior")]
    public float customFloorOffset = -0.75f;

    [Header("--- COMPORTAMIENTO VUFORIA ---")]
    [Tooltip("Generar automáticamente al detectar la marca")]
    public bool autoSpawnOnTracking = true;

    [Tooltip("Sólo mostrar cuando el target esté en estado TRACKED")]
    public bool onlyWhenTracked = true;

    [Tooltip("Volver a generar objetos cada vez que se detecte la marca")]
    public bool regenerateOnEachDetection = false;

    [Tooltip("Ocultar los objetos cuando se pierda el seguimiento momentáneamente")]
    public bool hideOnTrackingLost = true;

    [Header("--- MODO DE PRUEBAS (SIN VUFORIA) ---")]
    [Tooltip("Simula detección permanente para probar en Play sin cámara ni imagen")]
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
    private bool hasStarted;

    /// <summary>
    /// Gestión de globos que quedan por explotar en la ronda actual.
    /// </summary>
    public int GlobosRestantes => GetActiveBalloonsCount();
    public int GlobosVivos => GetActiveBalloonsCount();
    public int GlobosTotalesRonda => spawnedInstances.Count;

    private int GetActiveBalloonsCount()
    {
        int count = 0;
        for (int i = 0; i < spawnedInstances.Count; i++)
        {
            GameObject obj = spawnedInstances[i];
            if (obj != null && obj.activeInHierarchy)
            {
                GloboTarget target = obj.GetComponent<GloboTarget>();
                if (target == null || !target.IsPopped) count++;
            }
        }
        return count;
    }

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

        if (GameManagerRA.Instance != null)
            GameManagerRA.Instance.OnPartidaFinalizada += HandlePartidaFinalizada;
    }

    private void OnDisable()
    {
        if (imageTargetObserver != null)
            imageTargetObserver.OnTargetStatusChanged -= HandleTargetStatusChanged;

        if (GameManagerRA.Instance != null)
            GameManagerRA.Instance.OnPartidaFinalizada -= HandlePartidaFinalizada;
    }

    private void HandlePartidaFinalizada(bool victoria, int score)
    {
        // Regla: eliminar los globos restantes únicamente cuando aparece el menú de fin de juego
        EliminarTodosLosGlobos();
    }

    private void Start()
    {
        hasStarted = true;
        if (GameManagerRA.Instance != null)
        {
            GameManagerRA.Instance.OnPartidaFinalizada -= HandlePartidaFinalizada;
            GameManagerRA.Instance.OnPartidaFinalizada += HandlePartidaFinalizada;
        }

        ARGameSpace.GetOrCreateInstance();
        UpdateVisualPlane();

        if (testModeWithoutVuforia)
        {
            OnTargetFound();
        }
        else
        {
            if (spawnedObjectsContainer != null)
                spawnedObjectsContainer.SetActive(false);

            if (planeVisualObject != null)
                planeVisualObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (GameManagerRA.Instance != null)
            GameManagerRA.Instance.OnPartidaFinalizada -= HandlePartidaFinalizada;
    }

    private void OnValidate()
    {
        if (cornerMargin > planeSize.x * 0.45f) cornerMargin = planeSize.x * 0.45f;
        if (cornerMargin > planeSize.y * 0.45f) cornerMargin = planeSize.y * 0.45f;

        if (Application.isPlaying && hasStarted)
        {
            if (testModeWithoutVuforia && !isCurrentlyTracked)
                OnTargetFound();
            else if (!testModeWithoutVuforia && isCurrentlyTracked && imageTargetObserver != null)
                OnTargetLost();

            UpdateVisualPlane();
        }
    }

    private void HandleTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus targetStatus)
    {
        bool tracked = targetStatus.Status == Status.TRACKED ||
                       (!onlyWhenTracked && (targetStatus.Status == Status.EXTENDED_TRACKED ||
                                             targetStatus.Status == Status.LIMITED));

        if (tracked)
        {
            testModeWithoutVuforia = false;
            if (!isCurrentlyTracked) OnTargetFound();
        }
        else if (!tracked && isCurrentlyTracked && !testModeWithoutVuforia)
        {
            OnTargetLost();
        }
    }

    [ContextMenu("Simular: Detectar Marca (Test Sin Vuforia)")]
    public void OnTargetFound()
    {
        isCurrentlyTracked = true;
        EnsureContainers();
        UpdateLayoutPose();

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

        // Al perder tracking solo se ocultan temporalmente, NUNCA se destruyen aquí
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
        UpdateLayoutPose();
    }

    private void LateUpdate()
    {
        UpdateLayoutPose();
    }

    private void UpdateLayoutPose()
    {
        Quaternion rotation = LayoutRotation;
        if (spawnedObjectsContainer != null)
        {
            spawnedObjectsContainer.transform.position = LayoutOrigin;
            spawnedObjectsContainer.transform.rotation = rotation;
            spawnedObjectsContainer.transform.localScale = Vector3.one * layoutScale;
        }
        if (planeVisualObject != null)
        {
            planeVisualObject.transform.position = LayoutMatrix.MultiplyPoint3x4(planeOffset + new Vector3(0, .001f, 0));
            planeVisualObject.transform.rotation = rotation * Quaternion.Euler(90, 0, 0);
        }
    }

    private void FitLayout()
    {
        layoutScale = 1; layoutShift = Vector2.zero;
        if (!fitLayoutToCamera) return;
        Camera camera = ARGameSpace.Instance != null ? ARGameSpace.Instance.CameraAR : Camera.main;
        if (camera == null) camera = Camera.main;
        layoutScale = maximumLayoutScale;
        if (camera != null)
        {
            Quaternion rotation = LayoutRotation;
            var plane = new Plane(rotation * Vector3.up, transform.position);
            Vector2 min = Vector2.one * float.PositiveInfinity, max = Vector2.one * float.NegativeInfinity;
            bool valid = true;

            foreach (var corner in new[] { new Vector2(.12f, .22f), new Vector2(.88f, .22f),
                new Vector2(.88f, .80f), new Vector2(.12f, .80f) })
            {
                Ray ray = camera.ViewportPointToRay(corner);
                if (!plane.Raycast(ray, out float distance)) { valid = false; break; }
                Vector3 local = Quaternion.Inverse(rotation) * (ray.GetPoint(distance) - transform.position);
                min = Vector2.Min(min, new Vector2(local.x, local.z));
                max = Vector2.Max(max, new Vector2(local.x, local.z));
            }
            if (valid)
            {
                Vector2 size = planeSize + new Vector2(.44f, .70f);
                layoutScale = Mathf.Clamp(Mathf.Min(maximumLayoutScale, (max.x - min.x) / size.x,
                    (max.y - min.y) / size.y), .002f, maximumLayoutScale);
                Vector2 half = size * (layoutScale * .5f);
                Vector2 desiredCenter = new Vector2(planeOffset.x, planeOffset.z) * layoutScale;
                Vector2 center = new Vector2(Mathf.Clamp(desiredCenter.x, min.x + half.x, max.x - half.x),
                    Mathf.Clamp(desiredCenter.y, min.y + half.y, max.y - half.y));
                layoutShift = center - desiredCenter;
            }
        }
        ARGameSpace.GetOrCreateInstance().SetGameplayScale(layoutScale);
        UpdateLayoutPose();
    }

    /// <summary>
    /// Genera la cantidad solicitada de globos en el área respetando distancias mínimas.
    /// </summary>
    [ContextMenu("Generar Objetos")]
    public void GenerateObjects()
    {
        var game = GameManagerRA.Instance;
        if (Application.isPlaying && game != null && game.HaIniciado && !game.PartidaEnCurso) return;

        EnsureContainers();
        EliminarTodosLosGlobos();

        spawnedPositions.Clear();
        spawnedCrossPoints.Clear();
        int successfullySpawned = 0;
        FitLayout();

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
                Debug.LogWarning($"[TargetPlaneSpawner] No se pudo encontrar espacio libre para el globo #{i + 1} " +
                                 $"(distancia mínima: {minDistanceBetweenObjects}m). Considera agrandar el plano.");
            }
        }

        Debug.Log($"[TargetPlaneSpawner] Generación completada: {successfullySpawned}/{numberOfObjects} globos colocados.");
    }

    private GameObject SpawnPrefabAt(Vector3 targetPoint, float floorLevel)
    {
        EnsureContainers();

        // Orientación del globo con nudo hacia el suelo según la gravedad real del mundo
        Vector3 wallNormalWorld = LayoutRotation * Vector3.up;
        Vector3 forwardInRoom = Vector3.ProjectOnPlane(wallNormalWorld, Vector3.up);
        if (forwardInRoom.sqrMagnitude < 0.001f)
            forwardInRoom = Vector3.ProjectOnPlane(LayoutRotation * Vector3.forward, Vector3.up);
        if (forwardInRoom.sqrMagnitude < 0.001f)
            forwardInRoom = Vector3.forward;
        forwardInRoom.Normalize();

        Quaternion balloonWorldRot = Quaternion.LookRotation(forwardInRoom, Vector3.up);
        Vector3 localWallUp = Vector3.forward;

        if (prefabToSpawn == null)
        {
#if UNITY_EDITOR
            prefabToSpawn = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Globo_Explosion.prefab");
#endif
            if (prefabToSpawn == null)
            {
                Debug.LogError("[TargetPlaneSpawner] No se ha asignado 'prefabToSpawn'. Asigna el prefab de globo en el Inspector.");
                return null;
            }
        }

        GameObject newObj = Instantiate(prefabToSpawn, spawnedObjectsContainer.transform);
        newObj.name = $"Globo_{spawnedInstances.Count + 1}";
        newObj.transform.rotation = balloonWorldRot;

        // Asegurar componente GloboTarget para colisiones y eventos de explosión
        if (newObj.GetComponent<GloboTarget>() == null)
            newObj.AddComponent<GloboTarget>();

        Collider balloonCol = newObj.GetComponent<Collider>();
        if (balloonCol == null)
        {
            SphereCollider sc = newObj.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.25f;
        }
        else
        {
            balloonCol.isTrigger = true;
        }

        // Posicionar en la superficie del plano con leve separación
        targetPoint.y = planeOffset.y + 0.03f;

        float riseDist = Random.Range(minRiseHeight, maxRiseHeight);
        Vector3 groundSpawnPos = targetPoint - (localWallUp * riseDist);
        if (useBottomCornerAsFloor) groundSpawnPos.z = Mathf.Min(floorLevel, targetPoint.z);
        groundSpawnPos.y = targetPoint.y;

        if (applyRiseAnimation && Application.isPlaying)
        {
            newObj.transform.localPosition = groundSpawnPos;

            BalloonAnimator animator = newObj.GetComponent<BalloonAnimator>();
            if (animator == null) animator = newObj.AddComponent<BalloonAnimator>();

            animator.alignKnotWithGravity = nudoHaciaAbajoGravedad;
            animator.movementAxis = riseAxis;
            animator.minHeight = minRiseHeight;
            animator.maxHeight = maxRiseHeight;
            animator.riseDuration = riseDuration;

            animator.StartAnimationToTarget(groundSpawnPos, targetPoint, balloonWorldRot, localWallUp);
        }
        else
        {
            newObj.transform.localPosition = targetPoint;
        }

        return newObj;
    }

    /// <summary>
    /// Elimina todos los globos instanciados. Se ejecuta al aparecer el menú de fin de juego o al reiniciar la ronda.
    /// </summary>
    [ContextMenu("Eliminar Todos los Globos")]
    public void EliminarTodosLosGlobos()
    {
        for (int i = 0; i < spawnedInstances.Count; i++)
        {
            GameObject obj = spawnedInstances[i];
            if (obj != null)
            {
                if (Application.isPlaying) Destroy(obj);
                else DestroyImmediate(obj);
            }
        }
        spawnedInstances.Clear();
        spawnedPositions.Clear();
        spawnedCrossPoints.Clear();
    }

    public void ClearSpawnedObjects() => EliminarTodosLosGlobos();

    /// <summary>
    /// Restablece los globos al reiniciar una ronda sin depender de volver a detectar la marca.
    /// </summary>
    [ContextMenu("Reiniciar Ronda")]
    public void ReiniciarRonda()
    {
        EliminarTodosLosGlobos();
        hasGeneratedOnce = false;
        if (isCurrentlyTracked || testModeWithoutVuforia)
        {
            GenerateObjects();
            hasGeneratedOnce = true;
        }
    }

    /// <summary>
    /// Retorna un punto aleatorio dentro del área configurada respetando los márgenes.
    /// </summary>
    public Vector3 GetRandomPointInArea()
    {
        switch (areaShape)
        {
            case AreaShape.Circle_OriginRay:
            {
                // Algoritmo Trello:
                // 1. Círculo invisible de radio X centrado en la marca (planeOffset).
                // 2. Dirección aleatoria desde el centro.
                float angle = Random.Range(0f, Mathf.PI * 2f);
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

                // 3. Punto de cruce en el perímetro del círculo de radio X:
                Vector3 crossPoint = planeOffset + (dir * Mathf.Max(0.01f, circleRadius));
                lastCalculatedCrossPoint = crossPoint;

                // 4. A distancia Y tras el cruce se escoge el punto final:
                float actualDistY = Mathf.Max(0f, circleCrossDistanceY + Random.Range(-distanceYJitter, distanceYJitter));
                return crossPoint + (dir * actualDistY);
            }

            case AreaShape.Circle_Uniform:
            {
                float safeRadius = Mathf.Max(0.01f, circleRadius - cornerMargin);
                float r = Mathf.Sqrt(Random.Range(0f, 1f)) * safeRadius;
                float angle = Random.Range(0f, Mathf.PI * 2f);

                lastCalculatedCrossPoint = planeOffset + new Vector3(Mathf.Cos(angle) * circleRadius, 0, Mathf.Sin(angle) * circleRadius);
                return planeOffset + new Vector3(r * Mathf.Cos(angle), 0, r * Mathf.Sin(angle));
            }

            case AreaShape.RectangularPlane:
            case AreaShape.CustomCorners:
            default:
            {
                float safeHalfWidth = Mathf.Max(0.01f, (planeSize.x * 0.5f) - cornerMargin);
                float safeHalfHeight = Mathf.Max(0.01f, (planeSize.y * 0.5f) - cornerMargin);

                float randomX = Random.Range(-safeHalfWidth, safeHalfWidth);
                float randomZ = Random.Range(-safeHalfHeight, safeHalfHeight);

                lastCalculatedCrossPoint = planeOffset;
                return planeOffset + new Vector3(randomX, 0, randomZ);
            }
        }
    }

    private bool IsDistanceValid(Vector3 candidate, List<Vector3> existingPoints, float minDist)
    {
        float sqrMinDist = minDist * minDist;
        for (int i = 0; i < existingPoints.Count; i++)
        {
            if ((candidate - existingPoints[i]).sqrMagnitude < sqrMinDist)
                return false;
        }
        return true;
    }

    [ContextMenu("Actualizar Plano Visual")]
    public void UpdateVisualPlane()
    {
        if (!showVisualPlane)
        {
            if (planeVisualObject != null) planeVisualObject.SetActive(false);
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

            Collider col = planeVisualObject.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }
        }

        planeVisualObject.transform.localPosition = planeOffset + new Vector3(0, 0.001f, 0);
        planeVisualObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        planeVisualObject.transform.localScale = new Vector3(planeSize.x, planeSize.y, 1f);

        MeshRenderer meshRenderer = planeVisualObject.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            if (planeMaterial != null)
            {
                meshRenderer.sharedMaterial = planeMaterial;
            }
            else
            {
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
        UpdateLayoutPose();
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
        Gizmos.matrix = LayoutMatrix;

        if (areaShape == AreaShape.RectangularPlane || areaShape == AreaShape.CustomCorners)
        {
            Vector3 center = planeOffset;
            Gizmos.color = isSelected ? new Color(0f, 0.8f, 1f, 0.9f) : new Color(0f, 0.5f, 0.8f, 0.4f);
            Gizmos.DrawWireCube(center, new Vector3(planeSize.x, 0.01f, planeSize.y));

            float safeW = Mathf.Max(0.01f, planeSize.x - cornerMargin * 2f);
            float safeH = Mathf.Max(0.01f, planeSize.y - cornerMargin * 2f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(center, new Vector3(safeW, 0.01f, safeH));
        }
        else
        {
            Gizmos.color = isSelected ? Color.cyan : new Color(0f, 0.8f, 1f, 0.4f);
            DrawWireCircle(planeOffset, circleRadius);

            if (areaShape == AreaShape.Circle_OriginRay)
            {
                Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.4f);
                DrawWireCircle(planeOffset, circleRadius + circleCrossDistanceY);
            }
            else
            {
                Gizmos.color = Color.yellow;
                DrawWireCircle(planeOffset, Mathf.Max(0.01f, circleRadius - cornerMargin));
            }
        }

        if (drawRayGizmos && spawnedPositions.Count > 0)
        {
            for (int i = 0; i < spawnedPositions.Count; i++)
            {
                Vector3 targetPos = spawnedPositions[i];
                if (areaShape == AreaShape.Circle_OriginRay && i < spawnedCrossPoints.Count)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(planeOffset, spawnedCrossPoints[i]);
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(spawnedCrossPoints[i], targetPos);
                }
                else
                {
                    Gizmos.color = new Color(0f, 0.9f, 0.9f, 0.4f);
                    Gizmos.DrawLine(planeOffset, targetPos);
                }
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
