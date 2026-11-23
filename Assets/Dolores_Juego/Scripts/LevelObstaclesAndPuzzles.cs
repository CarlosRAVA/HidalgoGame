using UnityEngine;

namespace Dolores1810
{
    /// <summary>Trampa de fuego cíclica: aviso visual antes de encenderse, daña al jugador si está activa.</summary>
    public class FlameTrap : MonoBehaviour
    {
        public float safeTime = 2.0f;
        public float warnTime = 0.95f;
        public float activeTime = 1.5f;
        public float phaseOffset = 0f;
        public int damage = 1;
        public Vector2 hitSize = new Vector2(1.1f, 2.0f);

        public SpriteRenderer flameRenderer;
        public SpriteRenderer glowRenderer;

        float nextDamageTime;
        int playerMask;

        void Awake()
        {
            playerMask = LayerMask.GetMask("Player");
        }

        void Update()
        {
            float cycle = safeTime + warnTime + activeTime;
            if (cycle <= 0.01f) return;
            float t = (Time.time + phaseOffset) % cycle;

            bool isWarn = t >= safeTime && t < safeTime + warnTime;
            bool isActive = t >= safeTime + warnTime;

            if (flameRenderer != null)
            {
                if (isActive)
                {
                    float flicker = 0.92f + Mathf.PingPong(Time.time * 16f, 0.24f);
                    flameRenderer.transform.localScale = new Vector3(1.15f, 1.55f * flicker, 1f);
                    flameRenderer.color = new Color(1f, 0.55f, 0.2f, 1f);
                }
                else if (isWarn)
                {
                    float pulse = 0.7f + Mathf.PingPong(Time.time * 12f, 0.35f);
                    flameRenderer.transform.localScale = new Vector3(0.75f, 0.65f * pulse, 1f);
                    flameRenderer.color = new Color(1f, 0.8f, 0.25f, 0.95f);
                }
                else
                {
                    flameRenderer.transform.localScale = new Vector3(0.45f, 0.3f, 1f);
                    flameRenderer.color = new Color(0.85f, 0.35f, 0.1f, 0.55f);
                }
            }

            if (glowRenderer != null)
            {
                glowRenderer.color = isActive
                    ? new Color(1f, 0.45f, 0.15f, 0.75f)
                    : (isWarn ? new Color(1f, 0.78f, 0.2f, 0.5f) : new Color(0.9f, 0.4f, 0.1f, 0.2f));
                glowRenderer.transform.localScale = Vector3.one * (isActive ? 2.6f : (isWarn ? 1.8f : 1.1f));
            }

            if (isActive && Time.time >= nextDamageTime)
            {
                Vector2 center = (Vector2)transform.position + new Vector2(0f, hitSize.y * 0.5f);
                Collider2D hit = Physics2D.OverlapBox(center, hitSize, 0f, playerMask);
                if (hit != null)
                {
                    var p = hit.GetComponentInParent<PlatformerPlayer>();
                    if (p != null && p.TakeDamage(damage, transform.position + Vector3.down * 0.3f))
                    {
                        nextDamageTime = Time.time + 0.8f;
                        AudioHub.Play(SfxId.Spike, 0.05f);
                    }
                }
            }
        }
    }

    /// <summary>Obstáculo de pinchos que se desliza horizontalmente entre dos puntos.</summary>
    public class MovingHazard : MonoBehaviour
    {
        public Vector3 pointA;
        public Vector3 pointB;
        public float speed = 3.2f;
        public float phase = 0f;

        void Update()
        {
            float u = Mathf.PingPong(Time.time * speed * 0.25f + phase, 1f);
            float smooth = u * u * (3f - 2f * u);
            transform.position = Vector3.Lerp(pointA, pointB, smooth);
        }
    }

    /// <summary>Plataforma móvil que transporta al jugador al pararse encima.</summary>
    public class MovingPlatform : MonoBehaviour
    {
        public Vector3 pointA;
        public Vector3 pointB;
        public float speed = 2.4f;
        public float phase = 0f;
        public Vector2 platformSize = new Vector2(3.2f, 0.34f);

        int playerMask;
        Vector3 prevPos;

        void Awake()
        {
            playerMask = LayerMask.GetMask("Player");
            prevPos = transform.position;
        }

        void FixedUpdate()
        {
            float u = Mathf.PingPong(Time.fixedTime * speed * 0.25f + phase, 1f);
            float smooth = u * u * (3f - 2f * u);
            Vector3 next = Vector3.Lerp(pointA, pointB, smooth);
            Vector3 delta = next - prevPos;
            transform.position = next;
            prevPos = next;

            if (delta.sqrMagnitude < 0.0000001f) return;
            Vector2 checkCenter = (Vector2)next + new Vector2(0f, platformSize.y * 0.5f + 0.14f);
            Vector2 checkSize = new Vector2(platformSize.x * 0.95f, 0.28f);
            Collider2D hit = Physics2D.OverlapBox(checkCenter, checkSize, 0f, playerMask);
            if (hit != null)
            {
                var prb = hit.GetComponentInParent<Rigidbody2D>();
                if (prb != null && prb.GetVel().y <= 0.25f)
                {
                    prb.position += (Vector2)delta;
                }
            }
        }
    }

    /// <summary>Cartel o puerta bloqueada con pista contextual (solo muestra indicación al acercarse).</summary>
    public class PuzzleHintSign : MonoBehaviour, IInteractable
    {
        public string hintText;
        public Gate linkedGate;
        public string Prompt { get { return (linkedGate != null && linkedGate.IsOpen) ? "" : hintText; } }
        public bool Available { get { return !string.IsNullOrEmpty(hintText) && (linkedGate == null || !linkedGate.IsOpen); } }
        public bool HintOnly { get { return true; } }
        public void Interact(PlatformerPlayer player) { }
    }

    /// <summary>Mini-Jefe guardián: bloquea una reja de paso hasta que el jugador lo derrote.</summary>
    public class MiniBossGuard : MonoBehaviour
    {
        public EnemyKnight knight;
        public Gate blockingGate;
        public string miniBossTitle = "Sargento de la Guardia";
        public int bonusCoins = 35;

        bool announced;
        bool opened;

        void Update()
        {
            if (opened) return;
            var gs = GameSession.I;
            var pl = gs != null ? gs.player : null;

            if (!announced && pl != null && !pl.IsDead && knight != null && !knight.IsDead)
            {
                float dx = Mathf.Abs(pl.transform.position.x - knight.transform.position.x);
                if (dx < 11f)
                {
                    announced = true;
                    if (knight.hpBar != null) knight.hpBar.gameObject.SetActive(true);
                    if (gs != null) gs.ShowToast("¡Mini-Jefe: " + miniBossTitle + "! Derrótalo para abrir la reja.", 3.4f);
                    AudioHub.Play(SfxId.BossAlert, 0.05f, 0.85f);
                }
            }

            if (knight != null && !knight.IsDead && blockingGate != null && !blockingGate.IsOpen)
            {
                float maxAllowedX = blockingGate.transform.position.x - 0.85f;
                if (knight.transform.position.x > maxAllowedX)
                {
                    Vector3 kp = knight.transform.position;
                    kp.x = maxAllowedX;
                    knight.transform.position = kp;
                }
            }

            if (knight == null || knight.IsDead)
            {
                opened = true;
                if (blockingGate != null)
                {
                    blockingGate.Open();
                    Fx.Sparkle(blockingGate.transform.position + Vector3.up * 1.2f, new Color(0.4f, 1f, 0.55f, 1f));
                }
                AudioHub.Play(SfxId.Secret, 0f);
                if (gs != null)
                {
                    Vector3 dropPos = blockingGate != null ? blockingGate.transform.position + new Vector3(-1.2f, 0.8f, 0f) : transform.position;
                    gs.DropCoins(dropPos, bonusCoins);
                    gs.SpawnPickup(Pickup.Kind.Heart, dropPos + Vector3.up * 0.4f, new Vector2(0f, 6.5f));
                    gs.ShowToast("¡" + miniBossTitle + " derrotado! La puerta se ha abierto.", 3.5f);
                }
            }
        }
    }

