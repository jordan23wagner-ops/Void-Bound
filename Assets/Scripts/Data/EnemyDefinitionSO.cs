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
    }
}
