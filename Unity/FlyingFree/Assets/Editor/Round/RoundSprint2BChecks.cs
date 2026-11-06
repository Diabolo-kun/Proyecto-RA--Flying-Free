using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class RoundSprint2BChecks
{
    const string Pending = "FlyingFree.Round.Pending";
    static RoundSprint2BChecks() { EditorApplication.playModeStateChanged += OnPlayMode; }
    [MenuItem("Flying Free/Sprint 2b/Probar puntuación y partida")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new System.Exception("Detén Play y guarda tus escenas antes de probar.");
        var original = EditorSceneManager.OpenScene("Assets/Scenes/GameRA.unity");
        var game = Object.FindAnyObjectByType<GameManagerRA>();
        var hud = GameObject.Find("HUD_Partida");
        if (game == null || hud == null) throw new System.Exception("Falta el gestor o el HUD de GameRA");
        var so = new SerializedObject(game);
        if (!so.FindProperty("avionesIlimitados").boolValue || so.FindProperty("tiempoDeJuego").floatValue != 30
            || so.FindProperty("globosParaGanar").intValue != 5) throw new System.Exception("Reglas incorrectas en GameRA");
        foreach (string field in new[] { "canvasVictoria", "canvasDerrota", "textoTiempo", "textoPuntuacion", "textoAviones", "textoEstado" })
            if (so.FindProperty(field).objectReferenceValue == null) throw new System.Exception("Falta referencia HUD: " + field);
        // Conservar las referencias y botones REALES de GameRA en una escena de prueba sin cámara física.
        var empty = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.MoveGameObjectToScene(game.gameObject, empty); SceneManager.MoveGameObjectToScene(hud, empty);
        EditorSceneManager.CloseScene(original, true); SceneManager.SetActiveScene(empty);
        SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
    }
    static void OnPlayMode(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Pending, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode) new GameObject("Pruebas_Logica_Partida").AddComponent<RoundPlayModeProbe>();
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Pending, false);
            if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetInt("FlyingFree.Round.Exit", 1));
        }
    }
}
