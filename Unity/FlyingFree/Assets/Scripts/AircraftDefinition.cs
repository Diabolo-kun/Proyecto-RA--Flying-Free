using UnityEngine;

namespace FlyingFree.Sprint2
{
    [CreateAssetMenu(menuName = "Flying Free/Perfil de avion")]
    public sealed class AircraftDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public GameObject prefab;
        public Texture2D thumbnail;
        public Color accent = Color.cyan;
        [Min(.1f)] public float minimumSpeed = 1.7f;
        [Min(.1f)] public float maximumSpeed = 3.3f;
        [Min(1f)] public float turnSpeed = 110f;
        [Min(.1f)] public float steeringResponse = 4.6f;
        [Range(0,.33f)] public float lift = .2f;
        [Tooltip("Compensacion del agarre lateral del ala respecto a Cuaderno, en metros.")]
        public Vector3 gripOffset;
        public float Speed01 => Mathf.InverseLerp(2.45f, 4.6f, maximumSpeed);
        public float Control01 => Mathf.InverseLerp(76f, 148f, turnSpeed);
    }
}
