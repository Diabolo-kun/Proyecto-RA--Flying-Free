using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Banco de pruebas interactivo para demostrar y validar las 3 animaciones/interacciones:
/// 1. Avión choca con Globo -> Globo reproduce animación de estallido (Globo_Explosion_Explotar_Globo), suma +1 al marcador y avión desaparece.
/// 2. Avión embiste al Pájaro (choque lateral/frontal) -> Avión explota en el aire (Explosion_Choque).
/// 3. Pájaro intercepta al Avión de frente con el pico -> Pájaro atrapa el avión con el pico/garra y se lo lleva volando hacia arriba.
/// </summary>
public class AnimacionesTestBench : MonoBehaviour
{
    [Header("Prefabs Requeridos")]
    [Tooltip("Prefab del avión de papel (Assets/Prefabs/Avion_Estandar.prefab)")]
    public GameObject planePrefab;

    [Tooltip("Prefab del pájaro (Assets/Prefabs/Pajaro_FlyingFree.prefab)")]
    public GameObject birdPrefab;

    [Tooltip("Prefab del globo con animación de explosión (Assets/Prefabs/Globo_Explosion.prefab)")]
    public GameObject balloonPrefab;

    [Tooltip("Prefab de la explosión del avión (Assets/Prefabs/Explosion_Choque.prefab)")]
    public GameObject explosionPrefab;

    [Header("Configuración de Pistas (Posiciones)")]
    [SerializeField] private float laneSpacing = 3.5f;
    [SerializeField] private float targetDistanceZ = 4.0f;
    [SerializeField] private float planeFlightSpeed = 2.4f;

    [Header("Controles UI")]
    [Tooltip("Botón pequeño para volver a tirar los aviones")]
    [SerializeField] public Button btnTirarAviones;
    [SerializeField] private Text statusText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Button btnLanzarTodo;
    [SerializeField] private Button btnCasoGlobo;
    [SerializeField] private Button btnCasoChoque;
    [SerializeField] private Button btnCasoCaptura;
    [SerializeField] private Button btnReiniciar;
    [SerializeField] private Button btnVolverMenu;

    // Contenedores para limpiar instancias fácilmente
    private Transform lane1Root;
    private Transform lane2Root;
    private Transform lane3Root;

    // Referencias activas
    private GameObject activeBalloon;
    private GameObject highBalloon;
    private GameObject activeBirdCrash;
    private GameObject highBird;
    private GameObject activeBirdCatcher;
    private string lastStatusMessage = "Iniciando demostración...";

    private void Awake()
    {
#if UNITY_EDITOR
        if (planePrefab == null)
            planePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Avion_Estandar.prefab");
        if (birdPrefab == null)
            birdPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Pajaro_FlyingFree.prefab");
        if (balloonPrefab == null)
            balloonPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Globo_Explosion.prefab");
        if (explosionPrefab == null)
            explosionPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Explosion_Choque.prefab");
#endif

        lane1Root = new GameObject("Pista_1_Globo").transform;
        lane2Root = new GameObject("Pista_2_ChoquePajaro").transform;
        lane3Root = new GameObject("Pista_3_CapturaPico").transform;

        lane1Root.SetParent(transform);
        lane2Root.SetParent(transform);
        lane3Root.SetParent(transform);
    }

    private void Start()
    {
        // Conectar botones UI si existen
        if (btnTirarAviones != null) btnTirarAviones.onClick.AddListener(RelanzarAviones);
        if (btnLanzarTodo != null) btnLanzarTodo.onClick.AddListener(LanzarLas3Pruebas);
        if (btnCasoGlobo != null) btnCasoGlobo.onClick.AddListener(LanzarCaso1_Globo);
        if (btnCasoChoque != null) btnCasoChoque.onClick.AddListener(LanzarCaso2_ChoquePajaro);
        if (btnCasoCaptura != null) btnCasoCaptura.onClick.AddListener(LanzarCaso3_CapturaPico);
        if (btnReiniciar != null) btnReiniciar.onClick.AddListener(ReiniciarTodo);
        if (btnVolverMenu != null) btnVolverMenu.onClick.AddListener(VolverAlMenu);

        // Preparar objetivos iniciales
        PrepararEscenario();

        SetStatus("¡Campo de pruebas listo! Iniciando demostración en 1 segundo...");
        StartCoroutine(AutoIniciarDemo());
    }

    private IEnumerator AutoIniciarDemo()
    {
        yield return new WaitForSeconds(1.2f);
        LanzarLas3Pruebas();
    }

