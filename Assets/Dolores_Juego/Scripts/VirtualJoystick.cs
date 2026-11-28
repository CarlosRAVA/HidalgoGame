using UnityEngine;
using UnityEngine.EventSystems;

namespace Dolores1810
{
    /// <summary>
    /// Joystick virtual táctil para movimiento en dispositivos móviles y editor.
    /// Basado en el manual del curso (VirtualJoystickPack).
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Componentes de la UI")]
        public RectTransform background;
        public RectTransform handle;

        [Header("Configuración")]
        public float handleRange = 1f;

        private Vector2 inputVector = Vector2.zero;

        public static VirtualJoystick Instance { get; private set; }

        public float Horizontal => inputVector.x;
        public float Vertical => inputVector.y;
        public Vector2 Direction => inputVector;

        void Awake()
        {
            Instance = this;
            if (background == null) background = GetComponent<RectTransform>();
        }

        void OnEnable()
        {
            if (Instance == null) Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (TouchControls.Instance != null) TouchControls.Instance.NotifyTouchUsed();
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (background == null || handle == null) return;

            Vector2 position = RectTransformUtility.WorldToScreenPoint(null, background.position);
            Vector2 radius = background.sizeDelta / 2f;
            float scale = background.lossyScale.x > 0.001f ? background.lossyScale.x : 1f;

            inputVector = (eventData.position - position) / (radius * scale);
            inputVector = (inputVector.magnitude > 1f) ? inputVector.normalized : inputVector;
            handle.anchoredPosition = inputVector * (radius.x * handleRange);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            inputVector = Vector2.zero;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
        }
    }
}
