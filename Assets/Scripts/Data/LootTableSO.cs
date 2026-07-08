using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidBound.Data
{
    [Serializable]
    public struct RarityWeight
    {
        public RarityTier rarity;
        public float weight;
    }

    // A material (e.g. a monster-part tool head) that can drop from this table,
    // rolled independently at its own chance.
    [Serializable]
    public struct MaterialDrop
    {
        public MaterialItemSO material;
        [Range(0f, 1f)] public float chance;
        public int minQuantity;
        public int maxQuantity;
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

        [Header("Material Drops (tool heads, etc.)")]
        public MaterialDrop[] materialDrops;

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

        // Independently rolls each material drop; returns the ones that hit with
        // their rolled quantity. zoneModifier scales the drop chance too.
        public List<(MaterialItemSO material, int quantity)> RollMaterials()
        {
            var result = new List<(MaterialItemSO, int)>();
            if (materialDrops == null) return result;
            foreach (var d in materialDrops)
            {
                if (d.material == null) continue;
                if (UnityEngine.Random.value > d.chance * zoneModifier) continue;
                int lo = Mathf.Max(1, d.minQuantity);
                int hi = Mathf.Max(lo, d.maxQuantity);
                result.Add((d.material, UnityEngine.Random.Range(lo, hi + 1)));
            }
            return result;
        }

        public int RollGold() => UnityEngine.Random.Range(goldMin, goldMax + 1);
        public int RollVoidShards() => UnityEngine.Random.Range(voidShardMin, voidShardMax + 1);
    }
}
