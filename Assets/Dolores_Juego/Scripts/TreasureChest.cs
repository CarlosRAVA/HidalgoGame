using UnityEngine;

namespace Dolores1810
{
    /// <summary>Cofre que se abre con E. Cada cofre se recuerda en el guardado: su recompensa se da una sola vez.</summary>
    public class TreasureChest : MonoBehaviour, IInteractable
    {
        public string chestId = "c";
        public int coins = 25;
        public bool hidden;
        public Sprite closedSprite;
        public Sprite openSprite;

        SpriteRenderer sr;
        bool opened;

        public string Prompt { get { return "Presiona E para abrir"; } }
        public bool Available { get { return !opened; } }
        public bool HintOnly { get { return false; } }

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            opened = false;
            if (sr != null && closedSprite != null) sr.sprite = closedSprite;
        }

        void Start()
        {
            opened = false;
            if (sr != null && closedSprite != null) sr.sprite = closedSprite;
        }

        public void Interact(PlatformerPlayer player)
        {
            if (opened) return;
            opened = true;
            if (openSprite != null) sr.sprite = openSprite;
            AudioHub.Play(SfxId.ChestOpen, 0.02f);
            Fx.Sparkle(transform.position + Vector3.up * 0.6f, new Color(1f, 0.9f, 0.4f, 1f));
            Fx.Pickup(transform.position + Vector3.up * 0.8f);
            var gs = GameSession.I;
            string saveKey = (gs != null ? ("L" + gs.levelIndex + "_") : "") + chestId + "_" + Mathf.RoundToInt(transform.position.x);
            PlayerProgress.MarkChestOpened(saveKey);
            if (gs != null)
            {
                gs.DropCoinsValue(transform.position + Vector3.up * 0.8f, coins, Mathf.Clamp(coins / 3, 3, 10));
                gs.AddScore(100);
                gs.NotifyChest(hidden);
                gs.ShowToast(hidden ? "¡Cofre escondido! +" + coins + " monedas" : "¡Cofre abierto! +" + coins + " monedas");
                if (hidden) gs.NotifySecret();
                if (gs.cam != null) gs.cam.Shake(0.07f, 0.12f);
            }
            PlayerProgress.SaveNow();
        }
    }
}
