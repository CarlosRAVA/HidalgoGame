using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Dolores
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

        public static Image Panel(Transform parent, string name, Vector2 center, Vector2 size)
        {
            var rt = Rect(name, parent);
            Place(rt, new Vector2(0.5f, 0.5f), center, size);
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
            Place(rt, new Vector2(0.5f, 0.5f), center, size);
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
            cb.fadeDuration = 0.08f;
            b.colors = cb;
            var t = Label(rt, "Texto", label, 40, Cream, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(t.rectTransform);
            rt.gameObject.AddComponent<UIButtonSounds>();
            b.onClick.AddListener(() => AudioHub.Play(SfxId.Click, 0f));
            if (onClick != null) b.onClick.AddListener(onClick);
            return b;
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