    private void Update()
    {
        // Atajos de teclado para facilitar las pruebas
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null)
        {
            if (kb.spaceKey.wasPressedThisFrame) LanzarLas3Pruebas();
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) LanzarCaso1_Globo();
            if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) LanzarCaso2_ChoquePajaro();
            if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) LanzarCaso3_CapturaPico();
            if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) LanzarPruebaVueloAlto();
            if (kb.rKey.wasPressedThisFrame) ReiniciarTodo();
            if (kb.escapeKey.wasPressedThisFrame) VolverAlMenu();
        }
#endif
    }

    /// <summary>
    /// Coloca el globo y los 2 pájaros en sus posiciones ideales esperando a los aviones,
    /// además de un globo y un pájaro elevados para verificar que el avión pasa por debajo sin colisión indebida.
    /// </summary>
    public void PrepararEscenario()
    {
        LimpiarPistas();

        // --- PISTA 1: Globo normal esperando en (-laneSpacing, 1.2, targetDistanceZ) ---
        if (balloonPrefab != null)
        {
            Vector3 posGlobo = new Vector3(-laneSpacing, 1.2f, targetDistanceZ);
            activeBalloon = Instantiate(balloonPrefab, posGlobo, Quaternion.identity, lane1Root);
            activeBalloon.name = "Globo_Objetivo";

            // Asegurar que Origen_Modelo compense la altura cinemática de Maya (-1.45m)
            Transform origen = activeBalloon.transform.Find("Origen_Modelo");
            if (origen != null && Mathf.Abs(origen.localPosition.y) < 0.1f)
            {
                origen.localPosition = new Vector3(origen.localPosition.x, -1.45f, origen.localPosition.z);
            }

            // Asegurar componente GloboTarget
            GloboTarget gt = activeBalloon.GetComponent<GloboTarget>();
            if (gt == null) gt = activeBalloon.AddComponent<GloboTarget>();

            // Configurar collider esférico preciso para el globo (0.22m de radio)
            foreach (var b in activeBalloon.GetComponents<BoxCollider>()) Destroy(b);
            SphereCollider sc = activeBalloon.GetComponent<SphereCollider>();
            if (sc == null) sc = activeBalloon.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.22f;
            sc.center = Vector3.zero;

            Rigidbody rb = activeBalloon.GetComponent<Rigidbody>();
            if (rb == null) rb = activeBalloon.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // --- PISTA 1 (PRUEBA DE ALTURA): Globo elevado a Y=1.68m (el avión a Y=1.2 pasa por debajo sin tocarlo) ---
        if (balloonPrefab != null)
        {
            Vector3 posGloboAlto = new Vector3(-laneSpacing, 1.68f, targetDistanceZ * 0.5f);
            highBalloon = Instantiate(balloonPrefab, posGloboAlto, Quaternion.identity, lane1Root);
            highBalloon.name = "Globo_Alto_NoImpacto";

            Transform origen = highBalloon.transform.Find("Origen_Modelo");
            if (origen != null && Mathf.Abs(origen.localPosition.y) < 0.1f)
            {
                origen.localPosition = new Vector3(origen.localPosition.x, -1.45f, origen.localPosition.z);
            }

            GloboTarget gt = highBalloon.GetComponent<GloboTarget>();
            if (gt == null) gt = highBalloon.AddComponent<GloboTarget>();

            foreach (var b in highBalloon.GetComponents<BoxCollider>()) Destroy(b);
            SphereCollider sc = highBalloon.GetComponent<SphereCollider>();
            if (sc == null) sc = highBalloon.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.22f;
            sc.center = Vector3.zero;

            Rigidbody rb = highBalloon.GetComponent<Rigidbody>();
            if (rb == null) rb = highBalloon.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // --- PISTA 2: Pájaro de costado (0, 1.2, targetDistanceZ) para recibir impacto lateral ---
        if (birdPrefab != null)
        {
            Vector3 posPajaro2 = new Vector3(0f, 1.2f, targetDistanceZ);
            // Rotado 80 grados para que muestre el costado/lomo al avión que viene
            activeBirdCrash = Instantiate(birdPrefab, posPajaro2, Quaternion.Euler(0, 80, 0), lane2Root);
            activeBirdCrash.name = "Pajaro_ChoqueExplosion";

            // Desactivar vuelo orbital si lo tiene para que espere en la pista
            PajaroOrbital orb = activeBirdCrash.GetComponent<PajaroOrbital>();
            if (orb != null) orb.enabled = false;

            PajaroInteraccion pi = activeBirdCrash.GetComponent<PajaroInteraccion>();
            if (pi == null) pi = activeBirdCrash.AddComponent<PajaroInteraccion>();

            // Collider ajustado al cuerpo del pájaro
            foreach (var b in activeBirdCrash.GetComponents<BoxCollider>()) Destroy(b);
            SphereCollider sc = activeBirdCrash.GetComponent<SphereCollider>();
            if (sc == null) sc = activeBirdCrash.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.22f;
            sc.center = Vector3.zero;

            Rigidbody rb = activeBirdCrash.GetComponent<Rigidbody>();
            if (rb == null) rb = activeBirdCrash.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // --- PISTA 2 (PRUEBA DE ALTURA): Pájaro elevado a Y=1.68m (el avión a Y=1.2 pasa por debajo sin chocar) ---
        if (birdPrefab != null)
        {
            Vector3 posPajaroAlto = new Vector3(0f, 1.68f, targetDistanceZ * 0.5f);
            highBird = Instantiate(birdPrefab, posPajaroAlto, Quaternion.Euler(0, 80, 0), lane2Root);
            highBird.name = "Pajaro_Alto_NoImpacto";

            PajaroOrbital orb = highBird.GetComponent<PajaroOrbital>();
            if (orb != null) orb.enabled = false;

            PajaroInteraccion pi = highBird.GetComponent<PajaroInteraccion>();
            if (pi == null) pi = highBird.AddComponent<PajaroInteraccion>();

            foreach (var b in highBird.GetComponents<BoxCollider>()) Destroy(b);
            SphereCollider sc = highBird.GetComponent<SphereCollider>();
            if (sc == null) sc = highBird.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.22f;
            sc.center = Vector3.zero;

            Rigidbody rb = highBird.GetComponent<Rigidbody>();
            if (rb == null) rb = highBird.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // --- PISTA 3: Pájaro encarando de frente (+laneSpacing, 1.4, targetDistanceZ + 0.5) para cazar con el pico ---
        if (birdPrefab != null)
        {
            Vector3 posPajaro3 = new Vector3(laneSpacing, 1.4f, targetDistanceZ + 0.8f);
            // Mirando de frente hacia el avión (rotación Y = 180)
            activeBirdCatcher = Instantiate(birdPrefab, posPajaro3, Quaternion.Euler(5, 180, 0), lane3Root);
            activeBirdCatcher.name = "Pajaro_CazadorPico";

            PajaroOrbital orb = activeBirdCatcher.GetComponent<PajaroOrbital>();
            if (orb != null) orb.enabled = false;

            PajaroInteraccion pi = activeBirdCatcher.GetComponent<PajaroInteraccion>();
            if (pi == null) pi = activeBirdCatcher.AddComponent<PajaroInteraccion>();

            foreach (var b in activeBirdCatcher.GetComponents<BoxCollider>()) Destroy(b);
            SphereCollider sc = activeBirdCatcher.GetComponent<SphereCollider>();
            if (sc == null) sc = activeBirdCatcher.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.28f;
            sc.center = new Vector3(0f, 0.05f, 0.15f); // Centrado en la zona frontal del pico

            Rigidbody rb = activeBirdCatcher.GetComponent<Rigidbody>();
            if (rb == null) rb = activeBirdCatcher.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    /// <summary>
    /// Lanza las 3 pruebas simultáneamente para ver los 3 comportamientos a la vez.
    /// </summary>
    public void LanzarLas3Pruebas()
    {
        SetStatus("¡Lanzando las 3 pruebas a la vez!");
        LanzarCaso1_Globo();
        LanzarCaso2_ChoquePajaro();
        LanzarCaso3_CapturaPico();
    }

    /// <summary>
    /// Caso 1: Avión vuela hacia el Globo. Al tocarlo, el globo estalla, suma punto y el avión desaparece.
    /// </summary>
    public void LanzarCaso1_Globo()
    {
        if (activeBalloon == null)
        {
            PrepararEscenario();
        }

        Vector3 spawnPos = new Vector3(-laneSpacing, 1.2f, 0f);
        PaperPlane plane = SpawnTestPlane(spawnPos, Vector3.forward, lane1Root, planeFlightSpeed);
        SetStatus("Pista 1: Avión volando hacia el Globo...");
    }

    /// <summary>
    /// Caso 2: Avión embiste al Pájaro de lado. Al chocar, el avión explota con Explosion_Choque.
    /// </summary>
    public void LanzarCaso2_ChoquePajaro()
    {
        if (activeBirdCrash == null)
        {
            PrepararEscenario();
        }

        Vector3 spawnPos = new Vector3(0f, 1.2f, 0f);
        PaperPlane plane = SpawnTestPlane(spawnPos, Vector3.forward, lane2Root, planeFlightSpeed * 1.1f);
        SetStatus("Pista 2: Avión embistiendo al Pájaro -> ¡Explosión cómica!");
    }

    /// <summary>
    /// Caso 3: Pájaro encara al avión y lo caza con el pico llevándoselo volando hacia arriba.
    /// </summary>
    public void LanzarCaso3_CapturaPico()
    {
        if (activeBirdCatcher == null)
        {
            PrepararEscenario();
        }

        Vector3 spawnPos = new Vector3(laneSpacing, 1.2f, 0f);
        PaperPlane plane = SpawnTestPlane(spawnPos, Vector3.forward, lane3Root, planeFlightSpeed * 0.8f);

        // Hacer avanzar suavemente al pájaro cazador hacia el avión para el encuentro
        StartCoroutine(MoverPajaroCazador(activeBirdCatcher));

        SetStatus("Pista 3: Pájaro intercepta al avión -> ¡Captura con el pico!");
    }

    private IEnumerator MoverPajaroCazador(GameObject bird)
    {
        if (bird == null) yield break;
        float elapsed = 0f;
        while (elapsed < 3f && bird != null)
        {
            PajaroInteraccion pi = bird.GetComponent<PajaroInteraccion>();
            if (pi != null && pi.HasCapturedPlane)
            {
                yield break; // Deja que PajaroInteraccion.Update() se encargue de elevarse
            }

            // Avanzar hacia adelante (hacia el avión) y descender levemente
            bird.transform.position += (bird.transform.forward + Vector3.down * 0.05f).normalized * 1.5f * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private PaperPlane SpawnTestPlane(Vector3 position, Vector3 direction, Transform parent, float speed)
    {
        if (planePrefab == null) return null;

        GameObject planeObj = Instantiate(planePrefab, position, Quaternion.LookRotation(direction), parent);
        PaperPlane pp = planeObj.GetComponent<PaperPlane>();
        if (pp != null)
        {
            pp.forwardSpeed = speed;
            if (explosionPrefab != null)
            {
                pp.explosionPrefab = explosionPrefab;
            }
            pp.Initialize(direction, null);
        }

        // Asegurar que tenga collider trigger calibrado en la punta del avión
        foreach (var b in planeObj.GetComponents<BoxCollider>()) Destroy(b);
        SphereCollider sc = planeObj.GetComponent<SphereCollider>();
        if (sc == null) sc = planeObj.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 0.08f;
        sc.center = new Vector3(0f, 0f, 0.12f); // En la punta/morro del avión de papel

        return pp;
    }

    /// <summary>
    /// Limpia los aviones actuales, restablece los objetivos si fueron destruidos o capturados, y vuelve a lanzar los 3 aviones de inmediato.
    /// </summary>
    public void RelanzarAviones()
    {
        StopAllCoroutines();
        // Restablece siempre el globo y ambos pájaros a sus posiciones y orientaciones iniciales exactas
        PrepararEscenario();

        SetStatus("¡Volviendo a tirar los aviones!");
        LanzarLas3Pruebas();
    }

    /// <summary>
    /// Caso 4: Lanza aviones a altura elevada (Y=1.68m) directamente hacia el Globo Alto y Pájaro Alto para demostrar que al volar a su altura sí impactan.
    /// </summary>
    public void LanzarPruebaVueloAlto()
    {
        if (highBalloon == null || highBird == null)
        {
            PrepararEscenario();
        }

        Vector3 spawnPosGloboAlto = new Vector3(-laneSpacing, 1.68f, 0f);
        SpawnTestPlane(spawnPosGloboAlto, Vector3.forward, lane1Root, planeFlightSpeed);

        Vector3 spawnPosPajaroAlto = new Vector3(0f, 1.68f, 0f);
        SpawnTestPlane(spawnPosPajaroAlto, Vector3.forward, lane2Root, planeFlightSpeed * 1.1f);

        SetStatus("Prueba de Altura: Aviones volando a Y=1.68m -> ¡Impactan directamente en Globo Alto y Pájaro Alto!");
    }

    private void LimpiarAviones()
    {
        foreach (Transform child in lane1Root)
        {
            if (child.gameObject != activeBalloon && child.gameObject != highBalloon) Destroy(child.gameObject);
        }
        foreach (Transform child in lane2Root)
        {
            if (child.gameObject != activeBirdCrash && child.gameObject != highBird) Destroy(child.gameObject);
        }
        foreach (Transform child in lane3Root)
        {
            if (child.gameObject != activeBirdCatcher) Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Reinicia todos los objetos de las pistas a su estado inicial.
    /// </summary>
    public void ReiniciarTodo()
    {
        StopAllCoroutines();
        PrepararEscenario();
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetScore();
        }
        SetStatus("Pistas reiniciadas. Pulsa 'Lanzar Todo' o Espacio para repetir.");
    }

    private void LimpiarPistas()
    {
        foreach (Transform child in lane1Root) Destroy(child.gameObject);
        foreach (Transform child in lane2Root) Destroy(child.gameObject);
        foreach (Transform child in lane3Root) Destroy(child.gameObject);
    }

    private void SetStatus(string msg)
    {
        lastStatusMessage = msg;
        if (statusText != null) statusText.text = msg;
        Debug.Log($"[AnimacionesTestBench] {msg}");
    }

    public void VolverAlMenu()
    {
        SceneManager.LoadScene("FlyingFreeMenu");
    }

    private void OnGUI()
    {
        // Panel superior
        GUI.Box(new Rect(10, 10, Screen.width - 20, 95), "");

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        GUI.Label(new Rect(20, 14, Screen.width - 40, 26), "CAMPO DE PRUEBAS: ANIMACIONES Y COLISIONES", titleStyle);

        int score = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
        GUIStyle scoreStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.2f, 0.9f, 0.4f) }
        };
        GUI.Label(new Rect(25, 42, 350, 25), $"PUNTUACIÓN (Globos estallados): {score}", scoreStyle);

        GUIStyle statusStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            normal = { textColor = Color.yellow }
        };
        GUI.Label(new Rect(25, 68, Screen.width - 280, 25), $"Estado: {lastStatusMessage}", statusStyle);

        // Botón pequeño para volver a tirar los aviones (esquina superior derecha)
        GUI.backgroundColor = new Color(0.2f, 0.85f, 0.5f, 1f);
        if (GUI.Button(new Rect(Screen.width - 235, 42, 215, 42), "✈ Volver a tirar aviones"))
        {
            RelanzarAviones();
        }
        GUI.backgroundColor = Color.white;

        // Barra inferior de botones
        float yBottom = Screen.height - 60;

        if (GUI.Button(new Rect(15, yBottom, 155, 48), "▶ Lanzar Todo (Espacio)"))
        {
            LanzarLas3Pruebas();
        }

        if (GUI.Button(new Rect(175, yBottom, 145, 48), "① Globo (1)"))
        {
            LanzarCaso1_Globo();
        }

        if (GUI.Button(new Rect(325, yBottom, 145, 48), "② Choque (2)"))
        {
            LanzarCaso2_ChoquePajaro();
        }

        if (GUI.Button(new Rect(475, yBottom, 145, 48), "③ Caza Pico (3)"))
        {
            LanzarCaso3_CapturaPico();
        }

        GUI.backgroundColor = new Color(0.95f, 0.65f, 0.2f, 1f);
        if (GUI.Button(new Rect(625, yBottom, 160, 48), "④ Vuelo Alto (4)"))
        {
            LanzarPruebaVueloAlto();
        }
        GUI.backgroundColor = Color.white;

        if (GUI.Button(new Rect(790, yBottom, 110, 48), "↺ Reset (R)"))
        {
            ReiniciarTodo();
        }

        if (GUI.Button(new Rect(Screen.width - 150, yBottom, 135, 48), "← Menú Principal"))
        {
            VolverAlMenu();
        }

        // Carteles 3D encima de cada objetivo
        DrawLaneLabel(new Vector3(-laneSpacing, 2.3f, targetDistanceZ), "① GLOBO (Y=1.2m)\n(Estallido + Puntos)");
        DrawLaneLabel(new Vector3(-laneSpacing, 2.1f, targetDistanceZ * 0.5f), "GLOBO ALTO (Y=1.68m)\n(Paso bajo: NO explota)");

        DrawLaneLabel(new Vector3(0f, 2.3f, targetDistanceZ), "② PÁJARO (Y=1.2m)\n(Choque y Explosión)");
        DrawLaneLabel(new Vector3(0f, 2.1f, targetDistanceZ * 0.5f), "PÁJARO ALTO (Y=1.68m)\n(Paso bajo: NO choca)");

        DrawLaneLabel(new Vector3(laneSpacing, 2.3f, targetDistanceZ + 0.8f), "③ PÁJARO\n(Captura con el Pico)");
    }

    private void DrawLaneLabel(Vector3 worldPos, string text)
    {
        Camera c = Camera.main;
        if (c == null) return;
        Vector3 screenPos = c.WorldToScreenPoint(worldPos);
        if (screenPos.z > 0)
        {
            GUIStyle labelStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            float w = 180;
            float h = 42;
            GUI.Box(new Rect(screenPos.x - w / 2, Screen.height - screenPos.y - h / 2, w, h), text, labelStyle);
        }
    }
}
