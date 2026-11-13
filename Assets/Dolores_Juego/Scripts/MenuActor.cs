using UnityEngine;

namespace Dolores1810
{
    /// <summary>Personaje decorativo del menú principal (solo reproduce una animación en bucle).</summary>
    public class MenuActor : MonoBehaviour
    {
        public FrameAnimator animator;
        public SpriteClip clip;
        public bool flip;

        void Start()
        {
            if (animator == null) return;
            if (animator.target != null) animator.target.flipX = flip;
            animator.Play(clip, true);
        }
    }
}
