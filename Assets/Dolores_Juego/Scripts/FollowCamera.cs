using UnityEngine;

namespace Dolores1810
{
    /// <summary>Cámara lateral suave con anticipación, límites del nivel, sacudida y enfoque de cinemáticas.</summary>
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(-50)]
    public class FollowCamera : MonoBehaviour
    {
        public Transform target;
        public float minX = 0f;
        public float maxX = 100f;
        public float minY = 2.6f;
        public float maxY = 14f;
        public float yOffset = 2.0f;
        public float lookAhead = 2.4f;
        public float smoothX = 5f;
        public float smoothY = 3.5f;

        Camera cam;
        float lookX;
        float shakeTime, shakeDur, shakeMag;
        Vector3 basePos;
        PlatformerPlayer player;
        Transform focus;
        float focusSize;
        float defaultSize;

        void Awake()
        {
            cam = GetComponent<Camera>();
            defaultSize = cam.orthographicSize;
        }

        void Start()
        {
            if (target != null) player = target.GetComponent<PlatformerPlayer>();
            Snap();
        }

        public void Shake(float magnitude, float duration)
        {
            if (magnitude >= shakeMag || shakeTime <= 0f)
            {
                shakeMag = magnitude;
                shakeDur = duration;
                shakeTime = duration;
            }
        }

        public void FocusOn(Transform t, float size)
        {
            focus = t;
            focusSize = size;
        }

        public void ClearFocus()
        {
            focus = null;
        }

        float HalfWidth() { return cam.orthographicSize * cam.aspect; }

        Vector3 Desired(bool useLook)
        {
            Transform t = focus != null ? focus : target;
            if (t == null) return transform.position;
            float lx = (useLook && focus == null) ? lookX : 0f;
            float x = t.position.x + lx;
            float yo = focus != null ? 1.2f : yOffset;
            float y = Mathf.Clamp(t.position.y + yo, minY, maxY);
            float hw = HalfWidth();
            float lo = minX + hw, hi = maxX - hw;
            if (hi < lo) { lo = hi = (minX + maxX) * 0.5f; }
            x = Mathf.Clamp(x, lo, hi);
            return new Vector3(x, y, transform.position.z);
        }

        public void Snap()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (target != null && player == null) player = target.GetComponent<PlatformerPlayer>();
            var d = Desired(false);
            basePos = d;
            transform.position = d;
        }

        void LateUpdate()
        {
            if (target == null) return;
            float dt = Time.deltaTime;
            float face = player != null ? player.Facing : 1f;
            lookX = Mathf.Lerp(lookX, face * lookAhead, dt * 2.2f);

            float wantSize = focus != null ? focusSize : defaultSize;
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, wantSize, 1f - Mathf.Exp(-2.2f * Time.unscaledDeltaTime));

            var d = Desired(true);
            basePos.x = Mathf.Lerp(basePos.x, d.x, 1f - Mathf.Exp(-smoothX * dt));
            basePos.y = Mathf.Lerp(basePos.y, d.y, 1f - Mathf.Exp(-smoothY * dt));
            basePos.z = d.z;
            Vector3 p = basePos;
            if (shakeTime > 0f)
            {
                shakeTime -= dt;
                float k = Mathf.Clamp01(shakeTime / Mathf.Max(0.01f, shakeDur));
                p.x += Random.Range(-1f, 1f) * shakeMag * k;
                p.y += Random.Range(-1f, 1f) * shakeMag * k;
                if (shakeTime <= 0f) shakeMag = 0f;
            }
            transform.position = p;
        }
    }
}
