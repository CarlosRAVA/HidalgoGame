using System.Collections.Generic;
using UnityEngine;

namespace Dolores1810
{
    /// <summary>
    /// Personaje jugable (Body_A): caminar, correr con resistencia, saltar, trepar escaleras, atacar con el arma
    /// equipada, interactuar (E), recibir daño en corazones y morir. Las mejoras de la tienda modifican sus valores.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class PlatformerPlayer : MonoBehaviour, IDamageable
    {
        public enum PState { Normal, Attacking, Hurt, Climbing, Dead }

        [Header("Movimiento")]
        public float walkSpeed = 4.8f;
        public float runSpeed = 8.4f;
        public float groundAccel = 70f;
        public float airAccel = 45f;
        public float jumpVelocity = 13.6f;
        public float gravityScale = 3.6f;
        public float fallGravityMul = 1.4f;
        public float jumpCutMul = 2.4f;
        public float maxFallSpeed = 24f;
        public float coyoteTime = 0.1f;
        public float jumpBuffer = 0.12f;
        public float climbSpeed = 3.4f;

        [Header("Resistencia al correr")]
        public float staminaDrain = 26f;
        public float staminaRegenDelay = 0.9f;
        [Range(0.05f, 1f)] public float exhaustedResume = 0.3f;

        [Header("Combate")]
        public float attackCooldown = 0.4f;
        public Vector2 attackBoxSize = new Vector2(2.0f, 1.6f);
        public Vector2 attackBoxOffset = new Vector2(1.05f, 0.8f);
        public float invulnTime = 1.1f;
        public float killY = -12f;
        public WeaponData weapon;

        [Header("Visual")]
        public SpriteRenderer visual;
        public FrameAnimator animator;
        public SpriteClip idle, walk, run, jump, fall, climb, hit, death;

        Rigidbody2D rb;
        BoxCollider2D col;
        int groundMask, enemyMask;
        PState state = PState.Normal;
        int health;
        int maxHealth;
        float facing = 1f;
        float moveInput, vertInput;
        bool grounded, running, exhausted;
        bool jumpHeldFlag;
        float coyoteTimer, jumpBufferTimer, attackCd, hurtTimer, invulnTimer, stepTimer;
        float lastFallSpeed, inputBlockedUntil, autoWalk;
        float stamina, staminaDelay;
        float interactScan;
        IInteractable nearby;
        ClimbZone climbZone;
        readonly HashSet<IDamageable> hitThisSwing = new HashSet<IDamageable>();

        public int Health { get { return health; } }
        public int MaxHealth { get { return maxHealth; } }
        public float Stamina { get { return stamina; } }
        public float StaminaMax { get { return PlayerStats.StaminaMax; } }
        public bool Exhausted { get { return exhausted; } }
        public bool IsRunning { get { return running; } }
        public float Facing { get { return facing; } }
        public bool IsDead { get { return state == PState.Dead; } }
        public PState State { get { return state; } }
        public Vector2 Center { get { return (Vector2)transform.position + new Vector2(0f, 0.75f); } }
        public WeaponData Weapon { get { return weapon; } }

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
            if (animator != null && animator.target == null) animator.target = visual;
            RefreshStats(false);
            health = maxHealth;
            stamina = StaminaMax;
        }

        void Start()
        {
            EquipById(PlayerProgress.EquippedWeaponId, false);
            if (animator != null) animator.Play(idle, true);
        }

        /// <summary>Recalcula corazones y resistencia máximos con las mejoras compradas.</summary>
        public void RefreshStats(bool healNewHeart)
        {
            int old = maxHealth;
            maxHealth = PlayerStats.MaxHearts;
            if (healNewHeart && maxHealth > old && health > 0) health = Mathf.Min(maxHealth, health + (maxHealth - old));
            health = Mathf.Min(health, maxHealth);
            stamina = Mathf.Min(stamina, StaminaMax);
            if (GameSession.I != null) GameSession.I.RaiseStats();
        }

        public void BlockInput(float seconds) { inputBlockedUntil = Time.unscaledTime + seconds; }
        public void AutoWalk(float dir) { autoWalk = dir; }
        public void StopAutoWalk() { autoWalk = 0f; }
        public void SetClimbZone(ClimbZone z) { climbZone = z; }
        public void ClearClimbZone(ClimbZone z) { if (climbZone == z) climbZone = null; }

        public bool EquipById(string id, bool announce)
        {
            var db = GameDatabase.I;
            if (db == null) return false;
            var w = db.GetWeapon(id);
            if (w == null || !PlayerProgress.OwnsWeapon(id)) return false;
            bool changed = weapon != w;
            weapon = w;
            PlayerProgress.Equip(id);
            if (changed && announce)
            {
                AudioHub.Play(SfxId.WeaponSwitch, 0.03f);
                if (GameSession.I != null) GameSession.I.ShowToast(w.displayName);
            }
            if (GameSession.I != null) GameSession.I.RaiseStats();
            return true;
        }

