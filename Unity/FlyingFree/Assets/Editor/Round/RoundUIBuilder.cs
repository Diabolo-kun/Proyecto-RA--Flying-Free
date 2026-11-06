using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public static class RoundUIBuilder
{
    static readonly Color Navy = new Color(.07f, .17f, .23f, .96f);
    static readonly Color Cream = new Color(.98f, .95f, .85f);
    static readonly Color Gold = new Color(.95f, .65f, .17f);
    static GameObject Object(string name, Transform parent)
    {
        var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false); return obj;
    }
    static RectTransform Rect(GameObject obj, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        var rt = obj.GetComponent<RectTransform>(); rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = offsetMin; rt.offsetMax = offsetMax; return rt;
    }
    static GameObject Panel(string name, Transform parent, Color color)
    {
        var obj = Object(name, parent); obj.AddComponent<Image>().color = color; return obj;
    }
    static TextMeshProUGUI Text(string name, Transform parent, string text, int size, Color color, TextAlignmentOptions align)
    {
        var obj = Object(name, parent); var label = obj.AddComponent<TextMeshProUGUI>();
        label.text = text; label.font = TMP_Settings.defaultFontAsset; label.fontSize = size; label.color = color;
        label.alignment = align; label.raycastTarget = false; label.textWrappingMode = TextWrappingModes.Normal;
        Rect(obj, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); return label;
    }
    static void Button(Transform parent, string name, string label, float y, UnityAction action)
    {
        var obj = Panel(name, parent, Gold); var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(.5f,.5f); rt.pivot = new Vector2(.5f,.5f); rt.sizeDelta = new Vector2(620,90); rt.anchoredPosition = new Vector2(0,y);
        var button = obj.AddComponent<Button>(); button.targetGraphic = obj.GetComponent<Image>(); UnityEventTools.AddPersistentListener(button.onClick,action);
        Text("Texto",obj.transform,label,34,Navy,TextAlignmentOptions.Center);
    }
    static GameObject Result(Transform parent, bool victory, GameManagerRA game, out TextMeshProUGUI summary)
    {
        var panel=Panel(victory?"Victoria":"Derrota",parent,new Color(.03f,.08f,.12f,.91f));
        Rect(panel,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        var card=Panel("Tarjeta",panel.transform,Navy); var rt=card.GetComponent<RectTransform>();
        rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.sizeDelta=new Vector2(810,710);rt.anchoredPosition=Vector2.zero;
        var title=Text("Titulo",card.transform,victory?"¡HAS GANADO!":"TIEMPO AGOTADO",58,Gold,TextAlignmentOptions.Center);
        Rect(title.gameObject,new Vector2(0,1),Vector2.one,new Vector2(30,-150),new Vector2(-30,-30));
        summary=Text("Resumen",card.transform,"0 de 5 globos\n0 aciertos · 0 fallos · 0 robos",32,Cream,TextAlignmentOptions.Center);
        Rect(summary.gameObject,new Vector2(0,1),Vector2.one,new Vector2(35,-360),new Vector2(-35,-155));
        Button(card.transform,"Repetir","VOLVER A JUGAR",-130,game.VolverAJugar);
        Button(card.transform,"Menu","MENÚ PRINCIPAL",-245,game.VolverAlMenuPrincipal);
        panel.SetActive(false); return panel;
    }
    static void Assign(SerializedObject game,string field,UnityEngine.Object value) { game.FindProperty(field).objectReferenceValue=value; }
    [MenuItem("Flying Free/Sprint 2b/Preparar HUD de partida")]
    public static void Build()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/GameRA.unity");
        var game=UnityEngine.Object.FindAnyObjectByType<GameManagerRA>(); if(game==null)throw new System.Exception("No hay GameManagerRA");
        var previous=GameObject.Find("HUD_Partida");if(previous!=null)UnityEngine.Object.DestroyImmediate(previous);
        foreach(string name in new[]{"BallonCounter","TextTimer","BallonCountText","BallonIcon"})
        {var old=GameObject.Find(name);if(old!=null)old.SetActive(false);}
        var root=new GameObject("HUD_Partida",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);scaler.matchWidthOrHeight=.5f;
        var safe=Object("ZonaSegura",root.transform);safe.AddComponent<RoundSafeArea>();
        Rect(safe,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        var top=Panel("Marcador",safe.transform,Navy);
        Rect(top,new Vector2(0,1),Vector2.one,new Vector2(35,-245),new Vector2(-35,-25));
        top.GetComponent<Image>().raycastTarget=false;
        var brand=Text("FlyingFree",top.transform,"FLYING FREE",26,Gold,TextAlignmentOptions.TopLeft);
        Rect(brand.gameObject,Vector2.zero,Vector2.one,new Vector2(30,0),new Vector2(-30,-15));
        var points=Text("Globos",top.transform,"GLOBOS  0 / 5",44,Cream,TextAlignmentOptions.Center);
        Rect(points.gameObject,new Vector2(0,.45f),new Vector2(.66f,.84f),new Vector2(20,0),Vector2.zero);
        var time=Text("Tiempo",top.transform,"00:30",48,Gold,TextAlignmentOptions.Center);
        Rect(time.gameObject,new Vector2(.66f,.45f),new Vector2(1,.84f),Vector2.zero,new Vector2(-20,0));
        var planes=Text("Aviones",top.transform,"AVIONES  ∞  ·  LANZADOS 0",27,Cream,TextAlignmentOptions.Center);
        Rect(planes.gameObject,new Vector2(0,.2f),new Vector2(1,.45f),new Vector2(15,0),new Vector2(-15,0));
        var status=Text("Estado",top.transform,"APUNTA A LA MARCA PARA CONTINUAR",23,Gold,TextAlignmentOptions.Center);
        Rect(status.gameObject,Vector2.zero,new Vector2(1,.22f),new Vector2(15,8),new Vector2(-15,0));
        var win=Result(safe.transform,true,game,out var winText);var lose=Result(safe.transform,false,game,out var loseText);
        var so=new SerializedObject(game);
        Assign(so,"textoTiempo",time);Assign(so,"textoPuntuacion",points);Assign(so,"textoAviones",planes);Assign(so,"textoEstado",status);
        Assign(so,"canvasVictoria",win);Assign(so,"canvasDerrota",lose);Assign(so,"textoResultadoVictoria",winText);Assign(so,"textoResultadoDerrota",loseText);
        so.FindProperty("avionesIlimitados").boolValue=true;so.FindProperty("avionesIniciales").intValue=5;
        so.FindProperty("globosObjetivoBase").intValue=5;so.FindProperty("globosParaGanar").intValue=5;
        so.FindProperty("objetivoProporcionalAlTiempo").boolValue=false;so.FindProperty("tiempoDeJuego").floatValue=30;
        so.FindProperty("esperarSeguimientoAR").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();
        foreach(var spawn in UnityEngine.Object.FindObjectsByType<TargetPlaneSpawner>(FindObjectsSortMode.None))
        {spawn.testModeWithoutVuforia=false;EditorUtility.SetDirty(spawn);}
        EditorUtility.SetDirty(game);EditorSceneManager.MarkSceneDirty(game.gameObject.scene);EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
        Debug.Log("PASS GameRA: HUD, aviones ilimitados, objetivo 5 globos en 30 segundos y espera de marca configurados");
    }
}
