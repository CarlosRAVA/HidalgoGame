using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dolores
{
    /// <summary>Interfaz del nivel: corazones, monedas, puntos, nombre de zona, pausa, game over y victoria.</summary>
    public class GameUI : MonoBehaviour
    {
        public static GameUI I;

        public Sprite heartSprite;
        public Sprite coinSprite;
        public Sprite panelSprite;
        public Sprite buttonSprite;
        public Font font;
        public Font titleFont;

        Canvas canvas;
        Image[] hearts;
        Text livesText, coinsText, scoreText, timeText, zoneTitle, zoneSub, toastText, hintText, victoryStats;
        CanvasGroup zoneGroup, toastGroup, hintGroup;
        GameObject pausePanel, gameOverPanel, victoryPanel;
        Image fade, flash;
        Button pauseFirst, gameOverFirst, victoryFirst, nextButton;
        float zoneTimer, toastTimer, hintTimer = 14f;
        bool loading;
        const float ZoneTime = 4.2f;
        const float ToastTime = 2.4f;

        void Awake()
        {
            I = this;
            UIFactory.Font = font;
            UIFactory.PanelSprite = panelSprite;
            UIFactory.ButtonSprite = buttonSprite;
            UIFactory.EnsureEventSystem();
            Build();
        }

        void Start()
        {
            BuildHearts();
            var gs = GameSession.I;
            if (gs != null)
            {
                gs.StatsChanged += Refresh;
                gs.StateChanged += OnStateChanged;
                gs.ZoneShown += ShowZone;
                gs.ToastShown += ShowToast;
                gs.DamageFlashed += OnDamage;
            }
            Refresh();
            StartCoroutine(FadeIn());
        }

        void OnDestroy()
        {
            if (I == this) I = null;
            var gs = GameSession.I;
            if (gs != null)
            {
                gs.StatsChanged -= Refresh;
                gs.StateChanged -= OnStateChanged;
                gs.ZoneShown -= ShowZone;
                gs.ToastShown -= ShowToast;
                gs.DamageFlashed -= OnDamage;
            }
        }

        // ------------------------------------------------------------ construcción
        void Build()
        {
            canvas = UIFactory.CreateCanvas("HUD", 10);
            canvas.transform.SetParent(transform, false);
            Transform root = canvas.transform;

            flash = UIFactory.FullScreen(root, "FlashDanio", new Color(0.8f, 0.05f, 0.05f, 0f));

            // Vidas, monedas
            livesText = UIFactory.Label(root, "Vidas", "Vidas: 3", 44, UIFactory.Cream, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(livesText.rectTransform, new Vector2(0f, 1f), new Vector2(44f, -122f), new Vector2(420f, 56f));

            var coinIcon = UIFactory.Icon(root, "IconoMoneda", coinSprite, new Vector2(0f, 1f), new Vector2(44f, -190f), new Vector2(54f, 54f));
            coinsText = UIFactory.Label(root, "Monedas", "x 0", 44, UIFactory.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(coinsText.rectTransform, new Vector2(0f, 1f), new Vector2(110f, -190f), new Vector2(300f, 56f));

            scoreText = UIFactory.Label(root, "Puntos", "PUNTOS 000000", 46, UIFactory.Cream, TextAnchor.MiddleRight, FontStyle.Bold);
            UIFactory.Place(scoreText.rectTransform, new Vector2(1f, 1f), new Vector2(-44f, -52f), new Vector2(720f, 60f));
            timeText = UIFactory.Label(root, "Tiempo", "00:00", 34, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleRight, FontStyle.Bold);
            UIFactory.Place(timeText.rectTransform, new Vector2(1f, 1f), new Vector2(-44f, -108f), new Vector2(400f, 44f));

            // Zona
            var zr = UIFactory.Rect("Zona", root);
            UIFactory.Place(zr, new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(1500f, 220f));
            zoneGroup = zr.gameObject.AddComponent<CanvasGroup>();
            zoneGroup.alpha = 0f;
            zoneGroup.blocksRaycasts = false;
            zoneTitle = UIFactory.Label(zr, "Titulo", "", 90, UIFactory.Gold, TextAnchor.UpperCenter, FontStyle.Normal, true, titleFont);
            UIFactory.Place(zoneTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(1500f, 110f));
            zoneSub = UIFactory.Label(zr, "Subtitulo", "", 38, UIFactory.Cream, TextAnchor.UpperCenter);
            UIFactory.Place(zoneSub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(1500f, 70f));

            // Aviso
            var tr = UIFactory.Rect("Aviso", root);
            UIFactory.Place(tr, new Vector2(0.5f, 0f), new Vector2(0f, 190f), new Vector2(1500f, 70f));
            toastGroup = tr.gameObject.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            toastGroup.blocksRaycasts = false;
            toastText = UIFactory.Label(tr, "Texto", "", 50, UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(toastText.rectTransform);

            // Ayuda de controles
            var hr = UIFactory.Rect("Ayuda", root);
            UIFactory.Place(hr, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1800f, 60f));
            hintGroup = hr.gameObject.AddComponent<CanvasGroup>();
            hintGroup.blocksRaycasts = false;
            hintText = UIFactory.Label(hr, "Texto", "A / D  mover    ESPACIO  saltar    J  atacar con el hacha    ESC  pausa", 36, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(hintText.rectTransform);

            BuildPause(root);
            BuildGameOver(root);
            BuildVictory(root);

            fade = UIFactory.FullScreen(root, "Fundido", new Color(0f, 0f, 0f, 1f));
        }

        void BuildHearts()
        {
            int n = 5;
            if (GameSession.I != null && GameSession.I.player != null) n = GameSession.I.player.maxHealth;
            hearts = new Image[n];
            for (int i = 0; i < n; i++)
            {
                hearts[i] = UIFactory.Icon(canvas.transform, "Corazon" + i, heartSprite, new Vector2(0f, 1f),
                    new Vector2(44f + i * 70f, -48f), new Vector2(62f, 62f));
            }
        }

        GameObject MakeOverlay(Transform root, string name, Color dim)
        {
            var img = UIFactory.FullScreen(root, name, dim);
            img.raycastTarget = true;
            return img.gameObject;
        }

        void BuildPause(Transform root)
        {
            pausePanel = MakeOverlay(root, "Pausa", new Color(0f, 0f, 0f, 0.62f));
            Transform t = pausePanel.transform;
            UIFactory.Panel(t, "Panel", Vector2.zero, new Vector2(780f, 820f));
            var title = UIFactory.Label(t, "Titulo", "PAUSA", 110, UIFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Normal, false, titleFont);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 290f), new Vector2(700f, 120f));
            pauseFirst = UIFactory.MakeButton(t, "Continuar", new Vector2(0f, 130f), new Vector2(560f, 100f), () => GameSession.I.SetPaused(false));
            UIFactory.MakeButton(t, "Reiniciar nivel", new Vector2(0f, 10f), new Vector2(560f, 100f), () => GameSession.I.Restart());
            UIFactory.MakeButton(t, "Menú principal", new Vector2(0f, -110f), new Vector2(560f, 100f), () => GameSession.I.GoMenu());
            var tip = UIFactory.Label(t, "Controles", "A/D mover · ESPACIO saltar · J atacar", 32, UIFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            UIFactory.Place(tip.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -270f), new Vector2(700f, 60f));
            pausePanel.SetActive(false);
        }

        void BuildGameOver(Transform root)
        {
            gameOverPanel = MakeOverlay(root, "GameOver", new Color(0.12f, 0f, 0f, 0.78f));
            Transform t = gameOverPanel.transform;
            UIFactory.Panel(t, "Panel", Vector2.zero, new Vector2(900f, 700f));
            var title = UIFactory.Label(t, "Titulo", "¡HAS CAÍDO!", 100, new Color(0.62f, 0.1f, 0.06f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold, false);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 230f), new Vector2(820f, 120f));
            var sub = UIFactory.Label(t, "Sub", "La lucha por la libertad continúa.\nInténtalo de nuevo.", 42, UIFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            UIFactory.Place(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(820f, 130f));
            gameOverFirst = UIFactory.MakeButton(t, "Reintentar", new Vector2(0f, -80f), new Vector2(560f, 100f), () => GameSession.I.Restart());
            UIFactory.MakeButton(t, "Menú principal", new Vector2(0f, -200f), new Vector2(560f, 100f), () => GameSession.I.GoMenu());
            gameOverPanel.SetActive(false);
        }

        void BuildVictory(Transform root)
        {
            victoryPanel = MakeOverlay(root, "Victoria", new Color(0.02f, 0.02f, 0.12f, 0.6f));
            Transform t = victoryPanel.transform;
            UIFactory.Panel(t, "Panel", Vector2.zero, new Vector2(1000f, 960f));
            var title = UIFactory.Label(t, "Titulo", "¡NIVEL COMPLETADO!", 84, new Color(0.55f, 0.32f, 0.02f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold, false);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 380f), new Vector2(940f, 110f));
            var sub = UIFactory.Label(t, "Sub", "La campana de Dolores ha sonado.", 40, UIFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            UIFactory.Place(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 290f), new Vector2(940f, 60f));
            victoryStats = UIFactory.Label(t, "Stats", "", 44, UIFactory.Ink, TextAnchor.UpperCenter, FontStyle.Bold, false);
            UIFactory.Place(victoryStats.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(900f, 300f));
            nextButton = UIFactory.MakeButton(t, "Siguiente nivel", new Vector2(0f, -170f), new Vector2(560f, 96f), () => GameSession.I.GoNext());
            victoryFirst = UIFactory.MakeButton(t, "Jugar de nuevo", new Vector2(0f, -280f), new Vector2(560f, 96f), () => GameSession.I.Restart());
            UIFactory.MakeButton(t, "Menú principal", new Vector2(0f, -390f), new Vector2(560f, 96f), () => GameSession.I.GoMenu());
            victoryPanel.SetActive(false);
        }

        // ------------------------------------------------------------ eventos
        void Refresh()
        {
            var gs = GameSession.I;
            if (gs == null || hearts == null) return;
            int hp = gs.player != null ? gs.player.Health : 0;
            for (int i = 0; i < hearts.Length; i++)
            {
                hearts[i].color = i < hp ? Color.white : new Color(0.2f, 0.2f, 0.2f, 0.6f);
            }
            livesText.text = "Vidas: " + gs.Lives;
            coinsText.text = "x " + gs.Coins;
            scoreText.text = "PUNTOS " + gs.Score.ToString("D6");
        }

        void OnStateChanged()
        {
            var gs = GameSession.I;
            if (gs == null) return;
            pausePanel.SetActive(gs.State == GameState.Paused);
            gameOverPanel.SetActive(gs.State == GameState.GameOver);
            bool showVictory = gs.State == GameState.Victory;
            victoryPanel.SetActive(showVictory);
            if (gs.State == GameState.Paused) UIFactory.Select(pauseFirst);
            if (gs.State == GameState.GameOver) UIFactory.Select(gameOverFirst);
            if (showVictory)
            {
                nextButton.gameObject.SetActive(gs.NextSceneAvailable);
                int m = (int)(gs.Elapsed / 60f);
                int s = (int)(gs.Elapsed % 60f);
                victoryStats.text = "Monedas: " + gs.Coins + "\nSoldados derrotados: " + gs.Kills + "\nVidas restantes: " + gs.Lives +
                    "\nTiempo: " + m.ToString("D2") + ":" + s.ToString("D2") + "\n<b>PUNTOS: " + gs.Score + "</b>";
                UIFactory.Select(gs.NextSceneAvailable ? nextButton : victoryFirst);
            }
        }

        void ShowZone(string title, string subtitle)
        {
            zoneTitle.text = title;
            zoneSub.text = subtitle;
            zoneTimer = ZoneTime;
        }

        void ShowToast(string msg)
        {
            toastText.text = msg;
            toastTimer = ToastTime;
        }

        void OnDamage()
        {
            var c = flash.color;
            c.a = 0.45f;
            flash.color = c;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            var gs = GameSession.I;

            if (zoneTimer > 0f)
            {
                zoneTimer -= dt;
                float a = Mathf.Min(Mathf.Clamp01((ZoneTime - zoneTimer) * 2.5f), Mathf.Clamp01(zoneTimer * 1.4f));
                zoneGroup.alpha = a;
            }
            else zoneGroup.alpha = 0f;

            if (toastTimer > 0f)
            {
                toastTimer -= dt;
                toastGroup.alpha = Mathf.Min(Mathf.Clamp01((ToastTime - toastTimer) * 4f), Mathf.Clamp01(toastTimer * 2f));
            }
            else toastGroup.alpha = 0f;

            if (hintTimer > 0f)
            {
                hintTimer -= dt;
                hintGroup.alpha = Mathf.Clamp01(hintTimer * 0.8f);
            }
            else hintGroup.alpha = 0f;

            if (flash.color.a > 0f)
            {
                var c = flash.color;
                c.a = Mathf.Max(0f, c.a - dt * 1.4f);
                flash.color = c;
            }

            if (gs != null && timeText != null)
            {
                int m = (int)(gs.Elapsed / 60f);
                int s = (int)(gs.Elapsed % 60f);
                timeText.text = m.ToString("D2") + ":" + s.ToString("D2");
            }
        }

        // ------------------------------------------------------------ transiciones
        IEnumerator FadeIn()
        {
            float t = 1f;
            while (t > 0f)
            {
                t -= Time.unscaledDeltaTime * 2.2f;
                var c = fade.color; c.a = Mathf.Clamp01(t); fade.color = c;
                yield return null;
            }
        }

        public void FadeAndLoad(string sceneName)
        {
            if (loading) return;
            loading = true;
            StartCoroutine(FadeOutLoad(sceneName));
        }

        IEnumerator FadeOutLoad(string sceneName)
        {
            fade.raycastTarget = true;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * 2.5f;
                var c = fade.color; c.a = Mathf.Clamp01(t); fade.color = c;
                yield return null;
            }
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }
}
