using System.Collections.Generic;

namespace Dolores1810
{
    /// <summary>Datos que se guardan en disco (firmados para detectar manipulación).</summary>
    [System.Serializable]
    public class SaveData
    {
        public int version = 1;
        public int coins;
        public List<string> ownedWeapons = new List<string> { "hacha" };
        public string equippedWeapon = "hacha";
        public int heartsLevel;
        public int staminaCapLevel;
        public int staminaRecLevel;
        public int speedLevel;
        public int attackLevel;
        public int shieldLevel;
        public List<int> completedLevels = new List<int>();
        public List<string> openedChests = new List<string>();
        public List<string> foundCollectibles = new List<string>();
        public List<string> doneMissions = new List<string>();
        public int totalKills;
        public int savedLives = 3;
        public bool hasStarted;
        public int lastLevel = 1;
        public bool gameFinished;

        // Estadísticas acumuladas por nivel (índices 0..3 corresponden a Niveles 1..4)
        public int[] levelKills = new int[4];
        public int[] levelCoins = new int[4];
        public int[] levelScores = new int[4];
        public float[] levelTimes = new float[4];
        public int[] levelBreaks = new int[4];
        public int[] levelMissions = new int[4];
    }
}
