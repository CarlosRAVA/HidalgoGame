using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Dolores1810
{
    /// <summary>Ayudas para construir interfaz (uGUI) desde código con el estilo del juego.</summary>
    public static class UIFactory
    {
        public static Font Font;
        public static Sprite PanelSprite;
        public static Sprite ButtonSprite;

        public static readonly Color Ink = new Color(0.23f, 0.13f, 0.07f, 1f);
        public static readonly Color Cream = new Color(1f, 0.95f, 0.8f, 1f);
        public static readonly Color Gold = new Color(1f, 0.82f, 0.32f, 1f);

        public static Font GetFont()
        {
            if (Font != null) return Font;
            Font f = null;
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { f = null; }
            if (f == null) f = Font.CreateDynamicFontFromOSFont("Arial", 32);
            return f;
        }

        public static Canvas CreateCanvas(string name, int sortOrder)
        {
            var go = new GameObject(name);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = sortOrder;
            var s = go.AddComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920f, 1080f);
            s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            s.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        /// <summary>Coloca con anclaje y pivote en el centro del rectángulo.</summary>
        public static void PlaceCenter(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static Image FullScreen(Transform parent, string name, Color color)
        {
            var rt = Rect(name, parent);
            Stretch(rt);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Image Box(Transform parent, string name, Color color, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var rt = Rect(name, parent);
            Place(rt, anchor, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Image Panel(Transform parent, string name, Vector2 center, Vector2 size)
        {
            var rt = Rect(name, parent);
            PlaceCenter(rt, new Vector2(0.5f, 0.5f), center, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = PanelSprite;
            img.type = PanelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            img.color = Color.white;
            return img;
        }

        public static Image Icon(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var rt = Rect(name, parent);
            Place(rt, anchor, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        public static Text Label(Transform parent, string name, string text, int size, Color color, TextAnchor align,
            FontStyle style = FontStyle.Normal, bool outline = true, Font fontOverride = null)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = fontOverride != null ? fontOverride : GetFont();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            if (outline)
            {
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0.1f, 0.05f, 0.02f, 0.9f);
                o.effectDistance = new Vector2(2.5f, -2.5f);
            }
            return t;
        }

        public static Button MakeButton(Transform parent, string label, Vector2 center, Vector2 size, UnityAction onClick)
        {
            var rt = Rect("Boton_" + label, parent);
            PlaceCenter(rt, new Vector2(0.5f, 0.5f), center, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = ButtonSprite;
            img.type = ButtonSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            img.color = Color.white;
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1f, 0.92f, 0.72f, 1f);
            cb.selectedColor = new Color(1f, 0.88f, 0.55f, 1f);
            cb.pressedColor = new Color(0.78f, 0.68f, 0.58f, 1f);
            cb.disabledColor = new Color(0.45f, 0.42f, 0.4f, 0.8f);
            cb.fadeDuration = 0.08f;
            b.colors = cb;
            var t = Label(rt, "Texto", label, 40, Cream, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(t.rectTransform);
            rt.gameObject.AddComponent<UIButtonSounds>();
            b.onClick.AddListener(() => AudioHub.Play(SfxId.Click, 0f));
            if (onClick != null) b.onClick.AddListener(onClick);
            return b;
        }

        public static void SetButtonText(Button b, string text)
        {
            if (b == null) return;
            var t = b.GetComponentInChildren<Text>();
            if (t != null) t.text = text;
        }

        public static Slider MakeSlider(Transform parent, Vector2 center, float width, float value, UnityAction<float> onChange)
        {
            var rt = Rect("Deslizador", parent);
            PlaceCenter(rt, new Vector2(0.5f, 0.5f), center, new Vector2(width, 44f));
            var bg = Rect("Fondo", rt);
            bg.anchorMin = new Vector2(0f, 0.3f); bg.anchorMax = new Vector2(1f, 0.7f); bg.offsetMin = Vector2.zero; bg.offsetMax = Vector2.zero;
            var bgi = bg.gameObject.AddComponent<Image>();
            bgi.color = new Color(0.2f, 0.12f, 0.07f, 0.95f);
            var fa = Rect("AreaRelleno", rt);
            fa.anchorMin = new Vector2(0f, 0.3f); fa.anchorMax = new Vector2(1f, 0.7f); fa.offsetMin = new Vector2(6f, 0f); fa.offsetMax = new Vector2(-6f, 0f);
            var fill = Rect("Relleno", fa);
            fill.anchorMin = new Vector2(0f, 0f); fill.anchorMax = new Vector2(0f, 1f); fill.offsetMin = Vector2.zero; fill.offsetMax = Vector2.zero;
            var fi = fill.gameObject.AddComponent<Image>();
            fi.color = new Color(0.9f, 0.55f, 0.2f, 1f);
            var ha = Rect("AreaAgarre", rt);
            ha.anchorMin = Vector2.zero; ha.anchorMax = Vector2.one; ha.offsetMin = new Vector2(16f, 0f); ha.offsetMax = new Vector2(-16f, 0f);
            var handle = Rect("Agarre", ha);
            handle.sizeDelta = new Vector2(30f, 44f);
            var hi = handle.gameObject.AddComponent<Image>();
            hi.color = Cream;
            var s = rt.gameObject.AddComponent<Slider>();
            s.fillRect = fill;
            s.handleRect = handle;
            s.targetGraphic = hi;
            s.direction = Slider.Direction.LeftToRight;
            s.minValue = 0f; s.maxValue = 1f;
            s.value = value;
            if (onChange != null) s.onValueChanged.AddListener(onChange);
            return s;
        }

        /// <summary>Barra horizontal hecha con dos rectángulos; el relleno se ajusta con SetBar.</summary>
        public static RectTransform MakeBar(Transform parent, string name, Color fillColor, Vector2 anchor, Vector2 pos, Vector2 size, out Image fillImage)
        {
            var back = Rect(name, parent);
            Place(back, anchor, pos, size);
            var bi = back.gameObject.AddComponent<Image>();
            bi.color = new Color(0.05f, 0.03f, 0.02f, 0.75f);
            bi.raycastTarget = false;
            var fill = Rect("Relleno", back);
            fill.anchorMin = new Vector2(0f, 0f); fill.anchorMax = new Vector2(1f, 1f);
            fill.offsetMin = new Vector2(3f, 3f); fill.offsetMax = new Vector2(-3f, -3f);
            fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = fillColor;
            fillImage.raycastTarget = false;
            return back;
        }

        public static void SetBar(Image fill, float v)
        {
            if (fill == null) return;
            var rt = fill.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(Mathf.Clamp01(v), 1f);
            rt.offsetMin = new Vector2(3f, 3f);
            rt.offsetMax = new Vector2(0f, -3f);
        }

        public static void Select(Button b)
        {
            if (b == null || EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(b.gameObject);
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            var m = go.AddComponent<InputSystemUIInputModule>();
            m.AssignDefaultActions();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
