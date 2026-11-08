using UnityEngine;
using UnityEngine.UI;

namespace FlyingFree.UI
{
    /// <summary>Scalable UI icons, independent from button text and artwork.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MenuIconGraphic : MaskableGraphic
    {
        public enum Symbol { Play, Gear, Exit, Back, Speaker, Music, Check, Pause, Help }
        public Symbol symbol;
        VertexHelper mesh;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            mesh=vh;vh.Clear();
            switch(symbol)
            {
                case Symbol.Play:
                    Poly(color,new Vector2(.22f,.08f),new Vector2(.86f,.5f),new Vector2(.22f,.92f));break;
                case Symbol.Pause:
                    Poly(color,new Vector2(.22f,.14f),new Vector2(.40f,.14f),new Vector2(.40f,.86f),new Vector2(.22f,.86f));
                    Poly(color,new Vector2(.60f,.14f),new Vector2(.78f,.14f),new Vector2(.78f,.86f),new Vector2(.60f,.86f));break;
                case Symbol.Help:
                    // Interrogación simple para el botón de ayuda.
                    Circle(.50f,.72f,.10f);
                    Poly(color,new Vector2(.42f,.52f),new Vector2(.58f,.52f),new Vector2(.58f,.62f),new Vector2(.62f,.68f),new Vector2(.50f,.78f),new Vector2(.38f,.68f),new Vector2(.42f,.62f));
                    Circle(.50f,.28f,.08f);break;
                case Symbol.Gear:
                    for(int i=0;i<40;i++)
                    {
                        float t=i*Mathf.PI*2/40,u=(i+1)*Mathf.PI*2/40;
                        float r=(i%4<2)?.49f:.40f,s=((i+1)%4<2)?.49f:.40f;
                        Poly(color,Radial(t,r),Radial(u,s),Radial(u,.18f),Radial(t,.18f));
                    }break;
                case Symbol.Exit:
                    Poly(color,new Vector2(.40f,.1f),new Vector2(.90f,.1f),new Vector2(.90f,.90f),new Vector2(.40f,.90f));
                    Poly(Color.white,new Vector2(.40f,.43f),new Vector2(.69f,.43f),new Vector2(.69f,.57f),new Vector2(.40f,.57f));
                    Poly(Color.white,new Vector2(.57f,.27f),new Vector2(.82f,.50f),new Vector2(.57f,.73f),new Vector2(.57f,.58f),new Vector2(.68f,.5f),new Vector2(.57f,.42f));
                    Poly(color,new Vector2(.08f,.43f),new Vector2(.41f,.43f),new Vector2(.41f,.57f),new Vector2(.08f,.57f));break;
                case Symbol.Back:
                    Poly(color,new Vector2(.23f,.50f),new Vector2(.65f,.91f),new Vector2(.80f,.76f),new Vector2(.48f,.44f));
                    Poly(color,new Vector2(.23f,.50f),new Vector2(.48f,.56f),new Vector2(.80f,.24f),new Vector2(.65f,.09f));break;
                case Symbol.Speaker:
                    Poly(color,new Vector2(.10f,.34f),new Vector2(.30f,.34f),new Vector2(.60f,.12f),new Vector2(.60f,.88f),new Vector2(.30f,.66f),new Vector2(.10f,.66f));
                    Arc(.52f,.5f,.36f,-.9f,.9f,.07f);break;
                case Symbol.Music:
                    Poly(color,new Vector2(.35f,.28f),new Vector2(.45f,.28f),new Vector2(.45f,.73f),new Vector2(.80f,.80f),new Vector2(.80f,.39f),new Vector2(.9f,.39f),new Vector2(.9f,.96f),new Vector2(.35f,.86f));
                    Circle(.28f,.25f,.18f);Circle(.73f,.36f,.18f);break;
                case Symbol.Check:
                    Poly(color,new Vector2(.08f,.48f),new Vector2(.23f,.62f),new Vector2(.47f,.36f),new Vector2(.40f,.14f));
                    Poly(color,new Vector2(.40f,.14f),new Vector2(.29f,.31f),new Vector2(.77f,.86f),new Vector2(.93f,.72f));break;
            }
        }
        Vector2 Radial(float t,float radius) => new Vector2(.5f+Mathf.Cos(t)*radius,.5f+Mathf.Sin(t)*radius);
        void Circle(float x,float y,float radius)
        {
            var points=new Vector2[20];for(int i=0;i<20;i++){float t=i*Mathf.PI*2/20;points[i]=new Vector2(x+Mathf.Cos(t)*radius,y+Mathf.Sin(t)*radius);}Poly(color,points);
        }
        void Arc(float x,float y,float radius,float a,float b,float thickness)
        {
            for(int i=0;i<12;i++)
            {
                float t=Mathf.Lerp(a,b,i/12f),u=Mathf.Lerp(a,b,(i+1)/12f);
                Poly(color,new Vector2(x+Mathf.Cos(t)*radius,y+Mathf.Sin(t)*radius),new Vector2(x+Mathf.Cos(u)*radius,y+Mathf.Sin(u)*radius),new Vector2(x+Mathf.Cos(u)*(radius-thickness),y+Mathf.Sin(u)*(radius-thickness)),new Vector2(x+Mathf.Cos(t)*(radius-thickness),y+Mathf.Sin(t)*(radius-thickness)));
            }
        }
        void Poly(Color tint,params Vector2[] points)
        {
            int start=mesh.currentVertCount;var rect=rectTransform.rect;
            foreach(var p in points){var v=UIVertex.simpleVert;v.position=new Vector2(rect.xMin+p.x*rect.width,rect.yMin+p.y*rect.height);v.color=tint;mesh.AddVert(v);}
            for(int i=1;i<points.Length-1;i++)mesh.AddTriangle(start,start+i,start+i+1);
        }
    }
}