        void CycleWeapon()
        {
            var db = GameDatabase.I;
            if (db == null || db.weapons == null) return;
            int start = db.WeaponIndex(weapon != null ? weapon.id : "hacha");
            for (int k = 1; k <= db.weapons.Length; k++)
            {
                var w = db.weapons[(start + k) % db.weapons.Length];
                if (w != null && PlayerProgress.OwnsWeapon(w.id)) { EquipById(w.id, true); return; }
            }
        }

        void SelectWeaponNumber(int n)
        {
            var db = GameDatabase.I;
            if (db == null || db.weapons == null || n < 1 || n > db.weapons.Length) return;
            var w = db.weapons[n - 1];
            if (w != null && PlayerProgress.OwnsWeapon(w.id)) EquipById(w.id, true);
        }

        void Update()
        {
            if (state == PState.Dead) return;
            if (transform.position.y < killY) { Kill(); return; }

            var gs = GameSession.I;
            bool canAct = (gs == null || gs.State == GameState.Playing) && Time.unscaledTime >= inputBlockedUntil;

            float h = 0f, v = 0f;
            bool jumpDown = false, jumpHeld = false, atkDown = false, runHeld = false, interact = false;
            if (autoWalk != 0f)
            {
                h = autoWalk;
            }
            else if (canAct && state != PState.Hurt)
            {
                bool upKeys = climbZone == null;
                h = GameInput.Horizontal();
                v = GameInput.Vertical();
                jumpDown = GameInput.JumpDown(upKeys);
                jumpHeld = GameInput.JumpHeld(upKeys);
                atkDown = GameInput.AttackDown();
                runHeld = GameInput.RunHeld();
                interact = GameInput.InteractDown();
                if (state == PState.Normal)
                {
                    if (GameInput.NextWeaponDown()) CycleWeapon();
                    int n = GameInput.WeaponNumberDown();
                    if (n > 0) SelectWeaponNumber(n);
                }
            }
            moveInput = h;
            vertInput = v;
            jumpHeldFlag = jumpHeld;

            bool wasGrounded = grounded;
            grounded = state != PState.Climbing && CheckGround();
            float vy = rb.GetVel().y;
            if (grounded) coyoteTimer = coyoteTime; else coyoteTimer -= Time.deltaTime;
            if (jumpDown) jumpBufferTimer = jumpBuffer; else jumpBufferTimer -= Time.deltaTime;

            if (!wasGrounded && grounded && lastFallSpeed < -7f)
            {
                AudioHub.Play(SfxId.Land, 0.05f, 0.8f);
                Fx.DustPuff(transform.position);
            }
            if (!grounded) lastFallSpeed = vy; else lastFallSpeed = 0f;

            // --- escalar
            if (state == PState.Climbing) TickClimb(h, v, jumpDown);
            else if (climbZone != null && canAct && (state == PState.Normal) && autoWalk == 0f) TryStartClimb(v);

            // --- resistencia
            TickStamina(h, runHeld);

            // --- salto
            if (state != PState.Climbing && jumpBufferTimer > 0f && coyoteTimer > 0f && state != PState.Hurt) DoJump();

            if (state == PState.Normal && Mathf.Abs(h) > 0.1f) facing = h > 0f ? 1f : -1f;

            attackCd -= Time.deltaTime;
            if (atkDown && state == PState.Normal && attackCd <= 0f && weapon != null) StartAttack();
            if (state == PState.Attacking) TickAttack();

            if (state == PState.Hurt)
            {
                hurtTimer -= Time.deltaTime;
                if (hurtTimer <= 0f) state = PState.Normal;
            }
            if (invulnTimer > 0f) invulnTimer -= Time.deltaTime;

            // --- interacción
            interactScan -= Time.deltaTime;
            if (interactScan <= 0f && canAct)
            {
                interactScan = 0.1f;
                ScanInteractables();
            }
            if (!canAct && gs != null) gs.SetPrompt(null);
            if (interact && nearby != null && nearby.Available && !nearby.HintOnly)
            {
                nearby.Interact(this);
            }

            UpdateAnimation();
            if (visual != null)
            {
                visual.flipX = facing < 0f;
                bool blink = invulnTimer > 0f && ((int)(Time.time * 14f)) % 2 == 0;
                visual.color = blink ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
            }
        }

