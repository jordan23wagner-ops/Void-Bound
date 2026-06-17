using UnityEngine;

namespace VoidBound.Data
{
    [CreateAssetMenu(fileName = "New RecipeDefinition", menuName = "VoidBound/Recipe Definition")]
    public class RecipeDefinitionSO : ScriptableObject
    {
        public string recipeId;
        public string displayName;
        public SkillType requiredSkill;
        public int requiredSkillLevel;
    }
}
