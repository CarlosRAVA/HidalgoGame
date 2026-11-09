using UnityEngine;

namespace Dolores
{
    /// <summary>La campana de Dolores: al tocarla termina el nivel.</summary>
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
            AudioHub.Play(SfxId.Bell, 0f);
            if (GameSession.I != null) GameSession.I.Victory();
        }

        void Update()
        {
            if (!triggered || bell == null) return;
            t += Time.deltaTime;
            float amp = Mathf.Exp(-t * 0.6f) * 30f;
            bell.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 5.5f) * amp);
        }
    }
}
