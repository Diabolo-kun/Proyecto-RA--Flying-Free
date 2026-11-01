using UnityEngine;
using UnityEngine.UI;

namespace FlyingFree.UI
{
    /// <summary>Original low-poly park drawn as UI geometry. No textures, camera or render-pipeline dependencies.</summary>
    [AddComponentMenu("Flying Free/Park Graphic")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ParkGraphic : MaskableGraphic
    {
        VertexHelper mesh;
        float w, h;
        static Color C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var value); return value; }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            mesh = vh; vh.Clear(); w = rectTransform.rect.width; h = rectTransform.rect.height;
            if (w <= 0 || h <= 0) return;
            Quad(0,0,w,h,C("9DE5ED"),C("399CDC"));
            Circle(w*.79f,h*.81f,h*.055f,C("FFF0A3"),32);
            Circle(w*.79f,h*.81f,h*.073f,new Color(1,.96f,.68f,.15f),32);
            Cloud(.1f,.85f,.85f); Cloud(.88f,.64f,.65f); Cloud(.43f,.92f,.7f);
            Poly(C("90CBB6"), P(0,.30f),P(.09f,.38f),P(.21f,.32f),P(.35f,.40f),P(.48f,.32f),P(.65f,.39f),P(.83f,.31f),P(1,.38f),P(1,0),P(0,0));
            Poly(C("76B98B"), P(0,.24f),P(.15f,.32f),P(.28f,.25f),P(.44f,.33f),P(.63f,.26f),P(.8f,.33f),P(1,.27f),P(1,0),P(0,0));
            Quad(0,0,w,h*.24f,C("79BD67"),C("A6D77B"));
            Poly(C("E8DAA0"),P(.34f,0),P(.65f,0),P(.56f,.24f),P(.53f,.27f),P(.49f,.27f),P(.46f,.23f));
            Poly(C("F7EBC5"),P(.42f,0),P(.65f,0),P(.56f,.24f),P(.53f,.27f),P(.51f,.27f));
            Tree(.045f,.16f,.24f); Tree(.15f,.25f,.14f); Tree(.91f,.19f,.22f); Tree(.80f,.26f,.12f);
            Tree(.985f,.11f,.29f); Tree(.005f,.06f,.31f);
            Lamp(.24f,.10f,.20f); Lamp(.72f,.17f,.14f);
            Bench(.12f,.055f,.13f); Bench(.86f,.09f,.10f);
            // Foreground facets and little yellow flowers.
            Poly(C("5B9F5F"),P(0,0),P(0,.065f),P(.14f,.026f),P(.28f,0));
            Poly(C("68AC5F"),P(.69f,0),P(.89f,.055f),P(1,.035f),P(1,0));
            for (int i=0;i<9;i++) Circle(w*(.07f+i*.022f),h*(.026f+(i%3)*.014f),h*.003f,C("FFE17A"),5);
            for (int i=0;i<6;i++) Circle(w*(.76f+i*.03f),h*(.025f+(i%2)*.014f),h*.003f,C("FFE17A"),5);
        }
        Vector2 P(float x,float y) => new Vector2(x*w,y*h);
        void Cloud(float x,float y,float s)
        {
            float r=h*.025f*s; Color c=new Color(1,1,1,.78f);
            Circle(w*x-r*1.7f,h*y,r,c,18); Circle(w*x,h*y+r*.6f,r*1.3f,c,18);
            Circle(w*x+r*1.6f,h*y,r*.9f,c,18); Quad(w*x-r*1.8f,h*y-r*.7f,r*3.5f,r,c,c);
        }
        void Tree(float x,float y,float size)
        {
            float a=x*w,b=y*h,s=size*h;
            Quad(a-s*.045f,b,s*.09f,s*.66f,C("8C7255"),C("A4885D"));
            Poly(C("629E59"),new Vector2(a,b+s*.33f),new Vector2(a-s*.39f,b+s*.57f),new Vector2(a-s*.31f,b+s*.95f),new Vector2(a,b+s*1.13f),new Vector2(a+s*.37f,b+s*.84f),new Vector2(a+s*.32f,b+s*.48f));
            Poly(C("91C96B"),new Vector2(a-s*.39f,b+s*.57f),new Vector2(a-s*.31f,b+s*.95f),new Vector2(a,b+s*1.13f),new Vector2(a-s*.05f,b+s*.69f));
            Poly(C("B7DA7A"),new Vector2(a-s*.31f,b+s*.95f),new Vector2(a,b+s*1.13f),new Vector2(a+s*.37f,b+s*.84f),new Vector2(a+s*.08f,b+s*.82f));
            Poly(C("7EB65F"),new Vector2(a,b+s*.33f),new Vector2(a-s*.05f,b+s*.69f),new Vector2(a+s*.37f,b+s*.84f),new Vector2(a+s*.32f,b+s*.48f));
        }
        void Lamp(float x,float y,float size)
        {
            float a=x*w,b=y*h,s=size*h; Color ink=C("275C68");
            Quad(a-s*.025f,b,s*.05f,s*.80f,ink,ink);
            Quad(a-s*.06f,b,s*.12f,s*.035f,ink,ink);
            Poly(ink,new Vector2(a-s*.09f,b+s*.80f),new Vector2(a-s*.12f,b+s*.99f),new Vector2(a+s*.12f,b+s*.99f),new Vector2(a+s*.09f,b+s*.80f));
            Quad(a-s*.055f,b+s*.83f,s*.11f,s*.12f,C("FFF4BE"),C("FFF4BE"));
            Poly(ink,new Vector2(a-s*.15f,b+s*.99f),new Vector2(a,b+s*1.1f),new Vector2(a+s*.15f,b+s*.99f));
        }
        void Bench(float x,float y,float size)
        {
            float a=x*w,b=y*h,s=size*h; Color wood=C("C89858"), frame=C("326571");
            Quad(a-s*.4f,b,s*.05f,s*.4f,frame,frame); Quad(a+s*.35f,b,s*.05f,s*.4f,frame,frame);
            Quad(a-s*.5f,b+s*.23f,s,s*.07f,wood,wood); Quad(a-s*.5f,b+s*.34f,s,s*.08f,wood,wood);
            Quad(a-s*.53f,b+s*.16f,s*1.06f,s*.06f,C("E2B36B"),C("E2B36B"));
        }
        void Quad(float x,float y,float width,float height,Color bottom,Color top)
        {
            int i=mesh.currentVertCount;
            Add(new Vector2(x,y),bottom); Add(new Vector2(x,y+height),top); Add(new Vector2(x+width,y+height),top); Add(new Vector2(x+width,y),bottom);
            mesh.AddTriangle(i,i+1,i+2);mesh.AddTriangle(i,i+2,i+3);
        }
        void Circle(float x,float y,float r,Color color,int n)
        {
            var points=new Vector2[n];for(int i=0;i<n;i++){float t=i*Mathf.PI*2/n;points[i]=new Vector2(x+Mathf.Cos(t)*r,y+Mathf.Sin(t)*r);}Poly(color,points);
        }
        void Poly(Color color,params Vector2[] points)
        {
            int start=mesh.currentVertCount;foreach(var p in points)Add(p,color);
            for(int i=1;i<points.Length-1;i++)mesh.AddTriangle(start,start+i,start+i+1);
        }
        void Add(Vector2 position,Color color)
        {
            var v=UIVertex.simpleVert;v.position=position+rectTransform.rect.min;v.color=color;mesh.AddVert(v);
        }
    }
}
