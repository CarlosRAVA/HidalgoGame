using System;
using UnityEngine;

namespace Dolores1810
{
    /// <summary>
    /// Progreso persistente del jugador: monedas, armas, mejoras, niveles. Es la única puerta de acceso a los datos
    /// guardados, así que todas las compras y desbloqueos pasan por aquí y se validan en un solo lugar.
    /// </summary>
    public static class PlayerProgress
    {
        static SaveData data;
        static bool dirty;
        static bool hooked;

        public static event Action Changed;

        public static SaveData Data
        {
            get
            {
                if (data == null)
                {
                    data = SaveSystem.Load();
                    if (!hooked) { hooked = true; Application.quitting += SaveNow; }
                }
                return data;
            }
        }

        static void Touch()
        {
            dirty = true;
            if (Changed != null) Changed();
        }

        public static void SaveNow()
        {
            if (data == null || !dirty) return;
            SaveSystem.Save(data);
            dirty = false;
        }

        public static void ReloadFromDisk()
        {
            data = null;
            dirty = false;
        }

        public static void ResetAll()
        {
            SaveSystem.Delete();
            data = new SaveData();
            dirty = true;
            SaveNow();
            if (Changed != null) Changed();
        }

        // ---------------------------------------------------------------- monedas
        public static int Coins { get { return Data.coins; } }

        public static void AddCoins(int n)
        {
            if (n <= 0) return;
            Data.coins = Mathf.Min(999999, Data.coins + n);
            Touch();
        }

        public static bool TrySpend(int n)
        {
            if (n < 0 || Data.coins < n) return false;
            Data.coins -= n;
            Touch();
            return true;
        }

        // ---------------------------------------------------------------- niveles
        public static bool IsCompleted(int level) { return Data.completedLevels.Contains(level); }

        /// <summary>El nivel 1 siempre está disponible; los demás se calculan a partir de los niveles completados.</summary>
        public static bool IsLevelUnlocked(int level) { return level <= 1 || IsCompleted(level - 1); }

        public static void CompleteLevel(int level)
        {
            if (level < 1 || level > 3) return;
            if (!Data.completedLevels.Contains(level)) Data.completedLevels.Add(level);
            if (level == 3) Data.gameFinished = true;
            Data.hasStarted = true;
            Data.lastLevel = Mathf.Min(3, level + 1);
            Touch();
            SaveNow();
        }

        public static void MarkStarted(int level)
        {
            Data.hasStarted = true;
            Data.lastLevel = Mathf.Clamp(level, 1, 3);
            Touch();
        }

        public static int ContinueLevel()
        {
            for (int i = 3; i >= 1; i--) if (IsLevelUnlocked(i) && !IsCompleted(i)) return i;
            return 1;
        }

        // ---------------------------------------------------------------- marcas del mundo
        public static bool IsChestOpened(string id) { return Data.openedChests.Contains(id); }
        public static void MarkChestOpened(string id) { if (!Data.openedChests.Contains(id)) { Data.openedChests.Add(id); Touch(); } }
        public static bool IsCollected(string id) { return Data.foundCollectibles.Contains(id); }
        public static void MarkCollected(string id) { if (!Data.foundCollectibles.Contains(id)) { Data.foundCollectibles.Add(id); Touch(); } }
        public static bool IsMissionDone(string id) { return Data.doneMissions.Contains(id); }
        public static void MarkMissionDone(string id) { if (!Data.doneMissions.Contains(id)) { Data.doneMissions.Add(id); Touch(); } }
        public static void AddKills(int n) { Data.totalKills += n; dirty = true; }

        public static int SavedLives
        {
            get { return Data.savedLives > 0 ? Mathf.Clamp(Data.savedLives, 1, GameRules.MaxLives) : 3; }
        }

        public static void SetSavedLives(int lives)
        {
            Data.savedLives = Mathf.Clamp(lives, 1, GameRules.MaxLives);
            Touch();
        }

