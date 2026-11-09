using UnityEngine;

/// <summary>
/// Anima un SpriteRenderer recorriendo frames de un sprite sheet en una fila.
/// Úsalo para props animados como el caldero (Alchemy Table), Bonfire, Grill, etc.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteSheetAnimator : MonoBehaviour
{
    [Header("Sprite Sheet")]
    [Tooltip("Todos los sprites del sheet sliceados en este array")]
    public Sprite[] frames;

    [Header("Animación")]
    [Tooltip("Frames por segundo")]
    public float fps = 8f;

    [Tooltip("¿La animación se repite en loop?")]
    public bool loop = true;

    [Tooltip("Offset de frame inicial aleatorio para que los props no estén sincronizados")]
    public bool randomOffset = true;

    private SpriteRenderer sr;
    private float timer;
    private int currentFrame;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        if (randomOffset && frames != null && frames.Length > 0)
            currentFrame = Random.Range(0, frames.Length);
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= 1f / fps)
        {
            timer = 0f;
            currentFrame++;

            if (currentFrame >= frames.Length)
            {
                if (loop)
                    currentFrame = 0;
                else
                {
                    currentFrame = frames.Length - 1;
                    enabled = false;
                }
            }

            sr.sprite = frames[currentFrame];
        }
    }
}
