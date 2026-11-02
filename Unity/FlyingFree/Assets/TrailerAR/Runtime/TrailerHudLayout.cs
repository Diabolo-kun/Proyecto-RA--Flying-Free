using UnityEngine;
using UnityEngine.UI;

namespace FlyingFree.Trailer
{
    [ExecuteAlways]
    public sealed class TrailerHudLayout : MonoBehaviour
    {
        public RectTransform safe, header, controls, prompt;
        public CanvasScaler scaler;
        Vector2 lastSize; Rect lastSafe;
        void Update()
        {
            var size = new Vector2(Screen.width, Screen.height);
            var area = UnityEngine.Device.Screen.safeArea;
            if (size == lastSize && area == lastSafe) return;
            Apply(size, area);
        }
        public void Apply(Vector2 size, Rect area)
        {
            if (safe == null || size.x <= 0 || size.y <= 0) return;
            lastSize = size; lastSafe = area;
            bool wide = size.x > size.y;
            scaler.referenceResolution = wide ? new Vector2(1280, 720) : new Vector2(720,1280);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            safe.anchorMin = area.min / size; safe.anchorMax = area.max / size;
            safe.offsetMin = safe.offsetMax = Vector2.zero;
            header.anchorMin = new Vector2(0,1); header.anchorMax = Vector2.one;
            header.pivot = new Vector2(.5f,1); header.offsetMin = new Vector2(18,-112); header.offsetMax = new Vector2(-18,-12);
            controls.anchorMin = Vector2.zero; controls.anchorMax = new Vector2(1,0);
            controls.pivot = new Vector2(.5f,0); controls.offsetMin = new Vector2(18,12); controls.offsetMax = new Vector2(-18,wide?192:238);
            prompt.anchorMin = prompt.anchorMax = new Vector2(.5f,.55f);
            prompt.anchoredPosition = Vector2.zero; prompt.sizeDelta = new Vector2(340,370);
        }
    }
}
