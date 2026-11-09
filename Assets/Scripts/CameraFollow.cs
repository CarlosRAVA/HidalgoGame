using UnityEngine;

/// <summary>
/// Cámara 2D que sigue suavemente al jugador manteniendo una vista centrada del nivel.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Objetivo")]
    [Tooltip("Transform del jugador a seguir")]
    public Transform target;

    [Header("Ajustes de Seguimiento")]
    [Range(0.01f, 1f)]
    public float smoothSpeed = 0.12f;

    [Tooltip("Desplazamiento respecto al objetivo")]
    public Vector3 offset = new Vector3(0, 0.5f, -10f);

    [Header("Límites del Nivel")]
    public bool enableBounds = true;
    public float minX = -12f;
    public float maxX = 12f;
    public float minY = -5f;
    public float maxY = 5f;

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                player = GameObject.Find("Player_Body_A");
            }
            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;

        if (enableBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        }

        desiredPosition.z = offset.z; // Mantener la profundidad Z de la cámara fija

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
    }
}
