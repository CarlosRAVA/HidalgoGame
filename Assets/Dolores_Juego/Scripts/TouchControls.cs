using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Dolores1810
{
    /// <summary>
    /// Botón táctil para interfaz móvil con respuesta háptica visual (escala y tinte).
    /// </summary>
    public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public RectTransform targetTransform;
        public Graphic targetGraphic;
        public Color normalColor = Color.white;
        public Color pressedColor = new Color(1.3f, 1.3f, 1.3f, 1f);

        public bool IsHeld { get; private set; }
        private bool wasPressed;

        void Awake()
        {
            if (targetTransform == null) targetTransform = GetComponent<RectTransform>();
            if (targetGraphic == null) targetGraphic = GetComponent<Graphic>();
            if (targetGraphic != null) normalColor = targetGraphic.color;
        }

        public bool ConsumePress()
        {
            if (wasPressed)
            {
                wasPressed = false;
                return true;
            }
            return false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            IsHeld = true;
            wasPressed = true;
            if (targetTransform != null) targetTransform.localScale = new Vector3(0.92f, 0.92f, 1f);
            if (targetGraphic != null) targetGraphic.color = pressedColor;
            if (TouchControls.Instance != null) TouchControls.Instance.NotifyTouchUsed();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            IsHeld = false;
            if (targetTransform != null) targetTransform.localScale = Vector3.one;
            if (targetGraphic != null) targetGraphic.color = normalColor;
        }

        void OnDisable()
        {
            IsHeld = false;
            wasPressed = false;
            if (targetTransform != null) targetTransform.localScale = Vector3.one;
            if (targetGraphic != null) targetGraphic.color = normalColor;
        }
    }

    /// <summary>
    /// Gestor centralizado de controles táctiles en pantalla (joystick y botones de acción).
    /// Auto-oculta los controles de movimiento/acción al usar teclado/gamepad,
    /// y los muestra inmediatamente al tocar o usar joystick. El botón de pausa siempre permanece visible.
    /// </summary>
    public class TouchControls : MonoBehaviour
    {
        public static TouchControls Instance { get; private set; }

        public CanvasGroup gameplayGroup;
        public VirtualJoystick joystick;
        public TouchButton btnJump;
        public TouchButton btnAttack;
        public TouchButton btnInteract;
        public TouchButton btnWeapon;
        public TouchButton btnRun;
        public TouchButton btnPause;

        private bool isTouchMode = true;
        public bool IsTouchMode => isTouchMode;

        void Awake()
        {
            Instance = this;
        }

        void OnEnable()
        {
            if (Instance == null) Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void NotifyTouchUsed()
        {
            isTouchMode = true;
        }

        void Update()
        {
            // 1. Detectar si el jugador usó teclado o gamepad
            bool keyboardOrGamepadUsed = false;
#if ENABLE_INPUT_SYSTEM
            var k = UnityEngine.InputSystem.Keyboard.current;
            if (k != null && k.anyKey.wasPressedThisFrame) keyboardOrGamepadUsed = true;
            var g = UnityEngine.InputSystem.Gamepad.current;
            if (g != null && (Mathf.Abs(g.leftStick.x.ReadValue()) > 0.2f || Mathf.Abs(g.leftStick.y.ReadValue()) > 0.2f ||
                g.buttonSouth.wasPressedThisFrame || g.buttonWest.wasPressedThisFrame || g.buttonEast.wasPressedThisFrame ||
                g.buttonNorth.wasPressedThisFrame || g.dpad.left.wasPressedThisFrame || g.dpad.right.wasPressedThisFrame))
            {
                keyboardOrGamepadUsed = true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.anyKeyDown) keyboardOrGamepadUsed = true;
#endif

            // 2. Detectar si el jugador usó pantalla táctil o interactuó con los controles
            bool touchUsed = false;
            if (Input.touchCount > 0) touchUsed = true;
            if (joystick != null && (Mathf.Abs(joystick.Horizontal) > 0.05f || Mathf.Abs(joystick.Vertical) > 0.05f)) touchUsed = true;
            if (btnJump != null && btnJump.IsHeld) touchUsed = true;
            if (btnAttack != null && btnAttack.IsHeld) touchUsed = true;
            if (btnInteract != null && btnInteract.IsHeld) touchUsed = true;
            if (btnWeapon != null && btnWeapon.IsHeld) touchUsed = true;
            if (btnRun != null && btnRun.IsHeld) touchUsed = true;
            if (btnPause != null && btnPause.IsHeld) touchUsed = true;

            if (touchUsed)
            {
                isTouchMode = true;
            }
            else if (keyboardOrGamepadUsed)
            {
                isTouchMode = false;
            }

            // 3. Transición suave del grupo de juego (joystick y botones de acción)
            if (gameplayGroup != null)
            {
                float target = isTouchMode ? 1f : 0f;
                gameplayGroup.alpha = Mathf.MoveTowards(gameplayGroup.alpha, target, Time.unscaledDeltaTime * 6f);
                gameplayGroup.blocksRaycasts = gameplayGroup.alpha > 0.05f;
                gameplayGroup.interactable = gameplayGroup.alpha > 0.05f;
            }
        }

        public static bool ConsumeJump() => Instance != null && Instance.btnJump != null && Instance.btnJump.ConsumePress();
        public static bool IsJumpHeld() => Instance != null && Instance.btnJump != null && Instance.btnJump.IsHeld;
        public static bool ConsumeAttack() => Instance != null && Instance.btnAttack != null && Instance.btnAttack.ConsumePress();
        public static bool ConsumeInteract() => Instance != null && Instance.btnInteract != null && Instance.btnInteract.ConsumePress();
        public static bool ConsumeWeapon() => Instance != null && Instance.btnWeapon != null && Instance.btnWeapon.ConsumePress();
        public static bool IsRunHeld() => Instance != null && Instance.btnRun != null && Instance.btnRun.IsHeld;
        public static bool ConsumePause() => Instance != null && Instance.btnPause != null && Instance.btnPause.ConsumePress();
    }
}
