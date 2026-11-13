using UnityEngine;

namespace Dolores1810
{
    /// <summary>Barriles y cajas que se rompen a golpes y sueltan monedas. Muestran una pista al acercarse.</summary>
    public class Breakable : MonoBehaviour, IDamageable, IInteractable
    {
        public int hits = 1;
        public int coinValue = 4;
        [Range(0f, 1f)] public float appleChance = 0.1f;

        int hp;
        float shake;
        Vector3 basePos;

        public string Prompt { get { return "Ataca (J) para romper"; } }
        public bool Available { get { return hp > 0; } }
        public bool HintOnly { get { return true; } }
        public void Interact(PlatformerPlayer player) { }

        void Awake()
        {
            basePos = transform.position;
            hp = Mathf.Max(1, hits) * GameRules.BaseWeaponDamage;
        }

        public bool TakeDamage(int amount, Vector2 sourcePos)
        {
            if (hp <= 0) return false;
            hp -= Mathf.Max(1, amount);
            shake = 0.2f;
            AudioHub.Play(SfxId.HitEnemy, 0.1f, 0.55f);
            Fx.Hit(transform.position + Vector3.up * 0.5f);
            if (hp <= 0) Break();
            return true;
        }

        void Break()
        {
            AudioHub.Play(SfxId.Break, 0.08f);
            Fx.Poof(transform.position);
            var gs = GameSession.I;
            if (gs != null)
            {
                Vector3 p = transform.position + Vector3.up * 0.6f;
                gs.DropCoinsValue(p, coinValue, Mathf.Clamp(coinValue, 1, 4));
                if (Random.value < appleChance) gs.SpawnPickup(Pickup.Kind.Apple, p, new Vector2(0f, 6f));
                gs.AddScore(5);
                gs.NotifyBreak();
            }
            Destroy(gameObject);
        }

        void Update()
        {
            if (shake > 0f)
            {
                shake -= Time.deltaTime;
                transform.position = basePos + new Vector3(Random.Range(-1f, 1f) * 0.07f, 0f, 0f);
                if (shake <= 0f) transform.position = basePos;
            }
        }
    }
}
