using UnityEngine;
using UnityEngine.EventSystems;

namespace Dolores1810
{
    /// <summary>Sonido al pasar el cursor o seleccionar un botón con teclado/control.</summary>
    public class UIButtonSounds : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            AudioHub.Play(SfxId.Hover, 0f, 0.6f);
        }

        public void OnSelect(BaseEventData eventData)
        {
            AudioHub.Play(SfxId.Hover, 0f, 0.6f);
        }
    }
}
