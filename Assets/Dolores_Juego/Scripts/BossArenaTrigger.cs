using UnityEngine;

namespace Dolores1810
{
    /// <summary>Al entrar al área del jefe se cierran las rejas y comienza el combate.</summary>
    public class BossArenaTrigger : MonoBehaviour
    {
        public BossKnight boss;
        bool used;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (used || boss == null) return;
            PlatformerPlayer p = other.GetComponentInParent<PlatformerPlayer>();
            if (p == null || p.IsDead) return;
            used = true;
            boss.Activate();
        }
    }
}
