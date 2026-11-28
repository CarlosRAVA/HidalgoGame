using UnityEngine;

namespace Dolores1810
{
    /// <summary>Arma del jugador (ScriptableObject).</summary>
    public class WeaponData : ScriptableObject
    {
        public string id = "hacha";
        public string displayName = "Hacha de leñador";
        [TextArea] public string description = "";
        public Sprite icon;
        public int damage = 10;
        [Tooltip("Multiplica la velocidad de la animación de ataque")]
        public float attackSpeed = 1f;
        [Tooltip("Multiplica el alcance del golpe")]
        public float reach = 1f;
        public int price = 0;
        public float knockback = 1f;
        public SpriteClip attackClip;
        public int hitStartFrame = 3;
        public int hitEndFrame = 5;
        public Color slashColor = Color.white;
        public SfxId swingSfx = SfxId.Swing;
    }
}
