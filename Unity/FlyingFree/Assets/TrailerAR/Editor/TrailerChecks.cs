using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using FlyingFree.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FlyingFree.Trailer.Editor
{
    [InitializeOnLoad]
    public static class TrailerChecks
    {
        static IEnumerator routine;
        static readonly List<string> report=new List<string>();
        static double readyAt;
        static TrailerChecks(){EditorApplication.update+=Tick;readyAt=EditorApplication.timeSinceStartup+2;}
        public static void Run()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(TrailerBuilder.MenuPrefab);
            var menu=prefab.GetComponent<FlyingFreeMenu>();
            if(menu.mainPage.GetComponentsInChildren<Button>(true).Length!=4)throw new Exception("El menú debe tener cuatro botones.");
            if(menu.onPlayRequested.GetPersistentMethodName(0)!="LoadGameRA")throw new Exception("Se ha perdido la conexión original de JUGAR.");
            Directory.CreateDirectory("TrailerEntrega");
            EditorSceneManager.OpenScene("Assets/Scenes/TrailerPreview.unity");
            SessionState.SetBool("FlyingFree.TrailerChecks",true);EditorApplication.EnterPlaymode();
        }
        public static void PreviewMenu()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/FlyingFreeMenu.unity");
            var menu=UnityEngine.Object.FindFirstObjectByType<FlyingFreeMenu>();
            CaptureMenu(menu,"TrailerEntrega/Vista_Previa_Menu.png");
            CaptureCanvas(menu.GetComponent<Canvas>(),Camera.main??UnityEngine.Object.FindFirstObjectByType<Camera>(),"TrailerEntrega/Vista_Previa_Menu_Vertical.png",720,1280);
            Debug.Log("TRAILER_MENU_PREVIEW_OK");
        }
        static void Tick()
        {
            if(!SessionState.GetBool("FlyingFree.TrailerChecks",false)||!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<readyAt)return;
            try
            {
                if(routine==null)routine=Verify();
                if(!routine.MoveNext())
                {
                    File.WriteAllLines("TrailerEntrega/Comprobaciones.txt",report);Debug.Log("TRAILER_CHECKS_OK\n"+string.Join("\n",report));
                    SessionState.SetBool("FlyingFree.TrailerChecks",false);EditorApplication.Exit(0);
                }
            }
            catch(Exception ex)
            {
                File.WriteAllText("TrailerEntrega/Comprobaciones.txt",string.Join("\n",report)+"\nFAIL "+ex);Debug.LogException(ex);
                SessionState.SetBool("FlyingFree.TrailerChecks",false);EditorApplication.Exit(1);
            }
        }
        static void Check(bool value,string message){if(!value)throw new Exception(message);report.Add("PASS "+message);}
        static void Click(Button button){ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler);}
        static IEnumerator Verify()
        {
            var player=UnityEngine.Object.FindFirstObjectByType<TrailerPlayer>();
            var preview=UnityEngine.Object.FindFirstObjectByType<TrailerPreview>();
            Check(player!=null&&preview!=null,"Escena de simulación cargada con el vídeo real");
            double timeout=EditorApplication.timeSinceStartup+40;
            while(!player.Ready){if(EditorApplication.timeSinceStartup>timeout)throw new Exception("No se ha podido preparar el vídeo real.");yield return null;}
            Check(player.Duration>1&&player.video.canSetTime,"Vídeo preparado, duración y búsqueda disponibles");
            Check(!player.video.isPlaying&&!player.MarkerVisible,"No se reproduce antes de reconocer la marca");
            // A batch-mode Editor is unfocused; explicitly model a foreground application.
            player.SendMessage("OnApplicationFocus",true);
            Click(preview.toggleMarker);
            double until=EditorApplication.timeSinceStartup+6;while(player.CurrentTime<.2&&EditorApplication.timeSinceStartup<until)yield return null;
            Check(player.video.isPlaying&&player.CurrentTime>.1,"La marca encontrada inicia la reproducción (visible="+player.MarkerVisible+", wanted="+player.WantsPlayback+", time="+player.CurrentTime+", estado="+player.statusText.text+")");
            Click(player.pauseButton);double paused=player.CurrentTime;
            until=EditorApplication.timeSinceStartup+1;while(EditorApplication.timeSinceStartup<until)yield return null;
            Check(!player.video.isPlaying&&Math.Abs(player.CurrentTime-paused)<.2,"Pausa conserva el fotograma");
            Click(preview.toggleMarker);Click(preview.toggleMarker);
            yield return null;
            Check(!player.WantsPlayback&&!player.video.isPlaying,"Perder y recuperar marca no anula una pausa manual");
            Click(player.pauseButton);Click(player.forwardButton);
            timeout=EditorApplication.timeSinceStartup+12;while(player.Seeking){if(EditorApplication.timeSinceStartup>timeout)throw new Exception("Timeout +10 segundos");yield return null;}
            Check(player.CurrentTime>9&&player.video.isPlaying,"Avance de 10 segundos conserva reproducción");
            Click(player.rewindButton);
            timeout=EditorApplication.timeSinceStartup+12;while(player.Seeking){if(EditorApplication.timeSinceStartup>timeout)throw new Exception("Timeout -10 segundos");yield return null;}
            Check(player.CurrentTime<3,"Retroceso de 10 segundos");
            Click(preview.toggleMarker);paused=player.CurrentTime;
            until=EditorApplication.timeSinceStartup+1;while(EditorApplication.timeSinceStartup<until)yield return null;
            Check(!player.video.isPlaying&&Math.Abs(player.CurrentTime-paused)<.2&&!player.surfaceRoot.gameObject.activeSelf,"Perder marca pausa audio/vídeo y oculta la pantalla RA");
            Click(preview.toggleMarker);yield return null;
            Check(player.WantsPlayback,"Recuperar marca conserva la intención de reanudar");
            player.BeginScrub();player.progress.value=.5f;player.EndScrub();
            timeout=EditorApplication.timeSinceStartup+12;while(player.Seeking){if(EditorApplication.timeSinceStartup>timeout)throw new Exception("Timeout barra de progreso");yield return null;}
            Check(Math.Abs(player.CurrentTime-player.Duration*.5)<1.5,"Arrastrar la barra busca una posición real del vídeo");
            player.SeekTo(2);player.SeekTo(8);player.SeekTo(5);
            timeout=EditorApplication.timeSinceStartup+12;while(player.Seeking){if(EditorApplication.timeSinceStartup>timeout)throw new Exception("Timeout solicitudes de avance");yield return null;}
            Check(Math.Abs(player.CurrentTime-5)<1.5,"Pulsaciones rápidas conservan la última posición solicitada");
            yield return null;yield return null;
            Capture(player,"TrailerEntrega/Vista_Previa_Trailer.png");
            player.SeekTo(player.Duration-.25);
            timeout=EditorApplication.timeSinceStartup+12;while(player.Seeking){if(EditorApplication.timeSinceStartup>timeout)throw new Exception("Timeout final de vídeo");yield return null;}
            until=EditorApplication.timeSinceStartup+1.5;while(EditorApplication.timeSinceStartup<until)yield return null;
            Check(player.Ended&&!player.WantsPlayback,"Al terminar se muestra la opción de volver a ver");
            Click(player.pauseButton);
            timeout=EditorApplication.timeSinceStartup+12;while(player.Seeking){if(EditorApplication.timeSinceStartup>timeout)throw new Exception("Timeout repetir vídeo");yield return null;}
            Check(!player.Ended&&player.WantsPlayback,"Repetir vuelve al inicio");
            Click(player.menuButton);
            timeout=EditorApplication.timeSinceStartup+20;while(SceneManager.GetActiveScene().name!="FlyingFreeMenu"){if(EditorApplication.timeSinceStartup>timeout)throw new Exception("Timeout regreso al menú");yield return null;}
            yield return null;
            var menu=UnityEngine.Object.FindFirstObjectByType<FlyingFreeMenu>();
            Check(menu!=null&&menu.mainPage.GetComponentsInChildren<Button>().Length==4,"Regreso al menú con JUGAR, TRÁILER, CONFIGURACIÓN y SALIR");
            CaptureMenu(menu,"TrailerEntrega/Vista_Previa_Menu.png");
            Check(UnityEngine.Object.FindFirstObjectByType<TrailerPlayer>()==null,"Volver al menú descarga el reproductor");
        }
        static void Capture(TrailerPlayer player,string path)
        {
            // Capture the actual decoder texture plus the rendered AR surface with its UI.
            var texture=player.video.texture as RenderTexture;
            if(texture!=null)Save(texture,"TrailerEntrega/Fotograma_Video.png");
            CaptureCanvas(UnityEngine.Object.FindFirstObjectByType<TrailerHudLayout>().GetComponent<Canvas>(),Camera.main,path);
        }
        static void CaptureMenu(FlyingFreeMenu menu,string path){CaptureCanvas(menu.GetComponent<Canvas>(),Camera.main??UnityEngine.Object.FindFirstObjectByType<Camera>(),path);}
        static void CaptureCanvas(Canvas canvas,Camera camera,string path,int width=1280,int height=720)
        {
            if(camera==null)return;
            var target=new RenderTexture(width,height,24);camera.targetTexture=target;camera.aspect=(float)width/height;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;camera.nearClipPlane=.01f;canvas.planeDistance=.02f;
            canvas.GetComponent<CanvasScaler>().enabled=false;canvas.scaleFactor=1;
            // Force the native Canvas geometry to adopt the new render target before layout.
            Canvas.ForceUpdateCanvases();Render(camera,target);Canvas.ForceUpdateCanvases();
            var layout=canvas.GetComponent<TrailerHudLayout>();if(layout!=null)layout.Apply(new Vector2(width,height),new UnityEngine.Rect(0,0,width,height));
            var menuLayout=canvas.GetComponent<ResponsiveMenu>();if(menuLayout!=null){menuLayout.enabled=false;menuLayout.ApplyViewport(new Vector2(width,height),new UnityEngine.Rect(0,0,width,height));}
            Canvas.ForceUpdateCanvases();
            Render(camera,target);
            Save(target,path);camera.targetTexture=null;
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;UnityEngine.Object.DestroyImmediate(target);
        }
        static void Render(Camera camera,RenderTexture target)
        {
            if(GraphicsSettings.currentRenderPipeline!=null)RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});else camera.Render();
        }
        static void Save(RenderTexture target,string path)
        {
            var old=RenderTexture.active;RenderTexture.active=target;
            var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);image.ReadPixels(new UnityEngine.Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=old;
        }
    }
}
