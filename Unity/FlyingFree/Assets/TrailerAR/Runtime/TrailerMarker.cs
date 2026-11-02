using System;
using UnityEngine;
using Vuforia;

namespace FlyingFree.Trailer
{
    /// <summary>Creates a distinct local Instant Image Target; never modifies existing skin databases.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class TrailerMarker : MonoBehaviour
    {
        public Texture2D markerImage;
        public float printedWidthMeters = .20f;
        public TrailerPlayer player;
        ImageTargetBehaviour observer;
        float lastPoll;
        void OnEnable()
        {
            VuforiaApplication.Instance.OnVuforiaStarted += CreateTarget;
            VuforiaApplication.Instance.OnVuforiaInitialized += Initialized;
            if (VuforiaApplication.Instance.IsRunning) CreateTarget();
        }
        void Initialized(VuforiaInitError error)
        {
            if (error != VuforiaInitError.NONE)
                player.SetTrackingError("No se puede iniciar la cámara RA. Revisa el permiso de cámara y la licencia de Vuforia.");
        }
        void CreateTarget()
        {
            if (observer != null || !isActiveAndEnabled) return;
            try
            {
                observer = VuforiaBehaviour.Instance.ObserverFactory.CreateImageTarget(markerImage, printedWidthMeters, "FlyingFree_Trailer");
                if (observer == null) throw new InvalidOperationException("No se pudo crear la marca.");
                observer.OnTargetStatusChanged += StatusChanged;
                player.ClearTrackingError();
                StatusChanged(observer, observer.TargetStatus);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Marca del tráiler: " + ex.Message, this);
                player.SetTrackingError("No se ha podido cargar la marca TRÁILER. Vuelve al menú e inténtalo de nuevo.");
            }
        }
        void StatusChanged(ObserverBehaviour source, TargetStatus status)
        {
            // Require the actual image: extended tracking alone must not keep the movie playing.
            player.SetMarkerVisible(VuforiaApplication.Instance.IsRunning && status.Status == Status.TRACKED, observer.transform);
        }
        void Update()
        {
            if (observer == null || Time.unscaledTime - lastPoll < .1f) return;
            lastPoll = Time.unscaledTime;
            bool tracked = VuforiaApplication.Instance.IsRunning && observer.TargetStatus.Status == Status.TRACKED;
            if (tracked != player.MarkerVisible) player.SetMarkerVisible(tracked, observer.transform);
        }
        void OnDisable()
        {
            VuforiaApplication.Instance.OnVuforiaStarted -= CreateTarget;
            VuforiaApplication.Instance.OnVuforiaInitialized -= Initialized;
            if (observer != null)
            {
                observer.OnTargetStatusChanged -= StatusChanged;
                // The surface belongs to the scene, not to the observer lifetime.
                if (player != null && player.surfaceRoot != null) { player.surfaceRoot.SetParent(null, true); player.SetMarkerVisible(false); }
                Destroy(observer.gameObject); observer = null;
            }
        }
    }
}
