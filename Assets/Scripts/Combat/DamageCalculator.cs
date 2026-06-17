using UnityEngine;

namespace VoidBound.Combat
{
    public static class DamageCalculator
    {
        private const float CritMultiplier = 1.5f;

        public static int CalculateDamage(StatsComponent attacker, StatsComponent target, int baseDamage)
        {
            float rawDamage = attacker.PhysicalDamage(baseDamage);

            bool isCrit = Random.value < attacker.CritChance;
            if (isCrit)
                rawDamage *= CritMultiplier;

            float reduced = rawDamage * target.DefenseMultiplier;
            int finalDamage = Mathf.Max(1, Mathf.RoundToInt(reduced));

            FloatingDamageNumber.Spawn(target.transform.position, finalDamage, isCrit);

            return finalDamage;
        }
    }
}
