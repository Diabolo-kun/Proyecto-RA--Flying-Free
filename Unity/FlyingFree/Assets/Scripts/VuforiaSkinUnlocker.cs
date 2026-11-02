using UnityEngine;
using Vuforia;

/// <summary>
/// Componente genérico para ImageTargets de Vuforia.
/// Cuando la cámara enfoca la marca en el mundo real, desbloquea y equipa
/// automáticamente el skin de avión asociado en PlaneSkinManager.
/// </summary>
public class VuforiaSkinUnlocker : MonoBehaviour
{
    [Tooltip("Nombre de la marca a equipar. Si se deja vacío, toma automáticamente el nombre del ObserverBehaviour de Vuforia")]
    [SerializeField] private string trackableNameOverride = "";

    [Header("Efecto de Audio / Feedback (Opcional)")]
    [Tooltip("Clip de sonido que sonará al desbloquear/equipar el nuevo skin")]
    [SerializeField] private AudioClip unlockSound;

    private ObserverBehaviour observer;

    private void Awake()
    {
        observer = GetComponent<ObserverBehaviour>();
        if (observer != null)
        {
            observer.OnTargetStatusChanged += OnTargetStatusChanged;
        }
    }

    private void OnDestroy()
    {
        if (observer != null)
        {
            observer.OnTargetStatusChanged -= OnTargetStatusChanged;
        }
    }

    private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        // Detectar cuando el target entra en visión de la cámara (Tracked)
        if (status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED)
        {
            ActivarSkin();
        }
    }

    /// <summary>
    /// Intenta desbloquear y equipar la variante correspondiente en PlaneSkinManager.
    /// </summary>
    public void ActivarSkin()
    {
        string nombreMarca = !string.IsNullOrEmpty(trackableNameOverride)
            ? trackableNameOverride
            : (observer != null ? observer.TargetName : gameObject.name);

        if (PlaneSkinManager.Instance != null)
        {
            bool cambiado = PlaneSkinManager.Instance.UnlockAndEquipByTrackable(nombreMarca);
            if (cambiado && unlockSound != null)
            {
                AudioSource.PlayClipAtPoint(unlockSound, transform.position);
            }
        }
        else
        {
            Debug.LogWarning($"[VuforiaSkinUnlocker] Marca '{nombreMarca}' detectada, pero no hay PlaneSkinManager en la escena.");
        }
    }
}
