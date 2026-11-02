#if UNITY_EDITOR_WIN
using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

// Put this file in Assets/Editor. It is excluded from Android builds.
// Load the existing Vuforia webcam driver from its package before Play Mode.
[InitializeOnLoad]
internal static class VuforiaDriverEditorFix
{
    private const uint SearchDllFolderAndDefaultDirectories = 0x00001100;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern IntPtr GetModuleHandleW(string moduleName);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode,
        ExactSpelling = true, SetLastError = true)]
    private static extern IntPtr LoadLibraryExW(string path, IntPtr file, uint flags);

    static VuforiaDriverEditorFix()
    {
        EditorApplication.delayCall += PrepareQuietly;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void PrepareQuietly()
    {
        PrepareDriver(false);
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
            PrepareDriver(true);
    }

    [MenuItem("Tools/Vuforia/Preparar controlador de webcam")]
    private static void PrepareFromMenu()
    {
        PrepareDriver(true);
    }

    private static void PrepareDriver(bool reportSuccess)
    {
        if (GetModuleHandleW("UnityDriver.dll") != IntPtr.Zero)
        {
            if (reportSuccess)
                Debug.Log("[Vuforia Webcam] Controlador de webcam disponible.");
            return;
        }

        PackageInfo package = PackageInfo.FindForAssetPath(
            "Packages/com.ptc.vuforia.engine/package.json");
        if (package == null)
        {
            if (reportSuccess)
                Debug.LogError("[Vuforia Webcam] No se encuentra el paquete Vuforia Engine.");
            return;
        }

        string driverPath = Path.GetFullPath(Path.Combine(package.resolvedPath,
            "Vuforia", "Plugins", "Windows", "x64", "UnityDriver.dll"));
        if (!File.Exists(driverPath))
        {
            Debug.LogError("[Vuforia Webcam] Falta UnityDriver.dll en el paquete instalado.");
            return;
        }

        IntPtr module = LoadLibraryExW(driverPath, IntPtr.Zero,
            SearchDllFolderAndDefaultDirectories);
        if (module == IntPtr.Zero)
        {
            int error = Marshal.GetLastWin32Error();
            Debug.LogError("[Vuforia Webcam] No se puede cargar UnityDriver.dll. " +
                "Codigo de Windows: " + error);
            return;
        }

        // Keep the module loaded for the editor session, including domain reloads.
        if (reportSuccess)
            Debug.Log("[Vuforia Webcam] Controlador de webcam cargado correctamente.");
    }
}
#endif
