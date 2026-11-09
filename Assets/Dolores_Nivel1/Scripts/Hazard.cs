using UnityEngine;

namespace Dolores
{
    /// <summary>Pinchos: dañan al jugador al tocarlos.</summary>
    public class Hazard : MonoBehaviour
    {
        public int damage = 1;
        public float cooldown = 0.8f;
        float next;

        void OnTriggerStay2D(Collider2D other)
        {
            if (Time.time < next) return;
            PlatformerPlayer p = other.GetComponentInParent<PlatformerPlayer>();
            if (p == null) return;
            if (p.TakeDamage(damage, transform.position + Vector3.down * 0.4f))
            {
                next = Time.time + cooldown;
                AudioHub.Play(SfxId.Spike, 0.05f);
            }
        }
    }
}
