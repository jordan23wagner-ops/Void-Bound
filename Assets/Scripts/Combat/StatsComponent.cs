using UnityEngine;
using VoidBound.Data;

namespace VoidBound.Combat
{
    public class StatsComponent : MonoBehaviour
    {
        [SerializeField] private CharacterStats baseStats = new CharacterStats(10, 10, 10, 10);

        public CharacterStats BaseStats => baseStats;
        public int MaxHP => 100 + baseStats.vig * 10;
        public float PhysicalDamage(int baseDamage) => baseDamage * (1f + baseStats.str * 0.02f);
        public float MagicDamage(int baseDamage) => baseDamage * (1f + baseStats.intel * 0.02f);
        public float AttackInterval => 1f / (1f + baseStats.dex * 0.01f);
        public float CritChance => Mathf.Min(baseStats.dex * 0.005f, 0.5f);
        public float DefenseMultiplier => 100f / (100f + baseStats.vig);

        public void SetBaseStats(CharacterStats stats)
        {
            baseStats = stats;
        }
    }
}
