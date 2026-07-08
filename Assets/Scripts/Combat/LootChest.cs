using UnityEngine;
using VoidBound.Core;
using VoidBound.Data;
using VoidBound.Inventory;
using VoidBound.Skilling;

namespace VoidBound.Combat
{
    // A one-time lootable world chest (§8 reward loop): open it to roll a loot
    // table — gold/shards + material drops (tool-head parts, etc.) go straight to
    // the player, gear spills as a walk-over WorldPickup. Opens once per approach
    // and never refills, darkening its mesh so it reads as emptied. Placed in
    // zones by LootChestsSetup.
    public class LootChest : Interactable
    {
        [SerializeField] private LootTableSO lootTable;

        private bool opened;

        // Open once on approach — no re-loot when you wander back in range.
        public override bool RepeatOnProximity => false;

        public void SetLootTable(LootTableSO table) => lootTable = table;

        public override void OnInteract(GameObject instigator)
        {
            if (opened || lootTable == null || instigator == null) return;
            opened = true;

            Vector3 top = transform.position + Vector3.up * 1.1f;
            float y = 0f;

            var currency = instigator.GetComponent<PlayerCurrency>();
            int gold = lootTable.RollGold();
            int shards = lootTable.RollVoidShards();
            if (currency != null)
            {
                if (gold > 0) currency.AddGold(gold);
                if (shards > 0) currency.AddVoidShards(shards);
            }

            var matInv = instigator.GetComponent<MaterialInventory>();
            if (matInv != null)
            {
                foreach (var d in lootTable.RollMaterials())
                {
                    matInv.AddMaterial(d.material, d.quantity);
                    FloatingDamageNumber.SpawnText(top + Vector3.up * y,
                        $"+{d.quantity} {d.material.displayName}",
                        RarityVisualEffects.GetRarityColor(d.material.tier));
                    y += 0.35f;
                }
            }

            var gear = lootTable.RollGear();
            if (gear != null) WorldPickup.Spawn(transform.position, gear);

            if (gold > 0)
                FloatingDamageNumber.SpawnText(top + Vector3.up * y,
                    $"+{gold} Gold", new Color(1f, 0.85f, 0.1f));

            MarkEmptied();
        }

        // Darken the mesh so an opened chest visibly reads as emptied.
        private void MarkEmptied()
        {
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                var mats = r.materials; // runtime instances — safe to recolour
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null) mats[i].color = mats[i].color * 0.45f;
                r.materials = mats;
            }
        }
    }
}
