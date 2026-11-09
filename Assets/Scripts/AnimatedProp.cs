using UnityEngine;

/// <summary>
/// Reproduce una secuencia de sprites en bucle para props animados del escenario
/// (por ejemplo: el caldero alquímico, fogatas, sartenes mágicas, etc.).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class AnimatedProp : MonoBehaviour
{
    [Header("Sprites de la Animación")]
    [Tooltip("Arrastra aquí los sprites del Sprite Sheet en orden")]
    public Sprite[] frames;

    [Header("Configuración")]
    [Tooltip("Velocidad de reproducción en cuadros por segundo")]
    [Range(1f, 30f)]
    public float framesPerSecond = 8f;

    [Tooltip("Si debe repetirse en bucle continuo")]
    public bool loop = true;

    [Tooltip("Desfase inicial aleatorio para que múltiples props del mismo tipo no estén sincronizados")]
    public bool randomStartFrame = true;

    private SpriteRenderer spriteRenderer;
    private float timer;
    private int currentIndex;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (frames != null && frames.Length > 0 && randomStartFrame)
        {
            currentIndex = Random.Range(0, frames.Length);
            spriteRenderer.sprite = frames[currentIndex];
        }
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;

        timer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(framesPerSecond, 0.001f);

        if (timer >= frameDuration)
        {
            timer -= frameDuration;
            currentIndex++;

            if (currentIndex >= frames.Length)
            {
                if (loop)
                {
                    currentIndex = 0;
                }
                else
                {
                    currentIndex = frames.Length - 1;
                    enabled = false;
                    return;
                }
            }

            spriteRenderer.sprite = frames[currentIndex];
        }
    }

    public void SetFrames(Sprite[] newFrames, float fps = 8f)
    {
        frames = newFrames;
        framesPerSecond = fps;
        currentIndex = 0;
        timer = 0f;
        if (spriteRenderer != null && frames != null && frames.Length > 0)
        {
            spriteRenderer.sprite = frames[0];
        }
    }
}

