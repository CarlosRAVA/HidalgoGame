using UnityEngine;

namespace Dolores1810
{
    /// <summary>Campana decorativa que se balancea suavemente (menú principal).</summary>
    public class BellSway : MonoBehaviour
    {
        public float amplitude = 5f;
        public float speed = 1.4f;

        void Update()
        {
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * speed) * amplitude);
        }
    }
}
