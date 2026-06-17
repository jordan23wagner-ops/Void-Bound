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
    }
}
