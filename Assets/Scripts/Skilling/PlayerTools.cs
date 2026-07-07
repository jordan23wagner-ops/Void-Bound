using UnityEngine;
using VoidBound.Data;

namespace VoidBound.Skilling
{
    // The player's effective tool tier per gather/craft skill — the gate that
    // replaced skill levels (GDD §5). A recipe/resource is available when the
    // player's tool tier for that skill is >= the requirement.
    //
    // Placeholder until the Crafting slice adds real, craftable tools: every
    // skill currently reports Common (so Common-gated recipes are open, higher
    // ones locked). SetToolTier lets future tools raise it.
    public class PlayerTools : MonoBehaviour
    {
        private readonly System.Collections.Generic.Dictionary<SkillType, RarityTier> tiers = new();

        // A skill's tool is "owned" once its (free) tier-0 tool has been crafted.
        // Gathering requires this; until then the player has no tool for it.
        public bool HasTool(SkillType skill) => tiers.ContainsKey(skill);

        // Owned tools (skill → tier), for save/load.
        public System.Collections.Generic.IReadOnlyDictionary<SkillType, RarityTier> Owned => tiers;

        public RarityTier GetToolTier(SkillType skill) =>
            tiers.TryGetValue(skill, out var t) ? t : RarityTier.Common;

        public void SetToolTier(SkillType skill, RarityTier tier)
        {
            if (!tiers.TryGetValue(skill, out var cur) || tier > cur)
                tiers[skill] = tier;
        }

        // Set the tier unconditionally (save load), bypassing the raise-only rule.
        public void LoadTier(SkillType skill, RarityTier tier) => tiers[skill] = tier;
    }
}
