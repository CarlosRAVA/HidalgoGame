using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Dolores1810
{
    /// <summary>Nombres de escenas del juego.</summary>
    public static class SceneNames
    {
        public const string Menu = "MainMenu";
        public static string ForLevel(int index) { return "Level" + index; }
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
        /// <summary>amount: puntos de daño (enemigos) o corazones (jugador). Devuelve true si el golpe fue aceptado.</summary>
        bool TakeDamage(int amount, Vector2 sourcePos);
    }

    public interface IInteractable
    {
        string Prompt { get; }
        bool Available { get; }
        /// <summary>Si es true solo muestra una pista y no responde a la tecla de interactuar.</summary>
        bool HintOnly { get; }
        void Interact(PlatformerPlayer player);
    }

    /// <summary>
    /// Lectura de controles (Input System nuevo, clásico o ambos).
    /// Mover A/D · Saltar Espacio · Atacar J/Z/X/clic · Correr Shift · Interactuar E · Trepar W/S · Armas Q o 1-5 · Pausa Esc/P.
    /// Control: stick, A saltar, X/B atacar, LT/LB correr, Y interactuar, cruceta arriba cambia arma, Start pausa.
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

        public static float Vertical()
        {
            float v = 0f;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.wKey.isPressed || k.upArrowKey.isPressed) v += 1f;
                if (k.sKey.isPressed || k.downArrowKey.isPressed) v -= 1f;
            }
            var g = Gamepad.current;
            if (g != null)
            {
                float sy = g.leftStick.y.ReadValue();
                if (Mathf.Abs(sy) > 0.35f) v += sy;
                if (g.dpad.up.isPressed) v += 1f;
                if (g.dpad.down.isPressed) v -= 1f;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1f;
#endif
            return Mathf.Clamp(v, -1f, 1f);
        }

        public static bool JumpDown(bool allowUpKeys)
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null && (k.spaceKey.wasPressedThisFrame || (allowUpKeys && (k.wKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame)))) return true;
            var g = Gamepad.current;
            if (g != null && g.buttonSouth.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Space)) return true;
            if (allowUpKeys && (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))) return true;
#endif
            return false;
        }

        public static bool JumpHeld(bool allowUpKeys)
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null && (k.spaceKey.isPressed || (allowUpKeys && (k.wKey.isPressed || k.upArrowKey.isPressed)))) return true;
            var g = Gamepad.current;
            if (g != null && g.buttonSouth.isPressed) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKey(KeyCode.Space)) return true;
            if (allowUpKeys && (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))) return true;
#endif
            return false;
        }

        public static bool AttackDown()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null && (k.jKey.wasPressedThisFrame || k.zKey.wasPressedThisFrame || k.xKey.wasPressedThisFrame)) return true;
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

        public static bool RunHeld()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null && (k.leftShiftKey.isPressed || k.rightShiftKey.isPressed)) return true;
            var g = Gamepad.current;
            if (g != null && (g.leftTrigger.isPressed || g.leftShoulder.isPressed)) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) return true;
#endif
            return false;
        }

        public static bool InteractDown()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null && k.eKey.wasPressedThisFrame) return true;
            var g = Gamepad.current;
            if (g != null && g.buttonNorth.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.E)) return true;
#endif
            return false;
        }

        public static bool NextWeaponDown()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null && k.qKey.wasPressedThisFrame) return true;
            var g = Gamepad.current;
            if (g != null && g.rightShoulder.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Q)) return true;
#endif
            return false;
        }

        /// <summary>Devuelve 1-5 si se presionó una tecla numérica de arma, o 0.</summary>
        public static int WeaponNumberDown()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.digit1Key.wasPressedThisFrame) return 1;
                if (k.digit2Key.wasPressedThisFrame) return 2;
                if (k.digit3Key.wasPressedThisFrame) return 3;
                if (k.digit4Key.wasPressedThisFrame) return 4;
                if (k.digit5Key.wasPressedThisFrame) return 5;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Alpha1)) return 1;
            if (Input.GetKeyDown(KeyCode.Alpha2)) return 2;
            if (Input.GetKeyDown(KeyCode.Alpha3)) return 3;
            if (Input.GetKeyDown(KeyCode.Alpha4)) return 4;
            if (Input.GetKeyDown(KeyCode.Alpha5)) return 5;
#endif
            return 0;
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

        public static bool AnyKeyDown()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null && k.anyKey.wasPressedThisFrame) return true;
            var g = Gamepad.current;
            if (g != null && (g.buttonSouth.wasPressedThisFrame || g.startButton.wasPressedThisFrame)) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.anyKeyDown) return true;
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

    /// <summary>Cuenta los enemigos que están persiguiendo o peleando (para cambiar a música de combate).</summary>
    public static class CombatTracker
    {
        public static int Aggro;
        public static void Reset() { Aggro = 0; }
        public static void Add() { Aggro++; }
        public static void Remove() { Aggro = Mathf.Max(0, Aggro - 1); }
    }
}
