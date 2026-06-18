using System;
using UnityEngine;
using VoidBound.Data;
using VoidBound.Skilling;

namespace VoidBound.Combat
{
    public enum WeaponStyle { Melee, Ranged, Magic }

    public static class WeaponStyleMap
    {
        public static WeaponStyle GetStyle(WeaponType type)
        {
            return type switch
            {
                WeaponType.Sword => WeaponStyle.Melee,
                WeaponType.Sword2H => WeaponStyle.Melee,
                WeaponType.Mace => WeaponStyle.Melee,
                WeaponType.Dagger => WeaponStyle.Melee,
                WeaponType.Bow => WeaponStyle.Ranged,
                WeaponType.Crossbow => WeaponStyle.Ranged,
                WeaponType.Staff => WeaponStyle.Magic,
                WeaponType.Wand => WeaponStyle.Magic,
                _ => WeaponStyle.Melee
            };
        }

        public static SkillType GetStatSkill(WeaponStyle style)
        {
            return style switch
            {
                WeaponStyle.Melee => SkillType.CombatSTR,
                WeaponStyle.Ranged => SkillType.CombatDEX,
                WeaponStyle.Magic => SkillType.CombatINT,
                _ => SkillType.CombatSTR
            };
        }
    }

    public static class CombatXPCalculator
    {
        private const float VigXPRatio = 0.5f;

        public static void AwardCombatXP(PlayerSkills skills, WeaponType weaponType, int damageDealt)
        {
            if (skills == null || damageDealt <= 0) return;

            var style = WeaponStyleMap.GetStyle(weaponType);
            var statSkill = WeaponStyleMap.GetStatSkill(style);

            int xpGain = damageDealt;
            skills.AddXP(statSkill, xpGain);

            int vigXP = Mathf.Max(1, Mathf.RoundToInt(xpGain * VigXPRatio));
            skills.AddXP(SkillType.CombatVIG, vigXP);
        }
    }

    public static class CombatLevelCalculator
    {
        public static int GetCombatLevel(PlayerSkills skills)
        {
            if (skills == null) return 1;
            int vig = skills.GetLevel(SkillType.CombatVIG);
            int str = skills.GetLevel(SkillType.CombatSTR);
            int dex = skills.GetLevel(SkillType.CombatDEX);
            int intel = skills.GetLevel(SkillType.CombatINT);
            return Mathf.Max(1, Mathf.RoundToInt((vig + str + dex + intel) / 4f));
        }
    }
}
