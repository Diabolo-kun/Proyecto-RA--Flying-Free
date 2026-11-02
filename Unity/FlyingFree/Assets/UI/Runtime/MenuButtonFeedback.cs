using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FlyingFree.UI
{
    /// <summary>Animates the visual child only, keeping the touch target stable.</summary>
    public sealed class MenuButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public RectTransform visual;
        bool hovered,pressed;
        public void OnPointerEnter(PointerEventData data){hovered=true; AplicarColor(true);}
        public void OnPointerExit(PointerEventData data){hovered=false;pressed=false; AplicarColor(false);}
        public void OnPointerDown(PointerEventData data){if(data.button==PointerEventData.InputButton.Left)pressed=true;}
        public void OnPointerUp(PointerEventData data){pressed=false;}
        void Update()
        {
            if(visual==null)return;
            float target=pressed?.965f:(hovered?1.025f:1);
            visual.localScale=Vector3.Lerp(visual.localScale,Vector3.one*target,1-Mathf.Exp(-18*Time.unscaledDeltaTime));
            visual.anchoredPosition=Vector2.Lerp(visual.anchoredPosition,new Vector2(0,pressed?-5:0),1-Mathf.Exp(-18*Time.unscaledDeltaTime));
        }
        void AplicarColor(bool encima)
        {
            Button boton = GetComponent<Button>();
            if (boton == null) return;
            ColorBlock colores = boton.colors;
            colores.selectedColor = encima ? colores.highlightedColor : colores.normalColor;
            boton.colors = colores;
        }
        void OnDisable(){hovered=pressed=false;AplicarColor(false);if(visual!=null){visual.localScale=Vector3.one;visual.anchoredPosition=Vector2.zero;}}
    }
}
