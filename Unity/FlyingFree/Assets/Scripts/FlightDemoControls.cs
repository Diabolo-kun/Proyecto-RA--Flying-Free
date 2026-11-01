using UnityEngine;

namespace FlyingFree.Sprint2
{
    public sealed class FlightDemoControls : MonoBehaviour
    {
        public PlaneMarkerSelector selector;
        public GameObject birdPrefab;
        public Transform spawnAnchor;
        public Camera previewCamera;
        PaperPlaneMotor plane;
        BirdCarryController bird;
        Vector2 steering;
        float power = .55f;
        bool dragging;
        void Update()
        {
            if (plane == null) return;
            plane.SetSteering(steering);
            if (previewCamera != null)
            {
                Vector3 focus = plane.transform.position;
                Vector3 eye = focus + new Vector3(.75f,.6f,-1.35f);
                previewCamera.transform.position = Vector3.Lerp(previewCamera.transform.position,eye,1-Mathf.Exp(-4*Time.deltaTime));
                previewCamera.transform.LookAt(focus);
            }
        }
        void Launch()
        {
            if (selector == null || selector.Selected == null) return;
            if (bird != null) Destroy(bird.gameObject);
            if (plane != null) Destroy(plane.gameObject);
            var p = spawnAnchor != null ? spawnAnchor.position : Vector3.zero;
            var r = spawnAnchor != null ? spawnAnchor.rotation : Quaternion.identity;
            var obj = Instantiate(selector.Selected.prefab,p,r); plane = obj.GetComponent<PaperPlaneMotor>();
            plane.Launch(power);
        }
        void Capture()
        {
            if (plane == null || birdPrefab == null || plane.IsCaptured) return;
            if (bird != null) Destroy(bird.gameObject);
            var obj = Instantiate(birdPrefab,plane.transform.position+Vector3.up*.25f+plane.transform.right*.55f,plane.transform.rotation);
            bird = obj.GetComponent<BirdCarryController>();bird.BeginCapture(plane);
        }
        void OnGUI()
        {
            float scale = Screen.width / 960f;
            var old = GUI.matrix;GUI.matrix = Matrix4x4.Scale(new Vector3(scale,scale,1));
            float height = Screen.height/scale;
            GUI.Box(new Rect(20,height-150,380,130),GUIContent.none);
            GUI.Label(new Rect(36,height-142,340,24),"FUERZA DEL LANZAMIENTO  " + Mathf.RoundToInt(power*100) + "%");
            power=GUI.HorizontalSlider(new Rect(36,height-112,340,25),power,0,1);
            if(GUI.Button(new Rect(36,height-76,160,40),"LANZAR")) Launch();
            if(GUI.Button(new Rect(212,height-76,160,40),"PAJARO: CAPTURAR")) Capture();
            Rect joystick=new Rect(776,height-166,140,140);GUI.Box(joystick,GUIContent.none);
            var e=Event.current;
            if(e.type==EventType.MouseDown && joystick.Contains(e.mousePosition)){dragging=true;e.Use();}
            if(dragging && (e.type==EventType.MouseDrag || e.type==EventType.MouseDown)) {var v=(e.mousePosition-joystick.center)/60;steering=Vector2.ClampMagnitude(new Vector2(v.x,-v.y),1);e.Use();}
            if(e.rawType==EventType.MouseUp){dragging=false;steering=Vector2.zero;}
            GUI.Box(new Rect(joystick.center.x-17+steering.x*45,joystick.center.y-17-steering.y*45,34,34),"+");
            GUI.Label(new Rect(786,height-24,140,22),"CONTROL DE VUELO");
            GUI.matrix=old;
        }
    }
}
