#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class BirdsSprint2BChecks
{
    const string Pending = "FlyingFree.Birds.TestPending";
    static BirdsSprint2BChecks() { EditorApplication.playModeStateChanged += OnPlayMode; }
    [MenuItem("Flying Free/Sprint 2b/Probar sistema de pájaros")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Detén Play antes de ejecutar las pruebas.");
        var gameScene = EditorSceneManager.OpenScene("Assets/Scenes/GameRA.unity");
        var flock = Object.FindAnyObjectByType<ControlBandada>();
        if (flock == null || flock.camaraAR == null || flock.prefabPajaro == null || flock.testModeWithoutVuforia)
            throw new System.Exception("GameRA debe contener una bandada con cámara, prefab y tracking real activado.");
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Pajaro.mat");
        var palette = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Pajaro_Paleta.png");
        if (material == null || material.mainTexture != palette)
            throw new System.Exception("El material del pájaro debe usar su paleta original.");
        Debug.Log("PASS GameRA: bandada conectada, tracking real y textura low poly correcta");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }
    static void OnPlayMode(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Pending, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
            new GameObject("Pruebas_Sprint2b_Pajaros").AddComponent<BirdSystemPlayModeProbe>();
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Pending, false);
            if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetInt("FlyingFree.Birds.Exit", 1));
        }
    }
}
#endif
