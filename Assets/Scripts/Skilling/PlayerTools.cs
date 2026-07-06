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

        public RarityTier GetToolTier(SkillType skill) =>
            tiers.TryGetValue(skill, out var t) ? t : RarityTier.Common;

        public void SetToolTier(SkillType skill, RarityTier tier)
        {
            if (!tiers.TryGetValue(skill, out var cur) || tier > cur)
                tiers[skill] = tier;
        }
    }
}
