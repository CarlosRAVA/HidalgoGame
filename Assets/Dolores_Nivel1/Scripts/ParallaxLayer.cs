using UnityEngine;


namespace Dolores
{
    /// <summary>Capa de fondo con efecto parallax y repetición horizontal.</summary>
    [DefaultExecutionOrder(100)]
    public class ParallaxLayer : MonoBehaviour
    {
        public Transform cam;
        [Tooltip("0 = fijo al mundo, 1 = pegado a la cámara (infinitamente lejos)")]
        public float speedX = 0.3f;
        public float speedY = 0.15f;
        [Tooltip("Ancho de una copia para repetir (0 = no repetir)")]
        public float tileWidth = 0f;
        public float drift = 0f;

        bool ready;
        float baseX, baseY, camBaseX, camBaseY, driftOffset;

        void LateUpdate()
        {
            if (cam == null)
            {
                if (Camera.main != null) cam = Camera.main.transform;
                if (cam == null) return;
            }
            if (!ready)
            {
                ready = true;
                baseX = transform.position.x;
                baseY = transform.position.y;
                camBaseX = cam.position.x;
                camBaseY = cam.position.y;
            }
            driftOffset += drift * Time.deltaTime;
            float cx = cam.position.x;
            float x = baseX + (cx - camBaseX) * speedX + driftOffset;
            if (tileWidth > 0f)
            {
                float rel = x - cx;
                rel = Mathf.Repeat(rel + tileWidth * 0.5f, tileWidth) - tileWidth * 0.5f;
                x = cx + rel;
            }
            float y = baseY + (cam.position.y - camBaseY) * speedY;
            transform.position = new Vector3(x, y, transform.position.z);
        }
    }
}
