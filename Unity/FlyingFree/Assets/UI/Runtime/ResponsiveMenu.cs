using UnityEngine;
using UnityEngine.UI;

namespace FlyingFree.UI
{
    [ExecuteAlways]
    public sealed class ResponsiveMenu : MonoBehaviour
    {
        public RectTransform safeArea, brand, plane, menuCard, footer, settingsCard;
        public CanvasScaler scaler;
        public MenuBackground background;
        public RectTransform trailerButton;
        [Tooltip("Editor QA only. Zero uses Screen.safeArea.")]
        public Rect simulatedSafeArea;
        public bool Landscape { get; private set; }
        Vector2 lastSize;
        Rect lastSafe;

        void OnEnable() { Refresh(true); }
        void Update() { Refresh(false); }
        public void Refresh(bool force = true)
        {
            if (safeArea == null || scaler == null || Screen.width <= 0 || Screen.height <= 0) return;
            Rect safe = simulatedSafeArea.width > 0 ? simulatedSafeArea : UnityEngine.Device.Screen.safeArea;
            Vector2 size = new Vector2(Screen.width, Screen.height);
            if (!force && size == lastSize && safe == lastSafe) return;
            ApplyViewport(size, safe);
        }
        /// <summary>Also used by the editor renderer to verify exact device sizes.</summary>
        public void ApplyViewport(Vector2 size, Rect safe)
        {
            lastSize = size; lastSafe = safe;
            safe.xMin = Mathf.Clamp(safe.xMin, 0, size.x);
            safe.xMax = Mathf.Clamp(safe.xMax, safe.xMin + 1, size.x);
            safe.yMin = Mathf.Clamp(safe.yMin, 0, size.y);
            safe.yMax = Mathf.Clamp(safe.yMax, safe.yMin + 1, size.y);
            safeArea.anchorMin = new Vector2(safe.xMin / size.x, safe.yMin / size.y);
            safeArea.anchorMax = new Vector2(safe.xMax / size.x, safe.yMax / size.y);
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            float aspect = safe.width / safe.height;
            Landscape = aspect >= 1.15f;
            if (background != null) background.Apply(size);
            scaler.referenceResolution = Landscape ? new Vector2(1920,1080) : new Vector2(1080,1920);
            // Expand fits both dimensions, including short phones and tablets.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            float canvasScale = Mathf.Min(size.x / scaler.referenceResolution.x, size.y / scaler.referenceResolution.y);
            scaler.GetComponent<Canvas>().scaleFactor = canvasScale;
            Canvas.ForceUpdateCanvases();
            Vector2 area = safe.size / canvasScale;
            if (area.x <= 0 || area.y <= 0) return;
            if (trailerButton != null)
            {
                float logoFraction = Landscape ? .34f : .42f;
                float controlsFraction = Landscape ? .58f : .39f;
                float logoScale = Mathf.Min((area.x-110)/800, area.y*logoFraction/800);
                float controlsScale = Mathf.Min((area.x-140)/800, area.y*controlsFraction/680);
                Place(brand,new Vector2(.5f,Landscape?.80f:.74f),new Vector2(800,800),logoScale);
                Place(menuCard,new Vector2(.5f,Landscape?.326f:.285f),new Vector2(800,680),controlsScale);
            }
            else if (Landscape)
            {
                float logoScale = Mathf.Min((area.x - 160) / 520, area.y * .50f / 520);
                Place(brand, new Vector2(.5f,.748f), new Vector2(520,520), logoScale);
                float controlsScale = Mathf.Min((area.x - 160) / 800, area.y * .435f / 510);
                Place(menuCard, new Vector2(.5f,.268f), new Vector2(800,510), controlsScale);
            }
            else
            {
                float logoScale = Mathf.Min((area.x - 110) / 800, area.y * .47f / 800);
                Place(brand, new Vector2(.5f,.728f), new Vector2(800,800), logoScale);
                float controlsScale = Mathf.Min((area.x - 140) / 800, area.y * .315f / 510);
                Place(menuCard, new Vector2(.5f,.282f), new Vector2(800,510), controlsScale);
            }
            if (plane != null) plane.gameObject.SetActive(false);
            if (footer != null) footer.gameObject.SetActive(false);
            Place(settingsCard, new Vector2(.5f,.5f), new Vector2(840,930), Mathf.Min(1.12f,Mathf.Min((area.x-80)/840,(area.y-70)/930)));
        }
        static void Place(RectTransform rect, Vector2 anchor, Vector2 size, float scale)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(.5f,.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one * Mathf.Max(.1f,scale);
        }
    }
}