    /// <summary>Palanca de puzzle de 3 antorchas.</summary>
    public class PuzzleTorchLever : MonoBehaviour, IInteractable
    {
        public LockGatePuzzle puzzle;
        public int leverIndex;
        public Sprite offSprite, onSprite;
        SpriteRenderer sr;
        bool stateOn;

        public string Prompt { get { return puzzle != null && puzzle.Solved ? "" : "Presiona E para accionar palanca " + (leverIndex + 1); } }
        public bool Available { get { return puzzle != null && !puzzle.Solved; } }
        public bool HintOnly { get { return false; } }

        void Awake() { sr = GetComponent<SpriteRenderer>(); }

        public void Interact(PlatformerPlayer player)
        {
            if (puzzle == null || puzzle.Solved) return;
            stateOn = !stateOn;
            if (sr != null)
            {
                if (stateOn && onSprite != null) sr.sprite = onSprite;
                else if (!stateOn && offSprite != null) sr.sprite = offSprite;
            }
            AudioHub.Play(SfxId.Lever, 0.04f);
            Fx.Sparkle(transform.position + Vector3.up * 0.5f, new Color(1f, 0.85f, 0.35f, 1f));
            puzzle.ToggleLever(leverIndex);
        }
    }

    /// <summary>Puzzle de combinación: 3 palancas que encienden/apagan 3 antorchas para abrir la puerta.</summary>
    public class LockGatePuzzle : MonoBehaviour
    {
        public bool[] torches = new bool[3];
        public SpriteRenderer[] torchGlows;
        public Gate gate;
        public int bonusCoins = 50;
        public bool Solved { get; private set; }

        void Start()
        {
            RefreshVisuals();
        }

        public void ToggleLever(int index)
        {
            if (Solved) return;
            if (index == 0) { torches[0] = !torches[0]; torches[1] = !torches[1]; }
            else if (index == 1) { torches[1] = !torches[1]; torches[2] = !torches[2]; }
            else if (index == 2) { torches[0] = !torches[0]; }

            RefreshVisuals();

            if (torches[0] && torches[1] && torches[2])
            {
                Solved = true;
                if (gate != null)
                {
                    gate.Open();
                    Fx.Sparkle(gate.transform.position + Vector3.up * 1.4f, new Color(0.4f, 1f, 0.55f, 1f));
                }
                AudioHub.Play(SfxId.Secret, 0f);
                if (GameSession.I != null)
                {
                    GameSession.I.AddCoins(bonusCoins);
                    GameSession.I.ShowToast("¡Acertijo resuelto! La puerta se ha abierto (+" + bonusCoins + " monedas)", 3.5f);
                }
            }
            else if (GameSession.I != null)
            {
                int lit = (torches[0] ? 1 : 0) + (torches[1] ? 1 : 0) + (torches[2] ? 1 : 0);
                GameSession.I.ShowToast("Antorchas encendidas: " + lit + " / 3", 1.8f);
            }
        }

        void RefreshVisuals()
        {
            if (torchGlows == null) return;
            for (int i = 0; i < torchGlows.Length && i < torches.Length; i++)
            {
                if (torchGlows[i] == null) continue;
                torchGlows[i].color = torches[i]
                    ? new Color(0.35f, 1f, 0.5f, 0.98f)
                    : new Color(0.35f, 0.25f, 0.25f, 0.35f);
                torchGlows[i].transform.localScale = Vector3.one * (torches[i] ? 2.1f : 0.9f);
            }
        }
    }

    /// <summary>Mástil/pedestal de una franja de la Bandera Mexicana (1° Izquierda Verde, 2° Centro Blanco, 3° Derecha Rojo).</summary>
    public class SequencePedestal : MonoBehaviour, IInteractable
    {
        public SequencePuzzleController controller;
        public int orderId; // 0 = Verde (Izquierda), 1 = Blanco (Centro), 2 = Rojo (Derecha)
        public string labelName;
        public Color flagColor;
        public SpriteRenderer bannerRenderer;
        public SpriteRenderer doorFlagStripe;
        public SpriteRenderer glow;
        public Transform bannerTransform;
        public bool activated;

        public string Prompt
        {
            get
            {
                if (controller != null && (controller.Solved || activated)) return "";
                return "Presiona E: Izar franja " + (orderId + 1) + "° (" + labelName + ")";
            }
        }
        public bool Available { get { return controller != null && !controller.Solved && !activated; } }
        public bool HintOnly { get { return false; } }

        public void SetLit(bool lit)
        {
            activated = lit;
            if (bannerTransform != null)
            {
                bannerTransform.localPosition = new Vector3(0f, lit ? 1.75f : 0.95f, 0f);
                bannerTransform.localScale = lit ? new Vector3(1.18f, 1.18f, 1f) : new Vector3(0.95f, 0.95f, 1f);
            }
            if (bannerRenderer != null)
            {
                bannerRenderer.color = lit ? Color.white : new Color(0.88f, 0.88f, 0.88f, 0.92f);
            }
            if (doorFlagStripe != null)
            {
                doorFlagStripe.color = lit ? Color.white : new Color(0.35f, 0.35f, 0.35f, 0.45f);
            }
            if (glow != null)
            {
                glow.color = lit
                    ? new Color(flagColor.r, flagColor.g, flagColor.b, 0.95f)
                    : new Color(flagColor.r, flagColor.g, flagColor.b, 0.45f);
                glow.transform.localScale = Vector3.one * (lit ? 2.2f : 1.2f);
            }
        }

        public void Interact(PlatformerPlayer player)
        {
            if (controller != null) controller.ActivateNode(this);
        }
    }

    /// <summary>Controlador del puzzle de formar la Bandera Mexicana en orden (1° Verde → 2° Blanco → 3° Rojo).</summary>
    public class SequencePuzzleController : MonoBehaviour
    {
        public SequencePedestal[] pedestals;
        public Gate gate;
        public int bonusCoins = 50;
        public bool Solved { get; private set; }
        int step;

        void Start()
        {
            ResetSequence();
        }

        public void ActivateNode(SequencePedestal node)
        {
            if (Solved || node == null) return;
            if (node.orderId == step)
            {
                node.SetLit(true);
                step++;
                AudioHub.Play(SfxId.Lever, 0.04f);
                Fx.Sparkle(node.transform.position + Vector3.up * 1.4f, node.flagColor);
                if (step >= 3)
                {
                    Solved = true;
                    if (gate != null)
                    {
                        gate.Open();
                        Fx.Sparkle(gate.transform.position + Vector3.up * 1.5f, new Color(0.35f, 1f, 0.5f, 1f));
                    }
                    AudioHub.Play(SfxId.Secret, 0f);
                    if (GameSession.I != null)
                    {
                        GameSession.I.AddCoins(bonusCoins);
                        GameSession.I.ShowToast("¡Bandera Mexicana formada (Verde, Blanco y Rojo)! ¡Puerta abierta! (+" + bonusCoins + " monedas)", 3.8f);
                    }
                }
                else if (GameSession.I != null)
                {
                    string nextName = step == 1 ? "2° Blanco (Centro)" : "3° Rojo (Derecha)";
                    GameSession.I.ShowToast("¡Franja " + node.labelName + " izada! (" + step + "/3) · Sigue: " + nextName, 2.2f);
                }
            }
            else
            {
                AudioHub.Play(SfxId.Error, 0f);
                ResetSequence();
                if (GameSession.I != null)
                    GameSession.I.ShowToast("Orden incorrecto. Forma la bandera en orden: 1° Verde → 2° Blanco → 3° Rojo", 3.0f);
            }
        }

        void ResetSequence()
        {
            step = 0;
            if (pedestals == null) return;
            for (int i = 0; i < pedestals.Length; i++)
                if (pedestals[i] != null) pedestals[i].SetLit(false);
        }
    }

