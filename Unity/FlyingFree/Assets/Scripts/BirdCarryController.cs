using UnityEngine;

namespace FlyingFree.Sprint2
{
    public sealed class BirdCarryController : MonoBehaviour
    {
        public Animator animator;
        public Transform gripSocket;
        [Min(.1f)] public float grabAtSeconds = 1.6f;
        [Min(2f)] public float captureDuration = 4f;
        [Min(.1f)] public float carrySpeed = .8f;
        public bool HasPlane => carried != null;
        PaperPlaneMotor target, carried;
        float elapsed;
        Vector3 startPosition, getaway;
        Quaternion approachRotation;
        bool capturing;
        public bool BeginCapture(PaperPlaneMotor plane)
        {
            if (plane == null || plane.IsCaptured || capturing || carried != null || gripSocket == null || animator == null) return false;
            target = plane; elapsed = 0; capturing = true; startPosition = transform.position;
            Quaternion planeRotation=plane.visual!=null ? plane.visual.rotation : plane.transform.rotation;
            approachRotation=planeRotation*Quaternion.Inverse(gripSocket.rotation)*transform.rotation;
            animator.CrossFadeInFixedTime("Capture", .08f);
            return true;
        }
        public void GrabPlane()
        {
            if (!capturing || carried != null || target == null || gripSocket == null) return;
            Vector3 offset = target.definition != null ? target.definition.gripOffset : Vector3.zero;
            // Align the bird to the moving plane, so the plane does not jump at attachment.
            Quaternion planeRotation=target.visual!=null ? target.visual.rotation : target.transform.rotation;
            var correction = planeRotation * Quaternion.Inverse(gripSocket.rotation);
            transform.rotation = correction * transform.rotation;
            transform.position += target.transform.position - gripSocket.TransformPoint(offset);
            carried = target; target = null; carried.SetCaptured(true);
            Vector3 worldScale = carried.transform.lossyScale;
            carried.transform.SetParent(gripSocket, false);
            carried.transform.localPosition = offset; carried.transform.localRotation = Quaternion.identity;
            var parentScale = gripSocket.lossyScale;
            carried.transform.localScale = new Vector3(worldScale.x/parentScale.x,worldScale.y/parentScale.y,worldScale.z/parentScale.z);
            getaway = (transform.rotation * new Vector3(.2f,.45f,1)).normalized;
        }
        void LateUpdate()
        {
            if (!capturing && carried == null) return;
            elapsed += Time.deltaTime;
            if (carried == null)
            {
                if (target == null) { capturing = false; animator.CrossFadeInFixedTime("Fly",.12f); return; }
                Quaternion planeRotation=target.visual!=null ? target.visual.rotation : target.transform.rotation;
                approachRotation=planeRotation*Quaternion.Inverse(gripSocket.rotation)*transform.rotation;
                transform.rotation = Quaternion.Slerp(transform.rotation, approachRotation, 1-Mathf.Exp(-10*Time.deltaTime));
                var offset = target.definition != null ? target.definition.gripOffset : Vector3.zero;
                Vector3 desired = transform.position + target.transform.position - gripSocket.TransformPoint(offset);
                float t = Mathf.Clamp01(elapsed / grabAtSeconds); t = t*t*(3-2*t);
                transform.position = Vector3.Lerp(startPosition, desired, t);
                if (elapsed >= grabAtSeconds) GrabPlane();
            }
            else transform.position += getaway * carrySpeed * Time.deltaTime;
            if (elapsed >= captureDuration && capturing) { capturing = false; animator.CrossFadeInFixedTime("Fly",.16f); }
        }
    }
}
