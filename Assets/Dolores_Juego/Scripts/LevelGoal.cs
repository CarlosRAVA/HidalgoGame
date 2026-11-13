using UnityEngine;

namespace Dolores1810
{
    /// <summary>La campana de Dolores: tocarla termina el juego (solo existe en el nivel 3).</summary>
    public class LevelGoal : MonoBehaviour
    {
        public Transform bell;
        bool triggered;
        float t;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (triggered) return;
            PlatformerPlayer p = other.GetComponentInParent<PlatformerPlayer>();
            if (p == null || p.IsDead) return;
            triggered = true;
            if (GameSession.I != null) GameSession.I.StartFinal(bell != null ? bell : transform);
            else AudioHub.Play(SfxId.Bell, 0f);
        }

        void Update()
        {
            if (!triggered || bell == null) return;
            t += Time.deltaTime;
            float amp = Mathf.Exp(-t * 0.35f) * 32f;
            bell.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 5.2f) * amp);
        }
    }
}
