using UnityEngine;

namespace FlyingFree.UI
{
    public sealed class PlaneFloat : MonoBehaviour
    {
        [Range(0,20)] public float amplitude = 8;
        [Range(0,2)] public float speed = .7f;
        void Update()
        {
            float t=Time.unscaledTime*speed;
            transform.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t)*2);
            // Visual child only: doesn't compete with the parent's responsive anchors.
            var rt=(RectTransform)transform;
            rt.anchoredPosition=new Vector2(0,Mathf.Sin(t*1.3f)*amplitude);
        }
        void OnDisable()
        {
            transform.localRotation=Quaternion.identity;
            ((RectTransform)transform).anchoredPosition=Vector2.zero;
        }
    }
}
