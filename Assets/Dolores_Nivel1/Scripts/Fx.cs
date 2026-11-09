using UnityEngine;

namespace Dolores
{
    public static class Fx
    {
        public static Sprite Spark, Dust;
        public static Material UnlitMat;

        static void Spawn(Sprite s, Vector3 pos, float a, float b, float life, Color col, Vector2 vel, int order)
        {
            if (s == null) return;
            var go = new GameObject("fx");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = order;
            if (UnlitMat != null) sr.sharedMaterial = UnlitMat;
            var ts = go.AddComponent<TimedSprite>();
            ts.life = life; ts.startScale = a; ts.endScale = b; ts.color = col; ts.velocity = vel;
            go.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        }

        public static void Hit(Vector3 pos)
        {
            Spawn(Spark, pos, 0.5f, 1.4f, 0.22f, new Color(1f, 0.93f, 0.6f, 1f), Vector2.zero, 50);
            for (int i = 0; i < 3; i++)
            {
                Spawn(Spark, pos, 0.2f, 0.45f, 0.35f, new Color(1f, 0.8f, 0.4f, 1f),
                    new Vector2(Random.Range(-3f, 3f), Random.Range(1f, 4f)), 50);
            }
        }

        public static void Pickup(Vector3 pos)
        {
            Spawn(Spark, pos, 0.3f, 1.0f, 0.3f, new Color(1f, 1f, 0.7f, 1f), Vector2.zero, 50);
        }

        public static void DustPuff(Vector3 pos, int count = 3)
        {
            for (int i = 0; i < count; i++)
            {
                Spawn(Dust, pos + new Vector3(Random.Range(-0.3f, 0.3f), 0.05f, 0f), 0.25f, 0.8f, 0.4f,
                    new Color(0.9f, 0.85f, 0.75f, 0.8f), new Vector2(Random.Range(-1.5f, 1.5f), Random.Range(0.2f, 1.2f)), 25);
            }
        }

        public static void Poof(Vector3 pos)
        {
            for (int i = 0; i < 6; i++)
            {
                Spawn(Dust, pos + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.2f, 1.4f), 0f), 0.4f, 1.4f, 0.6f,
                    new Color(0.85f, 0.85f, 0.9f, 0.9f), new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(0.3f, 1.5f)), 25);
            }
        }
    }
}
