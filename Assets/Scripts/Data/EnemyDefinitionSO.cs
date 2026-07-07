using UnityEngine;

namespace VoidBound.Data
{
    [CreateAssetMenu(fileName = "New EnemyDefinition", menuName = "VoidBound/Enemy Definition")]
    public class EnemyDefinitionSO : ScriptableObject
    {
        public string enemyId;
        public string displayName;
        public EnemyTier tier;
        public GameObject visualPrefab;
        public CharacterStats baseStats = new CharacterStats(5, 5, 5, 5);
        public int baseDamage = 5;
        public float moveSpeed = 3f;
        public float aggroRange = 8f;
        public float attackRange = 2f;
        public LootTableSO lootTable;

        [Header("Poison (§4) — DoT applied to the player on hit; off by default")]
        public bool appliesPoison;
        [Range(0f, 1f)] public float poisonChance = 1f;
        public int poisonDamage = 8;      // total HP dealt over the duration
        public float poisonDuration = 6f; // seconds

        [Header("Visible Gear (rendered on the body by EquipmentVisuals)")]
        public GearItemSO weapon;
        public GearItemSO[] armor;
    }
}
