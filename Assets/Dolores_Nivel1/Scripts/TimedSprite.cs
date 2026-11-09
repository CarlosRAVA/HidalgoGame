using UnityEngine;

namespace Dolores
{
    /// <summary>Efectos visuales pequeños (chispas, polvo) hechos con sprites que se desvanecen.</summary>
    public class TimedSprite : MonoBehaviour
    {
        public float life = 0.35f;
        public float startScale = 0.6f;
        public float endScale = 1.2f;
        public Vector2 velocity;
        public Color color = Color.white;
        SpriteRenderer sr;
        float t;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / life);
            transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, k);
            transform.position += (Vector3)(velocity * Time.deltaTime);
            if (sr != null)
            {
                var c = color;
                c.a = color.a * (1f - k);
                sr.color = c;
            }
            if (t >= life) Destroy(gameObject);
        }
    }
}
