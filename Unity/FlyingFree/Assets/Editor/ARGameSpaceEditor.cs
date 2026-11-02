using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(ARGameSpace))]
public class ARGameSpaceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ARGameSpace gs = (ARGameSpace)target;

        DrawDefaultInspector();

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("ACCIONES Y VINCULACIÓN AR", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        GUI.backgroundColor = new Color(0.25f, 0.75f, 1f, 1f);
        if (GUILayout.Button("🎯 Auto-Conectar con ImageTarget y Controladores", GUILayout.Height(34)))
        {
            Undo.RecordObject(gs, "Auto-Conectar GameSpace");
            SetupGameSpaceInScene(gs);
            EditorUtility.SetDirty(gs);
            SceneView.RepaintAll();
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "ARGameSpace sirve como punto de referencia maestro unificado para:\n" +
            "1. La pared de globos (Image Target en Z=0).\n" +
            "2. El pasillo de vuelo de los pájaros (mitad de camino entre la marca y la cámara).\n" +
            "3. El lanzamiento y física del avión de papel (eje vertical fijado a la gravedad real).",
            MessageType.Info
        );
    }

    [MenuItem("Flying Free/AR/Configurar ARGameSpace en Escena", false, 10)]
    public static void SetupGameSpaceMenuItem()
    {
        ARGameSpace gs = FindObjectOfType<ARGameSpace>();
        if (gs == null)
        {
            GameObject go = new GameObject("AR_GameSpace");
            Undo.RegisterCreatedObjectUndo(go, "Crear AR_GameSpace");
            gs = go.AddComponent<ARGameSpace>();
        }

        SetupGameSpaceInScene(gs);
    }

    public static void SetupGameSpaceInScene(ARGameSpace gs)
    {
        if (gs == null) return;

        // 1. Conectar con el ImageTarget principal
        GameObject targetObj = GameObject.Find("ImageTarget-PlanoGlobos");
        if (targetObj != null)
        {
            SerializedObject so = new SerializedObject(gs);
            so.FindProperty("targetMarker").objectReferenceValue = targetObj.transform;
            so.ApplyModifiedProperties();
            gs.UpdateTransformPose(true);
            Debug.Log("[ARGameSpaceEditor] TargetMarker conectado a: " + targetObj.name);
        }

        // 2. Conectar TouchFlightController
        TouchFlightController flight = FindObjectOfType<TouchFlightController>();
        if (flight != null)
        {
            SerializedObject soFlight = new SerializedObject(flight);
            SerializedProperty propRef = soFlight.FindProperty("arReference");
            if (propRef != null)
            {
                propRef.objectReferenceValue = gs.transform;
                soFlight.ApplyModifiedProperties();
                EditorUtility.SetDirty(flight);
                Debug.Log("[ARGameSpaceEditor] TouchFlightController.arReference conectado a ARGameSpace.");
            }
        }

        // 3. Conectar ControlBandada
        ControlBandada bandada = FindObjectOfType<ControlBandada>();
        if (bandada != null)
        {
            SerializedObject soBandada = new SerializedObject(bandada);
            SerializedProperty propGs = soBandada.FindProperty("gameSpace");
            if (propGs != null)
            {
                propGs.objectReferenceValue = gs;
                soBandada.ApplyModifiedProperties();
                EditorUtility.SetDirty(bandada);
                Debug.Log("[ARGameSpaceEditor] ControlBandada.gameSpace conectado a ARGameSpace.");
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[ARGameSpaceEditor] ¡Configuración de ARGameSpace completada con éxito en la escena!");
    }
}
