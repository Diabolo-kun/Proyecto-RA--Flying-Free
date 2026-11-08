using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FlyingFree.Editor
{
    /// <summary>
    /// Clona GameRA y la deja lista para probar el lanzamiento impulsando el móvil.
    /// Menú: Flying Free / Crear o Actualizar Escena Prueba Lanzamiento Móvil
    /// </summary>
    public static class PruebaLanzamientoMovilBuilder
    {
        public const string SourceScene = "Assets/Scenes/GameRA.unity";
        public const string ScenePath = "Assets/Scenes/PruebaLanzamientoMovil.unity";
        public const string PlanePrefab = "Assets/Prefabs/Avion_Estandar.prefab";

        [MenuItem("Flying Free/Crear o Actualizar Escena Prueba Lanzamiento Móvil")]
        public static void CreateOrUpdateScene()
        {
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScene))
            {
                Debug.LogError("[PruebaLanzamientoMovil] No se encontró " + SourceScene);
                return;
            }

            AssetDatabase.CopyAsset(SourceScene, ScenePath);
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            TouchFlightController touch = Object.FindFirstObjectByType<TouchFlightController>();
            if (touch != null)
                touch.gameObject.SetActive(false);

            Transform joystick = GameObject.Find("JoystickBase") != null
                ? GameObject.Find("JoystickBase").transform
                : null;
            if (joystick != null)
                joystick.gameObject.SetActive(false);

            GameManagerRA gameManager = Object.FindFirstObjectByType<GameManagerRA>();
            if (gameManager != null)
            {
                SerializedObject soGm = new SerializedObject(gameManager);
                soGm.FindProperty("tiempoDeJuego").floatValue = 120f;
                soGm.ApplyModifiedPropertiesWithoutUndo();
            }

            LanzarAvionMovimiento lanzar = Object.FindFirstObjectByType<LanzarAvionMovimiento>(FindObjectsInactive.Include);
            if (lanzar == null)
            {
                GameObject host = new GameObject("ControlLanzamiento");
                SceneManager.MoveGameObjectToScene(host, scene);
                lanzar = host.AddComponent<LanzarAvionMovimiento>();
            }

            Camera arCam = null;
            var arGo = GameObject.Find("ARCamera");
            if (arGo != null) arCam = arGo.GetComponent<Camera>();
            if (arCam == null) arCam = Camera.main;

            SerializedObject so = new SerializedObject(lanzar);
            so.FindProperty("arCamera").objectReferenceValue = arCam;
            so.FindProperty("avionPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PlanePrefab);

            ARGameSpace gs = Object.FindFirstObjectByType<ARGameSpace>();
            if (gs != null)
                so.FindProperty("arReference").objectReferenceValue = gs.transform;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(lanzar);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            var currentScenes = EditorBuildSettings.scenes;
            bool alreadyInBuild = false;
            foreach (var s in currentScenes)
            {
                if (s.path == ScenePath)
                {
                    alreadyInBuild = true;
                    break;
                }
            }

            if (!alreadyInBuild)
            {
                var newScenes = new EditorBuildSettingsScene[currentScenes.Length + 1];
                for (int i = 0; i < currentScenes.Length; i++)
                    newScenes[i] = currentScenes[i];
                newScenes[currentScenes.Length] = new EditorBuildSettingsScene(ScenePath, true);
                EditorBuildSettings.scenes = newScenes;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PruebaLanzamientoMovil] Escena lista en " + ScenePath);
        }
    }
}
