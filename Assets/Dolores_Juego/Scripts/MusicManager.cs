using UnityEngine;

namespace Dolores1810
{
    public enum Mood { None, Menu, Explore, Combat, Boss, Victory, Final }

    /// <summary>
    /// Único reproductor de música del juego. Solo dos AudioSource que se mezclan (fundido cruzado), así que nunca
    /// suenan varias pistas a la vez. Cambia entre exploración, combate y jefe según lo que pasa en el nivel.
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        static MusicManager instance;

        public static MusicManager Existing { get { return instance; } }

        public static MusicManager Ensure()
        {
            if (instance == null)
            {
                var go = new GameObject("MusicManager");
                Object.DontDestroyOnLoad(go);
                instance = go.AddComponent<MusicManager>();
            }
            return instance;
        }

        AudioSource[] src = new AudioSource[2];
        int active;
        AudioClip current;
        bool currentLoop;
        Mood mood = Mood.None;
        AudioClip menuClip, exploreClip, combatClip, bossClip, victoryClip, finalClip;
        float combatTimer, calmTimer;
        float duckLevel = 1f;
        float fadeSpeed = 0.9f;
        bool hasPlayer;

        void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            for (int i = 0; i < 2; i++)
            {
                src[i] = gameObject.AddComponent<AudioSource>();
                src[i].playOnAwake = false;
                src[i].loop = true;
                src[i].spatialBlend = 0f;
                src[i].volume = 0f;
            }
            GameSettings.Apply();
        }

        /// <summary>Define las pistas de la escena actual y empieza la de exploración (o menú).</summary>
        public void SetSet(AudioClip menu, AudioClip explore, AudioClip combat, AudioClip boss, AudioClip victory, AudioClip final)
        {
            menuClip = menu; exploreClip = explore; combatClip = combat; bossClip = boss; victoryClip = victory; finalClip = final;
            combatTimer = 0f; calmTimer = 0f;
            mood = Mood.None;
            SetMood(menu != null ? Mood.Menu : Mood.Explore, true);
        }

        public void SetMood(Mood m, bool force = false)
        {
            if (m == mood && !force) return;
            mood = m;
            AudioClip c = null;
            bool loop = true;
            switch (m)
            {
                case Mood.Menu: c = menuClip; break;
                case Mood.Explore: c = exploreClip; break;
                case Mood.Combat: c = combatClip != null ? combatClip : exploreClip; break;
                case Mood.Boss: c = bossClip != null ? bossClip : combatClip; break;
                case Mood.Victory: c = victoryClip; loop = false; break;
                case Mood.Final: c = finalClip != null ? finalClip : victoryClip; loop = false; break;
            }
            Crossfade(c, loop, m == Mood.Victory || m == Mood.Final ? 0.4f : 1.2f);
        }

        public Mood CurrentMood { get { return mood; } }

        void Crossfade(AudioClip clip, bool loop, float seconds)
        {
            if (clip == current && src[active].isPlaying && loop == currentLoop) return;
            fadeSpeed = 1f / Mathf.Max(0.05f, seconds);
            int next = 1 - active;
            if (clip != null)
            {
                src[next].clip = clip;
                src[next].loop = loop;
                src[next].volume = 0f;
                src[next].Play();
            }
            active = next;
            current = clip;
            currentLoop = loop;
        }

        public void Duck(float level) { duckLevel = Mathf.Clamp01(level); }

        public void StopAll(float seconds = 1f)
        {
            Crossfade(null, true, seconds);
            mood = Mood.None;
        }

        public void RegisterGameplay(bool on) { hasPlayer = on; }

        void Update()
        {
            float target = GameSettings.Music * duckLevel;
            for (int i = 0; i < 2; i++)
            {
                float t = (i == active && current != null) ? target : 0f;
                src[i].volume = Mathf.MoveTowards(src[i].volume, t, Time.unscaledDeltaTime * fadeSpeed * Mathf.Max(0.2f, target + 0.2f));
                if (i != active && src[i].isPlaying && src[i].volume <= 0.001f) src[i].Stop();
            }

            if (!hasPlayer) return;
            if (mood == Mood.Explore || mood == Mood.Combat)
            {
                if (CombatTracker.Aggro > 0)
                {
                    combatTimer += Time.unscaledDeltaTime; calmTimer = 0f;
                    if (mood == Mood.Explore && combatTimer > 0.6f) SetMood(Mood.Combat);
                }
                else
                {
                    calmTimer += Time.unscaledDeltaTime; combatTimer = 0f;
                    if (mood == Mood.Combat && calmTimer > 3.5f) SetMood(Mood.Explore);
                }
            }
        }
    }
}
