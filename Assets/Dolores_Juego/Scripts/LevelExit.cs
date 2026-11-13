using UnityEngine;

namespace Dolores1810
{
    /// <summary>Salida de un nivel: inicia una breve secuencia y pasa al siguiente nivel.</summary>
    public class LevelExit : MonoBehaviour
    {
        public string nextScene = "Level2";
        [TextArea] public string text = "";
        bool triggered;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (triggered) return;
            PlatformerPlayer p = other.GetComponentInParent<PlatformerPlayer>();
            if (p == null || p.IsDead) return;
            triggered = true;
            if (GameSession.I != null) GameSession.I.BeginExit(nextScene, text);
        }
    }
}
