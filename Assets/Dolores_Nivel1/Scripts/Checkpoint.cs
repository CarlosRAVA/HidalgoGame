using UnityEngine;

namespace Dolores
{
    /// <summary>Banderines de punto de control: al tocarlos se guarda el progreso y se curan 2 corazones.</summary>
    public class Checkpoint : MonoBehaviour
    {
        public int index;
        public SpriteRenderer flag;
        public SpriteRenderer glow;
        public Transform spawn;
        public Color inactiveColor = new Color(0.55f, 0.55f, 0.6f, 1f);
        public int healOnActivate = 2;

        bool active;
        float glowT;

        void Start()
        {
            if (flag != null) flag.color = inactiveColor;
            if (glow != null)
            {
                var c = glow.color; c.a = 0f; glow.color = c;
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (active) return;
            PlatformerPlayer p = other.GetComponentInParent<PlatformerPlayer>();
            if (p == null || p.IsDead) return;
            active = true;
            if (GameSession.I != null) GameSession.I.SetCheckpoint(spawn != null ? spawn.position : transform.position, index);
            AudioHub.Play(SfxId.Checkpoint, 0f);
            p.Heal(healOnActivate);
            Fx.Pickup(transform.position + Vector3.up * 1.2f);
        }

        void Update()
        {
            if (!active) return;
            glowT = Mathf.Min(1f, glowT + Time.deltaTime * 2f);
            if (flag != null) flag.color = Color.Lerp(inactiveColor, Color.white, glowT);
            if (glow != null)
            {
                var c = glow.color;
                c.a = glowT * (0.5f + 0.12f * Mathf.Sin(Time.time * 4f));
                glow.color = c;
            }
        }
    }
}
