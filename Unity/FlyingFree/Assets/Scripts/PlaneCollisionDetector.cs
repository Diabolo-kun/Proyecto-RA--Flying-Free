using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Barrido de los colliders del avión entre dos poses de vuelo. Complementa los triggers:
/// mover un Transform en LateUpdate puede atravesar un globo entre pasos de PhysX.
/// No mueve el avión ni decide puntuación o robos: envía contactos al mismo resolutor.
/// </summary>
internal sealed class PlaneCollisionDetector
{
    readonly PaperPlane plane;
    readonly List<Collider> sources = new List<Collider>();
    readonly Dictionary<Collider, float> distances = new Dictionary<Collider, float>();
    readonly List<Collider> ordered = new List<Collider>();
    readonly Comparison<Collider> compare;
    Collider[] overlaps = new Collider[32];
    RaycastHit[] hits = new RaycastHit[32];

    public PlaneCollisionDetector(PaperPlane owner) { plane = owner; compare = Compare; }

    public void CheckMovement(Vector3 previousPosition, Quaternion previousRotation)
    {
        if (!plane.CanInteract) return;
        // Los globos, las poses Vuforia y el avión se mueven por Transform.
        Physics.SyncTransforms();
        plane.GetComponentsInChildren(false, sources);
        distances.Clear(); ordered.Clear();
        Quaternion undoRotation = previousRotation * Quaternion.Inverse(plane.transform.rotation);
        foreach (var source in sources)
        {
            if (!source.enabled || source.GetComponentInParent<PaperPlane>() != plane) continue;
            Shape shape = Shape.From(source);
            Vector3 start = previousPosition + undoRotation * (shape.center - plane.transform.position);
            Vector3 delta = shape.center - start;
            Quaternion startRotation = undoRotation * shape.rotation;
            QueryOverlaps(source, shape, start, startRotation, 0);
            float length = delta.magnitude;
            if (length > .000001f)
            {
                int count;
                while (true)
                {
                    count = shape.sphere
                        ? Physics.SphereCastNonAlloc(start, shape.radius, delta / length, hits, length,
                            Physics.AllLayers, QueryTriggerInteraction.Collide)
                        : Physics.BoxCastNonAlloc(start, shape.extents, delta / length, hits, startRotation,
                            length, Physics.AllLayers, QueryTriggerInteraction.Collide);
                    if (count < hits.Length) break;
                    Array.Resize(ref hits, hits.Length * 2);
                }
                for (int i = 0; i < count; i++) Add(source, hits[i].collider, hits[i].distance / length);
            }
            // Incluye contactos iniciales, avión quieto y la nueva orientación tras girar.
            QueryOverlaps(source, shape, shape.center, shape.rotation, 1);
        }
        ordered.AddRange(distances.Keys);
        ordered.Sort(compare);
        foreach (var target in ordered)
        {
            if (!plane.CanInteract) break;
            plane.HandleContact(target, Vector3.Lerp(previousPosition, plane.transform.position, distances[target]));
        }
    }

    void QueryOverlaps(Collider source, Shape shape, Vector3 center, Quaternion rotation, float fraction)
    {
        int count;
        while (true)
        {
            count = shape.sphere
                ? Physics.OverlapSphereNonAlloc(center, shape.radius, overlaps, Physics.AllLayers, QueryTriggerInteraction.Collide)
                : Physics.OverlapBoxNonAlloc(center, shape.extents, overlaps, rotation, Physics.AllLayers, QueryTriggerInteraction.Collide);
            if (count < overlaps.Length) break;
            Array.Resize(ref overlaps, overlaps.Length * 2);
        }
        for (int i = 0; i < count; i++) Add(source, overlaps[i], fraction);
    }

    void Add(Collider source, Collider target, float fraction)
    {
        if (target == null || !target.enabled || !target.gameObject.activeInHierarchy
            || target.GetComponentInParent<PaperPlane>() != null
            || Physics.GetIgnoreLayerCollision(source.gameObject.layer, target.gameObject.layer)
            || Physics.GetIgnoreCollision(source, target)) return;
        // Los triggers decorativos, de selección y de cámara no son obstáculos.
        if (target.isTrigger && target.GetComponentInParent<GloboTarget>() == null
            && target.GetComponentInParent<PajaroInteraccion>() == null
            && target.GetComponentInParent<PlaneTarget>() == null) return;
        if (!distances.TryGetValue(target, out float old) || fraction < old) distances[target] = fraction;
    }

    int Compare(Collider a, Collider b)
    {
        int result = distances[a].CompareTo(distances[b]);
        return result != 0 ? result : a.GetInstanceID().CompareTo(b.GetInstanceID());
    }

    struct Shape
    {
        public Vector3 center, extents;
        public Quaternion rotation;
        public bool sphere;
        public float radius;
        public static Shape From(Collider collider)
        {
            var scale = collider.transform.lossyScale;
            scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            if (collider is SphereCollider ball)
                return new Shape { sphere = true, center = ball.transform.TransformPoint(ball.center),
                    radius = Mathf.Max(.0001f, ball.radius * Mathf.Max(scale.x, scale.y, scale.z)), rotation = Quaternion.identity };
            if (collider is BoxCollider box)
                return new Shape { center = box.transform.TransformPoint(box.center),
                    extents = Vector3.Max(Vector3.Scale(box.size * .5f, scale), Vector3.one * .0001f),
                    rotation = box.transform.rotation };
            // Respaldo conservador para colliders personalizados. Los cinco aviones usan BoxCollider.
            return new Shape { center = collider.bounds.center, extents = collider.bounds.extents, rotation = Quaternion.identity };
        }
    }
}
