#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Ensayos de la tarea 4. Los contactos de movimiento usan PhysX real; tracking explícitamente simulado.</summary>
public sealed class CollisionPlayModeProbe : MonoBehaviour
{
    readonly List<string> results = new List<string>();
    readonly List<GameObject> owned = new List<GameObject>();
    readonly Dictionary<PaperPlane, int> finished = new Dictionary<PaperPlane, int>();
    Camera cam;
    ARGameSpace space;
    ScoreManager score;
    GameManagerRA game;
    bool failure;
    string phase = "Colisiones e interacciones";
    string Output => Path.GetFullPath("Logs/PruebasColisiones/Colisiones_PlayMode.txt");
    void OnGUI()
    {
        GUI.Box(new Rect(14, 14, 580, 80), "TAREA 4 · PRUEBAS DE COLISIONES · SIMULACIÓN\n" + phase
            + "\nEl informe se abrirá al terminar. No requiere cámara ni marca.");
    }
    IEnumerator Start()
    {
        Application.logMessageReceived += OnLog;
        Time.captureFramerate = 60;
        var tests = Cases();
        while (true)
        {
            bool more = false;
            try { more = tests.MoveNext(); } catch (Exception ex) { failure = true; results.Add("FAIL " + ex); }
            if (!more || failure) break;
            yield return tests.Current;
        }
        Application.logMessageReceived -= OnLog;
        foreach (var obj in owned) if (obj != null) Destroy(obj);
        Time.captureFramerate = 0;
        results.Insert(0, $"Unity {Application.unityVersion} · Vuforia 10.17.4 · Tarea 4 · {DateTime.Now:yyyy-MM-dd HH:mm}");
        results.Add(failure ? "RESULTADO: ERROR" : "RESULTADO: TODAS LAS PRUEBAS PASAN");
        Directory.CreateDirectory(Path.GetDirectoryName(Output)); File.WriteAllLines(Output, results);
        Debug.Log(string.Join("\n", results));
        SessionState.SetInt("FlyingFree.Collisions.Exit", failure ? 1 : 0);
        EditorApplication.isPlaying = false;
    }
    void OnLog(string message, string stack, LogType type)
    {
        if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
        failure = true; results.Add("ERROR UNITY " + message + "\n" + stack);
    }
    void Check(bool condition, string label)
    { if (!condition) throw new Exception(label); results.Add("PASS " + label); }
    static void Field(object obj, string name, object value) => obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value);
    GameObject Own(GameObject obj) { owned.Add(obj); return obj; }
    GameObject Prefab(string name) => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + name + ".prefab");
    PaperPlane Plane(Vector3 position, float speed = 0, string model = "Avion_Cuaderno", bool initialize = true)
    {
        var obj = Own(Instantiate(Prefab(model), position, space.transform.rotation));
        var old = obj.GetComponent<FlyingFree.Sprint2.PaperPlaneMotor>(); if (old != null) old.enabled = false;
        var plane = obj.GetComponent<PaperPlane>() ?? obj.AddComponent<PaperPlane>();
        if (old != null) plane.SetVisual(old.visual);
        plane.forwardSpeed = speed; plane.explosionPrefab = null;
        if (initialize) plane.Initialize(space.transform.forward, space.transform);
        finished[plane] = 0; plane.OnFinished += hit => finished[plane]++;
        return plane;
    }
    GloboTarget Balloon(Vector3 position, bool compound = false)
    {
        var obj = Own(Instantiate(Prefab("Globo_Explosion"), position, Quaternion.identity, space.transform));
        var target = obj.GetComponent<GloboTarget>();
        if (compound)
        {
            var child = new GameObject("SegundoCollider"); child.transform.SetParent(obj.transform, false);
            child.AddComponent<SphereCollider>().radius = .22f;
        }
        return target;
    }
    PajaroInteraccion Bird(Vector3 position)
    {
        var obj = Own(Instantiate(Prefab("Pajaro_FlyingFree"), position, Quaternion.identity, space.transform));
        var bird = obj.GetComponent<PajaroInteraccion>(); bird.Configurar(null, cam);
        bird.radioDeteccion = .1f; return bird;
    }
    Collider Solid(Vector3 position, bool trigger = false)
    {
        var obj = Own(new GameObject(trigger ? "TriggerDecorativo" : "Pared")); obj.transform.position = position;
        var col = obj.AddComponent<BoxCollider>(); col.size = new Vector3(1, 1, .02f); col.isTrigger = trigger;
        return col;
    }
    bool AllCollidersOff(Component obj)
    { foreach (var col in obj.GetComponentsInChildren<Collider>(true)) if (col.enabled) return false; return true; }
    void Clean()
    {
        foreach (var obj in owned.ToArray())
            if (obj != null && obj != cam.gameObject && obj != space.gameObject && obj != score.gameObject && (game == null || obj != game.gameObject))
            { obj.SetActive(false); Destroy(obj); owned.Remove(obj); }
        score.ResetScore();
    }
    void Snapshot(string name, Vector3 focus, float distance = 1, bool keepCamera = false, int width = 1280, int height = 720)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Output));
        if (!keepCamera) { cam.transform.position = focus + new Vector3(.65f, .25f, -.9f) * distance; cam.transform.LookAt(focus); }
        var rt = new RenderTexture(width, height, 24); cam.targetTexture = rt; cam.aspect = (float)width / height;
        cam.Render(); var previous = RenderTexture.active; RenderTexture.active = rt;
        var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
        pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
        File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(Output), name), pixels.EncodeToPNG());
        RenderTexture.active = previous; cam.targetTexture = null; cam.ResetAspect(); Destroy(rt); Destroy(pixels);
    }
    IEnumerator Cases()
    {
        cam = Own(new GameObject("CamaraPruebasColisiones")).AddComponent<Camera>(); cam.tag = "MainCamera";
        RenderSettings.ambientLight = new Color(.7f, .7f, .7f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.7f, .7f, .7f);
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.55f, .76f, .88f);
        cam.transform.position = new Vector3(0, 1.4f, -4);
        space = Own(new GameObject("GameSpaceSimulado")).AddComponent<ARGameSpace>(); space.enabled = false;
        score = Own(new GameObject("MarcadorPruebas")).AddComponent<ScoreManager>();
        var light = Own(new GameObject("Luz")); light.AddComponent<Light>().type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(40, -30, 0);
        yield return null;
        foreach (var model in new[] { "Avion_Cuaderno", "Avion_Rayo", "Avion_Brisa", "Avion_AsCorazones", "Avion_Estandar" })
        {
            phase = model + ": barrido rápido";
            var balloon = Balloon(new Vector3(0, 1.4f, 0), true);
            var plane = Plane(new Vector3(0, 1.4f, -1), 120, model);
            yield return null; yield return null;
            Check(balloon.IsPopped && !plane.IsFlying, model + ": detecta globo cruzado a 120 m/s entre frames");
            Check(score.CurrentScore == 1 && finished[plane] == 1, model + ": una sola puntuación y notificación");
            Check(AllCollidersOff(balloon) && AllCollidersOff(plane), model + ": retira todos los colliders tras el acierto");
            balloon.PopBalloon(plane); plane.HandleContact(balloon.GetComponent<Collider>());
            Check(score.CurrentScore == 1 && finished[plane] == 1, model + ": callbacks repetidos se ignoran");
            Clean(); yield return null;
        }
        phase = "Orden de contactos y solapamientos";
        var near = Balloon(new Vector3(0, 1.4f, -.35f));
        var far = Balloon(new Vector3(0, 1.4f, .35f));
        var fast = Plane(new Vector3(0, 1.4f, -1), 120);
        yield return null; yield return null;
        Check(near.IsPopped && !far.IsPopped && score.CurrentScore == 1, "Dos globos en un paso: consume únicamente el primero del recorrido");
        Clean(); yield return null;
        var overlap = Balloon(new Vector3(0, 1.4f, 0), true);
        var still = Plane(overlap.transform.position);
        yield return null; yield return null;
        Check(overlap.IsPopped && finished[still] == 1, "Solapamiento inicial sin desplazamiento: detectado una sola vez");
        Clean(); yield return null;
        var oneBalloon = Balloon(new Vector3(0, 1.4f, 0));
        var first = Plane(new Vector3(0, 1.4f, -1)); var second = Plane(new Vector3(0, 1.4f, -2));
        int popEvents = 0; var onPop = new UnityEvent(); onPop.AddListener(() => popEvents++); Field(oneBalloon, "onPopped", onPop);
        Check(oneBalloon.TryPopBalloon(first) && !oneBalloon.TryPopBalloon(second), "Dos aviones en el mismo globo: solo se acepta el primero");
        Check(second.IsFlying && score.CurrentScore == 1 && popEvents == 1, "Un globo consumido no elimina al segundo avión ni repite su evento");
        Clean(); yield return null;
        var previewBalloon = Balloon(new Vector3(0, 1.4f, 0));
        var preview = Plane(previewBalloon.transform.position, initialize: false);
        yield return null; yield return null;
        Check(!previewBalloon.IsPopped && score.CurrentScore == 0, "Una vista previa sin lanzar no puede explotar globos");
        Check(!previewBalloon.TryPopBalloon(null), "Un contacto sin avión no genera puntos ni explosión");
        previewBalloon.enabled = false;
        var live = Plane(new Vector3(0, 1.4f, -1)); previewBalloon.PopBalloon(live);
        Check(!previewBalloon.IsPopped && live.IsFlying, "Un componente de globo desactivado ignora contactos");
        Clean(); yield return null;
        phase = "Capas, obstáculos y colliders compuestos";
        var wall = Solid(new Vector3(0, 1.4f, 0)); fast = Plane(new Vector3(0, 1.4f, -1), 120);
        yield return null; yield return null;
        Check(!fast.IsFlying && finished[fast] == 1 && score.CurrentScore == 0, "Pared fina a alta velocidad: una pérdida y ningún punto");
        Clean(); yield return null;
        var decorative = Solid(new Vector3(0, 1.4f, 0), true); fast = Plane(new Vector3(0, 1.4f, -1), 120);
        yield return null; yield return null;
        Check(fast.IsFlying && finished[fast] == 0, "Los triggers decorativos no consumen aviones");
        Clean(); yield return null;
        var ignored = Balloon(new Vector3(0, 1.4f, 0)); ignored.gameObject.layer = 9;
        Physics.IgnoreLayerCollision(8, 9, true);
        fast = Plane(new Vector3(0, 1.4f, -1), 120); fast.gameObject.layer = 8;
        yield return null; yield return null;
        bool respectsLayer = !ignored.IsPopped && fast.IsFlying; Physics.IgnoreLayerCollision(8, 9, false);
        Check(respectsLayer, "El barrido respeta la matriz de colisión de capas");
        Clean(); yield return null;
        ignored = Balloon(new Vector3(0, 1.4f, 0)); fast = Plane(new Vector3(0, 1.4f, -1), 120);
        foreach (var a in fast.GetComponentsInChildren<Collider>()) foreach (var b in ignored.GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(a, b);
        yield return null; yield return null;
        Check(!ignored.IsPopped && fast.IsFlying, "El barrido respeta Physics.IgnoreCollision entre objetos");
        Clean(); yield return null;
        var many = Balloon(new Vector3(0, 1.4f, 0));
        for (int i = 0; i < 70; i++) Solid(new Vector3(0, 1.4f, 0), true);
        fast = Plane(new Vector3(0, 1.4f, -1), 120);
        yield return null; yield return null;
        Check(many.IsPopped && score.CurrentScore == 1, "Más de 32 contactos: amplía el buffer sin perder el globo válido");
        Clean(); yield return null;
        var classic = Solid(new Vector3(0, 1.4f, 0), true).gameObject.AddComponent<PlaneTarget>();
        int targetEvents = 0; var hit = new UnityEvent(); var classicPlane = Plane(new Vector3(0, 1.4f, -1));
        hit.AddListener(() => { targetEvents++; classic.NotifyHit(classicPlane); }); Field(classic, "onHit", hit);
        classic.NotifyHit(classicPlane); classic.NotifyHit(classicPlane);
        Check(classic.IsHit && targetEvents == 1 && finished[classicPlane] == 1, "Objetivo clásico: evita contactos duplicados y callbacks reentrantes");
        Clean(); yield return null;
        var childTarget = Balloon(new Vector3(0, 1.4f, 0));
        var compoundPlane = Plane(new Vector3(0, 1.4f, -1), 120);
        foreach (var col in compoundPlane.GetComponentsInChildren<Collider>()) col.enabled = false;
        var childCollider = new GameObject("ColliderHijoAvion"); childCollider.transform.SetParent(compoundPlane.transform, false);
        childCollider.AddComponent<SphereCollider>().radius = .08f;
        yield return null; yield return null;
        Check(childTarget.IsPopped && finished[compoundPlane] == 1, "Un collider hijo del avión también barre y resuelve contactos con la raíz");
        Clean(); yield return null;
        var scaled = Balloon(new Vector3(0, 1.4f, 0));
        scaled.transform.localScale = new Vector3(.6f, .3f, .45f);
        fast = Plane(new Vector3(0, 1.4f, -1), 120); fast.transform.localScale = Vector3.one * .5f;
        yield return null; yield return null;
        Check(scaled.IsPopped && score.CurrentScore == 1, "Los barridos respetan la escala real de avión y globo");
        Clean(); yield return null;
        phase = "Explosión completa";
        var animated = Balloon(new Vector3(0, 1.4f, 0)); var moving = animated.gameObject.AddComponent<BalloonAnimator>();
        moving.randomizeColor = false; moving.enableIdleFloat = true;
        var impact = Plane(new Vector3(0, 1.4f, -1)); animated.PopBalloon(impact);
        yield return null;
        Check(!moving.enabled, "El movimiento del globo se detiene al comenzar la explosión");
        var animator = animated.GetComponentInChildren<Animator>();
        Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Globo_Explosion_Explotar_Globo"), "Se reproduce el clip real de explosión del prefab");
        Vector3 explosionPosition = animated.transform.position;
        for (int i = 0; i < 70; i++) yield return null;
        Check(animated != null && Vector3.Distance(explosionPosition, animated.transform.position) < .0001f, "La explosión no se corta a 0,8 s ni sigue flotando");
        Snapshot("Explosion_Globo_Unity.png", explosionPosition);
        for (int i = 0; i < 300; i++) yield return null;
        Check(animated == null, "El globo se elimina al finalizar su animación de 5,625 s");
        Clean(); yield return null;
        phase = "Colisiones con pájaros";
        var birdPlane = Plane(new Vector3(0, 1.4f, -1));
        var bird = Bird(birdPlane.transform.position + Vector3.right * .27f);
        yield return null; yield return null;
        Check(bird.Estado != PajaroInteraccion.EstadoRobo.Patrulla, "Contacto físico pájaro-avión inicia la intercepción sin pulsar captura");
        bird.HandlePlaneCollision(birdPlane); bird.HandlePlaneCollision(birdPlane);
        var rival = Bird(birdPlane.transform.position + Vector3.left * .27f);
        Check(!rival.IntentarInterceptar(birdPlane), "Dos pájaros en contacto no pueden reservar el mismo avión");
        rival.DesaparecerPajaro();
        for (int i = 0; i < 100 && !birdPlane.IsCaptured; i++) yield return null;
        Check(birdPlane.IsCaptured && bird.HasCapturedPlane && finished[birdPlane] == 1, "Robo real tras el contacto: una sola notificación");
        var afterCapture = Balloon(birdPlane.transform.position);
        Check(!afterCapture.TryPopBalloon(birdPlane) && score.CurrentScore == 0, "Un avión robado no puede explotar un globo por un callback tardío");
        Check(AllCollidersOff(birdPlane), "Los colliders del avión robado están desactivados");
        Snapshot("Colision_Pajaro_Unity.png", birdPlane.transform.position);
        Clean(); yield return null;
        var crossingPlane = Plane(new Vector3(0, 1.4f, -1), 120, "Avion_Rayo");
        var crossingBird = Bird(new Vector3(0, 1.4f, 0));
        yield return null; yield return null;
        Check(crossingBird.Estado != PajaroInteraccion.EstadoRobo.Patrulla,
            "El barrido detecta el pájaro a 120 m/s aunque el avión haya salido del radio al final del frame");
        for (int i = 0; i < 100 && !crossingPlane.IsCaptured; i++) yield return null;
        Check(crossingPlane.IsCaptured && finished[crossingPlane] == 1, "Un cruce rápido de pájaro culmina en un único robo");
        Clean(); yield return null;
        phase = "Ascenso paralelo al plano de la marca";
        var poses = new[] { Quaternion.identity, Quaternion.Euler(90, 0, 0),
            Quaternion.Euler(65, 25, 38), Quaternion.Euler(180, 0, 0) };
        var poseNames = new[] { "Origen centrado en target", "Marca en pared", "Marca inclinada", "Marca invertida" };
        for (int poseIndex = 0; poseIndex < poses.Length; poseIndex++)
        {
            var marker = Own(new GameObject("MarcaSimuladaGlobos"));
            marker.transform.SetPositionAndRotation(Vector3.zero, poses[poseIndex]);
            // Cámara REALISTA frente a la imagen: normal Y del target hacia la cámara,
            // Z de la imagen hacia arriba en pantalla. No dar por hecho que Y mundial
            // sea la altura: puede coincidir con la normal en un origen target-relative.
            cam.transform.SetPositionAndRotation(marker.transform.position + marker.transform.up * 2.2f,
                Quaternion.LookRotation(-marker.transform.up, marker.transform.forward));
            var spawner = marker.AddComponent<TargetPlaneSpawner>();
            spawner.testModeWithoutVuforia = false; spawner.prefabToSpawn = Prefab("Globo_Explosion");
            spawner.planeOffset = new Vector3(0, 0, .75f); spawner.riseDuration = .3f; spawner.showVisualPlane = false;
            yield return null;
            spawner.OnTargetFound();
            var generated = marker.transform.Find("SpawnedObjectsContainer").GetComponentsInChildren<GloboTarget>();
            Check(generated.Length == 6 && generated[0].GetComponentInChildren<Animator>() != null,
                poseNames[poseIndex] + ": genera seis globos del prefab real");
            var start = new Vector3[generated.Length]; var depth = new float[generated.Length];
            for (int i = 0; i < generated.Length; i++)
            { start[i] = generated[i].transform.position; depth[i] = cam.WorldToViewportPoint(start[i]).z; }
            for (int i = 0; i < 12; i++) yield return null;
            bool parallel = true, upward = true, upright = true, constantDepth = true;
            for (int i = 0; i < generated.Length; i++)
            {
                Vector3 displacement = generated[i].transform.position - start[i];
                parallel &= Mathf.Abs(Vector3.Dot(displacement, marker.transform.up)) < .0001f;
                upward &= Vector3.Dot(displacement, cam.transform.up) > .01f;
                constantDepth &= Mathf.Abs(cam.WorldToViewportPoint(generated[i].transform.position).z - depth[i]) < .0001f;
                upright &= Vector3.Dot(generated[i].transform.up, spawner.LayoutRotation * Vector3.forward) > .995f;
            }
            Check(parallel && upward, poseNames[poseIndex] + ": ascienden por el plano de la imagen, hacia arriba en pantalla");
            Check(constantDepth, poseNames[poseIndex] + ": mantienen la profundidad y no avanzan hacia la cámara");
            Check(upright, poseNames[poseIndex] + ": los nudos miran a la parte inferior de la marca");
            for (int i = 0; i < 16; i++) yield return null;
            bool onImage = true;
            foreach (var balloon in generated)
                onImage &= Vector3.Dot(balloon.transform.position - marker.transform.position, marker.transform.forward) > 0
                    && Mathf.Abs(Vector3.Dot(balloon.transform.position - marker.transform.position, marker.transform.up) - .03f) < .001f;
            Check(onImage, poseNames[poseIndex] + ": formación sobre la marca con separación normal constante de 3 cm");
            Quaternion corridor = ARGameSpace.UprightMarkerRotation(marker.transform, cam);
            Check(Vector3.Dot(corridor * Vector3.up, cam.transform.up) > .999f
                && Vector3.Dot(corridor * Vector3.forward, marker.transform.position - cam.transform.position) > 0,
                poseNames[poseIndex] + ": el avión y los pájaros comparten el plano de juego y la altura de la imagen");
            if (poseIndex == 0)
            {
                Snapshot("Globos_Paralelos_Camara_Unity.png", Vector3.zero, keepCamera: true);
                // No sustituir por una cámara global alejada: se comprueba una cámara
                // situada en la normal del target, donde el error anterior era visible.
                foreach (float distance in new[] { .35f, .7f, 1.5f })
                {
                    cam.transform.position = marker.transform.position + marker.transform.up * distance;
                    for (int i = 0; i < 3; i++) yield return null;
                    foreach (var balloon in generated)
                        Check(Mathf.Abs(cam.WorldToViewportPoint(balloon.transform.position).z - (distance - .03f)) < .001f,
                            "Cámara a " + distance + " m: " + balloon.name + " conserva su distancia sin salir por la normal");
                }
                cam.transform.position = marker.transform.position + marker.transform.up * 4.2f;
                Snapshot("Globos_Paralelos_Portrait_Unity.png", Vector3.zero, keepCamera: true, width: 1080, height: 2400);
            }
            marker.transform.rotation = Quaternion.Euler(25, -15, 45);
            cam.transform.SetPositionAndRotation(marker.transform.position + marker.transform.up * 2.2f,
                Quaternion.LookRotation(-marker.transform.up, marker.transform.forward));
            yield return null; yield return null;
            bool afterTilt = true;
            foreach (var balloon in generated)
                afterTilt &= Vector3.Dot(balloon.transform.up, spawner.LayoutRotation * Vector3.forward) > .995f
                    && Mathf.Abs(Vector3.Dot(balloon.transform.position - marker.transform.position, marker.transform.up) - .03f) < .001f;
            Check(afterTilt, poseNames[poseIndex] + ": cambiar la pose mantiene los globos paralelos a la imagen");
            spawner.OnTargetLost();
            Check(!generated[0].gameObject.activeInHierarchy, poseNames[poseIndex] + ": perder la marca oculta los globos");
            spawner.OnTargetFound(); yield return null; yield return null;
            Check(marker.GetComponentsInChildren<GloboTarget>().Length == 6,
                poseNames[poseIndex] + ": recuperar la marca no duplica globos");
            if (poseIndex == 0)
            {
                // Usar el mismo marco y rumbo del lanzamiento real.
                space.transform.SetPositionAndRotation(marker.transform.position, ARGameSpace.UprightMarkerRotation(marker.transform, cam));
                var orientedPlane = Plane(generated[0].transform.position - space.transform.forward, 120);
                yield return null; yield return null;
                Check(score.CurrentScore == 1 && finished[orientedPlane] == 1,
                    "La formación paralela conserva impacto, explosión y punto con el avión real");
            }
            Clean(); yield return null; space.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
        phase = "Encuadre compacto para móvil y webcam";
        for (int format = 0; format < 2; format++)
        foreach (float distance in new[] { .30f, .50f, .80f })
        {
            bool portrait = format == 0;
            cam.fieldOfView = 41.5f; cam.aspect = portrait ? 1080f / 2400 : 1920f / 1080; cam.nearClipPlane = .05f;
            var marker = Own(new GameObject("MarcaEncuadre"));
            marker.transform.rotation = Quaternion.Euler(90, 0, 0);
            cam.transform.SetPositionAndRotation(marker.transform.up * distance,
                Quaternion.LookRotation(-marker.transform.up, marker.transform.forward));
            Field(space, "targetMarker", marker.transform); Field(space, "isCurrentlyTracked", true);
            space.transform.SetPositionAndRotation(marker.transform.position, ARGameSpace.UprightMarkerRotation(marker.transform, cam));
            var spawner = marker.AddComponent<TargetPlaneSpawner>();
            spawner.testModeWithoutVuforia = false; spawner.prefabToSpawn = Prefab("Globo_Explosion");
            spawner.fitLayoutToCamera = true; spawner.maximumLayoutScale = .15f;
            spawner.planeOffset = new Vector3(0, 0, .3f); spawner.riseDuration = .25f; spawner.showVisualPlane = false;
            yield return null; spawner.OnTargetFound();
            for (int i = 0; i < 25; i++) yield return null;
            var balloons = marker.GetComponentsInChildren<GloboTarget>();
            string label = (portrait ? "Vertical" : "Horizontal") + " a " + distance + " m";
            Check(balloons.Length == 6 && spawner.LayoutScale < .16f, label + ": seis globos alrededor de la marca con escala compacta");
            bool visible = true, nearMarker = true;
            foreach (var balloon in balloons)
            {
                nearMarker &= Vector3.Distance(balloon.transform.position, marker.transform.position) < .35f;
                foreach (var renderer in balloon.GetComponentsInChildren<Renderer>())
                {
                    if (!renderer.enabled || renderer.gameObject.name.StartsWith("Globo_Trozo_")) continue;
                    var bounds = renderer.bounds;
                    for (int corner = 0; corner < 8; corner++)
                    {
                        Vector3 point = bounds.center + Vector3.Scale(bounds.extents,
                            new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                        Vector3 viewport = cam.WorldToViewportPoint(point);
                        visible &= viewport.z > cam.nearClipPlane && viewport.x > .02f && viewport.x < .98f
                            && viewport.y > .08f && viewport.y < .88f;
                    }
                }
            }
            Check(visible, label + ": cuerpos e hilos completos visibles, sin invadir el HUD");
            Check(nearMarker, label + ": ningún globo queda a metros del logo");
            Check(Mathf.Abs(space.GameplayScale - spawner.LayoutScale) < .00001f,
                label + ": avión, globo, pájaro y colliders comparten escala");
            float keptScale = spawner.LayoutScale;
            cam.transform.position += cam.transform.right * .001f;
            yield return null;
            Check(Mathf.Abs(spawner.LayoutScale - keptScale) < .00001f, label + ": mover la cámara no cambia el tamaño durante la ronda");
            if (Mathf.Abs(distance - .50f) < .01f)
                Snapshot(portrait ? "Encuadre_Compacto_Vertical.png" : "Encuadre_Compacto_Horizontal.png",
                    Vector3.zero, keepCamera: true, width: portrait ? 1080 : 1920, height: portrait ? 2400 : 1080);
            cam.aspect = portrait ? 1080f / 2400 : 1920f / 1080;
            var launcher = Own(new GameObject("LanzadorCompactoReal")).AddComponent<TouchFlightController>();
            Field(launcher, "cam", cam); Field(launcher, "arReference", space.transform); Field(launcher, "planePrefab", Prefab("Avion_Cuaderno"));
            Vector3 aim = cam.WorldToScreenPoint(balloons[0].transform.position);
            Check(launcher.TryLaunch(new Vector2(aim.x, aim.y)), label + ": lanza usando el controlador real hacia el globo visible");
            var launched = launcher.CurrentPlane;
            Check(cam.WorldToViewportPoint(launched.transform.position).z > cam.nearClipPlane,
                label + ": el avión nace delante del recorte de cámara");
            for (int i = 0; i < 240 && launched != null && launched.IsFlying; i++) yield return null;
            Check(score.CurrentScore == 1 && balloons[0].IsPopped,
                label + ": el avión realmente alcanza el globo pulsado y suma un punto");
            var carryPlane = Plane(space.transform.TransformPoint(new Vector3(0, 0, -1)), 0);
            carryPlane.transform.localScale *= space.GameplayScale;
            var compactBird = Bird(carryPlane.transform.position + space.transform.right * (.27f * space.GameplayScale));
            Check(compactBird.IntentarInterceptar(carryPlane), label + ": el pájaro detecta un avión a la escala compacta");
            for (int i = 0; i < 100 && !carryPlane.IsCaptured; i++) yield return null;
            Check(carryPlane.IsCaptured && finished[carryPlane] == 1, label + ": el robo compacto se resuelve una sola vez");
            compactBird.DesaparecerPajaro();
            var flock = Own(new GameObject("BandadaCompacta")).AddComponent<ControlBandada>();
            flock.camaraAR = cam; flock.gameSpace = space; flock.prefabPajaro = Prefab("Pajaro_FlyingFree");
            flock.minimoActivos = flock.maximoActivos = 1; flock.intervaloAparicion = .1f;
            for (int i = 0; i < 70; i++) yield return null;
            Check(flock.PajarosActivosCount == 1, label + ": la aparición automática de pájaros funciona con la escala reducida");
            Clean(); yield return null; space.SetGameplayScale(1);
            Field(space, "targetMarker", null); space.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
        cam.ResetAspect(); cam.fieldOfView = 60;
        phase = "Relocalización y pausa AR";
        var falseHit = Balloon(new Vector3(2, 1.4f, 0));
        var anchored = Plane(new Vector3(0, 1.4f, 0));
        space.transform.position = Vector3.right * 4;
        // Mantener el obstáculo en el mundo, entre ambas poses, para detectar barridos AR falsos.
        falseHit.transform.SetParent(null, true); falseHit.transform.position = new Vector3(2, 1.4f, 0);
        yield return null; yield return null;
        Check(!falseHit.IsPopped && anchored.IsFlying, "Relocalizar la referencia 4 m no cuenta como recorrido de vuelo");
        Clean(); yield return null; space.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        game = Own(new GameObject("PartidaPruebasColisiones")).AddComponent<GameManagerRA>();
        Field(game, "globosParaGanar", 5); Field(game, "esperarSeguimientoAR", true); game.IniciarPartida();
        Field(space, "isCurrentlyTracked", true);
        var pausedPlane = Plane(new Vector3(0, 1.4f, -1)); Check(game.TryRegisterLaunch(pausedPlane), "Registra el lanzamiento con la partida existente");
        var pausedBalloon = Balloon(new Vector3(0, 1.4f, 0)); var pausedBird = Bird(pausedPlane.transform.position + Vector3.right * .27f);
        var pausedWall = Solid(new Vector3(0, 1.4f, -1));
        Field(space, "isCurrentlyTracked", false);
        pausedBalloon.PopBalloon(pausedPlane); pausedPlane.HandleContact(pausedWall); pausedBird.HandlePlaneCollision(pausedPlane);
        Check(pausedPlane.IsFlying && !pausedBalloon.IsPopped && pausedBird.Estado == PajaroInteraccion.EstadoRobo.Patrulla && score.CurrentScore == 0,
            "Sin tracking: globo, pájaro y obstáculo ignoran todos los contactos");
        pausedWall.enabled = false; pausedBird.DesaparecerPajaro(); Field(space, "isCurrentlyTracked", true);
        pausedBalloon.PopBalloon(pausedPlane);
        Check(pausedBalloon.IsPopped && score.CurrentScore == 1 && game.AvionesAcertados == 1 && game.AvionesFallados == 0,
            "Tracking recuperado: el impacto vuelve a funcionar con la puntuación real");
        Clean(); yield return null; game.IniciarPartida();
        phase = "Transacciones y listeners reentrantes";
        var reentrantPlane = Plane(new Vector3(0, 1.4f, -1)); game.TryRegisterLaunch(reentrantPlane);
        var original = Balloon(new Vector3(0, 1.4f, 0)); var extra = Balloon(new Vector3(1, 1.4f, 0));
        Action<int> listener = points => { original.PopBalloon(reentrantPlane); extra.PopBalloon(reentrantPlane); };
        ScoreManager.OnScoreUpdated += listener;
        original.PopBalloon(reentrantPlane); ScoreManager.OnScoreUpdated -= listener;
        Check(original.IsPopped && !extra.IsPopped && score.CurrentScore == 1 && game.AvionesAcertados == 1,
            "Un listener de puntuación que provoca otra colisión no duplica puntos ni globos");
        Check(finished[reentrantPlane] == 1 && game.AvionesEnVuelo == 0, "Transacción única compatible con el gestor de partida");
        Clean(); yield return null;
        Field(game, "globosParaGanar", 1); game.IniciarPartida();
        var winningPlane = Plane(new Vector3(0, 1.4f, -1)); game.TryRegisterLaunch(winningPlane);
        var winner = Balloon(new Vector3(0, 1.4f, 0)); winner.PopBalloon(winningPlane);
        Check(winner.IsPopped && game.HaGanado && game.AvionesAcertados == 1 && game.AvionesFallados == 0,
            "El globo decisivo activa explosión y victoria sin registrar un fallo");
        var late = Balloon(new Vector3(1, 1.4f, 0)); var latePlane = Plane(new Vector3(0, 1.4f, -1)); late.PopBalloon(latePlane);
        Check(!late.IsPopped && score.CurrentScore == 1 && !latePlane.CanInteract, "Fin de partida: los contactos tardíos no alteran resultado ni explosiones");
    }
}
#endif
