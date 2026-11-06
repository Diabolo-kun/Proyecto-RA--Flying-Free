#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using FlyingFree.Sprint2;

/// <summary>Pruebas reales en Play Mode con los prefabs y animaciones del proyecto.</summary>
public sealed class BirdSystemPlayModeProbe : MonoBehaviour
{
    readonly List<string> results = new List<string>();
    readonly List<GameObject> owned = new List<GameObject>();
    int finishedCount;
    bool failure;
    Camera cam;
    ARGameSpace space;
    string Output => Environment.GetEnvironmentVariable("FLYINGFREE_BIRD_REPORT") ?? Path.GetFullPath("Logs/PruebasPajaros/Pajaros_PlayMode.txt");
    IEnumerator Start()
    {
        Application.logMessageReceived += OnLog;
        Time.captureFramerate = 60;
        IEnumerator test = Cases();
        while (true)
        {
            bool more = false;
            try { more = test.MoveNext(); }
            catch (Exception ex) { failure = true; results.Add("FAIL " + ex); }
            if (!more || failure) break;
            yield return test.Current;
        }
        Application.logMessageReceived -= OnLog;
        foreach (var obj in owned) if (obj != null) Destroy(obj);
        Time.captureFramerate = 0;
        results.Add(failure ? "RESULTADO: ERROR" : "RESULTADO: TODAS LAS PRUEBAS PASAN");
        Directory.CreateDirectory(Path.GetDirectoryName(Output)); File.WriteAllLines(Output, results);
        Debug.Log(string.Join("\n", results));
        SessionState.SetInt("FlyingFree.Birds.Exit", failure ? 1 : 0);
        EditorApplication.isPlaying = false;
    }
    void OnLog(string message, string stack, LogType type)
    { if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) { failure = true; results.Add("ERROR UNITY " + message + "\n" + stack); } }
    void Check(bool ok, string description)
    { if (!ok) throw new Exception(description); results.Add("PASS " + description); }
    GameObject Own(GameObject obj) { owned.Add(obj); return obj; }
    static void Field(object obj, string name, object value) { obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value); }
    PajaroInteraccion Bird(Vector3 pos)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Pajaro_FlyingFree.prefab");
        var obj = Own(Instantiate(prefab, pos, Quaternion.identity, space.transform));
        var bird = obj.GetComponent<PajaroInteraccion>(); bird.Configurar(null, cam);
        bird.tiempoMinimoHuida = .5f; bird.tiempoMaximoHuida = 1.2f;
        return bird;
    }
    PaperPlane Plane(string name, Vector3 pos, Vector3 direction, float speed)
    {
        var obj = Own(Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + name + ".prefab"), pos, Quaternion.LookRotation(direction)));
        var motor = obj.GetComponent<PaperPlaneMotor>(); if (motor != null) motor.enabled = false;
        var plane = obj.GetComponent<PaperPlane>(); if (plane == null) plane = obj.AddComponent<PaperPlane>();
        plane.forwardSpeed = speed; plane.explosionPrefab = null; plane.Initialize(direction, space.transform);
        plane.OnFinished += hit =>
        {
            if (hit) throw new Exception("Un robo se notificó como acierto");
            if (plane.IsCaptured)
            {
                Vector3 predicted = plane.PreviousWorldPosition + plane.WorldVelocity * Mathf.Min(Time.deltaTime, .05f);
                if (Vector3.Distance(predicted, plane.transform.position) > .001f)
                    throw new Exception("El avión saltó de posición al cambiar de padre");
            }
            finishedCount++;
        };
        return plane;
    }
    void Screenshot(string name, Vector3 focus)
    {
        Vector3 oldPosition = cam.transform.position; Quaternion oldRotation = cam.transform.rotation;
        cam.transform.position = focus + new Vector3(.9f, .48f, -1.1f); cam.transform.LookAt(focus);
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.57f, .73f, .82f);
        var texture = new RenderTexture(1280, 720, 24); cam.targetTexture = texture; cam.Render();
        RenderTexture old = RenderTexture.active; RenderTexture.active = texture;
        var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false); pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
        File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(Output), name), pixels.EncodeToPNG());
        cam.targetTexture = null; RenderTexture.active = old; Destroy(texture); Destroy(pixels);
        cam.transform.SetPositionAndRotation(oldPosition, oldRotation);
    }
    IEnumerator Cases()
    {
        cam = Own(new GameObject("CamaraPruebas")).AddComponent<Camera>(); cam.tag = "MainCamera";
        cam.transform.position = new Vector3(0, 1.5f, -3); cam.transform.rotation = Quaternion.identity;
        space = Own(new GameObject("GameSpacePruebas")).AddComponent<ARGameSpace>(); space.enabled = false;
        space.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        var light = Own(new GameObject("LuzPruebas")).AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.4f;
        light.transform.rotation = Quaternion.Euler(45, -30, 0);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Pajaro_FlyingFree.prefab");
        Check(prefab != null, "Existe el prefab low poly original");
        var bandada = Own(new GameObject("BandadaPruebas")).AddComponent<ControlBandada>();
        bandada.camaraAR = cam; bandada.gameSpace = space; bandada.prefabPajaro = prefab;
        bandada.minimoActivos = 3; bandada.maximoActivos = 3; bandada.intervaloAparicion = .1f;
        bandada.radioVuelo = 1.6f; bandada.velocidad = .85f;
        var seen = new HashSet<PajaroOrbital>(); bool hidden = true, capped = true;
        for (int i = 0; i < 160; i++)
        {
            yield return null;
            capped &= bandada.PajarosActivosCount <= 3;
            foreach (var p in space.GetComponentsInChildren<PajaroOrbital>())
                if (seen.Add(p)) hidden &= Vector3.Dot(cam.transform.forward, p.transform.position - cam.transform.position) < -.3f;
        }
        Check(seen.Count == 3 && hidden && capped, "Aparición aleatoria fuera de vista y límite de tres pájaros");
        for (int i = 0; i < 20; i++) bandada.CrearPajaro();
        Check(bandada.PajarosActivosCount == 3, "El spawn manual también respeta el máximo");
        var sample = space.GetComponentInChildren<PajaroOrbital>(); Vector3 previous = sample.transform.position;
        for (int i = 0; i < 30; i++) yield return null;
        Check(Vector3.Distance(previous, sample.transform.position) > .1f, "Vuelo orbital real con oscilación y avance");
        bandada.testModeWithoutVuforia = false;
        Field(space, "hasEverBeenTracked", true); Field(space, "isCurrentlyTracked", false);
        yield return null; previous = sample.transform.position;
        for (int i = 0; i < 20; i++) yield return null;
        Check(Vector3.Distance(previous, sample.transform.position) < .0001f, "Pérdida de tracking pausa el vuelo aunque la marca se hubiese visto antes");
        Field(space, "isCurrentlyTracked", true);
        for (int i = 0; i < 20; i++) yield return null;
        Check(Vector3.Distance(previous, sample.transform.position) > .05f, "El vuelo se reanuda al recuperar tracking");
        bandada.enabled = false; bandada.LimpiarPajaros(); yield return null;
        Check(bandada.PajarosActivosCount == 0 && space.GetComponentsInChildren<PajaroOrbital>().Length == 0, "Limpieza completa de la bandada");
        Destroy(bandada.gameObject); yield return null;

        foreach (string name in new[] { "Avion_Cuaderno", "Avion_Rayo", "Avion_Brisa", "Avion_AsCorazones", "Avion_Estandar" })
        {
            finishedCount = 0;
            var plane = Plane(name, new Vector3(0, 1.4f, -1.5f), Vector3.forward, 1.2f);
            Vector3 scale = plane.transform.lossyScale;
            var bird = Bird(plane.transform.position + Vector3.right * .55f + Vector3.up * .15f);
            Check(bird.beakSocket.name == "PaperGrip_SOCKET", name + ": socket lateral auténtico");
            var other = Bird(plane.transform.position + Vector3.left * .55f);
            Check(bird.IntentarInterceptar(plane) && !other.IntentarInterceptar(plane), name + ": reserva exclusiva frente a otro pájaro");
            Destroy(other.gameObject);
            for (int i = 0; i < 120 && plane != null && !plane.IsCaptured; i++) yield return null;
            Check(plane != null && plane.IsCaptured && bird.HasCapturedPlane, name + ": intercepción y robo reales en Play Mode");
            Check(finishedCount == 1 && !plane.IsFlying, name + ": una sola notificación de avión perdido");
            Check(Vector3.Distance(scale, plane.transform.lossyScale) < .001f, name + ": conserva el tamaño al sujetarlo");
            Check(plane.transform.parent == bird.beakSocket, name + ": sujeto al socket animado");
            Check(Mathf.Abs(Vector3.Dot(bird.transform.forward, plane.transform.forward)) < .6f, name + ": agarre desde el costado y no desde atrás");
            Check(bird.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Capture"), name + ": se reproduce la animación de captura");
            Check(Vector3.Distance(plane.transform.localPosition, plane.CaptureGripOffset) < .001f, name + ": agarre alineado sin salto de posición");
            if (name == "Avion_Cuaderno") Screenshot("Robo_Lateral_Unity.png", (bird.transform.position + plane.transform.position) * .5f);
            // Un callback físico ya encolado de un globo no debe sumar puntos con un avión robado.
            var score = Own(new GameObject("ScorePrueba")).AddComponent<ScoreManager>();
            var balloonObject = Own(new GameObject("GloboPrueba")); balloonObject.AddComponent<SphereCollider>();
            var balloon = balloonObject.AddComponent<GloboTarget>(); balloon.PopBalloon(plane);
            Check(score.CurrentScore == 0, name + ": el avión robado no explota globos ni suma puntos");
            Destroy(balloonObject); Destroy(score.gameObject);
            foreach (var col in plane.GetComponentsInChildren<Collider>()) Check(!col.enabled, name + ": collider desactivado durante el robo");
            plane.OnHitBalloon(); bird.HandlePlaneCollision(plane); plane.CaptureByBird(bird.beakSocket);
            Check(finishedCount == 1, name + ": callbacks repetidos no duplican la pérdida");
            Vector3 start = bird.transform.position;
            for (int i = 0; i < 12; i++) yield return null;
            Check(bird != null && Vector3.Distance(start, bird.transform.position) > .1f, name + ": huida con el avión");
            for (int i = 0; i < 100; i++) yield return null;
            Check(bird == null && plane == null, name + ": desaparecen juntos sin dejar el avión huérfano");
        }
        // Cancelación de un objetivo destruido o de un pájaro desactivado.
        var cancelPlane = Plane("Avion_Cuaderno", new Vector3(0, 1.4f, -1.5f), Vector3.forward, 1);
        var cancelBird = Bird(cancelPlane.transform.position + Vector3.right * .6f);
        Check(cancelBird.IntentarInterceptar(cancelPlane), "Inicio del intento cancelable");
        cancelBird.enabled = false;
        Check(!cancelPlane.IsReservedByOtherBird(this), "Desactivar un pájaro libera su reserva");
        Destroy(cancelBird.gameObject); Destroy(cancelPlane.gameObject); yield return null;
        cancelPlane = Plane("Avion_Cuaderno", new Vector3(0, 1.4f, -1.5f), Vector3.forward, 1);
        cancelBird = Bird(cancelPlane.transform.position + Vector3.right * .6f);
        cancelBird.IntentarInterceptar(cancelPlane); Destroy(cancelPlane.gameObject);
        for (int i = 0; i < 100; i++) yield return null;
        Check(cancelBird == null, "Objetivo destruido durante aproximación: huida y limpieza sin excepciones");
        Check(PaperPlane.ActivePlanes.Count == 0, "Registro de aviones limpio después de destruirlos");
        // Intercepción automática de un avión que cruza el trigger a alta velocidad.
        finishedCount = 0;
        var fastPlane = Plane("Avion_Rayo", new Vector3(-.6f, 1.4f, -1.5f), Vector3.right, 40);
        var fastBird = Bird(new Vector3(0, 1.4f, -1.5f));
        for (int i = 0; i < 90 && fastPlane != null && !fastPlane.IsCaptured; i++) yield return null;
        Check(fastPlane != null && fastPlane.IsCaptured && finishedCount == 1, "Intercepción automática a 40 m/s sin pulsar captura");
        for (int i = 0; i < 100; i++) yield return null;
        Check(fastPlane == null && fastBird == null, "Limpieza del robo a alta velocidad");
        // La referencia AR se desplaza/rota; el avión llevado debe conservar su pose relativa al socket.
        finishedCount = 0;
        var trackedPlane = Plane("Avion_Brisa", new Vector3(0, 1.4f, -1.5f), Vector3.forward, 1);
        var trackedBird = Bird(trackedPlane.transform.position + Vector3.right * .55f);
        trackedBird.IntentarInterceptar(trackedPlane);
        for (int i = 0; i < 90 && !trackedPlane.IsCaptured; i++) yield return null;
        Check(trackedPlane.IsCaptured, "Preparación del robo para relocalización AR");
        Vector3 relativeGrip = trackedPlane.transform.localPosition;
        space.transform.SetPositionAndRotation(new Vector3(3, .5f, -2), Quaternion.Euler(0, 70, 0));
        yield return null;
        Check(Vector3.Distance(relativeGrip, trackedPlane.transform.localPosition) < .0001f, "Relocalización AR conserva el agarre lateral sin saltos");
        Destroy(trackedBird.gameObject); yield return null;
        space.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        var game = Own(new GameObject("PartidaPrueba")).AddComponent<GameManagerRA>();
        bandada = Own(new GameObject("BandadaFinPrueba")).AddComponent<ControlBandada>();
        bandada.camaraAR = cam; bandada.gameSpace = space; bandada.prefabPajaro = prefab;
        for (int i = 0; i < 150; i++) yield return null;
        Check(bandada.PajarosActivosCount > 0, "Bandada conectada a una partida en curso");
        typeof(GameManagerRA).GetMethod("FinalizarPartida", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, null);
        yield return null; yield return null;
        Check(bandada.PajarosActivosCount == 0 && !bandada.PuedeSimular, "Fin de partida elimina pájaros e impide nuevas apariciones");
    }
}
#endif
