using UnityEngine;

namespace Dolores1810
{
    /// <summary>Al entrar al área del jefe se cierran las rejas y comienza el combate.</summary>
    public class BossArenaTrigger : MonoBehaviour
    {
        public BossKnight boss;

        void TryActivate(Collider2D other)
        {
            if (boss == null || boss.Active || boss.IsDead) return;
            PlatformerPlayer p = other.GetComponentInParent<PlatformerPlayer>();
            if (p == null || p.IsDead) return;
            boss.Activate();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            TryActivate(other);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            TryActivate(other);
        }
    }
}
