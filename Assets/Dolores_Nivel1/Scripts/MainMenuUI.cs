using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dolores
{
    /// <summary>Menú principal: Jugar, Controles, Créditos y Salir.</summary>
    public class MainMenuUI : MonoBehaviour
    {
        public Sprite panelSprite;
        public Sprite buttonSprite;
        public Font font;
        public Font titleFont;
        public string levelScene = SceneNames.Level1;

        GameObject mainGroup, controlsPanel, creditsPanel;
        Button playButton, controlsBack, creditsBack;
        Image fade;
        bool loading;

        void Awake()
        {
            Time.timeScale = 1f;
            UIFactory.Font = font;
            UIFactory.PanelSprite = panelSprite;
            UIFactory.ButtonSprite = buttonSprite;
            UIFactory.EnsureEventSystem();
            Build();
        }

        void Start()
        {
            UIFactory.Select(playButton);
            StartCoroutine(FadeIn());
        }

        void Build()
        {
            var canvas = UIFactory.CreateCanvas("Menu", 10);
            canvas.transform.SetParent(transform, false);
            Transform root = canvas.transform;

            mainGroup = UIFactory.Rect("Principal", root).gameObject;
            UIFactory.Stretch(mainGroup.GetComponent<RectTransform>());
            Transform m = mainGroup.transform;

            var year = UIFactory.Label(m, "Anio", "1810", 250, UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Normal, true, titleFont);
            UIFactory.Place(year.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1400f, 260f));
            var title = UIFactory.Label(m, "Titulo", "EL GRITO DE LA LIBERTAD", 92, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal, true, titleFont);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -310f), new Vector2(1700f, 110f));
            var sub = UIFactory.Label(m, "Sub", "Dolores, Guanajuato · Nivel 1: La noche del 15 de septiembre", 38, UIFactory.Cream, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Place(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -410f), new Vector2(1700f, 60f));

            playButton = UIFactory.MakeButton(m, "Jugar", new Vector2(0f, -70f), new Vector2(520f, 100f), StartGame);
            UIFactory.MakeButton(m, "Controles", new Vector2(0f, -190f), new Vector2(520f, 100f), () => ShowPanel(controlsPanel, controlsBack));
            UIFactory.MakeButton(m, "Créditos", new Vector2(0f, -310f), new Vector2(520f, 100f), () => ShowPanel(creditsPanel, creditsBack));
            UIFactory.MakeButton(m, "Salir", new Vector2(0f, -430f), new Vector2(520f, 100f), Quit);

            controlsPanel = BuildInfoPanel(root, "Controles", "CONTROLES",
                "<b>Mover</b>: A / D o flechas\n<b>Saltar</b>: ESPACIO, W o flecha arriba\n<b>Atacar con el hacha</b>: J, Z, X o clic izquierdo\n<b>Pausa</b>: ESC o P\n\n" +
                "<b>Control</b>: stick o cruceta = mover · A = saltar · X o B = atacar · Start = pausa\n\n" +
                "<b>Objetivo</b>: cruza el pueblo, derrota a los soldados realistas y toca la campana de la parroquia. " +
                "Activa los banderines para guardar tu avance, recoge monedas y busca los tesoros escondidos.", out controlsBack);
            creditsPanel = BuildInfoPanel(root, "Créditos", "CRÉDITOS",
                "<b>1810: El Grito de la Libertad</b>\nNivel 1 hecho en Unity.\n\n" +
                "<b>Arte</b>: Pixel Crawler (personaje Body_A) y Craftpix.net (Fantasy Knight, Medieval Training Arena, Medieval Ruins, Tropical Medieval City).\n\n" +
                "<b>Fondos del pueblo, casas, parroquia, interfaz, música y efectos de sonido</b>: creados para este proyecto.", out creditsBack);

            fade = UIFactory.FullScreen(root, "Fundido", new Color(0f, 0f, 0f, 1f));
        }

        GameObject BuildInfoPanel(Transform root, string name, string title, string body, out Button back)
        {
            var overlay = UIFactory.FullScreen(root, name, new Color(0f, 0f, 0f, 0.65f));
            overlay.raycastTarget = true;
            Transform t = overlay.transform;
            UIFactory.Panel(t, "Panel", Vector2.zero, new Vector2(1240f, 860f));
            var ti = UIFactory.Label(t, "Titulo", title, 92, UIFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            UIFactory.Place(ti.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 340f), new Vector2(1100f, 110f));
            var tx = UIFactory.Label(t, "Texto", body, 40, UIFactory.Ink, TextAnchor.UpperLeft, FontStyle.Normal, false);
            UIFactory.Place(tx.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(1060f, 560f));
            back = UIFactory.MakeButton(t, "Volver", new Vector2(0f, -340f), new Vector2(420f, 90f), () => HidePanels());
            overlay.gameObject.SetActive(false);
            return overlay.gameObject;
        }

        void ShowPanel(GameObject panel, Button first)
        {
            panel.SetActive(true);
            UIFactory.Select(first);
        }

        void HidePanels()
        {
            controlsPanel.SetActive(false);
            creditsPanel.SetActive(false);
            UIFactory.Select(playButton);
        }

        void Update()
        {
            if ((controlsPanel.activeSelf || creditsPanel.activeSelf) && GameInput.PauseDown()) HidePanels();
        }

        void StartGame()
        {
            if (loading) return;
            loading = true;
            AudioHub.Play(SfxId.Checkpoint, 0f, 0.6f);
            StartCoroutine(FadeOutLoad(levelScene));
        }

        void Quit()
        {
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
            if (AudioHub.I != null) AudioHub.I.StopMusic();
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
