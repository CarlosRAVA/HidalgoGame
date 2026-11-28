using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Dolores1810
{
    /// <summary>Fogata animada con luz parpadeante.</summary>
    public class CampFire : MonoBehaviour
    {
        public Sprite[] frames;
        public SpriteRenderer target;
        public Light2D fireLight;
        public float fps = 8f;
        float t;

        void Update()
        {
            t += Time.deltaTime;
            if (target != null && frames != null && frames.Length > 0) target.sprite = frames[((int)(t * fps)) % frames.Length];
            if (fireLight != null) fireLight.intensity = 1.1f + 0.35f * Mathf.Sin(t * 11f) + 0.2f * Mathf.Sin(t * 23f);
        }
    }
}
