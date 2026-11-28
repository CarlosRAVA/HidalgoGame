using UnityEngine;

namespace Dolores1810
{
    /// <summary>Palanca que abre una reja (zonas secretas).</summary>
    public class GateLever : MonoBehaviour, IInteractable
    {
        public Gate gate;
        public Sprite offSprite, onSprite;
        SpriteRenderer sr;
        bool used;

        public string Prompt { get { return "Presiona E para accionar la palanca"; } }
        public bool Available { get { return !used; } }
        public bool HintOnly { get { return false; } }

        void Awake() { sr = GetComponent<SpriteRenderer>(); }

        public void Interact(PlatformerPlayer player)
        {
            if (used) return;
            used = true;
            if (sr != null && onSprite != null) sr.sprite = onSprite;
            if (gate != null) gate.Open();
            Fx.Sparkle(transform.position + Vector3.up * 0.5f, new Color(0.6f, 1f, 0.6f, 1f));
            if (GameSession.I != null) GameSession.I.ShowToast("Se oyó algo abrirse...");
        }
    }
}
