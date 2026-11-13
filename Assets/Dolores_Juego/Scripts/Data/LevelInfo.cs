using UnityEngine;

namespace Dolores1810
{
    [System.Serializable]
    public class LevelInfo
    {
        public int index = 1;
        public string sceneName = "Level1";
        public string title = "";
        [TextArea] public string description = "";
        public string introLine = "";
        public Sprite thumbnail;
    }
}
