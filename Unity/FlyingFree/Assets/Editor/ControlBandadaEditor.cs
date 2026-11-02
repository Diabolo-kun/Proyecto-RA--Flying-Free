using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ControlBandada))]
public class ControlBandadaEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ControlBandada bandada = (ControlBandada)target;

        // Dibuja los campos habituales
        DrawDefaultInspector();

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("ACCIONES RÁPIDAS (TESTING)", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Estado en vivo
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField($"Pájaros activos en vuelo: {bandada.PajarosActivosCount}", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
        }

        GUI.backgroundColor = new Color(0.2f, 0.85f, 0.45f, 1f);
        if (GUILayout.Button("▶ Forzar Generar Pájaro Ahora", GUILayout.Height(34)))
        {
            Undo.RecordObject(bandada, "Generar Pajaro");
            bandada.CrearPajaro();
            SceneView.RepaintAll();
        }

        GUI.backgroundColor = new Color(0.95f, 0.35f, 0.35f, 1f);
        if (GUILayout.Button("🗑️ Limpiar Todos los Pájaros", GUILayout.Height(28)))
        {
            Undo.RecordObject(bandada, "Limpiar Pajaros");
            bandada.LimpiarPajaros();
            SceneView.RepaintAll();
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "Con 'Test Mode Without Vuforia' activado, los pájaros orbitan alrededor de la cámara seleccionada (o Main Camera) sin requerir ARCore/Vuforia. En la Scene View puedes ver la órbita en cyan y el cono trasero de spawn en verde.",
            MessageType.Info
        );
    }
}
