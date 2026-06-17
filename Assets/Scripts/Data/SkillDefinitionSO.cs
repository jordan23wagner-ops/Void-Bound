using UnityEngine;

namespace VoidBound.Data
{
    [CreateAssetMenu(fileName = "New SkillDefinition", menuName = "VoidBound/Skill Definition")]
    public class SkillDefinitionSO : ScriptableObject
    {
        public string skillId;
        public string displayName;
        public SkillType skillType;
    }
}