    /// <summary>Interruptor del puzzle contrarreloj en plataformas.</summary>
    public class TimedSwitchNode : MonoBehaviour, IInteractable
    {
        public TimedSwitchPuzzle puzzle;
        public int index;
        public SpriteRenderer glow;
        public Sprite offSprite, onSprite;
        SpriteRenderer sr;
        public bool active;

        public string Prompt { get { return (puzzle != null && (puzzle.Solved || active)) ? "" : "Presiona E: Activar interruptor"; } }
        public bool Available { get { return puzzle != null && !puzzle.Solved && !active; } }
        public bool HintOnly { get { return false; } }

        void Awake() { sr = GetComponent<SpriteRenderer>(); }

        public void SetLit(bool lit)
        {
            active = lit;
            if (sr != null)
            {
                if (lit && onSprite != null) sr.sprite = onSprite;
                else if (!lit && offSprite != null) sr.sprite = offSprite;
            }
            if (glow != null)
            {
                glow.color = lit ? new Color(0.35f, 1f, 0.5f, 0.95f) : new Color(0.35f, 0.78f, 1f, 0.45f);
                glow.transform.localScale = Vector3.one * (lit ? 2.1f : 1.2f);
            }
        }

        public void Interact(PlatformerPlayer player)
        {
            if (puzzle != null) puzzle.TriggerNode(this);
        }
    }

    /// <summary>Puzzle contrarreloj: activar 3 interruptores en plataformas antes de que expire el tiempo.</summary>
    public class TimedSwitchPuzzle : MonoBehaviour
    {
        public TimedSwitchNode[] nodes;
        public Gate gate;
        public float timeLimit = 14f;
        public int bonusCoins = 60;
        public bool Solved { get; private set; }
        float timer;
        bool running;

        void Start()
        {
            ResetNodes();
        }

        public void TriggerNode(TimedSwitchNode node)
        {
            if (Solved || node == null || node.active) return;
            node.SetLit(true);
            if (!running)
            {
                running = true;
                timer = timeLimit;
            }
            AudioHub.Play(SfxId.Lever, 0.04f);
            Fx.Sparkle(node.transform.position + Vector3.up * 0.6f, new Color(0.4f, 1f, 0.55f, 1f));

            int count = 0;
            for (int i = 0; i < nodes.Length; i++) if (nodes[i] != null && nodes[i].active) count++;
            if (count >= nodes.Length)
            {
                Solved = true;
                running = false;
                if (gate != null)
                {
                    gate.Open();
                    Fx.Sparkle(gate.transform.position + Vector3.up * 1.4f, new Color(0.4f, 1f, 0.55f, 1f));
                }
                AudioHub.Play(SfxId.Secret, 0f);
                if (GameSession.I != null)
                {
                    GameSession.I.AddCoins(bonusCoins);
                    GameSession.I.ShowToast("¡Interruptores activados a tiempo! ¡Puerta abierta! (+" + bonusCoins + " monedas)", 3.5f);
                }
            }
            else if (GameSession.I != null)
            {
                GameSession.I.ShowToast("Interruptor " + count + "/" + nodes.Length + " · ¡Quedan " + Mathf.CeilToInt(timer) + "s!", 2.2f);
            }
        }

        void Update()
        {
            if (!running || Solved) return;
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                running = false;
                ResetNodes();
                AudioHub.Play(SfxId.Error, 0f);
                if (GameSession.I != null)
                    GameSession.I.ShowToast("¡Tiempo agotado! Los interruptores se reiniciaron.", 2.4f);
            }
        }

