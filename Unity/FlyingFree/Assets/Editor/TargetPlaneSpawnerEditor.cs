using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TargetPlaneSpawner))]
public class TargetPlaneSpawnerEditor : Editor
{
    private bool showTestingSection = true;

    public override void OnInspectorGUI()
    {
        TargetPlaneSpawner spawner = (TargetPlaneSpawner)target;

        // Dibuja los campos normales configurados en el script
        DrawDefaultInspector();

        EditorGUILayout.Space(12);

        // ===============================================================
        // SECCIÓN 1: ACCIONES RÁPIDAS (MODO EDICIÓN SIN DAR AL PLAY)
        // ===============================================================
        EditorGUILayout.LabelField("ACCIONES RÁPIDAS (MODO EDICIÓN)", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        GUI.backgroundColor = new Color(0.2f, 0.85f, 0.4f, 1f);
        if (GUILayout.Button("▶ Generar Globos (Prueba Inmediata)", GUILayout.Height(36)))
        {
            Undo.RecordObject(spawner, "Generar Objetos");
            spawner.GenerateObjects();
            EditorUtility.SetDirty(spawner);
            SceneView.RepaintAll();
        }

        GUI.backgroundColor = new Color(0.95f, 0.35f, 0.35f, 1f);
        if (GUILayout.Button("🗑️ Limpiar Globos", GUILayout.Height(28)))
        {
            Undo.RecordObject(spawner, "Limpiar Objetos");
            spawner.ClearSpawnedObjects();
            EditorUtility.SetDirty(spawner);
            SceneView.RepaintAll();
        }
        EditorGUILayout.Space(4);

        GUI.backgroundColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        if (GUILayout.Button("🔄 Actualizar Plano Visual", GUILayout.Height(24)))
        {
            Undo.RecordObject(spawner, "Actualizar Plano");
            spawner.UpdateVisualPlane();
            EditorUtility.SetDirty(spawner);
            SceneView.RepaintAll();
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndVertical();

        EditorGUILayout.HelpBox(
            "Pulsa 'Generar Globos' para previsualizar los puntos aleatorios, márgenes de esquinas y distancias en la Scene View sin entrar en Play Mode.",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        // ===============================================================
        // SECCIÓN 2: SIMULACIÓN Y PRUEBAS (SIN VUFORIA / SIN CÁMARA)
        // ===============================================================
        showTestingSection = EditorGUILayout.Foldout(showTestingSection, "SIMULACIÓN DE VUFORIA (TESTING EN PC)", true, EditorStyles.foldoutHeader);
        if (showTestingSection)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Toggle rápido de modo pruebas
            bool currentTestMode = spawner.testModeWithoutVuforia;
            bool newTestMode = EditorGUILayout.ToggleLeft(" Activar Modo Test (Simula detección automática sin cámara)", currentTestMode, EditorStyles.boldLabel);
            if (newTestMode != currentTestMode)
            {
                Undo.RecordObject(spawner, "Cambiar Modo Test");
                spawner.testModeWithoutVuforia = newTestMode;
                EditorUtility.SetDirty(spawner);
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Simular Eventos de Rastreo de la Marca:", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.35f, 0.85f, 0.45f, 1f);
            if (GUILayout.Button("🟢 Simular Detección (Target Found)", GUILayout.Height(30)))
            {
                Undo.RecordObject(spawner, "Simular Target Found");
                spawner.OnTargetFound();
                EditorUtility.SetDirty(spawner);
                SceneView.RepaintAll();
            }

            GUI.backgroundColor = new Color(0.95f, 0.45f, 0.45f, 1f);
            if (GUILayout.Button("🔴 Simular Pérdida (Target Lost)", GUILayout.Height(30)))
            {
                Undo.RecordObject(spawner, "Simular Target Lost");
                spawner.OnTargetLost();
                EditorUtility.SetDirty(spawner);
                SceneView.RepaintAll();
            }

            EditorGUILayout.EndHorizontal();

            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Ideal para trabajar cómodamente en PC sin tener que apuntar con una webcam o imprimir la imagen de Vuforia.",
                MessageType.None
            );

            EditorGUILayout.EndVertical();
        }
    }
}
