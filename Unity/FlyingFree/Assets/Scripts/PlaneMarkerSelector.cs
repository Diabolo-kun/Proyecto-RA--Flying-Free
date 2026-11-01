using System;
using UnityEngine;

namespace FlyingFree.Sprint2
{
    public sealed class PlaneMarkerSelector : MonoBehaviour
    {
        public AircraftDefinition[] aircraft;
        public Transform previewAnchor;
        public bool showInterface = true;
        public bool previewWithoutCameraTracking;
        public AircraftDefinition Selected { get; private set; }
        public bool MarkerVisible { get; private set; }
        public event Action<AircraftDefinition> SelectionChanged;
        GameObject preview;
        int selectedIndex;
        void Start()
        {
            if (aircraft != null && aircraft.Length > 0) Select(0);
            if (previewWithoutCameraTracking) SetMarkerVisible(true, previewAnchor);
        }
        public void SetMarkerVisible(bool visible, Transform anchor = null)
        {
            MarkerVisible = visible;
            if (anchor != null) previewAnchor = anchor;
            if (preview != null)
            {
                preview.transform.SetParent(previewAnchor, false);
                preview.transform.localPosition = new Vector3(0, .11f, 0);
                preview.SetActive(visible);
            }
        }
        public void Select(int index)
        {
            if (aircraft == null || index < 0 || index >= aircraft.Length || aircraft[index] == null) return;
            selectedIndex = index; Selected = aircraft[index];
            DeletePreview();
            if (Selected.prefab != null && previewAnchor != null)
            {
                preview = Instantiate(Selected.prefab, previewAnchor);
                preview.name = "Vista previa - " + Selected.displayName;
                preview.transform.localPosition = new Vector3(0, .11f, 0);
                preview.transform.localScale = Vector3.one * .65f;
                var motor = preview.GetComponent<PaperPlaneMotor>();
                if (motor != null) { motor.Stop(); motor.enabled = false; }
                foreach (var col in preview.GetComponentsInChildren<Collider>()) col.enabled = false;
                preview.SetActive(MarkerVisible);
            }
            SelectionChanged?.Invoke(Selected);
        }
        void Update()
        {
            if (preview != null && MarkerVisible) preview.transform.localRotation = Quaternion.Euler(-12, Time.time * 18, 0);
        }
        void OnGUI()
        {
            if (!showInterface) return;
            float scale = Screen.width / 960f;
            var old = GUI.matrix; GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            var title = new GUIStyle(GUI.skin.label) { fontSize = 25, alignment = TextAnchor.MiddleCenter };
            var text = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true, alignment = TextAnchor.MiddleCenter };
            GUI.Box(new Rect(30, 18, 900, 54), GUIContent.none);
            GUI.Label(new Rect(40, 23, 880, 44), MarkerVisible ? "FLYING FREE  ·  ELIGE TU AVION" : "Apunta al marcador Flying Free", title);
            if (MarkerVisible && aircraft != null)
            {
                for (int i = 0; i < aircraft.Length; i++)
                {
                    var d = aircraft[i]; if (d == null) continue;
                    Rect card = new Rect(36 + i * 300, 85, 288, 218);
                    GUI.color = i == selectedIndex ? d.accent : new Color(.35f,.4f,.44f);
                    GUI.Box(card, GUIContent.none); GUI.color = Color.white;
                    if (d.thumbnail != null) GUI.DrawTexture(new Rect(card.x+9,card.y+6,270,112),d.thumbnail,ScaleMode.ScaleToFit);
                    GUI.Label(new Rect(card.x+7,card.y+119,274,28),d.displayName,new GUIStyle(title){fontSize=19});
                    GUI.Label(new Rect(card.x+7,card.y+147,274,36),d.description,text);
                    GUI.Box(new Rect(card.x+42,card.y+187,204,25),i==selectedIndex ? "SELECCIONADO" : "ELEGIR");
                    if (GUI.Button(card,GUIContent.none,GUIStyle.none)) Select(i);
                }
            }
            GUI.matrix = old;
        }
        void DeletePreview()
        {
            if(preview==null)return;
            if(Application.isPlaying)Destroy(preview);else DestroyImmediate(preview);
            preview=null;
        }
        void OnDestroy() { DeletePreview(); }
    }
}
