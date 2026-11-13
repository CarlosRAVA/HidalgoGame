using UnityEngine;

namespace Dolores1810
{
    /// <summary>Elemento de la tienda: arma o mejora con varios niveles de compra.</summary>
    public class ShopItemData : ScriptableObject
    {
        public string id;
        public ShopKind kind = ShopKind.Weapon;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        [Tooltip("Precio de cada nivel de compra. Las armas usan un solo precio.")]
        public int[] prices = { 100 };
        public string weaponId;

        public int MaxLevel { get { return prices != null ? prices.Length : 0; } }
    }
}
