using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace FlyingFree.Sprint2.Editor
{
    public static class FlyingFreeAssetBuilder
    {
        const string Root = "Assets/FlyingFreeSprint2";
        static readonly string[] Ids = {"Cuaderno","Rayo","Brisa"};
        static readonly float[] SpeedsMin = {1.7f,2.4f,1.15f}, SpeedsMax = {3.3f,4.6f,2.45f}, Turns={110,76,148}, Responses={4.6f,2.5f,7};
        static readonly float[] Lift = {.2f,.13f,.28f}, WidthScale={1,.77f,1.22f};
        static readonly string[] Descriptions={"Equilibrio entre velocidad y control.","Mas rapido. Anticipa los giros.","Mas lento y facil de controlar."};
        [MenuItem("Flying Free/Sprint 2/Preparar prefabs y demo")]
        public static void Prepare() { Setup(); }
        [MenuItem("Flying Free/Sprint 2/Activar adaptador Vuforia")]
        public static void EnableVuforia()
        {
            if(Type.GetType("Vuforia.VuforiaBehaviour, Vuforia.Unity.Engine")==null){Debug.LogError("Instala Vuforia Engine en este proyecto antes de activar el adaptador.");return;}
            foreach(var group in new[]{BuildTargetGroup.Standalone,BuildTargetGroup.Android,BuildTargetGroup.iOS})
            {
                var target = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
                string defs = PlayerSettings.GetScriptingDefineSymbols(target);
                if(!defs.Split(';').Contains("FLYINGFREE_VUFORIA")) PlayerSettings.SetScriptingDefineSymbols(target, defs + ";FLYINGFREE_VUFORIA");
            }
        }
        public static void Build()
        {
            Setup(); Verify();
            string dest=Environment.GetEnvironmentVariable("FLYINGFREE_DELIVERY_ROOT");
            if(!string.IsNullOrEmpty(dest))
            {
                AssetDatabase.ExportPackage(Root,Path.Combine(dest,"FlyingFree_Sprint2.unitypackage"),ExportPackageOptions.Recurse);
                Debug.Log("FLYINGFREE_PACKAGE_OK "+dest);
            }
        }
        public static void ExportDelivery()
        {
            AssetDatabase.Refresh();AssetDatabase.SaveAssets();
            string dest=Environment.GetEnvironmentVariable("FLYINGFREE_DELIVERY_ROOT");
            if(string.IsNullOrEmpty(dest))throw new Exception("Missing delivery folder");
            AssetDatabase.ExportPackage(Root,Path.Combine(dest,"FlyingFree_Sprint2.unitypackage"),ExportPackageOptions.Recurse);
            Debug.Log("FLYINGFREE_FINAL_EXPORT_OK");
        }
        static void Folder(string name) { Directory.CreateDirectory(Root+"/"+name); }
        static Material Mat(string name,string texture)
        {
            string path=Root+"/Materials/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(PipelineMaterialSupport.ActiveShader());AssetDatabase.CreateAsset(m,path);}else m.shader=PipelineMaterialSupport.ActiveShader();
            var t=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+texture);
            if(m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap",t);
            if(m.HasProperty("_MainTex")) m.SetTexture("_MainTex",t);
            if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.08f);
            if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.08f);
            if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",0);
            EditorUtility.SetDirty(m);return m;
        }
        static void ConfigureImports()
        {
            foreach(var file in Directory.GetFiles(Root+"/Models","*.fbx"))
            {
                var p=file.Replace('\\','/');var importer=AssetImporter.GetAtPath(p) as ModelImporter;
                if(importer==null)throw new Exception("No FBX importer: "+p);
                importer.importCameras=false;importer.importLights=false;importer.importNormals=ModelImporterNormals.Import;importer.importBlendShapes=false;importer.isReadable=true;
                importer.globalScale=1;importer.useFileScale=true;importer.optimizeGameObjects=false;importer.meshCompression=ModelImporterMeshCompression.Off;
                bool animation=p.EndsWith("Pajaro_Vuelo.fbx")||p.EndsWith("Pajaro_Captura.fbx");
                importer.importAnimation=animation;
                importer.animationType=p.Contains("Pajaro")?ModelImporterAnimationType.Generic:ModelImporterAnimationType.None;
                if(p.Contains("Pajaro")){importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.motionNodeName="Root_JNT";}
                importer.animationCompression=ModelImporterAnimationCompression.Off;
                if(animation)
                {
                    bool fly=p.Contains("Vuelo");var clips=importer.defaultClipAnimations;
                    var clip=clips.FirstOrDefault(x=>x.name==(fly?"Vuelo":"Captura"))??clips.FirstOrDefault();
                    if(clip==null)throw new Exception("Missing animation take: "+p);
                    clip.name=fly?"Fly":"Capture";clip.loopTime=fly;clip.loopPose=fly;
                    clip.keepOriginalOrientation=true;clip.keepOriginalPositionXZ=true;clip.keepOriginalPositionY=true;
                    clip.lockRootRotation=true;clip.lockRootPositionXZ=true;clip.lockRootHeightY=true;
                    importer.clipAnimations=new[]{clip};
                }
                importer.SaveAndReimport();
            }
            foreach(var file in Directory.GetFiles(Root+"/Textures","*.png"))
            {
                var p=file.Replace('\\','/');var importer=AssetImporter.GetAtPath(p) as TextureImporter;
                importer.maxTextureSize=1024;importer.sRGBTexture=true;importer.textureCompression=TextureImporterCompression.Compressed;
                importer.mipmapEnabled=true;importer.SaveAndReimport();
            }
            var marker=AssetImporter.GetAtPath(Root+"/Markers/Selector_FlyingFree.png") as TextureImporter;
            if(marker!=null){marker.isReadable=true;marker.mipmapEnabled=false;marker.maxTextureSize=1024;marker.textureCompression=TextureImporterCompression.Uncompressed;marker.SaveAndReimport();}
        }
        static AnimationClip Clip(string file,string name)
        {
            var clip=AssetDatabase.LoadAllAssetsAtPath(Root+"/Models/"+file).OfType<AnimationClip>().FirstOrDefault(c=>c.name==name);
            if(clip==null)throw new Exception("Clip not found "+name);return clip;
        }
        static GameObject InstantiateModel(string file,Transform parent,Material material)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/"+file);
            if(asset==null)throw new Exception("Missing model: "+file);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(asset);instance.transform.SetParent(parent,false);instance.name="Visual";
            foreach(var renderer in instance.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=material;
            return instance;
        }
        static AircraftDefinition[] Setup()
        {
            foreach(var folder in new[]{"Materials","Profiles","Prefabs","Animations","Scenes"})Folder(folder);
            AssetDatabase.Refresh();ConfigureImports();
            var profiles=new List<AircraftDefinition>();
            for(int i=0;i<Ids.Length;i++)
            {
                var path=Root+"/Profiles/"+Ids[i]+".asset";var def=AssetDatabase.LoadAssetAtPath<AircraftDefinition>(path);
                if(def==null){def=ScriptableObject.CreateInstance<AircraftDefinition>();AssetDatabase.CreateAsset(def,path);}
                def.id=Ids[i];def.displayName=Ids[i];def.description=Descriptions[i];def.minimumSpeed=SpeedsMin[i];def.maximumSpeed=SpeedsMax[i];def.turnSpeed=Turns[i];def.steeringResponse=Responses[i];def.lift=Lift[i];def.gripOffset=new Vector3(-(WidthScale[i]-1)*.052f,0,0);
                def.accent=i==0?new Color(.28f,.53f,.68f):i==1?new Color(.87f,.47f,.2f):new Color(.28f,.62f,.5f);
                def.thumbnail=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Thumbnails/Avion_"+Ids[i]+".png");
                var go=new GameObject("Avion_"+Ids[i]);var visual=InstantiateModel("Avion_"+Ids[i]+".fbx",go.transform,Mat("Avion_"+Ids[i],"Avion_"+Ids[i]+"_Albedo.png"));
                var box=go.AddComponent<BoxCollider>();box.isTrigger=true;
                var renderer=visual.GetComponentInChildren<Renderer>();var bounds=renderer.bounds;box.center=go.transform.InverseTransformPoint(bounds.center);box.size=new Vector3(bounds.size.x,Mathf.Max(.018f,bounds.size.y),bounds.size.z);
                var motor=go.AddComponent<PaperPlaneMotor>();motor.definition=def;motor.visual=visual.transform;
                var rigid=go.GetComponent<Rigidbody>();rigid.isKinematic=true;rigid.useGravity=false;rigid.interpolation=RigidbodyInterpolation.Interpolate;rigid.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
                def.prefab=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/Avion_"+Ids[i]+".prefab");UnityEngine.Object.DestroyImmediate(go);EditorUtility.SetDirty(def);profiles.Add(def);
            }
            string controllerPath=Root+"/Animations/Pajaro.controller";
            if(AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)!=null)AssetDatabase.DeleteAsset(controllerPath);
            var controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);var machine=controller.layers[0].stateMachine;
            var fly=machine.AddState("Fly");fly.motion=Clip("Pajaro_Vuelo.fbx","Fly");machine.defaultState=fly;
            var capture=machine.AddState("Capture");capture.motion=Clip("Pajaro_Captura.fbx","Capture");
            var back=capture.AddTransition(fly);back.hasExitTime=true;back.exitTime=1;back.duration=.04f;back.hasFixedDuration=true;
            var bird=new GameObject("Pajaro_FlyingFree");var birdVisual=InstantiateModel("Pajaro_Vuelo.fbx",bird.transform,Mat("Pajaro","Pajaro_Paleta.png"));
            var animator=birdVisual.GetComponent<Animator>();if(animator==null)animator=birdVisual.AddComponent<Animator>();
            animator.avatar=AssetDatabase.LoadAllAssetsAtPath(Root+"/Models/Pajaro_Vuelo.fbx").OfType<Avatar>().FirstOrDefault();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var carry=bird.AddComponent<BirdCarryController>();carry.animator=animator;carry.gripSocket=bird.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="PaperGrip_SOCKET");
            foreach(var renderer in bird.GetComponentsInChildren<SkinnedMeshRenderer>()) renderer.localBounds=new Bounds(new Vector3(0,0,-.025f),new Vector3(.95f,.95f,.65f));
            if(carry.gripSocket==null)throw new Exception("Missing exposed grip socket");
            var birdPrefab=PrefabUtility.SaveAsPrefabAsset(bird,Root+"/Prefabs/Pajaro_FlyingFree.prefab");UnityEngine.Object.DestroyImmediate(bird);
            var markerRoot=new GameObject("Selector_Marcador_FlyingFree");var anchor=new GameObject("Vista_Previa").transform;anchor.SetParent(markerRoot.transform,false);
            var selector=markerRoot.AddComponent<PlaneMarkerSelector>();selector.aircraft=profiles.ToArray();selector.previewAnchor=anchor;
            var binding=markerRoot.AddComponent<VuforiaSelectionMarker>();binding.selector=selector;binding.markerImage=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Markers/Selector_FlyingFree.png");
            PrefabUtility.SaveAsPrefabAsset(markerRoot,Root+"/Prefabs/Selector_Marcador.prefab");UnityEngine.Object.DestroyImmediate(markerRoot);
            CreateDemo(profiles.ToArray(),birdPrefab);
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();return profiles.ToArray();
        }
        static void CreateDemo(AircraftDefinition[] profiles,GameObject birdPrefab)
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));camera.tag="MainCamera";camera.transform.position=new Vector3(.7f,.7f,-1.3f);camera.transform.LookAt(new Vector3(0,.15f,0));camera.GetComponent<Camera>().backgroundColor=new Color(.12f,.2f,.25f);camera.GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor;
            var light=new GameObject("Sol",typeof(Light));light.GetComponent<Light>().type=LightType.Directional;light.GetComponent<Light>().intensity=1.2f;light.transform.rotation=Quaternion.Euler(40,-30,0);RenderSettings.ambientLight=new Color(.45f,.5f,.55f);
            var anchor=new GameObject("Marcador_simulado");
            var selector=anchor.AddComponent<PlaneMarkerSelector>();selector.aircraft=profiles;selector.previewAnchor=anchor.transform;selector.previewWithoutCameraTracking=true;
            var spawn=new GameObject("Punto_de_lanzamiento").transform;spawn.position=new Vector3(0,.2f,0);
            var controls=anchor.AddComponent<FlightDemoControls>();controls.selector=selector;controls.birdPrefab=birdPrefab;controls.spawnAnchor=spawn;controls.previewCamera=camera.GetComponent<Camera>();
            var ground=GameObject.CreatePrimitive(PrimitiveType.Plane);ground.name="Suelo_preview";ground.transform.position=new Vector3(0,-.15f,4);ground.transform.localScale=Vector3.one*3;UnityEngine.Object.DestroyImmediate(ground.GetComponent<Collider>());
            var m=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Suelo_Demo.mat");if(m==null){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,Root+"/Materials/Suelo_Demo.mat");}m.color=new Color(.3f,.43f,.36f);ground.GetComponent<Renderer>().sharedMaterial=m;
            EditorSceneManager.SaveScene(scene,Root+"/Scenes/Demo_Sprint2.unity");
        }
        static void Verify()
        {
            var report=new List<string>();
            foreach(var id in Ids)
            {
                var def=AssetDatabase.LoadAssetAtPath<AircraftDefinition>(Root+"/Profiles/"+id+".asset");if(def==null||def.prefab==null||def.thumbnail==null)throw new Exception("Incomplete aircraft "+id);
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(def.prefab);var size=instance.GetComponentInChildren<Renderer>().bounds.size;
                if(size.z<.25f||size.z>.37f||size.x<.14f||size.x>.27f)throw new Exception("FBX unit mismatch "+id+" "+size);
                report.Add(id+": "+size+", speed "+def.minimumSpeed+".."+def.maximumSpeed+", turn "+def.turnSpeed);UnityEngine.Object.DestroyImmediate(instance);
            }
            var fly=Clip("Pajaro_Vuelo.fbx","Fly");var capture=Clip("Pajaro_Captura.fbx","Capture");
            if(Mathf.Abs(fly.length-2)>.05f||Mathf.Abs(capture.length-4)>.05f)throw new Exception("Incorrect animation durations "+fly.length+" "+capture.length);
            var instanceBird=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Pajaro_FlyingFree.prefab"));
            var visual=instanceBird.transform.Find("Visual").gameObject;
            fly.SampleAnimation(visual,0);var start=instanceBird.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name,t=>t.localRotation);
            fly.SampleAnimation(visual,fly.length);float max=0;
            foreach(var t in instanceBird.GetComponentsInChildren<Transform>())max=Mathf.Max(max,Quaternion.Angle(start[t.name],t.localRotation));
            if(max>.1f)throw new Exception("Flight loop seam "+max);
            if(instanceBird.GetComponentsInChildren<SkinnedMeshRenderer>().Length!=1)throw new Exception("Bird must use one skinned renderer");
            report.Add("Bird flight: "+fly.length+" seconds. Capture: "+capture.length+" seconds. Loop rotation seam: "+max);
            UnityEngine.Object.DestroyImmediate(instanceBird);
            string dest=Environment.GetEnvironmentVariable("FLYINGFREE_DELIVERY_ROOT");if(!string.IsNullOrEmpty(dest))File.WriteAllLines(Path.Combine(dest,"_build/Verificacion_Unity.txt"),report);
            Debug.Log("FLYINGFREE_VERIFIED "+string.Join(" | ",report));
        }
    }
}
