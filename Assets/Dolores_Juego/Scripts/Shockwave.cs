using UnityEngine;

namespace Dolores1810
{
    /// <summary>Onda de choque rasante que lanza el jefe. Se esquiva saltando.</summary>
    public class Shockwave : MonoBehaviour
    {
        float dir, speed, life = 1.8f;
        int hearts;
        int playerMask;

        public static void Spawn(Vector3 pos, float direction, float speed, int hearts, Sprite sprite)
        {
            var go = new GameObject("Onda");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 30;
            sr.color = new Color(1f, 0.7f, 0.3f, 0.9f);
            if (Fx.UnlitMat != null) sr.sharedMaterial = Fx.UnlitMat;
            go.transform.localScale = new Vector3(1.6f * (direction >= 0f ? 1f : -1f), 1.0f, 1f);
            var w = go.AddComponent<Shockwave>();
            w.dir = direction; w.speed = speed; w.hearts = hearts;
            w.playerMask = LayerMask.GetMask("Player");
        }

        void Update()
        {
            var gs = GameSession.I;
            if (gs != null && (gs.State == GameState.Paused || gs.State == GameState.Shop)) return;
            life -= Time.deltaTime;
            transform.position += new Vector3(dir * speed * Time.deltaTime, 0f, 0f);
            if (Random.value < 0.4f) Fx.DustPuff(transform.position, 1);
            Collider2D c = Physics2D.OverlapBox((Vector2)transform.position + new Vector2(0f, 0.3f), new Vector2(1.0f, 0.6f), 0f, playerMask);
            if (c != null)
            {
                var p = c.GetComponentInParent<PlatformerPlayer>();
                if (p != null && p.TakeDamage(hearts, transform.position)) { Destroy(gameObject); return; }
            }
            if (life <= 0f) Destroy(gameObject);
        }
    }
}
