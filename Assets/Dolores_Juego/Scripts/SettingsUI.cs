using UnityEngine;
using UnityEngine.UI;

namespace Dolores1810
{
    /// <summary>Configuración de audio (general, música, efectos) y borrado de progreso.</summary>
    public class SettingsUI : MonoBehaviour
    {
        Text masterText, musicText, sfxText;
        Slider masterSlider;
        Button resetButton, backButton;
        bool confirmReset;
        System.Action onBack;

        public static SettingsUI Create(Transform parent, System.Action onBack, bool allowReset)
        {
            var overlay = UIFactory.FullScreen(parent, "Configuracion", new Color(0f, 0f, 0f, 0.7f));
            overlay.raycastTarget = true;
            var ui = overlay.gameObject.AddComponent<SettingsUI>();
            ui.onBack = onBack;
            ui.Build(allowReset);
            return ui;
        }

        void Build(bool allowReset)
        {
            Transform t = transform;
            UIFactory.Panel(t, "Panel", Vector2.zero, new Vector2(1360f, 840f));
            var title = UIFactory.Label(t, "Titulo", "CONFIGURACIÓN", 76, UIFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            UIFactory.PlaceCenter(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(1100f, 100f));

            masterSlider = AddRow(t, "Volumen general", 190f, GameSettings.Master, v => { GameSettings.SetMaster(v); UpdateTexts(); }, out masterText);
            Text mt; AddRow(t, "Música", 60f, GameSettings.Music, v => { GameSettings.SetMusic(v); UpdateTexts(); }, out mt); musicText = mt;
            Text st; AddRow(t, "Efectos de sonido", -70f, GameSettings.Sfx, v => { GameSettings.SetSfx(v); UpdateTexts(); AudioHub.Play(SfxId.Coin, 0f); }, out st); sfxText = st;

            if (allowReset)
            {
                resetButton = UIFactory.MakeButton(t, "Borrar progreso", new Vector2(-250f, -270f), new Vector2(440f, 90f), OnReset);
            }
            backButton = UIFactory.MakeButton(t, "Volver", new Vector2(allowReset ? 250f : 0f, -270f), new Vector2(380f, 90f), Back);
            UpdateTexts();
        }

        Slider AddRow(Transform t, string label, float y, float value, UnityEngine.Events.UnityAction<float> onChange, out Text valueText)
        {
            var l = UIFactory.Label(t, label, label, 40, UIFactory.Ink, TextAnchor.MiddleLeft, FontStyle.Bold, false);
            UIFactory.PlaceCenter(l.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-200f, y + 36f), new Vector2(560f, 50f));
            var s = UIFactory.MakeSlider(t, new Vector2(-90f, y - 14f), 780f, value, onChange);
            valueText = UIFactory.Label(t, label + "_valor", "", 40, UIFactory.Ink, TextAnchor.MiddleRight, FontStyle.Bold, false);
            UIFactory.PlaceCenter(valueText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(410f, y - 14f), new Vector2(140f, 50f));
            return s;
        }

        void UpdateTexts()
        {
            if (masterText != null) masterText.text = Mathf.RoundToInt(GameSettings.Master * 100f) + "%";
            if (musicText != null) musicText.text = Mathf.RoundToInt(GameSettings.Music * 100f) + "%";
            if (sfxText != null) sfxText.text = Mathf.RoundToInt(GameSettings.Sfx * 100f) + "%";
        }

        void OnReset()
        {
            if (!confirmReset)
            {
                confirmReset = true;
                UIFactory.SetButtonText(resetButton, "¿Seguro? Pulsa otra vez");
                return;
            }
            confirmReset = false;
            PlayerProgress.ResetAll();
            UIFactory.SetButtonText(resetButton, "Progreso borrado");
        }

        void OnEnable()
        {
            confirmReset = false;
            if (resetButton != null) UIFactory.SetButtonText(resetButton, "Borrar progreso");
            if (masterSlider != null && EventSystemReady()) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(masterSlider.gameObject);
        }

        static bool EventSystemReady() { return UnityEngine.EventSystems.EventSystem.current != null; }

        public void Show() { gameObject.SetActive(true); }

        void Back()
        {
            gameObject.SetActive(false);
            if (onBack != null) onBack();
        }
    }
}
