using UnityEngine;

namespace VoidBound.Data
{
    [CreateAssetMenu(fileName = "New SkillDefinition", menuName = "VoidBound/Skill Definition")]
    public class SkillDefinitionSO : ScriptableObject
    {
        public string skillId;
        public string displayName;
        public SkillType skillType;

        [Header("XP Curve (3x multiplier, level 99 cap)")]
        public float xpMultiplier = 3f;
        public int maxLevel = 99;

        public RecipeDefinitionSO[] recipes;

        public int XPForLevel(int level)
        {
            if (level <= 1) return 0;
            // OSRS-style XP curve from RunePortal: sum(i + 300 * 2^(i/7)) / 4 * 3
            double total = 0;
            for (int i = 1; i < level; i++)
                total += i + 300.0 * System.Math.Pow(2, i / 7.0);
            return (int)(total / 4.0 * xpMultiplier);
        }
    }
}
