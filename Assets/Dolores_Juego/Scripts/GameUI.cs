using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dolores1810
{
    /// <summary>
    /// Interfaz del nivel: corazones, resistencia, arma, monedas, objetivo, aviso de interacción, barra del jefe,
    /// cinemáticas, pausa, configuración, tienda, game over y pantalla final.
    /// </summary>
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
        Image staminaFill, weaponIcon, bossFill;
        Text livesText, coinsText, scoreText, timeText, zoneTitle, zoneSub, toastText, hintText, objectiveText, weaponText, promptText;
        Text bossName, introTitle, introSub, exitText, victoryStats, pauseMissions;
        CanvasGroup zoneGroup, toastGroup, hintGroup, promptGroup, bossGroup, introGroup, exitGroup, objectiveGroup;
        RectTransform barTop, barBottom, staminaBack;
        GameObject pausePanel, gameOverPanel, victoryPanel;
        ShopUI shop;
        SettingsUI settings;
        Image fade, flash;
        Button pauseFirst, gameOverFirst, victoryFirst;
        float zoneTimer, toastTimer, hintTimer = 16f, toastDuration = 2.6f, letterbox, letterboxTarget;
        bool loading, introActive;
        const float ZoneTime = 4.2f;

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
                gs.ObjectiveChanged += OnObjective;
                gs.PromptChanged += OnPrompt;
                gs.BossStartedEvent += OnBossStart;
                gs.BossHealthEvent += OnBossHealth;
                gs.BossEndedEvent += OnBossEnd;
                gs.IntroShown += OnIntro;
                gs.IntroEnded += OnIntroEnd;
                gs.ExitShown += OnExit;
            }
            Refresh();
            OnObjective();
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
                gs.ObjectiveChanged -= OnObjective;
                gs.PromptChanged -= OnPrompt;
                gs.BossStartedEvent -= OnBossStart;
                gs.BossHealthEvent -= OnBossHealth;
                gs.BossEndedEvent -= OnBossEnd;
                gs.IntroShown -= OnIntro;
                gs.IntroEnded -= OnIntroEnd;
                gs.ExitShown -= OnExit;
            }
        }

        // ------------------------------------------------------------ construcción
        CanvasGroup Group(RectTransform rt, float alpha)
        {
            var g = rt.gameObject.AddComponent<CanvasGroup>();
            g.alpha = alpha;
            g.blocksRaycasts = false;
            g.interactable = false;
            return g;
        }

        void Build()
        {
            canvas = UIFactory.CreateCanvas("HUD", 10);
            canvas.transform.SetParent(transform, false);
            Transform root = canvas.transform;

            flash = UIFactory.FullScreen(root, "FlashDanio", new Color(0.8f, 0.05f, 0.05f, 0f));

            // Resistencia
            Image sf;
            staminaBack = UIFactory.MakeBar(root, "Resistencia", new Color(0.35f, 0.85f, 0.35f, 1f), new Vector2(0f, 1f), new Vector2(44f, -104f), new Vector2(400f, 26f), out sf);
            staminaFill = sf;
            var stl = UIFactory.Label(staminaBack, "Texto", "RESISTENCIA", 20, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(stl.rectTransform);

            // Arma
            weaponIcon = UIFactory.Icon(root, "ArmaIcono", null, new Vector2(0f, 1f), new Vector2(44f, -146f), new Vector2(64f, 64f));
            weaponText = UIFactory.Label(root, "ArmaNombre", "", 32, UIFactory.Cream, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(weaponText.rectTransform, new Vector2(0f, 1f), new Vector2(118f, -146f), new Vector2(520f, 64f));

            // Monedas y vidas
            UIFactory.Icon(root, "IconoMoneda", coinSprite, new Vector2(0f, 1f), new Vector2(44f, -222f), new Vector2(52f, 52f));
            coinsText = UIFactory.Label(root, "Monedas", "x 0", 42, UIFactory.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(coinsText.rectTransform, new Vector2(0f, 1f), new Vector2(108f, -222f), new Vector2(300f, 52f));
            livesText = UIFactory.Label(root, "Vidas", "Vidas: 3", 34, UIFactory.Cream, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(livesText.rectTransform, new Vector2(0f, 1f), new Vector2(330f, -222f), new Vector2(260f, 52f));

            scoreText = UIFactory.Label(root, "Puntos", "PUNTOS 000000", 44, UIFactory.Cream, TextAnchor.MiddleRight, FontStyle.Bold);
            UIFactory.Place(scoreText.rectTransform, new Vector2(1f, 1f), new Vector2(-44f, -52f), new Vector2(700f, 58f));
            timeText = UIFactory.Label(root, "Tiempo", "00:00", 32, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleRight, FontStyle.Bold);
            UIFactory.Place(timeText.rectTransform, new Vector2(1f, 1f), new Vector2(-44f, -104f), new Vector2(400f, 40f));

            // Objetivo
            var orr = UIFactory.Rect("Objetivo", root);
            UIFactory.PlaceCenter(orr, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(1100f, 50f));
            objectiveGroup = Group(orr, 0f);
            objectiveText = UIFactory.Label(orr, "Texto", "", 34, new Color(1f, 0.95f, 0.7f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(objectiveText.rectTransform);

            // Barra de jefe
            var br = UIFactory.Rect("Jefe", root);
            UIFactory.PlaceCenter(br, new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(920f, 80f));
            bossGroup = Group(br, 0f);
            bossName = UIFactory.Label(br, "Nombre", "", 38, new Color(1f, 0.8f, 0.7f, 1f), TextAnchor.UpperCenter, FontStyle.Bold);
            UIFactory.Place(bossName.rectTransform, new Vector2(0.5f, 1f), new Vector2(-460f, 0f), new Vector2(920f, 44f));
            Image bf;
            var bbar = UIFactory.MakeBar(br, "Barra", new Color(0.85f, 0.15f, 0.12f, 1f), new Vector2(0.5f, 0f), new Vector2(-460f, 0f), new Vector2(920f, 30f), out bf);
            bossFill = bf;

            // Zona
            var zr = UIFactory.Rect("Zona", root);
            UIFactory.Place(zr, new Vector2(0.5f, 1f), new Vector2(-750f, -200f), new Vector2(1500f, 220f));
            zoneGroup = Group(zr, 0f);
            zoneTitle = UIFactory.Label(zr, "Titulo", "", 90, UIFactory.Gold, TextAnchor.UpperCenter, FontStyle.Normal, true, titleFont);
            UIFactory.Place(zoneTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(-750f, 0f), new Vector2(1500f, 110f));
            zoneSub = UIFactory.Label(zr, "Subtitulo", "", 38, UIFactory.Cream, TextAnchor.UpperCenter);
            UIFactory.Place(zoneSub.rectTransform, new Vector2(0.5f, 1f), new Vector2(-750f, -110f), new Vector2(1500f, 70f));

            // Aviso (toast)
            var tr = UIFactory.Rect("Aviso", root);
            UIFactory.PlaceCenter(tr, new Vector2(0.5f, 0f), new Vector2(0f, 350f), new Vector2(1700f, 120f));
            toastGroup = Group(tr, 0f);
            toastText = UIFactory.Label(tr, "Texto", "", 44, UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(toastText.rectTransform);

            // Interacción
            var pr = UIFactory.Rect("Interaccion", root);
            UIFactory.PlaceCenter(pr, new Vector2(0.5f, 0f), new Vector2(0f, 235f), new Vector2(1000f, 80f));
            promptGroup = Group(pr, 0f);
            var pbg = UIFactory.FullScreen(pr, "Fondo", new Color(0f, 0f, 0f, 0.55f));
            promptText = UIFactory.Label(pr, "Texto", "", 44, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(promptText.rectTransform);

            // Ayuda de controles
            var hr = UIFactory.Rect("Ayuda", root);
            UIFactory.PlaceCenter(hr, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1900f, 60f));
            hintGroup = Group(hr, 1f);
            hintText = UIFactory.Label(hr, "Texto", "A/D mover   SHIFT correr   ESPACIO saltar   J atacar   E interactuar   Q cambiar arma   ESC pausa", 32, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(hintText.rectTransform);

            // Barras de cine
            var bt = UIFactory.FullScreen(root, "CineArriba", Color.black);
            barTop = bt.rectTransform;
            barTop.anchorMin = new Vector2(0f, 1f); barTop.anchorMax = new Vector2(1f, 1f); barTop.pivot = new Vector2(0.5f, 1f);
            barTop.offsetMin = new Vector2(0f, -140f); barTop.offsetMax = Vector2.zero;
            var bb = UIFactory.FullScreen(root, "CineAbajo", Color.black);
            barBottom = bb.rectTransform;
            barBottom.anchorMin = new Vector2(0f, 0f); barBottom.anchorMax = new Vector2(1f, 0f); barBottom.pivot = new Vector2(0.5f, 0f);
            barBottom.offsetMin = Vector2.zero; barBottom.offsetMax = new Vector2(0f, 140f);
            SetLetterbox(0f);

            // Presentación de nivel
            var ir = UIFactory.Rect("Introduccion", root);
            UIFactory.PlaceCenter(ir, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(1700f, 320f));
            introGroup = Group(ir, 0f);
            introTitle = UIFactory.Label(ir, "Titulo", "", 96, UIFactory.Gold, TextAnchor.UpperCenter, FontStyle.Normal, true, titleFont);
            UIFactory.Place(introTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(-850f, 0f), new Vector2(1700f, 130f));
            introSub = UIFactory.Label(ir, "Subtitulo", "", 44, Color.white, TextAnchor.UpperCenter, FontStyle.Bold);
            UIFactory.Place(introSub.rectTransform, new Vector2(0.5f, 1f), new Vector2(-850f, -150f), new Vector2(1700f, 150f));

            // Texto de salida
            var er = UIFactory.Rect("TextoSalida", root);
            UIFactory.PlaceCenter(er, new Vector2(0.5f, 0.5f), new Vector2(0f, -330f), new Vector2(1600f, 220f));
            exitGroup = Group(er, 0f);
            exitText = UIFactory.Label(er, "Texto", "", 46, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(exitText.rectTransform);

            BuildPause(root);
            settings = SettingsUI.Create(root, OnSettingsBack, false);
            settings.gameObject.SetActive(false);
            shop = ShopUI.Create(root, OnShopClosed);
            shop.gameObject.SetActive(false);
            BuildGameOver(root);
            BuildVictory(root);

            fade = UIFactory.FullScreen(root, "Fundido", new Color(0f, 0f, 0f, 1f));
        }

        void BuildHearts()
        {
            int n = 7;
            hearts = new Image[n];
            for (int i = 0; i < n; i++)
            {
                hearts[i] = UIFactory.Icon(canvas.transform, "Corazon" + i, heartSprite, new Vector2(0f, 1f), new Vector2(44f + i * 64f, -40f), new Vector2(58f, 58f));
                hearts[i].gameObject.SetActive(false);
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
            UIFactory.Panel(t, "Panel", Vector2.zero, new Vector2(1280f, 880f));
            var title = UIFactory.Label(t, "Titulo", "PAUSA", 110, UIFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Normal, false, titleFont);
            UIFactory.PlaceCenter(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 340f), new Vector2(900f, 130f));
            pauseFirst = UIFactory.MakeButton(t, "Continuar", new Vector2(-330f, 160f), new Vector2(520f, 96f), () => GameSession.I.SetPaused(false));
            UIFactory.MakeButton(t, "Reiniciar nivel", new Vector2(-330f, 40f), new Vector2(520f, 96f), () => GameSession.I.Restart());
            UIFactory.MakeButton(t, "Configuración", new Vector2(-330f, -80f), new Vector2(520f, 96f), OpenSettings);
            UIFactory.MakeButton(t, "Menú principal", new Vector2(-330f, -200f), new Vector2(520f, 96f), () => GameSession.I.GoMenu());
            var mh = UIFactory.Label(t, "MisionesTitulo", "MISIONES DEL NIVEL", 36, UIFactory.Ink, TextAnchor.MiddleLeft, FontStyle.Bold, false);
            UIFactory.PlaceCenter(mh.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(250f, 190f), new Vector2(560f, 50f));
            pauseMissions = UIFactory.Label(t, "Misiones", "", 32, UIFactory.Ink, TextAnchor.UpperLeft, FontStyle.Normal, false);
            UIFactory.PlaceCenter(pauseMissions.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(250f, 50f), new Vector2(560f, 240f));
            var tip = UIFactory.Label(t, "Controles", "A/D mover · SHIFT correr · ESPACIO saltar · J atacar\nE interactuar · W/S trepar · Q o 1-5 cambiar arma", 28, UIFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            UIFactory.PlaceCenter(tip.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -340f), new Vector2(1150f, 100f));
            pausePanel.SetActive(false);
        }

        void BuildGameOver(Transform root)
        {
            gameOverPanel = MakeOverlay(root, "GameOver", new Color(0.12f, 0f, 0f, 0.78f));
            Transform t = gameOverPanel.transform;
            UIFactory.Panel(t, "Panel", Vector2.zero, new Vector2(900f, 700f));
            var title = UIFactory.Label(t, "Titulo", "¡HAS CAÍDO!", 100, new Color(0.62f, 0.1f, 0.06f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold, false);
            UIFactory.PlaceCenter(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 230f), new Vector2(820f, 120f));
            var sub = UIFactory.Label(t, "Sub", "La lucha por la libertad continúa.\nTus monedas y mejoras se conservan.", 40, UIFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            UIFactory.PlaceCenter(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(820f, 130f));
            gameOverFirst = UIFactory.MakeButton(t, "Reintentar", new Vector2(0f, -80f), new Vector2(560f, 100f), () => GameSession.I.Restart());
            UIFactory.MakeButton(t, "Menú principal", new Vector2(0f, -200f), new Vector2(560f, 100f), () => GameSession.I.GoMenu());
            gameOverPanel.SetActive(false);
        }

        void BuildVictory(Transform root)
        {
            victoryPanel = MakeOverlay(root, "Victoria", new Color(0.02f, 0.02f, 0.12f, 0.6f));
            Transform t = victoryPanel.transform;
            UIFactory.Panel(t, "Panel", Vector2.zero, new Vector2(1100f, 960f));
            var title = UIFactory.Label(t, "Titulo", "¡FIN DEL JUEGO!", 92, new Color(0.55f, 0.32f, 0.02f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold, false);
            UIFactory.PlaceCenter(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 390f), new Vector2(1000f, 110f));
            var sub = UIFactory.Label(t, "Sub", "La campana de Dolores ha sonado y comenzó la lucha por la Independencia de México.", 36, UIFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            UIFactory.PlaceCenter(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 290f), new Vector2(980f, 110f));
            victoryStats = UIFactory.Label(t, "Stats", "", 40, UIFactory.Ink, TextAnchor.UpperCenter, FontStyle.Bold, false);
            UIFactory.PlaceCenter(victoryStats.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(960f, 330f));
            victoryFirst = UIFactory.MakeButton(t, "Menú principal", new Vector2(0f, -310f), new Vector2(560f, 96f), () => GameSession.I.GoMenu());
            UIFactory.MakeButton(t, "Jugar de nuevo", new Vector2(0f, -420f), new Vector2(560f, 96f), () => GameSession.I.LoadScene(SceneNames.ForLevel(1)));
            victoryPanel.SetActive(false);
        }

        // ------------------------------------------------------------ eventos
        void Refresh()
        {
            var gs = GameSession.I;
            if (gs == null || hearts == null) return;
            var pl = gs.player;
            int hp = pl != null ? pl.Health : 0;
            int max = pl != null ? pl.MaxHealth : GameRules.BaseHearts;
            for (int i = 0; i < hearts.Length; i++)
            {
                bool visible = i < max;
                hearts[i].gameObject.SetActive(visible);
                if (visible) hearts[i].color = i < hp ? Color.white : new Color(0.2f, 0.2f, 0.2f, 0.65f);
            }
            livesText.text = "Vidas: " + gs.Lives;
            coinsText.text = "x " + PlayerProgress.Coins;
            scoreText.text = "PUNTOS " + gs.Score.ToString("D6");
            var w = pl != null ? pl.Weapon : null;
            if (w != null)
            {
                weaponIcon.sprite = w.icon;
                weaponText.text = w.displayName + "  (daño " + pl.CurrentDamage() + ")";
            }
        }

        void OnStateChanged()
        {
            var gs = GameSession.I;
            if (gs == null) return;
            pausePanel.SetActive(gs.State == GameState.Paused && !settings.gameObject.activeSelf);
            if (gs.State == GameState.Paused) pauseMissions.text = gs.MissionLines();
            gameOverPanel.SetActive(gs.State == GameState.GameOver);
            bool showShop = gs.State == GameState.Shop;
            if (showShop && !shop.gameObject.activeSelf) shop.Show();
            if (!showShop && shop.gameObject.activeSelf) shop.gameObject.SetActive(false);
            if (gs.State != GameState.Paused && settings.gameObject.activeSelf) settings.gameObject.SetActive(false);
            bool showVictory = gs.State == GameState.Victory;
            victoryPanel.SetActive(showVictory);
            if (gs.State == GameState.Paused) UIFactory.Select(pauseFirst);
            if (gs.State == GameState.GameOver) UIFactory.Select(gameOverFirst);
            if (showVictory)
            {
                victoryStats.text = BuildFinalStats(gs);
                UIFactory.Select(victoryFirst);
                letterboxTarget = 0f;
            }
        }

        string BuildFinalStats(GameSession gs)
        {
            int m = (int)(gs.Elapsed / 60f);
            int s = (int)(gs.Elapsed % 60f);
            var d = PlayerProgress.Data;
            return "Soldados derrotados (partida): " + gs.Kills + "\nMonedas en tu bolsa: " + PlayerProgress.Coins +
                "\nCofres abiertos: " + d.openedChests.Count + "\nPergaminos encontrados: " + d.foundCollectibles.Count + " / 9" +
                "\nMisiones del nivel: " + gs.MissionsDoneCount() + " / " + (gs.missions != null ? gs.missions.Length : 0) +
                "\nTiempo del nivel: " + m.ToString("D2") + ":" + s.ToString("D2") + "\n<b>PUNTOS: " + gs.Score + "</b>";
        }

        void OpenSettings()
        {
            pausePanel.SetActive(false);
            settings.Show();
        }

        void OnSettingsBack()
        {
            var gs = GameSession.I;
            if (gs != null && gs.State == GameState.Paused)
            {
                pausePanel.SetActive(true);
                UIFactory.Select(pauseFirst);
            }
        }

        void OnShopClosed()
        {
            if (GameSession.I != null) GameSession.I.CloseShop();
        }

        void ShowZone(string title, string subtitle)
        {
            zoneTitle.text = title;
            zoneSub.text = subtitle;
            zoneTimer = ZoneTime;
        }

        void ShowToast(string msg, float seconds)
        {
            toastText.text = msg;
            toastDuration = Mathf.Max(1.5f, seconds);
            toastTimer = toastDuration;
        }

        void OnDamage()
        {
            var c = flash.color;
            c.a = 0.45f;
            flash.color = c;
        }

        void OnObjective()
        {
            var gs = GameSession.I;
            if (gs == null || objectiveText == null) return;
            objectiveText.text = string.IsNullOrEmpty(gs.Objective) ? "" : "Objetivo: " + gs.Objective;
        }

        void OnPrompt()
        {
            var gs = GameSession.I;
            string p = gs != null ? gs.Prompt : null;
            if (!string.IsNullOrEmpty(p)) promptText.text = p;
        }

        void OnBossStart(string name, int max)
        {
            bossName.text = name;
            UIFactory.SetBar(bossFill, 1f);
            bossShown = true;
        }
        bool bossShown;

        void OnBossHealth(int cur, int max)
        {
            UIFactory.SetBar(bossFill, max > 0 ? (float)cur / max : 0f);
        }

        void OnBossEnd() { bossShown = false; }

        void OnIntro(string title, string subtitle)
        {
            introTitle.text = title;
            introSub.text = subtitle;
            introActive = true;
            letterboxTarget = 1f;
            introTimer = 0f;
        }
        float introTimer;

        void OnIntroEnd()
        {
            introActive = false;
            letterboxTarget = 0f;
        }

        void OnExit(string text)
        {
            exitText.text = text;
            exitShown = true;
            letterboxTarget = 1f;
        }
        bool exitShown;

        void SetLetterbox(float k)
        {
            letterbox = k;
            if (barTop != null) barTop.offsetMin = new Vector2(0f, -140f * k);
            if (barBottom != null) barBottom.offsetMax = new Vector2(0f, 140f * k);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            var gs = GameSession.I;

            if (zoneTimer > 0f)
            {
                zoneTimer -= dt;
                zoneGroup.alpha = Mathf.Min(Mathf.Clamp01((ZoneTime - zoneTimer) * 2.5f), Mathf.Clamp01(zoneTimer * 1.4f));
            }
            else zoneGroup.alpha = 0f;

            if (toastTimer > 0f)
            {
                toastTimer -= dt;
                toastGroup.alpha = Mathf.Min(Mathf.Clamp01((toastDuration - toastTimer) * 4f), Mathf.Clamp01(toastTimer * 2f));
            }
            else toastGroup.alpha = 0f;

            if (hintTimer > 0f)
            {
                if (gs != null && gs.State == GameState.Playing) hintTimer -= dt;
                hintGroup.alpha = Mathf.Clamp01(hintTimer * 0.8f);
            }
            else hintGroup.alpha = 0f;

            bool showPrompt = gs != null && !string.IsNullOrEmpty(gs.Prompt) && gs.State == GameState.Playing;
            promptGroup.alpha = Mathf.MoveTowards(promptGroup.alpha, showPrompt ? 1f : 0f, dt * 8f);

            bool playing = gs != null && (gs.State == GameState.Playing || gs.State == GameState.PlayerDead);
            objectiveGroup.alpha = Mathf.MoveTowards(objectiveGroup.alpha, playing && !string.IsNullOrEmpty(objectiveText.text) ? 1f : 0f, dt * 3f);
            bossGroup.alpha = Mathf.MoveTowards(bossGroup.alpha, bossShown && playing ? 1f : 0f, dt * 3f);

            if (introActive || exitShown)
            {
                introTimer += dt;
            }
            float ia = introActive ? Mathf.Min(Mathf.Clamp01(introTimer * 1.6f), 1f) : Mathf.MoveTowards(introGroup.alpha, 0f, dt * 2.5f);
            introGroup.alpha = ia;
            exitGroup.alpha = exitShown ? Mathf.MoveTowards(exitGroup.alpha, 1f, dt * 1.5f) : 0f;

            if (!Mathf.Approximately(letterbox, letterboxTarget))
                SetLetterbox(Mathf.MoveTowards(letterbox, letterboxTarget, dt * 1.6f));

            if (flash.color.a > 0f)
            {
                var c = flash.color;
                c.a = Mathf.Max(0f, c.a - dt * 1.4f);
                flash.color = c;
            }

            if (gs != null && gs.player != null)
            {
                float max = Mathf.Max(1f, gs.player.StaminaMax);
                UIFactory.SetBar(staminaFill, gs.player.Stamina / max);
                staminaFill.color = gs.player.Exhausted ? new Color(0.9f, 0.3f, 0.2f, 1f) : (gs.player.IsRunning ? new Color(1f, 0.85f, 0.25f, 1f) : new Color(0.35f, 0.85f, 0.35f, 1f));
                var sbr = staminaBack.sizeDelta;
                staminaBack.sizeDelta = new Vector2(Mathf.Lerp(sbr.x, 400f * (max / GameRules.StaminaBase), dt * 6f), sbr.y);
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
                t += Time.unscaledDeltaTime * 2.0f;
                var c = fade.color; c.a = Mathf.Clamp01(t); fade.color = c;
                yield return null;
            }
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }
}
