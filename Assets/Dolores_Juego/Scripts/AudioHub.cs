using System.Collections.Generic;
using UnityEngine;

namespace Dolores1810
{
    public enum SfxId
    {
        Jump, Land, Swing, HitEnemy, PlayerHurt, EnemyDie, PlayerDie, Coin, Life, Heal, Gem, Checkpoint,
        Bell, Break, Spike, Click, Hover, Pause, Step, Alert, EnemySwing, Victory, GameOver,
        ChestOpen, Interact, Buy, Error, WeaponSwitch, Climb, Secret, Whoosh, BossAlert, Shockwave,
        Firework, Splash, ShopOpen, LevelComplete, Breath, Lever
    }

    /// <summary>Efectos de sonido de la escena. La música la maneja MusicManager (una sola pista a la vez).</summary>
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

        AudioSource[] pool;
        int poolIndex;
        Dictionary<SfxId, Entry> map;

        void Awake()
        {
            I = this;
            map = new Dictionary<SfxId, Entry>();
            if (sfx != null)
            {
                for (int i = 0; i < sfx.Length; i++)
                    if (sfx[i] != null && sfx[i].clip != null) map[sfx[i].id] = sfx[i];
            }
            pool = new AudioSource[14];
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

        public static void Play(SfxId id, float pitchVariation = 0.05f, float volumeMul = 1f)
        {
            if (I != null) I.PlayInternal(id, pitchVariation, volumeMul, 1f);
        }

        public static void PlayPitched(SfxId id, float pitch, float volumeMul = 1f)
        {
            if (I != null) I.PlayInternal(id, 0f, volumeMul, pitch);
        }

        void PlayInternal(SfxId id, float pitchVariation, float volumeMul, float basePitch)
        {
            Entry e;
            if (map == null || !map.TryGetValue(id, out e)) return;
            var s = pool[poolIndex];
            poolIndex = (poolIndex + 1) % pool.Length;
            s.pitch = basePitch + Random.Range(-pitchVariation, pitchVariation);
            s.volume = GameSettings.Sfx * e.volume * volumeMul;
            s.clip = e.clip;
            s.Play();
        }
    }
}
