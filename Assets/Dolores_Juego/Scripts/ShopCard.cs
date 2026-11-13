using UnityEngine;
using UnityEngine.EventSystems;

namespace Dolores1810
{
    /// <summary>Tarjeta de la tienda: al seleccionarla o pasar el cursor muestra el detalle.</summary>
    public class ShopCard : MonoBehaviour, ISelectHandler, IPointerEnterHandler
    {
        public ShopUI owner;
        public int index;

        public void OnSelect(BaseEventData eventData) { if (owner != null) owner.ShowDetail(index); }
        public void OnPointerEnter(PointerEventData eventData) { if (owner != null) owner.ShowDetail(index); }
    }
}