        // ---------------------------------------------------------------- armas
        public static bool OwnsWeapon(string id) { return Data.ownedWeapons.Contains(id); }
        public static string EquippedWeaponId { get { return Data.equippedWeapon; } }

        public static bool Equip(string id)
        {
            if (!OwnsWeapon(id)) return false;
            if (Data.equippedWeapon == id) return true;
            Data.equippedWeapon = id;
            Touch();
            return true;
        }

        public static bool TryBuyWeapon(ShopItemData item, WeaponData w)
        {
            if (item == null || w == null || OwnsWeapon(w.id)) return false;
            if (!TrySpend(w.price)) return false;
            Data.ownedWeapons.Add(w.id);
            Data.equippedWeapon = w.id;
            Touch();
            SaveNow();
            return true;
        }

        // ---------------------------------------------------------------- mejoras
        public static int GetUpgradeLevel(ShopKind kind)
        {
            switch (kind)
            {
                case ShopKind.Hearts: return Data.heartsLevel;
                case ShopKind.StaminaCapacity: return Data.staminaCapLevel;
                case ShopKind.StaminaRecovery: return Data.staminaRecLevel;
                case ShopKind.Speed: return Data.speedLevel;
                case ShopKind.Attack: return Data.attackLevel;
                case ShopKind.Shield: return Data.shieldLevel;
            }
            return 0;
        }

        static void SetUpgradeLevel(ShopKind kind, int v)
        {
            switch (kind)
            {
                case ShopKind.Hearts: Data.heartsLevel = v; break;
                case ShopKind.StaminaCapacity: Data.staminaCapLevel = v; break;
                case ShopKind.StaminaRecovery: Data.staminaRecLevel = v; break;
                case ShopKind.Speed: Data.speedLevel = v; break;
                case ShopKind.Attack: Data.attackLevel = v; break;
                case ShopKind.Shield: Data.shieldLevel = v; break;
            }
        }

        public static int MaxUpgradeLevel(ShopItemData item)
        {
            if (item == null) return 0;
            if (item.kind == ShopKind.Hearts) return Mathf.Min(item.MaxLevel, GameRules.MaxHeartUpgrades);
            if (item.kind == ShopKind.Shield) return Mathf.Min(item.MaxLevel, GameRules.MaxShieldUpgrades);
            return item.MaxLevel;
        }

        public static bool TryBuyUpgrade(ShopItemData item)
        {
            if (item == null || item.kind == ShopKind.Weapon) return false;
            int lvl = GetUpgradeLevel(item.kind);
            if (lvl >= MaxUpgradeLevel(item)) return false;
            if (!TrySpend(item.prices[lvl])) return false;
            SetUpgradeLevel(item.kind, lvl + 1);
            Touch();
            SaveNow();
            return true;
        }
    }

    /// <summary>Valores derivados de las mejoras compradas.</summary>
    public static class PlayerStats
    {
        public static int MaxHearts { get { return GameRules.BaseHearts + Mathf.Min(PlayerProgress.Data.heartsLevel, GameRules.MaxHeartUpgrades); } }
        public static float StaminaMax { get { return GameRules.StaminaBase * (1f + 0.25f * PlayerProgress.Data.staminaCapLevel); } }
        public static float StaminaRecovery { get { return GameRules.StaminaRecoveryBase * (1f + 0.3f * PlayerProgress.Data.staminaRecLevel); } }
        public static float SpeedMultiplier { get { return 1f + 0.06f * PlayerProgress.Data.speedLevel; } }
        public static float AttackMultiplier { get { return 1f + 0.15f * PlayerProgress.Data.attackLevel; } }
        public static int MaxShield { get { return Mathf.Clamp(PlayerProgress.Data.shieldLevel, 0, GameRules.MaxShieldUpgrades); } }
        public static float ShieldRegenSeconds { get { int lvl = Mathf.Max(1, PlayerProgress.Data.shieldLevel); return Mathf.Max(4f, GameRules.ShieldRegenBaseSeconds - (lvl - 1) * 1.5f); } }
    }
}
