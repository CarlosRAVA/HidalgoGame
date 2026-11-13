using UnityEngine;

namespace Dolores1810
{
    /// <summary>Vida ambiental: pájaros que cruzan el cielo de día y luciérnagas de noche.</summary>
    public class AmbientLife : MonoBehaviour
    {
        public Sprite[] birdFrames;
        public Sprite glowSprite;
        public Material unlit;
        public DayNightCycle cycle;
        public Camera cam;
        public int fireflyCount = 16;
        public bool forceNight;

        class Bird { public Transform t; public SpriteRenderer sr; public float speed; public float phase; public float baseY; }
        class Fly { public Transform t; public SpriteRenderer sr; public Vector2 offset; public float phase; public float speed; }

        Bird[] birds = new Bird[3];
        Fly[] flies;
        float birdTimer = 4f;

        void Start()
        {
            if (cam == null) cam = Camera.main;
            flies = new Fly[fireflyCount];
            for (int i = 0; i < flies.Length; i++)
            {
                var go = new GameObject("Luciernaga");
                go.transform.SetParent(transform);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = glowSprite;
                sr.sortingOrder = 45;
                if (unlit != null) sr.sharedMaterial = unlit;
                sr.color = new Color(0.9f, 1f, 0.5f, 0f);
                go.transform.localScale = Vector3.one * Random.Range(0.18f, 0.32f);
                var f = new Fly();
                f.t = go.transform; f.sr = sr;
                f.offset = new Vector2(Random.Range(-9f, 9f), Random.Range(0.4f, 3.6f));
                f.phase = Random.Range(0f, 6.28f); f.speed = Random.Range(0.6f, 1.4f);
                flies[i] = f;
            }
        }

        void Update()
        {
            if (cam == null) return;
            Vector3 cp = cam.transform.position;
            float night = forceNight ? 1f : (cycle != null ? cycle.NightWeight : 0f);
            float day = forceNight ? 0f : (cycle != null ? cycle.DayWeight : 1f);

            for (int i = 0; i < flies.Length; i++)
            {
                var f = flies[i];
                float t = Time.time * f.speed + f.phase;
                f.t.position = new Vector3(cp.x + f.offset.x + Mathf.Sin(t) * 0.7f, 0.6f + f.offset.y + Mathf.Cos(t * 1.3f) * 0.4f, 0f);
                float a = night * (0.35f + 0.65f * Mathf.Abs(Mathf.Sin(t * 1.7f)));
                var c = f.sr.color; c.a = a; f.sr.color = c;
            }

            birdTimer -= Time.deltaTime;
            if (birdTimer <= 0f && day > 0.5f && birdFrames != null && birdFrames.Length > 0)
            {
                birdTimer = Random.Range(7f, 16f);
                for (int i = 0; i < birds.Length; i++)
                {
                    if (birds[i] != null) continue;
                    var go = new GameObject("Pajaro");
                    go.transform.SetParent(transform);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = -60;
                    if (unlit != null) sr.sharedMaterial = unlit;
                    sr.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);
                    var b = new Bird();
                    b.t = go.transform; b.sr = sr; b.speed = Random.Range(2.2f, 3.6f); b.phase = Random.Range(0f, 6f);
                    b.baseY = cp.y + Random.Range(1.5f, 3.4f);
                    go.transform.position = new Vector3(cp.x - 14f, b.baseY, 0f);
                    go.transform.localScale = Vector3.one * Random.Range(0.8f, 1.3f);
                    birds[i] = b;
                    break;
                }
            }
            for (int i = 0; i < birds.Length; i++)
            {
                var b = birds[i];
                if (b == null) continue;
                b.t.position += new Vector3(b.speed * Time.deltaTime, Mathf.Sin(Time.time * 1.5f + b.phase) * 0.4f * Time.deltaTime, 0f);
                int fr = ((int)(Time.time * 6f + b.phase)) % birdFrames.Length;
                b.sr.sprite = birdFrames[fr];
                if (b.t.position.x > cp.x + 16f) { Destroy(b.t.gameObject); birds[i] = null; }
            }
        }
    }
}
