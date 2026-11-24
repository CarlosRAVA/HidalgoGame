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
                    if (gs != null) gs.ShowToast("¡Mini-Jefe: " + miniBossTitle + "! Derrótalo para abrir la puerta.", 3.4f);
                    AudioHub.Play(SfxId.BossAlert, 0.05f, 0.85f);
                }
            }

            if (knight != null && !knight.IsDead && blockingGate != null && !blockingGate.IsOpen)
            {
                float maxAllowedX = blockingGate.transform.position.x - 0.9f;
                float minAllowedX = blockingGate.transform.position.x - 5.8f;
                if (knight.transform.position.x > maxAllowedX || knight.transform.position.x < minAllowedX)
                {
                    Vector3 kp = knight.transform.position;
                    kp.x = Mathf.Clamp(kp.x, minAllowedX, maxAllowedX);
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

    /// <summary>Palanca del puzzle de 3 antorchas: cada palanca enciende su antorcha de forma permanente sin repetirse.</summary>
    public class PuzzleTorchLever : MonoBehaviour, IInteractable
    {
        public LockGatePuzzle puzzle;
        public int leverIndex;
        public Sprite offSprite, onSprite;
        SpriteRenderer sr;
        bool stateOn;

        public string Prompt { get { return (puzzle == null || puzzle.Solved || stateOn) ? "" : "Presiona E para encender antorcha " + (leverIndex + 1); } }
        public bool Available { get { return puzzle != null && !puzzle.Solved && !stateOn; } }
        public bool HintOnly { get { return false; } }

        void Awake() { sr = GetComponent<SpriteRenderer>(); }

        public void Interact(PlatformerPlayer player)
        {
            if (puzzle == null || puzzle.Solved || stateOn) return;
            stateOn = true;
            if (sr != null && onSprite != null) sr.sprite = onSprite;
            AudioHub.Play(SfxId.Lever, 0.04f);
            Fx.Sparkle(transform.position + Vector3.up * 0.5f, new Color(1f, 0.85f, 0.35f, 1f));
            puzzle.ActivateTorch(leverIndex);
        }
    }

    /// <summary>Puzzle de las 3 antorchas: cada palanca enciende su antorcha de manera definitiva y al encender las 3 abre la puerta.</summary>
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

        public void ActivateTorch(int index)
        {
            if (Solved || index < 0 || index >= torches.Length) return;
            torches[index] = true;
            RefreshVisuals();

            int lit = (torches[0] ? 1 : 0) + (torches[1] ? 1 : 0) + (torches[2] ? 1 : 0);
            if (lit >= 3)
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
                    GameSession.I.ShowToast("¡Las 3 antorchas están encendidas! ¡Puerta abierta! (+" + bonusCoins + " monedas)", 3.5f);
                }
            }
            else if (GameSession.I != null)
            {
                GameSession.I.ShowToast("Antorcha encendida (" + lit + " / 3)", 1.8f);
            }
        }

        void RefreshVisuals()
        {
            if (torchGlows == null) return;
            for (int i = 0; i < torchGlows.Length && i < torches.Length; i++)
            {
                if (torchGlows[i] == null) continue;
                torchGlows[i].color = torches[i]
                    ? new Color(1f, 0.78f, 0.22f, 0.98f)
                    : new Color(0.32f, 0.25f, 0.25f, 0.35f);
                torchGlows[i].transform.localScale = Vector3.one * (torches[i] ? 2.2f : 0.85f);
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

    /// <summary>Construye obstáculos, acertijos con puertas cerradas, mini-jefes y el gran final compacto de los niveles 1, 2 y 3.</summary>
    public class LevelObstaclesAndPuzzles : MonoBehaviour
    {
        class Assets
        {
            public Sprite spike, platform, leverOff, leverOn, doorSprite, chestClosed, chestOpen, sign, glow, flagCheckpoint, pixel, fallbackBox;
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
                case 4: BuildLevel4(gs, a); break;
            }
            AlignEnvironmentAndHouses(gs, a);
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

            var ch = FindAnyObjectByType<TreasureChest>();
            if (ch != null) { a.chestClosed = ch.closedSprite; a.chestOpen = ch.openSprite; }

            var cp = FindAnyObjectByType<Checkpoint>();
            if (cp != null && cp.flag != null) a.flagCheckpoint = cp.flag.sprite;

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
            a.doorSprite = MakeColonialDoorSprite();
            if (a.spike == null) a.spike = MakeSolidSprite(new Color(0.85f, 0.25f, 0.2f, 1f), 32, 24);
            if (a.platform == null) a.platform = a.fallbackBox;
            if (a.leverOff == null) a.leverOff = a.fallbackBox;
            if (a.leverOn == null) a.leverOn = a.leverOff;
            if (a.chestClosed == null) a.chestClosed = a.fallbackBox;
            if (a.chestOpen == null) a.chestOpen = a.chestClosed;
            if (a.flagCheckpoint == null) a.flagCheckpoint = a.sign;

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

        /// <summary>Genera el sprite sólido de una puerta colonial de madera y herrajes para que siempre se vea claramente cerrada.</summary>
        static Sprite MakeColonialDoorSprite()
        {
            const int w = 44, h = 176;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color frameDark = new Color(0.14f, 0.08f, 0.04f, 1f);
            Color woodA = new Color(0.42f, 0.24f, 0.12f, 1f);
            Color woodB = new Color(0.35f, 0.19f, 0.09f, 1f);
            Color iron = new Color(0.24f, 0.25f, 0.28f, 1f);
            Color goldStud = new Color(0.92f, 0.74f, 0.26f, 1f);

            for (int y = 0; y < h; y++)
            {
                bool ironBand = (y >= 18 && y <= 28) || (y >= 82 && y <= 92) || (y >= 146 && y <= 156);
                for (int x = 0; x < w; x++)
                {
                    bool border = x < 3 || x >= w - 3 || y < 3 || y >= h - 3;
                    bool plankLine = (x == 14 || x == 29);
                    Color c = border ? frameDark : (ironBand ? iron : (plankLine ? frameDark : ((x / 15) % 2 == 0 ? woodA : woodB)));
                    if (ironBand && (x == 9 || x == 22 || x == 35) && (y == 23 || y == 87 || y == 151))
                        c = goldStud;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 40f);
        }

        /// <summary>Genera el sprite nítido de cada franja de la Bandera de México (0: Verde, 1: Blanco con águila, 2: Rojo).</summary>
        static Sprite MakeFlagStripeSprite(int kind)
        {
            const int w = 48, h = 72;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color mainCol = kind == 0
                ? new Color(0.0f, 0.60f, 0.27f, 1f)      // Verde Bandera Mexicana (#009944)
                : (kind == 1
                    ? new Color(0.98f, 0.98f, 0.99f, 1f)  // Blanco Bandera Mexicana (#FAFAFC)
                    : new Color(0.81f, 0.07f, 0.15f, 1f));// Rojo Bandera Mexicana (#CE1126)

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

        // ------------------------------------------------------------------ niveles (compactos ~276-336m con final claro)
        void BuildLevel1(GameSession gs, Assets a)
        {
            DisableLaddersInRange(225f, 950f);

            // Puzzle del Nivel 1: Bandera Mexicana en orden (1° Verde, 2° Blanco, 3° Rojo) + Puerta cerrada + Checkpoint
            SpawnSequencePuzzle(gs, a, "L1_puz1", 62f, 0f, 45, true);
            SpawnCheckpointAt(a, 78f, 0f, 10);

            // Obstáculos dinámicos en el recorrido
            SpawnFlameTrap(a, 88f, 0f, 0f);
            SpawnMovingHazard(a, new Vector3(136f, 0f, 0f), new Vector3(144f, 0f, 0f), 3.1f);
            SpawnPlatform(a, 140f, 2.15f, 3.4f);

            // Mini-Jefe que bloquea el paso justo frente a la segunda puerta cerrada (14 golpes = 140 HP)
            SpawnMiniBoss(gs, a, "Sargento de la Plaza", 168.5f, 171f, 0f, 14, 2, new Color(1f, 0.85f, 0.55f, 1f), 40);
            SpawnCheckpointAt(a, 191f, 0f, 11);

            SpawnSpikeBed(a, 204f, 0f, 4);
            SpawnMovingPlatform(a, new Vector3(202f, 1.9f, 0f), new Vector3(210f, 1.9f, 0f), 3.2f, 2.2f);

            // Gran Final del Nivel 1: Arena del Jefe (Comandante realista en x=230..264) + Arco Final de Salida en x=276
            SetupLevel1Finale(gs, a, 230f, 250f, 264f, 276f);
        }

        void BuildLevel2(GameSession gs, Assets a)
        {
            // Liberar suelo de plataformas bajas en x=150..158 y quitar la escalera sin salida de x=329 y plataformas de la arena
            DisablePlatformsInRange(145f, 165f, 0.5f, 6f);
            DisableLaddersInRange(260f, 950f);
            DisablePlatformsInRange(288f, 360f, 0.5f, 10f);
            ClearZoneRange(204f, 224f);

            SpawnFlameTrap(a, 56f, 0f, 0f);

            // Acertijo 1 del Nivel 2: Las 3 Antorchas Insurgentes + Puerta cerrada + Checkpoint
            SpawnTorchPuzzle(gs, a, "L2_puz1", 66f, 0f, 50, true);
            SpawnCheckpointAt(a, 84f, 0f, 10);

            // Acertijo 2 del Nivel 2 (pasando el puente y el mercader en x=140): Bandera Mexicana (1° Verde → 2° Blanco → 3° Rojo)
            SpawnSequencePuzzle(gs, a, "L2_puz2", 152f, 0f, 60, true);

            // Obstáculos dinámicos
            SpawnMovingHazard(a, new Vector3(172f, 0f, 0f), new Vector3(180f, 0f, 0f), 3.4f);
            SpawnPlatform(a, 176f, 2.15f, 3.4f);

            // Mini-Jefe en la cima de la loma ANTES de la escalera del mirador (escalera en x=227.6 queda 100% libre)
            SpawnMiniBoss(gs, a, "Guardián del Arroyo", 212.5f, 215f, 3f, 16, 2, new Color(0.8f, 1f, 0.85f, 1f), 45);

            // Acertijo 3 del Nivel 2 (al bajar la loma en x=266): Las 3 Antorchas del Alba
            SpawnTorchPuzzle(gs, a, "L2_puz3", 268f, 0f, 70, true);
            RelocateNearestCheckpoint(280f, 283.5f, 0f);

            // Gran Final del Nivel 2: Arena del Jefe (Capitán realista en x=290..324) + Arco Final del Amanecer en x=336
            SetupLevel2Finale(gs, a, 290f, 310f, 324f, 336f);
        }

        void BuildLevel3(GameSession gs, Assets a)
        {
            // Liberar suelo en x=56..72 y desactivar escaleras/plataformas posteriores a x=230
            DisablePlatformsInRange(52f, 76f, 0.5f, 6f);
            DisableLaddersInRange(230f, 950f);
            DisablePlatformsInRange(230f, 950f, 0.5f, 10f);

            // Acertijo 1 del Nivel 3: Bandera Mexicana en orden (1° Verde, 2° Blanco, 3° Rojo)
            SpawnSequencePuzzle(gs, a, "L3_puz1", 56f, 0f, 65, true);
            SpawnCheckpointAt(a, 76f, 0f, 10);

            // Acertijo 2 del Nivel 3 (después del mercader en x=92): Las 3 Antorchas de la Plaza
            SpawnTorchPuzzle(gs, a, "L3_puz2", 106f, 0f, 75, true);
            SpawnCheckpointAt(a, 124f, 0f, 11);

            // Obstáculos dinámicos
            SpawnFlameTrap(a, 138f, 0f, 0f);
            SpawnFlameTrap(a, 143f, 0f, 2.2f);
            SpawnSpikeBed(a, 180f, 0f, 4);
            SpawnMovingPlatform(a, new Vector3(178f, 1.95f, 0f), new Vector3(186f, 1.95f, 0f), 3.2f, 2.4f);

            // Mini-Jefe de la Plaza de Armas (16 golpes = 160 HP)
            SpawnMiniBoss(gs, a, "Capitán de los Faroles", 213.5f, 216f, 0f, 16, 3, new Color(1f, 0.78f, 0.72f, 1f), 60);
            SpawnCheckpointAt(a, 225f, 0f, 12);

            // Final del Nivel 3: Jefe Coronel realista (desafiante pero accesible: 16 golpes = 160 HP) + Arco hacia el Nivel 4 en x=304
            SetupLevel3Finale(gs, a, 236f, 260f, 280f, 304f);
        }

        void BuildLevel4(GameSession gs, Assets a)
        {
            // Liberar suelo en x=56..72 y desactivar escaleras/plataformas posteriores a x=230
            DisablePlatformsInRange(52f, 76f, 0.5f, 6f);
            DisableLaddersInRange(230f, 950f);
            DisablePlatformsInRange(230f, 950f, 0.5f, 10f);

            SpawnFlameTrap(a, 42f, 0f, 0.8f);

            // Acertijo 1 del Nivel 4: Las 3 Antorchas de Dolores
            SpawnTorchPuzzle(gs, a, "L4_puz1", 56f, 0f, 80, true);
            SpawnCheckpointAt(a, 75f, 0f, 10);

            // Acertijo 2 del Nivel 4 (después del mercader en x=92): Bandera Mexicana (1° Verde, 2° Blanco, 3° Rojo)
            SpawnSequencePuzzle(gs, a, "L4_puz2", 108f, 0f, 90, true);
            SpawnCheckpointAt(a, 126f, 0f, 11);

            // Obstáculos más intensos rumbo a la Parroquia
            SpawnFlameTrap(a, 138f, 0f, 0f);
            SpawnFlameTrap(a, 143f, 0f, 1.5f);
            SpawnFlameTrap(a, 148f, 0f, 3.0f);
            SpawnSpikeBed(a, 180f, 0f, 4);
            SpawnMovingPlatform(a, new Vector3(178f, 1.95f, 0f), new Vector3(186f, 1.95f, 0f), 3.2f, 2.6f);
            SpawnMovingHazard(a, new Vector3(194f, 0f, 0f), new Vector3(202f, 0f, 0f), 3.7f);
            SpawnPlatform(a, 198f, 2.15f, 3.4f);

            // Mini-Jefe de Élite antes de la Parroquia (18 golpes = 180 HP)
            SpawnMiniBoss(gs, a, "Inquisidor de la Corona", 213.5f, 216f, 0f, 18, 3, new Color(0.95f, 0.68f, 0.95f, 1f), 75);
            SpawnCheckpointAt(a, 225f, 0f, 12);

            // Gran Final del Nivel 4 y del Juego: General Realista (más difícil: 24 golpes = 240 HP, 4 corazones) + Campana de Dolores en x=310
            SetupLevel4Finale(gs, a, 236f, 262f, 282f, 310f);
        }

        // ------------------------------------------------------------------ finales de nivel y recorte de longitud
        void SetupLevel1Finale(GameSession gs, Assets a, float gateLeftX, float bossX, float gateRightX, float exitX)
        {
            ClearZoneRange(gateLeftX - 4f, exitX + 20f);
            var existingBoss = FindAnyObjectByType<BossKnight>();
            if (existingBoss != null) existingBoss.gameObject.SetActive(false);
            var existingTrig = FindAnyObjectByType<BossArenaTrigger>();
            if (existingTrig != null) existingTrig.gameObject.SetActive(false);

            // Crear Jefe Final del Nivel 1
            var tmpl = FindTemplateKnight();
            if (tmpl != null)
            {
                var cfg = ScriptableObject.CreateInstance<EnemyConfig>();
                cfg.displayName = "Comandante realista";
                cfg.tier = EnemyTier.MuyResistente;
                cfg.skin = tmpl.config != null ? tmpl.config.skin : 2;
                cfg.tint = new Color(1f, 0.82f, 0.65f, 1f);
                cfg.scale = 1.44f;
                cfg.isBoss = true;
                cfg.hitsToKill = 10;
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
                cfg.coinReward = 45;
                cfg.scoreValue = 650;

                var root = new GameObject("Jefe_ComandanteNivel1");
                root.layer = a.enemyLayer;
                root.transform.SetParent(transform);
                root.transform.position = new Vector3(bossX, 0.05f, 0f);
                var rb = root.AddComponent<Rigidbody2D>();
                rb.mass = 6f;
                root.AddComponent<BoxCollider2D>();

                var vis = new GameObject("Visual");
                vis.transform.SetParent(root.transform, false);
                var sr = vis.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 10;
                if (a.litMat != null) sr.sharedMaterial = a.litMat;
                var an = vis.AddComponent<FrameAnimator>();
                an.target = sr;

                Gate leftGate = SpawnArenaTrapGate(a, "RejaArenaIzq_N1", gateLeftX, 0f);
                Gate rightGate = SpawnBlockingDoor(a, "PuertaSalidaJefe_N1", gateRightX, 0f, false,
                    "¡Derrota al Comandante realista para abrir la salida del Nivel 1!");

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
                trig.transform.position = new Vector3(gateLeftX + 5f, 4f, 0f);
                var tbc = trig.AddComponent<BoxCollider2D>();
                tbc.isTrigger = true;
                tbc.size = new Vector2(6f, 14f);
                trig.AddComponent<BossArenaTrigger>().boss = boss;
            }

            // Mover SalidaDelNivel a exitX y construir Arco Triunfal de Salida
            var exit = FindAnyObjectByType<LevelExit>();
            if (exit != null)
            {
                exit.transform.position = new Vector3(exitX, 0f, 0f);
            }
            SpawnFinaleMonument(a, exitX, 0f, "¡FINAL DEL NIVEL 1! Cruza el arco para avanzar al Nivel 2");
            FinalizeLevelBounds(gs, a, exitX, 5, 7, 2, 1);
        }

        void SetupLevel2Finale(GameSession gs, Assets a, float gateLeftX, float bossX, float gateRightX, float exitX)
        {
            ClearZoneRange(gateLeftX - 4f, exitX + 20f);

            var boss = FindAnyObjectByType<BossKnight>();
            if (boss != null)
            {
                boss.bossName = "Capitán realista";
                boss.finalBoss = false;
                boss.coinReward = 50;
                boss.config = CloneBossConfig(boss.config, "Capitán realista", 14, 3, 3.0f, 1.45f, new Color(1f, 0.85f, 0.7f, 1f));
                boss.Relocate(new Vector3(bossX, 0.05f, 0f));
                Gate leftGate = SpawnArenaTrapGate(a, "RejaArenaIzq_N2", gateLeftX, 0f);
                Gate rightGate = SpawnBlockingDoor(a, "PuertaSalidaJefe_N2", gateRightX, 0f, false,
                    "¡Derrota al Capitán realista para abrir la salida del Nivel 2!");
                if (boss.arenaGates != null)
                {
                    for (int i = 0; i < boss.arenaGates.Length; i++)
                        if (boss.arenaGates[i] != null) boss.arenaGates[i].gameObject.SetActive(false);
                }
                if (boss.rewardGate != null) boss.rewardGate.gameObject.SetActive(false);
                boss.arenaGates = new Gate[] { leftGate };
                boss.rewardGate = rightGate;
                boss.InitConfig();

                var trig = FindAnyObjectByType<BossArenaTrigger>();
                if (trig != null) trig.transform.position = new Vector3(gateLeftX + 5f, 4f, 0f);
            }

            var exit = FindAnyObjectByType<LevelExit>();
            if (exit != null)
            {
                exit.transform.position = new Vector3(exitX, 0f, 0f);
            }
            SpawnFinaleMonument(a, exitX, 0f, "¡FINAL DEL NIVEL 2! Cruza el arco del alba hacia el Nivel 3");
            FinalizeLevelBounds(gs, a, exitX, 5, 8, 2, 1);
        }

        void SetupLevel3Finale(GameSession gs, Assets a, float gateLeftX, float bossX, float gateRightX, float exitX)
        {
            ClearZoneRange(gateLeftX - 4f, exitX + 22f);

            var boss = FindAnyObjectByType<BossKnight>();
            if (boss != null)
            {
                boss.bossName = "Coronel realista";
                boss.finalBoss = false;
                boss.coinReward = 65;
                boss.config = CloneBossConfig(boss.config, "Coronel realista", 16, 3, 3.15f, 1.48f, new Color(1f, 0.82f, 0.76f, 1f));
                boss.Relocate(new Vector3(bossX, 0.05f, 0f));
                Gate leftGate = SpawnArenaTrapGate(a, "RejaArenaIzq_N3", gateLeftX, 0f);
                Gate rightGate = SpawnBlockingDoor(a, "PuertaSalidaJefe_N3", gateRightX, 0f, false,
                    "¡Derrota al Coronel realista para abrir el paso hacia el Nivel 4!");
                if (boss.arenaGates != null)
                {
                    for (int i = 0; i < boss.arenaGates.Length; i++)
                        if (boss.arenaGates[i] != null) boss.arenaGates[i].gameObject.SetActive(false);
                }
                if (boss.rewardGate != null) boss.rewardGate.gameObject.SetActive(false);
                boss.arenaGates = new Gate[] { leftGate };
                boss.rewardGate = rightGate;
                boss.InitConfig();

                var trig = FindAnyObjectByType<BossArenaTrigger>();
                if (trig != null) trig.transform.position = new Vector3(gateLeftX + 5f, 4f, 0f);
            }

            // En el Nivel 3 ocultamos la Parroquia y la Campana (que ahora son el gran final del Nivel 4)
            var parroquia = GameObject.Find("Parroquia");
            if (parroquia != null) parroquia.SetActive(false);

            var goal = FindAnyObjectByType<LevelGoal>();
            if (goal != null) goal.gameObject.SetActive(false);

            // Crear o mover SalidaDelNivel hacia Level4
            var exit = FindAnyObjectByType<LevelExit>();
            if (exit == null)
            {
                var exitGo = new GameObject("SalidaDelNivel");
                exitGo.transform.SetParent(transform);
                var bc = exitGo.AddComponent<BoxCollider2D>();
                bc.isTrigger = true;
                bc.size = new Vector2(3f, 12f);
                bc.offset = new Vector2(0f, 4f);
                exit = exitGo.AddComponent<LevelExit>();
            }
            exit.gameObject.SetActive(true);
            exit.transform.position = new Vector3(exitX, 0f, 0f);
            exit.nextScene = "Level4";
            exit.text = "Has liberado la Plaza de Armas.\nEl General Realista resguarda la Parroquia de Dolores para la batalla final...";

            SpawnFinaleMonument(a, exitX, 0f, "¡FINAL DEL NIVEL 3! Cruza el arco hacia el Nivel 4: La Parroquia de Dolores");
            FinalizeLevelBounds(gs, a, exitX, 6, 8, 2, 1);
        }

        void SetupLevel4Finale(GameSession gs, Assets a, float gateLeftX, float bossX, float gateRightX, float bellX)
        {
            ClearZoneRange(gateLeftX - 4f, bellX + 22f);

            var boss = FindAnyObjectByType<BossKnight>();
            if (boss != null)
            {
                boss.bossName = "General Realista";
                boss.finalBoss = true;
                boss.coinReward = 100;
                boss.config = CloneBossConfig(boss.config, "General Realista", 24, 4, 3.65f, 1.58f, new Color(1f, 0.58f, 0.52f, 1f));
                boss.Relocate(new Vector3(bossX, 0.05f, 0f));
                Gate leftGate = SpawnArenaTrapGate(a, "RejaArenaIzq_N4", gateLeftX, 0f);
                Gate rightGate = SpawnBlockingDoor(a, "PuertaSalidaJefe_N4", gateRightX, 0f, false,
                    "¡Derrota al General Realista para llegar a la Campana de Dolores!");
                if (boss.arenaGates != null)
                {
                    for (int i = 0; i < boss.arenaGates.Length; i++)
                        if (boss.arenaGates[i] != null) boss.arenaGates[i].gameObject.SetActive(false);
                }
                if (boss.rewardGate != null) boss.rewardGate.gameObject.SetActive(false);
                boss.arenaGates = new Gate[] { leftGate };
                boss.rewardGate = rightGate;
                boss.InitConfig();

                var trig = FindAnyObjectByType<BossArenaTrigger>();
                if (trig != null) trig.transform.position = new Vector3(gateLeftX + 5f, 4f, 0f);
            }

            var exit = FindAnyObjectByType<LevelExit>(FindObjectsInactive.Include);
            if (exit != null) exit.gameObject.SetActive(false);

            var parroquia = GameObject.Find("Parroquia");
            if (parroquia != null)
            {
                parroquia.SetActive(true);
                parroquia.transform.position = new Vector3(bellX - 11f, -0.05f, 0f);
            }

            var goal = FindAnyObjectByType<LevelGoal>(FindObjectsInactive.Include);
            if (goal != null)
            {
                goal.gameObject.SetActive(true);
                goal.transform.position = new Vector3(bellX, 0f, 0f);
            }
            SpawnFinaleMonument(a, bellX, 0f, "¡GRAN FINAL! Toca la Campana de Dolores para dar el Grito de Independencia");
            FinalizeLevelBounds(gs, a, bellX, 6, 9, 2, 1);
        }

        EnemyConfig CloneBossConfig(EnemyConfig baseCfg, string displayName, int hitsToKill, int heartsDamage, float chaseSpeed, float scale, Color tint)
        {
            var cfg = ScriptableObject.CreateInstance<EnemyConfig>();
            cfg.displayName = displayName;
            cfg.tier = EnemyTier.MuyResistente;
            cfg.skin = baseCfg != null ? baseCfg.skin : 3;
            cfg.tint = tint;
            cfg.scale = scale;
            cfg.isBoss = true;
            cfg.hitsToKill = hitsToKill;
            cfg.heartsDamage = heartsDamage;
            cfg.patrolSpeed = baseCfg != null ? baseCfg.patrolSpeed : 1.6f;
            cfg.chaseSpeed = chaseSpeed;
            cfg.aggroRange = 14f;
            cfg.loseRange = 22f;
            cfg.attackRange = 2.6f;
            cfg.attackCooldown = 1.5f;
            cfg.staggerChance = 0f;
            cfg.hitBoxSize = new Vector2(2.6f, 1.6f);
            cfg.hitBoxOffset = new Vector2(1.5f, 1.0f);
            cfg.colliderWidth = baseCfg != null ? baseCfg.colliderWidth : 1.05f;
            cfg.colliderHeight = baseCfg != null ? baseCfg.colliderHeight : 1.9f;
            cfg.coinReward = 80;
            cfg.scoreValue = 1500;
            return cfg;
        }

        void DisableLaddersInRange(float xMin, float xMax)
        {
            var ladders = FindObjectsByType<ClimbZone>(FindObjectsInactive.Include);
            for (int i = 0; i < ladders.Length; i++)
            {
                if (ladders[i] != null && ladders[i].transform.position.x >= xMin && ladders[i].transform.position.x <= xMax)
                    ladders[i].gameObject.SetActive(false);
            }
        }

        void DisablePlatformsInRange(float xMin, float xMax, float yMin, float yMax)
        {
            var effectors = FindObjectsByType<PlatformEffector2D>(FindObjectsInactive.Include);
            for (int i = 0; i < effectors.Length; i++)
            {
                if (effectors[i] == null || effectors[i].transform.IsChildOf(transform)) continue;
                Vector3 p = effectors[i].transform.position;
                if (p.x >= xMin && p.x <= xMax && p.y >= yMin && p.y <= yMax)
                {
                    if (effectors[i].gameObject.name.Contains("Puente")) continue;
                    effectors[i].gameObject.SetActive(false);
                }
            }
        }

        void RelocateNearestCheckpoint(float nearX, float newX, float newY)
        {
            var cps = FindObjectsByType<Checkpoint>(FindObjectsInactive.Include);
            for (int i = 0; i < cps.Length; i++)
            {
                if (cps[i] != null && !cps[i].transform.IsChildOf(transform) && Mathf.Abs(cps[i].transform.position.x - nearX) < 6f)
                {
                    cps[i].transform.position = new Vector3(newX, newY, 0f);
                }
            }
        }

        bool TrySampleSolidGroundTop(float x, int groundMask, out float topY)
        {
            topY = 0f;
            RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(x, 12f), Vector2.down, 18f, groundMask);
            float bestY = -999f;
            bool found = false;
            for (int i = 0; i < hits.Length; i++)
            {
                var col = hits[i].collider;
                if (col == null || col.isTrigger) continue;
                if (col.GetComponent<PlatformEffector2D>() != null) continue;
                if (col.GetComponent<Gate>() != null) continue;
                if (col.gameObject.name.Contains("Muro")) continue;
                if (hits[i].point.y > bestY)
                {
                    bestY = hits[i].point.y;
                    found = true;
                }
            }
            if (found) topY = Mathf.Round(bestY * 20f) / 20f;
            return found;
        }

        /// <summary>
        /// Corrige cualquier desfase visual en las casitas, haciendas, luces de ventana y capas de fondo parallax:
        /// asienta todas las casas de primer y segundo plano sobre el suelo firme, oculta o reubica las que
        /// sobresalgan en escalones/ríos y sincroniza las capas del horizonte.
        /// </summary>
        void AlignEnvironmentAndHouses(GameSession gs, Assets a)
        {
            int groundMask = 1 << a.groundLayer;

            // 1. Ajustar altura base de las capas de fondo (Fondo) para que el pueblo y bosque no queden enterrados
            var bgRoot = GameObject.Find("Fondo");
            if (bgRoot != null)
            {
                var layers = bgRoot.GetComponentsInChildren<ParallaxLayer>(true);
                for (int i = 0; i < layers.Length; i++)
                {
                    var pl = layers[i];
                    if (pl == null) continue;
                    string ln = pl.gameObject.name;
                    Vector3 lp = pl.transform.position;
                    if (ln.Contains("PuebloLejano") || ln.Contains("PuebloLuces"))
                    {
                        lp.y = -0.65f;
                        pl.speedX = 0.50f;
                        pl.speedY = 0.78f;
                        pl.tileWidth = 20.48f;
                    }
                    else if (ln.Contains("BosqueCercano"))
                    {
                        lp.y = -0.55f;
                        pl.speedY = 0.78f;
                    }
                    else if (ln.Contains("BosqueLejano"))
                    {
                        lp.y = -0.65f;
                        pl.speedY = 0.76f;
                    }
                    else if (ln.Contains("Lomas"))
                    {
                        lp.y = -0.80f;
                        pl.speedY = 0.74f;
                    }
                    else if (ln.Contains("MontanasCercanas"))
                    {
                        lp.y = -0.85f;
                        pl.speedY = 0.72f;
                    }
                    else if (ln.Contains("MontanasLejanas"))
                    {
                        lp.y = -0.90f;
                        pl.speedY = 0.70f;
                    }
                    pl.transform.position = lp;
                }
            }

            // 2. Alinear todas las casitas y haciendas al nivel exacto del suelo y evitar que floten en escalones o fosas
            var casasRoot = GameObject.Find("Casas");
            if (casasRoot == null) return;

            var activeHouses = new System.Collections.Generic.List<SpriteRenderer>();
            var windowLights = new System.Collections.Generic.List<Transform>();

            for (int i = 0; i < casasRoot.transform.childCount; i++)
            {
                Transform ch = casasRoot.transform.GetChild(i);
                if (ch == null || !ch.gameObject.activeSelf) continue;
                string cn = ch.gameObject.name;
                if (cn.Contains("LuzVentana") || cn.Contains("LuzParroquia"))
                {
                    windowLights.Add(ch);
                    continue;
                }

                var sr = ch.GetComponent<SpriteRenderer>();
                if (sr == null || sr.sprite == null) continue;

                float scX = Mathf.Abs(ch.localScale.x);
                float halfW = sr.sprite.bounds.size.x * scX * 0.44f;
                float cx = ch.position.x;

                if (cn == "CasaDestacada" || cn == "Parroquia")
                {
                    float gy;
                    if (TrySampleSolidGroundTop(cx, groundMask, out gy))
                    {
                        ch.position = new Vector3(cx, gy - 0.04f, ch.position.z);
                    }
                    activeHouses.Add(sr);
                    continue;
                }

                float topC, topL, topR;
                bool okC = TrySampleSolidGroundTop(cx, groundMask, out topC);
                bool okL = TrySampleSolidGroundTop(cx - halfW, groundMask, out topL);
                bool okR = TrySampleSolidGroundTop(cx + halfW, groundMask, out topR);

                bool flat = okC && okL && okR && Mathf.Abs(topC - topL) < 0.12f && Mathf.Abs(topC - topR) < 0.12f;
                if (!flat)
                {
                    // Intentar desplazar ligeramente la casa hacia el lado plano para que no quede colgando en un escalón o puente
                    float[] shifts = { -1.8f, 1.8f, -3.2f, 3.2f };
                    for (int s = 0; s < shifts.Length; s++)
                    {
                        float testX = cx + shifts[s];
                        bool sC = TrySampleSolidGroundTop(testX, groundMask, out topC);
                        bool sL = TrySampleSolidGroundTop(testX - halfW, groundMask, out topL);
                        bool sR = TrySampleSolidGroundTop(testX + halfW, groundMask, out topR);
                        if (sC && sL && sR && Mathf.Abs(topC - topL) < 0.12f && Mathf.Abs(topC - topR) < 0.12f)
                        {
                            cx = testX;
                            flat = true;
                            break;
                        }
                    }
                }

                if (!flat)
                {
                    ch.gameObject.SetActive(false);
                    continue;
                }

                // Asentar firmemente la casa sobre el suelo (incluyendo CasaFondo que antes flotaba a +0.4m)
                float yOffset = cn == "CasaFondo" ? -0.02f : -0.04f;
                ch.position = new Vector3(cx, topC + yOffset, ch.position.z);
                activeHouses.Add(sr);
            }

            // 3. Alinear los brillos de ventana (LuzVentana) con su casa correspondiente
            for (int i = 0; i < windowLights.Count; i++)
            {
                Transform wl = windowLights[i];
                if (wl == null || wl.gameObject.name.Contains("LuzParroquia")) continue;
                SpriteRenderer bestHouse = null;
                float bestDist = 3.6f;
                for (int h = 0; h < activeHouses.Count; h++)
                {
                    var hs = activeHouses[h];
                    if (hs == null || !hs.gameObject.activeSelf) continue;
                    float d = Mathf.Abs(wl.position.x - hs.transform.position.x);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestHouse = hs;
                    }
                }
                if (bestHouse == null)
                {
                    wl.gameObject.SetActive(false);
                }
                else
                {
                    float hScale = Mathf.Abs(bestHouse.transform.localScale.y);
                    float winX = bestHouse.transform.position.x;
                    float winY = bestHouse.transform.position.y + 1.15f * hScale;
                    wl.position = new Vector3(winX, winY, wl.position.z);
                }
            }

            // 4. Poblar el escenario con composiciones creativas usando los assets de Craftpix y Dolores_Juego
            DecorateCreativeScenery(gs, a, groundMask, activeHouses);
        }

        static readonly System.Collections.Generic.Dictionary<string, Sprite> s_externalSprites =
            new System.Collections.Generic.Dictionary<string, Sprite>();

        static Sprite LoadAssetSprite(string relPath, float ppu = 128f, float bottomSkipRatio = 0f)
        {
            string key = relPath + "|" + ppu.ToString("F0") + "|" + bottomSkipRatio.ToString("F2");
            Sprite cached;
            if (s_externalSprites.TryGetValue(key, out cached) && cached != null)
                return cached;

            string fullPath = System.IO.Path.Combine(Application.dataPath, relPath);
            if (!System.IO.File.Exists(fullPath)) return null;

            byte[] bytes = System.IO.File.ReadAllBytes(fullPath);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(tex, bytes)) return null;

            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            int y0 = Mathf.Clamp(Mathf.RoundToInt(tex.height * bottomSkipRatio), 0, tex.height - 4);
            int h = tex.height - y0;
            var sp = Sprite.Create(tex, new Rect(0, y0, tex.width, h), new Vector2(0.5f, 0f), ppu);
            s_externalSprites[key] = sp;
            return sp;
        }

        SpriteRenderer SpawnSceneryProp(Assets a, Transform parent, string name, Sprite sp, Vector3 pos, int sortingOrder, float scale, Color tint, bool flipX = false, bool unlit = false)
        {
            if (sp == null) return null;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(flipX ? -scale : scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.color = tint;
            sr.sortingOrder = sortingOrder;
            var mat = unlit ? a.unlitMat : a.litMat;
            if (mat != null) sr.sharedMaterial = mat;
            return sr;
        }

        void SpawnBrazierWithGlow(Assets a, Transform parent, Sprite brazierSp, float x, float y, int sortingOrder, float scale = 0.85f)
        {
            if (brazierSp == null) return;
            var sr = SpawnSceneryProp(a, parent, "BraseroColonial", brazierSp, new Vector3(x, y - 0.03f, 0f), sortingOrder, scale, Color.white);
            if (sr != null && a.glow != null)
            {
                float topY = sr.bounds.max.y - 0.12f;
                SpawnSceneryProp(a, sr.transform, "LuzBrasero", a.glow, new Vector3(x, topY, 0f), sortingOrder + 1, 1.85f, new Color(1f, 0.78f, 0.32f, 0.62f), false, true);
            }
        }

        void SpawnStatueWithLanternGlow(Assets a, Transform parent, Sprite statueSp, float x, float y, int sortingOrder, float scale = 0.92f, bool flipX = false)
        {
            if (statueSp == null) return;
            var sr = SpawnSceneryProp(a, parent, "EstatuaFarol", statueSp, new Vector3(x, y - 0.03f, 0f), sortingOrder, scale, Color.white, flipX);
            if (sr != null && a.glow != null)
            {
                float dir = flipX ? 0.22f : -0.22f;
                SpawnSceneryProp(a, sr.transform, "LuzFarolEstatua", a.glow, new Vector3(x + dir * scale, y + 0.72f * scale, 0f), sortingOrder + 1, 1.65f, new Color(1f, 0.84f, 0.36f, 0.58f), false, true);
            }
        }

        /// <summary>
        /// Enriquece creativamente el escenario de los niveles 1 a 4 utilizando los assets de Craftpix y Dolores_Juego:
        /// variedad arquitectónica en edificios, guirnaldas festivas de papel picado, faroles con halo cálido,
        /// estatuas con farol, ruinas coloniales, arcos de piedra con campana, puestos de mercado y torres vigía.
        /// </summary>
        void DecorateCreativeScenery(GameSession gs, Assets a, int groundMask, System.Collections.Generic.List<SpriteRenderer> activeHouses)
        {
            var root = new GameObject("EscenarioCreativo");
            root.transform.SetParent(transform, false);

            // Cargar edificios adicionales de Craftpix Tropical Medieval City (recortando la sombra ovalada inferior)
            Sprite bldStorehouse = LoadAssetSprite("craftpix-988114-free-tropical-medieval-city-2d-tileset/PNG/buildings/building_4/building_1.png", 122f, 0.06f);
            Sprite bldPorchHouse = LoadAssetSprite("craftpix-988114-free-tropical-medieval-city-2d-tileset/PNG/buildings/building_5/building_1.png", 122f, 0.08f);
            Sprite bldWorkshop   = LoadAssetSprite("craftpix-988114-free-tropical-medieval-city-2d-tileset/PNG/buildings/building_10/building_1.png", 122f, 0.06f);
            Sprite bldGreenShop  = LoadAssetSprite("craftpix-988114-free-tropical-medieval-city-2d-tileset/PNG/buildings/building_11/building_1.png", 122f, 0.06f);
            Sprite bldWatchtower = LoadAssetSprite("craftpix-988114-free-tropical-medieval-city-2d-tileset/PNG/buildings/building_12/building_1.png", 124f, 0.06f);
            Sprite bldTavern     = LoadAssetSprite("craftpix-988114-free-tropical-medieval-city-2d-tileset/PNG/buildings/building_14/building_1.png", 122f, 0.06f);
            Sprite bldInn        = LoadAssetSprite("craftpix-988114-free-tropical-medieval-city-2d-tileset/PNG/buildings/building_16/building_1.png", 122f, 0.06f);

            // Cargar props ambientales de Craftpix Medieval Arena, Medieval Ruins, Tropical City y Dolores_Juego
            Sprite spArchBell    = LoadAssetSprite("craftpix-net-916026-free-medieval-training-arena-2d-tileset/PNG/Environment/Cartoon_Medieval_Training Arena_Level_Set_Environment - Arch.png", 100f, 0f);
            Sprite spBanner1     = LoadAssetSprite("craftpix-net-916026-free-medieval-training-arena-2d-tileset/PNG/Environment/Cartoon_Medieval_Training Arena_Level_Set_Environment - Banner 01.png", 110f, 0f);
            Sprite spBanner2     = LoadAssetSprite("craftpix-net-916026-free-medieval-training-arena-2d-tileset/PNG/Environment/Cartoon_Medieval_Training Arena_Level_Set_Environment - Banner 02.png", 110f, 0f);
            Sprite spBanner3     = LoadAssetSprite("craftpix-net-916026-free-medieval-training-arena-2d-tileset/PNG/Environment/Cartoon_Medieval_Training Arena_Level_Set_Environment - Banner 03.png", 110f, 0f);
            Sprite spFlag        = LoadAssetSprite("craftpix-net-916026-free-medieval-training-arena-2d-tileset/PNG/Environment/Cartoon_Medieval_Training Arena_Level_Set_Environment - Flag.png", 110f, 0f);
            Sprite spTable1      = LoadAssetSprite("craftpix-net-916026-free-medieval-training-arena-2d-tileset/PNG/Environment/Cartoon_Medieval_Training Arena_Level_Set_Environment - Table 01.png", 120f, 0f);
            Sprite spDeadTree1   = LoadAssetSprite("craftpix-net-916026-free-medieval-training-arena-2d-tileset/PNG/Environment/Cartoon_Medieval_Training Arena_Level_Set_Environment - Tree 01.png", 115f, 0f);
            Sprite spDeadTree2   = LoadAssetSprite("craftpix-net-916026-free-medieval-training-arena-2d-tileset/PNG/Environment/Cartoon_Medieval_Training Arena_Level_Set_Environment - Tree 02.png", 115f, 0f);

            Sprite spStatue      = LoadAssetSprite("craftpix-net-370528-free-medieval-ruins-cartoon-2d-tileset/PNG/Environment/Decor_Statue.png", 115f, 0f);
            Sprite spRuin1       = LoadAssetSprite("craftpix-net-370528-free-medieval-ruins-cartoon-2d-tileset/PNG/Environment/Decor_Ruins_01.png", 115f, 0f);
            Sprite spRuin2       = LoadAssetSprite("craftpix-net-370528-free-medieval-ruins-cartoon-2d-tileset/PNG/Environment/Decor_Ruins_02.png", 115f, 0f);
            Sprite spPlant       = LoadAssetSprite("craftpix-net-370528-free-medieval-ruins-cartoon-2d-tileset/PNG/Environment/Decor_Plant.png", 128f, 0f);

            Sprite spBrazier     = LoadAssetSprite("craftpix-988114-free-tropical-medieval-city-2d-tileset/PNG/decor/decor_1.png", 115f, 0.10f);
            Sprite spBowl        = LoadAssetSprite("craftpix-988114-free-tropical-medieval-city-2d-tileset/PNG/decor/decor_5.png", 140f, 0.14f);
            Sprite spCart        = LoadAssetSprite("craftpix-988114-free-tropical-medieval-city-2d-tileset/PNG/decor/decor_14.png", 125f, 0.12f);
            Sprite spGreenery3   = LoadAssetSprite("craftpix-988114-free-tropical-medieval-city-2d-tileset/PNG/decor/greenery_3.png", 128f, 0.16f);
            Sprite spPalm1       = LoadAssetSprite("craftpix-988114-free-tropical-medieval-city-2d-tileset/PNG/decor/tree_1.png", 115f, 0.12f);
            Sprite spPalm2       = LoadAssetSprite("craftpix-988114-free-tropical-medieval-city-2d-tileset/PNG/decor/tree_2.png", 115f, 0.12f);

            Sprite spPapelPicado = LoadAssetSprite("Dolores_Juego/Art/Props/papel_picado.png", 100f, 0f);
            Sprite spLamp        = LoadAssetSprite("Dolores_Juego/Art/Props/lamp.png", 120f, 0f);

            int lvl = gs != null ? gs.levelIndex : 1;
            float maxX = (lvl == 1) ? 276f : ((lvl == 2) ? 336f : ((lvl == 3) ? 304f : 310f));
            bool isTown = (lvl != 2);

            // 1. Variedad arquitectónica: intercalar edificios pintados de Craftpix en la fila de casas y fondo
            Sprite[] townVariety = { bldWorkshop, bldGreenShop, bldInn, bldTavern, bldWatchtower, bldStorehouse };
            Sprite[] ruralVariety = { bldPorchHouse, bldStorehouse, bldTavern, bldWorkshop, bldInn };
            Sprite[] palette = isTown ? townVariety : ruralVariety;

            int bgCount = 0;
            int fgCount = 0;
            for (int i = 0; i < activeHouses.Count; i++)
            {
                var hs = activeHouses[i];
                if (hs == null || !hs.gameObject.activeSelf) continue;
                float hx = hs.transform.position.x;
                if (hx > maxX + 18f) continue;

                string hn = hs.gameObject.name;
                if (hn == "CasaDestacada" || hn == "Parroquia") continue;

                if (hn == "CasaFondo")
                {
                    bgCount++;
                    if (bgCount % 2 == 0 && palette.Length > 0)
                    {
                        Sprite rep = palette[(bgCount / 2) % palette.Length];
                        if (rep != null) hs.sprite = rep;
                    }
                }
                else if (hn == "Casa" || hn == "Hacienda")
                {
                    fgCount++;
                    if (fgCount % 3 == 2 && palette.Length > 0)
                    {
                        Sprite rep = palette[(fgCount / 3) % palette.Length];
                        if (rep != null && rep != bldWatchtower) hs.sprite = rep;
                    }
                }
            }

            // 2. Guirnaldas festivas de Papel Picado y Faroles Coloniales con halo cálido en las calles del pueblo (Niveles 1, 3 y 4)
            if (isTown && spPapelPicado != null)
            {
                int garlandIdx = 0;
                for (float gx = 10f; gx < maxX - 12f; gx += 15.5f)
                {
                    float gy;
                    if (!TrySampleSolidGroundTop(gx, groundMask, out gy)) gy = 0f;
                    float hOffset = 5.15f + (garlandIdx % 2) * 0.35f;
                    SpawnSceneryProp(a, root.transform, "GuirnaldaPapelPicado", spPapelPicado,
                        new Vector3(gx, gy + hOffset, 0f), 24, 1.32f, Color.white, garlandIdx % 2 == 1);

                    // Segundo cordón en segundo plano para dar profundidad festiva a la calle
                    if (garlandIdx % 2 == 0)
                    {
                        SpawnSceneryProp(a, root.transform, "GuirnaldaFondo", spPapelPicado,
                            new Vector3(gx + 7.5f, gy + 4.45f, 0f), -28, 1.08f, new Color(0.88f, 0.86f, 0.96f, 0.88f));
                    }
                    garlandIdx++;
                }
            }

            // Añadir halos cálidos a los Faroles existentes y colocar faroles adicionales en zonas oscuras
            if (spLamp != null && isTown)
            {
                for (float lx = 22f; lx < maxX - 18f; lx += 26f)
                {
                    float ly;
                    if (!TrySampleSolidGroundTop(lx, groundMask, out ly)) continue;
                    var lampSr = SpawnSceneryProp(a, root.transform, "FarolColonialExtra", spLamp, new Vector3(lx, ly - 0.03f, 0f), -19, 1.0f, Color.white);
                    if (lampSr != null && a.glow != null)
                    {
                        SpawnSceneryProp(a, lampSr.transform, "HaloFarol", a.glow, new Vector3(lx, ly + 2.45f, 0f), -18, 2.2f, new Color(1f, 0.85f, 0.42f, 0.52f), false, true);
                    }
                }
            }

            // 3. Plaza de Inicio (x = 4..20): Arco de campana colonial, estatua guardián con farol, estandartes y braseros
            {
                float startY;
                if (!TrySampleSolidGroundTop(6.5f, groundMask, out startY)) startY = 0f;
                SpawnSceneryProp(a, root.transform, "ArcoInicioNivel", spArchBell, new Vector3(6.5f, startY - 0.04f, 0f), -26, 1.18f, Color.white);
                SpawnStatueWithLanternGlow(a, root.transform, spStatue, 3.6f, startY, -24, 0.95f, false);
                SpawnBrazierWithGlow(a, root.transform, spBrazier, 9.4f, startY, -22, 0.82f);
                SpawnSceneryProp(a, root.transform, "EstandarteInicio", spFlag, new Vector3(11.2f, startY - 0.03f, 0f), -25, 0.95f, Color.white);
                SpawnSceneryProp(a, root.transform, "ArbustoInicio", spGreenery3, new Vector3(4.8f, startY - 0.04f, 0f), -21, 0.9f, Color.white);
            }

            // 4. Plazas de Mercader (alrededor de cada tienda activa): carreta, mesas, vasijas, estandartes y vegetación
            var shops = FindObjectsByType<ShopPoint>(FindObjectsInactive.Exclude);
            for (int i = 0; i < shops.Length; i++)
            {
                if (shops[i] == null) continue;
                float sx = shops[i].transform.position.x;
                if (sx > maxX) continue;
                float sy;
                if (!TrySampleSolidGroundTop(sx, groundMask, out sy)) sy = shops[i].transform.position.y;

                SpawnSceneryProp(a, root.transform, "CarretaMercado", spCart, new Vector3(sx - 3.6f, sy - 0.03f, 0f), -23, 0.88f, Color.white);
                SpawnSceneryProp(a, root.transform, "MesaMercado", spTable1, new Vector3(sx + 3.4f, sy - 0.03f, 0f), -23, 0.92f, Color.white);
                SpawnSceneryProp(a, root.transform, "VasijaMercado", spBowl, new Vector3(sx + 3.4f, sy + 0.72f, 0f), -22, 0.75f, Color.white);
                SpawnBrazierWithGlow(a, root.transform, spBrazier, sx - 2.1f, sy, -21, 0.80f);
                SpawnBrazierWithGlow(a, root.transform, spBrazier, sx + 2.1f, sy, -21, 0.80f);
                if (spPapelPicado != null)
                {
                    SpawnSceneryProp(a, root.transform, "DoselMercado", spPapelPicado, new Vector3(sx, sy + 4.2f, 0f), 22, 1.25f, Color.white);
                }
            }

            // 5. Monumentos y Ruinas en cada Acertijo (Bandera y Antorchas) y Mini-Jefes
            var hintSigns = FindObjectsByType<PuzzleHintSign>(FindObjectsInactive.Exclude);
            for (int i = 0; i < hintSigns.Length; i++)
            {
                if (hintSigns[i] == null) continue;
                float px = hintSigns[i].transform.position.x;
                float py = hintSigns[i].transform.position.y;
                if (px > maxX + 5f) continue;

                // En los letreros de pista al inicio de cada puzzle, colocar estatua guardián con farol y ruina colonial
                if (hintSigns[i].gameObject.name.Contains("CartelPista"))
                {
                    SpawnStatueWithLanternGlow(a, root.transform, spStatue, px - 1.4f, py, -22, 0.88f, false);
                    SpawnSceneryProp(a, root.transform, "RuinaPuzzle", (i % 2 == 0) ? spRuin1 : spRuin2, new Vector3(px + 4.5f, py - 0.04f, 0f), -27, 0.95f, new Color(0.92f, 0.90f, 0.96f, 1f));
                    SpawnBrazierWithGlow(a, root.transform, spBrazier, px + 0.9f, py, -21, 0.78f);
                }
            }

            var miniBosses = FindObjectsByType<MiniBossGuard>(FindObjectsInactive.Exclude);
            for (int i = 0; i < miniBosses.Length; i++)
            {
                var mb = miniBosses[i];
                if (mb == null || mb.blockingGate == null) continue;
                float gx = mb.blockingGate.transform.position.x;
                float gy = mb.blockingGate.transform.position.y;

                // Enmarcar la puerta del Mini-Jefe con un arco de piedra al fondo, estandartes y braseros
                SpawnSceneryProp(a, root.transform, "ArcoMiniJefe", spArchBell, new Vector3(gx, gy - 0.04f, 0f), -25, 1.15f, new Color(0.92f, 0.88f, 0.86f, 1f));
                SpawnSceneryProp(a, root.transform, "PendonMiniJefeIzq", spBanner1, new Vector3(gx - 1.55f, gy + 2.35f, 0f), -24, 0.92f, Color.white);
                SpawnSceneryProp(a, root.transform, "PendonMiniJefeDer", spBanner2, new Vector3(gx + 1.55f, gy + 2.35f, 0f), -24, 0.92f, Color.white);
                SpawnBrazierWithGlow(a, root.transform, spBrazier, gx - 1.8f, gy, -20, 0.85f);
            }

            // 6. Paisaje creativo a lo largo del recorrido (palmeras, árboles antiguos, ruinas y estatuas en Nivel 2; jardines coloniales en Niveles 1, 3 y 4)
            Sprite[] banners = { spBanner1, spBanner2, spBanner3 };
            for (float wx = 26f; wx < maxX - 35f; wx += 22f)
            {
                float wy;
                if (!TrySampleSolidGroundTop(wx, groundMask, out wy)) continue;
                int stepIdx = Mathf.RoundToInt(wx / 22f);

                if (!isTown)
                {
                    // Nivel 2: Ruta nocturna insurgente con palmeras, árboles secos, ruinas musgosas y estatuas con farol guiando el camino
                    Sprite treeSp = (stepIdx % 3 == 0) ? spPalm1 : ((stepIdx % 3 == 1) ? spPalm2 : ((stepIdx % 2 == 0) ? spDeadTree1 : spDeadTree2));
                    SpawnSceneryProp(a, root.transform, "ArbolCaminoCreativo", treeSp, new Vector3(wx, wy - 0.05f, 0f), -34, 0.92f, new Color(0.84f, 0.88f, 0.94f, 1f), stepIdx % 2 == 1);

                    if (stepIdx % 2 == 0)
                    {
                        SpawnSceneryProp(a, root.transform, "RuinaCamino", (stepIdx % 4 == 0) ? spRuin1 : spRuin2, new Vector3(wx + 6.5f, wy - 0.04f, 0f), -28, 0.95f, Color.white);
                    }
                    else
                    {
                        SpawnStatueWithLanternGlow(a, root.transform, spStatue, wx + 5.5f, wy, -24, 0.88f, stepIdx % 3 == 0);
                    }
                    SpawnSceneryProp(a, root.transform, "PlantaCamino", (stepIdx % 2 == 0) ? spPlant : spGreenery3, new Vector3(wx - 3.2f, wy - 0.03f, 0f), -19, 0.88f, Color.white);
                }
                else
                {
                    // Niveles 1, 3 y 4: Palmeras de plaza colonial, jardines con plantas, estandartes en fachadas y braseros
                    if (stepIdx % 2 == 0)
                    {
                        SpawnSceneryProp(a, root.transform, "PalmeraPlaza", (stepIdx % 4 == 0) ? spPalm1 : spPalm2,
                            new Vector3(wx, wy - 0.05f, 0f), -42, 0.78f, new Color(0.90f, 0.92f, 0.98f, 1f), stepIdx % 3 == 0);
                    }
                    Sprite bSp = banners[stepIdx % banners.Length];
                    SpawnSceneryProp(a, root.transform, "PendonColonial", bSp, new Vector3(wx + 4.2f, wy + 2.4f, 0f), -35, 0.88f, Color.white);
                    SpawnSceneryProp(a, root.transform, "JardineraColonial", (stepIdx % 2 == 0) ? spPlant : spGreenery3,
                        new Vector3(wx + 2.4f, wy - 0.03f, 0f), -19, 0.88f, Color.white);
                }
            }

            // 7. Gran Plaza de la Arena del Jefe y Monumento Final
            var boss = FindAnyObjectByType<BossKnight>();
            if (boss != null)
            {
                float bx = boss.transform.position.x;
                float by;
                if (!TrySampleSolidGroundTop(bx, groundMask, out by)) by = 0f;

                // Torres vigía gemelas y arcos de piedra flanqueando la arena del jefe final
                SpawnSceneryProp(a, root.transform, "TorreVigiaArenaIzq", bldWatchtower, new Vector3(bx - 16f, by - 0.05f, 0f), -37, 0.74f, new Color(0.90f, 0.88f, 0.95f, 1f));
                SpawnSceneryProp(a, root.transform, "TorreVigiaArenaDer", bldWatchtower, new Vector3(bx + 15f, by - 0.05f, 0f), -37, 0.74f, new Color(0.90f, 0.88f, 0.95f, 1f), true);
                SpawnSceneryProp(a, root.transform, "ArcoCentroArena", spArchBell, new Vector3(bx, by - 0.04f, 0f), -29, 1.35f, new Color(0.88f, 0.84f, 0.84f, 1f));
                SpawnStatueWithLanternGlow(a, root.transform, spStatue, bx - 9.5f, by, -25, 1.0f, false);
                SpawnStatueWithLanternGlow(a, root.transform, spStatue, bx + 9.5f, by, -25, 1.0f, true);
                SpawnBrazierWithGlow(a, root.transform, spBrazier, bx - 5.5f, by, -22, 0.90f);
                SpawnBrazierWithGlow(a, root.transform, spBrazier, bx + 5.5f, by, -22, 0.90f);
            }

            // Flanquear el Arco Final / Campana de Dolores con arco de piedra, estatuas y braseros
            float endY;
            if (!TrySampleSolidGroundTop(maxX, groundMask, out endY)) endY = 0f;
            SpawnSceneryProp(a, root.transform, "ArcoPiedraMeta", spArchBell, new Vector3(maxX, endY - 0.04f, 0f), -26, 1.30f, Color.white);
            SpawnStatueWithLanternGlow(a, root.transform, spStatue, maxX - 3.6f, endY, -23, 0.96f, false);
            SpawnStatueWithLanternGlow(a, root.transform, spStatue, maxX + 3.6f, endY, -23, 0.96f, true);
            SpawnBrazierWithGlow(a, root.transform, spBrazier, maxX - 2.6f, endY, -21, 0.88f);
            SpawnBrazierWithGlow(a, root.transform, spBrazier, maxX + 2.6f, endY, -21, 0.88f);
        }

        void ClearZoneRange(float xMin, float xMax)
        {
            var hazards = FindObjectsByType<Hazard>(FindObjectsInactive.Include);
            for (int i = 0; i < hazards.Length; i++)
            {
                if (hazards[i] != null && hazards[i].transform.position.x >= xMin && hazards[i].transform.position.x <= xMax)
                    hazards[i].gameObject.SetActive(false);
            }
            var knights = FindObjectsByType<EnemyKnight>(FindObjectsInactive.Include);
            for (int i = 0; i < knights.Length; i++)
            {
                if (knights[i] != null && knights[i].GetComponent<MiniBossGuard>() == null &&
                    knights[i].transform.position.x >= xMin && knights[i].transform.position.x <= xMax)
                    knights[i].gameObject.SetActive(false);
            }
            var breakables = FindObjectsByType<Breakable>(FindObjectsInactive.Include);
            for (int i = 0; i < breakables.Length; i++)
            {
                if (breakables[i] != null && breakables[i].transform.position.x >= xMin && breakables[i].transform.position.x <= xMax)
                    breakables[i].gameObject.SetActive(false);
            }
        }

        void FinalizeLevelBounds(GameSession gs, Assets a, float endX, int breakTarget, int killTarget, int chestTarget, int scrollTarget)
        {
            // Ajustar cámara y ciclo día/noche a la nueva longitud compacta
            if (gs.cam != null) gs.cam.maxX = endX + 8f;
            var dnc = FindAnyObjectByType<DayNightCycle>();
            if (dnc != null) dnc.endX = Mathf.Max(80f, endX - 15f);

            // Muro límite final detrás de la meta
            var endWall = new GameObject("MuroFinalNivel");
            endWall.layer = a.groundLayer;
            endWall.transform.SetParent(transform);
            endWall.transform.position = new Vector3(endX + 7f, 8f, 0f);
            var wbc = endWall.AddComponent<BoxCollider2D>();
            wbc.size = new Vector2(1.2f, 30f);

            // Reubicar pergaminos y cofres que hayan quedado más allá del nuevo final para que puedan recogerse
            float[] scrollSafeX = { 42f, 132f, 198f };
            var scrolls = FindObjectsByType<Collectible>(FindObjectsInactive.Include);
            int sIdx = 0;
            for (int i = 0; i < scrolls.Length; i++)
            {
                if (scrolls[i] != null && scrolls[i].transform.position.x > endX - 25f)
                {
                    float nx = scrollSafeX[sIdx % scrollSafeX.Length] + sIdx * 3f;
                    scrolls[i].transform.position = new Vector3(nx, 1.35f, 0f);
                    sIdx++;
                }
            }

            float[] chestSafeX = { 50f, 156f, 200f };
            var chests = FindObjectsByType<TreasureChest>(FindObjectsInactive.Include);
            int cIdx = 0;
            for (int i = 0; i < chests.Length; i++)
            {
                if (chests[i] != null && chests[i].transform.position.x > endX - 25f)
                {
                    float nx = chestSafeX[cIdx % chestSafeX.Length] + cIdx * 3f;
                    chests[i].transform.position = new Vector3(nx, 0f, 0f);
                    cIdx++;
                }
            }

            // Desactivar enemigos, rejas y palancas antiguas que no pertenezcan a este gestor o estén más allá del nuevo final
            var knights = FindObjectsByType<EnemyKnight>(FindObjectsInactive.Include);
            for (int i = 0; i < knights.Length; i++)
            {
                if (knights[i] != null && knights[i].transform.position.x > endX - 10f)
                    knights[i].gameObject.SetActive(false);
            }
            var oldGates = FindObjectsByType<Gate>(FindObjectsInactive.Include);
            for (int i = 0; i < oldGates.Length; i++)
            {
                if (oldGates[i] != null && !oldGates[i].transform.IsChildOf(transform))
                    oldGates[i].gameObject.SetActive(false);
            }
            var oldLevers = FindObjectsByType<GateLever>(FindObjectsInactive.Include);
            for (int i = 0; i < oldLevers.Length; i++)
            {
                if (oldLevers[i] != null && !oldLevers[i].transform.IsChildOf(transform))
                    oldLevers[i].gameObject.SetActive(false);
            }

            // Ajustar metas de las misiones a la nueva duración compacta del nivel
            if (gs.missions != null)
            {
                for (int i = 0; i < gs.missions.Length; i++)
                {
                    var m = gs.missions[i];
                    if (m == null) continue;
                    switch (m.kind)
                    {
                        case MissionKind.Break:
                            m.target = breakTarget;
                            m.text = "Rompe " + breakTarget + " cajas o barriles";
                            break;
                        case MissionKind.Kills:
                            m.target = killTarget;
                            m.text = "Derrota " + killTarget + " soldados";
                            break;
                        case MissionKind.Chests:
                            m.target = chestTarget;
                            m.text = "Abre " + chestTarget + " cofres";
                            break;
                        case MissionKind.Collect:
                            m.target = scrollTarget;
                            m.text = scrollTarget == 1 ? "Encuentra 1 pergamino histórico" : ("Encuentra " + scrollTarget + " pergaminos históricos");
                            break;
                    }
                }
                gs.RaiseStats();
            }
        }

        void SpawnFinaleMonument(Assets a, float x, float y, string bannerHint)
        {
            var arch = new GameObject("ArcoFinalNivel");
            arch.transform.SetParent(transform);
            arch.transform.position = new Vector3(x, y, 0f);

            // Columnas laterales del arco final
            for (int side = -1; side <= 1; side += 2)
            {
                var col = new GameObject("Columna_" + side);
                col.transform.SetParent(arch.transform, false);
                col.transform.localPosition = new Vector3(side * 2.1f, 2.1f, 0f);
                var csr = col.AddComponent<SpriteRenderer>();
                csr.sprite = a.platform;
                csr.color = new Color(0.52f, 0.38f, 0.24f, 1f);
                csr.sortingOrder = -6;
                if (a.litMat != null) csr.sharedMaterial = a.litMat;
                csr.drawMode = SpriteDrawMode.Sliced;
                csr.size = new Vector2(0.55f, 4.2f);

                var glow = new GameObject("LuzFinal_" + side);
                glow.transform.SetParent(arch.transform, false);
                glow.transform.localPosition = new Vector3(side * 2.1f, 3.8f, 0f);
                var gsr = glow.AddComponent<SpriteRenderer>();
                gsr.sprite = a.glow;
                gsr.color = new Color(1f, 0.85f, 0.35f, 0.85f);
                gsr.sortingOrder = 14;
                if (a.unlitMat != null) gsr.sharedMaterial = a.unlitMat;
                glow.transform.localScale = Vector3.one * 2.4f;
            }

            // Dintel superior del Arco Triunfal
            var top = new GameObject("DintelFinal");
            top.transform.SetParent(arch.transform, false);
            top.transform.localPosition = new Vector3(0f, 4.3f, 0f);
            var tsr = top.AddComponent<SpriteRenderer>();
            tsr.sprite = a.platform;
            tsr.color = new Color(0.75f, 0.56f, 0.25f, 1f);
            tsr.sortingOrder = -5;
            if (a.litMat != null) tsr.sharedMaterial = a.litMat;
            tsr.drawMode = SpriteDrawMode.Tiled;
            tsr.tileMode = SpriteTileMode.Continuous;
            tsr.size = new Vector2(5.0f, 0.48f);

            // Bandera Mexicana coronando el Arco Final (Verde, Blanco con escudo, Rojo)
            for (int i = 0; i < 3; i++)
            {
                var fs = new GameObject("BanderaFinal_" + i);
                fs.transform.SetParent(arch.transform, false);
                fs.transform.localPosition = new Vector3((i - 1) * 0.82f, 5.15f, 0f);
                var fsr = fs.AddComponent<SpriteRenderer>();
                fsr.sprite = a.flagStripes[i];
                fsr.sortingOrder = 15;
                if (a.unlitMat != null) fsr.sharedMaterial = a.unlitMat;
                fs.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            }

            SpawnClueSign(a, x - 2.6f, y, bannerHint);
        }

        void SpawnCheckpointAt(Assets a, float x, float y, int index)
        {
            var root = new GameObject("Banderines_Extra_" + index);
            root.transform.SetParent(transform);
            root.transform.position = new Vector3(x, y, 0f);
            var flagGo = new GameObject("Banderines");
            flagGo.transform.SetParent(root.transform, false);
            flagGo.transform.localPosition = new Vector3(0f, -0.02f, 0f);
            var flag = flagGo.AddComponent<SpriteRenderer>();
            flag.sprite = a.flagCheckpoint;
            flag.sortingOrder = -8;
            if (a.litMat != null) flag.sharedMaterial = a.litMat;

            var glowGo = new GameObject("Brillo");
            glowGo.transform.SetParent(root.transform, false);
            glowGo.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            var glow = glowGo.AddComponent<SpriteRenderer>();
            glow.sprite = a.glow;
            glow.color = new Color(1f, 0.9f, 0.5f, 0f);
            glow.sortingOrder = -7;
            if (a.unlitMat != null) glow.sharedMaterial = a.unlitMat;
            glowGo.transform.localScale = Vector3.one * 3.2f;

            var spawn = new GameObject("Reaparicion");
            spawn.transform.SetParent(root.transform, false);
            spawn.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            var bc = root.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(3.2f, 4f);
            bc.offset = new Vector2(0f, 2f);
            var cp = root.AddComponent<Checkpoint>();
            cp.index = index;
            cp.flag = flag;
            cp.glow = glow;
            cp.spawn = spawn.transform;
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
        /// <summary>Reja de entrada de arena de jefe: permanece oculta mientras está abierta y solo baja al iniciar el combate.</summary>
        Gate SpawnArenaTrapGate(Assets a, string doorName, float x, float y)
        {
            var gateGo = new GameObject(doorName);
            gateGo.layer = a.groundLayer;
            gateGo.transform.SetParent(transform);
            gateGo.transform.position = new Vector3(x, y + 2.2f, 0f);

            var doorVis = new GameObject("HojaPuerta");
            doorVis.transform.SetParent(gateGo.transform, false);
            var dsr = doorVis.AddComponent<SpriteRenderer>();
            dsr.sprite = a.doorSprite;
            dsr.sortingOrder = 7;
            if (a.litMat != null) dsr.sharedMaterial = a.litMat;

            var gateCol = gateGo.AddComponent<BoxCollider2D>();
            gateCol.size = new Vector2(1.0f, 4.4f);

            var gate = gateGo.AddComponent<Gate>();
            gate.openOffset = new Vector3(0f, 4.6f, 0f);
            gate.startOpen = true;
            gate.hideWhenOpen = true;
            gate.blocker = gateCol;
            gate.Init();
            return gate;
        }

        /// <summary>Puerta colonial sólida y siempre cerrada al inicio (para puzzles, mini-jefes y salida de jefe).</summary>
        Gate SpawnBlockingDoor(Assets a, string doorName, float x, float y, bool startOpen, string closedHint)
        {
            var doorRoot = new GameObject("Marco_" + doorName);
            doorRoot.transform.SetParent(transform);
            doorRoot.transform.position = new Vector3(x, y, 0f);

            // Dintel superior decorativo del marco de la puerta (sin colisionador superior para no bloquear escaleras ni plataformas)
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
            hsr.size = new Vector2(1.7f, 0.45f);

            // Hoja sólida de la puerta que llega hasta el suelo (y = 0 .. 4.4m) y desaparece al abrirse
            var gateGo = new GameObject(doorName);
            gateGo.layer = a.groundLayer;
            gateGo.transform.SetParent(doorRoot.transform, false);
            gateGo.transform.localPosition = new Vector3(0f, 2.2f, 0f);

            var doorVis = new GameObject("HojaPuerta");
            doorVis.transform.SetParent(gateGo.transform, false);
            doorVis.transform.localPosition = Vector3.zero;
            var dsr = doorVis.AddComponent<SpriteRenderer>();
            dsr.sprite = a.doorSprite;
            dsr.sortingOrder = 7;
            if (a.litMat != null) dsr.sharedMaterial = a.litMat;

            var gateCol = gateGo.AddComponent<BoxCollider2D>();
            gateCol.size = new Vector2(1.05f, 4.4f);

            var gate = gateGo.AddComponent<Gate>();
            gate.openOffset = new Vector3(0f, 4.6f, 0f);
            gate.startOpen = false;
            gate.hideWhenOpen = true;
            gate.blocker = gateCol;
            gate.statusLight = null;
            gate.Init();

            if (!string.IsNullOrEmpty(closedHint))
            {
                var hintGo = new GameObject("AvisoPuerta");
                hintGo.transform.SetParent(doorRoot.transform, false);
                hintGo.transform.localPosition = new Vector3(-0.75f, 1.1f, 0f);
                var hbc = hintGo.AddComponent<BoxCollider2D>();
                hbc.isTrigger = true;
                hbc.size = new Vector2(1.5f, 2.4f);
                var phs = hintGo.AddComponent<PuzzleHintSign>();
                phs.hintText = closedHint;
                phs.linkedGate = gate;
            }

            return gate;
        }

        Gate SpawnPuzzleDoorAndReward(GameSession gs, Assets a, string chestId, float doorX, float y, int chestCoins, bool withHeart, string closedHint)
        {
            Gate gate = SpawnBlockingDoor(a, "PuertaPuzzle_" + chestId, doorX, y, false, closedHint);

            var chGo = new GameObject("CofrePuzzle_" + chestId);
            chGo.transform.SetParent(transform);
            chGo.transform.position = new Vector3(doorX + 2.0f, y, 0f);
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
                hGo.transform.position = new Vector3(doorX + 3.1f, y + 0.75f, 0f);
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
            ClearZoneRange(startX - 1f, startX + 12f);
            SpawnClueSign(a, startX, y, "Acertijo de las Antorchas: Enciende las 3 antorchas con E para abrir la puerta");

            var ctrlGo = new GameObject("PuzzleAntorchas_" + id);
            ctrlGo.transform.SetParent(transform);
            var ctrl = ctrlGo.AddComponent<LockGatePuzzle>();
            ctrl.bonusCoins = coins;
            ctrl.torches = new bool[] { false, false, false };
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
            ClearZoneRange(startX - 1f, startX + 12f);
            SpawnClueSign(a, startX, y, "Acertijo de la Bandera (E): Iza en orden 1° Verde → 2° Blanco → 3° Rojo para abrir la puerta");

            var ctrlGo = new GameObject("PuzzleBandera_" + id);
            ctrlGo.transform.SetParent(transform);
            var ctrl = ctrlGo.AddComponent<SequencePuzzleController>();
            ctrl.bonusCoins = coins;

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

                var baseGo = new GameObject("BaseColor");
                baseGo.transform.SetParent(pGo.transform, false);
                baseGo.transform.localPosition = new Vector3(0f, 0.25f, 0f);
                var baseSr = baseGo.AddComponent<SpriteRenderer>();
                baseSr.sprite = a.flagStripes[i];
                baseSr.sortingOrder = -3;
                if (a.unlitMat != null) baseSr.sharedMaterial = a.unlitMat;
                baseGo.transform.localScale = new Vector3(0.75f, 0.35f, 1f);

                var bannerGo = new GameObject("FranjaBandera_" + names[i]);
                bannerGo.transform.SetParent(pGo.transform, false);
                bannerGo.transform.localPosition = new Vector3(0f, 0.95f, 0f);
                var bannerSr = bannerGo.AddComponent<SpriteRenderer>();
                bannerSr.sprite = a.flagStripes[i];
                bannerSr.sortingOrder = 11;
                if (a.unlitMat != null) bannerSr.sharedMaterial = a.unlitMat;

                var doorStripeGo = new GameObject("FranjaPuerta_" + names[i]);
                doorStripeGo.transform.SetParent(flagBoard.transform, false);
                doorStripeGo.transform.localPosition = new Vector3((i - 1) * 0.78f, 0f, 0f);
                var doorStripeSr = doorStripeGo.AddComponent<SpriteRenderer>();
                doorStripeSr.sprite = a.flagStripes[i];
                doorStripeSr.sortingOrder = 14;
                if (a.unlitMat != null) doorStripeSr.sharedMaterial = a.unlitMat;
                doorStripeGo.transform.localScale = new Vector3(0.76f, 0.76f, 1f);

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
                node.orderId = i;
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

        // ------------------------------------------------------------------ mini-jefes
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
            ClearZoneRange(gateX - 8f, gateX + 3f);
            var tmpl = FindTemplateKnight();
            if (tmpl == null) return;

            var cfg = ScriptableObject.CreateInstance<EnemyConfig>();
            cfg.displayName = title;
            cfg.tier = EnemyTier.MuyResistente;
            cfg.skin = tmpl.config != null ? tmpl.config.skin : 2;
            cfg.tint = tint;
            cfg.scale = 1.34f;
            cfg.hitsToKill = Mathf.Max(12, hitsToKill);
            cfg.heartsDamage = heartsDamage;
            cfg.patrolSpeed = 1.2f;
            cfg.chaseSpeed = 3.0f;
            cfg.aggroRange = 6.2f;
            cfg.loseRange = 9.5f;
            cfg.attackRange = 2.4f;
            cfg.attackCooldown = 1.45f;
            cfg.staggerChance = 0.15f;
            cfg.hitBoxSize = new Vector2(2.4f, 1.5f);
            cfg.hitBoxOffset = new Vector2(1.5f, 0.95f);
            cfg.colliderWidth = 1.1f;
            cfg.colliderHeight = 2.0f;
            cfg.coinReward = coinReward;
            cfg.scoreValue = 400;
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
            ek.patrolRadius = 0.9f;
            ek.visual = sr;
            ek.animator = an;
            ek.idle = tmpl.idle;
            ek.walk = tmpl.walk;
            ek.run = tmpl.run;
            ek.attack = tmpl.attack;
            ek.hurt = tmpl.hurt;
            ek.die = tmpl.die;

            var bar = new GameObject("BarraVida");
            bar.transform.SetParent(root.transform, false);
            bar.transform.localPosition = new Vector3(-0.85f, cfg.colliderHeight * cfg.scale + 0.55f, 0f);
            var back = new GameObject("Fondo");
            back.transform.SetParent(bar.transform, false);
            back.transform.localPosition = new Vector3(-0.04f, 0f, 0f);
            var bsr = back.AddComponent<SpriteRenderer>();
            bsr.sprite = a.pixel;
            bsr.color = new Color(0.08f, 0.03f, 0.03f, 0.94f);
            bsr.sortingOrder = 40;
            if (a.unlitMat != null) bsr.sharedMaterial = a.unlitMat;
            back.transform.localScale = new Vector3(1.78f, 0.24f, 1f);

            var fill = new GameObject("Vida");
            fill.transform.SetParent(bar.transform, false);
            var fsr = fill.AddComponent<SpriteRenderer>();
            fsr.sprite = a.pixel;
            fsr.color = new Color(0.95f, 0.72f, 0.2f, 1f);
            fsr.sortingOrder = 41;
            if (a.unlitMat != null) fsr.sharedMaterial = a.unlitMat;
            fill.transform.localScale = new Vector3(1.7f, 0.18f, 1f);

            var lblGo = new GameObject("NombreMiniJefe");
            lblGo.transform.SetParent(bar.transform, false);
            lblGo.transform.localPosition = new Vector3(0.85f, 0.20f, 0f);
            var tm = lblGo.AddComponent<TextMesh>();
            tm.text = title;
            tm.characterSize = 0.095f;
            tm.fontSize = 36;
            tm.anchor = TextAnchor.LowerCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.color = new Color(1f, 0.9f, 0.45f, 1f);
            var mr = lblGo.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 42;

            ek.hpBar = bar.transform;
            ek.hpFill = fsr;
            ek.hpBarWidth = 1.7f;
            ek.InitConfig();
            bar.SetActive(true);

            Gate gate = SpawnBlockingDoor(a, "PuertaMiniJefe_" + title, gateX, y, false,
                "¡Puerta cerrada! Derrota al Mini-Jefe (" + title + ") para abrir el paso");

            var guard = root.AddComponent<MiniBossGuard>();
            guard.knight = ek;
            guard.blockingGate = gate;
            guard.miniBossTitle = title;
            guard.bonusCoins = coinReward;
        }
    }
}
