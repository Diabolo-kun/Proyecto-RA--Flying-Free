using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FlyingFree.UI.Editor
{
    [InitializeOnLoad]
    public static class FlyingFreeQA
    {
        static readonly string Output=Path.GetFullPath("../deliverables/Previews-v2");
        static readonly List<string> report=new List<string>();
        static double readyAt;
        static FlyingFreeQA()
        {
            readyAt=EditorApplication.timeSinceStartup+2;
            EditorApplication.update += Poll;
        }
        static void Poll()
        {
            if (!SessionState.GetBool("FlyingFree.QA",false) || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup<readyAt) return;
            EditorApplication.update-=Poll;
            Verify();
        }
        public static void Run()
        {
            Directory.CreateDirectory(Output);
            PlayerPrefs.DeleteKey("FlyingFree.UI.Sound");PlayerPrefs.DeleteKey("FlyingFree.UI.Music");PlayerPrefs.DeleteKey("FlyingFree.UI.Muted");PlayerPrefs.Save();
            EditorSceneManager.OpenScene(FlyingFreeBuilder.Root+"/Scenes/FlyingFreeMenu.unity");
            SessionState.SetBool("FlyingFree.QA",true);
            EditorApplication.EnterPlaymode();
        }
        static void Check(bool condition,string message)
        {
            if(!condition)throw new Exception(message);
            report.Add("PASS " + message);
        }
        static void Click(Button button)
        {
            ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler);
        }
        static void Verify()
        {
            try
            {
                var menu=UnityEngine.Object.FindFirstObjectByType<FlyingFreeMenu>();
                Check(menu!=null,"Escena y referencias cargadas en Play Mode");
                var bg=menu.GetComponentInChildren<MenuBackground>();
                Check(bg!=null&&bg.landscape!=null&&bg.portrait!=null,"Fondos de parque para ambas orientaciones presentes");
                var logo=menu.transform.Find("Safe Area/Menú principal/Marca/Logo original Flying Free").GetComponent<Image>();
                Check(logo.sprite!=null&&logo.sprite.name=="FlyingFreeLogo","Logo original asignado");
                Check(((Image)menu.playButton.targetGraphic).sprite.border.x>0,"Sprite de botones conserva nine-slicing");
                Check(menu.playButton.GetComponentInChildren<MenuIconGraphic>().symbol==MenuIconGraphic.Symbol.Play,"Icono JUGAR independiente del texto");
                Check(EventSystem.current!=null,"EventSystem creado automáticamente");
                Check(menu.mainPage.GetComponentsInChildren<Button>().Length==3,"Exactamente tres botones principales");
                int plays=0,exits=0;menu.onPlayRequested.AddListener(()=>plays++);menu.onExitRequested.AddListener(()=>exits++);
                Click(menu.playButton);Check(plays==1,"JUGAR emite su UnityEvent mediante PointerClick");
                menu.quitApplicationOnExit=false;Click(menu.exitButton);Check(exits==1,"SALIR emite su UnityEvent mediante PointerClick");
                Click(menu.settingsButton);Check(menu.IsSettingsOpen&&!menu.mainPage.activeSelf,"CONFIGURACIÓN abre panel y bloquea menú");
                float sound=-1,music=-1;menu.onSoundVolumeChanged.AddListener(v=>sound=v);menu.onMusicVolumeChanged.AddListener(v=>music=v);
                menu.soundSlider.value=.37f;menu.musicSlider.value=.63f;
                Check(Mathf.Approximately(sound,.37f)&&Mathf.Approximately(music,.63f),"Sliders publican niveles de sonido y música");
                menu.muteToggle.isOn=true;Check(sound==0&&music==0,"Silenciar emite cero en ambos canales");
                menu.muteToggle.isOn=false;Check(Mathf.Approximately(sound,.37f)&&Mathf.Approximately(music,.63f),"Desactivar silencio restaura niveles");
                Click(menu.backButton);Check(!menu.IsSettingsOpen&&menu.mainPage.activeSelf,"VOLVER AL MENÚ cierra configuración");
                Check(Mathf.Approximately(PlayerPrefs.GetFloat("FlyingFree.UI.Sound"),.37f),"Preferencias persistidas");
                var layout=menu.GetComponent<ResponsiveMenu>();layout.enabled=false;layout.scaler.enabled=false;
                var canvas=menu.GetComponent<Canvas>();
                var cameraObject=new GameObject("QA render camera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();
                camera.orthographic=true;camera.nearClipPlane=.1f;camera.farClipPlane=100;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.magenta;
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;
                Capture(menu,layout,camera,1080,1920,new Rect(0,90,1080,1770),"01-menu-vertical",false);
                Capture(menu,layout,camera,1920,1080,new Rect(90,30,1740,1050),"02-menu-horizontal",false);
                Capture(menu,layout,camera,1080,1920,new Rect(0,90,1080,1770),"03-configuracion-vertical",true);
                Capture(menu,layout,camera,1920,1080,new Rect(90,30,1740,1050),"04-configuracion-horizontal",true);
                Capture(menu,layout,camera,1536,2048,new Rect(0,24,1536,2000),"05-tablet",false);
                Capture(menu,layout,camera,1280,720,new Rect(50,20,1180,700),"06-movil-compacto",true);
                // Reload a second instance to verify values survive controller recreation.
                var loaded=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(FlyingFreeBuilder.Root+"/Prefabs/FlyingFreeMenu.prefab"));
                var restored=loaded.GetComponent<FlyingFreeMenu>();
                Check(Mathf.Approximately(restored.SoundVolume,.37f)&&Mathf.Approximately(restored.MusicVolume,.63f),"Preferencias restauradas al recrear prefab");
                UnityEngine.Object.DestroyImmediate(loaded);
                // Leave the sample with sensible defaults after QA.
                PlayerPrefs.DeleteKey("FlyingFree.UI.Sound");PlayerPrefs.DeleteKey("FlyingFree.UI.Music");PlayerPrefs.DeleteKey("FlyingFree.UI.Muted");PlayerPrefs.Save();
                File.WriteAllLines(Path.Combine(Output,"verification.txt"),report);
                Debug.Log("FLYING_FREE_QA_OK\n"+string.Join("\n",report));
                SessionState.SetBool("FlyingFree.QA",false);EditorApplication.Exit(0);
            }
            catch(Exception ex)
            {
                File.WriteAllText(Path.Combine(Output,"verification.txt"),string.Join("\n",report)+"\nFAIL "+ex);
                Debug.LogException(ex);SessionState.SetBool("FlyingFree.QA",false);EditorApplication.Exit(1);
            }
        }
        static void Capture(FlyingFreeMenu menu,ResponsiveMenu layout,Camera camera,int width,int height,Rect safe,string name,bool settings)
        {
            menu.mainPage.SetActive(!settings);menu.settingsPage.SetActive(settings);
            var rt=new RenderTexture(width,height,24);camera.targetTexture=rt;camera.orthographicSize=height*.5f;camera.aspect=(float)width/height;
            camera.Render();Canvas.ForceUpdateCanvases();
            layout.ApplyViewport(new Vector2(width,height),safe);
            Check(layout.background.image.texture==(width>height?layout.background.landscape:layout.background.portrait),name+" fondo correcto para la orientación");
            var follower=menu.settingsPage.GetComponentInChildren<SafeAreaFollower>(true);
            var followerRect=(RectTransform)follower.transform;followerRect.anchorMin=layout.safeArea.anchorMin;followerRect.anchorMax=layout.safeArea.anchorMax;
            Canvas.ForceUpdateCanvases();
            var parent=(RectTransform)(settings?follower.transform:layout.safeArea);
            foreach(var selectable in (settings?menu.settingsPage:menu.mainPage).GetComponentsInChildren<Selectable>())
            {
                var corners=new Vector3[4];((RectTransform)selectable.transform).GetWorldCorners(corners);
                foreach(var corner in corners)
                {
                    var point=(Vector2)parent.InverseTransformPoint(corner);
                    Check(parent.rect.xMin-1<=point.x&&point.x<=parent.rect.xMax+1&&parent.rect.yMin-1<=point.y&&point.y<=parent.rect.yMax+1,name+" control dentro de safe area: "+selectable.name+" p="+point+" rect="+parent.rect+" root="+((RectTransform)menu.transform).rect);
                }
            }
            // Every generated label must fit its box, including CONFIGURACIÓN.
            foreach(var text in (settings?menu.settingsPage:menu.mainPage).GetComponentsInChildren<Text>())
                Check(text.preferredHeight<=text.rectTransform.rect.height+2,name+" texto cabe: "+text.name);
            camera.Render();RenderTexture.active=rt;
            var texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();
            File.WriteAllBytes(Path.Combine(Output,name+".png"),texture.EncodeToPNG());
            RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
