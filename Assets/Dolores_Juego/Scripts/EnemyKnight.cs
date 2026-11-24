using System.Collections;
using UnityEngine;

namespace Dolores1810
{
    /// <summary>
    /// Soldado con armadura. Toda su dificultad sale de un EnemyConfig (golpes para derrotarlo, corazones de daño,
    /// velocidades, alcance, recompensas). Patrulla, persigue, ataca con aviso, se estremece y muere con animación.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class EnemyKnight : MonoBehaviour, IDamageable
    {
        enum EState { Patrol, Chase, Attack, Hurt, Dead }

        public EnemyConfig config;
        public float patrolRadius = 3f;

        [Header("Visual")]
        public SpriteRenderer visual;
        public FrameAnimator animator;
        public SpriteClip idle, walk, run, attack, hurt, die;
        public Transform hpBar;
        public SpriteRenderer hpFill;
        public float hpBarWidth = 1.2f;
        public int hitStartFrame = 5;
        public int hitEndFrame = 7;

        Rigidbody2D rb;
        BoxCollider2D col;
        int groundMask, playerMask;
        EState state = EState.Patrol;
        int hp, maxHp;
        int facing = -1;
        float homeX;
        int patrolDir = -1;
        float patrolTimer;
        bool waiting;
        float cooldown, hurtTimer, flashTimer, desiredVelX;
        bool didHit, alerted, counted;
        Transform camT;
        Color baseColor = Color.white;
        Vector3 startPos;

        public bool IsDead { get { return state == EState.Dead; } }

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
            homeX = transform.position.x;
            InitConfig();
            if (hpBar != null) hpBar.gameObject.SetActive(false);
        }

