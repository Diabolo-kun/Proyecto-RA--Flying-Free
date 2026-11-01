using System;
using UnityEngine;

namespace FlyingFree.Sprint2
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PaperPlaneMotor : MonoBehaviour
    {
        public AircraftDefinition definition;
        public Transform visual;
        public bool IsFlying { get; private set; }
        public bool IsCaptured { get; private set; }
        public float Speed { get; private set; }
        public event Action<Collider> Hit;
        Rigidbody body;
        Vector2 input, steering;
        float yaw;
        Quaternion launchBasis;
        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }
        public void Launch(float power01)
        {
            if (definition == null || IsCaptured) return;
            Speed = Mathf.Lerp(definition.minimumSpeed, definition.maximumSpeed, Mathf.Clamp01(power01));
            launchBasis = transform.rotation; yaw = 0; steering = Vector2.zero;
            IsFlying = true;
        }
        public void SetSteering(Vector2 joystick) { input = Vector2.ClampMagnitude(joystick, 1); }
        public void SetCaptured(bool value)
        {
            IsCaptured = value; IsFlying = false; input = steering = Vector2.zero;
            if(body!=null){body.interpolation=value?RigidbodyInterpolation.None:RigidbodyInterpolation.Interpolate;body.detectCollisions=!value;}
            if(value && visual!=null)visual.localRotation=Quaternion.identity;
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = !value;
        }
        public void Stop() { IsFlying = false; input = steering = Vector2.zero; }
        void FixedUpdate()
        {
            if (!IsFlying || IsCaptured || definition == null) return;
            float dt = Time.fixedDeltaTime;
            steering = Vector2.Lerp(steering, input, 1 - Mathf.Exp(-definition.steeringResponse * dt));
            yaw += steering.x * definition.turnSpeed * dt;
            var rotation = launchBasis * Quaternion.Euler(-steering.y * 28, yaw, 0);
            var movement = rotation * Vector3.forward * Speed + Vector3.down * (.34f - definition.lift);
            body.MoveRotation(rotation); body.MovePosition(body.position + movement * dt);
            if (visual != null) visual.localRotation = Quaternion.Euler(0, 0, -steering.x * 23);
        }
        void OnTriggerEnter(Collider other) { if (IsFlying && !IsCaptured) Hit?.Invoke(other); }
    }
}
