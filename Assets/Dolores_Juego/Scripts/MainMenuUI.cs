using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dolores1810
{
    /// <summary>Menú principal: Jugar, Continuar, Seleccionar nivel, Tienda, Configuración, Créditos y Salir.</summary>
    public class MainMenuUI : MonoBehaviour
    {
        public Sprite panelSprite;
        public Sprite buttonSprite;
        public Sprite coinSprite;
        public Sprite lockSprite;
        public Sprite checkSprite;
        public Font font;
        public Font titleFont;
        public AudioClip menuMusic;

        GameObject mainGroup, creditsPanel, levelPanel, controlsPanel;
        ShopUI shop;
        SettingsUI settings;
        Button playButton, continueButton, creditsBack, levelBack, controlsBack;
        Text coinsText;
        Image fade;
        bool loading;

        class Card { public Button button; public Image shade, lockIcon; public Text status; public int level; }
        readonly Card[] cards = new Card[4];

        void Awake()
        {
            Time.timeScale = 1f;
            UIFactory.Font = font;
            UIFactory.PanelSprite = panelSprite;
            UIFactory.ButtonSprite = buttonSprite;
            UIFactory.EnsureEventSystem();
            GameSettings.Apply();
            Build();
        }

        void Start()
        {
            var mm = MusicManager.Ensure();
            mm.RegisterGameplay(false);
            mm.Duck(1f);
            mm.SetSet(menuMusic, null, null, null, null, null);
            RefreshMain();
            UIFactory.Select(PlayOrContinue());
            StartCoroutine(FadeIn());
            PlayerProgress.Changed += RefreshMain;
        }

        void OnDestroy()
        {
            PlayerProgress.Changed -= RefreshMain;
        }

        Button PlayOrContinue()
        {
            return (continueButton != null && continueButton.interactable) ? continueButton : playButton;
        }

        void Build()
        {
            var canvas = UIFactory.CreateCanvas("Menu", 10);
            canvas.transform.SetParent(transform, false);
            Transform root = canvas.transform;

            mainGroup = UIFactory.Rect("Principal", root).gameObject;
            UIFactory.Stretch(mainGroup.GetComponent<RectTransform>());
            Transform m = mainGroup.transform;

            var year = UIFactory.Label(m, "Anio", "1810", 230, UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Normal, true, titleFont);
            UIFactory.PlaceCenter(year.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1400f, 250f));
            var title = UIFactory.Label(m, "Titulo", "EL GRITO DE LA LIBERTAD", 92, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal, true, titleFont);
            UIFactory.PlaceCenter(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -300f), new Vector2(1700f, 110f));
            var sub = UIFactory.Label(m, "Sub", "Dolores, Guanajuato · México, 1810", 42, UIFactory.Cream, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.PlaceCenter(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -380f), new Vector2(1700f, 54f));

            Vector2 sz = new Vector2(540f, 70f);
            playButton = UIFactory.MakeButton(m, "Jugar", new Vector2(0f, 100f), sz, () => { PlayerProgress.SetSavedLives(3); StartLevel(1); });
            continueButton = UIFactory.MakeButton(m, "Continuar partida", new Vector2(0f, 26f), sz, () => StartLevel(PlayerProgress.ContinueLevel()));
            UIFactory.MakeButton(m, "Seleccionar nivel", new Vector2(0f, -48f), sz, ShowLevels);
            UIFactory.MakeButton(m, "Tienda y mejoras", new Vector2(0f, -122f), sz, ShowShop);
            UIFactory.MakeButton(m, "Controles", new Vector2(0f, -196f), sz, ShowControls);
            UIFactory.MakeButton(m, "Configuración", new Vector2(0f, -270f), sz, ShowSettings);
            UIFactory.MakeButton(m, "Créditos", new Vector2(0f, -344f), sz, ShowCredits);
            UIFactory.MakeButton(m, "Salir", new Vector2(0f, -418f), sz, Quit);

            BuildControlsCard(m);

            UIFactory.Icon(m, "IconoMoneda", coinSprite, new Vector2(1f, 1f), new Vector2(-300f, -30f), new Vector2(54f, 54f));
            coinsText = UIFactory.Label(m, "Monedas", "x 0", 44, UIFactory.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(coinsText.rectTransform, new Vector2(1f, 1f), new Vector2(-236f, -30f), new Vector2(220f, 54f));

            BuildLevelPanel(root);
            controlsPanel = BuildInfoPanel(root, "PanelControles", "CONTROLES Y MECÁNICAS",
                "<b><color=#8A4B14>TECLADO Y RATÓN</color></b>\n" +
                "• <b>Moverse:</b> A / D o Flechas   ·   <b>Correr:</b> SHIFT (consume resistencia)\n" +
                "• <b>Saltar:</b> ESPACIO o W   ·   <b>Trepar escaleras:</b> W / S\n" +
                "• <b>Atacar:</b> J, Z, X o Clic izquierdo\n" +
                "• <b>Interactuar (tienda, cofres, palancas, puzzles):</b> E\n" +
                "• <b>Cambiar arma:</b> Q o teclas 1 - 5   ·   <b>Pausa:</b> ESC o P\n\n" +
                "<b><color=#8A4B14>CONTROL (GAMEPAD)</color></b>\n" +
                "• <b>Mover / Trepar:</b> Stick izquierdo o Cruceta   ·   <b>Saltar:</b> Botón A\n" +
                "• <b>Atacar:</b> X / B / RT   ·   <b>Correr:</b> LT / LB\n" +
                "• <b>Interactuar:</b> Y   ·   <b>Cambiar arma:</b> RB   ·   <b>Pausa:</b> START\n\n" +
                "<b><color=#8A4B14>CONSEJOS DE AVENTURA</color></b>\n" +
                "• Compra el <b>Escudo insurgente</b> en la tienda para absorber golpes enemigos.\n" +
                "• Esquiva trampas de fuego, pinchos móviles y resuelve los acertijos con E para abrir rejas con recompensas.", out controlsBack);
            var ctrlTxt = controlsPanel.transform.Find("Texto");
            if (ctrlTxt != null)
            {
                var tComp = ctrlTxt.GetComponent<Text>();
                if (tComp != null) tComp.fontSize = 31;
            }
            creditsPanel = BuildInfoPanel(root, "Creditos", "CRÉDITOS",
                "<b>1810: El Grito de la Libertad</b>\nJuego de plataformas en Unity.\n\n" +
                "<b>Arte</b>: personaje Body_A (Pixel Crawler) y paquetes de Craftpix.net: Fantasy Knight, Medieval Training Arena, " +
                "Medieval Ruins y Tropical Medieval City.\n\n" +
                "<b>Creado para este proyecto</b>: casas coloniales, parroquia, fondos, árboles, interfaz, iconos, armas animadas, música y efectos de sonido.\n\n" +
                "<b>Historia</b>: inspirada en el Grito de Dolores, 16 de septiembre de 1810.", out creditsBack);
            settings = SettingsUI.Create(root, OnSubBack, true);
            settings.gameObject.SetActive(false);
            shop = ShopUI.Create(root, OnSubBack);
            shop.gameObject.SetActive(false);

            fade = UIFactory.FullScreen(root, "Fundido", new Color(0f, 0f, 0f, 1f));
        }

        void BuildLevelPanel(Transform root)
        {
            var overlay = UIFactory.FullScreen(root, "Niveles", new Color(0f, 0f, 0f, 0.7f));
            overlay.raycastTarget = true;
            levelPanel = overlay.gameObject;
            Transform t = levelPanel.transform;
            UIFactory.Panel(t, "Panel", Vector2.zero, new Vector2(1780f, 900f));
            var title = UIFactory.Label(t, "Titulo", "SELECCIONAR NIVEL", 76, UIFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            UIFactory.PlaceCenter(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 385f), new Vector2(1400f, 100f));

            var db = GameDatabase.I;
            for (int i = 0; i < 4; i++)
            {
                int lv = i + 1;
                float x = -622.5f + i * 415f;
                var info = db != null ? db.GetLevel(lv) : null;
                var cardBack = UIFactory.Box(t, "Tarjeta" + lv, new Color(0.3f, 0.18f, 0.08f, 0.18f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(395f, 640f));
                UIFactory.PlaceCenter(cardBack.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, 20f), new Vector2(395f, 640f));
                var thumb = UIFactory.Icon(t, "Miniatura" + lv, info != null ? info.thumbnail : null, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 210f));
                UIFactory.PlaceCenter(thumb.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, 235f), new Vector2(360f, 210f));
                thumb.preserveAspect = false;
                var shade = UIFactory.Box(t, "Sombra" + lv, new Color(0f, 0f, 0f, 0.6f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 210f));
                UIFactory.PlaceCenter(shade.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, 235f), new Vector2(360f, 210f));
                var lockI = UIFactory.Icon(t, "Candado" + lv, lockSprite, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(105f, 105f));
                UIFactory.PlaceCenter(lockI.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, 235f), new Vector2(105f, 105f));
                var nm = UIFactory.Label(t, "Nombre" + lv, info != null ? info.title : "Nivel " + lv, 33, UIFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Bold, false);
                UIFactory.PlaceCenter(nm.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, 72f), new Vector2(376f, 86f));
                var ds = UIFactory.Label(t, "Descripcion" + lv, info != null ? info.description : "", 26, UIFactory.Ink, TextAnchor.UpperCenter, FontStyle.Bold, false);
                UIFactory.PlaceCenter(ds.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, -58f), new Vector2(372f, 170f));
                var st = UIFactory.Label(t, "Estado" + lv, "", 27, new Color(0.45f, 0.25f, 0.05f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold, false);
                UIFactory.PlaceCenter(st.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, -170f), new Vector2(376f, 50f));
                var btn = UIFactory.MakeButton(t, "Comenzar", new Vector2(x, -250f), new Vector2(330f, 76f), () => StartLevel(lv));
                var c = new Card(); c.button = btn; c.shade = shade; c.lockIcon = lockI; c.status = st; c.level = lv;
                cards[i] = c;
            }
            levelBack = UIFactory.MakeButton(t, "Volver", new Vector2(0f, -400f), new Vector2(360f, 80f), OnSubBack);
            levelPanel.SetActive(false);
        }

        void BuildControlsCard(Transform parent)
        {
            var card = UIFactory.Box(parent, "ControlesResumen", new Color(0.14f, 0.08f, 0.04f, 0.90f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 520f));
            UIFactory.PlaceCenter(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-620f, -170f), new Vector2(600f, 520f));
            UIFactory.Box(card.transform, "BordeSup", new Color(0.92f, 0.72f, 0.24f, 0.9f), new Vector2(0f, 1f), Vector2.zero, new Vector2(600f, 3f));
            UIFactory.Box(card.transform, "BordeIzq", new Color(0.92f, 0.72f, 0.24f, 0.9f), new Vector2(0f, 0f), Vector2.zero, new Vector2(5f, 520f));

            var hdr = UIFactory.Label(card.transform, "Titulo", "CONTROLES", 36, UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            UIFactory.Place(hdr.rectTransform, new Vector2(0f, 1f), new Vector2(16f, -12f), new Vector2(568f, 46f));
            UIFactory.Box(card.transform, "Linea", new Color(0.92f, 0.72f, 0.24f, 0.5f), new Vector2(0f, 1f), new Vector2(20f, -62f), new Vector2(560f, 2f));

            string[] lines =
            {
                "<color=#FFD152><b>Moverse:</b></color>  [A / D] o [Flechas] · Stick",
                "<color=#FFD152><b>Correr:</b></color>  [SHIFT] (resistencia) · [LT/LB]",
                "<color=#FFD152><b>Saltar:</b></color>  [ESPACIO] / [W] · Botón [A]",
                "<color=#FFD152><b>Atacar:</b></color>  [J / Z / X] o [Clic] · [X / B]",
                "<color=#FFD152><b>Interactuar:</b></color>  [E] (tienda/puzzles) · [Y]",
                "<color=#FFD152><b>Escalar:</b></color>  [W / S] (en escaleras) · Stick",
                "<color=#FFD152><b>Cambiar arma:</b></color>  [Q] o [1 - 5] · [RB]",
                "<color=#FFD152><b>Pausa:</b></color>  [ESC / P] · [START]"
            };
            for (int i = 0; i < lines.Length; i++)
            {
                var row = UIFactory.Label(card.transform, "Ctrl_" + i, lines[i], 26, UIFactory.Cream, TextAnchor.MiddleLeft, FontStyle.Bold, false);
                UIFactory.Place(row.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -74f - i * 53f), new Vector2(556f, 46f));
            }
        }

        GameObject BuildInfoPanel(Transform root, string name, string title, string body, out Button back)
        {
            var overlay = UIFactory.FullScreen(root, name, new Color(0f, 0f, 0f, 0.65f));
            overlay.raycastTarget = true;
            Transform t = overlay.transform;
            UIFactory.Panel(t, "Panel", Vector2.zero, new Vector2(1280f, 880f));
            var ti = UIFactory.Label(t, "Titulo", title, 90, UIFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            UIFactory.PlaceCenter(ti.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 350f), new Vector2(1100f, 110f));
            var tx = UIFactory.Label(t, "Texto", body, 36, UIFactory.Ink, TextAnchor.UpperLeft, FontStyle.Normal, false);
            UIFactory.PlaceCenter(tx.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(1100f, 560f));
            back = UIFactory.MakeButton(t, "Volver", new Vector2(0f, -350f), new Vector2(420f, 90f), OnSubBack);
            overlay.gameObject.SetActive(false);
            return overlay.gameObject;
        }

        void RefreshMain()
        {
            if (coinsText != null) coinsText.text = "x " + PlayerProgress.Coins;
            if (continueButton != null) continueButton.interactable = PlayerProgress.Data.hasStarted;
            for (int i = 0; i < cards.Length; i++)
            {
                var c = cards[i];
                if (c == null) continue;
                bool unlocked = PlayerProgress.IsLevelUnlocked(c.level);
                c.button.interactable = unlocked;
                c.shade.gameObject.SetActive(!unlocked);
                c.lockIcon.gameObject.SetActive(!unlocked);
                if (!unlocked) c.status.text = "BLOQUEADO · Completa el nivel " + (c.level - 1);
                else c.status.text = PlayerProgress.IsCompleted(c.level) ? "COMPLETADO" : "DISPONIBLE";
            }
        }

        void ShowLevels()
        {
            RefreshMain();
            levelPanel.SetActive(true);
            Button first = cards[0].button;
            for (int i = cards.Length - 1; i >= 0; i--) if (cards[i].button.interactable) { first = cards[i].button; break; }
            UIFactory.Select(first);
        }

        void ShowShop() { shop.Show(); }
        void ShowSettings() { settings.Show(); }
        void ShowControls() { controlsPanel.SetActive(true); UIFactory.Select(controlsBack); }
        void ShowCredits() { creditsPanel.SetActive(true); UIFactory.Select(creditsBack); }

        void OnSubBack()
        {
            creditsPanel.SetActive(false);
            controlsPanel.SetActive(false);
            levelPanel.SetActive(false);
            if (settings != null) settings.gameObject.SetActive(false);
            if (shop != null) shop.gameObject.SetActive(false);
            RefreshMain();
            UIFactory.Select(PlayOrContinue());
        }

        void Update()
        {
            bool sub = creditsPanel.activeSelf || controlsPanel.activeSelf || levelPanel.activeSelf || settings.gameObject.activeSelf || shop.gameObject.activeSelf;
            if (sub && GameInput.PauseDown()) OnSubBack();
        }

        void StartLevel(int level)
        {
            if (loading || !PlayerProgress.IsLevelUnlocked(level)) return;
            loading = true;
            AudioHub.Play(SfxId.Checkpoint, 0f, 0.6f);
            StartCoroutine(FadeOutLoad(SceneNames.ForLevel(level)));
        }

        void Quit()
        {
            PlayerProgress.SaveNow();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        IEnumerator FadeIn()
        {
            float t = 1f;
            while (t > 0f)
            {
                t -= Time.unscaledDeltaTime * 2f;
                var c = fade.color; c.a = Mathf.Clamp01(t); fade.color = c;
                yield return null;
            }
        }

        IEnumerator FadeOutLoad(string scene)
        {
            fade.raycastTarget = true;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * 1.6f;
                var c = fade.color; c.a = Mathf.Clamp01(t); fade.color = c;
                yield return null;
            }
            SceneManager.LoadScene(scene);
        }
    }
}
