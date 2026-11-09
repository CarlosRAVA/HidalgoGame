using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dolores
{
    public enum GameState { Playing, Paused, PlayerDead, GameOver, Victory }

    /// <summary>Estado general del nivel: vidas, puntos, checkpoints, pausa, game over y victoria.</summary>
    [DefaultExecutionOrder(-100)]
    public class GameSession : MonoBehaviour
    {
        public static GameSession I;

        [Header("Referencias")]
        public PlatformerPlayer player;
        public FollowCamera cam;
        public Transform startPoint;

        [Header("Reglas")]
        public int startLives = 3;
        public int coinsForExtraLife = 50;
        public string menuScene = SceneNames.Menu;
        public string nextScene = SceneNames.Level2;

        [Header("Recursos para objetos que aparecen en juego")]
        public Sprite sparkSprite;
        public Sprite dustSprite;
        public Sprite[] coinSprites;
        public Sprite appleSprite;
        public Sprite heartSprite;
        public Material litMaterial;
        public Material unlitMaterial;

        public GameState State { get; private set; }
        public int Lives { get; private set; }
        public int Score { get; private set; }
        public int Coins { get; private set; }
        public int Kills { get; private set; }
        public float Elapsed { get; private set; }
        public int CheckpointIndex { get; private set; }

        public event System.Action StatsChanged;
        public event System.Action StateChanged;
        public event System.Action<string, string> ZoneShown;
        public event System.Action<string> ToastShown;
        public event System.Action DamageFlashed;

        Vector3 respawnPos;

        public bool NextSceneAvailable
        {
            get { return !string.IsNullOrEmpty(nextScene) && Application.CanStreamedLevelBeLoaded(nextScene); }
        }

        void Awake()
        {
            I = this;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            State = GameState.Playing;
            CheckpointIndex = -1;
            Fx.Spark = sparkSprite;
            Fx.Dust = dustSprite;
            Fx.UnlitMat = unlitMaterial;
            int pl = LayerMask.NameToLayer("Player");
            int en = LayerMask.NameToLayer("Enemy");
            if (pl >= 0) Physics2D.IgnoreLayerCollision(pl, pl, true);
            if (pl >= 0 && en >= 0)
            {
                Physics2D.IgnoreLayerCollision(pl, en, true);
                Physics2D.IgnoreLayerCollision(en, en, true);
            }
        }

        void Start()
        {
            Lives = startLives;
            if (startPoint != null) respawnPos = startPoint.position;
            else if (player != null) respawnPos = player.transform.position;
            RaiseStats();
        }

        void OnDestroy()
        {
            if (I == this) I = null;
            Time.timeScale = 1f;
        }

        void Update()
        {
            if (State == GameState.Playing || State == GameState.PlayerDead) Elapsed += Time.deltaTime;

            if (GameInput.PauseDown())
            {
                if (State == GameState.Playing) SetPaused(true);
                else if (State == GameState.Paused) SetPaused(false);
            }
        }

        public void SetPaused(bool paused)
        {
            if (paused && State != GameState.Playing) return;
            if (!paused && State != GameState.Paused) return;
            State = paused ? GameState.Paused : GameState.Playing;
            Time.timeScale = paused ? 0f : 1f;
            if (AudioHub.I != null) AudioHub.I.SetMusicLevel(paused ? 0.3f : 1f);
            AudioHub.Play(SfxId.Pause, 0f);
            if (!paused && player != null) player.BlockInput(0.25f);
            if (StateChanged != null) StateChanged();
        }

        public void RaiseStats()
        {
            if (StatsChanged != null) StatsChanged();
        }

        public void AddScore(int amount)
        {
            Score += amount;
            RaiseStats();
        }

        public void AddCoin()
        {
            Coins++;
            Score += 10;
            if (coinsForExtraLife > 0 && Coins % coinsForExtraLife == 0)
            {
                AddLife();
                ShowToast("¡Vida extra por 50 monedas!");
            }
            RaiseStats();
        }

        public void AddLife()
        {
            Lives = Mathf.Min(9, Lives + 1);
            RaiseStats();
        }

        public void RegisterKill(int score)
        {
            Kills++;
            Score += score;
            RaiseStats();
        }

        public void SetCheckpoint(Vector3 pos, int index)
        {
            respawnPos = pos;
            CheckpointIndex = index;
            ShowToast("¡Punto de control!");
        }

        public void ShowZone(string title, string subtitle)
        {
            if (ZoneShown != null) ZoneShown(title, subtitle);
        }

        public void ShowToast(string msg)
        {
            if (ToastShown != null) ToastShown(msg);
        }

        public void FlashDamage()
        {
            if (DamageFlashed != null) DamageFlashed();
        }

        public void OnPlayerDied()
        {
            if (State == GameState.Victory) return;
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
                ShowToast(Lives == 1 ? "¡Última vida!" : "Vidas restantes: " + Lives);
                if (StateChanged != null) StateChanged();
            }
            else
            {
                State = GameState.GameOver;
                if (AudioHub.I != null) AudioHub.I.StopMusic();
                AudioHub.Play(SfxId.GameOver, 0f);
                if (StateChanged != null) StateChanged();
            }
        }

        public void Victory()
        {
            if (State == GameState.Victory || State == GameState.GameOver) return;
            State = GameState.Victory;
            Score += 500 + Lives * 100;
            RaiseStats();
            if (AudioHub.I != null) AudioHub.I.SetMusicLevel(0.25f);
            StartCoroutine(VictoryRoutine());
        }

        IEnumerator VictoryRoutine()
        {
            yield return new WaitForSecondsRealtime(2.6f);
            AudioHub.Play(SfxId.Victory, 0f);
            if (StateChanged != null) StateChanged();
        }

        public void Restart()
        {
            LoadScene(SceneManager.GetActiveScene().name);
        }

        public void GoMenu()
        {
            LoadScene(menuScene);
        }

        public void GoNext()
        {
            if (NextSceneAvailable) LoadScene(nextScene);
        }

        public void LoadScene(string sceneName)
        {
            Time.timeScale = 1f;
            if (GameUI.I != null) GameUI.I.FadeAndLoad(sceneName);
            else SceneManager.LoadScene(sceneName);
        }

        public Pickup SpawnPickup(Pickup.Kind kind, Vector3 pos, Vector2 launch)
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
            p.dropped = true;
            p.launchVelocity = launch;
            p.coinSprites = coinSprites;
            p.staticSprite = kind == Pickup.Kind.Apple ? appleSprite : (kind == Pickup.Kind.Heart ? heartSprite : null);
            p.Init();
            return p;
        }

        public void DropCoins(Vector3 pos, int count)
        {
            for (int i = 0; i < count; i++)
            {
                SpawnPickup(Pickup.Kind.Coin, pos, new Vector2(Random.Range(-3.5f, 3.5f), Random.Range(5f, 8f)));
            }
        }
    }
}
