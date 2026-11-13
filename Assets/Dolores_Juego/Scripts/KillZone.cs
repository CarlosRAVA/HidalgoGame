using UnityEngine;

namespace Dolores1810
{
    /// <summary>Agua del río: si el jugador cae, vuelve al último punto de control.</summary>
    public class KillZone : MonoBehaviour
    {
        void OnTriggerEnter2D(Collider2D other)
        {
            var p = other.GetComponentInParent<PlatformerPlayer>();
            if (p == null || p.IsDead) return;
            Fx.Splash(p.transform.position);
            AudioHub.Play(SfxId.Splash, 0.05f);
            p.Kill();
        }
    }
}
