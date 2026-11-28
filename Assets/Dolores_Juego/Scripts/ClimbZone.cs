using UnityEngine;

namespace Dolores1810
{
    /// <summary>Zona de escalera: solo aquí se puede trepar. Los límites los fija el constructor del nivel.</summary>
    public class ClimbZone : MonoBehaviour, IInteractable
    {
        public float TopY = 4f;
        public float BottomY = 0f;
        public float exitDirection = 1f;
        public float CenterX { get { return transform.position.x; } }

        public string Prompt { get { return "Presiona W para trepar"; } }
        public bool Available { get { return true; } }
        public bool HintOnly { get { return true; } }
        public void Interact(PlatformerPlayer player) { }

        void OnTriggerEnter2D(Collider2D other)
        {
            var p = other.GetComponentInParent<PlatformerPlayer>();
            if (p != null) p.SetClimbZone(this);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var p = other.GetComponentInParent<PlatformerPlayer>();
            if (p != null) p.ClearClimbZone(this);
        }
    }
}
