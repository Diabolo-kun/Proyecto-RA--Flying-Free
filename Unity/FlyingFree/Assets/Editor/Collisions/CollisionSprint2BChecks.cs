using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Ejecuta los ensayos con PhysX y los prefabs reales, sin cámara ni licencia de seguimiento.</summary>
[InitializeOnLoad]
public static class CollisionSprint2BChecks
{
    const string Pending = "FlyingFree.Collisions.Pending";
    const string Scenes = "FlyingFree.Collisions.Scenes";
    [Serializable] class SavedScenes { public SceneSetup[] scenes; }
    static CollisionSprint2BChecks() { EditorApplication.playModeStateChanged += OnPlayMode; }

    [MenuItem("Flying Free/Sprint 2b/Probar colisiones e interacciones")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Detén Play antes de ejecutar las pruebas.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        SessionState.SetString(Scenes, JsonUtility.ToJson(new SavedScenes { scenes = EditorSceneManager.GetSceneManagerSetup() }));
        EditorSceneManager.OpenScene("Assets/Scenes/GameRA.unity");
        var spawner = UnityEngine.Object.FindAnyObjectByType<TargetPlaneSpawner>();
        if (spawner == null || !spawner.uprightLayout || !spawner.fitLayoutToCamera || spawner.maximumLayoutScale > .15f)
            throw new Exception("GameRA debe tener ascenso paralelo y encuadre compacto automático activados");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }
    static void OnPlayMode(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Pending, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
            new GameObject("Pruebas_Colisiones_Tarea4").AddComponent<CollisionPlayModeProbe>();
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Pending, false);
            if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetInt("FlyingFree.Collisions.Exit", 1));
            else
            {
                var saved = JsonUtility.FromJson<SavedScenes>(SessionState.GetString(Scenes, "{}"));
                if (saved?.scenes != null && saved.scenes.Length > 0 && !string.IsNullOrEmpty(saved.scenes[0].path))
                    EditorSceneManager.RestoreSceneManagerSetup(saved.scenes);
                EditorUtility.RevealInFinder("Logs/PruebasColisiones/Colisiones_PlayMode.txt");
            }
        }
    }

    public static void Build()
    {
        var scenes = Array.ConvertAll(Array.FindAll(EditorBuildSettings.scenes, s => s.enabled), s => s.path);
        var report = BuildPipeline.BuildPlayer(scenes, "Builds/ValidacionColisiones/FlyingFree.exe",
            BuildTarget.StandaloneWindows64, BuildOptions.Development);
        System.IO.Directory.CreateDirectory("Logs/PruebasColisiones");
        System.IO.File.WriteAllText("Logs/PruebasColisiones/Compilacion.txt",
            $"Unity {Application.unityVersion}\nResultado: {report.summary.result}\nErrores: {report.summary.totalErrors}\nAvisos: {report.summary.totalWarnings}\n");
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new Exception("La compilación de validación ha fallado");
    }
}
