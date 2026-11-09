using UnityEngine;

namespace Dolores
{
    /// <summary>Coleccionables: moneda, manzana (cura), corazón (vida extra), diamante, estrella y cofre.</summary>
    public class Pickup : MonoBehaviour
    {
        public enum Kind { Coin, Apple, Heart, Diamond, Star, Chest }

        public Kind kind;
        public Sprite[] coinSprites;
        public Sprite staticSprite;
        public Sprite openSprite;
        public float coinFps = 9f;
        public float bobAmplitude = 0.1f;
        public float bobSpeed = 3f;
        public bool dropped;
        public Vector2 launchVelocity;
        public int bonusCoins = 10;

        SpriteRenderer sr;
        Vector3 basePos;
        float t;
        bool collected;
        bool initialized;
        bool launching;
        bool opened;
        Vector2 vel;
        float launchTime;
        int groundMask;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        void Start()
        {
            if (!initialized) Init();
        }

        public void Init()
        {
            if (initialized) return;
            initialized = true;
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            groundMask = LayerMask.GetMask("Ground");
            t = Random.value * 6f;
            if (kind == Kind.Coin && coinSprites != null && coinSprites.Length > 0) sr.sprite = coinSprites[0];
            else if (staticSprite != null) sr.sprite = staticSprite;
            basePos = transform.position;
            launching = dropped;
            vel = launchVelocity;
            if (kind == Kind.Chest) bobAmplitude = 0f;
        }

        void Update()
        {
            if (collected || !initialized) return;
            float dt = Time.deltaTime;
            t += dt;

            if (kind == Kind.Coin && coinSprites != null && coinSprites.Length > 0)
            {
                sr.sprite = coinSprites[((int)(t * coinFps)) % coinSprites.Length];
            }

            if (launching)
            {
                launchTime += dt;
                vel.y -= 24f * dt;
                transform.position += (Vector3)(vel * dt);
                if (vel.y < 0f)
                {
                    RaycastHit2D h = Physics2D.Raycast(transform.position, Vector2.down, 0.55f, groundMask);
                    if (h.collider != null || launchTime > 1.6f)
                    {
                        launching = false;
                        float y = h.collider != null ? h.point.y + 0.5f : transform.position.y;
                        basePos = new Vector3(transform.position.x, y, transform.position.z);
                    }
                }
                return;
            }

            if (kind == Kind.Coin || kind == Kind.Heart || kind == Kind.Apple)
            {
                var gs = GameSession.I;
                if (gs != null && gs.player != null && !gs.player.IsDead && gs.State == GameState.Playing)
                {
                    float range = kind == Kind.Coin ? 2.4f : 1.2f;
                    Vector2 pc = gs.player.Center;
                    if (Vector2.Distance(pc, basePos) < range)
                    {
                        basePos = Vector3.MoveTowards(basePos, new Vector3(pc.x, pc.y, basePos.z), 13f * dt);
                    }
                }
            }
            transform.position = basePos + new Vector3(0f, Mathf.Sin(t * bobSpeed) * bobAmplitude, 0f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || !initialized) return;
            PlatformerPlayer p = other.GetComponentInParent<PlatformerPlayer>();
            if (p == null || p.IsDead) return;
            Collect(p);
        }

        void Collect(PlatformerPlayer p)
        {
            var gs = GameSession.I;
            switch (kind)
            {
                case Kind.Coin:
                    if (gs != null) gs.AddCoin();
                    AudioHub.Play(SfxId.Coin, 0.04f, 0.7f);
                    break;
                case Kind.Apple:
                    p.Heal(1);
                    if (gs != null) gs.AddScore(25);
                    AudioHub.Play(SfxId.Heal, 0.02f);
                    break;
                case Kind.Heart:
                    p.Heal(2);
                    if (gs != null)
                    {
                        gs.AddLife();
                        gs.AddScore(100);
                        gs.ShowToast("¡Vida extra!");
                    }
                    AudioHub.Play(SfxId.Life, 0f);
                    break;
                case Kind.Diamond:
                    if (gs != null) gs.AddScore(150);
                    AudioHub.Play(SfxId.Gem, 0f);
                    break;
                case Kind.Star:
                    if (gs != null)
                    {
                        gs.AddScore(300);
                        gs.ShowToast("¡Estrella secreta! +300");
                    }
                    AudioHub.Play(SfxId.Gem, 0f);
                    break;
                case Kind.Chest:
                    if (opened) return;
                    opened = true;
                    if (openSprite != null) sr.sprite = openSprite;
                    AudioHub.Play(SfxId.Gem, 0f);
                    if (gs != null)
                    {
                        gs.AddScore(200);
                        gs.DropCoins(transform.position + Vector3.up * 0.8f, bonusCoins);
                        gs.ShowToast("¡Cofre del tesoro!");
                    }
                    Fx.Pickup(transform.position + Vector3.up * 0.6f);
                    return;
            }
            collected = true;
            Fx.Pickup(transform.position);
            Destroy(gameObject);
        }
    }
}
