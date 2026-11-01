using UnityEngine;
using UnityEngine.UI;

namespace FlyingFree.UI
{
    [ExecuteAlways]
    public sealed class MenuBackground : MonoBehaviour
    {
        public RawImage image;
        public Texture landscape,portrait;
        public void Apply(Vector2 viewport)
        {
            if(image==null)return;
            var texture=viewport.x/viewport.y>=1.15f?landscape:portrait;
            if(texture==null)return;
            image.texture=texture;
            float source=(float)texture.width/texture.height, target=viewport.x/viewport.y;
            // Cover without stretching or letterboxing.
            image.uvRect=target>source?new Rect(0,(1-source/target)*.5f,1,source/target):new Rect((1-target/source)*.5f,0,target/source,1);
        }
        void OnEnable(){Apply(new Vector2(Mathf.Max(1,Screen.width),Mathf.Max(1,Screen.height)));}
    }
}
