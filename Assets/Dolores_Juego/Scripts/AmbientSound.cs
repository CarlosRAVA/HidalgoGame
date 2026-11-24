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
            // Se desactivan los loops continuos de grillos/ambiente (amb_day y amb_night) para evitar el zumbido constante.
            dayLoop = null;
            nightLoop = null;
            bell = gameObject.AddComponent<AudioSource>();
            bell.playOnAwake = false;
            bell.spatialBlend = 0f;
            bellTimer = Random.Range(18f, bellEverySeconds);
        }

        void Update()
        {
            if (day != null && day.isPlaying) day.Stop();
            if (night != null && night.isPlaying) night.Stop();
            var gs = GameSession.I;
            if (distantBell != null && gs != null && gs.State == GameState.Playing)
            {
                bellTimer -= Time.deltaTime;
                if (bellTimer <= 0f)
                {
                    bellTimer = bellEverySeconds + Random.Range(-10f, 15f);
                    bell.pitch = Random.Range(0.92f, 1.05f);
                    bell.volume = GameSettings.Sfx * 0.18f;
                    bell.clip = distantBell;
                    bell.Play();
                }
            }
        }
    }
}
