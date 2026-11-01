using UnityEngine;

namespace FlyingFree.UI
{
    [ExecuteAlways]
    public sealed class SafeAreaFollower : MonoBehaviour
    {
        public RectTransform source;
        void OnEnable() { LateUpdate(); }
        void LateUpdate()
        {
            if (source == null) return;
            var rt=(RectTransform)transform;
            rt.anchorMin=source.anchorMin;rt.anchorMax=source.anchorMax;
            rt.offsetMin=rt.offsetMax=Vector2.zero;
        }
    }
}
