using UnityEngine;

namespace Dolores1810
{
    /// <summary>Sonido ambiental de calles y campo: de día viento y pájaros, de noche grillos; con campanadas lejanas.</summary>
    public class AmbientSound : MonoBehaviour
    {
        public AudioClip dayLoop;
        public AudioClip nightLoop;
        public AudioClip distantBell;
        public DayNightCycle cycle;
        [Range(0f, 1f)] public float volume = 0.5f;
        public float bellEverySeconds = 55f;

        AudioSource day, night, bell;
        float bellTimer;

        void Start()
        {
            day = MakeSource(dayLoop);
            night = MakeSource(nightLoop);
            bell = gameObject.AddComponent<AudioSource>();
            bell.playOnAwake = false;
            bell.spatialBlend = 0f;
            bellTimer = Random.Range(12f, bellEverySeconds);
        }

        AudioSource MakeSource(AudioClip c)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.clip = c;
            s.loop = true;
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.volume = 0f;
            if (c != null) s.Play();
            return s;
        }

        void Update()
        {
            float n = cycle != null ? cycle.NightWeight : 0f;
            float d = cycle != null ? cycle.DayWeight : 1f;
            float v = GameSettings.Sfx * volume;
            if (day != null) day.volume = Mathf.Lerp(day.volume, v * Mathf.Clamp01(d + (1f - n) * 0.3f), Time.unscaledDeltaTime * 2f);
            if (night != null) night.volume = Mathf.Lerp(night.volume, v * n, Time.unscaledDeltaTime * 2f);
            var gs = GameSession.I;
            if (distantBell != null && gs != null && gs.State == GameState.Playing)
            {
                bellTimer -= Time.deltaTime;
                if (bellTimer <= 0f)
                {
                    bellTimer = bellEverySeconds + Random.Range(-10f, 15f);
                    bell.pitch = Random.Range(0.92f, 1.05f);
                    bell.volume = GameSettings.Sfx * 0.22f;
                    bell.clip = distantBell;
                    bell.Play();
                }
            }
        }
    }
}
