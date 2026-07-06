using UnityEngine;

namespace VoidBound.Data
{
    // A gather tool (axe / rod / pickaxe / sickle). Crafting one raises the
    // player's tool tier for its skill (PlayerTools), which gates what that
    // skill can harvest (GDD §5). Not an equipment slot — tools are owned, not
    // worn.
    [CreateAssetMenu(fileName = "New Tool", menuName = "VoidBound/Tool Item")]
    public class ToolItemSO : ScriptableObject
    {
        public string itemId;
        public string displayName;
        public SkillType skill;  // the gather skill this tool serves
        public RarityTier tier;  // tool tier on the rarity spine
    }
}
