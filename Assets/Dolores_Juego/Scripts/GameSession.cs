using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dolores1810
{
    public enum GameState { Playing, Paused, PlayerDead, GameOver, Victory, Cinematic, Shop }

    public enum MissionKind { Break, Kills, Chests, Collect }

    [System.Serializable]
    public class Mission
    {
        public string id = "m";
        public string text = "";
        public MissionKind kind = MissionKind.Break;
        public int target = 5;
        public int reward = 30;
        [System.NonSerialized] public int progress;
        [System.NonSerialized] public bool done;
    }

    /// <summary>
    /// Control de un nivel: vidas, puntos, misiones, objetivo actual, checkpoints, pausa, tienda, jefe, salida al
    /// siguiente nivel y final del juego. Las monedas viven en PlayerProgress (se conservan entre niveles).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameSession : MonoBehaviour
    {
        public static GameSession I;

        [Header("Referencias")]
        public PlatformerPlayer player;
        public FollowCamera cam;
        public Transform startPoint;

        [Header("Nivel")]
        public int levelIndex = 1;
        public string levelTitle = "";
        public string levelSubtitle = "";
        public string startObjective = "";
        public Mission[] missions;

        [Header("Reglas")]
        public int startLives = 3;
        public string menuScene = SceneNames.Menu;

        [Header("Música del nivel")]
        public AudioClip exploreClip;
        public AudioClip combatClip;
        public AudioClip bossClip;
        public AudioClip victoryClip;
        public AudioClip finalClip;

        [Header("Recursos para objetos que aparecen en juego")]
        public Sprite sparkSprite;
        public Sprite dustSprite;
        public Sprite confettiSprite;
        public Sprite[] coinSprites;
        public Sprite appleSprite;
        public Sprite heartSprite;
        public Material litMaterial;
        public Material unlitMaterial;

        public GameState State { get; private set; }
        public int Lives { get; private set; }
        public int Score { get; private set; }
        public int CoinsThisLevel { get; private set; }
        public int Kills { get; private set; }
        public float Elapsed { get; private set; }
        public int CheckpointIndex { get; private set; }
        public string Objective { get; private set; }
        public string Prompt { get; private set; }
        public bool BossActive { get; private set; }
        public bool BossDefeated { get; private set; }
        public string FinalSummary { get; private set; }

        public event System.Action StatsChanged;
        public event System.Action StateChanged;
        public event System.Action<string, string> ZoneShown;
        public event System.Action<string, float> ToastShown;
        public event System.Action DamageFlashed;
        public event System.Action ObjectiveChanged;
        public event System.Action PromptChanged;
        public event System.Action<string, int> BossStartedEvent;
        public event System.Action<int, int> BossHealthEvent;
        public event System.Action BossEndedEvent;
        public event System.Action<string, string> IntroShown;
        public event System.Action IntroEnded;
        public event System.Action<string> ExitShown;
        public event System.Action<string> MissionDone;
        public event System.Action Respawned;

        Vector3 respawnPos;
        float autosave;
        bool exiting;

        public bool IsLastLevel { get { return levelIndex >= 3; } }

        void Awake()
        {
            I = this;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            State = GameState.Playing;
            CheckpointIndex = -1;
            Fx.Spark = sparkSprite;
            Fx.Dust = dustSprite;
            Fx.Confetti = confettiSprite;
            Fx.UnlitMat = unlitMaterial;
            CombatTracker.Reset();
            GameSettings.Apply();
            int pl = LayerMask.NameToLayer("Player");
            int en = LayerMask.NameToLayer("Enemy");
            if (pl >= 0) Physics2D.IgnoreLayerCollision(pl, pl, true);
            if (pl >= 0 && en >= 0)
            {
                Physics2D.IgnoreLayerCollision(pl, en, true);
                Physics2D.IgnoreLayerCollision(en, en, true);
            }
            if (missions != null)
            {
                for (int i = 0; i < missions.Length; i++) { missions[i].progress = 0; missions[i].done = false; }
            }
            Objective = startObjective;
        }

        void Start()
        {
            Lives = startLives;
            if (startPoint != null) respawnPos = startPoint.position;
            else if (player != null) respawnPos = player.transform.position;
            PlayerProgress.MarkStarted(levelIndex);
            var mm = MusicManager.Ensure();
            mm.SetSet(null, exploreClip, combatClip, bossClip, victoryClip, finalClip);
            mm.RegisterGameplay(true);
            RaiseStats();
            StartCoroutine(IntroRoutine());
        }

        void OnDestroy()
        {
            if (I == this) I = null;
            Time.timeScale = 1f;
            var mm = MusicManager.Existing;
            if (mm != null) { mm.RegisterGameplay(false); mm.Duck(1f); }
            PlayerProgress.SaveNow();
        }

        // ------------------------------------------------------------------ bucle
        void Update()
        {
            if (State == GameState.Playing || State == GameState.PlayerDead) Elapsed += Time.deltaTime;

            autosave += Time.unscaledDeltaTime;
            if (autosave > 20f) { autosave = 0f; PlayerProgress.SaveNow(); }

            if (GameInput.PauseDown())
            {
                if (State == GameState.Playing) SetPaused(true);
                else if (State == GameState.Paused) SetPaused(false);
                else if (State == GameState.Shop) CloseShop();
            }
        }

        IEnumerator IntroRoutine()
        {
            State = GameState.Cinematic;
            if (player != null) player.BlockInput(0.1f);
            if (IntroShown != null) IntroShown(levelTitle, levelSubtitle);
            if (StateChanged != null) StateChanged();
            float t = 0f;
            while (t < 4.2f)
            {
                t += Time.unscaledDeltaTime;
                if (t > 1.0f && GameInput.AnyKeyDown()) break;
                yield return null;
            }
            State = GameState.Playing;
            if (IntroEnded != null) IntroEnded();
            if (StateChanged != null) StateChanged();
            if (!string.IsNullOrEmpty(Objective) && ObjectiveChanged != null) ObjectiveChanged();
            if (player != null) player.BlockInput(0.2f);
        }

        // ------------------------------------------------------------------ pausa y tienda
        public void SetPaused(bool paused)
        {
            if (paused && State != GameState.Playing) return;
            if (!paused && State != GameState.Paused) return;
            State = paused ? GameState.Paused : GameState.Playing;
            Time.timeScale = paused ? 0f : 1f;
            MusicManager.Ensure().Duck(paused ? 0.35f : 1f);
            AudioHub.Play(SfxId.Pause, 0f);
            if (paused) PlayerProgress.SaveNow();
            if (!paused && player != null) player.BlockInput(0.25f);
            if (StateChanged != null) StateChanged();
        }

        public void OpenShop()
        {
            if (State != GameState.Playing) return;
            State = GameState.Shop;
            Time.timeScale = 0f;
            MusicManager.Ensure().Duck(0.4f);
            SetPrompt(null);
            if (StateChanged != null) StateChanged();
        }

        public void CloseShop()
        {
            if (State != GameState.Shop) return;
            State = GameState.Playing;
            Time.timeScale = 1f;
            MusicManager.Ensure().Duck(1f);
            if (player != null) { player.BlockInput(0.25f); player.RefreshStats(true); }
            PlayerProgress.SaveNow();
            if (StateChanged != null) StateChanged();
        }

        // ------------------------------------------------------------------ estadísticas
        public void RaiseStats() { if (StatsChanged != null) StatsChanged(); }

        public void AddScore(int amount) { Score += amount; RaiseStats(); }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            CoinsThisLevel += amount;
            Score += 10 * amount;
            PlayerProgress.AddCoins(amount);
            RaiseStats();
        }

        public void AddLife()
        {
            Lives = Mathf.Min(GameRules.MaxLives, Lives + 1);
            RaiseStats();
        }

        public void RegisterKill(int score)
        {
            Kills++;
            Score += score;
            PlayerProgress.AddKills(1);
            Progress(MissionKind.Kills);
            RaiseStats();
        }

        public void SetCheckpoint(Vector3 pos, int index)
        {
            respawnPos = pos;
            CheckpointIndex = index;
            ShowToast("¡Punto de control!");
        }

        public void ShowZone(string title, string subtitle) { if (ZoneShown != null) ZoneShown(title, subtitle); }
        public void ShowToast(string msg) { ShowToast(msg, 2.6f); }
        public void ShowToast(string msg, float seconds) { if (ToastShown != null) ToastShown(msg, seconds); }
        public void FlashDamage() { if (DamageFlashed != null) DamageFlashed(); }

        public void SetObjective(string text)
        {
            if (Objective == text) return;
            Objective = text;
            if (ObjectiveChanged != null) ObjectiveChanged();
        }

        public void SetPrompt(string text)
        {
            if (Prompt == text) return;
            Prompt = text;
            if (PromptChanged != null) PromptChanged();
        }

        // ------------------------------------------------------------------ misiones
        void Progress(MissionKind kind)
        {
            if (missions == null) return;
            for (int i = 0; i < missions.Length; i++)
            {
                var m = missions[i];
                if (m.kind != kind || m.done) continue;
                m.progress++;
                if (m.progress >= m.target)
                {
                    m.done = true;
                    string key = "L" + levelIndex + "_" + m.id;
                    if (!PlayerProgress.IsMissionDone(key))
                    {
                        PlayerProgress.MarkMissionDone(key);
                        AddCoins(m.reward);
                        ShowToast("Misión cumplida: " + m.text + " (+" + m.reward + " monedas)", 4f);
                        AudioHub.Play(SfxId.Secret, 0f);
                    }
                    else ShowToast("Misión cumplida: " + m.text, 3f);
                    if (MissionDone != null) MissionDone(m.text);
                }
            }
            RaiseStats();
        }

        public void NotifyBreak() { Progress(MissionKind.Break); }
        public void NotifyChest(bool hidden) { Progress(MissionKind.Chests); }
        public void NotifyCollectible() { Progress(MissionKind.Collect); }
        public void NotifySecret() { RaiseStats(); }
        public void NotifyBossDefeated() { BossDefeated = true; RaiseStats(); }

        public string MissionLines()
        {
            if (missions == null || missions.Length == 0) return "";
            var sb = new StringBuilder();
            for (int i = 0; i < missions.Length; i++)
            {
                var m = missions[i];
                sb.Append(m.done ? "[x] " : "[ ] ").Append(m.text);
                if (!m.done) sb.Append("  ").Append(m.progress).Append("/").Append(m.target);
                if (i < missions.Length - 1) sb.Append("\n");
            }
            return sb.ToString();
        }

        public int MissionsDoneCount()
        {
            int n = 0;
            if (missions != null) for (int i = 0; i < missions.Length; i++) if (missions[i].done) n++;
            return n;
        }

        // ------------------------------------------------------------------ jefe
        public void BossStarted(string name, int maxHp)
        {
            BossActive = true;
            SetObjective("Derrota a " + name);
            if (BossStartedEvent != null) BossStartedEvent(name, maxHp);
        }
        public void BossHealth(int cur, int max) { if (BossHealthEvent != null) BossHealthEvent(cur, max); }
        public void BossEnded()
        {
            BossActive = false;
            if (BossEndedEvent != null) BossEndedEvent();
        }

        // ------------------------------------------------------------------ vidas
        public void OnPlayerDied()
        {
            if (State == GameState.Victory || State == GameState.Cinematic && exiting) return;
            Lives--;
            State = GameState.PlayerDead;
            RaiseStats();
            if (StateChanged != null) StateChanged();
            StartCoroutine(DeathRoutine());
        }

        IEnumerator DeathRoutine()
        {
            yield return new WaitForSeconds(1.9f);
            if (Lives > 0)
            {
                State = GameState.Playing;
                player.Respawn(respawnPos);
                if (Respawned != null) Respawned();
                ShowToast(Lives == 1 ? "¡Última vida!" : "Vidas restantes: " + Lives);
                if (StateChanged != null) StateChanged();
            }
            else
            {
                State = GameState.GameOver;
                MusicManager.Ensure().StopAll(0.8f);
                AudioHub.Play(SfxId.GameOver, 0f);
                if (StateChanged != null) StateChanged();
            }
        }

        // ------------------------------------------------------------------ salida de nivel
        public void BeginExit(string nextScene, string text)
        {
            if (State != GameState.Playing || exiting) return;
            exiting = true;
            StartCoroutine(ExitRoutine(nextScene, text));
        }

        IEnumerator ExitRoutine(string nextScene, string text)
        {
            State = GameState.Cinematic;
            SetPrompt(null);
            if (StateChanged != null) StateChanged();
            AudioHub.Play(SfxId.LevelComplete, 0f);
            if (player != null) player.AutoWalk(1f);
            if (ExitShown != null) ExitShown(text);
            float t = 0f;
            while (t < 3.4f) { t += Time.unscaledDeltaTime; yield return null; }
            PlayerProgress.CompleteLevel(levelIndex);
            LoadScene(nextScene);
        }

        // ------------------------------------------------------------------ final del juego
        public void StartFinal(Transform bell)
        {
            if (State != GameState.Playing || exiting) return;
            exiting = true;
            StartCoroutine(FinalRoutine(bell));
        }

        IEnumerator FinalRoutine(Transform bell)
        {
            State = GameState.Cinematic;
            SetPrompt(null);
            if (StateChanged != null) StateChanged();
            if (player != null) player.StopAutoWalk();
            MusicManager.Ensure().SetMood(Mood.Final);
            AudioHub.Play(SfxId.Bell, 0f);
            if (cam != null) cam.FocusOn(bell, 3.3f);
            ShowToast("¡La campana de Dolores ha sonado!", 5f);
            float t = 0f;
            float nextFw = 0.6f;
            while (t < 8.5f)
            {
                t += Time.unscaledDeltaTime;
                if (t > nextFw)
                {
                    nextFw += Random.Range(0.35f, 0.8f);
                    Vector3 c = bell.position + new Vector3(Random.Range(-8f, 8f), Random.Range(2.5f, 6f), 0f);
                    Fx.Firework(c);
                    AudioHub.Play(SfxId.Firework, 0.1f, 0.7f);
                    if (Random.value < 0.4f) Fx.ConfettiBurst(c, 14);
                }
                yield return null;
            }
            PlayerProgress.CompleteLevel(levelIndex);
            PlayerProgress.SaveNow();
            State = GameState.Victory;
            if (StateChanged != null) StateChanged();
        }

        // ------------------------------------------------------------------ escenas
        public void Restart() { LoadScene(SceneManager.GetActiveScene().name); }
        public void GoMenu() { LoadScene(menuScene); }

        public void LoadScene(string sceneName)
        {
            Time.timeScale = 1f;
            PlayerProgress.SaveNow();
            if (GameUI.I != null) GameUI.I.FadeAndLoad(sceneName);
            else SceneManager.LoadScene(sceneName);
        }

        // ------------------------------------------------------------------ objetos creados en juego
        public Pickup SpawnPickup(Pickup.Kind kind, Vector3 pos, Vector2 launch)
        {
            return SpawnPickupValue(kind, pos, launch, 1);
        }

        public Pickup SpawnPickupValue(Pickup.Kind kind, Vector3 pos, Vector2 launch, int value)
        {
            var go = new GameObject(kind.ToString());
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 15;
            if (litMaterial != null) sr.sharedMaterial = litMaterial;
            var c = go.AddComponent<CircleCollider2D>();
            c.isTrigger = true;
            c.radius = 0.45f;
            var p = go.AddComponent<Pickup>();
            p.kind = kind;
            p.value = value;
            p.dropped = true;
            p.launchVelocity = launch;
            p.coinSprites = coinSprites;
            p.staticSprite = kind == Pickup.Kind.Apple ? appleSprite : (kind == Pickup.Kind.Heart ? heartSprite : null);
            if (value > 1) go.transform.localScale = Vector3.one * 1.25f;
            p.Init();
            return p;
        }

        public void DropCoins(Vector3 pos, int count)
        {
            DropCoinsValue(pos, count, Mathf.Min(count, 6));
        }

        /// <summary>Suelta monedas que suman 'total' repartidas en 'pieces' piezas.</summary>
        public void DropCoinsValue(Vector3 pos, int total, int pieces)
        {
            if (total <= 0) return;
            pieces = Mathf.Clamp(pieces, 1, total);
            int each = total / pieces;
            int rest = total - each * pieces;
            for (int i = 0; i < pieces; i++)
            {
                int v = each + (i == 0 ? rest : 0);
                SpawnPickupValue(Pickup.Kind.Coin, pos, new Vector2(Random.Range(-3.5f, 3.5f), Random.Range(5f, 8f)), v);
            }
        }
    }
}
