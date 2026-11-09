using System.Collections.Generic;
using UnityEngine;

namespace Dolores
{
    /// <summary>
    /// Controlador del personaje (Body_A) para el nivel de plataformas: correr, saltar, atacar con hacha,
    /// recibir daño y morir. Usa Rigidbody2D + BoxCollider2D y animación por sprites.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class PlatformerPlayer : MonoBehaviour, IDamageable
    {
        public enum PState { Normal, Attacking, Hurt, Dead }

        [Header("Movimiento")]
        public float moveSpeed = 6.2f;
        public float groundAccel = 70f;
        public float airAccel = 45f;
        public float jumpVelocity = 13.6f;
        public float gravityScale = 3.6f;
        public float fallGravityMul = 1.4f;
        public float jumpCutMul = 2.4f;
        public float maxFallSpeed = 24f;
        public float coyoteTime = 0.1f;
        public float jumpBuffer = 0.12f;

        [Header("Combate")]
        public int maxHealth = 5;
        public int attackDamage = 1;
        public float attackCooldown = 0.42f;
        public int hitStartFrame = 3;
        public int hitEndFrame = 5;
        public Vector2 attackBoxSize = new Vector2(2.0f, 1.6f);
        public Vector2 attackBoxOffset = new Vector2(1.05f, 0.8f);
        public float invulnTime = 1.1f;
        public float killY = -9f;

        [Header("Visual")]
        public SpriteRenderer visual;
        public FrameAnimator animator;
        public SpriteClip idle, run, jump, fall, attack, hit, death;

        Rigidbody2D rb;
        BoxCollider2D col;
        int groundMask, enemyMask;
        PState state = PState.Normal;
        int health;
        float facing = 1f;
        float moveInput;
        bool grounded;
        bool jumpHeldFlag;
        float coyoteTimer, jumpBufferTimer, attackCd, hurtTimer, invulnTimer, stepTimer;
        float lastFallSpeed;
        float inputBlockedUntil;
        readonly HashSet<IDamageable> hitThisSwing = new HashSet<IDamageable>();

        public int Health { get { return health; } }
        public float Facing { get { return facing; } }
        public bool IsDead { get { return state == PState.Dead; } }
        public PState State { get { return state; } }
        public Vector2 Center { get { return (Vector2)transform.position + new Vector2(0f, 0.75f); } }

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<BoxCollider2D>();
            rb.gravityScale = gravityScale;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
            var mat = new PhysicsMaterial2D("PlayerSinFriccion");
            mat.friction = 0f;
            mat.bounciness = 0f;
            col.sharedMaterial = mat;
            groundMask = LayerMask.GetMask("Ground");
            enemyMask = LayerMask.GetMask("Enemy");
            health = maxHealth;
            if (animator != null && animator.target == null) animator.target = visual;
        }

        void Start()
        {
            if (animator != null) animator.Play(idle, true);
        }

        public void BlockInput(float seconds)
        {
            inputBlockedUntil = Time.unscaledTime + seconds;
        }

        void Update()
        {
            if (state == PState.Dead) return;

            if (transform.position.y < killY)
            {
                Die();
                return;
            }

            var gs = GameSession.I;
            bool canAct = (gs == null || gs.State == GameState.Playing) && Time.unscaledTime >= inputBlockedUntil;

            float h = 0f;
            bool jumpDown = false, jumpHeld = false, atkDown = false;
            if (canAct && state != PState.Hurt)
            {
                h = GameInput.Horizontal();
                jumpDown = GameInput.JumpDown();
                jumpHeld = GameInput.JumpHeld();
                atkDown = GameInput.AttackDown();
            }
            moveInput = h;
            jumpHeldFlag = jumpHeld;

            bool wasGrounded = grounded;
            grounded = CheckGround();
            float vy = rb.GetVel().y;
            if (grounded) coyoteTimer = coyoteTime; else coyoteTimer -= Time.deltaTime;
            if (jumpDown) jumpBufferTimer = jumpBuffer; else jumpBufferTimer -= Time.deltaTime;

            if (!wasGrounded && grounded && lastFallSpeed < -7f)
            {
                AudioHub.Play(SfxId.Land, 0.05f, 0.8f);
                Fx.DustPuff(transform.position);
            }
            if (!grounded) lastFallSpeed = vy; else lastFallSpeed = 0f;

            if (jumpBufferTimer > 0f && coyoteTimer > 0f && state != PState.Hurt)
            {
                DoJump();
            }

            if (state == PState.Normal && Mathf.Abs(h) > 0.1f) facing = h > 0f ? 1f : -1f;

            attackCd -= Time.deltaTime;
            if (atkDown && state == PState.Normal && attackCd <= 0f) StartAttack();
            if (state == PState.Attacking) TickAttack();

            if (state == PState.Hurt)
            {
                hurtTimer -= Time.deltaTime;
                if (hurtTimer <= 0f) state = PState.Normal;
            }
            if (invulnTimer > 0f) invulnTimer -= Time.deltaTime;

            UpdateAnimation();
            if (visual != null)
            {
                visual.flipX = facing < 0f;
                bool blink = invulnTimer > 0f && ((int)(Time.time * 14f)) % 2 == 0;
                visual.color = blink ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
            }
        }

