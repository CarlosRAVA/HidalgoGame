using UnityEngine;

namespace Dolores1810
{
    /// <summary>Pergamino histórico escondido: se cuenta una sola vez en el guardado y muestra un dato de la época.</summary>
    public class Collectible : MonoBehaviour
    {
        public string collectibleId = "p";
        [TextArea] public string fact = "";
        public int coins = 20;

        float t, baseY;
        bool taken;

        void Start()
        {
            baseY = transform.position.y;
            t = Random.value * 6f;
            if (PlayerProgress.IsCollected(collectibleId)) taken = true;
            if (taken) gameObject.SetActive(false);
        }

        void Update()
        {
            t += Time.deltaTime;
            var p = transform.position;
            p.y = baseY + Mathf.Sin(t * 2.4f) * 0.12f;
            transform.position = p;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (taken) return;
            var p = other.GetComponentInParent<PlatformerPlayer>();
            if (p == null || p.IsDead) return;
            taken = true;
            PlayerProgress.MarkCollected(collectibleId);
            AudioHub.Play(SfxId.Secret, 0f);
            Fx.Sparkle(transform.position, new Color(1f, 0.95f, 0.6f, 1f));
            var gs = GameSession.I;
            if (gs != null)
            {
                gs.AddCoins(coins);
                gs.AddScore(200);
                gs.NotifyCollectible();
                gs.ShowToast(fact, 6f);
            }
            PlayerProgress.SaveNow();
            Destroy(gameObject);
        }
    }
}
