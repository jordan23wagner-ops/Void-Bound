using System;

namespace VoidBound.Quests
{
    // Lightweight global signal hub the quest tracker listens to. The gameplay
    // producers (EnemyAI on death, ResourceNode on gather, CraftingUI on craft)
    // raise these; PlayerQuests subscribes and advances active objectives. Kept
    // static + decoupled so producers never need a reference to the quest system.
    public static class QuestEvents
    {
        // enemyId of the slain enemy (may be null/empty for an unclassified kill).
        public static event Action<string> EnemyKilled;
        // (materialId, quantity) actually harvested from a resource node.
        public static event Action<string, int> Gathered;
        // (recipeId, outputId) of a successful craft — either may match a Craft objective.
        public static event Action<string, string> Crafted;

        public static void RaiseEnemyKilled(string enemyId) => EnemyKilled?.Invoke(enemyId);
        public static void RaiseGathered(string materialId, int quantity) => Gathered?.Invoke(materialId, quantity);
        public static void RaiseCrafted(string recipeId, string outputId) => Crafted?.Invoke(recipeId, outputId);
    }
}
