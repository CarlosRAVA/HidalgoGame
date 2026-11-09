using System.Collections.Generic;
using UnityEngine;

namespace Dolores
{
    public enum SfxId
    {
        Jump, Land, Swing, HitEnemy, PlayerHurt, EnemyDie, PlayerDie, Coin, Life, Heal, Gem, Checkpoint,
        Bell, Break, Spike, Click, Hover, Pause, Step, Alert, EnemySwing, Victory, GameOver
    }

    /// <summary>Reproduce música de fondo y efectos de sonido. Un AudioHub por escena.</summary>
    public class AudioHub : MonoBehaviour
    {
        [System.Serializable]
        public class Entry
        {
            public SfxId id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
        }

        public static AudioHub I;

        public Entry[] sfx;
        public AudioClip musicClip;
        [Range(0f, 1f)] public float musicVolume = 0.4f;
        [Range(0f, 1f)] public float sfxVolume = 0.9f;

        AudioSource music;
        AudioSource[] pool;
        int poolIndex;
        Dictionary<SfxId, Entry> map;
        float musicTarget;

        void Awake()
        {
            I = this;
            map = new Dictionary<SfxId, Entry>();
            if (sfx != null)
            {
                for (int i = 0; i < sfx.Length; i++)
                {
                    if (sfx[i] != null && sfx[i].clip != null) map[sfx[i].id] = sfx[i];
                }
            }
            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.spatialBlend = 0f;
            music.clip = musicClip;
            musicTarget = musicVolume;
            music.volume = 0f;
            if (musicClip != null) music.Play();

            pool = new AudioSource[12];
            for (int i = 0; i < pool.Length; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                pool[i] = s;
            }
        }

        void OnDestroy()
        {
            if (I == this) I = null;
        }

        void Update()
        {
            if (music != null)
            {
                music.volume = Mathf.MoveTowards(music.volume, musicTarget, Time.unscaledDeltaTime * 0.6f);
            }
        }

        public void SetMusicLevel(float multiplier)
        {
            musicTarget = musicVolume * Mathf.Clamp01(multiplier);
        }

        public void StopMusic()
        {
            musicTarget = 0f;
        }

        public static void Play(SfxId id, float pitchVariation = 0.05f, float volumeMul = 1f)
        {
            if (I != null) I.PlayInternal(id, pitchVariation, volumeMul);
        }

        void PlayInternal(SfxId id, float pitchVariation, float volumeMul)
        {
            Entry e;
            if (map == null || !map.TryGetValue(id, out e)) return;
            var s = pool[poolIndex];
            poolIndex = (poolIndex + 1) % pool.Length;
            s.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            s.volume = sfxVolume * e.volume * volumeMul;
            s.clip = e.clip;
            s.Play();
        }
    }
}
