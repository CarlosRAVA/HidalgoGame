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

        static Font s_fallbackFont;
        static Sprite s_circleSprite;
        static Sprite s_ringSprite;
        static Sprite s_arrowUpSprite;
        static Sprite s_swordIconSprite;
        static Sprite s_speedIconSprite;
        static Sprite s_swapIconSprite;

        public static float DistToSeg(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p - a, ba = b - a;
            float lenSq = ba.sqrMagnitude;
            if (lenSq < 0.0001f) return (p - a).magnitude;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / lenSq);
            return (pa - ba * h).magnitude;
        }

        public static Sprite GetCircleSprite()
        {
            if (s_circleSprite != null) return s_circleSprite;
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            float center = (size - 1) * 0.5f;
            float radius = center - 1.5f;
            var colors = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius + 1.2f - dist);
                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            s_circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return s_circleSprite;
        }

        public static Sprite GetRingSprite(float thicknessFraction = 0.10f)
        {
            if (s_ringSprite != null) return s_ringSprite;
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            float center = (size - 1) * 0.5f;
            float outerRadius = center - 1.5f;
            float innerRadius = outerRadius * (1f - thicknessFraction);
            var colors = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alphaOuter = Mathf.Clamp01(outerRadius + 1.2f - dist);
                    float alphaInner = Mathf.Clamp01(dist - innerRadius + 1.2f);
                    float alpha = Mathf.Min(alphaOuter, alphaInner);
                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            s_ringSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return s_ringSprite;
        }

        public static Sprite GetArrowUpSprite()
        {
            if (s_arrowUpSprite != null) return s_arrowUpSprite;
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var colors = new Color[size * size];
            Vector2 tip = new Vector2(31.5f, 52f);
            Vector2 left = new Vector2(14f, 18f);
            Vector2 right = new Vector2(49f, 18f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float d1 = (p.x - tip.x) * (left.y - tip.y) - (p.y - tip.y) * (left.x - tip.x);
                    float d2 = (p.x - left.x) * (right.y - left.y) - (p.y - left.y) * (right.x - left.x);
                    float d3 = (p.x - right.x) * (tip.y - right.y) - (p.y - right.y) * (tip.x - right.x);
                    bool inside = (d1 >= 0 && d2 >= 0 && d3 >= 0);
                    float alpha = inside ? 1f : 0f;
                    if (!inside)
                    {
                        float minDist = Mathf.Min(DistToSeg(p, tip, left), Mathf.Min(DistToSeg(p, left, right), DistToSeg(p, right, tip)));
                        alpha = Mathf.Clamp01(1.2f - minDist);
                    }
                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            s_arrowUpSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return s_arrowUpSprite;
        }

        public static Sprite GetSwordIconSprite()
        {
            if (s_swordIconSprite != null) return s_swordIconSprite;
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var colors = new Color[size * size];
            Vector2 bStart = new Vector2(22f, 22f), bEnd = new Vector2(50f, 50f);
            Vector2 gStart = new Vector2(16f, 28f), gEnd = new Vector2(28f, 16f);
            Vector2 hStart = new Vector2(22f, 22f), hEnd = new Vector2(14f, 14f);
            Vector2 pommel = new Vector2(13f, 13f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float dB = DistToSeg(p, bStart, bEnd) - 3.2f;
                    float dG = DistToSeg(p, gStart, gEnd) - 2.8f;
                    float dH = DistToSeg(p, hStart, hEnd) - 2.2f;
                    float dP = Vector2.Distance(p, pommel) - 3.5f;
                    float dTip = Vector2.Distance(p, new Vector2(51f, 51f)) - 1.8f;
                    float minDist = Mathf.Min(Mathf.Min(dB, dG), Mathf.Min(dH, Mathf.Min(dP, dTip)));
                    float alpha = Mathf.Clamp01(1.2f - minDist);
                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            s_swordIconSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return s_swordIconSprite;
        }

        public static Sprite GetSpeedIconSprite()
        {
            if (s_speedIconSprite != null) return s_speedIconSprite;
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var colors = new Color[size * size];
            Vector2 c1Top = new Vector2(16f, 48f), c1Mid = new Vector2(30f, 32f), c1Bot = new Vector2(16f, 16f);
            Vector2 c2Top = new Vector2(32f, 48f), c2Mid = new Vector2(46f, 32f), c2Bot = new Vector2(32f, 16f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float d1A = DistToSeg(p, c1Top, c1Mid);
                    float d1B = DistToSeg(p, c1Mid, c1Bot);
                    float d2A = DistToSeg(p, c2Top, c2Mid);
                    float d2B = DistToSeg(p, c2Mid, c2Bot);
                    float minDist = Mathf.Min(Mathf.Min(d1A, d1B), Mathf.Min(d2A, d2B)) - 3.4f;
                    float alpha = Mathf.Clamp01(1.2f - minDist);
                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            s_speedIconSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return s_speedIconSprite;
        }

        public static Sprite GetSwapIconSprite()
        {
            if (s_swapIconSprite != null) return s_swapIconSprite;
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var colors = new Color[size * size];
            Vector2 center = new Vector2(31.5f, 31.5f);
            float ringR = 17f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float dist = Vector2.Distance(p, center);
                    float dRing = Mathf.Abs(dist - ringR) - 2.8f;
                    float angle = Mathf.Atan2(p.y - center.y, p.x - center.x) * Mathf.Rad2Deg;
                    if (angle < 0) angle += 360f;
                    bool inArc1 = (angle >= 25f && angle <= 155f);
                    bool inArc2 = (angle >= 205f && angle <= 335f);
                    float alpha = (inArc1 || inArc2) ? Mathf.Clamp01(1.2f - dRing) : 0f;

                    float dTip1 = DistToSeg(p, new Vector2(48f, 38f), new Vector2(48f, 26f)) - 2.5f;
                    float dTip1b = DistToSeg(p, new Vector2(48f, 26f), new Vector2(40f, 26f)) - 2.5f;
                    float dTip2 = DistToSeg(p, new Vector2(15f, 26f), new Vector2(15f, 38f)) - 2.5f;
                    float dTip2b = DistToSeg(p, new Vector2(15f, 38f), new Vector2(23f, 38f)) - 2.5f;
                    float minTip = Mathf.Min(Mathf.Min(dTip1, dTip1b), Mathf.Min(dTip2, dTip2b));
                    if (minTip < 1.2f) alpha = Mathf.Max(alpha, Mathf.Clamp01(1.2f - minTip));

                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            s_swapIconSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return s_swapIconSprite;
        }

        public static Font GetFont()
        {
            if (Font != null) return Font;
            if (s_fallbackFont != null) return s_fallbackFont;
            try { s_fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { s_fallbackFont = null; }
            if (s_fallbackFont == null) s_fallbackFont = Font.CreateDynamicFontFromOSFont("Arial", 16);
            return s_fallbackFont;
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

        public static Canvas CreateCanvasWithSafeArea(string name, int sortOrder, out RectTransform safeArea)
        {
            var c = CreateCanvas(name, sortOrder);
            var sa = Rect("SafeArea", c.transform);
            Stretch(sa);
            sa.gameObject.AddComponent<SafeAreaFitter>();
            safeArea = sa;
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
                o.effectColor = new Color(0.08f, 0.04f, 0.01f, 0.85f);
                o.effectDistance = new Vector2(1.2f, -1.2f);
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
            cb.highlightedColor = new Color(0.68f, 0.52f, 0.38f, 1f);
            cb.selectedColor = new Color(0.58f, 0.42f, 0.28f, 1f);
            cb.pressedColor = new Color(0.45f, 0.32f, 0.20f, 1f);
            cb.disabledColor = new Color(0.45f, 0.42f, 0.4f, 0.8f);
            cb.fadeDuration = 0.08f;
            b.colors = cb;
            var t = Label(rt, "Texto", label, 40, Cream, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.alignByGeometry = true;
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
