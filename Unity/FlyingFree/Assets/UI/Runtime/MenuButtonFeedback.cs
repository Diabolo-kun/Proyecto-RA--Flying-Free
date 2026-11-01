using UnityEngine;
using UnityEngine.EventSystems;

namespace FlyingFree.UI
{
    /// <summary>Animates the visual child only, keeping the touch target stable.</summary>
    public sealed class MenuButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        public RectTransform visual;
        bool hovered,pressed,focused;
        public void OnPointerEnter(PointerEventData data){hovered=true;}
        public void OnPointerExit(PointerEventData data){hovered=false;pressed=false;}
        public void OnPointerDown(PointerEventData data){if(data.button==PointerEventData.InputButton.Left)pressed=true;}
        public void OnPointerUp(PointerEventData data){pressed=false;}
        public void OnSelect(BaseEventData data){focused=true;}
        public void OnDeselect(BaseEventData data){focused=false;}
        void Update()
        {
            if(visual==null)return;
            float target=pressed?.965f:(hovered||focused?1.025f:1);
            visual.localScale=Vector3.Lerp(visual.localScale,Vector3.one*target,1-Mathf.Exp(-18*Time.unscaledDeltaTime));
            visual.anchoredPosition=Vector2.Lerp(visual.anchoredPosition,new Vector2(0,pressed?-5:0),1-Mathf.Exp(-18*Time.unscaledDeltaTime));
        }
        void OnDisable(){hovered=pressed=focused=false;if(visual!=null){visual.localScale=Vector3.one;visual.anchoredPosition=Vector2.zero;}}
    }
}
