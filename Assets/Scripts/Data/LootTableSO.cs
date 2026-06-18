using System;
using UnityEngine;

namespace VoidBound.Data
{
    [Serializable]
    public struct RarityWeight
    {
        public RarityTier rarity;
        public float weight;
    }

    [CreateAssetMenu(fileName = "New LootTable", menuName = "VoidBound/Loot Table")]
    public class LootTableSO : ScriptableObject
    {
        public string tableId;
        public string displayName;

        [Header("Gear Drops")]
        public GearItemSO[] gearPool;
        public RarityWeight[] rarityWeights;
        [Range(0f, 1f)] public float gearDropChance = 0.5f;

        [Header("Currency")]
        public int goldMin;
        public int goldMax = 10;
        public int voidShardMin;
        public int voidShardMax;

        [Header("Zone Scaling")]
        public float zoneModifier = 1f;

        public RarityTier RollRarity()
        {
            if (rarityWeights == null || rarityWeights.Length == 0)
                return RarityTier.Common;

            float totalWeight = 0f;
            foreach (var rw in rarityWeights)
                totalWeight += rw.weight * zoneModifier;

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;
            foreach (var rw in rarityWeights)
            {
                cumulative += rw.weight * zoneModifier;
                if (roll <= cumulative)
                    return rw.rarity;
            }
            return rarityWeights[rarityWeights.Length - 1].rarity;
        }

        public GearItemSO RollGear()
        {
            if (gearPool == null || gearPool.Length == 0) return null;
            if (UnityEngine.Random.value > gearDropChance) return null;
            return gearPool[UnityEngine.Random.Range(0, gearPool.Length)];
        }

        public int RollGold() => UnityEngine.Random.Range(goldMin, goldMax + 1);
        public int RollVoidShards() => UnityEngine.Random.Range(voidShardMin, voidShardMax + 1);
    }
}
