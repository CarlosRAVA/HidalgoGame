using UnityEngine;

namespace Dolores1810
{
    /// <summary>Volúmenes del jugador (general, música, efectos). Se guardan en PlayerPrefs.</summary>
    public static class GameSettings
    {
        const string KMaster = "d1810_master", KMusic = "d1810_music", KSfx = "d1810_sfx";
        static bool loaded;
        static float master = 1f, music = 0.8f, sfx = 0.9f;

        public static event System.Action Changed;

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            master = PlayerPrefs.GetFloat(KMaster, 1f);
            music = PlayerPrefs.GetFloat(KMusic, 0.8f);
            sfx = PlayerPrefs.GetFloat(KSfx, 0.9f);
            AudioListener.volume = master;
        }

        public static float Master { get { Load(); return master; } }
        public static float Music { get { Load(); return music; } }
        public static float Sfx { get { Load(); return sfx; } }

        public static void SetMaster(float v) { Load(); master = Mathf.Clamp01(v); AudioListener.volume = master; Save(); }
        public static void SetMusic(float v) { Load(); music = Mathf.Clamp01(v); Save(); }
        public static void SetSfx(float v) { Load(); sfx = Mathf.Clamp01(v); Save(); }

        static void Save()
        {
            PlayerPrefs.SetFloat(KMaster, master);
            PlayerPrefs.SetFloat(KMusic, music);
            PlayerPrefs.SetFloat(KSfx, sfx);
            if (Changed != null) Changed();
        }

        public static void Apply()
        {
            Load();
            AudioListener.volume = master;
        }
    }
}
