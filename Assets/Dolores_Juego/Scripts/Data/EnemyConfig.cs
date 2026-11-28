using UnityEngine;

namespace Dolores1810
{
    /// <summary>Configuración de un tipo de enemigo (ScriptableObject). Se edita en el Inspector.</summary>
    public class EnemyConfig : ScriptableObject
    {
        public string displayName = "Soldado";
        public EnemyTier tier = EnemyTier.Debil;
        public bool isBoss;
        [Tooltip("1 = lancero, 2 = hachero, 3 = oficial dorado")]
        public int skin = 1;
        public Color tint = Color.white;
        public float scale = 1f;

        [Header("Resistencia y daño")]
        [Tooltip("Golpes con el arma inicial (daño 10) para derrotarlo")]
        public int hitsToKill = 2;
        [Tooltip("Corazones que quita al jugador por ataque")]
        public int heartsDamage = 1;

        [Header("Comportamiento")]
        public float patrolSpeed = 1.6f;
        public float chaseSpeed = 3.2f;
        public float aggroRange = 9f;
        public float loseRange = 15f;
        public float attackRange = 2.5f;
        public float attackCooldown = 1.6f;
        [Range(0f, 1f)] public float staggerChance = 1f;

        [Header("Cuerpo y alcance")]
        public Vector2 hitBoxSize = new Vector2(2.7f, 1.3f);
        public Vector2 hitBoxOffset = new Vector2(1.7f, 0.9f);
        public float colliderWidth = 0.9f;
        public float colliderHeight = 1.75f;

        [Header("Recompensas")]
        public int coinReward = 3;
        public int scoreValue = 50;
        [Range(0f, 1f)] public float appleChance = 0.1f;
        public bool dropsHeart;

        public int MaxHealth { get { return hitsToKill * GameRules.BaseWeaponDamage; } }

        public void ApplyTierDefaults()
        {
            int t = Mathf.Clamp((int)tier, 0, 3);
            hitsToKill = GameRules.TierHits[t];
            heartsDamage = GameRules.TierHearts[t];
        }
    }
}
