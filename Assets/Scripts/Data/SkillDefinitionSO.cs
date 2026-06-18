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
            return Mathf.RoundToInt(Mathf.Pow(level, 2) * xpMultiplier * 10f);
        }
    }
}
