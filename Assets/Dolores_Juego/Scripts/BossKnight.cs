using System.Collections;
using UnityEngine;

namespace Dolores1810
{
    /// <summary>
    /// Jefe (Capitán del nivel 2 y Coronel del nivel 3). Patrones: embestida con aviso, golpe al suelo que lanza
    /// ondas de choque que hay que saltar y, con poca vida, furia con ataques más rápidos. Barra de vida en pantalla.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class BossKnight : MonoBehaviour, IDamageable
    {
        enum BState { Dormant, Chase, Windup, Lunge, Slam, Recover, Dead }

        public EnemyConfig config;
        public string bossName = "Capitán realista";
        public SpriteRenderer visual;
        public FrameAnimator animator;
        public SpriteClip idle, run, attack, hurt, die;
        public Gate[] arenaGates;
        public Gate rewardGate;
        public Sprite waveSprite;
        public int coinReward = 40;
        public bool finalBoss;

        Rigidbody2D rb;
        BoxCollider2D col;
        int playerMask;
        BState state = BState.Dormant;
        int hp, maxHp;
        int facing = -1;
        float timer, flashTimer, desiredVelX, nextAttackAt;
        bool didHit, enraged, waveSpawned;
        int patternIndex;
        Color baseColor = Color.white;
        Vector3 startPos;

        Transform overheadRoot;
        SpriteRenderer overheadFill;
        TextMesh overheadLabel;
        static Sprite sharedBarSprite;
        const float OverheadBarWidth = 2.4f;

        public bool Active { get { return state != BState.Dormant && state != BState.Dead; } }
        public bool IsDead { get { return state == BState.Dead; } }

        static Sprite GetBarSprite()
        {
            if (sharedBarSprite != null) return sharedBarSprite;
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            var cols = new Color[16 * 16];
            for (int i = 0; i < cols.Length; i++) cols[i] = Color.white;
            tex.SetPixels(cols);
            tex.Apply();
            sharedBarSprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0f, 0.5f), 16f);
            return sharedBarSprite;
        }

        void EnsureOverheadBar()
        {
            if (overheadRoot == null)
            {
                var ex = transform.Find("BarraVidaJefe");
                if (ex != null) overheadRoot = ex;
            }
            if (overheadRoot == null)
            {
                var rootGo = new GameObject("BarraVidaJefe");
                rootGo.transform.SetParent(transform, false);
                overheadRoot = rootGo.transform;

                Sprite sp = GetBarSprite();
                Material unlit = (GameSession.I != null && GameSession.I.unlitMaterial != null) ? GameSession.I.unlitMaterial : null;

                var borderGo = new GameObject("Borde");
                borderGo.transform.SetParent(overheadRoot, false);
                borderGo.transform.localPosition = new Vector3(-OverheadBarWidth * 0.5f - 0.06f, 0f, 0f);
                borderGo.transform.localScale = new Vector3(OverheadBarWidth + 0.12f, 0.34f, 1f);
                var bsr = borderGo.AddComponent<SpriteRenderer>();
                bsr.sprite = sp;
                bsr.color = new Color(0.95f, 0.76f, 0.25f, 0.96f);
                bsr.sortingOrder = 44;
                if (unlit != null) bsr.sharedMaterial = unlit;

                var backGo = new GameObject("Fondo");
                backGo.transform.SetParent(overheadRoot, false);
                backGo.transform.localPosition = new Vector3(-OverheadBarWidth * 0.5f - 0.03f, 0f, 0f);
                backGo.transform.localScale = new Vector3(OverheadBarWidth + 0.06f, 0.28f, 1f);
                var ksr = backGo.AddComponent<SpriteRenderer>();
                ksr.sprite = sp;
                ksr.color = new Color(0.08f, 0.03f, 0.03f, 0.96f);
                ksr.sortingOrder = 45;
                if (unlit != null) ksr.sharedMaterial = unlit;

                var fillGo = new GameObject("Vida");
                fillGo.transform.SetParent(overheadRoot, false);
                fillGo.transform.localPosition = new Vector3(-OverheadBarWidth * 0.5f, 0f, 0f);
                fillGo.transform.localScale = new Vector3(OverheadBarWidth, 0.22f, 1f);
                overheadFill = fillGo.AddComponent<SpriteRenderer>();
                overheadFill.sprite = sp;
                overheadFill.color = new Color(0.92f, 0.18f, 0.15f, 1f);
                overheadFill.sortingOrder = 46;
                if (unlit != null) overheadFill.sharedMaterial = unlit;

                var lblGo = new GameObject("NombreJefe");
                lblGo.transform.SetParent(overheadRoot, false);
                lblGo.transform.localPosition = new Vector3(0f, 0.22f, 0f);
                overheadLabel = lblGo.AddComponent<TextMesh>();
                overheadLabel.characterSize = 0.11f;
                overheadLabel.fontSize = 42;
                overheadLabel.anchor = TextAnchor.LowerCenter;
                overheadLabel.alignment = TextAlignment.Center;
                overheadLabel.fontStyle = FontStyle.Bold;
                overheadLabel.color = new Color(1f, 0.9f, 0.48f, 1f);
                var mr = lblGo.GetComponent<MeshRenderer>();
                if (mr != null) mr.sortingOrder = 47;
            }
            UpdateOverheadBar();
        }

        public void UpdateOverheadBar()
        {
            if (overheadRoot == null) return;
            float headY = (config != null ? config.colliderHeight * config.scale : 2.8f) + 0.7f;
            overheadRoot.localPosition = new Vector3(0f, headY, 0f);
            overheadRoot.gameObject.SetActive(state != BState.Dead);
            if (overheadFill != null)
            {
                float k = maxHp > 0 ? Mathf.Clamp01((float)hp / maxHp) : 0f;
                overheadFill.transform.localScale = new Vector3(OverheadBarWidth * k, 0.22f, 1f);
                overheadFill.color = enraged
                    ? new Color(1f, 0.42f, 0.12f, 1f)
                    : new Color(0.92f, 0.18f, 0.15f, 1f);
            }
            if (overheadLabel != null)
            {
                overheadLabel.text = bossName + " (" + Mathf.Max(0, hp) + "/" + maxHp + ")";
            }
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<BoxCollider2D>();
            rb.gravityScale = 3.2f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var mat = new PhysicsMaterial2D("JefeSinFriccion");
            mat.friction = 0f;
            col.sharedMaterial = mat;
            playerMask = LayerMask.GetMask("Player");
            InitConfig();
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
            else if (maxHp <= 0) maxHp = 150;
            hp = maxHp;
            if (visual != null) visual.color = baseColor;
            EnsureOverheadBar();
        }

        void Start()
        {
            InitConfig();
            startPos = transform.position;
            if (animator != null && idle != null) animator.Play(idle, true);
            SetFacing(-1);
            if (GameSession.I != null)
            {
                GameSession.I.Respawned -= ResetFight;
                GameSession.I.Respawned += ResetFight;
            }
        }

        public void Relocate(Vector3 pos)
        {
            startPos = pos;
            transform.position = pos;
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.position = pos;
                rb.SetVel(Vector2.zero);
            }
        }

        void OnDestroy()
        {
            if (GameSession.I != null) GameSession.I.Respawned -= ResetFight;
            if (Active) CombatTracker.Remove();
        }

        void SetFacing(int d) { facing = d; if (visual != null) visual.flipX = d < 0; }

        public void Activate()
        {
            if (state != BState.Dormant) return;
            state = BState.Chase;
            timer = 0f;
            flashTimer = 0f;
            didHit = false;
            waveSpawned = false;
            nextAttackAt = Time.time + 1.0f;
            if (col != null) col.enabled = true;
            if (rb != null) rb.simulated = true;
            CombatTracker.Add();
            if (arenaGates != null)
            {
                foreach (var g in arenaGates) if (g != null) g.Close();
            }
            AudioHub.Play(SfxId.BossAlert, 0f);
            var gs = GameSession.I;
            if (gs != null)
            {
                gs.BossStarted(bossName, maxHp);
                gs.ShowToast(bossName);
                gs.Respawned -= ResetFight;
                gs.Respawned += ResetFight;
            }
            MusicManager.Ensure().SetMood(Mood.Boss);
            EnsureOverheadBar();
        }

        /// <summary>Si el jugador muere durante el combate, el jefe reinicia toda su vida y las rejas se abren para poder volver a entrar.</summary>
        public void ResetFight()
        {
            if (state == BState.Dead) return;
            var gs = GameSession.I;
            bool wasActive = state != BState.Dormant;
            state = BState.Dormant;
            hp = maxHp;
            enraged = false;
            desiredVelX = 0f;
            timer = 0f;
            flashTimer = 0f;
            didHit = false;
            waveSpawned = false;
            nextAttackAt = 0f;
            if (wasActive) CombatTracker.Remove();
            transform.position = startPos;
            if (rb != null)
            {
                rb.simulated = true;
                rb.position = startPos;
                rb.SetVel(Vector2.zero);
            }
            if (col != null) col.enabled = true;
            if (visual != null) visual.color = baseColor;
            if (animator != null && idle != null) animator.Play(idle, true);
            SetFacing(-1);
            if (arenaGates != null)
            {
                foreach (var g in arenaGates) if (g != null) g.Open();
            }
            if (wasActive && gs != null) gs.BossEnded();
            if (wasActive) MusicManager.Ensure().SetMood(Mood.Explore);
            UpdateOverheadBar();
        }

        void Update()
        {
            if (state == BState.Dead) return;
            var gs = GameSession.I;
            if (gs != null && (gs.State == GameState.Paused || gs.State == GameState.Shop)) return;

            PlatformerPlayer pl = gs != null ? gs.player : null;
            bool playerDead = (pl != null && pl.IsDead) || (gs != null && (gs.State == GameState.PlayerDead || gs.State == GameState.GameOver));
            if (playerDead)
            {
                if (state != BState.Dormant || hp < maxHp)
                {
                    ResetFight();
                }
                return;
            }
            bool target = pl != null && !pl.IsDead && (gs == null || gs.State == GameState.Playing);

            if (state == BState.Dormant)
            {
                if (target && Mathf.Abs(pl.transform.position.x - transform.position.x) < 10f && Mathf.Abs(pl.transform.position.y - transform.position.y) < 5f)
                {
                    Activate();
                }
                return;
            }

            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                if (flashTimer <= 0f && state != BState.Windup && visual != null) visual.color = baseColor;
            }

            float dx = target ? pl.transform.position.x - transform.position.x : 0f;
            timer -= Time.deltaTime;

            switch (state)
            {
                case BState.Chase:
                    if (!target) { desiredVelX = 0f; animator.Play(idle); break; }
                    SetFacing(dx > 0f ? 1 : -1);
                    float spd = (config != null ? config.chaseSpeed : 3f) * (enraged ? 1.25f : 1f);
                    if (Mathf.Abs(dx) > 2.0f) { desiredVelX = facing * spd; animator.Play(run); }
                    else { desiredVelX = 0f; animator.Play(idle); }
                    if (Time.time >= nextAttackAt) BeginPattern();
                    break;
                case BState.Windup:
                    desiredVelX = 0f;
                    visual.color = ((int)(Time.time * 20f)) % 2 == 0 ? new Color(1f, 0.35f, 0.3f, 1f) : baseColor;
                    if (timer <= 0f)
                    {
                        visual.color = baseColor;
                        if (patternIndex % 2 == 0) StartLunge(); else StartSlam();
                    }
                    break;
                case BState.Lunge:
                    desiredVelX = facing * 9.5f;
                    if (!didHit) HitCheck(1.3f, new Vector2(1.6f, 1.6f), new Vector2(0.9f, 0.9f));
                    if (timer <= 0f) EndAttack(1.0f);
                    break;
                case BState.Slam:
                    desiredVelX = 0f;
                    if (!waveSpawned && (animator.Frame >= 6 || timer <= 0.45f)) { waveSpawned = true; SpawnWaves(); }
                    if (animator.Finished || timer <= 0f) EndAttack(0.8f);
                    break;
                case BState.Recover:
                    desiredVelX = 0f;
                    animator.Play(idle);
                    if (timer <= 0f) state = BState.Chase;
                    break;
            }
        }

        void BeginPattern()
        {
            patternIndex++;
            state = BState.Windup;
            timer = enraged ? 0.55f : 0.8f;
            didHit = false;
            AudioHub.Play(SfxId.Alert, 0f, 1f);
            animator.Play(idle);
        }

        void StartLunge()
        {
            state = BState.Lunge;
            timer = 0.55f;
            didHit = false;
            animator.Play(run);
            AudioHub.Play(SfxId.Whoosh, 0.05f, 1f);
        }

        void StartSlam()
        {
            state = BState.Slam;
            timer = 1.15f;
            waveSpawned = false;
            animator.Play(attack, true);
            AudioHub.Play(SfxId.EnemySwing, 0.04f, 1f);
        }

        void EndAttack(float recover)
        {
            state = BState.Recover;
            timer = enraged ? recover * 0.6f : recover;
            nextAttackAt = Time.time + (enraged ? 1.0f : 1.8f);
        }

        void HitCheck(float unused, Vector2 size, Vector2 offset)
        {
            int hearts = config != null ? config.heartsDamage : 3;
            Vector2 center = (Vector2)transform.position + new Vector2(offset.x * facing * (config != null ? config.scale : 1f), offset.y);
            Collider2D c = Physics2D.OverlapBox(center, size * (config != null ? config.scale : 1f), 0f, playerMask);
            if (c == null) return;
            var p = c.GetComponentInParent<PlatformerPlayer>();
            if (p != null && p.TakeDamage(hearts, transform.position)) didHit = true;
        }

        void SpawnWaves()
        {
            AudioHub.Play(SfxId.Shockwave, 0.03f, 1f);
            if (GameSession.I != null && GameSession.I.cam != null) GameSession.I.cam.Shake(0.25f, 0.3f);
            int hearts = Mathf.Max(1, (config != null ? config.heartsDamage : 3) - 1);
            Shockwave.Spawn(transform.position + new Vector3(facing * 1.2f, 0f, 0f), facing, 6.5f, hearts, waveSprite);
            if (enraged) Shockwave.Spawn(transform.position + new Vector3(-facing * 1.2f, 0f, 0f), -facing, 6.5f, hearts, waveSprite);
        }

        void FixedUpdate()
        {
            if (state == BState.Dead || state == BState.Dormant) return;
            var v = rb.GetVel();
            v.x = Mathf.MoveTowards(v.x, desiredVelX, 60f * Time.fixedDeltaTime);
            rb.SetVel(v);
        }

        public bool TakeDamage(int amount, Vector2 sourcePos)
        {
            if (state == BState.Dead) return false;
            if (state == BState.Dormant) Activate();
            hp -= Mathf.Max(1, amount);
            AudioHub.Play(SfxId.HitEnemy, 0.06f);
            Fx.Hit(transform.position + new Vector3(0f, 1.3f, 0f));
            flashTimer = 0.1f;
            visual.color = new Color(1f, 0.6f, 0.6f, 1f);
            if (GameSession.I != null)
            {
                GameSession.I.BossHealth(Mathf.Max(0, hp), maxHp);
                if (GameSession.I.cam != null) GameSession.I.cam.Shake(0.08f, 0.1f);
            }
            if (!enraged && hp <= maxHp / 2)
            {
                enraged = true;
                AudioHub.Play(SfxId.BossAlert, 0f);
                Fx.Poof(transform.position);
                if (GameSession.I != null) GameSession.I.ShowToast("¡" + bossName + " se enfurece!");
            }
            if (hp <= 0) { Die(); return true; }
            UpdateOverheadBar();
            return true;
        }

        void Die()
        {
            state = BState.Dead;
            UpdateOverheadBar();
            CombatTracker.Remove();
            if (GameSession.I != null) GameSession.I.Respawned -= ResetFight;
            desiredVelX = 0f;
            animator.Play(die, true);
            AudioHub.Play(SfxId.EnemyDie, 0f);
            rb.SetVel(Vector2.zero);
            col.enabled = false;
            rb.simulated = false;
            var gs = GameSession.I;
            if (gs != null)
            {
                gs.BossEnded();
                gs.RegisterKill(config != null ? config.scoreValue : 500);
                Vector3 p = transform.position + new Vector3(0f, 1.2f, 0f);
                gs.DropCoins(p, coinReward);
                gs.SpawnPickup(Pickup.Kind.Heart, p, new Vector2(1.5f, 8f));
                gs.SpawnPickup(Pickup.Kind.Diamond, p, new Vector2(-1.5f, 8f));
                if (gs.cam != null) gs.cam.Shake(0.3f, 0.5f);
                gs.ShowToast("¡" + bossName + " derrotado!");
                gs.NotifyBossDefeated();
            }
            Fx.ConfettiBurst(transform.position + Vector3.up, 40);
            foreach (var g in arenaGates) if (g != null) g.Open();
            if (rewardGate != null) rewardGate.Open();
            MusicManager.Ensure().SetMood(Mood.Explore);
            StartCoroutine(Fade());
        }

        IEnumerator Fade()
        {
            yield return new WaitForSeconds(2.5f);
            float f = 0f;
            while (f < 1f)
            {
                f += Time.deltaTime;
                visual.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - f);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
