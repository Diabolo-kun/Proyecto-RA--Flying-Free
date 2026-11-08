using System;
using System.Collections.Generic;
using UnityEngine;
using FlyingFree.Sprint2;

/// <summary>
/// Define una variante o apariencia (skin) de avión de papel en el juego.
/// Soporta vincular directamente un perfil ScriptableObject (AircraftDefinition)
/// o asignar los datos y prefabs manualmente.
/// </summary>
[Serializable]
public class PlaneSkinEntry
{
    [Tooltip("Perfil ScriptableObject del avión (AircraftDefinition). Si se asigna, toma automáticamente el nombre, descripción, prefab y thumbnail")]
    public AircraftDefinition perfil;

    [Tooltip("Identificador único de la variante (si se deja vacío, toma el id del perfil o nombre del asset)")]
    public string id;

    [Tooltip("Nombre legible para mostrar en el menú de configuración (si se deja vacío, toma el displayName del perfil)")]
    public string nombreVisible;

    [Tooltip("Descripción o características de la variante (opcional)")]
    [TextArea(2, 4)]
    public string descripcion;

    [Tooltip("Prefab directo del avión (opcional si ya está configurado en el perfil)")]
    public PaperPlane prefab;

    [Tooltip("Nombre exacto del ImageTarget en Vuforia que activa/desbloquea esta variante (ej: 'ascorazones', 'joker')")]
    public string targetTrackableName;

    [Tooltip("Icono representativo de la variante (opcional)")]
    public Sprite icono;

    [Tooltip("Si es true, la variante está disponible desde el primer inicio sin necesidad de escanear")]
    public bool desbloqueadoPorDefecto;

    /// <summary>
    /// ID efectivo resolviendo entre el campo manual y el perfil AircraftDefinition.
    /// </summary>
    public string EffectiveId
    {
        get
        {
            if (!string.IsNullOrEmpty(id)) return id;
            if (perfil != null && !string.IsNullOrEmpty(perfil.id)) return perfil.id;
            if (perfil != null) return perfil.name;
            return string.Empty;
        }
    }

    /// <summary>
    /// Nombre visible efectivo resolviendo entre el campo manual y el perfil AircraftDefinition.
    /// </summary>
    public string EffectiveDisplayName
    {
        get
        {
            if (!string.IsNullOrEmpty(nombreVisible)) return nombreVisible;
            if (perfil != null && !string.IsNullOrEmpty(perfil.displayName)) return perfil.displayName;
            if (perfil != null) return perfil.name;
            return "Estándar";
        }
    }

    /// <summary>
    /// Obtiene el componente PaperPlane del prefab directo o del prefab asignado en el perfil.
    /// </summary>
    public PaperPlane GetPrefab()
    {
        if (prefab != null) return prefab;
        if (perfil != null && perfil.prefab != null)
        {
            return perfil.prefab.GetComponent<PaperPlane>();
        }
        return null;
    }
}

/// <summary>
/// Gestor central y persistente de variantes y apariencias de aviones de papel.
/// - Escalable a cualquier número de variantes presentes y futuras.
/// - Admite perfiles de avión (AircraftDefinition) y prefabs directos.
/// - Desbloquea y equipa variantes automáticamente al escanear marcas de Vuforia.
/// - Guarda las variantes desbloqueadas y la seleccionada en PlayerPrefs.
/// - Permite ciclar entre variantes desbloqueadas o restablecer al original.
/// - Notifica cambios a la UI mediante eventos C# (OnSkinChanged, OnSkinUnlocked).
/// </summary>
[DefaultExecutionOrder(-50)]
public class PlaneSkinManager : MonoBehaviour
{
    private static PlaneSkinManager _instance;

