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
        public Sprite shieldSprite;
        public Sprite scrollSprite;
        public Sprite coinSprite;
        public Sprite panelSprite;
        public Sprite buttonSprite;
        public Font font;
        public Font titleFont;

        class MissionCardUI
        {
            public GameObject root;
            public Image cardBg, accent, iconBg, icon, barFill;
            public Text catText, rewardText, titleText, progressText;
        }

        class HudMissionRowUI
        {
            public GameObject root;
            public Image barFill;
            public Text badgeText, titleText, countText;
        }

        Canvas canvas;
        Image[] hearts;
        Image[] shields;
        Image staminaFill, weaponIcon, bossFill;
        Text livesText, coinsText, scoreText, timeText, zoneTitle, zoneSub, toastText, hintText, objectiveText, weaponText, promptText;
        Text bossName, introTitle, introSub, exitText, victoryStats, pauseMissionSummary, hudMissionSummary;
        CanvasGroup zoneGroup, toastGroup, hintGroup, promptGroup, bossGroup, introGroup, exitGroup, objectiveGroup, hudMissionsGroup;
        RectTransform barTop, barBottom, staminaBack;
        GameObject pausePanel, gameOverPanel, victoryPanel;
        MissionCardUI[] pauseMissionCards;
        HudMissionRowUI[] hudMissionRows;
        Sprite cachedShieldSprite, cachedScrollSprite;
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
            UIFactory.Place(scoreText.rectTransform, new Vector2(1f, 1f), new Vector2(-44f, -38f), new Vector2(700f, 54f));
            timeText = UIFactory.Label(root, "Tiempo", "00:00", 32, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleRight, FontStyle.Bold);
            UIFactory.Place(timeText.rectTransform, new Vector2(1f, 1f), new Vector2(-44f, -94f), new Vector2(400f, 38f));

            // Tracker de misiones en HUD
            var hmr = UIFactory.Rect("MisionesHUD", root);
            UIFactory.Place(hmr, new Vector2(1f, 1f), new Vector2(-36f, -148f), new Vector2(456f, 232f));
            hudMissionsGroup = Group(hmr, 0f);
            UIFactory.FullScreen(hmr, "Fondo", new Color(0.12f, 0.07f, 0.04f, 0.78f));
            UIFactory.Box(hmr, "Borde", new Color(0.92f, 0.72f, 0.24f, 0.9f), new Vector2(0f, 0f), Vector2.zero, new Vector2(4f, 232f));
            var hmh = UIFactory.Label(hmr, "Titulo", "MISIONES DEL NIVEL", 20, UIFactory.Gold, TextAnchor.MiddleLeft, FontStyle.Bold, false);
            UIFactory.Place(hmh.rectTransform, new Vector2(0f, 1f), new Vector2(14f, -6f), new Vector2(320f, 30f));
            hudMissionSummary = UIFactory.Label(hmr, "Conteo", "0/4", 20, UIFactory.Cream, TextAnchor.MiddleRight, FontStyle.Bold, false);
            UIFactory.Place(hudMissionSummary.rectTransform, new Vector2(1f, 1f), new Vector2(-12f, -6f), new Vector2(100f, 30f));
            UIFactory.Box(hmr, "Linea", new Color(0.92f, 0.72f, 0.24f, 0.55f), new Vector2(0f, 1f), new Vector2(12f, -38f), new Vector2(432f, 1.5f));
            hudMissionRows = new HudMissionRowUI[4];
            for (int i = 0; i < 4; i++)
            {
                var rowRt = UIFactory.Rect("Fila_" + i, hmr);
                UIFactory.Place(rowRt, new Vector2(0f, 1f), new Vector2(12f, -44f - i * 45f), new Vector2(432f, 42f));
                var row = new HudMissionRowUI();
                row.root = rowRt.gameObject;
                row.badgeText = UIFactory.Label(rowRt, "Estado", "[•]", 19, UIFactory.Gold, TextAnchor.MiddleLeft, FontStyle.Bold, false);
                UIFactory.Place(row.badgeText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, -1f), new Vector2(36f, 25f));
                row.titleText = UIFactory.Label(rowRt, "Nombre", "", 19, UIFactory.Cream, TextAnchor.MiddleLeft, FontStyle.Bold, false);
                UIFactory.Place(row.titleText.rectTransform, new Vector2(0f, 1f), new Vector2(36f, -1f), new Vector2(326f, 25f));
                row.countText = UIFactory.Label(rowRt, "Progreso", "", 18, UIFactory.Gold, TextAnchor.MiddleRight, FontStyle.Bold, false);
                UIFactory.Place(row.countText.rectTransform, new Vector2(1f, 1f), new Vector2(0f, -1f), new Vector2(68f, 25f));
                Image hbf;
                UIFactory.MakeBar(rowRt, "Barra", UIFactory.Gold, new Vector2(0f, 1f), new Vector2(0f, -28f), new Vector2(432f, 11f), out hbf);
                row.barFill = hbf;
                hudMissionRows[i] = row;
            }

            // Objetivo
            var orr = UIFactory.Rect("Objetivo", root);
            UIFactory.PlaceCenter(orr, new Vector2(0.5f, 1f), new Vector2(0f, -47f), new Vector2(640f, 54f));
            objectiveGroup = Group(orr, 0f);
            UIFactory.FullScreen(orr, "Fondo", new Color(0.14f, 0.08f, 0.04f, 0.84f));
            UIFactory.Box(orr, "BordeSup", new Color(0.95f, 0.76f, 0.28f, 0.9f), new Vector2(0f, 1f), Vector2.zero, new Vector2(640f, 2.5f));
            UIFactory.Box(orr, "BordeInf", new Color(0.95f, 0.76f, 0.28f, 0.9f), new Vector2(0f, 0f), Vector2.zero, new Vector2(640f, 2.5f));
            objectiveText = UIFactory.Label(orr, "Texto", "", 27, new Color(1f, 0.95f, 0.72f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold, false);
            UIFactory.Stretch(objectiveText.rectTransform);
            objectiveText.rectTransform.offsetMin = new Vector2(18f, 2f);
            objectiveText.rectTransform.offsetMax = new Vector2(-18f, -2f);

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

        Sprite GetShieldSprite()
        {
            if (shieldSprite != null) return shieldSprite;
            if (cachedShieldSprite != null) return cachedShieldSprite;
            var db = GameDatabase.I;
            var sh = db != null ? db.FindUpgrade(ShopKind.Shield) : null;
            if (sh != null && sh.icon != null) cachedShieldSprite = sh.icon;
            else cachedShieldSprite = heartSprite;
            return cachedShieldSprite;
        }

        Sprite GetScrollSprite()
        {
            if (scrollSprite != null) return scrollSprite;
            if (cachedScrollSprite != null) return cachedScrollSprite;
            var col = FindAnyObjectByType<Collectible>();
            if (col != null)
            {
                var sr = col.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null) cachedScrollSprite = sr.sprite;
            }
            if (cachedScrollSprite == null) cachedScrollSprite = coinSprite;
            return cachedScrollSprite;
        }

        Sprite GetMissionIcon(MissionKind kind)
        {
            var db = GameDatabase.I;
            switch (kind)
            {
                case MissionKind.Kills:
                    var esp = db != null ? db.GetWeapon("espada") : null;
                    return esp != null && esp.icon != null ? esp.icon : coinSprite;
                case MissionKind.Break:
                    var hac = db != null ? db.GetWeapon("hacha") : null;
                    return hac != null && hac.icon != null ? hac.icon : coinSprite;
                case MissionKind.Chests:
                    return coinSprite;
                case MissionKind.Collect:
                    return GetScrollSprite();
            }
            return coinSprite;
        }

        static string MissionCategoryName(MissionKind kind)
        {
            switch (kind)
            {
                case MissionKind.Kills: return "COMBATE";
                case MissionKind.Break: return "DESTRUCCIÓN";
                case MissionKind.Chests: return "TESOROS";
                case MissionKind.Collect: return "HISTORIA";
            }
            return "MISIÓN";
        }

        void BuildHearts()
        {
            int n = 7;
            hearts = new Image[n];
            for (int i = 0; i < n; i++)
            {
                hearts[i] = UIFactory.Icon(canvas.transform, "Corazon" + i, heartSprite, new Vector2(0f, 1f), new Vector2(36f + i * 56f, -34f), new Vector2(52f, 52f));
                hearts[i].gameObject.SetActive(false);
            }
            shields = new Image[GameRules.MaxShieldUpgrades];
            Sprite shSp = GetShieldSprite();
            for (int i = 0; i < shields.Length; i++)
            {
                shields[i] = UIFactory.Icon(canvas.transform, "Escudo" + i, shSp, new Vector2(0f, 1f), new Vector2(36f + i * 52f, -36f), new Vector2(48f, 48f));
                shields[i].gameObject.SetActive(false);
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
            pausePanel = MakeOverlay(root, "Pausa", new Color(0f, 0f, 0f, 0.68f));
            Transform t = pausePanel.transform;
            UIFactory.Panel(t, "Panel", Vector2.zero, new Vector2(1380f, 900f));
            var title = UIFactory.Label(t, "Titulo", "PAUSA", 104, UIFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Normal, false, titleFont);
            UIFactory.PlaceCenter(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 355f), new Vector2(900f, 120f));
            pauseFirst = UIFactory.MakeButton(t, "Continuar", new Vector2(-400f, 180f), new Vector2(480f, 92f), () => GameSession.I.SetPaused(false));
            UIFactory.MakeButton(t, "Reiniciar nivel", new Vector2(-400f, 65f), new Vector2(480f, 92f), () => GameSession.I.Restart());
            UIFactory.MakeButton(t, "Configuración", new Vector2(-400f, -50f), new Vector2(480f, 92f), OpenSettings);
            UIFactory.MakeButton(t, "Menú principal", new Vector2(-400f, -165f), new Vector2(480f, 92f), () => GameSession.I.GoMenu());

            var board = UIFactory.Box(t, "TableroMisiones", new Color(0.22f, 0.13f, 0.06f, 0.95f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(710f, 545f));
            UIFactory.PlaceCenter(board.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(245f, 10f), new Vector2(710f, 545f));
            var header = UIFactory.Box(board.transform, "Encabezado", new Color(0.14f, 0.08f, 0.04f, 0.98f), new Vector2(0f, 1f), Vector2.zero, new Vector2(710f, 56f));
            UIFactory.Box(header.transform, "LineaOro", new Color(0.9f, 0.7f, 0.25f, 0.85f), new Vector2(0f, 0f), Vector2.zero, new Vector2(710f, 3f));
            var mh = UIFactory.Label(header.transform, "MisionesTitulo", "MISIONES DEL NIVEL", 30, UIFactory.Gold, TextAnchor.MiddleLeft, FontStyle.Bold, false);
            UIFactory.Place(mh.rectTransform, new Vector2(0f, 1f), new Vector2(20f, -6f), new Vector2(410f, 44f));
            pauseMissionSummary = UIFactory.Label(header.transform, "Resumen", "0 / 4 COMPLETADAS", 22, UIFactory.Cream, TextAnchor.MiddleRight, FontStyle.Bold, false);
            UIFactory.Place(pauseMissionSummary.rectTransform, new Vector2(1f, 1f), new Vector2(-18f, -6f), new Vector2(260f, 44f));

            pauseMissionCards = new MissionCardUI[4];
            for (int i = 0; i < 4; i++)
            {
                var cardBg = UIFactory.Box(board.transform, "TarjetaMision_" + i, new Color(0.31f, 0.19f, 0.1f, 0.96f), new Vector2(0.5f, 1f), new Vector2(0f, -68f - i * 118f), new Vector2(674f, 108f));
                var c = new MissionCardUI();
                c.root = cardBg.gameObject;
                c.cardBg = cardBg;
                c.accent = UIFactory.Box(cardBg.transform, "Franja", UIFactory.Gold, new Vector2(0f, 0f), Vector2.zero, new Vector2(8f, 108f));
                c.iconBg = UIFactory.Box(cardBg.transform, "IconoFondo", new Color(0.15f, 0.08f, 0.04f, 0.9f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(76f, 76f));
                UIFactory.PlaceCenter(c.iconBg.rectTransform, new Vector2(0f, 0.5f), new Vector2(58f, 0f), new Vector2(76f, 76f));
                c.icon = UIFactory.Icon(c.iconBg.transform, "Icono", null, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(58f, 58f));
                UIFactory.PlaceCenter(c.icon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(58f, 58f));
                c.catText = UIFactory.Label(cardBg.transform, "Categoria", "", 18, UIFactory.Gold, TextAnchor.MiddleLeft, FontStyle.Bold, false);
                UIFactory.Place(c.catText.rectTransform, new Vector2(0f, 1f), new Vector2(110f, -8f), new Vector2(250f, 24f));
                c.rewardText = UIFactory.Label(cardBg.transform, "Recompensa", "", 19, UIFactory.Gold, TextAnchor.MiddleRight, FontStyle.Bold, false);
                UIFactory.Place(c.rewardText.rectTransform, new Vector2(1f, 1f), new Vector2(-16f, -8f), new Vector2(270f, 24f));
                c.titleText = UIFactory.Label(cardBg.transform, "Titulo", "", 25, UIFactory.Cream, TextAnchor.MiddleLeft, FontStyle.Bold, false);
                UIFactory.Place(c.titleText.rectTransform, new Vector2(0f, 1f), new Vector2(110f, -34f), new Vector2(545f, 34f));
                Image pbf;
                UIFactory.MakeBar(cardBg.transform, "Barra", UIFactory.Gold, new Vector2(0f, 1f), new Vector2(110f, -74f), new Vector2(440f, 18f), out pbf);
                c.barFill = pbf;
                c.progressText = UIFactory.Label(cardBg.transform, "Progreso", "", 22, UIFactory.Cream, TextAnchor.MiddleRight, FontStyle.Bold, false);
                UIFactory.Place(c.progressText.rectTransform, new Vector2(1f, 1f), new Vector2(-16f, -68f), new Vector2(104f, 30f));
                pauseMissionCards[i] = c;
            }

            var tip = UIFactory.Label(t, "Controles", "A/D mover · SHIFT correr · ESPACIO saltar · J atacar\nE interactuar · W/S trepar · Q o 1-5 cambiar arma", 28, UIFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            UIFactory.PlaceCenter(tip.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -355f), new Vector2(1240f, 96f));
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
            if (shields != null)
            {
                int curS = pl != null ? pl.Shield : 0;
                int maxS = pl != null ? pl.MaxShield : PlayerStats.MaxShield;
                for (int i = 0; i < shields.Length; i++)
                {
                    bool visible = i < maxS;
                    shields[i].gameObject.SetActive(visible);
                    if (visible)
                    {
                        shields[i].rectTransform.anchoredPosition = new Vector2(36f + max * 56f + 10f + i * 52f, -36f);
                        shields[i].color = i < curS ? new Color(0.75f, 0.92f, 1f, 1f) : new Color(0.2f, 0.25f, 0.35f, 0.55f);
                    }
                }
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
            RefreshMissions(gs);
        }

        void RefreshMissions(GameSession gs)
        {
            var list = gs != null ? gs.missions : null;
            int total = list != null ? list.Length : 0;
            int doneCount = gs != null ? gs.MissionsDoneCount() : 0;
            Color greenAccent = new Color(0.38f, 0.92f, 0.48f, 1f);
            Color greenText = new Color(0.72f, 1f, 0.74f, 1f);
            Color activeBg = new Color(0.31f, 0.19f, 0.1f, 0.96f);
            Color doneBg = new Color(0.14f, 0.3f, 0.17f, 0.96f);

            if (hudMissionSummary != null) hudMissionSummary.text = doneCount + "/" + total;
            if (hudMissionRows != null)
            {
                for (int i = 0; i < hudMissionRows.Length; i++)
                {
                    var r = hudMissionRows[i];
                    if (r == null) continue;
                    bool show = i < total && list[i] != null;
                    r.root.SetActive(show);
                    if (!show) continue;
                    var m = list[i];
                    float frac = m.done ? 1f : (m.target > 0 ? Mathf.Clamp01((float)m.progress / m.target) : 0f);
                    r.badgeText.text = m.done ? "[✓]" : "[•]";
                    r.badgeText.color = m.done ? greenAccent : UIFactory.Gold;
                    r.titleText.text = m.text;
                    r.titleText.color = m.done ? greenText : UIFactory.Cream;
                    r.countText.text = m.done ? "LISTA" : (m.progress + "/" + m.target);
                    r.countText.color = m.done ? greenAccent : UIFactory.Gold;
                    r.barFill.color = m.done ? greenAccent : UIFactory.Gold;
                    UIFactory.SetBar(r.barFill, frac);
                }
            }

            if (pauseMissionSummary != null) pauseMissionSummary.text = doneCount + " / " + total + " COMPLETADAS";
            if (pauseMissionCards != null)
            {
                for (int i = 0; i < pauseMissionCards.Length; i++)
                {
                    var c = pauseMissionCards[i];
                    if (c == null) continue;
                    bool show = i < total && list[i] != null;
                    c.root.SetActive(show);
                    if (!show) continue;
                    var m = list[i];
                    float frac = m.done ? 1f : (m.target > 0 ? Mathf.Clamp01((float)m.progress / m.target) : 0f);
                    c.cardBg.color = m.done ? doneBg : activeBg;
                    c.accent.color = m.done ? greenAccent : UIFactory.Gold;
                    c.icon.sprite = GetMissionIcon(m.kind);
                    c.icon.color = m.done ? greenText : Color.white;
                    c.catText.text = MissionCategoryName(m.kind);
                    c.catText.color = m.done ? greenAccent : UIFactory.Gold;
                    c.rewardText.text = m.done ? "¡COMPLETADA!" : ("+" + m.reward + " monedas");
                    c.rewardText.color = m.done ? greenAccent : UIFactory.Gold;
                    c.titleText.text = m.text;
                    c.titleText.color = m.done ? greenText : UIFactory.Cream;
                    c.barFill.color = m.done ? greenAccent : UIFactory.Gold;
                    UIFactory.SetBar(c.barFill, frac);
                    c.progressText.text = m.done ? (m.target + " / " + m.target) : (m.progress + " / " + m.target);
                    c.progressText.color = m.done ? greenAccent : UIFactory.Cream;
                }
            }
        }

        void OnStateChanged()
        {
            var gs = GameSession.I;
            if (gs == null) return;
            pausePanel.SetActive(gs.State == GameState.Paused && !settings.gameObject.activeSelf);
            if (gs.State == GameState.Paused) RefreshMissions(gs);
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
        }

        void OnBossHealth(int cur, int max)
        {
            UIFactory.SetBar(bossFill, max > 0 ? (float)cur / max : 0f);
        }

        void OnBossEnd() { }

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
            if (hudMissionsGroup != null)
            {
                bool hasMissions = gs != null && gs.missions != null && gs.missions.Length > 0;
                float targetHudMissions = playing && hasMissions ? (zoneTimer > 0f ? 0.35f : 1f) : 0f;
                hudMissionsGroup.alpha = Mathf.MoveTowards(hudMissionsGroup.alpha, targetHudMissions, dt * 3f);
            }
            bossGroup.alpha = 0f;

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
