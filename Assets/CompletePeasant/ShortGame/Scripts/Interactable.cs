using UnityEngine;

namespace CompletePeasant.ShortGame
{
    public enum Interaction { Checkpoint, Person, Exit, Shrine }
    public class Interactable : MonoBehaviour
    {
        public Interaction kind;
        public string label;
        public int checkpointIndex;
        public Skill reward;
        [TextArea(2, 4)] public string dialogue;
        public void Use()
        {
            Adventure game = Adventure.Instance;
            if (game.Loading || game.Player.Dead) return;
            if (kind == Interaction.Checkpoint) game.Rest();
            else if (kind == Interaction.Exit) game.Proceed();
            else if (kind == Interaction.Shrine) game.GrantSkill(reward);
            else if (game.Data.bossesDefeated > game.Stage.data.index)
                game.ui.Dialogue(game.Stage.data.index < game.stages.Length - 1 ? "The embers lead east." : "Dawn restored.");
            else game.ui.Story(gameObject.name, dialogue);
        }
    }
}
