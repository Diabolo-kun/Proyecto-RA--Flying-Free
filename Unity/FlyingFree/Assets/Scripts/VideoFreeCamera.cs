using UnityEngine;
using System.Collections.Generic;
namespace FlyingFree.Video
{
    [RequireComponent(typeof(Camera))]
    public sealed class VideoFreeCamera : MonoBehaviour
    {
        public float speed=12,mouseSensitivity=2;
        readonly HashSet<KeyCode> held=new HashSet<KeyCode>();
        bool rightMouse;
        void Update()
        {
            if(!GetComponent<Camera>().enabled){held.Clear();rightMouse=false;return;}
            if(rightMouse)
            {
                float x=(held.Contains(KeyCode.D)?1:0)-(held.Contains(KeyCode.A)?1:0),z=(held.Contains(KeyCode.W)?1:0)-(held.Contains(KeyCode.S)?1:0),y=(held.Contains(KeyCode.E)?1:0)-(held.Contains(KeyCode.Q)?1:0);
                transform.position+=(transform.right*x+transform.forward*z+Vector3.up*y)*speed*(held.Contains(KeyCode.LeftShift)?3:1)*Time.deltaTime;
            }
        }
        void OnGUI()
        {
            if(!GetComponent<Camera>().enabled)return;var e=Event.current;
            if(e.type==EventType.KeyDown)held.Add(e.keyCode);
            if(e.type==EventType.KeyUp)held.Remove(e.keyCode);
            if(e.type==EventType.MouseDown&&e.button==1)rightMouse=true;
            if(e.type==EventType.MouseUp&&e.button==1)rightMouse=false;
            if(e.type==EventType.MouseDrag&&e.button==1)
            {
                var angles=transform.eulerAngles;angles.y+=e.delta.x*mouseSensitivity*.15f;angles.x+=e.delta.y*mouseSensitivity*.15f;transform.rotation=Quaternion.Euler(angles.x,angles.y,0);
            }
        }
    }
}
