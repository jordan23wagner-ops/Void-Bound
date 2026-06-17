using UnityEngine;

namespace VoidBound.Data
{
    [CreateAssetMenu(fileName = "New LootTable", menuName = "VoidBound/Loot Table")]
    public class LootTableSO : ScriptableObject
    {
        public string tableId;
        public string displayName;
    }
}
