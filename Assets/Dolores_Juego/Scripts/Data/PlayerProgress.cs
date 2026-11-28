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
            if (level < 1 || level > 4) return;
            if (!Data.completedLevels.Contains(level)) Data.completedLevels.Add(level);
            if (level == 4) Data.gameFinished = true;
            Data.hasStarted = true;
            Data.lastLevel = Mathf.Min(4, level + 1);
            Touch();
            SaveNow();
        }

        public static void MarkStarted(int level)
        {
            Data.hasStarted = true;
            Data.lastLevel = Mathf.Clamp(level, 1, 4);
            Touch();
        }

        public static int ContinueLevel()
        {
            for (int i = 4; i >= 1; i--) if (IsLevelUnlocked(i) && !IsCompleted(i)) return i;
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

        /// <summary>Registra las estadísticas alcanzadas en un nivel (1..4) para la sumatoria final de toda la campaña.</summary>
        public static void RecordLevelStats(int levelIndex, int kills, int coinsEarned, int score, float elapsed, int breaks, int missionsDone)
        {
            int idx = Mathf.Clamp(levelIndex - 1, 0, 3);
            var d = Data;
            if (d.levelKills == null || d.levelKills.Length != 4) d.levelKills = new int[4];
            if (d.levelCoins == null || d.levelCoins.Length != 4) d.levelCoins = new int[4];
            if (d.levelScores == null || d.levelScores.Length != 4) d.levelScores = new int[4];
            if (d.levelTimes == null || d.levelTimes.Length != 4) d.levelTimes = new float[4];
            if (d.levelBreaks == null || d.levelBreaks.Length != 4) d.levelBreaks = new int[4];
            if (d.levelMissions == null || d.levelMissions.Length != 4) d.levelMissions = new int[4];

            if (kills > d.levelKills[idx]) d.levelKills[idx] = kills;
            if (coinsEarned > d.levelCoins[idx]) d.levelCoins[idx] = coinsEarned;
            if (score > d.levelScores[idx]) d.levelScores[idx] = score;
            if (elapsed > 0.1f) d.levelTimes[idx] = Mathf.Max(d.levelTimes[idx], elapsed);
            if (breaks > d.levelBreaks[idx]) d.levelBreaks[idx] = breaks;
            if (missionsDone > d.levelMissions[idx]) d.levelMissions[idx] = missionsDone;
            dirty = true;
        }

        public static int GetTotalKillsAllLevels(int curLevel, int curKills)
        {
            var d = Data;
            int idx = Mathf.Clamp(curLevel - 1, 0, 3);
            int sum = 0;
            for (int i = 0; i < 4; i++)
            {
                int v = (d.levelKills != null && i < d.levelKills.Length) ? d.levelKills[i] : 0;
                if (i == idx) v = Mathf.Max(v, curKills);
                sum += v;
            }
            return Mathf.Max(sum, d.totalKills);
        }

        public static int GetTotalCoinsAllLevels(int curLevel, int curCoins)
        {
            var d = Data;
            int idx = Mathf.Clamp(curLevel - 1, 0, 3);
            int sum = 0;
            for (int i = 0; i < 4; i++)
            {
                int v = (d.levelCoins != null && i < d.levelCoins.Length) ? d.levelCoins[i] : 0;
                if (i == idx) v = Mathf.Max(v, curCoins);
                sum += v;
            }
            return Mathf.Max(sum, d.coins);
        }

        public static int GetTotalScoreAllLevels(int curLevel, int curScore)
        {
            var d = Data;
            int idx = Mathf.Clamp(curLevel - 1, 0, 3);
            int sum = 0;
            for (int i = 0; i < 4; i++)
            {
                int v = (d.levelScores != null && i < d.levelScores.Length) ? d.levelScores[i] : 0;
                if (i == idx) v = Mathf.Max(v, curScore);
                sum += v;
            }
            return Mathf.Max(sum, curScore);
        }

        public static float GetTotalTimeAllLevels(int curLevel, float curTime)
        {
            var d = Data;
            int idx = Mathf.Clamp(curLevel - 1, 0, 3);
            float sum = 0f;
            for (int i = 0; i < 4; i++)
            {
                float v = (d.levelTimes != null && i < d.levelTimes.Length) ? d.levelTimes[i] : 0f;
                if (i == idx) v = Mathf.Max(v, curTime);
                sum += v;
            }
            return Mathf.Max(sum, curTime);
        }

        public static int GetTotalBreaksAllLevels(int curLevel, int curBreaks)
        {
            var d = Data;
            int idx = Mathf.Clamp(curLevel - 1, 0, 3);
            int sum = 0;
            for (int i = 0; i < 4; i++)
            {
                int v = (d.levelBreaks != null && i < d.levelBreaks.Length) ? d.levelBreaks[i] : 0;
                if (i == idx) v = Mathf.Max(v, curBreaks);
                sum += v;
            }
            return Mathf.Max(sum, curBreaks);
        }

        public static int GetTotalMissionsAllLevels(int curLevel, int curMissions)
        {
            var d = Data;
            int idx = Mathf.Clamp(curLevel - 1, 0, 3);
            int sum = 0;
            for (int i = 0; i < 4; i++)
            {
                int v = (d.levelMissions != null && i < d.levelMissions.Length) ? d.levelMissions[i] : 0;
                if (i == idx) v = Mathf.Max(v, curMissions);
                sum += v;
            }
            int globalDone = d.doneMissions != null ? d.doneMissions.Count : 0;
            return Mathf.Max(sum, globalDone);
        }

        public static int GetLevelScore(int levelOneBased, int curLevel, int curScore)
        {
            var d = Data;
            int idx = Mathf.Clamp(levelOneBased - 1, 0, 3);
            int v = (d.levelScores != null && idx < d.levelScores.Length) ? d.levelScores[idx] : 0;
            if (levelOneBased == curLevel) v = Mathf.Max(v, curScore);
            return v;
        }

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
