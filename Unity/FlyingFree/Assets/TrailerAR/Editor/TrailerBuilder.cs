using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlyingFree.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using Vuforia;
using Image = UnityEngine.UI.Image;

namespace FlyingFree.Trailer.Editor
{
    public static class TrailerBuilder
    {
        public const string Root="Assets/TrailerAR";
        public const string MenuPrefab="Assets/UI/Prefabs/FlyingFreeMenu.prefab";
        static Font font;
        static Sprite blue,cream,round;
        static Texture2D marker;
        static VideoClip clip;

        [MenuItem("Flying Free/Tráiler/Preparar escenas de tráiler")]
        public static void Build()
        {
            Directory.CreateDirectory(Root+"/Materials");
            font=AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/Kalam-Bold.ttf");
            blue=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Art/V2/ButtonBlue.asset");
            cream=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Art/V2/ButtonCream.asset");
            round=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Art/RoundedPanel.png");
            if(font==null||blue==null||cream==null)throw new Exception("Faltan los assets del menú existente.");
            string markerPath=Root+"/Art/Marca_Trailer_FlyingFree.png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(markerPath);
            importer.textureType=TextureImporterType.Default;importer.isReadable=true;importer.mipmapEnabled=false;
            importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            marker=AssetDatabase.LoadAssetAtPath<Texture2D>(markerPath);
            clip=AssetDatabase.LoadAssetAtPath<VideoClip>(Root+"/Video/Flying_Free_Video.mp4");
            if(clip==null)throw new Exception("No se puede importar el vídeo del tráiler.");
            UpdateMenu();
            CreateScene(false);CreateScene(true);
            var scenes=EditorBuildSettings.scenes.ToList();
            scenes.RemoveAll(s=>s.path=="Assets/Scenes/TrailerAR.unity");
            scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/TrailerAR.unity",true));
            EditorBuildSettings.scenes=scenes.ToArray();
            Directory.CreateDirectory("TrailerEntrega");
            File.Copy(markerPath,"TrailerEntrega/Marca_Trailer_FlyingFree.png",true);
            File.WriteAllText("TrailerEntrega/Video.txt",$"Video original: Flying_Free_Video.mp4\nDuración importada: {clip.length:F2} segundos\nResolución: {clip.width} x {clip.height}\nFotogramas/segundo: {clip.frameRate:F2}\n");
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/Scenes/FlyingFreeMenu.unity");
            Debug.Log("TRAILER_BUILD_OK");
        }
        static void UpdateMenu()
        {
            var root=PrefabUtility.LoadPrefabContents(MenuPrefab);
            try
            {
                var menu=root.GetComponent<FlyingFreeMenu>();
                var layout=root.GetComponent<ResponsiveMenu>();
                var link=root.GetComponent<TrailerMenuLink>()??root.AddComponent<TrailerMenuLink>();
                Transform actions=menu.playButton.transform.parent;
                Transform existing=actions.Find("TRÁILER");
                var button=existing!=null?existing.GetComponent<Button>():UnityEngine.Object.Instantiate(menu.exitButton,actions);
                button.name="TRÁILER";button.onClick=new Button.ButtonClickedEvent();
                UnityEventTools.AddPersistentListener(button.onClick,link.OpenTrailer);
                var label=button.GetComponentInChildren<Text>();label.text="TRÁILER";label.fontSize=55;
                var icon=button.GetComponentInChildren<MenuIconGraphic>();icon.symbol=MenuIconGraphic.Symbol.Play;icon.color=new Color(.12f,.44f,.68f);
                button.transform.SetSiblingIndex(menu.playButton.transform.GetSiblingIndex()+1);
                var slots=new[]{menu.playButton,button,menu.settingsButton,menu.exitButton};
                for(int i=0;i<slots.Length;i++)Box((RectTransform)slots[i].transform,0,246-i*164,i==0?800:760,i==0?160:150);
                layout.trailerButton=(RectTransform)button.transform;
                layout.Refresh();
                PrefabUtility.SaveAsPrefabAsset(root,MenuPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        static void CreateScene(bool preview)
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            Camera camera;
            if(!preview)
            {
                var game=EditorSceneManager.OpenScene("Assets/Scenes/GameRA.unity",OpenSceneMode.Additive);
                var source=game.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<VuforiaBehaviour>(true)).First();
                var ar=UnityEngine.Object.Instantiate(source.gameObject);ar.name="ARCamera";
                SceneManager.MoveGameObjectToScene(ar,scene);
                camera=ar.GetComponent<Camera>();camera.transform.position=Vector3.zero;camera.transform.rotation=Quaternion.identity;
                EditorSceneManager.CloseScene(game,true);
            }
            else
            {
                var go=new GameObject("Cámara de simulación",typeof(Camera),typeof(AudioListener));camera=go.GetComponent<Camera>();
                camera.transform.position=new Vector3(0,.35f,-.28f);camera.transform.LookAt(Vector3.zero);camera.fieldOfView=52;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.39f,.49f);
            }
            camera.tag="MainCamera";
            var host=new GameObject("Tráiler Flying Free",typeof(VideoPlayer),typeof(AudioSource));
            var player=host.AddComponent<TrailerPlayer>();player.simulation=preview;
            player.video=host.GetComponent<VideoPlayer>();player.video.clip=clip;player.video.playOnAwake=false;
            player.audioSource=host.GetComponent<AudioSource>();player.audioSource.playOnAwake=false;player.audioSource.spatialBlend=0;
            var surface=new GameObject("Pantalla sobre la marca");player.surfaceRoot=surface.transform;
            var renderer=Quad("Vídeo 16 a 9",surface.transform,new Vector3(0,.003f,0),new Vector2(.30f,.16875f),Material("TrailerScreen",Color.white));
            player.screenRenderer=renderer;
            var frame=Quad("Marco azul",surface.transform,new Vector3(0,.002f,0),new Vector2(.312f,.181f),Material("TrailerFrame",new Color(.035f,.09f,.15f)));
            surface.SetActive(false);
            BuildHud(player);
            if(preview)
            {
                var anchor=new GameObject("Marca simulada (no usa cámara real)");
                var paperMaterial=Material("PreviewMarker",Color.white);paperMaterial.mainTexture=marker;
                Quad("Papel de marca",anchor.transform,Vector3.zero,new Vector2(.20f,.20f),paperMaterial);
                var simulator=host.AddComponent<TrailerPreview>();simulator.player=player;simulator.simulatedAnchor=anchor.transform;
                var button=Button("SIMULAR MARCA",player.menuButton.transform.parent.parent,false,22);
                Anchor((RectTransform)button.transform,new Vector2(.35f,.75f),new Vector2(.65f,.82f));
                simulator.toggleMarker=button;simulator.label=button.GetComponentInChildren<Text>();
            }
            else
            {
                var tracking=host.AddComponent<TrailerMarker>();tracking.markerImage=marker;tracking.player=player;tracking.printedWidthMeters=.20f;
            }
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/"+(preview?"TrailerPreview":"TrailerAR")+".unity");
        }
        static void BuildHud(TrailerPlayer player)
        {
            var go=new GameObject("Interfaz tráiler",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=50;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            go.AddComponent<MenuInput>();
            var safe=Rect("Safe Area",go.transform);Stretch(safe);
            var header=Panel("Cabecera",safe,new Color(.04f,.12f,.21f,.90f));
            var title=Text("Título",header.transform,"TRÁILER · FLYING FREE",27,Color.white);Anchor(title.rectTransform,new Vector2(.025f,.52f),new Vector2(.70f,.98f));
            player.statusText=Text("Estado",header.transform,"Enfoca la marca TRÁILER",20,new Color(.96f,.83f,.45f));Anchor(player.statusText.rectTransform,new Vector2(.025f,.02f),new Vector2(.70f,.55f));
            player.menuButton=Button("MENÚ",header.transform,false,28);Anchor((RectTransform)player.menuButton.transform,new Vector2(.74f,.15f),new Vector2(.985f,.88f));
            var controls=Panel("Controles",safe,new Color(.04f,.12f,.21f,.93f));
            player.timeText=Text("Tiempo",controls.transform,"00:00 / 00:00",23,Color.white);Anchor(player.timeText.rectTransform,new Vector2(.63f,.79f),new Vector2(.96f,.99f));
            var progressLabel=Text("Etiqueta progreso",controls.transform,"AVANCE DEL TRÁILER",20,new Color(.96f,.83f,.45f));Anchor(progressLabel.rectTransform,new Vector2(.04f,.79f),new Vector2(.58f,.99f));
            var sliderRoot=Rect("Barra de progreso",controls.transform);Anchor(sliderRoot,new Vector2(.04f,.52f),new Vector2(.96f,.81f));
            sliderRoot.gameObject.AddComponent<Image>().color=Color.clear;
            var track=Panel("Pista",sliderRoot,new Color(.50f,.65f,.71f));Anchor(track.rectTransform,new Vector2(0,.36f),new Vector2(1,.64f));track.raycastTarget=false;
            var fillArea=Rect("Área relleno",track.transform);Stretch(fillArea);
            var fill=Panel("Relleno",fillArea,new Color(.98f,.76f,.23f));Stretch(fill.rectTransform);fill.raycastTarget=false;
            var handles=Rect("Área control",sliderRoot);Anchor(handles,new Vector2(0,.15f),new Vector2(1,.85f));handles.offsetMin=new Vector2(12,0);handles.offsetMax=new Vector2(-12,0);
            var handle=Panel("Control",handles,Color.white);Box(handle.rectTransform,0,0,30,0);
            var slider=sliderRoot.gameObject.AddComponent<Slider>();slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.maxValue=1;slider.minValue=0;
            sliderRoot.gameObject.AddComponent<TrailerSeekBar>();player.progress=slider;
            player.rewindButton=Button("−10 s",controls.transform,false,28);Anchor((RectTransform)player.rewindButton.transform,new Vector2(.025f,.06f),new Vector2(.27f,.48f));
            player.pauseButton=Button("PAUSAR",controls.transform,true,28);Anchor((RectTransform)player.pauseButton.transform,new Vector2(.29f,.06f),new Vector2(.71f,.48f));
            player.pauseText=player.pauseButton.GetComponentInChildren<Text>();
            player.forwardButton=Button("+10 s",controls.transform,false,28);Anchor((RectTransform)player.forwardButton.transform,new Vector2(.73f,.06f),new Vector2(.975f,.48f));
            var prompt=Panel("Busca la marca",safe,new Color(.04f,.12f,.21f,.94f));
            var image=Rect("Vista de marca",prompt.transform);Box(image,0,50,200,200);var raw=image.gameObject.AddComponent<RawImage>();raw.texture=marker;raw.raycastTarget=false;
            var help=Text("Instrucciones",prompt.transform,"Apunta a la nueva marca\nTRÁILER de Flying Free",23,Color.white);Box(help.rectTransform,0,-98,315,110);
            player.scanPrompt=prompt.gameObject;
            player.retryButton=Button("REINTENTAR VÍDEO",safe,true,26);Anchor((RectTransform)player.retryButton.transform,new Vector2(.23f,.42f),new Vector2(.77f,.54f));player.retryButton.gameObject.SetActive(false);
            var layout=go.AddComponent<TrailerHudLayout>();layout.safe=safe;layout.header=header.rectTransform;layout.controls=controls.rectTransform;layout.prompt=prompt.rectTransform;layout.scaler=scaler;
            layout.Apply(new Vector2(Screen.width,Screen.height),new UnityEngine.Rect(0,0,Screen.width,Screen.height));
        }
        static MeshRenderer Quad(string name,Transform parent,Vector3 position,Vector2 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.name=name;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localRotation=Quaternion.Euler(90,0,0);go.transform.localScale=new Vector3(size.x,size.y,1);
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;return renderer;
        }
        static Material Material(string name,Color color)
        {
            string path=Root+"/Materials/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Unlit/Texture"));AssetDatabase.CreateAsset(material,path);}
            material.color=color;if(material.HasProperty("_Cull"))material.SetFloat("_Cull",0);EditorUtility.SetDirty(material);return material;
        }
        static RectTransform Rect(string name,Transform parent){var go=new GameObject(name,typeof(RectTransform));var rt=(RectTransform)go.transform;rt.SetParent(parent,false);return rt;}
        static void Stretch(RectTransform rt){Anchor(rt,Vector2.zero,Vector2.one);}
        static void Anchor(RectTransform rt,Vector2 min,Vector2 max){rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=rt.offsetMax=Vector2.zero;}
        static void Box(RectTransform rt,float x,float y,float width,float height){rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.anchoredPosition=new Vector2(x,y);rt.sizeDelta=new Vector2(width,height);}
        static Image Panel(string name,Transform parent,Color color){var rt=Rect(name,parent);var image=rt.gameObject.AddComponent<Image>();image.sprite=round;image.type=Image.Type.Sliced;image.color=color;return image;}
        static Text Text(string name,Transform parent,string text,int size,Color color){var rt=Rect(name,parent);var value=rt.gameObject.AddComponent<Text>();value.font=font;value.text=text;value.fontSize=size;value.color=color;value.alignment=TextAnchor.MiddleCenter;value.raycastTarget=false;return value;}
        static Button Button(string label,Transform parent,bool primary,int size)
        {
            var image=Panel(label,parent,Color.white);image.sprite=primary?blue:cream;
            image.type=Image.Type.Simple;
            var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var text=Text("Texto",image.transform,label,size,primary?Color.white:new Color(.08f,.22f,.32f));Stretch(text.rectTransform);text.rectTransform.offsetMin=new Vector2(12,5);text.rectTransform.offsetMax=new Vector2(-12,-5);
            return button;
        }
    }
}
