using UnityEngine;

namespace Dolores1810
{
    /// <summary>Reja o puerta que sube y baja. Se abre con una palanca, al resolver un puzzle o al derrotar a un jefe.</summary>
    public class Gate : MonoBehaviour
    {
        public Vector3 openOffset = new Vector3(0f, 3.4f, 0f);
        public bool startOpen;
        public BoxCollider2D blocker;
        public SpriteRenderer statusLight;

        Vector3 closedPos, openPos;
        bool open;
        bool initialized;
        float k;

        public bool IsOpen { get { return open; } }

        void Awake()
        {
            Init();
        }

        void Start()
        {
            if (!initialized) Init();
            else
            {
                openPos = closedPos + openOffset;
                Apply();
            }
        }

        public void Init()
        {
            closedPos = transform.position;
            openPos = closedPos + openOffset;
            if (blocker == null) blocker = GetComponent<BoxCollider2D>();
            open = startOpen;
            k = startOpen ? 1f : 0f;
            initialized = true;
            Apply();
        }

        public void Open()
        {
            if (!initialized) Init();
            openPos = closedPos + openOffset;
            if (!open)
            {
                open = true;
                if (blocker != null) blocker.enabled = false;
                UpdateStatusLight();
                AudioHub.Play(SfxId.Lever, 0.02f);
            }
        }

        public void Close()
        {
            if (!initialized) Init();
            openPos = closedPos + openOffset;
            if (open)
            {
                open = false;
                if (blocker != null) blocker.enabled = true;
                UpdateStatusLight();
                AudioHub.Play(SfxId.Whoosh, 0.02f, 0.7f);
            }
        }

        void UpdateStatusLight()
        {
            if (statusLight == null) return;
            statusLight.color = open
                ? new Color(0.2f, 0.95f, 0.4f, 0.95f)
                : new Color(0.95f, 0.2f, 0.18f, 0.9f);
        }

        void Apply()
        {
            transform.position = Vector3.Lerp(closedPos, openPos, k);
            if (blocker != null) blocker.enabled = !open && k < 0.45f;
            UpdateStatusLight();
        }

        void Update()
        {
            float target = open ? 1f : 0f;
            if (Mathf.Approximately(k, target)) return;
            k = Mathf.MoveTowards(k, target, Time.deltaTime * 2.2f);
            Apply();
        }
    }
}
