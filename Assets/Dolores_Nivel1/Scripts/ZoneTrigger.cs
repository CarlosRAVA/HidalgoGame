using UnityEngine;

namespace Dolores
{
    /// <summary>Muestra el nombre de la zona al entrar (una sola vez).</summary>
    public class ZoneTrigger : MonoBehaviour
    {
        public string title;
        public string subtitle;
        bool shown;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (shown) return;
            PlatformerPlayer p = other.GetComponentInParent<PlatformerPlayer>();
            if (p == null) return;
            shown = true;
            if (GameSession.I != null) GameSession.I.ShowZone(title, subtitle);
        }
    }
}
