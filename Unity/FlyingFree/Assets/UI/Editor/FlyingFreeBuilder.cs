using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FlyingFree.UI.Editor
{
    public static class FlyingFreeBuilder
    {
        public const string Root = "Assets/FlyingFree/UI";
        static Font font;
        static Sprite round;
        static Sprite buttonBlue, buttonCream;
        static readonly Color Ink = Hex("173E55"), Blue = Hex("217AB4"), Yellow = Hex("FFDA69"), Cream = Hex("F5FBF7");

        [MenuItem("Flying Free/Crear escena y prefab de muestra")]
        public static void Build()
        {
            Directory.CreateDirectory(Root + "/Art"); Directory.CreateDirectory(Root + "/Prefabs"); Directory.CreateDirectory(Root + "/Scenes");
            round = CreateRoundedSprite();
            font = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/Kalam-Bold.ttf");
            if(font==null)throw new InvalidOperationException("Falta Fonts/Kalam-Bold.ttf");
            buttonBlue=ImportButton("ButtonBlue");buttonCream=ImportButton("ButtonCream");
            var logo=ImportLogo();
            var landscape=ImportBackground("ParkLandscape");var portrait=ImportBackground("ParkPortrait");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Flying Free • Menú", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080,1920); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            root.AddComponent<MenuInput>();
            var bg = Rect("Parque low-poly", root.transform); Stretch(bg);
            var bgImage=bg.gameObject.AddComponent<RawImage>();bgImage.raycastTarget=false;
            var background=bg.gameObject.AddComponent<MenuBackground>();background.image=bgImage;background.landscape=landscape;background.portrait=portrait;
            var safe = Rect("Safe Area", root.transform); Stretch(safe);
            var main = Rect("Menú principal", safe); Stretch(main);
            var brand = Rect("Marca", main);
            var logoRect=Rect("Logo original Flying Free",brand);Stretch(logoRect);
            var logoImage=logoRect.gameObject.AddComponent<Image>();logoImage.sprite=logo;logoImage.preserveAspect=true;logoImage.raycastTarget=false;
            var card = Rect("Acciones",main);
            var play = ArtButton("JUGAR",card,true,MenuIconGraphic.Symbol.Play,66); Box((RectTransform)play.transform,0,170,800,166);
            var settings = ArtButton("CONFIGURACIÓN",card,false,MenuIconGraphic.Symbol.Gear,46); Box((RectTransform)settings.transform,0,0,760,154);
            var exit = ArtButton("SALIR",card,false,MenuIconGraphic.Symbol.Exit,59); Box((RectTransform)exit.transform,0,-164,760,154);

            // Scrim fills the complete screen; interactive settings stay inside the safe area.
            var scrim = Panel("Configuración • fondo",root.transform,new Color(.045f,.16f,.23f,.65f)); Stretch(scrim.rectTransform);
            scrim.sprite=null;
            var settingsSafe = Rect("Safe Area configuración",scrim.transform); Stretch(settingsSafe);
            var config = Rect("Panel configuración",settingsSafe);
            var frame=Panel("Marco azul",config,Hex("243F58"));Stretch(frame.rectTransform);Shadow(frame.gameObject,new Vector2(0,-12));
            var configBg = Panel("Papel crema",config,Hex("FFF6E5")); Stretch(configBg.rectTransform);configBg.rectTransform.offsetMin=new Vector2(8,12);configBg.rectTransform.offsetMax=new Vector2(-8,-6);
            var paper=Rect("Textura de papel",configBg.transform);Stretch(paper);paper.offsetMin=new Vector2(25,25);paper.offsetMax=new Vector2(-25,-25);
            var paperImage=paper.gameObject.AddComponent<RawImage>();paperImage.texture=buttonCream.texture;paperImage.uvRect=new Rect(.25f,.37f,.5f,.25f);paperImage.color=new Color(1,1,1,.20f);paperImage.raycastTarget=false;
            var header=Panel("Cabecera azul",config,Blue);Box(header.rectTransform,0,326,750,198);
            var title=Label("Título",config,"CONFIGURACIÓN",48,FontStyle.Normal,Color.white); Box(title.rectTransform,0,343,710,90);
            var subtitle=Label("Subtítulo",config,"Tu aventura, a tu manera",29,FontStyle.Normal,Hex("DBF1FF")); Box(subtitle.rectTransform,0,278,710,54);
            Text soundValue,musicValue;
            var sound = Slider("SONIDO",config,119,out soundValue);
            var music = Slider("MÚSICA",config,-61,out musicValue);
            var mute=Toggle(config);
            Box((RectTransform)mute.transform,0,-238,670,98);
            var back=ArtButton("VOLVER AL MENÚ",config,true,MenuIconGraphic.Symbol.Back,41); Box((RectTransform)back.transform,0,-365,725,142);
            var controller=root.AddComponent<FlyingFreeMenu>();
            controller.mainPage=main.gameObject;controller.settingsPage=scrim.gameObject;
            controller.playButton=play;controller.settingsButton=settings;controller.exitButton=exit;controller.backButton=back;
            controller.soundSlider=sound;controller.musicSlider=music;controller.soundValue=soundValue;controller.musicValue=musicValue;controller.muteToggle=mute;
            var responsive=root.AddComponent<ResponsiveMenu>();
            responsive.safeArea=safe;responsive.scaler=scaler;responsive.brand=brand;responsive.menuCard=card;responsive.settingsCard=config;responsive.background=background;
            // Separate follower because both pages have their own hierarchy.
            var follower=settingsSafe.gameObject.AddComponent<SafeAreaFollower>();follower.source=safe;
            scrim.gameObject.SetActive(false);
            responsive.Refresh();
            PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/FlyingFreeMenu.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/FlyingFreeMenu.prefab");
            PrefabUtility.InstantiatePrefab(prefab,scene);
            EditorSceneManager.SaveScene(scene,Root+"/Scenes/FlyingFreeMenu.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Root+"/Scenes/FlyingFreeMenu.unity",true)};
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait=true;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
            PlayerSettings.allowedAutorotateToLandscapeLeft=true;PlayerSettings.allowedAutorotateToLandscapeRight=true;
            AssetDatabase.SaveAssets();
            Export();
            Debug.Log("FLYING_FREE_BUILD_OK");
        }
        [MenuItem("Flying Free/Exportar paquete de UI")]
        public static void Export()
        {
            AssetDatabase.Refresh();
            string target=Path.GetFullPath("../deliverables/FlyingFree-Menu-v2.unitypackage"); Directory.CreateDirectory(Path.GetDirectoryName(target));
            // uGUI is installed by Package Manager; never export its source scripts.
            AssetDatabase.ExportPackage("Assets/FlyingFree",target,ExportPackageOptions.Recurse);
            Debug.Log("FLYING_FREE_EXPORT_OK " + target);
        }

        static RectTransform Rect(string name,Transform parent)
        {
            var go=new GameObject(name,typeof(RectTransform));var rt=(RectTransform)go.transform;rt.SetParent(parent,false);return rt;
        }
        static void Stretch(RectTransform rt){rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;}
        static void Box(RectTransform rt,float x,float y,float w,float h){rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.anchoredPosition=new Vector2(x,y);rt.sizeDelta=new Vector2(w,h);}
        static Color Hex(string value){ColorUtility.TryParseHtmlString("#"+value,out var color);return color;}
        static Image Panel(string name,Transform parent,Color color)
        {
            var rt=Rect(name,parent);var image=rt.gameObject.AddComponent<Image>();image.sprite=round;image.type=Image.Type.Sliced;image.color=color;return image;
        }
        static Text Label(string name,Transform parent,string value,int size,FontStyle style,Color color)
        {
            var rt=Rect(name,parent);var text=rt.gameObject.AddComponent<Text>();text.font=font;text.text=value;text.fontSize=size;text.fontStyle=style;text.color=color;
            text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
            return text;
        }
        static void Shadow(GameObject go,Vector2 offset)
        {
            var shadow=go.AddComponent<Shadow>();shadow.effectColor=new Color(.06f,.22f,.30f,.23f);shadow.effectDistance=offset;
        }
        static Button Button(string value,Transform parent,Color color,Color textColor,int fontSize)
        {
            var image=Panel(value,parent,color);Shadow(image.gameObject,new Vector2(0,-5));var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var colors=button.colors;colors.highlightedColor=new Color(.94f,.98f,1);colors.pressedColor=new Color(.76f,.86f,.91f);colors.selectedColor=new Color(.9f,.95f,1);colors.fadeDuration=.09f;button.colors=colors;
            var label=Label("Texto",image.transform,value,fontSize,FontStyle.Bold,textColor);Stretch(label.rectTransform);label.rectTransform.offsetMin=new Vector2(20,8);label.rectTransform.offsetMax=new Vector2(-20,-8);
            return button;
        }
        static Button ArtButton(string value,Transform parent,bool primary,MenuIconGraphic.Symbol icon,int fontSize)
        {
            var rt=Rect(value,parent);
            var hit=rt.gameObject.AddComponent<Image>();hit.color=Color.clear;
            var visual=Rect("Arte y contenido",rt);Stretch(visual);
            var art=visual.gameObject.AddComponent<Image>();art.sprite=primary?buttonBlue:buttonCream;art.type=Image.Type.Sliced;art.raycastTarget=false;
            var iconRect=Rect("Icono",visual);iconRect.anchorMin=iconRect.anchorMax=new Vector2(.145f,.55f);iconRect.sizeDelta=new Vector2(88,88);
            var graphic=iconRect.gameObject.AddComponent<MenuIconGraphic>();graphic.symbol=icon;graphic.color=icon==MenuIconGraphic.Symbol.Exit?Hex("ED683D"):(primary?Color.white:Ink);graphic.raycastTarget=false;
            var text=Label("Texto",visual,value,fontSize,FontStyle.Normal,primary?Color.white:Ink);
            text.rectTransform.anchorMin=new Vector2(.245f,.14f);text.rectTransform.anchorMax=new Vector2(.96f,.94f);text.rectTransform.offsetMin=text.rectTransform.offsetMax=Vector2.zero;
            if(primary){Shadow(text.gameObject,new Vector2(0,-2));Shadow(iconRect.gameObject,new Vector2(0,-2));}
            var button=rt.gameObject.AddComponent<Button>();button.targetGraphic=art;
            var colors=button.colors;colors.highlightedColor=new Color(1,1,.96f);colors.pressedColor=new Color(.8f,.86f,.9f);colors.selectedColor=Color.white;colors.fadeDuration=.08f;button.colors=colors;
            rt.gameObject.AddComponent<MenuButtonFeedback>().visual=visual;
            return button;
        }
        static Texture2D ImportBackground(string name)
        {
            string path=Root+"/Art/V2/"+name+".png";AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;
            importer.maxTextureSize=4096;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static Sprite ImportLogo()
        {
            string path=Root+"/Art/V2/FlyingFreeLogo.png";AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.maxTextureSize=2048;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static Sprite ImportButton(string name)
        {
            string path=Root+"/Art/V2/"+name+".png";AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;
            importer.isReadable=true;importer.maxTextureSize=4096;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=texture.GetPixels32();
            int xMin=texture.width,yMin=texture.height,xMax=0,yMax=0;
            for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>80){xMin=Mathf.Min(xMin,x);xMax=Mathf.Max(xMax,x);yMin=Mathf.Min(yMin,y);yMax=Mathf.Max(yMax,y);}
            xMin=Mathf.Max(0,xMin-6);yMin=Mathf.Max(0,yMin-6);xMax=Mathf.Min(texture.width-1,xMax+6);yMax=Mathf.Min(texture.height-1,yMax+6);
            // This is sprite import metadata. The original transparent PNG is retained unchanged.
            var sprite=Sprite.Create(texture,new Rect(xMin,yMin,xMax-xMin+1,yMax-yMin+1),new Vector2(.5f,.5f),300,0,SpriteMeshType.FullRect,new Vector4(200,100,200,100));sprite.name=name;
            string spritePath=Root+"/Art/V2/"+name+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if(existing==null)AssetDatabase.CreateAsset(sprite,spritePath);else{EditorUtility.CopySerialized(sprite,existing);UnityEngine.Object.DestroyImmediate(sprite);sprite=existing;EditorUtility.SetDirty(sprite);}
            return sprite;
        }
        static Slider Slider(string label,Transform parent,float y,out Text value)
        {
            var rt=Rect(label,parent);Box(rt,0,y,670,140);
            var name=Label("Etiqueta",rt,label,30,FontStyle.Bold,Ink);Box(name.rectTransform,-150,42,370,50);name.alignment=TextAnchor.MiddleLeft;
            value=Label("Valor",rt,label=="SONIDO"?"80%":"60%",29,FontStyle.Bold,Blue);Box(value.rectTransform,257,42,150,50);value.alignment=TextAnchor.MiddleRight;
            // Transparent hit surface supplies a 90-unit-high touch target around the track.
            var hit=rt.gameObject.AddComponent<Image>();hit.color=Color.clear;
            var track=Panel("Pista",rt,Hex("D4E5E2"));Box(track.rectTransform,0,-24,630,22);track.raycastTarget=false;
            var fillArea=Rect("Área relleno",rt);Box(fillArea,0,-24,630,22);
            var fill=Panel("Relleno",fillArea,Blue);Stretch(fill.rectTransform);fill.raycastTarget=false;
            var handleArea=Rect("Área control",rt);Box(handleArea,0,-24,630,22);
            var handle=Panel("Control",handleArea,Yellow);Box(handle.rectTransform,0,0,62,62);Shadow(handle.gameObject,new Vector2(0,-3));
            var slider=rt.gameObject.AddComponent<Slider>();slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;
            slider.minValue=0;slider.maxValue=1;slider.wholeNumbers=false;slider.value=label=="SONIDO"?.8f:.6f;return slider;
        }
        static Toggle Toggle(Transform parent)
        {
            var rt=Rect("Silenciar todo",parent);var image=rt.gameObject.AddComponent<Image>();image.color=Color.clear;
            var box=Panel("Casilla",rt,Ink);Box(box.rectTransform,-298,0,68,68);
            var inside=Panel("Interior",box.transform,Hex("FFF9EB"));Stretch(inside.rectTransform);inside.rectTransform.offsetMin=new Vector2(5,5);inside.rectTransform.offsetMax=new Vector2(-5,-5);inside.raycastTarget=false;
            var tickRect=Rect("Activado",box.transform);Stretch(tickRect);tickRect.offsetMin=new Vector2(10,10);tickRect.offsetMax=new Vector2(-10,-10);
            var tick=tickRect.gameObject.AddComponent<MenuIconGraphic>();tick.symbol=MenuIconGraphic.Symbol.Check;tick.color=Blue;tick.raycastTarget=false;
            var label=Label("Texto",rt,"Silenciar todo",31,FontStyle.Bold,Ink);Box(label.rectTransform,39,0,570,80);label.alignment=TextAnchor.MiddleLeft;
            var toggle=rt.gameObject.AddComponent<Toggle>();toggle.targetGraphic=box;toggle.graphic=tick;toggle.isOn=false;return toggle;
        }
        static Sprite CreateRoundedSprite()
        {
            const int n=128;const float r=30;
            var texture=new Texture2D(n,n,TextureFormat.RGBA32,false);
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                float dx=Mathf.Max(Mathf.Abs(x+.5f-n*.5f)-(n*.5f-r),0),dy=Mathf.Max(Mathf.Abs(y+.5f-n*.5f)-(n*.5f-r),0);
                texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(r-Mathf.Sqrt(dx*dx+dy*dy)+.5f)));
            }
            texture.Apply();string path=Root+"/Art/RoundedPanel.png";File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single;importer.textureCompression=TextureImporterCompression.Uncompressed;
            var textureSettings=new TextureImporterSettings();importer.ReadTextureSettings(textureSettings);textureSettings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(textureSettings);
            importer.spriteBorder=new Vector4(32,32,32,32);importer.spritePixelsPerUnit=100;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
