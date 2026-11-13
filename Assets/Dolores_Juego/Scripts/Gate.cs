using UnityEngine;

namespace Dolores1810
{
    /// <summary>Reja o puerta que sube y baja. Se abre con una palanca o al derrotar a un jefe.</summary>
    public class Gate : MonoBehaviour
    {
        public Vector3 openOffset = new Vector3(0f, 3.4f, 0f);
        public bool startOpen;
        public BoxCollider2D blocker;

        Vector3 closedPos, openPos;
        bool open;
        float k;

        void Awake()
        {
            closedPos = transform.position;
            openPos = closedPos + openOffset;
            if (blocker == null) blocker = GetComponent<BoxCollider2D>();
            open = startOpen;
            k = startOpen ? 1f : 0f;
            Apply();
        }

        public void Open() { if (!open) { open = true; AudioHub.Play(SfxId.Lever, 0.02f); } }
        public void Close() { if (open) { open = false; AudioHub.Play(SfxId.Whoosh, 0.02f, 0.7f); } }

        void Apply()
        {
            transform.position = Vector3.Lerp(closedPos, openPos, k);
            if (blocker != null) blocker.enabled = k < 0.6f;
        }

        void Update()
        {
            float target = open ? 1f : 0f;
            if (Mathf.Approximately(k, target)) return;
            k = Mathf.MoveTowards(k, target, Time.deltaTime * 1.4f);
            Apply();
        }
    }
}
