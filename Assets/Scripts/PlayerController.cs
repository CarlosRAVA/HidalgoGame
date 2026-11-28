using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Controlador completo para el personaje Body_A en Unity 6.
/// Compatible tanto con el New Input System (Keyboard / Gamepad) como con el Legacy Input Manager.
/// Soporta WASD, flechas de dirección, Shift para correr y animaciones automáticas de sprites.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    [Header("Velocidades de Movimiento")]
    [Tooltip("Velocidad normal al caminar")]
    public float walkSpeed = 4.5f;

    [Tooltip("Velocidad al correr con Shift presionado")]
    public float runSpeed = 7.5f;

    [Header("Sprites de Animación (Body_A)")]
    public Sprite[] idleSprites;
    public Sprite[] walkSprites;
    public Sprite[] runSprites;

    [Header("Frecuencia de Animación (FPS)")]
    public float idleFps = 6f;
    public float walkFps = 9f;
    public float runFps = 12f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator animator;

    private Vector2 moveInput;
    private bool isRunning;
    private float animTimer;
    private int currentFrame;

    private static readonly int ParamSpeed = Animator.StringToHash("Speed");
    private static readonly int ParamIsRunning = Animator.StringToHash("IsRunning");

    void Awake()
    {
        InitializeComponents();
    }

    void Start()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    void Update()
    {
        ReadInput();
        UpdateVisuals();
    }

    void FixedUpdate()
    {
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        Vector2 targetVelocity = moveInput * currentSpeed;

        if (rb != null)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = targetVelocity;
#else
            rb.velocity = targetVelocity;
#endif
        }
        else
        {
            // Fallback en caso de que Rigidbody2D no esté activo
            transform.position += (Vector3)(targetVelocity * Time.fixedDeltaTime);
        }
    }

    private void ReadInput()
    {
        Vector2 input = Vector2.zero;
        bool runPressed = false;

        // 1. Lectura a través del New Input System (Unity 6 por defecto)
#if ENABLE_INPUT_SYSTEM
        try
        {
            if (Keyboard.current != null)
            {
                var kb = Keyboard.current;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;

                if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed)
                    runPressed = true;
            }

            if (Gamepad.current != null)
            {
                var gp = Gamepad.current;
                Vector2 stick = gp.leftStick.ReadValue();
                if (stick.sqrMagnitude > 0.04f)
                    input += stick;

                if (gp.dpad.up.isPressed) input.y += 1f;
                if (gp.dpad.down.isPressed) input.y -= 1f;
                if (gp.dpad.left.isPressed) input.x -= 1f;
                if (gp.dpad.right.isPressed) input.x += 1f;

                if (gp.buttonSouth.isPressed || gp.rightTrigger.isPressed)
                    runPressed = true;
            }
        }
        catch
        {
            // Fallback si no está inicializado el New Input System
        }
#endif

        // 2. Soporte fallback para teclas y ejes directos
        try
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) input.y += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) input.y -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) input.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) input.x += 1f;

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                runPressed = true;

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            if (Mathf.Abs(h) > 0.1f) input.x = h;
            if (Mathf.Abs(v) > 0.1f) input.y = v;
        }
        catch
        {
            // Ignorar si el Legacy Input Manager está desactivado
        }

        moveInput = input.sqrMagnitude > 1f ? input.normalized : input;
        isRunning = runPressed && (moveInput.sqrMagnitude > 0.01f);
    }

    private void UpdateVisuals()
    {
        if (sr == null) return;

        // Volteo horizontal según dirección
        if (moveInput.x > 0.01f)
            sr.flipX = false;
        else if (moveInput.x < -0.01f)
            sr.flipX = true;

        float speed = moveInput.magnitude * (isRunning ? runSpeed : walkSpeed);

        // Si hay Animator con Controller válido, pasarle los parámetros
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetFloat(ParamSpeed, speed);
            animator.SetBool(ParamIsRunning, isRunning);
        }

        // Animación directa de cuadros para máxima fiabilidad
        UpdateSpriteFrames();
    }

    private void UpdateSpriteFrames()
    {
        Sprite[] currentSprites = idleSprites;
        float fps = idleFps;

        if (moveInput.sqrMagnitude > 0.01f)
        {
            if (isRunning && runSprites != null && runSprites.Length > 0)
            {
                currentSprites = runSprites;
                fps = runFps;
            }
            else if (walkSprites != null && walkSprites.Length > 0)
            {
                currentSprites = walkSprites;
                fps = walkFps;
            }
        }

        if (currentSprites == null || currentSprites.Length == 0) return;

        animTimer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(fps, 0.01f);

        if (animTimer >= frameDuration)
        {
            animTimer -= frameDuration;
            currentFrame = (currentFrame + 1) % currentSprites.Length;
            if (sr != null)
            {
                sr.sprite = currentSprites[currentFrame];
            }
        }
    }
}
