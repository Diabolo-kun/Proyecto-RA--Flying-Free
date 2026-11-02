using UnityEngine;
using UnityEngine.EventSystems;

namespace FlyingFree.Trailer
{
    public sealed class TrailerSeekBar : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        public TrailerPlayer player;
        public void OnBeginDrag(PointerEventData data) { player.BeginScrub(); }
        public void OnEndDrag(PointerEventData data) { player.EndScrub(); }
        public void OnPointerDown(PointerEventData data) { player.BeginScrub(); }
        public void OnPointerUp(PointerEventData data) { player.EndScrub(); }
    }
}
