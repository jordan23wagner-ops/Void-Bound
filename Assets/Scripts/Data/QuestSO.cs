using System;
using UnityEngine;

namespace VoidBound.Data
{
    public enum QuestObjectiveType { Gather, Craft, Kill }

    [Serializable]
    public struct QuestObjective
    {
        public QuestObjectiveType type;
        // Gather → material itemId; Craft → recipeId or output itemId; Kill →
        // enemyId ("" = any enemy counts).
        public string targetId;
        public int required;
        public string label; // player-facing, e.g. "Chop 3 Pale Oak Logs"
    }

    [Serializable]
    public struct QuestReward
    {
        public int gold;
        public int voidShards;
        public SkillType xpSkill;
        public int xpAmount;
        public MaterialItemSO rewardMaterial;
        public int rewardMaterialQty;
        public GearItemSO rewardGear;
    }

    // A single authored quest: a short chain of objectives plus a reward, given
    // by a QuestGiverStation and tracked by PlayerQuests. Authored by the editor
    // setup script (QuestContentSetup) so objective ids resolve to real assets.
    [CreateAssetMenu(fileName = "New Quest", menuName = "VoidBound/Quest")]
    public class QuestSO : ScriptableObject
    {
        public string questId;
        public string title;
        public string giverName = "Quest Giver";

        [TextArea] public string offerText;   // shown before accepting
        [TextArea] public string activeText;  // shown while in progress
        [TextArea] public string turnInText;  // shown when complete, at turn-in

        public QuestObjective[] objectives;
        public QuestReward reward;
    }
}