    public static PlaneSkinManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<PlaneSkinManager>();
            }
            return _instance;
        }
        private set => _instance = value;
    }

    private const string PrefKeySelectedSkin = "FlyingFree.SelectedSkinId";
    private const string PrefKeyUnlockedSkins = "FlyingFree.UnlockedSkinsList";

    [Header("Perfil de Avión por Defecto")]
    [Tooltip("Perfil ScriptableObject por defecto (arrastra aquí Estandar.asset)")]
    [SerializeField] private AircraftDefinition perfilPorDefecto;

    [Header("Lista de Variantes de Aviones (Modular y Escalable)")]
    [Tooltip("Añade aquí todas las variantes de avión presentes y futuras")]
    [SerializeField] private List<PlaneSkinEntry> skinsDisponibles = new List<PlaneSkinEntry>();

    [Header("Prefab de Respaldo")]
    [Tooltip("Prefab de respaldo si no se usa perfil ni hay skins en la lista")]
    [SerializeField] private PaperPlane defaultPlanePrefab;

    public static event Action<PlaneSkinEntry> OnSkinChanged;
    public static event Action<PlaneSkinEntry> OnSkinUnlocked;

    private PlaneSkinEntry skinActivo;
    private readonly HashSet<string> skinsDesbloqueados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public PlaneSkinEntry SkinActivo => skinActivo;
    public IReadOnlyList<PlaneSkinEntry> SkinsDisponibles => skinsDisponibles;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        CargarSkinsDesbloqueados();
        CargarSkinGuardado();
    }

    /// <summary>
    /// Carga el catálogo de variantes desbloqueadas desde PlayerPrefs.
    /// </summary>
    private void CargarSkinsDesbloqueados()
    {
        skinsDesbloqueados.Clear();

        // 1. Añadir las variantes marcadas como desbloqueadas por defecto (o la primera si no hay ninguna)
        for (int i = 0; i < skinsDisponibles.Count; i++)
        {
            if (skinsDisponibles[i].desbloqueadoPorDefecto || i == 0)
            {
                string id = skinsDisponibles[i].EffectiveId;
                if (!string.IsNullOrEmpty(id))
                    skinsDesbloqueados.Add(id);
            }
        }

        // Si hay perfil por defecto y la lista estaba vacía
        if (skinsDisponibles.Count == 0 && perfilPorDefecto != null)
        {
            string defId = !string.IsNullOrEmpty(perfilPorDefecto.id) ? perfilPorDefecto.id : perfilPorDefecto.name;
            if (!string.IsNullOrEmpty(defId)) skinsDesbloqueados.Add(defId);
        }

        // 2. Cargar las variantes desbloqueadas previamente por el jugador
        string guardados = PlayerPrefs.GetString(PrefKeyUnlockedSkins, "");
        if (!string.IsNullOrEmpty(guardados))
        {
            string[] ids = guardados.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string id in ids)
            {
                string idLimpio = id.Trim();
                if (!string.IsNullOrEmpty(idLimpio))
                    skinsDesbloqueados.Add(idLimpio);
            }
        }
    }

    /// <summary>
    /// Guarda el catálogo de skins desbloqueados en PlayerPrefs.
    /// </summary>
    private void GuardarSkinsDesbloqueados()
    {
        string guardados = string.Join(",", skinsDesbloqueados);
        PlayerPrefs.SetString(PrefKeyUnlockedSkins, guardados);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Comprueba si una variante específica está desbloqueada.
    /// </summary>
    public bool IsSkinUnlocked(string skinId)
    {
        if (string.IsNullOrEmpty(skinId)) return false;
        return skinsDesbloqueados.Contains(skinId);
    }

    /// <summary>
    /// Desbloquea una variante y la añade a la colección permanente del jugador.
    /// </summary>
    public bool UnlockSkin(string skinId)
    {
        if (string.IsNullOrEmpty(skinId)) return false;

        PlaneSkinEntry entry = FindSkinEntry(skinId);
        string id = entry != null ? entry.EffectiveId : skinId;

        if (!skinsDesbloqueados.Contains(id))
        {
            skinsDesbloqueados.Add(id);
            GuardarSkinsDesbloqueados();
            string nombre = entry != null ? entry.EffectiveDisplayName : id;
            Debug.Log($"[PlaneSkinManager] ¡Variante desbloqueada!: {nombre} ({id})");
            if (entry != null) OnSkinUnlocked?.Invoke(entry);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Devuelve todas las variantes actualmente desbloqueadas para su uso.
    /// </summary>
    public List<PlaneSkinEntry> GetUnlockedSkins()
    {
        List<PlaneSkinEntry> list = new List<PlaneSkinEntry>();
        foreach (var skin in skinsDisponibles)
        {
            if (IsSkinUnlocked(skin.EffectiveId))
                list.Add(skin);
        }
        return list;
    }

    /// <summary>
    /// Carga la variante guardada en PlayerPrefs o equipa la primera por defecto.
    /// </summary>
    public void CargarSkinGuardado()
    {
        string guardado = PlayerPrefs.GetString(PrefKeySelectedSkin, "");

        if (!string.IsNullOrEmpty(guardado))
        {
            skinActivo = FindSkinEntry(guardado);
        }

        if (skinActivo == null && skinsDisponibles.Count > 0)
        {
            skinActivo = skinsDisponibles[0];
        }

        Debug.Log($"[PlaneSkinManager] Skin cargado: {GetActiveSkinName()}");
        OnSkinChanged?.Invoke(skinActivo);
    }

    /// <summary>
    /// Busca una entrada en la lista por su ID.
    /// </summary>
    public PlaneSkinEntry FindSkinEntry(string skinId)
    {
        if (string.IsNullOrEmpty(skinId)) return null;
        return skinsDisponibles.Find(s => s.EffectiveId.Equals(skinId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Devuelve el prefab de PaperPlane que debe instanciarse según el skin activo.
    /// </summary>
    public PaperPlane GetActivePrefab()
    {
        if (skinActivo != null)
        {
            PaperPlane p = skinActivo.GetPrefab();
            if (p != null) return p;
        }

        if (skinsDisponibles.Count > 0)
        {
            PaperPlane p = skinsDisponibles[0].GetPrefab();
            if (p != null) return p;
        }

        if (perfilPorDefecto != null && perfilPorDefecto.prefab != null)
        {
            PaperPlane p = perfilPorDefecto.prefab.GetComponent<PaperPlane>();
            if (p != null) return p;
        }

        return defaultPlanePrefab;
    }

    /// <summary>
    /// Devuelve el nombre del skin actual para mostrarlo en el texto de configuración.
    /// </summary>
    public string GetActiveSkinName()
    {
        if (skinActivo != null)
        {
            return skinActivo.EffectiveDisplayName;
        }

        if (skinsDisponibles.Count > 0)
        {
            return skinsDisponibles[0].EffectiveDisplayName;
        }

        if (perfilPorDefecto != null && !string.IsNullOrEmpty(perfilPorDefecto.displayName))
        {
            return perfilPorDefecto.displayName;
        }

        return "Estándar";
    }

    /// <summary>
    /// Equipa una variante por su ID (ej: 'estandar', 'ascorazones').
    /// </summary>
    public bool EquipSkinById(string skinId)
    {
        PlaneSkinEntry entry = FindSkinEntry(skinId);
        if (entry != null)
        {
            UnlockSkin(entry.EffectiveId);
            skinActivo = entry;
            PlayerPrefs.SetString(PrefKeySelectedSkin, entry.EffectiveId);
            PlayerPrefs.Save();

            Debug.Log($"[PlaneSkinManager] ¡Skin equipado: {entry.EffectiveDisplayName}!");
            OnSkinChanged?.Invoke(skinActivo);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Busca si alguna variante de avión está asociada al nombre de la marca de Vuforia,
    /// la desbloquea de forma permanente y la equipa.
    /// </summary>
    public bool UnlockAndEquipByTrackable(string trackableName)
    {
        if (string.IsNullOrEmpty(trackableName)) return false;

        PlaneSkinEntry entry = skinsDisponibles.Find(s =>
            (!string.IsNullOrEmpty(s.targetTrackableName) && s.targetTrackableName.Equals(trackableName, StringComparison.OrdinalIgnoreCase)) ||
            s.EffectiveId.Equals(trackableName, StringComparison.OrdinalIgnoreCase));

        if (entry != null)
        {
            UnlockSkin(entry.EffectiveId);

            if (skinActivo != entry)
            {
                skinActivo = entry;
                PlayerPrefs.SetString(PrefKeySelectedSkin, entry.EffectiveId);
                PlayerPrefs.Save();

                Debug.Log($"[PlaneSkinManager] ¡Marca Vuforia '{trackableName}' detectada! Variante equipada: {entry.EffectiveDisplayName}");
                OnSkinChanged?.Invoke(skinActivo);
            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// Cambia a la siguiente variante desbloqueada disponible en la colección.
    /// </summary>
    public void EquipNextSkin()
    {
        List<PlaneSkinEntry> unlocked = GetUnlockedSkins();
        if (unlocked.Count <= 1) return;

        int currentIndex = unlocked.FindIndex(s => s.EffectiveId.Equals(skinActivo?.EffectiveId, StringComparison.OrdinalIgnoreCase));
        int nextIndex = (currentIndex + 1) % unlocked.Count;
        EquipSkinById(unlocked[nextIndex].EffectiveId);
    }

    /// <summary>
    /// Cambia a la variante desbloqueada anterior en la colección.
    /// </summary>
    public void EquipPreviousSkin()
    {
        List<PlaneSkinEntry> unlocked = GetUnlockedSkins();
        if (unlocked.Count <= 1) return;

        int currentIndex = unlocked.FindIndex(s => s.EffectiveId.Equals(skinActivo?.EffectiveId, StringComparison.OrdinalIgnoreCase));
        int prevIndex = (currentIndex - 1 + unlocked.Count) % unlocked.Count;
        EquipSkinById(unlocked[prevIndex].EffectiveId);
    }

    /// <summary>
    /// Restablece el avión al modelo original (primer elemento de la lista o perfil por defecto).
    /// Llamado desde el botón de reiniciar en configuración.
    /// </summary>
    public void ResetToDefault()
    {
        PlaneSkinEntry cuaderno = skinsDisponibles.Find(s =>
            (!string.IsNullOrEmpty(s.EffectiveId) && s.EffectiveId.IndexOf("cuaderno", StringComparison.OrdinalIgnoreCase) >= 0) ||
            (!string.IsNullOrEmpty(s.EffectiveDisplayName) && s.EffectiveDisplayName.IndexOf("cuaderno", StringComparison.OrdinalIgnoreCase) >= 0) ||
            (s.perfil != null && s.perfil.name != null && s.perfil.name.IndexOf("cuaderno", StringComparison.OrdinalIgnoreCase) >= 0));

        if (cuaderno != null)
        {
            skinActivo = cuaderno;
            PlayerPrefs.SetString(PrefKeySelectedSkin, skinActivo.EffectiveId);
        }
        else if (skinsDisponibles.Count > 0)
        {
            skinActivo = skinsDisponibles[0];
            PlayerPrefs.SetString(PrefKeySelectedSkin, skinActivo.EffectiveId);
        }
        else if (perfilPorDefecto != null)
        {
            string defId = !string.IsNullOrEmpty(perfilPorDefecto.id) ? perfilPorDefecto.id : perfilPorDefecto.name;
            PlayerPrefs.SetString(PrefKeySelectedSkin, defId);
        }
        else
        {
            PlayerPrefs.DeleteKey(PrefKeySelectedSkin);
        }

        PlayerPrefs.SetString("FlyingFree.AvionSeleccionado", "Avion_Cuaderno");
        PlayerPrefs.Save();

        Debug.Log($"[PlaneSkinManager] Avión restablecido a Cuaderno: {GetActiveSkinName()}");
        OnSkinChanged?.Invoke(skinActivo);
    }
}
