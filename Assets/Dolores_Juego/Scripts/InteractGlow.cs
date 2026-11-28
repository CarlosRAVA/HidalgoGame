using UnityEngine;

namespace Dolores1810
{
    /// <summary>
    /// Brillo discreto para objetos con los que se puede interactuar: un halo que respira suavemente y que se
    /// intensifica cuando el jugador se acerca.
    /// </summary>
    public class InteractGlow : MonoBehaviour
    {
        public Color glowColor = new Color(1f, 0.85f, 0.4f, 1f);
        public float nearDistance = 3.2f;
        public Sprite haloSprite;

        SpriteRenderer rim, halo;
        SpriteRenderer src;
        float phase;

        void Start()
        {
            src = GetComponent<SpriteRenderer>();
            if (src == null) { enabled = false; return; }
            phase = Random.value * 6f;
            var rimGO = new GameObject("BrilloBorde");
            rimGO.transform.SetParent(transform, false);
            rimGO.transform.localScale = new Vector3(1.09f, 1.07f, 1f);
            rim = rimGO.AddComponent<SpriteRenderer>();
            rim.sortingOrder = src.sortingOrder - 1;
            if (Fx.UnlitMat != null) rim.sharedMaterial = Fx.UnlitMat;
            if (haloSprite != null)
            {
                var hGO = new GameObject("BrilloHalo");
                hGO.transform.SetParent(transform, false);
                Bounds b = src.sprite != null ? src.sprite.bounds : new Bounds(Vector3.zero, Vector3.one);
                hGO.transform.localPosition = new Vector3(b.center.x, b.center.y, 0f);
                float s = Mathf.Max(b.size.x, b.size.y) * 1.7f;
                hGO.transform.localScale = new Vector3(s, s, 1f);
                halo = hGO.AddComponent<SpriteRenderer>();
                halo.sprite = haloSprite;
                halo.sortingOrder = src.sortingOrder - 2;
                if (Fx.UnlitMat != null) halo.sharedMaterial = Fx.UnlitMat;
            }
        }

        void LateUpdate()
        {
            if (src == null || rim == null) return;
            rim.sprite = src.sprite;
            rim.flipX = src.flipX;
            float near = 0f;
            var gs = GameSession.I;
            if (gs != null && gs.player != null && !gs.player.IsDead)
            {
                float d = Vector2.Distance(gs.player.Center, transform.position);
                near = Mathf.Clamp01(1f - d / nearDistance);
            }
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 2.2f + phase);
            float a = Mathf.Lerp(0.10f, 0.22f, pulse) + near * 0.30f;
            var c = glowColor; c.a = a;
            rim.color = c;
            if (halo != null)
            {
                var h = glowColor; h.a = Mathf.Lerp(0.05f, 0.12f, pulse) + near * 0.22f;
                halo.color = h;
            }
        }
    }
}
