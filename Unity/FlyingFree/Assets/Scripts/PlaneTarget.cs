using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Ponlo en cualquier objeto con Collider (puede ser trigger o sólido)
/// para que cuente como objetivo cuando el avión lo toque.
/// </summary>
public class PlaneTarget : MonoBehaviour
{
    [SerializeField] private UnityEvent onHit;
    public bool IsHit { get; private set; }

    public void NotifyHit(PaperPlane plane)
    {
        if (!isActiveAndEnabled || IsHit || plane == null || !plane.CanInteract) return;
        // Reservar ambos objetos antes de invocar código externo evita callbacks reentrantes.
        IsHit = true;
        plane.CompleteTargetHit();
        onHit?.Invoke();
        Debug.Log($"{name}: objetivo alcanzado");
    }
}