        public void InitConfig()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (col == null) col = GetComponent<BoxCollider2D>();
            if (animator != null && animator.target == null) animator.target = visual;
            if (config != null)
            {
                maxHp = config.MaxHealth;
                baseColor = config.tint;
                if (visual != null) visual.transform.localScale = Vector3.one * config.scale;
                if (col != null)
                {
                    col.size = new Vector2(config.colliderWidth * config.scale, config.colliderHeight * config.scale);
                    col.offset = new Vector2(0f, config.colliderHeight * config.scale * 0.5f);
                }
            }
            else if (maxHp <= 0) maxHp = 20;
            hp = maxHp;
            if (visual != null) visual.color = baseColor;
        }

        void Start()
        {
            InitConfig();
            startPos = transform.position;
            homeX = startPos.x;
            patrolTimer = Random.Range(1f, 3f);
            cooldown = Random.Range(0.3f, 1f);
            SetFacing(-1);
            animator.Play(idle, true);
            if (GameSession.I != null)
            {
                if (GameSession.I.cam != null) camT = GameSession.I.cam.transform;
                GameSession.I.Respawned -= ResetFullHealth;
                GameSession.I.Respawned += ResetFullHealth;
            }
        }

        void OnDestroy()
        {
            if (GameSession.I != null) GameSession.I.Respawned -= ResetFullHealth;
            if (counted) { counted = false; CombatTracker.Remove(); }
        }

        public void ResetFullHealth()
        {
            if (state == EState.Dead) return;
            hp = maxHp;
            state = EState.Patrol;
            desiredVelX = 0f;
            hurtTimer = 0f;
            flashTimer = 0f;
            didHit = false;
            alerted = false;
            waiting = true;
            patrolTimer = 1.0f;
            LeaveCombat();
            if (startPos.sqrMagnitude > 0.0001f)
            {
                transform.position = startPos;
                homeX = startPos.x;
                if (rb != null)
                {
                    rb.position = startPos;
                    rb.SetVel(Vector2.zero);
                }
            }
            if (visual != null) visual.color = baseColor;
            if (animator != null && idle != null) animator.Play(idle, true);
            if (hpFill != null)
            {
                hpFill.transform.localScale = new Vector3(hpBarWidth, hpFill.transform.localScale.y, 1f);
            }
        }

        void SetFacing(int dir)
        {
            facing = dir;
            if (visual != null) visual.flipX = dir < 0;
        }

        void EnterCombat()
        {
            if (!counted) { counted = true; CombatTracker.Add(); }
        }

        void LeaveCombat()
        {
            if (counted) { counted = false; CombatTracker.Remove(); }
        }

        bool EdgeAhead(int dir)
        {
            Vector2 o = (Vector2)transform.position + new Vector2(dir * 0.75f, 0.4f);
            return Physics2D.Raycast(o, Vector2.down, 1.5f, groundMask).collider == null;
        }

        bool WallAhead(int dir)
        {
            Vector2 o = (Vector2)transform.position + new Vector2(0f, 0.8f);
            return Physics2D.Raycast(o, new Vector2(dir, 0f), 0.8f, groundMask).collider != null;
        }

        void Update()
        {
            if (state == EState.Dead) return;
            var gs = GameSession.I;
            if (gs != null && (gs.State == GameState.Paused || gs.State == GameState.Shop)) return;

            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                if (visual != null) visual.color = flashTimer > 0f ? new Color(1f, 0.55f, 0.55f, 1f) : baseColor;
            }

            if (camT != null && Mathf.Abs(transform.position.x - camT.position.x) > 38f)
            {
                desiredVelX = 0f;
                animator.Play(idle);
                return;
            }

            PlatformerPlayer pl = gs != null ? gs.player : null;
            bool playerDead = (pl != null && pl.IsDead) || (gs != null && (gs.State == GameState.PlayerDead || gs.State == GameState.GameOver));
            if (playerDead)
            {
                if (hp < maxHp || state != EState.Patrol)
                {
                    ResetFullHealth();
                }
                return;
            }
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
                case EState.Patrol: TickPatrol(hasTarget, adx, dy); break;
                case EState.Chase: TickChase(hasTarget, dx, adx, dy); break;
                case EState.Attack: TickAttack(); break;
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
            float pSpeed = config != null ? config.patrolSpeed : 1.5f;
            float aggro = config != null ? config.aggroRange : 9f;
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
                desiredVelX = patrolDir * pSpeed;
                SetFacing(patrolDir);
                animator.Play(walk);
                if (patrolTimer <= 0f)
                {
                    waiting = true;
                    patrolTimer = Random.Range(0.8f, 1.8f);
                }
            }

            if (hasTarget && adx < aggro && Mathf.Abs(dy) < 3.2f)
            {
                state = EState.Chase;
                EnterCombat();
                if (!alerted)
                {
                    alerted = true;
                    AudioHub.Play(SfxId.Alert, 0.1f, 0.7f);
                }
            }
        }

        void TickChase(bool hasTarget, float dx, float adx, float dy)
        {
            float lose = config != null ? config.loseRange : 15f;
            float range = config != null ? config.attackRange : 2.5f;
            float chase = config != null ? config.chaseSpeed : 3f;
            if (!hasTarget || adx > lose)
            {
                state = EState.Patrol;
                homeX = transform.position.x;
                alerted = false;
                waiting = true;
                patrolTimer = 1f;
                LeaveCombat();
                return;
            }
            SetFacing(dx > 0f ? 1 : -1);
            if (adx <= range && Mathf.Abs(dy) < 1.9f)
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
                    desiredVelX = facing * chase;
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
            // aviso visual antes del golpe: parpadeo amarillo durante la preparación
            if (!didHit && f >= 2 && f < hitStartFrame && visual != null && flashTimer <= 0f)
                visual.color = ((int)(Time.time * 18f)) % 2 == 0 ? new Color(1f, 0.95f, 0.6f, 1f) : baseColor;
            else if (flashTimer <= 0f && visual != null) visual.color = baseColor;
            if (!didHit && f >= hitStartFrame && f <= hitEndFrame) DoHit();
            if (animator.Finished)
            {
                state = EState.Chase;
                cooldown = config != null ? config.attackCooldown : 1.5f;
            }
        }

        void DoHit()
        {
            float sc = config != null ? config.scale : 1f;
            Vector2 off = config != null ? config.hitBoxOffset : new Vector2(1.7f, 0.9f);
            Vector2 size = config != null ? config.hitBoxSize : new Vector2(2.7f, 1.3f);
            Vector2 center = (Vector2)transform.position + new Vector2(off.x * sc * facing, off.y * sc);
            Collider2D c = Physics2D.OverlapBox(center, size * sc, 0f, playerMask);
            if (c == null) return;
            PlatformerPlayer p = c.GetComponentInParent<PlatformerPlayer>();
            if (p == null) return;
            int hearts = config != null ? config.heartsDamage : 1;
            if (p.TakeDamage(hearts, transform.position)) didHit = true;
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
            hp -= Mathf.Max(1, amount);
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
            float stagger = config != null ? config.staggerChance : 1f;
            if (Random.value <= stagger)
            {
                state = EState.Hurt;
                hurtTimer = 0.42f;
                animator.Play(hurt, true);
                rb.SetVel(new Vector2(dir * 5.5f, 2.5f));
            }
            else
            {
                rb.SetVel(new Vector2(dir * 1.8f, 0f));
            }
            if (state == EState.Patrol) { state = EState.Chase; EnterCombat(); }
            return true;
        }

        void ShowBar()
        {
            if (hpBar == null) return;
            hpBar.gameObject.SetActive(true);
            if (hpFill != null)
            {
                float k = Mathf.Clamp01((float)hp / Mathf.Max(1, maxHp));
                hpFill.transform.localScale = new Vector3(hpBarWidth * k, hpFill.transform.localScale.y, 1f);
                hpFill.color = Color.Lerp(new Color(0.9f, 0.15f, 0.1f), new Color(0.35f, 0.9f, 0.2f), k);
            }
        }

        void Die()
        {
            state = EState.Dead;
            if (GameSession.I != null) GameSession.I.Respawned -= ResetFullHealth;
            LeaveCombat();
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
                int score = config != null ? config.scoreValue : 50;
                gs.RegisterKill(score);
                Vector3 p = transform.position + new Vector3(0f, 1.0f, 0f);
                int coins = config != null ? config.coinReward : 2;
                if (coins > 0) gs.DropCoins(p, coins);
                float ap = config != null ? config.appleChance : 0.1f;
                if (Random.value < ap) gs.SpawnPickup(Pickup.Kind.Apple, p, new Vector2(Random.Range(-2f, 2f), 6f));
                if (config != null && config.dropsHeart) gs.SpawnPickup(Pickup.Kind.Heart, p, new Vector2(0f, 7f));
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
                if (visual != null) visual.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - f / 0.7f);
                yield return null;
            }
            Destroy(gameObject);
        }

        void OnDrawGizmosSelected()
        {
            if (config == null) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube((Vector2)transform.position + new Vector2(config.hitBoxOffset.x * facing, config.hitBoxOffset.y), config.hitBoxSize);
        }
    }
}