        void ResetNodes()
        {
            if (nodes == null) return;
            for (int i = 0; i < nodes.Length; i++)
                if (nodes[i] != null) nodes[i].SetLit(false);
        }
    }

    /// <summary>Construye obstáculos dinámicos, acertijos con puertas, mini-jefes y el jefe final de Nivel 1.</summary>
    public class LevelObstaclesAndPuzzles : MonoBehaviour
    {
        class Assets
        {
            public Sprite spike, platform, leverOff, leverOn, fence, chestClosed, chestOpen, sign, glow, pixel, fallbackBox;
            public Sprite[] flagStripes; // 0 = Verde, 1 = Blanco con escudo, 2 = Rojo
            public Material litMat, unlitMat;
            public int groundLayer, enemyLayer;
        }

        public static void EnsureForLevel(GameSession gs)
        {
            if (gs == null || FindAnyObjectByType<LevelObstaclesAndPuzzles>() != null) return;
            var host = new GameObject("ObstaculosYPuzzles");
            var comp = host.AddComponent<LevelObstaclesAndPuzzles>();
            comp.Build(gs);
        }

        void Build(GameSession gs)
        {
            var a = GatherAssets(gs);
            switch (gs.levelIndex)
            {
                case 1: BuildLevel1(gs, a); break;
                case 2: BuildLevel2(gs, a); break;
                case 3: BuildLevel3(gs, a); break;
            }
        }

        Assets GatherAssets(GameSession gs)
        {
            var a = new Assets();
            a.litMat = gs.litMaterial;
            a.unlitMat = gs.unlitMaterial;
            a.groundLayer = LayerMask.NameToLayer("Ground");
            if (a.groundLayer < 0) a.groundLayer = 0;
            a.enemyLayer = LayerMask.NameToLayer("Enemy");
            if (a.enemyLayer < 0) a.enemyLayer = 0;

            var hz = FindAnyObjectByType<Hazard>();
            if (hz != null)
            {
                var sr = hz.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) a.spike = sr.sprite;
            }

            var plat = FindAnyObjectByType<PlatformEffector2D>();
            if (plat != null)
            {
                var sr = plat.GetComponent<SpriteRenderer>();
                if (sr != null) a.platform = sr.sprite;
            }

            var lv = FindAnyObjectByType<GateLever>();
            if (lv != null) { a.leverOff = lv.offSprite; a.leverOn = lv.onSprite; }

            var gt = FindAnyObjectByType<Gate>();
            if (gt != null)
            {
                var sr = gt.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) a.fence = sr.sprite;
            }

            var ch = FindAnyObjectByType<TreasureChest>();
            if (ch != null) { a.chestClosed = ch.closedSprite; a.chestOpen = ch.openSprite; }

            var ig = FindAnyObjectByType<InteractGlow>();
            if (ig != null && ig.haloSprite != null) a.glow = ig.haloSprite;
            if (a.glow == null) a.glow = gs.sparkSprite;

            var signGo = GameObject.Find("Letrero");
            if (signGo != null)
            {
                var sr = signGo.GetComponent<SpriteRenderer>();
                if (sr != null) a.sign = sr.sprite;
            }
            if (a.sign == null) a.sign = a.leverOff;

            a.fallbackBox = MakeSolidSprite(new Color(0.55f, 0.42f, 0.28f, 1f), 32, 32);
            a.pixel = MakePivotLeftSprite(Color.white, 16, 16);
            if (a.spike == null) a.spike = MakeSolidSprite(new Color(0.85f, 0.25f, 0.2f, 1f), 32, 24);
            if (a.platform == null) a.platform = a.fallbackBox;
            if (a.fence == null) a.fence = MakeSolidSprite(new Color(0.35f, 0.35f, 0.4f, 1f), 24, 64);
            if (a.leverOff == null) a.leverOff = a.fallbackBox;
            if (a.leverOn == null) a.leverOn = a.leverOff;
            if (a.chestClosed == null) a.chestClosed = a.fallbackBox;
            if (a.chestOpen == null) a.chestOpen = a.chestClosed;

            a.flagStripes = new Sprite[]
            {
                MakeFlagStripeSprite(0), // Verde mexicano
                MakeFlagStripeSprite(1), // Blanco con escudo
                MakeFlagStripeSprite(2)  // Rojo mexicano
            };
            return a;
        }

        static Sprite MakeSolidSprite(Color c, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var cols = new Color[w * h];
            for (int i = 0; i < cols.Length; i++) cols[i] = c;
            tex.SetPixels(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
        }

        static Sprite MakePivotLeftSprite(Color c, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var cols = new Color[w * h];
            for (int i = 0; i < cols.Length; i++) cols[i] = c;
            tex.SetPixels(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0f, 0.5f), 16f);
        }

        /// <summary>Genera el sprite nítido de cada franja de la Bandera de México (0: Verde, 1: Blanco con águila, 2: Rojo).</summary>
        static Sprite MakeFlagStripeSprite(int kind)
        {
            const int w = 48, h = 72;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color mainCol = kind == 0
                ? new Color(0.0f, 0.60f, 0.27f, 1f)     // Verde Bandera Mexicana (#009944)
                : (kind == 1
                    ? new Color(0.98f, 0.98f, 0.99f, 1f) // Blanco Bandera Mexicana (#FAFAFC)
                    : new Color(0.81f, 0.07f, 0.15f, 1f)); // Rojo Bandera Mexicana (#CE1126)

            Color borderDark = new Color(0.14f, 0.09f, 0.05f, 1f);
            Color goldTrim = new Color(0.92f, 0.74f, 0.24f, 1f);
            Color eagleBrown = new Color(0.48f, 0.26f, 0.08f, 1f);
            Color nopalGreen = new Color(0.05f, 0.55f, 0.24f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool outer = x < 2 || x >= w - 2 || y < 2 || y >= h - 2;
                    bool gold = !outer && (x < 4 || x >= w - 4 || y < 4 || y >= h - 4);
                    Color c = outer ? borderDark : (gold ? goldTrim : mainCol);

                    if (!outer && !gold && kind == 1)
                    {
                        float dx = (x - w * 0.5f) / 11f;
                        float dy = (y - h * 0.52f) / 11f;
                        float dist = dx * dx + dy * dy;
                        if (dist < 1.0f && dy > -0.35f)
                        {
                            c = dist < 0.55f ? eagleBrown : goldTrim;
                        }
                        else if (dist < 1.15f && dy <= -0.35f && dy > -0.95f)
                        {
                            c = nopalGreen;
                        }
                    }
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 48f);
        }

        // ------------------------------------------------------------------ niveles
        void BuildLevel1(GameSession gs, Assets a)
        {
            // Obstáculos
            SpawnFlameTrap(a, 84f, 0f, 0f);
            SpawnSpikeBed(a, 214f, 0f, 4);
            SpawnMovingPlatform(a, new Vector3(212f, 1.9f, 0f), new Vector3(220f, 1.9f, 0f), 3.2f, 2.2f);
            SpawnMovingHazard(a, new Vector3(346f, 0f, 0f), new Vector3(354f, 0f, 0f), 3.1f);
            SpawnPlatform(a, 350f, 2.15f, 3.4f);
            SpawnFlameTrap(a, 478f, 0f, 0f);
            SpawnFlameTrap(a, 483f, 0f, 2.2f);
            SpawnMovingHazard(a, new Vector3(611f, 0f, 0f), new Vector3(619f, 0f, 0f), 3.3f);
            SpawnPlatform(a, 615f, 2.15f, 3.4f);
            SpawnFlameTrap(a, 675f, 0f, 0.5f);

            // Puzzle 1: Bandera Mexicana en orden (1° Verde, 2° Blanco, 3° Rojo) + Puerta que se abre al completarlo
            SpawnSequencePuzzle(gs, a, "L1_puz1", 95f, 0f, 45, true);
            // Puzzle 2: Combinación de 3 Palancas y Antorchas + Puerta
            SpawnTorchPuzzle(gs, a, "L1_puz2", 324f, 0f, 55, false);

            // Mini-Jefes que bloquean el paso hasta derrotarlos
            SpawnMiniBoss(gs, a, "Sargento de la Plaza", 275f, 280.5f, 0f, 55, 2, new Color(1f, 0.85f, 0.55f, 1f), 35);
            SpawnMiniBoss(gs, a, "Teniente de las Barricadas", 508f, 513.5f, 0f, 70, 2, new Color(0.85f, 0.92f, 1f, 1f), 40);

            // Jefe final del Nivel 1 (antes de la salida en x=772)
            SpawnLevel1EndBoss(gs, a);
        }

        void BuildLevel2(GameSession gs, Assets a)
        {
            // Obstáculos
            SpawnFlameTrap(a, 64f, 0f, 0f);
            SpawnMovingHazard(a, new Vector3(168f, 0f, 0f), new Vector3(176f, 0f, 0f), 3.4f);
            SpawnPlatform(a, 172f, 2.15f, 3.4f);
            SpawnFlameTrap(a, 308f, 0f, 0f);
            SpawnFlameTrap(a, 313f, 0f, 2.2f);
            SpawnSpikeBed(a, 444f, 0f, 4);
            SpawnMovingPlatform(a, new Vector3(442f, 1.95f, 0f), new Vector3(450f, 1.95f, 0f), 3.2f, 2.4f);
            SpawnMovingHazard(a, new Vector3(528f, 0f, 0f), new Vector3(536f, 0f, 0f), 3.5f);
            SpawnPlatform(a, 532f, 2.15f, 3.4f);
            SpawnFlameTrap(a, 680f, 0f, 1.1f);

            // Puzzle 1: Contrarreloj en plataformas + Puerta
            SpawnTimedPuzzle(gs, a, "L2_puz1",
                new Vector3[] { new Vector3(146.5f, 0f, 0f), new Vector3(150f, 2.2f, 0f), new Vector3(158f, 3.5f, 0f) },
                162.5f, 0f, 60, true);
            // Puzzle 2: Bandera Mexicana en orden (1° Verde, 2° Blanco, 3° Rojo) + Puerta
            SpawnSequencePuzzle(gs, a, "L2_puz2", 408f, 0f, 65, false);

            // Mini-Jefes que bloquean el paso hasta derrotarlos
            SpawnMiniBoss(gs, a, "Guardián del Arroyo", 270f, 275.5f, 0f, 75, 2, new Color(0.8f, 1f, 0.85f, 1f), 45);
            SpawnMiniBoss(gs, a, "Comandante del Bosque", 647f, 652.5f, 0f, 90, 3, new Color(1f, 0.78f, 0.68f, 1f), 50);
        }

        void BuildLevel3(GameSession gs, Assets a)
        {
            // Obstáculos
            SpawnFlameTrap(a, 126f, 0f, 0f);
            SpawnSpikeBed(a, 214f, 0f, 4);
            SpawnMovingPlatform(a, new Vector3(212f, 1.95f, 0f), new Vector3(220f, 1.95f, 0f), 3.2f, 2.4f);
            SpawnMovingHazard(a, new Vector3(235f, 0f, 0f), new Vector3(243f, 0f, 0f), 3.6f);
            SpawnPlatform(a, 239f, 2.15f, 3.4f);
            SpawnFlameTrap(a, 410f, 0f, 0f);
            SpawnFlameTrap(a, 415f, 0f, 2.2f);
            SpawnMovingHazard(a, new Vector3(532f, 0f, 0f), new Vector3(540f, 0f, 0f), 3.8f);
            SpawnPlatform(a, 536f, 2.15f, 3.4f);
            SpawnFlameTrap(a, 756f, 0f, 0f);
            SpawnFlameTrap(a, 761f, 0f, 2.2f);

            // Puzzle 1: Combinación de 3 Palancas y Antorchas + Puerta
            SpawnTorchPuzzle(gs, a, "L3_puz1", 58f, 0f, 70, false);
            // Puzzle 2: Bandera Mexicana en orden (1° Verde, 2° Blanco, 3° Rojo) + Puerta
            SpawnSequencePuzzle(gs, a, "L3_puz2", 256f, 0f, 80, true);

            // Mini-Jefes que bloquean el paso hasta derrotarlos
            SpawnMiniBoss(gs, a, "Capitán de los Faroles", 293f, 298.5f, 0f, 95, 3, new Color(1f, 0.85f, 0.6f, 1f), 55);
            SpawnMiniBoss(gs, a, "Guardia Mayor de la Parroquia", 555f, 560.5f, 0f, 110, 3, new Color(1f, 0.72f, 0.72f, 1f), 65);
        }

        // ------------------------------------------------------------------ constructores de obstáculos
        void SpawnFlameTrap(Assets a, float x, float y, float phaseOffset)
        {
            var go = new GameObject("TrampaFuego");
            go.transform.SetParent(transform);
            go.transform.position = new Vector3(x, y, 0f);

            var baseSr = go.AddComponent<SpriteRenderer>();
            baseSr.sprite = a.platform;
            baseSr.color = new Color(0.28f, 0.22f, 0.2f, 1f);
            baseSr.sortingOrder = -2;
            baseSr.drawMode = SpriteDrawMode.Sliced;
            baseSr.size = new Vector2(1.2f, 0.28f);

            var flameGo = new GameObject("Llama");
            flameGo.transform.SetParent(go.transform, false);
            flameGo.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            var flameSr = flameGo.AddComponent<SpriteRenderer>();
            flameSr.sprite = a.glow;
            flameSr.sortingOrder = 16;
            if (a.unlitMat != null) flameSr.sharedMaterial = a.unlitMat;

            var glowGo = new GameObject("Brillo");
            glowGo.transform.SetParent(go.transform, false);
            glowGo.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            var glowSr = glowGo.AddComponent<SpriteRenderer>();
            glowSr.sprite = a.glow;
            glowSr.sortingOrder = 15;
            if (a.unlitMat != null) glowSr.sharedMaterial = a.unlitMat;

            var ft = go.AddComponent<FlameTrap>();
            ft.phaseOffset = phaseOffset;
            ft.flameRenderer = flameSr;
            ft.glowRenderer = glowSr;
        }

        void SpawnSpikeBed(Assets a, float x0, float y, int count)
        {
            var root = new GameObject("PinchosExtra");
            root.transform.SetParent(transform);
            root.transform.position = new Vector3(x0 + count * 0.5f, y, 0f);
            for (int i = 0; i < count; i++)
            {
                var sp = new GameObject("Pico_" + i);
                sp.transform.SetParent(root.transform, false);
                sp.transform.localPosition = new Vector3(-count * 0.5f + 0.5f + i, -0.02f, 0f);
                var sr = sp.AddComponent<SpriteRenderer>();
                sr.sprite = a.spike;
                sr.sortingOrder = 5;
                if (a.litMat != null) sr.sharedMaterial = a.litMat;
            }
            var bc = root.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(count - 0.3f, 0.45f);
            bc.offset = new Vector2(0f, 0.25f);
            root.AddComponent<Hazard>();
        }

        void SpawnMovingHazard(Assets a, Vector3 pointA, Vector3 pointB, float speed)
        {
            var root = new GameObject("PinchoMovil");
            root.transform.SetParent(transform);
            root.transform.position = pointA;

            for (int i = -1; i <= 1; i++)
            {
                var sp = new GameObject("Pico_" + i);
                sp.transform.SetParent(root.transform, false);
                sp.transform.localPosition = new Vector3(i * 0.5f, 0f, 0f);
                var sr = sp.AddComponent<SpriteRenderer>();
                sr.sprite = a.spike;
                sr.color = new Color(1f, 0.72f, 0.65f, 1f);
                sr.sortingOrder = 7;
                if (a.litMat != null) sr.sharedMaterial = a.litMat;
            }

            var halo = new GameObject("Brillo");
            halo.transform.SetParent(root.transform, false);
            halo.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            var hsr = halo.AddComponent<SpriteRenderer>();
            hsr.sprite = a.glow;
            hsr.color = new Color(1f, 0.3f, 0.2f, 0.4f);
            hsr.sortingOrder = 6;
            if (a.unlitMat != null) hsr.sharedMaterial = a.unlitMat;
            halo.transform.localScale = Vector3.one * 1.8f;

            var bc = root.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(1.45f, 0.55f);
            bc.offset = new Vector2(0f, 0.28f);
            root.AddComponent<Hazard>();
            var mh = root.AddComponent<MovingHazard>();
            mh.pointA = pointA;
            mh.pointB = pointB;
            mh.speed = speed;
        }

        void SpawnPlatform(Assets a, float x, float top, float width)
        {
            const float h = 0.34375f;
            var go = new GameObject("PlataformaExtra");
            go.layer = a.groundLayer;
            go.transform.SetParent(transform);
            go.transform.position = new Vector3(x, top - h * 0.5f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = a.platform;
            if (a.litMat != null) sr.sharedMaterial = a.litMat;
            sr.sortingOrder = 2;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = new Vector2(width, h);
            var bc = go.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(width, h);
            bc.usedByEffector = true;
            var eff = go.AddComponent<PlatformEffector2D>();
            eff.useOneWay = true;
            eff.surfaceArc = 170f;
        }

        void SpawnMovingPlatform(Assets a, Vector3 pointA, Vector3 pointB, float width, float speed)
        {
            const float h = 0.34375f;
            var go = new GameObject("PlataformaMovil");
            go.layer = a.groundLayer;
            go.transform.SetParent(transform);
            go.transform.position = new Vector3(pointA.x, pointA.y - h * 0.5f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = a.platform;
            sr.color = new Color(1f, 0.92f, 0.72f, 1f);
            if (a.litMat != null) sr.sharedMaterial = a.litMat;
            sr.sortingOrder = 3;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = new Vector2(width, h);
            var bc = go.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(width, h);
            bc.usedByEffector = true;
            var eff = go.AddComponent<PlatformEffector2D>();
            eff.useOneWay = true;
            eff.surfaceArc = 170f;

            var mp = go.AddComponent<MovingPlatform>();
            mp.pointA = new Vector3(pointA.x, pointA.y - h * 0.5f, 0f);
            mp.pointB = new Vector3(pointB.x, pointB.y - h * 0.5f, 0f);
            mp.speed = speed;
            mp.platformSize = new Vector2(width, h);
        }

        // ------------------------------------------------------------------ puertas de bloqueo y recompensas
        Gate SpawnBlockingDoor(Assets a, string doorName, float x, float y, bool startOpen, string closedHint)
        {
            var doorRoot = new GameObject("Marco_" + doorName);
            doorRoot.transform.SetParent(transform);
            doorRoot.transform.position = new Vector3(x, y, 0f);

            // Dintel superior del marco de la puerta
            var header = new GameObject("Dintel");
            header.transform.SetParent(doorRoot.transform, false);
            header.transform.localPosition = new Vector3(0f, 4.45f, 0f);
            var hsr = header.AddComponent<SpriteRenderer>();
            hsr.sprite = a.platform;
            hsr.color = new Color(0.45f, 0.32f, 0.22f, 1f);
            hsr.sortingOrder = 8;
            if (a.litMat != null) hsr.sharedMaterial = a.litMat;
            hsr.drawMode = SpriteDrawMode.Tiled;
            hsr.tileMode = SpriteTileMode.Continuous;
            hsr.size = new Vector2(1.6f, 0.42f);

            // Luz indicadora de estado (Rojo = cerrada, Verde = abierta)
            var lightGo = new GameObject("LuzEstadoPuerta");
            lightGo.transform.SetParent(doorRoot.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 4.5f, 0f);
            var lsr = lightGo.AddComponent<SpriteRenderer>();
            lsr.sprite = a.glow;
            lsr.sortingOrder = 16;
            if (a.unlitMat != null) lsr.sharedMaterial = a.unlitMat;
            lightGo.transform.localScale = Vector3.one * 1.6f;

            // Reja móvil que sube al abrirse
            var gateGo = new GameObject(doorName);
            gateGo.layer = a.groundLayer;
            gateGo.transform.SetParent(doorRoot.transform, false);
            gateGo.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            for (int i = 0; i < 4; i++)
            {
                var bar = new GameObject("Barrotes_" + i);
                bar.transform.SetParent(gateGo.transform, false);
                bar.transform.localPosition = new Vector3(0f, -2.2f + i * 1.1f, 0f);
                var bsr = bar.AddComponent<SpriteRenderer>();
                bsr.sprite = a.fence;
                bsr.sortingOrder = 7;
                if (a.litMat != null) bsr.sharedMaterial = a.litMat;
                bar.transform.localScale = new Vector3(1.15f, 1.15f, 1f);
            }
            var gateCol = gateGo.AddComponent<BoxCollider2D>();
            gateCol.size = new Vector2(0.9f, 4.4f);

            var gate = gateGo.AddComponent<Gate>();
            gate.openOffset = new Vector3(0f, 4.6f, 0f);
            gate.startOpen = startOpen;
            gate.blocker = gateCol;
            gate.statusLight = lsr;
            gate.Init();

            if (!string.IsNullOrEmpty(closedHint))
            {
                var hintGo = new GameObject("AvisoPuerta");
                hintGo.transform.SetParent(doorRoot.transform, false);
                hintGo.transform.localPosition = new Vector3(-0.7f, 1.1f, 0f);
                var hbc = hintGo.AddComponent<BoxCollider2D>();
                hbc.isTrigger = true;
                hbc.size = new Vector2(1.4f, 2.4f);
                var phs = hintGo.AddComponent<PuzzleHintSign>();
                phs.hintText = closedHint;
                phs.linkedGate = gate;
            }

            return gate;
        }

        Gate SpawnPuzzleDoorAndReward(GameSession gs, Assets a, string chestId, float doorX, float y, int chestCoins, bool withHeart, string closedHint)
        {
            Gate gate = SpawnBlockingDoor(a, "PuertaPuzzle_" + chestId, doorX, y, false, closedHint);

            // Cofre de recompensa justo detrás de la puerta (accesible en cuanto se abre la puerta)
            var chGo = new GameObject("CofrePuzzle_" + chestId);
            chGo.transform.SetParent(transform);
            chGo.transform.position = new Vector3(doorX + 2.1f, y, 0f);
            var chSr = chGo.AddComponent<SpriteRenderer>();
            chSr.sprite = a.chestClosed;
            chSr.sortingOrder = 12;
            if (a.litMat != null) chSr.sharedMaterial = a.litMat;
            var chBc = chGo.AddComponent<BoxCollider2D>();
            chBc.isTrigger = true;
            chBc.size = new Vector2(1.2f, 1.1f);
            chBc.offset = new Vector2(0f, 0.55f);
            var ch = chGo.AddComponent<TreasureChest>();
            ch.chestId = chestId;
            ch.coins = chestCoins;
            ch.hidden = true;
            ch.closedSprite = a.chestClosed;
            ch.openSprite = a.chestOpen;
            var glow = chGo.AddComponent<InteractGlow>();
            glow.glowColor = new Color(0.95f, 0.7f, 1f, 1f);
            glow.haloSprite = a.glow;

            if (withHeart && gs.heartSprite != null)
            {
                var hGo = new GameObject("CorazonPuzzle");
                hGo.transform.SetParent(transform);
                hGo.transform.position = new Vector3(doorX + 3.2f, y + 0.75f, 0f);
                var hSr = hGo.AddComponent<SpriteRenderer>();
                hSr.sprite = gs.heartSprite;
                hSr.sortingOrder = 15;
                if (a.litMat != null) hSr.sharedMaterial = a.litMat;
                var hc = hGo.AddComponent<CircleCollider2D>();
                hc.isTrigger = true;
                hc.radius = 0.42f;
                var pk = hGo.AddComponent<Pickup>();
                pk.kind = Pickup.Kind.Heart;
                pk.staticSprite = gs.heartSprite;
            }

            return gate;
        }

        void SpawnClueSign(Assets a, float x, float y, string hint)
        {
            var go = new GameObject("PistaPuzzle");
            go.transform.SetParent(transform);
            go.transform.position = new Vector3(x, y - 0.02f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = a.sign;
            sr.sortingOrder = -4;
            if (a.litMat != null) sr.sharedMaterial = a.litMat;
            var bc = go.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(1.3f, 1.4f);
            bc.offset = new Vector2(0f, 0.7f);
            var s = go.AddComponent<PuzzleHintSign>();
            s.hintText = hint;
            var glow = go.AddComponent<InteractGlow>();
            glow.glowColor = new Color(1f, 0.9f, 0.45f, 1f);
            glow.haloSprite = a.glow;
        }

        // ------------------------------------------------------------------ puzzles
        void SpawnTorchPuzzle(GameSession gs, Assets a, string id, float startX, float y, int coins, bool withHeart)
        {
            SpawnClueSign(a, startX, y, "Acertijo de las Antorchas: Enciende las 3 llamas a la vez con las palancas (E) para abrir la puerta");

            var ctrlGo = new GameObject("PuzzleAntorchas_" + id);
            ctrlGo.transform.SetParent(transform);
            var ctrl = ctrlGo.AddComponent<LockGatePuzzle>();
            ctrl.bonusCoins = coins;
            ctrl.torches = new bool[] { false, true, false };
            ctrl.torchGlows = new SpriteRenderer[3];

            for (int i = 0; i < 3; i++)
            {
                float lx = startX + 2.2f + i * 2.2f;
                var tGo = new GameObject("AntorchaEstado_" + i);
                tGo.transform.SetParent(ctrlGo.transform);
                tGo.transform.position = new Vector3(lx, y + 1.85f, 0f);
                var tSr = tGo.AddComponent<SpriteRenderer>();
                tSr.sprite = a.glow;
                tSr.sortingOrder = 14;
                if (a.unlitMat != null) tSr.sharedMaterial = a.unlitMat;
                ctrl.torchGlows[i] = tSr;

                var lvGo = new GameObject("PalancaPuzzle_" + i);
                lvGo.transform.SetParent(ctrlGo.transform);
                lvGo.transform.position = new Vector3(lx, y - 0.02f, 0f);
                var sr = lvGo.AddComponent<SpriteRenderer>();
                sr.sprite = a.leverOff;
                sr.sortingOrder = -4;
                if (a.litMat != null) sr.sharedMaterial = a.litMat;
                var bc = lvGo.AddComponent<BoxCollider2D>();
                bc.isTrigger = true;
                bc.size = new Vector2(1.0f, 1.1f);
                bc.offset = new Vector2(0f, 0.55f);
                var pl = lvGo.AddComponent<PuzzleTorchLever>();
                pl.puzzle = ctrl;
                pl.leverIndex = i;
                pl.offSprite = a.leverOff;
                pl.onSprite = a.leverOn;
                var ig = lvGo.AddComponent<InteractGlow>();
                ig.glowColor = new Color(0.55f, 1f, 0.65f, 1f);
                ig.haloSprite = a.glow;
            }

            ctrl.gate = SpawnPuzzleDoorAndReward(gs, a, id, startX + 8.8f, y, coins, withHeart,
                "Puerta cerrada: Enciende las 3 antorchas con las palancas (E) para abrirla");
        }

        /// <summary>Puzzle de la Bandera Mexicana: colores reales en orden (1° Izquierda Verde, 2° Centro Blanco, 3° Derecha Rojo).</summary>
        void SpawnSequencePuzzle(GameSession gs, Assets a, string id, float startX, float y, int coins, bool withHeart)
        {
            SpawnClueSign(a, startX, y, "Acertijo de la Bandera (E): Iza en orden 1° Verde → 2° Blanco → 3° Rojo para abrir la puerta");

            var ctrlGo = new GameObject("PuzzleBandera_" + id);
            ctrlGo.transform.SetParent(transform);
            var ctrl = ctrlGo.AddComponent<SequencePuzzleController>();
            ctrl.bonusCoins = coins;

            // Colores oficiales de la Bandera Mexicana en orden correcto de izquierda a derecha: Verde, Blanco, Rojo
            string[] names = { "Verde", "Blanco", "Rojo" };
            Color[] colors =
            {
                new Color(0.0f, 0.60f, 0.27f, 1f),   // 1° Izquierda: Verde Bandera Mexicana
                new Color(0.98f, 0.98f, 1.0f, 1f),   // 2° Centro: Blanco Bandera Mexicana
                new Color(0.81f, 0.07f, 0.15f, 1f)   // 3° Derecha: Rojo Bandera Mexicana
            };

            float doorX = startX + 9.2f;
            ctrl.gate = SpawnPuzzleDoorAndReward(gs, a, id, doorX, y, coins, withHeart,
                "Puerta cerrada: Forma la Bandera Mexicana en orden (1° Verde, 2° Blanco, 3° Rojo) con E");

            // Marco de la Bandera Mexicana completa sobre la puerta que se ilumina franja por franja
            var flagBoard = new GameObject("BanderaSobrePuerta");
            flagBoard.transform.SetParent(ctrlGo.transform);
            flagBoard.transform.position = new Vector3(doorX, y + 3.1f, 0f);

            var nodes = new SequencePedestal[3];
            for (int i = 0; i < 3; i++)
            {
                float px = startX + 2.2f + i * 2.3f;
                var pGo = new GameObject("Mastil_" + names[i]);
                pGo.transform.SetParent(ctrlGo.transform);
                pGo.transform.position = new Vector3(px, y, 0f);

                // Poste / mástil del estandarte
                var poleGo = new GameObject("Poste");
                poleGo.transform.SetParent(pGo.transform, false);
                poleGo.transform.localPosition = new Vector3(0f, 1.15f, 0f);
                var poleSr = poleGo.AddComponent<SpriteRenderer>();
                poleSr.sprite = a.platform;
                poleSr.color = new Color(0.38f, 0.25f, 0.14f, 1f);
                poleSr.sortingOrder = -4;
                if (a.litMat != null) poleSr.sharedMaterial = a.litMat;
                poleSr.drawMode = SpriteDrawMode.Sliced;
                poleSr.size = new Vector2(0.22f, 2.3f);

                // Placa base con el color de la franja siempre visible
                var baseGo = new GameObject("BaseColor");
                baseGo.transform.SetParent(pGo.transform, false);
                baseGo.transform.localPosition = new Vector3(0f, 0.25f, 0f);
                var baseSr = baseGo.AddComponent<SpriteRenderer>();
                baseSr.sprite = a.flagStripes[i];
                baseSr.sortingOrder = -3;
                if (a.unlitMat != null) baseSr.sharedMaterial = a.unlitMat;
                baseGo.transform.localScale = new Vector3(0.75f, 0.35f, 1f);

                // Estandarte / Franja de la bandera que se iza al activar con E
                var bannerGo = new GameObject("FranjaBandera_" + names[i]);
                bannerGo.transform.SetParent(pGo.transform, false);
                bannerGo.transform.localPosition = new Vector3(0f, 0.95f, 0f);
                var bannerSr = bannerGo.AddComponent<SpriteRenderer>();
                bannerSr.sprite = a.flagStripes[i];
                bannerSr.sortingOrder = 11;
                if (a.unlitMat != null) bannerSr.sharedMaterial = a.unlitMat;

                // Franja correspondiente en la bandera completa junto a la puerta (Izquierda Verde, Centro Blanco, Derecha Rojo)
                var doorStripeGo = new GameObject("FranjaPuerta_" + names[i]);
                doorStripeGo.transform.SetParent(flagBoard.transform, false);
                doorStripeGo.transform.localPosition = new Vector3((i - 1) * 0.78f, 0f, 0f);
                var doorStripeSr = doorStripeGo.AddComponent<SpriteRenderer>();
                doorStripeSr.sprite = a.flagStripes[i];
                doorStripeSr.sortingOrder = 14;
                if (a.unlitMat != null) doorStripeSr.sharedMaterial = a.unlitMat;
                doorStripeGo.transform.localScale = new Vector3(0.76f, 0.76f, 1f);

                // Halo luminoso del color patrio
                var glGo = new GameObject("Brillo");
                glGo.transform.SetParent(pGo.transform, false);
                glGo.transform.localPosition = new Vector3(0f, 1.35f, 0f);
                var glSr = glGo.AddComponent<SpriteRenderer>();
                glSr.sprite = a.glow;
                glSr.sortingOrder = 10;
                if (a.unlitMat != null) glSr.sharedMaterial = a.unlitMat;

                var bc = pGo.AddComponent<BoxCollider2D>();
                bc.isTrigger = true;
                bc.size = new Vector2(1.25f, 2.0f);
                bc.offset = new Vector2(0f, 1.0f);

                var node = pGo.AddComponent<SequencePedestal>();
                node.controller = ctrl;
                node.orderId = i; // 0 = Verde, 1 = Blanco, 2 = Rojo (en orden)
                node.labelName = names[i];
                node.flagColor = colors[i];
                node.bannerRenderer = bannerSr;
                node.bannerTransform = bannerGo.transform;
                node.doorFlagStripe = doorStripeSr;
                node.glow = glSr;
                nodes[i] = node;
            }
            ctrl.pedestals = nodes;
        }

        void SpawnTimedPuzzle(GameSession gs, Assets a, string id, Vector3[] positions, float doorX, float doorY, int coins, bool withHeart)
        {
            SpawnClueSign(a, positions[0].x - 1.6f, positions[0].y, "Acertijo Contrarreloj: Activa los 3 interruptores con E en menos de 14s para abrir la puerta");

            var ctrlGo = new GameObject("PuzzleContrarreloj_" + id);
            ctrlGo.transform.SetParent(transform);
            var ctrl = ctrlGo.AddComponent<TimedSwitchPuzzle>();
            ctrl.bonusCoins = coins;
            ctrl.timeLimit = 14f;

            var nodes = new TimedSwitchNode[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                var swGo = new GameObject("Interruptor_" + i);
                swGo.transform.SetParent(ctrlGo.transform);
                swGo.transform.position = positions[i];
                var sr = swGo.AddComponent<SpriteRenderer>();
                sr.sprite = a.leverOff;
                sr.sortingOrder = -3;
                if (a.litMat != null) sr.sharedMaterial = a.litMat;
                var bc = swGo.AddComponent<BoxCollider2D>();
                bc.isTrigger = true;
                bc.size = new Vector2(1.1f, 1.2f);
                bc.offset = new Vector2(0f, 0.55f);

                var glGo = new GameObject("Luz");
                glGo.transform.SetParent(swGo.transform, false);
                glGo.transform.localPosition = new Vector3(0f, 1.25f, 0f);
                var glSr = glGo.AddComponent<SpriteRenderer>();
                glSr.sprite = a.glow;
                glSr.sortingOrder = 15;
                if (a.unlitMat != null) glSr.sharedMaterial = a.unlitMat;

                var nd = swGo.AddComponent<TimedSwitchNode>();
                nd.puzzle = ctrl;
                nd.index = i;
                nd.offSprite = a.leverOff;
                nd.onSprite = a.leverOn;
                nd.glow = glSr;
                nodes[i] = nd;
            }
            ctrl.nodes = nodes;
            ctrl.gate = SpawnPuzzleDoorAndReward(gs, a, id, doorX, doorY, coins, withHeart,
                "Puerta cerrada: Activa los 3 interruptores en las plataformas en menos de 14s");
        }

        // ------------------------------------------------------------------ mini-jefes y jefe final de nivel 1
        EnemyKnight FindTemplateKnight()
        {
            var all = FindObjectsByType<EnemyKnight>(FindObjectsInactive.Include);
            EnemyKnight best = null;
            for (int i = 0; i < all.Length; i++)
            {
                var k = all[i];
                if (k == null || k.idle == null || k.attack == null) continue;
                best = k;
                if (k.config != null && k.config.skin >= 2) break;
            }
            return best;
        }

        void SpawnMiniBoss(GameSession gs, Assets a, string title, float x, float gateX, float y, int hitsToKill, int heartsDamage, Color tint, int coinReward)
        {
            var tmpl = FindTemplateKnight();
            if (tmpl == null) return;

            var cfg = ScriptableObject.CreateInstance<EnemyConfig>();
            cfg.displayName = title;
            cfg.tier = EnemyTier.Resistente;
            cfg.skin = tmpl.config != null ? tmpl.config.skin : 2;
            cfg.tint = tint;
            cfg.scale = 1.32f;
            cfg.hitsToKill = Mathf.Max(5, hitsToKill / 10);
            cfg.heartsDamage = heartsDamage;
            cfg.patrolSpeed = 1.4f;
            cfg.chaseSpeed = 3.1f;
            cfg.aggroRange = 10f;
            cfg.loseRange = 16f;
            cfg.attackRange = 2.4f;
            cfg.attackCooldown = 1.5f;
            cfg.staggerChance = 0.25f;
            cfg.hitBoxSize = new Vector2(2.4f, 1.5f);
            cfg.hitBoxOffset = new Vector2(1.5f, 0.95f);
            cfg.colliderWidth = 1.1f;
            cfg.colliderHeight = 2.0f;
            cfg.coinReward = coinReward;
            cfg.scoreValue = 350;
            cfg.appleChance = 0.5f;
            cfg.dropsHeart = true;

            var root = new GameObject("MiniJefe_" + title);
            root.layer = a.enemyLayer;
            root.transform.SetParent(transform);
            root.transform.position = new Vector3(x, y + 0.05f, 0f);
            var rb = root.AddComponent<Rigidbody2D>();
            rb.mass = 8f;
            var bc = root.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(cfg.colliderWidth * cfg.scale, cfg.colliderHeight * cfg.scale);
            bc.offset = new Vector2(0f, cfg.colliderHeight * cfg.scale * 0.5f);

            var vis = new GameObject("Visual");
            vis.transform.SetParent(root.transform, false);
            var sr = vis.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 11;
            if (a.litMat != null) sr.sharedMaterial = a.litMat;
            var an = vis.AddComponent<FrameAnimator>();
            an.target = sr;

            var ek = root.AddComponent<EnemyKnight>();
            ek.config = cfg;
            ek.patrolRadius = 2.5f;
            ek.visual = sr;
            ek.animator = an;
            ek.idle = tmpl.idle;
            ek.walk = tmpl.walk;
            ek.run = tmpl.run;
            ek.attack = tmpl.attack;
            ek.hurt = tmpl.hurt;
            ek.die = tmpl.die;

            // Barra de vida sobre el Mini-Jefe
            var bar = new GameObject("BarraVida");
            bar.transform.SetParent(root.transform, false);
            bar.transform.localPosition = new Vector3(-0.75f, cfg.colliderHeight * cfg.scale + 0.5f, 0f);
            var back = new GameObject("Fondo");
            back.transform.SetParent(bar.transform, false);
            back.transform.localPosition = new Vector3(-0.04f, 0f, 0f);
            var bsr = back.AddComponent<SpriteRenderer>();
            bsr.sprite = a.pixel;
            bsr.color = new Color(0.08f, 0.03f, 0.03f, 0.92f);
            bsr.sortingOrder = 40;
            if (a.unlitMat != null) bsr.sharedMaterial = a.unlitMat;
            back.transform.localScale = new Vector3(1.58f, 0.24f, 1f);

            var fill = new GameObject("Vida");
            fill.transform.SetParent(bar.transform, false);
            var fsr = fill.AddComponent<SpriteRenderer>();
            fsr.sprite = a.pixel;
            fsr.color = new Color(0.95f, 0.72f, 0.2f, 1f);
            fsr.sortingOrder = 41;
            if (a.unlitMat != null) fsr.sharedMaterial = a.unlitMat;
            fill.transform.localScale = new Vector3(1.5f, 0.18f, 1f);

            ek.hpBar = bar.transform;
            ek.hpFill = fsr;
            ek.hpBarWidth = 1.5f;
            ek.InitConfig();
            bar.SetActive(true);

            // Reja que bloquea el camino hasta derrotar al Mini-Jefe
            Gate gate = SpawnBlockingDoor(a, "RejaMiniJefe_" + title, gateX, y, false,
                "¡Paso bloqueado! Derrota al Mini-Jefe (" + title + ") para abrir la reja");

            var guard = root.AddComponent<MiniBossGuard>();
            guard.knight = ek;
            guard.blockingGate = gate;
            guard.miniBossTitle = title;
            guard.bonusCoins = coinReward;
        }

        void SpawnLevel1EndBoss(GameSession gs, Assets a)
        {
            if (FindAnyObjectByType<BossKnight>() != null) return;
            var tmpl = FindTemplateKnight();
            if (tmpl == null) return;

            // Despejar pinchos o soldados normales dentro de la arena del jefe (x = 719..766)
            var hazards = FindObjectsByType<Hazard>(FindObjectsInactive.Include);
            for (int i = 0; i < hazards.Length; i++)
            {
                if (hazards[i] != null && hazards[i].transform.position.x > 720f && hazards[i].transform.position.x < 766f)
                    hazards[i].gameObject.SetActive(false);
            }
            var knights = FindObjectsByType<EnemyKnight>(FindObjectsInactive.Include);
            for (int i = 0; i < knights.Length; i++)
            {
                if (knights[i] != null && knights[i].transform.position.x > 722f && knights[i].transform.position.x < 766f)
                    knights[i].gameObject.SetActive(false);
            }

            var cfg = ScriptableObject.CreateInstance<EnemyConfig>();
            cfg.displayName = "Comandante realista";
            cfg.tier = EnemyTier.MuyResistente;
            cfg.skin = tmpl.config != null ? tmpl.config.skin : 2;
            cfg.tint = new Color(1f, 0.82f, 0.65f, 1f);
            cfg.scale = 1.44f;
            cfg.isBoss = true;
            cfg.hitsToKill = 11;
            cfg.heartsDamage = 2;
            cfg.patrolSpeed = 1.5f;
            cfg.chaseSpeed = 3.0f;
            cfg.aggroRange = 12f;
            cfg.loseRange = 20f;
            cfg.attackRange = 2.5f;
            cfg.attackCooldown = 1.8f;
            cfg.staggerChance = 0f;
            cfg.hitBoxSize = new Vector2(2.5f, 1.6f);
            cfg.hitBoxOffset = new Vector2(1.5f, 1.0f);
            cfg.colliderWidth = 1.1f;
            cfg.colliderHeight = 2.0f;
            cfg.coinReward = 40;
            cfg.scoreValue = 650;

            var root = new GameObject("Jefe_ComandanteNivel1");
            root.layer = a.enemyLayer;
            root.transform.SetParent(transform);
            root.transform.position = new Vector3(748f, 0.05f, 0f);
            var rb = root.AddComponent<Rigidbody2D>();
            rb.mass = 6f;
            var bc = root.AddComponent<BoxCollider2D>();

            var vis = new GameObject("Visual");
            vis.transform.SetParent(root.transform, false);
            var sr = vis.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 10;
            if (a.litMat != null) sr.sharedMaterial = a.litMat;
            var an = vis.AddComponent<FrameAnimator>();
            an.target = sr;

            Gate leftGate = SpawnBlockingDoor(a, "RejaArenaIzq_N1", 720f, 0f, true, "");
            Gate rightGate = SpawnBlockingDoor(a, "RejaSalidaJefe_N1", 764f, 0f, false,
                "¡Derrota al Comandante realista para abrir la salida del pueblo!");

            var boss = root.AddComponent<BossKnight>();
            boss.config = cfg;
            boss.bossName = "Comandante realista";
            boss.visual = sr;
            boss.animator = an;
            boss.idle = tmpl.idle;
            boss.run = tmpl.run;
            boss.attack = tmpl.attack;
            boss.hurt = tmpl.hurt;
            boss.die = tmpl.die;
            boss.waveSprite = gs.dustSprite != null ? gs.dustSprite : a.glow;
            boss.coinReward = 45;
            boss.finalBoss = false;
            boss.arenaGates = new Gate[] { leftGate };
            boss.rewardGate = rightGate;
            boss.InitConfig();

            var trig = new GameObject("ArenaDelJefe_Nivel1");
            trig.transform.SetParent(transform);
            trig.transform.position = new Vector3(726f, 4f, 0f);
            var tbc = trig.AddComponent<BoxCollider2D>();
            tbc.isTrigger = true;
            tbc.size = new Vector2(6f, 14f);
            trig.AddComponent<BossArenaTrigger>().boss = boss;
        }
    }
}
