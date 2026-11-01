using UnityEngine;

namespace FlyingFree
{
    /// <summary>
    /// Gestiona las páginas del Canvas del Menú:
    /// - Página Principal (Jugar, Configuración, Salir)
    /// - Página de Selección de Juego / Modos (GameRA, Pruebas, Nuevos Juegos, Volver)
    /// - Página de Configuración (Audio, Mute, Volver)
    /// </summary>
    public class GameSelectionController : MonoBehaviour
    {
        [Header("--- PÁGINAS DEL CANVAS ---")]
        [Tooltip("Panel de la pantalla inicial (Jugar, Configuración, Salir)")]
        public GameObject mainPage;

        [Tooltip("Panel con los botones para elegir a qué juego o modo entrar")]
        public GameObject gameSelectPage;

        [Tooltip("Panel de ajustes de sonido/música")]
        public GameObject settingsPage;

        [Header("--- CARGADOR DE ESCENAS ---")]
        [SerializeField] private SceneLoader sceneLoader;

        private void Awake()
        {
            if (sceneLoader == null)
                sceneLoader = GetComponent<SceneLoader>() ?? gameObject.AddComponent<SceneLoader>();

            ShowMainPage();
        }

        /// <summary>
        /// Muestra únicamente la página principal.
        /// </summary>
        public void ShowMainPage()
        {
            if (mainPage != null) mainPage.SetActive(true);
            if (gameSelectPage != null) gameSelectPage.SetActive(false);
            if (settingsPage != null) settingsPage.SetActive(false);
        }

        /// <summary>
        /// Abre el panel de selección de juego (llamado por el botón JUGAR).
        /// </summary>
        public void OpenGameSelection()
        {
            if (mainPage != null) mainPage.SetActive(false);
            if (gameSelectPage != null) gameSelectPage.SetActive(true);
            if (settingsPage != null) settingsPage.SetActive(false);
        }

        /// <summary>
        /// Regresa de la selección de juego a la página principal.
        /// </summary>
        public void BackToMainFromGameSelection()
        {
            ShowMainPage();
        }

        /// <summary>
        /// Abre la página de configuración.
        /// </summary>
        public void OpenSettings()
        {
            if (mainPage != null) mainPage.SetActive(false);
            if (gameSelectPage != null) gameSelectPage.SetActive(false);
            if (settingsPage != null) settingsPage.SetActive(true);
        }

        /// <summary>
        /// Regresa de la configuración a la página principal.
        /// </summary>
        public void BackToMainFromSettings()
        {
            ShowMainPage();
        }

        // ==========================================
        // ACCIONES DE CARGA DIRECTAS PARA BOTONES
        // ==========================================

        public void PlayGameRA()
        {
            sceneLoader.LoadGameRA();
        }

        public void PlaySecondGame()
        {
            sceneLoader.LoadSecondGame();
        }

        public void PlayCustomScene(string sceneName)
        {
            sceneLoader.LoadScene(sceneName);
        }
    }
}
