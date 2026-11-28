using UnityEngine;

namespace Dolores1810
{
    /// <summary>
    /// Ajusta automáticamente el contenedor de la interfaz a la zona segura (Safe Area)
    /// del dispositivo móvil para evitar que cámaras frontales (notches, punch-holes),
    /// esquinas curvas y barras de navegación del sistema tapen botones o información.
    /// Funciona de forma transparente en cualquier aspecto de pantalla (16:9, 18:9, 19.5:9, 20:9, tablets).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform targetRect;
        private Rect lastSafeArea = Rect.zero;
        private Vector2Int lastScreenSize = Vector2Int.zero;
        private ScreenOrientation lastOrientation = ScreenOrientation.Unknown;

        void Awake()
        {
            targetRect = GetComponent<RectTransform>();
            Refresh();
        }

        void OnEnable()
        {
            Refresh();
        }

        void Update()
        {
            if (lastSafeArea != Screen.safeArea ||
                lastScreenSize.x != Screen.width ||
                lastScreenSize.y != Screen.height ||
                lastOrientation != Screen.orientation)
            {
                Refresh();
            }
        }

        public void Refresh()
        {
            if (targetRect == null) targetRect = GetComponent<RectTransform>();
            if (Screen.width <= 0 || Screen.height <= 0) return;

            lastSafeArea = Screen.safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            lastOrientation = Screen.orientation;

            Vector2 min = lastSafeArea.position;
            Vector2 max = lastSafeArea.position + lastSafeArea.size;

            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            targetRect.anchorMin = min;
            targetRect.anchorMax = max;
            targetRect.offsetMin = Vector2.zero;
            targetRect.offsetMax = Vector2.zero;
        }
    }
}
