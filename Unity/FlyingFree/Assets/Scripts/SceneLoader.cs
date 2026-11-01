using UnityEngine;
using UnityEngine.SceneManagement;

namespace FlyingFree
{
    /// <summary>
    /// Controlador de navegación y carga de escenas.
    /// Asigna este componente a un GameObject en tus Canvas para enlazarlo
    /// a los botones OnClick() desde el Inspector de Unity.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        [Header("--- NOMBRES DE ESCENAS CONFIGURADAS ---")]
        [Tooltip("Nombre exacto de la escena del Menú Principal")]
        public string menuSceneName = "FlyingFreeMenu";

        [Tooltip("Nombre exacto de la escena del juego de Realidad Aumentada")]
        public string gameRASceneName = "GameRA";

        [Tooltip("Nombre de la escena de un segundo juego (opcional)")]
        public string secondGameSceneName = "SegundoJuego";

        /// <summary>
        /// Carga cualquier escena por su nombre exacto en el proyecto.
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("[SceneLoader] Se intentó cargar una escena con nombre vacío.");
                return;
            }

            Debug.Log($"[SceneLoader] Cargando escena: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// Carga directamente el juego de Realidad Aumentada (GameRA).
        /// </summary>
        public void LoadGameRA()
        {
            LoadScene(gameRASceneName);
        }

        /// <summary>
        /// Carga el segundo juego si está configurado.
        /// </summary>
        public void LoadSecondGame()
        {
            LoadScene(secondGameSceneName);
        }

        /// <summary>
        /// Regresa al Menú Principal (FlyingFreeMenu).
        /// </summary>
        public void LoadMenu()
        {
            LoadScene(menuSceneName);
        }

        /// <summary>
        /// Recarga la escena activa actual.
        /// </summary>
        public void ReloadCurrentScene()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            Debug.Log($"[SceneLoader] Recargando escena actual: {currentScene}");
            SceneManager.LoadScene(currentScene);
        }

        /// <summary>
        /// Cierra la aplicación (en Android / PC) o detiene Play Mode en el editor.
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[SceneLoader] Cerrando aplicación...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
