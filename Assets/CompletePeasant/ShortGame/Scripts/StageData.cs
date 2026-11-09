using UnityEngine;

namespace CompletePeasant.ShortGame
{
    public enum Skill
    {
        None,
        EmberDash,
        AshWings,
        Sunbreak
    }

    [CreateAssetMenu(menuName = "Complete Peasant/Short stage")]
    public class StageData : ScriptableObject
    {
        public int index;
        public string title;
        public string sceneName;
        public string bossName;
        public bool hasBoss = true;
        public int bossPattern;
        public Skill reward;
        [TextArea(2, 4)]
        public string story;
        [TextArea(2, 4)]
        public string victoryLine;
        public Sprite background;
        public Color sky;
        public AudioClip music, bossMusic;
    }
}
