using UnityEngine;

namespace Dolores
{
    /// <summary>Barriles y cajas que se rompen a golpes y sueltan monedas.</summary>
    public class Breakable : MonoBehaviour, IDamageable
    {
        public int hp = 1;
        public int coins = 2;
        [Range(0f, 1f)] public float appleChance = 0.1f;

        float shake;
        Vector3 basePos;

        void Awake()
        {
            basePos = transform.position;
        }

        public bool TakeDamage(int amount, Vector2 sourcePos)
        {
            if (hp <= 0) return false;
            hp -= amount;
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
                gs.DropCoins(p, coins);
                if (Random.value < appleChance) gs.SpawnPickup(Pickup.Kind.Apple, p, new Vector2(0f, 6f));
                gs.AddScore(5);
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
