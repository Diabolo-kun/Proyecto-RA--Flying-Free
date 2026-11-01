using UnityEngine;
using UnityEngine.EventSystems;

namespace FlyingFree.UI
{
    /// <summary>Creates an EventSystem only if the host scene doesn't already supply one.</summary>
    public sealed class MenuInput : MonoBehaviour
    {
        GameObject owned;
        void OnEnable()
        {
            if (!Application.isPlaying || FindFirstObjectByType<EventSystem>() != null) return;
            owned = new GameObject("Flying Free EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            owned.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            owned.AddComponent<StandaloneInputModule>();
#endif
        }
        void OnDisable() { if (owned != null) Destroy(owned); }
    }
}
