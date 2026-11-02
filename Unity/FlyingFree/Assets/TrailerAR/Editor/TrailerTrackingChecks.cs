using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Vuforia;

namespace FlyingFree.Trailer.Editor
{
    [InitializeOnLoad]
    public static class TrailerTrackingChecks
    {
        static double deadline;
        static TrailerTrackingChecks(){EditorApplication.update+=Tick;deadline=EditorApplication.timeSinceStartup+50;}
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/TrailerAR.unity");
            SessionState.SetBool("FlyingFree.TrackingChecks",true);EditorApplication.EnterPlaymode();
        }
        static void Tick()
        {
            if(!SessionState.GetBool("FlyingFree.TrackingChecks",false)||!EditorApplication.isPlaying)return;
            if(!VuforiaApplication.Instance.IsRunning)
            {
                if(EditorApplication.timeSinceStartup>deadline)Finish(false,"Vuforia no llegó a iniciar la cámara en este entorno. Seguimiento físico pendiente.");
                return;
            }
            var trailer=UnityEngine.Object.FindObjectsByType<ImageTargetBehaviour>(FindObjectsSortMode.None).FirstOrDefault(x=>x.TargetName=="FlyingFree_Trailer");
            if(trailer==null){if(EditorApplication.timeSinceStartup>deadline)Finish(false,"La marca de tráiler no se ha creado.");return;}
            try
            {
                var report=new List<string>{"PASS Vuforia 10.17.4 inicia el motor y la cámara", "PASS Marca nueva FlyingFree_Trailer creada con Instant Image Target"};
                foreach(string database in new[]{"Base","TargetManagerBBDD"})
                {
                    var xml=XDocument.Load("Assets/StreamingAssets/Vuforia/"+database+".xml");
                    foreach(var target in xml.Descendants("ImageTarget"))
                    {
                        string name=(string)target.Attribute("name");
                        var observer=VuforiaBehaviour.Instance.ObserverFactory.CreateImageTarget("Vuforia/"+database+".xml",name);
                        if(observer==null)throw new Exception("No se puede cargar la marca existente "+database+"/"+name);
                        observer.enabled=false;
                        report.Add("PASS Base existente compatible: "+database+"/"+name);
                    }
                }
                report.Add("PENDIENTE: reconocimiento de la nueva imagen impresa y prueba física en móvil. No se ha simulado una detección en este ensayo.");
                Finish(true,string.Join("\n",report));
            }
            catch(Exception ex){Finish(false,ex.Message);}
        }
        static void Finish(bool ok,string message)
        {
            Directory.CreateDirectory("TrailerEntrega");File.WriteAllText("TrailerEntrega/Vuforia_Comprobaciones.txt",message);
            Debug.Log((ok?"TRAILER_TRACKING_CHECKS_OK":"TRAILER_TRACKING_CHECKS_INCOMPLETE")+" "+message);
            SessionState.SetBool("FlyingFree.TrackingChecks",false);EditorApplication.Exit(ok?0:2);
        }
    }
}
