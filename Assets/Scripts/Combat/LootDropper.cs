using UnityEngine;
using VoidBound.Data;
using VoidBound.Inventory;
using VoidBound.Skilling;

namespace VoidBound.Combat
{
    public class LootDropper : MonoBehaviour
    {
        [SerializeField] private LootTableSO lootTable;
        [SerializeField] private EnemyTier tier;

        public void SetLootTable(LootTableSO table, EnemyTier enemyTier)
        {
            lootTable = table;
            tier = enemyTier;
        }

        public void DropLoot(Vector3 position)
        {
            if (lootTable == null) return;

            int gold = lootTable.RollGold();
            int shards = lootTable.RollVoidShards();

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var currency = player.GetComponent<PlayerCurrency>();
                if (currency != null)
                {
                    if (gold > 0)
                    {
                        currency.AddGold(gold);
                        FloatingDamageNumber.SpawnText(position + Vector3.right * 0.5f,
                            $"+{gold} Gold", new Color(1f, 0.85f, 0.1f));
                    }
                    if (shards > 0)
                    {
                        currency.AddVoidShards(shards);
                        FloatingDamageNumber.SpawnText(position + Vector3.left * 0.5f,
                            $"+{shards} Void Shards", new Color(0.6f, 0.1f, 1f));
                    }
                }
            }

            var gearDrop = lootTable.RollGear();
            if (gearDrop != null)
            {
                WorldPickup.Spawn(position, gearDrop);
            }

            // Material drops (e.g. tool-head parts) go straight to the pouch, like
            // currency, with a floating tag rather than a walk-over pickup.
            var mats = lootTable.RollMaterials();
            if (mats.Count > 0 && player != null)
            {
                var matInv = player.GetComponent<MaterialInventory>();
                if (matInv != null)
                {
                    float up = 0.9f;
                    foreach (var d in mats)
                    {
                        matInv.AddMaterial(d.material, d.quantity);
                        FloatingDamageNumber.SpawnText(position + Vector3.up * up,
                            $"+{d.quantity} {d.material.displayName}", new Color(0.75f, 0.9f, 1f));
                        up += 0.35f;
                    }
                }
            }
        }
    }
}
