using UnityEditor;
using UnityEngine;
namespace FlyingFree.Video.Editor
{
    [CustomEditor(typeof(VideoScenePlayer))]
    public sealed class VideoScenePlayerEditor : UnityEditor.Editor
    {
        [MenuItem("Flying Free/Video/Abrir escena de demostracion")]
        public static void OpenDemo()=>UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/FlyingFreeVideo/Scenes/Video_Parque_Demo.unity");
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();var player=(VideoScenePlayer)target;
            if(GUILayout.Button("Ver fotograma en el editor")){player.SetTime(player.timeSeconds);SceneView.RepaintAll();}
            if(GUILayout.Button("Volver al inicio")){player.SetTime(0);SceneView.RepaintAll();}
        }
    }
}
