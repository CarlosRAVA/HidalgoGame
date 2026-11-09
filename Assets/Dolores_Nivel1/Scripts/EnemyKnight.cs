using System.Collections;
using UnityEngine;

namespace Dolores
{
    /// <summary>
    /// Soldado con armadura: patrulla, persigue al jugador, ataca cuerpo a cuerpo, se estremece al recibir
    /// golpes y muere con animación. Los tres caballeros usan este mismo script con valores distintos.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class EnemyKnight : MonoBehaviour, IDamageable
    {
        enum EState { Patrol, Chase, Attack, Hurt, Dead }

        [Header("Estadísticas")]
        public int maxHp = 2;
        public float patrolSpeed = 1.6f;
        public float chaseSpeed = 3.2f;
        public float aggroRange = 9f;
        public float loseRange = 15f;
        public float patrolRadius = 3f;
        public float attackStartRange = 2.5f;
        public int damage = 1;
        public float attackCooldown = 1.5f;
        public int hitStartFrame = 5;
        public int hitEndFrame = 7;
        public Vector2 attackBoxSize = new Vector2(2.6f, 1.3f);
        public Vector2 attackBoxOffset = new Vector2(1.7f, 0.9f);
        [Range(0f, 1f)] public float staggerChance = 1f;
        public int scoreValue = 50;
        public int coinDrops = 1;
        [Range(0f, 1f)] public float appleChance = 0.1f;
        public bool dropsHeart;

        [Header("Visual")]
        public SpriteRenderer visual;
        public FrameAnimator animator;
        public SpriteClip idle, walk, run, attack, hurt, die;
        public Transform hpBar;
        public SpriteRenderer hpFill;
        public float hpBarWidth = 1.2f;

        Rigidbody2D rb;
        BoxCollider2D col;
        int groundMask, playerMask;
        EState state = EState.Patrol;
        int hp;
        int facing = -1;
        float homeX;
        int patrolDir = -1;
        float patrolTimer;
        bool waiting;
        float cooldown;
        float hurtTimer;
        float flashTimer;
        float desiredVelX;
        bool didHit;
        Transform camT;
        bool alerted;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<BoxCollider2D>();
            rb.gravityScale = 3.2f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var mat = new PhysicsMaterial2D("EnemigoSinFriccion");
            mat.friction = 0f;
            mat.bounciness = 0f;
            col.sharedMaterial = mat;
            groundMask = LayerMask.GetMask("Ground");
            playerMask = LayerMask.GetMask("Player");
            hp = maxHp;
            homeX = transform.position.x;
            if (animator != null && animator.target == null) animator.target = visual;
            if (hpBar != null) hpBar.gameObject.SetActive(false);
        }

        void Start()
        {
            patrolTimer = Random.Range(1f, 3f);
            cooldown = Random.Range(0.3f, 1f);
            SetFacing(-1);
            animator.Play(idle, true);
            if (GameSession.I != null && GameSession.I.cam != null) camT = GameSession.I.cam.transform;
        }

        void SetFacing(int dir)
        {
            facing = dir;
            if (visual != null) visual.flipX = dir < 0;
        }

        bool EdgeAhead(int dir)
        {
            Vector2 o = (Vector2)transform.position + new Vector2(dir * 0.75f, 0.4f);
            RaycastHit2D h = Physics2D.Raycast(o, Vector2.down, 1.5f, groundMask);
            return h.collider == null;
        }

        bool WallAhead(int dir)
        {
            Vector2 o = (Vector2)transform.position + new Vector2(0f, 0.8f);
            RaycastHit2D h = Physics2D.Raycast(o, new Vector2(dir, 0f), 0.8f, groundMask);
            return h.collider != null;
        }

        void Update()
        {
            if (state == EState.Dead) return;
            var gs = GameSession.I;
            if (gs != null && gs.State == GameState.Paused) return;

            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                if (visual != null) visual.color = flashTimer > 0f ? new Color(1f, 0.55f, 0.55f, 1f) : Color.white;
            }

            if (camT != null && Mathf.Abs(transform.position.x - camT.position.x) > 38f)
            {
                desiredVelX = 0f;
                animator.Play(idle);
                return;
            }