        void TickStamina(float h, bool runHeld)
        {
            float dt = Time.deltaTime;
            float max = StaminaMax;
            bool moving = Mathf.Abs(h) > 0.1f;
            bool wantRun = runHeld && moving && !exhausted && state != PState.Attacking && state != PState.Climbing && state != PState.Hurt;
            if (grounded)
            {
                running = wantRun && stamina > 0f;
            }
            else if (running && (exhausted || !runHeld)) running = false;

            if (running && grounded)
            {
                stamina -= staminaDrain * dt;
                staminaDelay = staminaRegenDelay;
                if (stamina <= 0f)
                {
                    stamina = 0f;
                    exhausted = true;
                    running = false;
                    AudioHub.Play(SfxId.Breath, 0.02f, 0.8f);
                }
            }
            else if (!running)
            {
                if (staminaDelay > 0f) staminaDelay -= dt;
                else
                {
                    float rate = PlayerStats.StaminaRecovery * (moving ? 1f : 1.5f);
                    stamina = Mathf.Min(max, stamina + rate * dt);
                }
            }
            if (exhausted && stamina >= max * exhaustedResume) exhausted = false;
        }

        // ------------------------------------------------------------------ escalada
        void TryStartClimb(float v)
        {
            if (climbZone == null) return;
            float y = transform.position.y;
            bool up = v > 0.5f && y < climbZone.TopY - 0.2f;
            bool down = v < -0.5f && y > climbZone.BottomY + 0.4f && y >= climbZone.TopY - 0.7f;
            if (!up && !down) return;
            state = PState.Climbing;
            attackCd = 0.2f;
            running = false;
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.SetVel(Vector2.zero);
            }
            if (animator != null) animator.Play(climb, true);
            AudioHub.Play(SfxId.Climb, 0.05f, 0.6f);
        }

        void TickClimb(float h, float v, bool jumpDown)
        {
            if (climbZone == null) { ExitClimb(Vector2.zero); return; }
            float y = transform.position.y;
            if (jumpDown)
            {
                ExitClimb(new Vector2(h * walkSpeed, jumpVelocity * 0.75f));
                return;
            }
            if (v > 0.1f && y >= climbZone.TopY - 0.06f)
            {
                float dir = climbZone.exitDirection >= 0f ? 1f : -1f;
                Vector2 p = new Vector2(climbZone.CenterX + dir * 0.9f, climbZone.TopY + 0.05f);
                if (rb == null) rb = GetComponent<Rigidbody2D>();
                if (rb != null) rb.position = p;
                transform.position = p;
                facing = dir;
                ExitClimb(Vector2.zero);
                return;
            }
            if (v < -0.1f && y <= climbZone.BottomY + 0.08f)
            {
                ExitClimb(Vector2.zero);
                return;
            }
            if (animator != null) animator.speed = Mathf.Abs(v) > 0.1f ? 1f : 0f;
            if (Mathf.Abs(v) > 0.1f)
            {
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f) { AudioHub.Play(SfxId.Climb, 0.1f, 0.5f); stepTimer = 0.34f; }
            }
        }

        void ExitClimb(Vector2 velocity)
        {
            state = PState.Normal;
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = gravityScale;
                rb.SetVel(velocity);
            }
            if (animator != null) animator.speed = 1f;
            jumpBufferTimer = 0f;
        }

        // ------------------------------------------------------------------ interacción
        void ScanInteractables()
        {
            Collider2D[] cs = Physics2D.OverlapCircleAll(Center, 1.9f);
            IInteractable best = null;
            float bestD = 999f;
            for (int i = 0; i < cs.Length; i++)
            {
                IInteractable it = cs[i].GetComponentInParent<IInteractable>();
                if (it == null || !it.Available) continue;
                float d = Vector2.Distance(Center, cs[i].bounds.center);
                bool preferReal = !it.HintOnly;
                if (preferReal) d -= 0.8f;
                if (d < bestD) { bestD = d; best = it; }
            }
            nearby = best;
            if (GameSession.I != null) GameSession.I.SetPrompt(best != null ? best.Prompt : null);
        }

        // ------------------------------------------------------------------ física
        void FixedUpdate()
        {
            var vel = rb.GetVel();
            if (state == PState.Dead)
            {
                vel.x = Mathf.MoveTowards(vel.x, 0f, 30f * Time.fixedDeltaTime);
                rb.SetVel(vel);
                return;
            }

            if (state == PState.Climbing)
            {
                float cx = climbZone != null ? climbZone.CenterX : transform.position.x;
                float vx = Mathf.Clamp((cx - transform.position.x) * 10f, -4f, 4f);
                rb.SetVel(new Vector2(vx, vertInput * climbSpeed));
                return;
            }

            if (state == PState.Hurt)
            {
                vel.x = Mathf.MoveTowards(vel.x, 0f, 14f * Time.fixedDeltaTime);
            }
            else
            {
                float baseSpeed = running ? runSpeed : walkSpeed;
                float speed = baseSpeed * PlayerStats.SpeedMultiplier * (state == PState.Attacking ? 0.5f : 1f);
                if (autoWalk != 0f) speed = walkSpeed * 0.9f;
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

        // ------------------------------------------------------------------ ataque
        void StartAttack()
        {
            state = PState.Attacking;
            running = false;
            attackCd = attackCooldown / Mathf.Max(0.5f, weapon.attackSpeed);
            hitThisSwing.Clear();
            if (animator != null)
            {
                animator.Play(weapon.attackClip, true);
                animator.speed = Mathf.Max(0.5f, weapon.attackSpeed);
            }
            AudioHub.Play(weapon.swingSfx, 0.08f);
        }

        void TickAttack()
        {
            if (weapon == null || animator == null || animator.Current != weapon.attackClip)
            {
                state = PState.Normal;
                return;
            }
            int f = animator.Frame;
            if (f >= weapon.hitStartFrame && f <= weapon.hitEndFrame) DoHitCheck();
            if (animator.Finished)
            {
                state = PState.Normal;
                animator.speed = 1f;
            }
        }

        public int CurrentDamage()
        {
            int baseDmg = weapon != null ? weapon.damage : GameRules.BaseWeaponDamage;
            return Mathf.Max(1, Mathf.RoundToInt(baseDmg * PlayerStats.AttackMultiplier));
        }

        void DoHitCheck()
        {
            float reach = weapon != null ? weapon.reach : 1f;
            Vector2 size = new Vector2(attackBoxSize.x * reach, attackBoxSize.y);
            Vector2 center = (Vector2)transform.position + new Vector2(attackBoxOffset.x * reach * facing, attackBoxOffset.y);
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, enemyMask);
            bool any = false;
            int dmg = CurrentDamage();
            for (int i = 0; i < hits.Length; i++)
            {
                IDamageable d = hits[i].GetComponentInParent<IDamageable>();
                if (d == null || hitThisSwing.Contains(d)) continue;
                hitThisSwing.Add(d);
                if (d.TakeDamage(dmg, transform.position)) any = true;
            }
            if (any && GameSession.I != null && GameSession.I.cam != null) GameSession.I.cam.Shake(0.09f, 0.14f);
        }

        void UpdateAnimation()
        {
            if (state == PState.Dead || state == PState.Hurt || state == PState.Attacking || state == PState.Climbing) return;
            if (!grounded)
            {
                animator.Play(rb.GetVel().y > 0.1f ? jump : fall);
            }
            else if (Mathf.Abs(moveInput) > 0.1f)
            {
                animator.Play(running ? run : walk);
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f)
                {
                    AudioHub.PlayPitched(SfxId.Step, running ? Random.Range(1.25f, 1.45f) : Random.Range(0.92f, 1.08f), running ? 0.7f : 0.5f);
                    stepTimer = running ? 0.24f : 0.4f;
                }
            }
            else
            {
                animator.Play(idle);
            }
        }

        // ------------------------------------------------------------------ vida
        /// <summary>amount = corazones que quita el golpe.</summary>
        public bool TakeDamage(int amount, Vector2 sourcePos)
        {
            if (state == PState.Dead || invulnTimer > 0f) return false;
            var gs = GameSession.I;
            if (gs != null && gs.State != GameState.Playing) return false;

            if (state == PState.Climbing) ExitClimb(Vector2.zero);
            health = Mathf.Max(0, health - Mathf.Max(1, amount));
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
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.SetVel(new Vector2(dir * 7f, 7f));
            if (animator != null) animator.Play(hit, true);
            return true;
        }

        public void Heal(int amount)
        {
            if (state == PState.Dead) return;
            health = Mathf.Min(maxHealth, health + amount);
            if (GameSession.I != null) GameSession.I.RaiseStats();
        }

        public void FullHeal()
        {
            if (state == PState.Dead) return;
            health = maxHealth;
            stamina = StaminaMax;
            if (GameSession.I != null) GameSession.I.RaiseStats();
        }

        public void Kill()
        {
            if (state == PState.Dead) return;
            health = 0;
            Die();
        }

        void Die()
        {
            if (state == PState.Dead) return;
            state = PState.Dead;
            health = 0;
            moveInput = 0f;
            running = false;
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.gravityScale = gravityScale;
            if (animator != null)
            {
                animator.speed = 1f;
                animator.Play(death, true);
            }
            AudioHub.Play(SfxId.PlayerDie, 0f);
            if (visual != null) visual.color = Color.white;
            if (GameSession.I != null) GameSession.I.SetPrompt(null);
            if (transform.position.y < -1.5f && rb != null)
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
            RefreshStats(false);
            health = maxHealth;
            stamina = StaminaMax;
            exhausted = false;
            state = PState.Normal;
            invulnTimer = 2f;
            hurtTimer = 0f;
            attackCd = 0f;
            facing = 1f;
            grounded = false;
            lastFallSpeed = 0f;
            autoWalk = 0f;
            animator.speed = 1f;
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
