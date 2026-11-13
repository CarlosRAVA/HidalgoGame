using UnityEngine;

namespace Dolores1810
{
    /// <summary>Catálogo con todas las armas, mejoras, enemigos y niveles. Vive en Resources/GameDatabase.</summary>
    public class GameDatabase : ScriptableObject
    {
        public WeaponData[] weapons;
        public ShopItemData[] shopItems;
        public EnemyConfig[] enemies;
        public LevelInfo[] levels;

        static GameDatabase instance;

        public static GameDatabase I
        {
            get
            {
                if (instance == null) instance = Resources.Load<GameDatabase>("GameDatabase");
                return instance;
            }
        }

        public WeaponData GetWeapon(string id)
        {
            if (weapons == null) return null;
            for (int i = 0; i < weapons.Length; i++) if (weapons[i] != null && weapons[i].id == id) return weapons[i];
            return null;
        }

        public ShopItemData GetItem(string id)
        {
            if (shopItems == null) return null;
            for (int i = 0; i < shopItems.Length; i++) if (shopItems[i] != null && shopItems[i].id == id) return shopItems[i];
            return null;
        }

        public ShopItemData FindUpgrade(ShopKind kind)
        {
            if (shopItems == null) return null;
            for (int i = 0; i < shopItems.Length; i++) if (shopItems[i] != null && shopItems[i].kind == kind) return shopItems[i];
            return null;
        }

        public LevelInfo GetLevel(int index)
        {
            if (levels == null) return null;
            for (int i = 0; i < levels.Length; i++) if (levels[i] != null && levels[i].index == index) return levels[i];
            return null;
        }

        public int WeaponIndex(string id)
        {
            if (weapons == null) return -1;
            for (int i = 0; i < weapons.Length; i++) if (weapons[i] != null && weapons[i].id == id) return i;
            return -1;
        }
    }
}