            PlatformerPlayer pl = gs != null ? gs.player : null;
            bool hasTarget = pl != null && !pl.IsDead && gs.State == GameState.Playing;
            float dx = 0f, dy = 0f;
            if (pl != null)
            {
                dx = pl.transform.position.x - transform.position.x;
                dy = pl.transform.position.y - transform.position.y;
            }
            float adx = Mathf.Abs(dx);
            cooldown -= Time.deltaTime;

            switch (state)
            {
                case EState.Patrol:
                    TickPatrol(hasTarget, adx, dy);
                    break;
                case EState.Chase:
                    TickChase(hasTarget, dx, adx, dy);
                    break;
                case EState.Attack:
                    TickAttack();
                    break;
                case EState.Hurt:
                    hurtTimer -= Time.deltaTime;
                    desiredVelX = 0f;
                    if (hurtTimer <= 0f)
                    {
                        state = EState.Chase;
                        cooldown = Mathf.Min(cooldown, 0.5f);
                    }
                    break;
            }
        }

        void TickPatrol(bool hasTarget, float adx, float dy)
        {
            patrolTimer -= Time.deltaTime;
            if (waiting)
            {
                desiredVelX = 0f;
                animator.Play(idle);
                if (patrolTimer <= 0f)
                {
                    waiting = false;
                    patrolTimer = Random.Range(2f, 3.5f);
                    float off0 = transform.position.x - homeX;
                    patrolDir = off0 > 0f ? -1 : 1;
                    if (Mathf.Abs(off0) < 0.5f) patrolDir = Random.value < 0.5f ? -1 : 1;
                }
            }
            else
            {
                float off = transform.position.x - homeX;
                if (off > patrolRadius) patrolDir = -1;
                else if (off < -patrolRadius) patrolDir = 1;
                if (EdgeAhead(patrolDir) || WallAhead(patrolDir))
                {
                    patrolDir = -patrolDir;
                    waiting = true;
                    patrolTimer = Random.Range(0.6f, 1.4f);
                }
                desiredVelX = patrolDir * patrolSpeed;
                SetFacing(patrolDir);
                animator.Play(walk);
                if (patrolTimer <= 0f)
                {
                    waiting = true;
                    patrolTimer = Random.Range(0.8f, 1.8f);
                }
            }

            if (hasTarget && adx < aggroRange && Mathf.Abs(dy) < 3.2f)
            {
                state = EState.Chase;
                if (!alerted)
                {
                    alerted = true;
                    AudioHub.Play(SfxId.Alert, 0.1f, 0.7f);
                }
            }
        }

        void TickChase(bool hasTarget, float dx, float adx, float dy)
        {
            if (!hasTarget || adx > loseRange)
            {
                state = EState.Patrol;
                homeX = transform.position.x;
                alerted = false;
                waiting = true;
                patrolTimer = 1f;
                return;
            }
            SetFacing(dx > 0f ? 1 : -1);
            if (adx <= attackStartRange && Mathf.Abs(dy) < 1.9f)
            {
                desiredVelX = 0f;
                if (cooldown <= 0f) StartAttack();
                else animator.Play(idle);
            }
            else
            {
                if (EdgeAhead(facing) || WallAhead(facing))
                {
                    desiredVelX = 0f;
                    animator.Play(idle);
                }
                else
                {
                    desiredVelX = facing * chaseSpeed;
                    animator.Play(run);
                }
            }
        }

        void StartAttack()
        {
            state = EState.Attack;
            didHit = false;
            desiredVelX = 0f;
            animator.Play(attack, true);
            AudioHub.Play(SfxId.EnemySwing, 0.06f, 0.8f);
        }

        void TickAttack()
        {
            desiredVelX = 0f;
            int f = animator.Frame;
            if (!didHit && f >= hitStartFrame && f <= hitEndFrame) DoHit();
            if (animator.Finished)
            {
                state = EState.Chase;
                cooldown = attackCooldown;
            }
        }

        void DoHit()
        {
            Vector2 center = (Vector2)transform.position + new Vector2(attackBoxOffset.x * facing, attackBoxOffset.y);
            Collider2D c = Physics2D.OverlapBox(center, attackBoxSize, 0f, playerMask);
            if (c == null) return;
            PlatformerPlayer p = c.GetComponentInParent<PlatformerPlayer>();
            if (p == null) return;
            if (p.TakeDamage(damage, transform.position)) didHit = true;
        }

        void FixedUpdate()
        {
            if (state == EState.Dead) return;
            var v = rb.GetVel();
            if (state == EState.Hurt) v.x = Mathf.MoveTowards(v.x, 0f, 18f * Time.fixedDeltaTime);
            else v.x = Mathf.MoveTowards(v.x, desiredVelX, 40f * Time.fixedDeltaTime);
            rb.SetVel(v);
        }

        public bool TakeDamage(int amount, Vector2 sourcePos)
        {
            if (state == EState.Dead) return false;
            hp -= amount;
            AudioHub.Play(SfxId.HitEnemy, 0.06f);
            Fx.Hit(transform.position + new Vector3(0f, 1.0f, 0f));
            flashTimer = 0.12f;
            if (visual != null) visual.color = new Color(1f, 0.55f, 0.55f, 1f);
            ShowBar();
            if (GameSession.I != null && GameSession.I.cam != null) GameSession.I.cam.Shake(0.06f, 0.1f);
            if (hp <= 0)
            {
                Die();
                return true;
            }
            float dir = transform.position.x >= sourcePos.x ? 1f : -1f;
            if (Random.value <= staggerChance)
            {
                state = EState.Hurt;
                hurtTimer = 0.42f;
                animator.Play(hurt, true);
                rb.SetVel(new Vector2(dir * 5.5f, 2.5f));
            }
            else
            {
                rb.SetVel(new Vector2(dir * 1.8f, 0f));
                if (state == EState.Patrol) state = EState.Chase;
            }
            if (state == EState.Patrol) state = EState.Chase;
            return true;
        }

        void ShowBar()
        {
            if (hpBar == null) return;
            hpBar.gameObject.SetActive(true);
            if (hpFill != null)
            {
                float k = Mathf.Clamp01((float)hp / maxHp);
                hpFill.transform.localScale = new Vector3(hpBarWidth * k, hpFill.transform.localScale.y, 1f);
                hpFill.color = Color.Lerp(new Color(0.9f, 0.15f, 0.1f), new Color(0.35f, 0.9f, 0.2f), k);
            }
        }

        void Die()
        {
            state = EState.Dead;
            desiredVelX = 0f;
            animator.Play(die, true);
            AudioHub.Play(SfxId.EnemyDie, 0.05f);
            rb.SetVel(Vector2.zero);
            col.enabled = false;
            rb.simulated = false;
            if (hpBar != null) hpBar.gameObject.SetActive(false);
            var gs = GameSession.I;
            if (gs != null)
            {
                gs.RegisterKill(scoreValue);
                Vector3 p = transform.position + new Vector3(0f, 1.0f, 0f);
                if (coinDrops > 0) gs.DropCoins(p, coinDrops);
                if (Random.value < appleChance) gs.SpawnPickup(Pickup.Kind.Apple, p, new Vector2(Random.Range(-2f, 2f), 6f));
                if (dropsHeart) gs.SpawnPickup(Pickup.Kind.Heart, p, new Vector2(0f, 7f));
                if (gs.cam != null) gs.cam.Shake(0.12f, 0.2f);
            }
            StartCoroutine(FadeAndRemove());
        }

        IEnumerator FadeAndRemove()
        {
            float t = 0f;
            while (!animator.Finished && t < 3f)
            {
                t += Time.deltaTime;
                yield return null;
            }
            yield return new WaitForSeconds(1.2f);
            float f = 0f;
            while (f < 0.7f)
            {
                f += Time.deltaTime;
                if (visual != null) visual.color = new Color(1f, 1f, 1f, 1f - f / 0.7f);
                yield return null;
            }
            Destroy(gameObject);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube((Vector2)transform.position + new Vector2(attackBoxOffset.x * facing, attackBoxOffset.y), attackBoxSize);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, aggroRange);
        }
    }
}
