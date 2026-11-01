using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace FlyingFree.Sprint2.Editor
{
    [InitializeOnLoad]
    public sealed class PipelineMaterialSupport : AssetPostprocessor
    {
        static bool queued;
        static PipelineMaterialSupport() { Queue(); EditorApplication.projectChanged += Queue; }
        public static Shader ActiveShader()
        {
            if(GraphicsSettings.currentRenderPipeline!=null)
            {
                var shader=Shader.Find("Universal Render Pipeline/Lit");if(shader!=null)return shader;
            }
            return Shader.Find("Standard");
        }
        static void Queue()
        {
            if(queued)return;queued=true;EditorApplication.delayCall+=Fix;
        }
        static void OnPostprocessAllAssets(string[] imported,string[] deleted,string[] moved,string[] movedFrom)
        {
            foreach(var path in imported)if(path.StartsWith("Assets/FlyingFreeSprint2/")){Queue();break;}
        }
        static void Fix()
        {
            queued=false;if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(!AssetDatabase.IsValidFolder("Assets/FlyingFreeSprint2/Materials"))return;
            Shader shader=ActiveShader();if(shader==null)return;bool changed=false;
            foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/FlyingFreeSprint2/Materials"}))
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));if(material==null||material.shader==shader)continue;
                Texture texture=material.HasProperty("_BaseMap")?material.GetTexture("_BaseMap"):material.mainTexture;
                Color color=material.HasProperty("_BaseColor")?material.GetColor("_BaseColor"):material.color;
                material.shader=shader;
                if(material.HasProperty("_BaseMap"))material.SetTexture("_BaseMap",texture);
                if(material.HasProperty("_MainTex"))material.SetTexture("_MainTex",texture);
                if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",color);
                if(material.HasProperty("_Color"))material.SetColor("_Color",color);
                if(material.HasProperty("_Smoothness"))material.SetFloat("_Smoothness",.08f);
                if(material.HasProperty("_Glossiness"))material.SetFloat("_Glossiness",.08f);
                EditorUtility.SetDirty(material);changed=true;
            }
            if(changed)AssetDatabase.SaveAssets();
        }
    }
}
