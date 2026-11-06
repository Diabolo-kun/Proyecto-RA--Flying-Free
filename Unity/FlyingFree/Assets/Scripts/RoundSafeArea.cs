using UnityEngine;

/// <summary>Mantiene el HUD fuera de la cámara frontal y de las zonas no utilizables del móvil.</summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public sealed class RoundSafeArea : MonoBehaviour
{
    void OnEnable() { Apply(); }
    void LateUpdate() { Apply(); }
    void Apply()
    {
        if (Screen.width <= 0 || Screen.height <= 0) return;
        Rect safe = Screen.safeArea;
        var rect = (RectTransform)transform;
        rect.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        rect.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
