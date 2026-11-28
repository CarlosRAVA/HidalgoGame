using UnityEngine;

namespace Dolores1810
{
    /// <summary>Efectos visuales pequeños hechos con sprites que se desvanecen.</summary>
    public static class Fx
    {
        public static Sprite Spark, Dust, Confetti;
        public static Material UnlitMat;

        static TimedSprite Spawn(Sprite s, Vector3 pos, float a, float b, float life, Color col, Vector2 vel, int order, float gravity)
        {
            if (s == null) return null;
            var go = new GameObject("fx");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = order;
            if (UnlitMat != null) sr.sharedMaterial = UnlitMat;
            var ts = go.AddComponent<TimedSprite>();
            ts.life = life; ts.startScale = a; ts.endScale = b; ts.color = col; ts.velocity = vel; ts.gravity = gravity;
            go.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            return ts;
        }

        public static void Hit(Vector3 pos)
        {
            Spawn(Spark, pos, 0.5f, 1.4f, 0.22f, new Color(1f, 0.93f, 0.6f, 1f), Vector2.zero, 50, 0f);
            for (int i = 0; i < 3; i++)
                Spawn(Spark, pos, 0.2f, 0.45f, 0.35f, new Color(1f, 0.8f, 0.4f, 1f), new Vector2(Random.Range(-3f, 3f), Random.Range(1f, 4f)), 50, 0f);
        }

        public static void Pickup(Vector3 pos)
        {
            Spawn(Spark, pos, 0.3f, 1.0f, 0.3f, new Color(1f, 1f, 0.7f, 1f), Vector2.zero, 50, 0f);
        }

        public static void Sparkle(Vector3 pos, Color c)
        {
            for (int i = 0; i < 6; i++)
                Spawn(Spark, pos + new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.2f, 0.6f), 0f), 0.15f, 0.5f, Random.Range(0.4f, 0.8f), c,
                    new Vector2(Random.Range(-0.8f, 0.8f), Random.Range(0.6f, 1.8f)), 50, 0f);
        }

        public static void DustPuff(Vector3 pos, int count = 3)
        {
            for (int i = 0; i < count; i++)
                Spawn(Dust, pos + new Vector3(Random.Range(-0.3f, 0.3f), 0.05f, 0f), 0.25f, 0.8f, 0.4f, new Color(0.9f, 0.85f, 0.75f, 0.8f),
                    new Vector2(Random.Range(-1.5f, 1.5f), Random.Range(0.2f, 1.2f)), 25, 0f);
        }

        public static void Poof(Vector3 pos)
        {
            for (int i = 0; i < 6; i++)
                Spawn(Dust, pos + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.2f, 1.4f), 0f), 0.4f, 1.4f, 0.6f, new Color(0.85f, 0.85f, 0.9f, 0.9f),
                    new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(0.3f, 1.5f)), 25, 0f);
        }

        public static void Splash(Vector3 pos)
        {
            for (int i = 0; i < 10; i++)
                Spawn(Dust, pos, 0.2f, 0.5f, 0.7f, new Color(0.6f, 0.85f, 1f, 0.9f), new Vector2(Random.Range(-3f, 3f), Random.Range(3f, 7f)), 40, 14f);
        }

        static readonly Color[] Party =
        {
            new Color(0.9f, 0.2f, 0.3f), new Color(1f, 0.8f, 0.2f), new Color(0.2f, 0.7f, 0.4f), new Color(0.25f, 0.5f, 0.95f), new Color(0.95f, 0.5f, 0.15f), new Color(0.7f, 0.3f, 0.85f)
        };

        public static void ConfettiBurst(Vector3 pos, int count)
        {
            Sprite s = Confetti != null ? Confetti : Spark;
            float cs = Confetti != null ? 4f : 1f;
            for (int i = 0; i < count; i++)
            {
                var ts = Spawn(s, pos, cs, cs, Random.Range(1.4f, 2.6f), Party[Random.Range(0, Party.Length)], new Vector2(Random.Range(-5f, 5f), Random.Range(3f, 9f)), 60, 9f);
                if (ts != null) ts.spin = Random.Range(-360f, 360f);
            }
        }

        public static void Firework(Vector3 pos)
        {
            Color c = Party[Random.Range(0, Party.Length)];
            for (int i = 0; i < 26; i++)
            {
                float a = i / 26f * Mathf.PI * 2f;
                float sp = Random.Range(2.5f, 5.5f);
                Spawn(Spark, pos, 0.25f, 0.6f, Random.Range(0.9f, 1.5f), c, new Vector2(Mathf.Cos(a) * sp, Mathf.Sin(a) * sp), 70, 3f);
            }
        }
    }
}
