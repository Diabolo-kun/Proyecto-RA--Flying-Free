using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vuforia;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

/// <summary>
/// Pide cámara en Android y arranca Vuforia solo cuando el permiso está listo.
/// Muestra el código de error real si la inicialización falla.
/// </summary>
[DefaultExecutionOrder(-200)]
public sealed class PermisoCamaraVuforia : MonoBehaviour
{
    static PermisoCamaraVuforia instancia;
    static bool iniciando;
    static bool vuforiaLista;

    string mensaje = "";
    bool mostrarAviso;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Arrancar()
    {
        if (instancia != null) return;
        var go = new GameObject("PermisoCamaraVuforia");
        DontDestroyOnLoad(go);
        instancia = go.AddComponent<PermisoCamaraVuforia>();
        SceneManager.sceneLoaded += instancia.AlCargarEscena;
        instancia.StartCoroutine(instancia.FlujoPermiso());
    }

    void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (vuforiaLista) return;
        if (EsEscenaAR(escena.name))
            StartCoroutine(FlujoPermiso());
    }

    static bool EsEscenaAR(string nombre)
    {
        return nombre == "GameRA" ||
               nombre == "EscenaAvionMarca" ||
               nombre == "TrailerAR" ||
               nombre == "PruebaLanzamientoMovil";
    }

    IEnumerator FlujoPermiso()
    {
        if (iniciando || vuforiaLista) yield break;
        iniciando = true;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            mensaje = "Necesitamos la cámara para la realidad aumentada.\nAcepta el permiso cuando salga.";
            mostrarAviso = true;

            bool respondido = false;
            bool concedido = false;
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => { concedido = true; respondido = true; };
            callbacks.PermissionDenied += _ => { concedido = false; respondido = true; };
            callbacks.PermissionDeniedAndDontAskAgain += _ => { concedido = false; respondido = true; };
            Permission.RequestUserPermission(Permission.Camera, callbacks);

            float limite = Time.realtimeSinceStartup + 60f;
            while (!respondido && Time.realtimeSinceStartup < limite)
                yield return null;

            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                yield return new WaitForSecondsRealtime(0.35f);
                if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
                {
                    respondido = false;
                    concedido = false;
                    Permission.RequestUserPermission(Permission.Camera, callbacks);
                    limite = Time.realtimeSinceStartup + 60f;
                    while (!respondido && Time.realtimeSinceStartup < limite)
                        yield return null;
                }
            }

            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                mensaje = "Sin permiso de cámara Vuforia no puede arrancar.\nAjustes > Apps > FlyingFree > Permisos > Cámara.";
                mostrarAviso = true;
                iniciando = false;
                yield break;
            }
        }

        mostrarAviso = false;
        yield return null;
        yield return new WaitForSecondsRealtime(0.2f);
#else
        yield return null;
#endif

        IniciarVuforia();
        iniciando = false;
    }

    void IniciarVuforia()
    {
        if (vuforiaLista) return;
        if (VuforiaApplication.Instance == null)
        {
            mensaje = "Vuforia no está disponible.";
            mostrarAviso = true;
            return;
        }

        VuforiaApplication.Instance.OnVuforiaInitialized -= AlInit;
        VuforiaApplication.Instance.OnVuforiaInitialized += AlInit;

        if (!VuforiaApplication.Instance.IsInitialized)
            VuforiaApplication.Instance.Initialize();
        else
        {
            vuforiaLista = true;
            mostrarAviso = false;
        }
    }

    void AlInit(VuforiaInitError error)
    {
        VuforiaApplication.Instance.OnVuforiaInitialized -= AlInit;

        if (error == VuforiaInitError.NONE)
        {
            vuforiaLista = true;
            mostrarAviso = false;
            Debug.Log("[PermisoCamaraVuforia] Vuforia OK.");
            return;
        }

        vuforiaLista = false;
        mostrarAviso = true;
        mensaje = MensajeError(error);
        Debug.LogError("[PermisoCamaraVuforia] Fallo: " + error);
    }

    static string MensajeError(VuforiaInitError error)
    {
        switch (error)
        {
            case VuforiaInitError.PERMISSION_ERROR:
                return "Falta el permiso de cámara.\nAjustes > Apps > FlyingFree > Cámara.";
            case VuforiaInitError.PLATFORM_ANDROID_CONFIG_INITIALIZATION_ERROR:
            case VuforiaInitError.PLATFORM_ANDROID_CONFIG_INVALID_ACTIVITY:
                return "Error de Android con Vuforia.\nHay que compilar con Application Entry Point = Activity.\nBorra la app e instala el build nuevo.";
            case VuforiaInitError.LICENSE_ERROR:
            case VuforiaInitError.LICENSE_CONFIG_INVALID_KEY:
            case VuforiaInitError.LICENSE_CONFIG_MISSING_KEY:
                return "Problema con la licencia de Vuforia.";
            case VuforiaInitError.LICENSE_CONFIG_NO_NETWORK_PERMANENT:
            case VuforiaInitError.LICENSE_CONFIG_NO_NETWORK_TRANSIENT:
                return "Vuforia necesita internet la primera vez.\nConéctate a Wi‑Fi e inténtalo de nuevo.";
            case VuforiaInitError.DEVICE_NOT_SUPPORTED:
                return "Este móvil no es compatible con Vuforia.";
            default:
                return "Error Vuforia: " + error + "\nBorra la app, reinstálala y acepta la cámara.";
        }
    }

    void OnGUI()
    {
        if (!mostrarAviso || string.IsNullOrEmpty(mensaje)) return;

        float w = Mathf.Min(Screen.width - 40f, 560f);
        float h = 220f;
        var rect = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.32f, w, h);
        GUI.Box(rect, "");
        GUILayout.BeginArea(new Rect(rect.x + 16f, rect.y + 16f, w - 32f, h - 32f));
        GUILayout.Label(mensaje);
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            if (GUILayout.Button("Pedir permiso otra vez", GUILayout.Height(44)))
                StartCoroutine(FlujoPermiso());
        }
        else if (!vuforiaLista)
        {
            if (GUILayout.Button("Reintentar Vuforia", GUILayout.Height(44)))
                StartCoroutine(FlujoPermiso());
        }
#endif
        GUILayout.EndArea();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        if (VuforiaApplication.Instance != null)
            VuforiaApplication.Instance.OnVuforiaInitialized -= AlInit;
        if (instancia == this)
            instancia = null;
    }
}
