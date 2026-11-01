using System;
using UnityEngine;

namespace FlyingFree.Video
{
    [Serializable] public class FrameKey { public int frame; public bool value; }
    [Serializable] public class VisibilityTrack { public string path; public FrameKey[] keys; }
    [Serializable] public class CameraPose { public float[] position,forward,up; public float fieldOfView,logo; }
    [Serializable] public class MaterialInfo { public string name,source,texture; public float[] color; public float roughness; public bool unlit; }
    [Serializable] public class ClipInfo { public string name; public int first,last; }
    [Serializable] public class AssetInfo { public string name; public string[] paths; public float[] origin; public int first,last; public ClipInfo[] clips; }
    [Serializable] public class VideoInfo { public int fps,firstFrame,lastFrame; public CameraPose[] camera; public MaterialInfo[] materials; public VisibilityTrack[] visibility; public AssetInfo[] assets; }

    public sealed class VideoScenePlayer : MonoBehaviour
    {
        public Transform animationRoot;
        public AnimationClip sequence;
        public TextAsset sourceData;
        public Camera cinematicCamera,explorationCamera;
        public Texture2D logo;
        public AudioSource audioSource;
        public bool playOnStart=true,showControls=true,useCinematicCamera=true;
        [Range(0,40)] public float timeSeconds;
        public bool Playing { get; private set; }
        public float Duration => 40f;
        VideoInfo info;
        float logoAlpha;
        static readonly int[] Cuts={133,253,349,433,481,553,625,769};
        public VideoInfo Info { get { if(info==null&&sourceData!=null)info=JsonUtility.FromJson<VideoInfo>(sourceData.text);return info; } }
        void Start()
        {
            if(animationRoot!=null)
            {
                var animator=animationRoot.GetComponent<Animator>();if(animator!=null)animator.enabled=false;
            }
            SetTime(timeSeconds);if(playOnStart)Play();
        }
        void Update()
        {
            if(Playing){timeSeconds=Mathf.Min(Duration,timeSeconds+Time.deltaTime);Sample();if(timeSeconds>=Duration)Pause();}
        }
        public void Play(){if(timeSeconds>=Duration)timeSeconds=0;Playing=true;SyncAudio();}
        public void Pause(){Playing=false;if(audioSource!=null)audioSource.Pause();}
        public void SetTime(float value){timeSeconds=Mathf.Clamp(value,0,Duration);Sample();if(Application.isPlaying)SyncAudio();}
        void SyncAudio()
        {
            if(audioSource==null||audioSource.clip==null)return;
            audioSource.time=Mathf.Clamp(timeSeconds,0,Mathf.Max(0,audioSource.clip.length-.03f));
            if(Playing&&!audioSource.isPlaying)audioSource.Play();else if(!Playing)audioSource.Pause();
        }
        public void Sample()
        {
            if(animationRoot!=null&&sequence!=null)sequence.SampleAnimation(animationRoot.gameObject,Mathf.Min(timeSeconds,sequence.length));
            if(Info==null||Info.camera==null||Info.camera.Length==0)return;
            float frame=timeSeconds*Info.fps;int a=Mathf.Clamp(Mathf.FloorToInt(frame),0,Info.camera.Length-1),b=Mathf.Min(a+1,Info.camera.Length-1);
            float u=frame-a;foreach(int cut in Cuts)if(b+1==cut)u=0;
            var p=Info.camera[a];var q=Info.camera[b];
            if(cinematicCamera!=null)
            {
                cinematicCamera.transform.position=Vector3.Lerp(V(p.position),V(q.position),u);
                cinematicCamera.transform.rotation=Quaternion.Slerp(Quaternion.LookRotation(V(p.forward),V(p.up)),Quaternion.LookRotation(V(q.forward),V(q.up)),u);
                cinematicCamera.fieldOfView=Mathf.Lerp(p.fieldOfView,q.fieldOfView,u);cinematicCamera.enabled=useCinematicCamera;
                var listener=cinematicCamera.GetComponent<AudioListener>();if(listener!=null)listener.enabled=useCinematicCamera;
            }
            if(explorationCamera!=null)
            {
                explorationCamera.enabled=!useCinematicCamera;var listener=explorationCamera.GetComponent<AudioListener>();if(listener!=null)listener.enabled=!useCinematicCamera;
            }
            logoAlpha=Mathf.Lerp(p.logo,q.logo,u);
        }
        public static Vector3 V(float[] p)=>p==null||p.Length<3?Vector3.zero:new Vector3(p[0],p[1],p[2]);
        void OnGUI()
        {
            if(!Application.isPlaying)return;
            if(Event.current.type==EventType.KeyDown&&Event.current.keyCode==KeyCode.F1){showControls=!showControls;Event.current.Use();}
            if(useCinematicCamera&&logo!=null&&logoAlpha>0)
            {
                var old=GUI.color;GUI.color=new Color(1,1,1,logoAlpha);GUI.DrawTexture(new Rect(Screen.width*.27f,Screen.height*.19f,Screen.width*.46f,Screen.height*.5f),logo,ScaleMode.ScaleToFit,true);GUI.color=old;
            }
            if(useCinematicCamera&&timeSeconds>39)
            {
                var old=GUI.color;GUI.color=new Color(0,0,0,timeSeconds-39);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=old;
            }
            if(!showControls)return;
            GUI.Box(new Rect(12,Screen.height-102,Screen.width-24,90),"FLYING FREE / MODELOS Y ANIMACIONES DEL VIDEO");
            if(GUI.Button(new Rect(25,Screen.height-72,100,27),Playing?"PAUSAR":"REPRODUCIR")){if(Playing)Pause();else Play();}
            if(GUI.Button(new Rect(133,Screen.height-72,100,27),"REINICIAR")){SetTime(0);Play();}
            if(GUI.Button(new Rect(241,Screen.height-72,165,27),useCinematicCamera?"EXPLORAR PARQUE":"CAMARA DEL VIDEO")){useCinematicCamera=!useCinematicCamera;Sample();}
            float value=GUI.HorizontalSlider(new Rect(421,Screen.height-65,Mathf.Max(100,Screen.width-565),20),timeSeconds,0,Duration);
            if(Mathf.Abs(value-timeSeconds)>.002f)SetTime(value);
            GUI.Label(new Rect(Screen.width-128,Screen.height-71,105,27),timeSeconds.ToString("0.0")+" / 40 s");
            GUI.Label(new Rect(25,Screen.height-39,Screen.width-50,23),"F1: ocultar controles | Explorar: boton derecho + raton, WASD y Q/E; Mayus para acelerar");
        }
    }
}
