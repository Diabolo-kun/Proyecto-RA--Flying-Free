using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace FlyingFree.Video.Editor
{
    [InitializeOnLoad]
    public sealed class VideoMaterialPipeline : AssetPostprocessor
    {
        const string Root="Assets/FlyingFreeVideo";
        static bool scheduled;
        static string pipeline;
        static VideoMaterialPipeline(){EditorApplication.delayCall+=Adapt;EditorApplication.update+=Check;}
        static void Check(){string current=GraphicsSettings.currentRenderPipeline==null?"Builtin":GraphicsSettings.currentRenderPipeline.GetType().FullName;if(current!=pipeline){pipeline=current;Schedule();}}
        static void OnPostprocessAllAssets(string[] imported,string[] deleted,string[] moved,string[] previous){if(Array.Exists(imported,p=>p.StartsWith(Root,StringComparison.Ordinal)))Schedule();}
        static void Schedule(){if(scheduled)return;scheduled=true;EditorApplication.delayCall+=Adapt;}
        static void Adapt()
        {
            scheduled=false;if(EditorApplication.isPlayingOrWillChangePlaymode||!AssetDatabase.IsValidFolder(Root+"/Materials"))return;
            bool urp=GraphicsSettings.currentRenderPipeline!=null;
            foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{Root+"/Materials"}))
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));if(material==null||material.shader==null)continue;
                bool unlit=material.shader.name.IndexOf("Unlit",StringComparison.OrdinalIgnoreCase)>=0;
                string desired=unlit?(urp?"Universal Render Pipeline/Unlit":"Unlit/Color"):(urp?"Universal Render Pipeline/Lit":"Standard");
                if(material.shader.name==desired)continue;var shader=Shader.Find(desired);if(shader==null)continue;
                Color color=material.HasProperty("_BaseColor")?material.GetColor("_BaseColor"):material.color;var texture=material.mainTexture;
                material.shader=shader;material.color=color;if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",color);
                material.mainTexture=texture;if(material.HasProperty("_BaseMap"))material.SetTexture("_BaseMap",texture);
                if(material.HasProperty("_Metallic"))material.SetFloat("_Metallic",0);if(material.HasProperty("_Glossiness"))material.SetFloat("_Glossiness",.14f);if(material.HasProperty("_Smoothness"))material.SetFloat("_Smoothness",.14f);EditorUtility.SetDirty(material);
            }
        }
    }
}
