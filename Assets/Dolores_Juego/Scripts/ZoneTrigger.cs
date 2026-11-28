using UnityEngine;

namespace Dolores1810
{
    /// <summary>Muestra el nombre de la zona y actualiza el objetivo actual al entrar (una sola vez).</summary>
    public class ZoneTrigger : MonoBehaviour
    {
        public string title;
        public string subtitle;
        public string objective;
        bool shown;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (shown) return;
            PlatformerPlayer p = other.GetComponentInParent<PlatformerPlayer>();
            if (p == null) return;
            shown = true;
            var gs = GameSession.I;
            if (gs == null) return;
            if (!string.IsNullOrEmpty(title)) gs.ShowZone(title, subtitle);
            if (!string.IsNullOrEmpty(objective)) gs.SetObjective(objective);
        }
    }
}
