using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Ponlo en cualquier objeto con Collider (puede ser trigger o sólido)
/// para que cuente como objetivo cuando el avión lo toque.
/// </summary>
public class PlaneTarget : MonoBehaviour
{
    [SerializeField] private UnityEvent onHit;

    public void NotifyHit(PaperPlane plane)
    {
        onHit?.Invoke();
        Debug.Log($"{name}: objetivo alcanzado");
    }
}
