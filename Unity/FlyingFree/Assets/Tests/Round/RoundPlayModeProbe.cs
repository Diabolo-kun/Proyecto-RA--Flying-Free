#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public sealed class RoundPlayModeProbe : MonoBehaviour
{
    readonly List<string> results = new List<string>();
    readonly List<GameObject> owned = new List<GameObject>();
    GameManagerRA game;
    TouchFlightController controller;
    ARGameSpace space;
    Camera cam;
    int finishedEvents;
    bool failed;
    string expectedError;
    string Output => Environment.GetEnvironmentVariable("FLYINGFREE_ROUND_REPORT") ?? Path.GetFullPath("Logs/PruebasPartida/Partida_PlayMode.txt");
    IEnumerator Start()
    {
        Application.logMessageReceived += OnLog; Time.captureFramerate = 60;
        IEnumerator tests = Cases();
        while (true)
        {
            bool more = false;
            try { more = tests.MoveNext(); } catch (Exception ex) { failed = true; results.Add("FAIL " + ex); }
            if (!more || failed) break;
            yield return tests.Current;
        }
        Application.logMessageReceived -= OnLog; Time.captureFramerate = 0;
        foreach (var obj in owned) if (obj != null) Destroy(obj);
        results.Add(failed ? "RESULTADO: ERROR" : "RESULTADO: TODAS LAS PRUEBAS PASAN");
        Directory.CreateDirectory(Path.GetDirectoryName(Output)); File.WriteAllLines(Output, results); Debug.Log(string.Join("\n", results));
        SessionState.SetInt("FlyingFree.Round.Exit", failed ? 1 : 0); EditorApplication.isPlaying = false;
    }
    void OnLog(string text, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        if (expectedError != null && text.Contains(expectedError)) { expectedError = null; return; }
        failed = true; results.Add("ERROR UNITY " + text + "\n" + stack);
    }
    void Check(bool ok, string label) { if (!ok) throw new Exception(label); results.Add("PASS " + label); }
    static void Field(object obj, string name, object value) { obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value); }
    static object Value(object obj, string name) => obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
    GameObject Own(GameObject obj) { owned.Add(obj); return obj; }
    GameObject Prefab(string name) => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + name + ".prefab");
    void ResetRound(int target = 5, float seconds = 30)
    {
        Field(game,"avionesIlimitados",true); Field(game,"globosParaGanar",target); Field(game,"tiempoDeJuego",seconds);
        Field(game,"objetivoProporcionalAlTiempo",false);Field(game,"esperarSeguimientoAR",false);
        game.IniciarPartida(); finishedEvents = 0;
    }
    PaperPlane Launch()
    {
        if (!controller.TryLaunch(new Vector2(Screen.width * .5f, Screen.height * .5f))) throw new Exception("No se pudo lanzar desde el controlador real");
        var plane = controller.CurrentPlane; plane.forwardSpeed = 0; plane.explosionPrefab = null; return plane;
    }
    GloboTarget Balloon()
    {
        var obj = Own(Instantiate(Prefab("Globo_Explosion"),new Vector3(20,20,20),Quaternion.identity));
        var balloon = obj.GetComponent<GloboTarget>(); if(balloon==null)balloon=obj.AddComponent<GloboTarget>(); return balloon;
    }
    void Snapshot(string name, int width = 1080, int height = 1920)
    {
        string path=Path.Combine(Path.GetDirectoryName(Output),name);Directory.CreateDirectory(Path.GetDirectoryName(path));
        var canvas=GameObject.Find("HUD_Partida").GetComponent<Canvas>();
        var previousMode=canvas.renderMode;var previousCamera=canvas.worldCamera;
        var rt=new RenderTexture(width,height,24);cam.targetTexture=rt;cam.aspect=(float)width/height;
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;
        Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;
        var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());
        RenderTexture.active=null;cam.targetTexture=null;canvas.renderMode=previousMode;canvas.worldCamera=previousCamera;
        Destroy(rt);Destroy(pixels);cam.ResetAspect();Canvas.ForceUpdateCanvases();
    }
    IEnumerator Cases()
    {
        game = GameManagerRA.Instance;
        cam = Own(new GameObject("CamaraPruebasPartida")).AddComponent<Camera>();cam.tag="MainCamera";
        cam.transform.position=new Vector3(0,1.5f,-3);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.5f,.7f,.78f);
        space=Own(new GameObject("GameSpacePruebasPartida")).AddComponent<ARGameSpace>();space.enabled=false;
        controller=Own(new GameObject("ControlLanzamientosPrueba")).AddComponent<TouchFlightController>();
        Field(controller,"cam",cam);Field(controller,"arReference",space.transform);Field(controller,"planePrefab",Prefab("Avion_Cuaderno"));
        game.OnPartidaFinalizada += (win,score) => finishedEvents++;
        yield return null;
        ResetRound();
        Check(game.AvionesIlimitados && game.AvionesLanzados==0 && game.PuedeLanzar,"Ronda inicial: lanzamientos ilimitados y contador a cero");
        Check(ScoreManager.Instance.CurrentScore==0 && game.GlobosParaGanar==5,"Objetivo y puntuación iniciales correctos");
        Check(((TMP_Text)Value(game,"textoAviones")).text.Contains("∞"),"HUD muestra infinito en lugar de cinco municiones");
        Snapshot("HUD_Partida.png");
        Snapshot("HUD_Partida_Horizontal.png",1920,1080);
        var plane=Launch();
        Check(game.AvionesLanzados==1 && game.AvionesEnVuelo==1 && !game.PuedeLanzar,"Un lanzamiento válido actualiza contador y vuelo activo");
        Check(!controller.TryLaunch(Vector2.zero) && game.AvionesLanzados==1 && controller.CurrentPlane==plane,"Un segundo toque no destruye ni sustituye el avión en vuelo");
        typeof(TouchFlightController).GetMethod("BeginTouch",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(controller,new object[]{Vector2.zero});
        Check((bool)Value(controller,"holding") && game.AvionesLanzados==1,"Un segundo toque permite retomar el joystick sin lanzar otro avión");
        plane.ExplodeInAir();plane.ExplodeInAir();plane.OnHitBalloon();
        Check(game.AvionesFallados==1 && game.AvionesPerdidos==1 && game.AvionesEnVuelo==0,"Choque repetido cuenta una única pérdida");
        Check(controller.CurrentPlane==null && game.PuedeLanzar,"Un fallo libera el control y permite volver a lanzar");
        for(int i=0;i<24;i++){var p=Launch();p.ExplodeInAir();}
        Check(game.AvionesLanzados==25 && game.AvionesFallados==25 && game.PartidaEnCurso && game.PuedeLanzar,"Se pueden realizar más de cinco lanzamientos sin derrota por munición");
        Check(ScoreManager.Instance.CurrentScore==0,"Los fallos no suman puntuación");
        controller.enabled=false;int count=game.AvionesLanzados;
        Check(!controller.TryLaunch(Vector2.zero) && game.AvionesLanzados==count,"Un controlador desactivado no registra lanzamientos");controller.enabled=true;
        Field(controller,"planePrefab",null);Field(controller,"avionesElegibles",new GameObject[0]);expectedError="'Plane Prefab' está vacío";
        Check(!controller.TryLaunch(Vector2.zero) && game.AvionesLanzados==count && expectedError==null,"Un prefab ausente no consume ni registra un avión");
        Field(controller,"planePrefab",Prefab("Avion_Cuaderno"));
        ResetRound();
        foreach(string model in new[]{"Avion_Cuaderno","Avion_Rayo","Avion_Brisa","Avion_AsCorazones","Avion_Estandar"})
        {
            Field(controller,"planePrefab",Prefab(model));plane=Launch();var balloon=Balloon();int before=ScoreManager.Instance.CurrentScore;
            balloon.PopBalloon(plane);balloon.PopBalloon(plane);Balloon().PopBalloon(plane);
            Check(ScoreManager.Instance.CurrentScore==before+1,model+": un globo suma exactamente una vez");
            Check(game.AvionesAcertados==before+1 && game.AvionesPerdidos==0,model+": el acierto no se registra como pérdida");
        }
        Check(game.HaGanado && !game.PartidaEnCurso && finishedEvents==1,"Victoria inmediata al alcanzar cinco globos, una sola vez");
        Check(((GameObject)Value(game,"canvasVictoria")).activeSelf && !((GameObject)Value(game,"canvasDerrota")).activeSelf,"Panel de victoria visible y derrota oculta");
        Check(!controller.TryLaunch(Vector2.zero),"No se puede lanzar después de ganar");
        ScoreManager.Instance.AddScore(99);Check(ScoreManager.Instance.CurrentScore==5 && finishedEvents==1,"Los puntos tardíos no alteran el resultado final");
        Snapshot("Victoria_Partida.png");
        Snapshot("Victoria_Partida_Horizontal.png",1920,1080);
        ResetRound(5,.05f);plane=Launch();
        for(int i=0;i<8;i++)yield return null;
        Check(!game.PartidaEnCurso && !game.HaGanado && finishedEvents==1 && game.TiempoRestante==0,"Derrota por tiempo agotado con aviones ilimitados");
        Check(game.AvionesEnVuelo==0 && game.AvionesFallados==1 && (plane==null || !plane.IsFlying),"El avión pendiente al agotarse el tiempo queda resuelto sin doble pérdida");
        Check(!controller.TryLaunch(Vector2.zero) && ((GameObject)Value(game,"canvasDerrota")).activeSelf,"Derrota bloquea lanzamientos y muestra su panel");
        ScoreManager.Instance.AddScore(1);Check(ScoreManager.Instance.CurrentScore==0,"No se pueden sumar puntos después de perder");
        Snapshot("Derrota_Partida.png");
        ResetRound(1);plane=Launch();Balloon().PopBalloon(plane);
        Check(game.HaGanado && game.AvionesAcertados==1 && game.AvionesFallados==0,"El punto decisivo se resuelve antes de cerrar la partida");
        ResetRound();plane=Launch();Destroy(plane.gameObject);yield return null;yield return null;
        Check(game.AvionesFallados==1 && game.AvionesEnVuelo==0 && game.PuedeLanzar,"Una destrucción externa del avión no deja bloqueada la partida");
        plane=Launch();plane.gameObject.SetActive(false);
        Check(game.AvionesFallados==2 && game.AvionesEnVuelo==0,"Una desactivación externa también resuelve el vuelo una vez");
        ResetRound();plane=Launch();Field(game,"duracionMaximaVuelo",.05f);
        for(int i=0;i<8;i++)yield return null;
        Check(game.AvionesFallados==1 && game.PuedeLanzar && game.PartidaEnCurso,"Un avión que tarda demasiado libera el siguiente intento antes de acabar la ronda");
        Field(game,"duracionMaximaVuelo",6f);ResetRound();plane=Launch();
        // Cruzar la pared sin acertar también es un fallo, en lugar de esperar veinte segundos.
        plane.transform.position=space.transform.TransformPoint(new Vector3(0,0,2));
        Field(plane,"localPosition",new Vector3(0,0,2));
        for(int i=0;i<14;i++)yield return null;
        Check(game.AvionesFallados==1 && game.PuedeLanzar,"Un avión que pasa de largo la marca se registra como fallo y permite reintentar");
        ResetRound();Field(game,"esperarSeguimientoAR",true);Field(space,"isCurrentlyTracked",false);
        float time=game.TiempoRestante;
        for(int i=0;i<20;i++)yield return null;
        Check(game.EsperandoSeguimiento && !game.PuedeLanzar && Mathf.Abs(game.TiempoRestante-time)<.0001f,"Antes de reconocer la marca no se pierde tiempo ni se puede lanzar");
        Field(space,"isCurrentlyTracked",true);yield return null;plane=Launch();plane.forwardSpeed=1;
        for(int i=0;i<6;i++)yield return null;
        Field(space,"isCurrentlyTracked",false);yield return null;Vector3 position=plane.transform.position;time=game.TiempoRestante;
        for(int i=0;i<20;i++)yield return null;
        Check(Vector3.Distance(position,plane.transform.position)<.0001f && Mathf.Abs(game.TiempoRestante-time)<.0001f,"Pérdida de tracking pausa reloj y avión libre");
        var pausedBalloon=Balloon();pausedBalloon.PopBalloon(plane);
        Check(ScoreManager.Instance.CurrentScore==0 && plane.IsFlying,"Una colisión durante la pausa no suma puntos ni elimina el avión");
        Field(space,"isCurrentlyTracked",true);
        for(int i=0;i<6;i++)yield return null;
        Check(game.TiempoRestante<time && Vector3.Distance(position,plane.transform.position)>.02f,"Tracking recuperado reanuda reloj y vuelo");
        ResetRound();
        Check(game.AvionesLanzados==0 && game.AvionesEnVuelo==0 && game.AvionesPerdidos==0 && ScoreManager.Instance.CurrentScore==0,"Reiniciar limpia todos los contadores y vuelos anteriores");
        Check(!((GameObject)Value(game,"canvasVictoria")).activeSelf && !((GameObject)Value(game,"canvasDerrota")).activeSelf,"Reiniciar oculta los paneles anteriores");
        foreach(var button in GameObject.Find("HUD_Partida").GetComponentsInChildren<Button>(true))
            Check(button.onClick.GetPersistentEventCount()==1 && button.onClick.GetPersistentTarget(0)==game,"Botón real conectado al gestor: "+button.name);
        // Robo real, usando el pájaro y el socket de la tarea 3.
        plane=Launch();
        var birdObject=Own(Instantiate(Prefab("Pajaro_FlyingFree"),plane.transform.position+Vector3.right*.55f,Quaternion.identity));
        var bird=birdObject.GetComponent<PajaroInteraccion>();bird.Configurar(null,cam);bird.IntentarInterceptar(plane);
        for(int i=0;i<100 && !plane.IsCaptured;i++)yield return null;
        Check(plane.IsCaptured && game.AvionesRobados==1 && game.AvionesFallados==0,"Robo lateral real cuenta como robo y no como fallo");
        Check(game.PuedeLanzar && controller.CurrentPlane==null && game.PartidaEnCurso,"Después de un robo se puede lanzar de nuevo con suministro infinito");
        plane.OnHitBalloon();Check(game.AvionesRobados==1 && game.AvionesAcertados==0,"Un callback tardío no convierte un robo en acierto");
        ResetRound();yield return null;
        Check(plane==null && game.AvionesLanzados==0,"Reiniciar también retira el avión que estaba en el pico del pájaro");
        var duplicate=Own(new GameObject("DuplicadoPrueba"));duplicate.AddComponent<ScoreManager>();duplicate.AddComponent<GameManagerRA>();yield return null;
        Check(GameManagerRA.Instance==game && duplicate!=null,"Los gestores duplicados no destruyen el gestor principal ni su GameObject");
        ScoreManager.Instance.AddScore(0);ScoreManager.Instance.AddScore(-5);
        Check(ScoreManager.Instance.CurrentScore==0,"Puntos nulos o negativos no corrompen el marcador");
        int eventCount=0;Action<int> listener=s=>eventCount++;ScoreManager.OnScoreUpdated+=listener;
        ResetRound();ScoreManager.Instance.AddScore(1);ScoreManager.OnScoreUpdated-=listener;
        Check(eventCount==2 && ScoreManager.Instance.CurrentScore==1,"Reinicio y suma emiten un solo evento cada uno, sin suscripciones duplicadas");
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded+=PrepararNavegacion;
        var previousGame=game;
        game.VolverAJugar();yield return null;yield return null;
        game=GameManagerRA.Instance;
        results.Add("INFO Recarga: escena="+SceneManager.GetActiveScene().name+" gestor="+(game!=null)+" mismaInstancia="+ReferenceEquals(game,previousGame));
        Check(SceneManager.GetActiveScene().name=="GameRA" && game!=null && !ReferenceEquals(game,previousGame),"Volver a jugar recarga realmente GameRA con un gestor nuevo");
        Check(game.AvionesIlimitados && game.AvionesLanzados==0 && ScoreManager.Instance.CurrentScore==0 && game.PartidaEnCurso,"La recarga real empieza una ronda limpia, con aviones infinitos");
        Check(game.EsperandoSeguimiento && Mathf.Abs(game.TiempoRestante-30)<.001f,"La ronda recargada espera la marca sin gastar sus treinta segundos");
        game.VolverAlMenuPrincipal();yield return null;yield return null;
        Check(SceneManager.GetActiveScene().name=="FlyingFreeMenu" && GameManagerRA.Instance==null,"Volver al menú carga la escena correcta y libera el gestor anterior");
        SceneManager.sceneLoaded-=PrepararNavegacion;
    }
    void PrepararNavegacion(Scene scene,LoadSceneMode mode)
    {
        if(scene.name!="GameRA")return;
        foreach(var behaviour in FindObjectsByType<Behaviour>(FindObjectsSortMode.None))
            if(behaviour.GetType().Namespace!=null && behaviour.GetType().Namespace.StartsWith("Vuforia"))behaviour.enabled=false;
        foreach(var spawner in FindObjectsByType<TargetPlaneSpawner>(FindObjectsSortMode.None))spawner.enabled=false;
        foreach(var flock in FindObjectsByType<ControlBandada>(FindObjectsSortMode.None))flock.enabled=false;
    }
}
#endif
