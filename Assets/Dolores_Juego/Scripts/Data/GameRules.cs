using UnityEngine;

namespace Dolores1810
{
    public enum EnemyTier { Debil, Intermedio, Resistente, MuyResistente }

    public enum ShopKind { Weapon, Hearts, StaminaCapacity, StaminaRecovery, Speed, Attack }

    /// <summary>Reglas numéricas del juego en un solo lugar.</summary>
    public static class GameRules
    {
        /// <summary>Daño del arma inicial (hacha). Los golpes necesarios se calculan contra este valor.</summary>
        public const int BaseWeaponDamage = 10;
        public const int BaseHearts = 5;
        public const int MaxHeartUpgrades = 2;
        public const int MaxLives = 9;

        /// <summary>Golpes necesarios (con el arma inicial) y daño en corazones por resistencia.</summary>
        public static readonly int[] TierHits = { 2, 4, 6, 8 };
        public static readonly int[] TierHearts = { 1, 2, 3, 4 };

        public static float StaminaBase = 100f;
        public static float StaminaRecoveryBase = 22f;
    }
}
