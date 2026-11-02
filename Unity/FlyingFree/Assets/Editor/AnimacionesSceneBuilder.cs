using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace FlyingFree.Editor
{
    public static class AnimacionesSceneBuilder
    {
        [MenuItem("Flying Free/Crear o Actualizar Escena Animaciones")]
        public static void CreateAnimacionesScene()
        {
            // 1. Crear nueva escena vacía
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 2. Crear Cámara
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.backgroundColor = new Color(0.2f, 0.3f, 0.45f, 1f);
            cam.fieldOfView = 58f;
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 500f;
            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<UniversalAdditionalCameraData>();

            // Posición elevada mirando hacia las 3 pistas
            camObj.transform.position = new Vector3(0f, 3.2f, -4.2f);
            camObj.transform.rotation = Quaternion.Euler(22f, 0f, 0f);

            // 3. Crear Luz Direccional
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 1.6f;
            light.shadows = LightShadows.Soft;
            lightObj.AddComponent<UniversalAdditionalLightData>();
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // 4. Crear ScoreManager
            GameObject scoreObj = new GameObject("ScoreManager");
            ScoreManager sm = scoreObj.AddComponent<ScoreManager>();

            // 5. Crear Suelo decorativo sutil
            GameObject floorObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floorObj.name = "Pista_Suelo";
            floorObj.transform.position = new Vector3(0f, 0f, 3f);
            floorObj.transform.localScale = new Vector3(1.5f, 1f, 1.5f);
            // Hacer que el collider del suelo sea plano para no interferir en triggers aéreos
            MeshCollider mc = floorObj.GetComponent<MeshCollider>();
            if (mc != null) mc.enabled = true;

            // 6. Crear Banco de Pruebas (AnimacionesTestBench)
            GameObject testBenchObj = new GameObject("AnimacionesTestBench");
            AnimacionesTestBench bench = testBenchObj.AddComponent<AnimacionesTestBench>();

            // Asignar los prefabs exactos
            bench.planePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Avion_Estandar.prefab");
            bench.birdPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Pajaro_FlyingFree.prefab");
            bench.balloonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Globo_Explosion.prefab");
            bench.explosionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Explosion_Choque.prefab");

            // 7. Crear Canvas con botón uGUI "Tirar Aviones"
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            GameObject btnObj = new GameObject("Boton_TirarAviones");
            btnObj.transform.SetParent(canvasObj.transform, false);
            UnityEngine.UI.Image img = btnObj.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.18f, 0.72f, 0.42f, 0.95f);
            UnityEngine.UI.Button btn = btnObj.AddComponent<UnityEngine.UI.Button>();
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.08f);
            rt.anchorMax = new Vector2(0.5f, 0.08f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(210f, 48f);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            UnityEngine.UI.Text txt = textObj.AddComponent<UnityEngine.UI.Text>();
            txt.text = "✈ Tirar Aviones";
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 18;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            bench.btnTirarAviones = btn;

            // 8. Guardar la escena en Assets/Scenes/Animaciones.unity
            string scenePath = "Assets/Scenes/Animaciones.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[AnimacionesSceneBuilder] ¡Escena creada y guardada con éxito en {scenePath}!");

            // 8. Registrar la escena en EditorBuildSettings si no está
            var currentScenes = EditorBuildSettings.scenes;
            bool alreadyInBuild = false;
            foreach (var s in currentScenes)
            {
                if (s.path == scenePath)
                {
                    alreadyInBuild = true;
                    break;
                }
            }

            if (!alreadyInBuild)
            {
                var newScenes = new EditorBuildSettingsScene[currentScenes.Length + 1];
                for (int i = 0; i < currentScenes.Length; i++)
                {
                    newScenes[i] = currentScenes[i];
                }
                newScenes[currentScenes.Length] = new EditorBuildSettingsScene(scenePath, true);
                EditorBuildSettings.scenes = newScenes;
                Debug.Log($"[AnimacionesSceneBuilder] Escena {scenePath} agregada a EditorBuildSettings.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
