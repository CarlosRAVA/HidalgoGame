using UnityEngine;


namespace Dolores1810
{
    /// <summary>Mantiene el cielo cubriendo toda la pantalla.</summary>
    [DefaultExecutionOrder(100)]
    public class SkyFollow : MonoBehaviour
    {
        public Camera cam;
        public SpriteRenderer reference;

        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null || reference == null || reference.sprite == null) return;
            Vector3 p = cam.transform.position;
            transform.position = new Vector3(p.x, p.y, 0f);
            float oh = cam.orthographicSize;
            float w = 2f * oh * cam.aspect;
            float h = 2f * oh;
            Vector2 sz = reference.sprite.bounds.size;
            float s = Mathf.Max(w / sz.x, h / sz.y) * 1.06f;
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
