using UnityEngine;
using UnityEngine.UI;

namespace FlyingFree.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PaperPlaneGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            for(int i=0;i<12;i++)
            {
                float t=i/11f; float x=.03f+t*.42f,y=.12f+Mathf.Sin(t*3.2f)*.16f;
                float r=.006f;
                Poly(vh,new Color(1,1,1,.25f+t*.5f),new Vector2(x-r,y),new Vector2(x,y+r*2),new Vector2(x+r,y),new Vector2(x,y-r*2));
            }
            Color navy=new Color(.08f,.25f,.38f,.20f);
            Poly(vh,navy,new Vector2(.31f,.59f),new Vector2(.96f,.92f),new Vector2(.72f,.19f),new Vector2(.58f,.43f),new Vector2(.48f,.20f),new Vector2(.47f,.48f));
            Poly(vh,new Color(.99f,1,1),new Vector2(.30f,.64f),new Vector2(.95f,.97f),new Vector2(.49f,.54f));
            Poly(vh,new Color(.72f,.86f,.92f),new Vector2(.49f,.54f),new Vector2(.95f,.97f),new Vector2(.47f,.25f));
            Poly(vh,new Color(.88f,.95f,1),new Vector2(.58f,.48f),new Vector2(.95f,.97f),new Vector2(.71f,.24f));
            Poly(vh,new Color(.40f,.66f,.79f),new Vector2(.49f,.54f),new Vector2(.58f,.48f),new Vector2(.47f,.25f));
        }
        void Poly(VertexHelper vh,Color color,params Vector2[] points)
        {
            int start=vh.currentVertCount;
            foreach(var p in points)
            {
                var v=UIVertex.simpleVert;
                v.position=new Vector2(rectTransform.rect.xMin+p.x*rectTransform.rect.width,rectTransform.rect.yMin+p.y*rectTransform.rect.height);
                v.color=color;vh.AddVert(v);
            }
            for(int i=1;i<points.Length-1;i++)vh.AddTriangle(start,start+i,start+i+1);
        }
    }
}
