using UnityEngine;
using UnityEngine.UI;

namespace FlyingFree.Trailer
{
    public sealed class TrailerPreview : MonoBehaviour
    {
        public TrailerPlayer player;
        public Transform simulatedAnchor;
        public Button toggleMarker;
        public Text label;
        void Awake() { toggleMarker.onClick.AddListener(ToggleMarker); }
        public void ToggleMarker()
        {
            bool visible = !player.MarkerVisible;
            player.SetMarkerVisible(visible,simulatedAnchor);
            label.text = visible ? "SIMULAR PÉRDIDA" : "SIMULAR MARCA";
        }
    }
}