        void FixedUpdate()
        {
            var vel = rb.GetVel();
            if (state == PState.Dead)
            {
                vel.x = Mathf.MoveTowards(vel.x, 0f, 30f * Time.fixedDeltaTime);
                rb.SetVel(vel);
                return;
            }

            if (state == PState.Hurt)
            {
                vel.x = Mathf.MoveTowards(vel.x, 0f, 14f * Time.fixedDeltaTime);
            }
            else
            {
                float speed = moveSpeed * (state == PState.Attacking ? 0.5f : 1f);
                float target = moveInput * speed;
                float acc = grounded ? groundAccel : airAccel;
                if (Mathf.Abs(moveInput) < 0.01f) acc *= 1.4f;
                vel.x = Mathf.MoveTowards(vel.x, target, acc * Time.fixedDeltaTime);
            }

            if (vel.y < -0.01f) rb.gravityScale = gravityScale * fallGravityMul;
            else if (vel.y > 0.01f && !jumpHeldFlag) rb.gravityScale = gravityScale * jumpCutMul;
            else rb.gravityScale = gravityScale;
            if (vel.y < -maxFallSpeed) vel.y = -maxFallSpeed;
            rb.SetVel(vel);
        }

        bool CheckGround()
        {
            if (rb.GetVel().y > 0.5f) return false;
            Bounds b = col.bounds;
            Vector2 c = new Vector2(b.center.x, b.min.y - 0.04f);
            Collider2D hitCol = Physics2D.OverlapBox(c, new Vector2(b.size.x * 0.9f, 0.1f), 0f, groundMask);
            return hitCol != null;
        }

        void DoJump()
        {
            var v = rb.GetVel();
            v.y = jumpVelocity;
            rb.SetVel(v);
            coyoteTimer = 0f;
            jumpBufferTimer = 0f;
            grounded = false;
            AudioHub.Play(SfxId.Jump, 0.06f, 0.8f);
            Fx.DustPuff(transform.position, 2);
        }

        void StartAttack()
        {
            state = PState.Attacking;
            attackCd = attackCooldown;
            hitThisSwing.Clear();
            animator.Play(attack, true);
            AudioHub.Play(SfxId.Swing, 0.08f);
        }

        void TickAttack()
        {
            int f = animator.Frame;
            if (animator.Current == attack && f >= hitStartFrame && f <= hitEndFrame) DoHitCheck();
            if (animator.Current == attack && animator.Finished) state = PState.Normal;
        }

        void DoHitCheck()
        {
            Vector2 center = (Vector2)transform.position + new Vector2(attackBoxOffset.x * facing, attackBoxOffset.y);
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, attackBoxSize, 0f, enemyMask);
            bool any = false;
            for (int i = 0; i < hits.Length; i++)
            {
                IDamageable d = hits[i].GetComponentInParent<IDamageable>();
                if (d == null || hitThisSwing.Contains(d)) continue;
                hitThisSwing.Add(d);
                if (d.TakeDamage(attackDamage, transform.position)) any = true;
            }
            if (any && GameSession.I != null && GameSession.I.cam != null) GameSession.I.cam.Shake(0.09f, 0.14f);
        }

        void UpdateAnimation()
        {
            if (state == PState.Dead || state == PState.Hurt || state == PState.Attacking) return;
            if (!grounded)
            {
                animator.Play(rb.GetVel().y > 0.1f ? jump : fall);
            }
            else if (Mathf.Abs(moveInput) > 0.1f)
            {
                animator.Play(run);
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f)
                {
                    AudioHub.Play(SfxId.Step, 0.12f, 0.55f);
                    stepTimer = 0.28f;
                }
            }
            else
            {
                animator.Play(idle);
            }
        }

        public bool TakeDamage(int amount, Vector2 sourcePos)
        {
            if (state == PState.Dead) return false;
            if (invulnTimer > 0f) return false;
            var gs = GameSession.I;
            if (gs != null && gs.State != GameState.Playing) return false;

            health = Mathf.Max(0, health - amount);
            if (gs != null)
            {
                gs.RaiseStats();
                gs.FlashDamage();
                if (gs.cam != null) gs.cam.Shake(0.22f, 0.25f);
            }
            AudioHub.Play(SfxId.PlayerHurt, 0.03f);
            Fx.Hit(transform.position + Vector3.up * 0.9f);
            if (health <= 0)
            {
                Die();
                return true;
            }
            state = PState.Hurt;
            hurtTimer = 0.38f;
            invulnTimer = invulnTime;
            float dir = transform.position.x >= sourcePos.x ? 1f : -1f;
            rb.SetVel(new Vector2(dir * 7f, 7f));
            animator.Play(hit, true);
            return true;
        }

        public void Heal(int amount)
        {
            if (state == PState.Dead) return;
            health = Mathf.Min(maxHealth, health + amount);
            if (GameSession.I != null) GameSession.I.RaiseStats();
        }

        void Die()
        {
            if (state == PState.Dead) return;
            state = PState.Dead;
            health = 0;
            moveInput = 0f;
            rb.gravityScale = gravityScale;
            animator.Play(death, true);
            AudioHub.Play(SfxId.PlayerDie, 0f);
            if (visual != null) visual.color = Color.white;
            if (transform.position.y < -4f)
            {
                rb.SetVel(Vector2.zero);
                rb.simulated = false;
            }
            if (GameSession.I != null) GameSession.I.OnPlayerDied();
        }

        public void Respawn(Vector3 pos)
        {
            rb.simulated = true;
            transform.position = pos;
            rb.SetVel(Vector2.zero);
            health = maxHealth;
            state = PState.Normal;
            invulnTimer = 2f;
            hurtTimer = 0f;
            attackCd = 0f;
            facing = 1f;
            grounded = false;
            lastFallSpeed = 0f;
            animator.Play(idle, true);
            if (visual != null) visual.color = Color.white;
            if (GameSession.I != null)
            {
                GameSession.I.RaiseStats();
                if (GameSession.I.cam != null) GameSession.I.cam.Snap();
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Vector2 center = (Vector2)transform.position + new Vector2(attackBoxOffset.x * facing, attackBoxOffset.y);
            Gizmos.DrawWireCube(center, attackBoxSize);
        }
    }
}
