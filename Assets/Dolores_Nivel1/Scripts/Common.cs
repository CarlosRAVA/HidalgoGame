using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Dolores
{
    /// <summary>Nombres de escenas usados por los menús.</summary>
    public static class SceneNames
    {
        public const string Menu = "MainMenu";
        public const string Level1 = "Level1";
        public const string Level2 = "Level2";
    }

    /// <summary>Secuencia de sprites que se reproduce con FrameAnimator.</summary>
    [System.Serializable]
    public class SpriteClip
    {
        public string name;
        public Sprite[] frames;
        public float fps = 10f;
        public bool loop = true;
    }

    public interface IDamageable
    {
        /// <summary>Devuelve true si el golpe fue aceptado.</summary>
        bool TakeDamage(int amount, Vector2 sourcePos);
    }

    /// <summary>
    /// Lectura de controles. Funciona con el nuevo Input System, con el Input Manager clásico o con ambos.
    /// Teclado: A/D o flechas = mover, Espacio/W/Flecha arriba = saltar, J/Z/X o clic izquierdo = atacar, Esc/P = pausa.
    /// Control: stick o cruceta, A = saltar, X/B = atacar, Start = pausa.
    /// </summary>
    public static class GameInput
    {
        public static float Horizontal()
        {
            float h = 0f;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.aKey.isPressed || k.leftArrowKey.isPressed) h -= 1f;
                if (k.dKey.isPressed || k.rightArrowKey.isPressed) h += 1f;
            }
            var g = Gamepad.current;
            if (g != null)
            {
                float sx = g.leftStick.x.ReadValue();
                if (Mathf.Abs(sx) > 0.25f) h += sx;
                if (g.dpad.left.isPressed) h -= 1f;
                if (g.dpad.right.isPressed) h += 1f;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;
#endif
            return Mathf.Clamp(h, -1f, 1f);
        }

        public static bool JumpDown()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null && (k.spaceKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame)) return true;
            var g = Gamepad.current;
            if (g != null && g.buttonSouth.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) return true;
#endif
            return false;
        }

        public static bool JumpHeld()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null && (k.spaceKey.isPressed || k.wKey.isPressed || k.upArrowKey.isPressed)) return true;
            var g = Gamepad.current;
            if (g != null && g.buttonSouth.isPressed) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) return true;
#endif
            return false;
        }

        public static bool AttackDown()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null && (k.jKey.wasPressedThisFrame || k.zKey.wasPressedThisFrame || k.xKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame)) return true;
            var m = Mouse.current;
            if (m != null && m.leftButton.wasPressedThisFrame) return true;
            var g = Gamepad.current;
            if (g != null && (g.buttonWest.wasPressedThisFrame || g.buttonEast.wasPressedThisFrame || g.rightTrigger.wasPressedThisFrame)) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.X) || Input.GetMouseButtonDown(0)) return true;
#endif
            return false;
        }

        public static bool PauseDown()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null && (k.escapeKey.wasPressedThisFrame || k.pKey.wasPressedThisFrame)) return true;
            var g = Gamepad.current;
            if (g != null && g.startButton.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) return true;
#endif
            return false;
        }
    }

    /// <summary>Compatibilidad entre versiones de Unity para la velocidad del Rigidbody2D.</summary>
    public static class RbExt
    {
        public static Vector2 GetVel(this Rigidbody2D rb)
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }

        public static void SetVel(this Rigidbody2D rb, Vector2 v)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = v;
#else
            rb.velocity = v;
#endif
        }
    }
}
