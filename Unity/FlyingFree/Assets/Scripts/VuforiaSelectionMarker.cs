using UnityEngine;
#if FLYINGFREE_VUFORIA
using Vuforia;
#endif

namespace FlyingFree.Sprint2
{
    public sealed class VuforiaSelectionMarker : MonoBehaviour
    {
        public PlaneMarkerSelector selector;
        public Texture2D markerImage;
        [Min(.01f)] public float printedWidthMeters = .20f;
#if FLYINGFREE_VUFORIA
        ImageTargetBehaviour observer;
        void OnEnable()
        {
            VuforiaApplication.Instance.OnVuforiaStarted += CreateTarget;
            if (observer != null) { observer.OnTargetStatusChanged += StatusChanged; StatusChanged(observer,observer.TargetStatus); }
        }
        void CreateTarget()
        {
            if (observer != null || markerImage == null || VuforiaBehaviour.Instance == null) return;
            observer = VuforiaBehaviour.Instance.ObserverFactory.CreateImageTarget(markerImage, printedWidthMeters, "FlyingFree_Selector");
            if (observer == null) return;
            observer.OnTargetStatusChanged += StatusChanged;
            StatusChanged(observer, observer.TargetStatus);
        }
        void StatusChanged(ObserverBehaviour _, TargetStatus status)
        {
            bool tracked = status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED;
            if (selector != null) selector.SetMarkerVisible(tracked, observer.transform);
        }
        void OnDisable()
        {
            VuforiaApplication.Instance.OnVuforiaStarted -= CreateTarget;
            if (observer != null) observer.OnTargetStatusChanged -= StatusChanged;
            if (selector != null) selector.SetMarkerVisible(false);
        }
#endif
        // Alternative: connect tracking callbacks from an existing ImageTarget/ARTrackedImage.
        public void TargetFound() { if (selector != null) selector.SetMarkerVisible(true,transform); }
        public void TargetLost() { if (selector != null) selector.SetMarkerVisible(false,transform); }
    }
}
