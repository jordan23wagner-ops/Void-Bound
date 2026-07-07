using System;
using System.Collections.Generic;
using UnityEngine;
using VoidBound.Data;

namespace VoidBound.Skilling
{
    [Serializable]
    public class SkillProgress
    {
        public SkillType type;
        public int level = 1;
        public int currentXP;
    }

    public class PlayerSkills : MonoBehaviour
    {
        [SerializeField] private SkillDefinitionSO[] skillDefinitions;

        private readonly Dictionary<SkillType, SkillProgress> skills = new();
        public event Action<SkillType, int, int> OnSkillXPChanged;
        public event Action<SkillType, int> OnSkillLevelUp;

        public IReadOnlyDictionary<SkillType, SkillProgress> Skills => skills;

        private void Awake()
        {
            foreach (SkillType type in Enum.GetValues(typeof(SkillType)))
                skills[type] = new SkillProgress { type = type };
        }

        public void SetDefinitions(SkillDefinitionSO[] defs)
        {
            skillDefinitions = defs;
        }

        public void AddXP(SkillType type, int amount)
        {
            if (!skills.TryGetValue(type, out var progress)) return;

            var def = GetDefinition(type);
            if (def == null || progress.level >= def.maxLevel) return;

            progress.currentXP += amount;
            OnSkillXPChanged?.Invoke(type, progress.currentXP, def.XPForLevel(progress.level + 1));

            while (progress.currentXP >= def.XPForLevel(progress.level + 1) && progress.level < def.maxLevel)
            {
                progress.currentXP -= def.XPForLevel(progress.level + 1);
                progress.level++;
                OnSkillLevelUp?.Invoke(type, progress.level);
                Debug.Log($"[Skill] {type} leveled up to {progress.level}!");
            }
        }

        // Restore a skill's progress from a save (bypasses XP curve / events).
        public void LoadProgress(SkillType type, int level, int xp)
        {
            if (!skills.TryGetValue(type, out var p)) { p = new SkillProgress { type = type }; skills[type] = p; }
            p.level = Mathf.Max(1, level);
            p.currentXP = Mathf.Max(0, xp);
            OnSkillXPChanged?.Invoke(type, p.currentXP, GetXPToNext(type));
        }

        public int GetLevel(SkillType type) => skills.TryGetValue(type, out var p) ? p.level : 1;
        public int GetXP(SkillType type) => skills.TryGetValue(type, out var p) ? p.currentXP : 0;

        public int GetXPToNext(SkillType type)
        {
            var def = GetDefinition(type);
            if (def == null) return 100;
            int lvl = GetLevel(type);
            return def.XPForLevel(lvl + 1);
        }

        private SkillDefinitionSO GetDefinition(SkillType type)
        {
            if (skillDefinitions == null) return null;
            foreach (var d in skillDefinitions)
                if (d != null && d.skillType == type) return d;
            return null;
        }
    }
}
