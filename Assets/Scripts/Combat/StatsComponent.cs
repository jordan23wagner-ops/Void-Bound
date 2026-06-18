using UnityEngine;
using VoidBound.Data;
using VoidBound.Skilling;

namespace VoidBound.Combat
{
    public class StatsComponent : MonoBehaviour
    {
        [SerializeField] private CharacterStats baseStats = new CharacterStats(10, 10, 10, 10);
        [SerializeField] private CharacterStats gearBonus;

        private PlayerSkills playerSkills;

        public CharacterStats BaseStats => baseStats;
        public CharacterStats GearBonus => gearBonus;

        public CharacterStats EffectiveStats
        {
            get
            {
                if (playerSkills != null)
                {
                    return new CharacterStats(
                        playerSkills.GetLevel(SkillType.CombatSTR) + gearBonus.str,
                        playerSkills.GetLevel(SkillType.CombatDEX) + gearBonus.dex,
                        playerSkills.GetLevel(SkillType.CombatVIG) + gearBonus.vig,
                        playerSkills.GetLevel(SkillType.CombatINT) + gearBonus.intel
                    );
                }
                return baseStats + gearBonus;
            }
        }

        public int MaxHP
        {
            get
            {
                int vig = playerSkills != null
                    ? playerSkills.GetLevel(SkillType.CombatVIG) + gearBonus.vig
                    : baseStats.vig + gearBonus.vig;
                return 100 + vig * 10;
            }
        }

        public float PhysicalDamage(int baseDamage)
        {
            var s = EffectiveStats;
            return baseDamage * (1f + s.str * 0.02f);
        }

        public float MagicDamage(int baseDamage)
        {
            var s = EffectiveStats;
            return baseDamage * (1f + s.intel * 0.02f);
        }

        public float AttackInterval
        {
            get
            {
                var s = EffectiveStats;
                return 1f / (1f + s.dex * 0.01f);
            }
        }

        public float CritChance
        {
            get
            {
                var s = EffectiveStats;
                return Mathf.Min(s.dex * 0.005f, 0.5f);
            }
        }

        public float DefenseMultiplier
        {
            get
            {
                var s = EffectiveStats;
                return 100f / (100f + s.vig);
            }
        }

        private void Awake()
        {
            playerSkills = GetComponent<PlayerSkills>();
        }

        public void SetBaseStats(CharacterStats stats) => baseStats = stats;
        public void AddGearBonus(CharacterStats bonus) => gearBonus = gearBonus + bonus;
        public void RemoveGearBonus(CharacterStats bonus)
        {
            gearBonus = new CharacterStats(
                gearBonus.str - bonus.str,
                gearBonus.dex - bonus.dex,
                gearBonus.vig - bonus.vig,
                gearBonus.intel - bonus.intel
            );
        }
    }
}
