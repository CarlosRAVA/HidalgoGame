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
                if (instance == null)
                {
                    instance = Resources.Load<GameDatabase>("GameDatabase");
                    if (instance != null) instance.EnsureUpgrades();
                }
                return instance;
            }
        }

        public void EnsureUpgrades()
        {
            if (FindUpgrade(ShopKind.Shield) != null) return;
            var shieldItem = ScriptableObject.CreateInstance<ShopItemData>();
            shieldItem.id = "mejora_escudo";
            shieldItem.kind = ShopKind.Shield;
            shieldItem.displayName = "Escudo insurgente";
            shieldItem.description = "Absorbe golpes enemigos antes de perder corazones (+1 carga de escudo por nivel, se regenera fuera de peligro).";
            shieldItem.prices = new int[] { 110, 220, 350 };
            shieldItem.weaponId = "";
            shieldItem.icon = CreateFallbackShieldSprite();

            int oldLen = shopItems != null ? shopItems.Length : 0;
            var next = new ShopItemData[oldLen + 1];
            for (int i = 0; i < oldLen; i++) next[i] = shopItems[i];
            next[oldLen] = shieldItem;
            shopItems = next;
        }

        static Sprite CreateFallbackShieldSprite()
        {
            const int sz = 64;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            var clear = new Color(0f, 0f, 0f, 0f);
            var border = new Color(0.15f, 0.12f, 0.1f, 1f);
            var steel = new Color(0.55f, 0.65f, 0.78f, 1f);
            var gold = new Color(0.95f, 0.78f, 0.25f, 1f);
            for (int y = 0; y < sz; y++)
            {
                float ny = y / (float)(sz - 1);
                float halfW = ny > 0.45f ? 0.38f : Mathf.Lerp(0.05f, 0.38f, Mathf.Pow(ny / 0.45f, 0.7f));
                for (int x = 0; x < sz; x++)
                {
                    float dx = Mathf.Abs(x / (float)(sz - 1) - 0.5f);
                    if (ny < 0.08f || ny > 0.92f || dx > halfW) { tex.SetPixel(x, y, clear); continue; }
                    bool isBorder = ny < 0.14f || ny > 0.86f || dx > halfW - 0.06f;
                    bool isEmblem = dx < 0.09f && ny > 0.3f && ny < 0.72f;
                    tex.SetPixel(x, y, isBorder ? border : (isEmblem ? gold : steel));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
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
