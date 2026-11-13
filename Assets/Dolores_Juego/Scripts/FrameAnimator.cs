using UnityEngine;

namespace Dolores1810
{
    /// <summary>Reproduce SpriteClip sobre un SpriteRenderer (animaciones cuadro a cuadro sin Animator).</summary>
    public class FrameAnimator : MonoBehaviour
    {
        public SpriteRenderer target;
        public float speed = 1f;

        SpriteClip current;
        float timer;
        int frame;
        bool finished;

        public System.Action OnFinished;

        public bool Finished { get { return finished; } }
        public int Frame { get { return frame; } }
        public SpriteClip Current { get { return current; } }
        public float NormalizedTime
        {
            get
            {
                if (current == null || current.frames == null || current.frames.Length == 0) return 0f;
                return Mathf.Clamp01((frame + timer * current.fps) / current.frames.Length);
            }
        }

        void Awake()
        {
            if (target == null) target = GetComponent<SpriteRenderer>();
        }

        public void Play(SpriteClip clip, bool restart = false)
        {
            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
            if (current == clip && !restart) return;
            current = clip;
            speed = 1f;
            timer = 0f;
            frame = 0;
            finished = false;
            if (target != null) target.sprite = clip.frames[0];
        }

        void Update()
        {
            if (current == null || finished || current.frames == null || current.frames.Length == 0) return;
            timer += Time.deltaTime * speed;
            float d = 1f / Mathf.Max(0.01f, current.fps);
            while (timer >= d)
            {
                timer -= d;
                frame++;
                if (frame >= current.frames.Length)
                {
                    if (current.loop)
                    {
                        frame = 0;
                    }
                    else
                    {
                        frame = current.frames.Length - 1;
                        finished = true;
                        if (target != null) target.sprite = current.frames[frame];
                        if (OnFinished != null) OnFinished();
                        return;
                    }
                }
                if (target != null) target.sprite = current.frames[frame];
            }
        }
    }
}
