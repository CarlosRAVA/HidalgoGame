using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Dolores
{
    /// <summary>
    /// Ciclo de día a noche según el avance del jugador por el nivel: de día en la plaza,
    /// atardecer en el mercado y noche al llegar a la parroquia.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class DayNightCycle : MonoBehaviour
    {
        public Transform player;
        public Camera cam;
        public float startX = 0f;
        public float endX = 860f;

        public Light2D globalLight;
        public SpriteRenderer skyDay, skyDusk, skyNight, stars, sun, moon;
        public SpriteRenderer[] townLights;
        public SpriteRenderer[] tinted;
        public SpriteRenderer[] glows;
        public Light2D[] lamps;

        public Color dayLight = Color.white;
        public Color duskLight = new Color(1f, 0.74f, 0.58f);
        public Color nightLight = new Color(0.42f, 0.48f, 0.82f);
        public float dayIntensity = 1f;
        public float duskIntensity = 0.95f;
        public float nightIntensity = 0.72f;
        public Color dayTint = Color.white;
        public Color duskTint = new Color(1f, 0.68f, 0.68f);
        public Color nightTint = new Color(0.32f, 0.36f, 0.6f);

        float t;
        bool init;

        static float Smooth(float a, float b, float x)
        {
            x = Mathf.Clamp01((x - a) / (b - a));
            return x * x * (3f - 2f * x);
        }

        static void SetAlpha(SpriteRenderer sr, float a)
        {
            if (sr == null) return;
            Color c = sr.color;
            c.a = Mathf.Clamp01(a);
            sr.color = c;
        }

        void LateUpdate()
        {
            if (player == null) return;
            float target = Mathf.InverseLerp(startX, endX, player.position.x);
            if (!init) { t = target; init = true; }
            t = Mathf.MoveTowards(t, target, Time.deltaTime * 0.15f);

            float wDay = 1f - Smooth(0.30f, 0.52f, t);
            float wNight = Smooth(0.62f, 0.84f, t);
            float wDusk = Mathf.Clamp01(1f - wDay - wNight);

            SetAlpha(skyNight, 1f);
            SetAlpha(skyDusk, 1f - wNight);
            SetAlpha(skyDay, wDay);
            SetAlpha(stars, wNight);
            if (townLights != null)
            {
                for (int i = 0; i < townLights.Length; i++) SetAlpha(townLights[i], wDusk * 0.55f + wNight);
            }

            if (sun != null)
            {
                Color c = Color.Lerp(Color.white, new Color(1f, 0.62f, 0.35f), Mathf.Clamp01(wDusk * 1.5f));
                c.a = Mathf.Clamp01(wDay + wDusk * 0.9f);
                sun.color = c;
            }
            SetAlpha(moon, wNight);

            if (cam != null)
            {
                float oh = cam.orthographicSize;
                float ow = oh * cam.aspect;
                Vector3 cp = cam.transform.position;
                if (sun != null)
                    sun.transform.position = new Vector3(cp.x + ow * 0.45f, cp.y + Mathf.Lerp(oh * 0.55f, -oh * 0.2f, Smooth(0.15f, 0.62f, t)), 0f);
                if (moon != null)
                    moon.transform.position = new Vector3(cp.x - ow * 0.35f, cp.y + Mathf.Lerp(-oh * 0.4f, oh * 0.55f, Smooth(0.58f, 0.9f, t)), 0f);
            }

            Color tint = dayTint * wDay + duskTint * wDusk + nightTint * wNight;
            if (tinted != null)
            {
                for (int i = 0; i < tinted.Length; i++)
                {
                    if (tinted[i] == null) continue;
                    Color c = tint;
                    c.a = tinted[i].color.a;
                    tinted[i].color = c;
                }
            }

            if (globalLight != null)
            {
                globalLight.color = dayLight * wDay + duskLight * wDusk + nightLight * wNight;
                globalLight.intensity = dayIntensity * wDay + duskIntensity * wDusk + nightIntensity * wNight;
            }

            float lampAmount = wDusk * 0.45f + wNight;
            float flicker = 0.93f + 0.07f * Mathf.Sin(Time.time * 7.3f);
            if (glows != null)
            {
                for (int i = 0; i < glows.Length; i++) SetAlpha(glows[i], lampAmount * 0.85f * flicker);
            }
            if (lamps != null)
            {
                for (int i = 0; i < lamps.Length; i++)
                {
                    if (lamps[i] == null) continue;
                    lamps[i].intensity = lampAmount * 1.25f * flicker;
                    lamps[i].enabled = lampAmount > 0.03f;
                }
            }
        }
    }
}
