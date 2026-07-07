using System;
using System.Collections.Generic;
using UnityEngine;
using VoidBound.Combat;
using VoidBound.Data;

namespace VoidBound.Inventory
{
    public class PlayerInventory : MonoBehaviour
    {
        private readonly Dictionary<EquipmentSlot, GearItemSO> equipped = new();
        private readonly List<GearItemSO> backpack = new();

        private StatsComponent stats;
        private PlayerUpgrades upgrades;
        // Exact stat bonus applied per slot, so unequip removes what equip added
        // even if the item's tier changed (upgraded) while worn.
        private readonly Dictionary<EquipmentSlot, CharacterStats> appliedMods = new();

        public IReadOnlyDictionary<EquipmentSlot, GearItemSO> Equipped => equipped;
        public IReadOnlyList<GearItemSO> Backpack => backpack;

        public event Action OnInventoryChanged;

        private void Awake()
        {
            stats = GetComponent<StatsComponent>();
            upgrades = GetComponent<PlayerUpgrades>();
        }

        public void AddItem(GearItemSO item)
        {
            backpack.Add(item);
            OnInventoryChanged?.Invoke();
        }

        // Removes one instance from the backpack without equipping it
        // (merchant sell, bank deposit). Returns false if not present.
        public bool RemoveItem(GearItemSO item)
        {
            if (!backpack.Remove(item)) return false;
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool EquipItem(GearItemSO item)
        {
            if (item == null) return false;

            EquipmentSlot slot = item.slot;

            if (equipped.TryGetValue(slot, out var current))
                UnequipItem(slot);

            equipped[slot] = item;
            backpack.Remove(item);
            var mods = ScaledMods(item);
            if (stats != null) stats.AddGearBonus(mods);
            appliedMods[slot] = mods;

            OnInventoryChanged?.Invoke();
            Debug.Log($"Equipped: {item.displayName} ({item.rarity}) → {slot}");
            return true;
        }

        public bool UnequipItem(EquipmentSlot slot)
        {
            if (!equipped.TryGetValue(slot, out var item)) return false;

            equipped.Remove(slot);
            backpack.Add(item);
            if (stats != null && appliedMods.TryGetValue(slot, out var applied)) stats.RemoveGearBonus(applied);
            appliedMods.Remove(slot);

            OnInventoryChanged?.Invoke();
            Debug.Log($"Unequipped: {item.displayName} from {slot}");
            return true;
        }

        public GearItemSO GetEquipped(EquipmentSlot slot)
        {
            equipped.TryGetValue(slot, out var item);
            return item;
        }

        // Replace the whole inventory (save load). Clears current gear (removing
        // worn stat bonuses) then repopulates. Any upgrade tiers must already be
        // set on PlayerUpgrades so equip scales stats correctly.
        public void LoadState(IEnumerable<GearItemSO> newBackpack, IEnumerable<GearItemSO> newEquipped)
        {
            foreach (var slot in new List<EquipmentSlot>(equipped.Keys))
                UnequipItem(slot); // removes bonuses + appliedMods, moves to backpack
            backpack.Clear();

            if (newBackpack != null)
                foreach (var it in newBackpack) if (it != null) backpack.Add(it);
            if (newEquipped != null)
                foreach (var it in newEquipped) if (it != null) EquipItem(it);

            OnInventoryChanged?.Invoke();
        }

        // Re-scales an equipped item's stat bonus to its current effective tier
        // (called after an Enchanted-Chest upgrade). Removes the exact bonus that
        // was applied, then adds the new one.
        public void RefreshEquipped(GearItemSO item)
        {
            if (item == null) return;
            var slot = item.slot;
            if (!equipped.TryGetValue(slot, out var eq) || eq != item) return;
            if (stats != null && appliedMods.TryGetValue(slot, out var old)) stats.RemoveGearBonus(old);
            var mods = ScaledMods(item);
            if (stats != null) stats.AddGearBonus(mods);
            appliedMods[slot] = mods;
            OnInventoryChanged?.Invoke();
        }

        private RarityTier EffectiveTier(GearItemSO item) =>
            upgrades != null ? upgrades.GetTier(item) : item.rarity;

        // The item's stat modifiers scaled by its effective tier (upgrades make
        // gear stronger as it climbs the ladder).
        private CharacterStats ScaledMods(GearItemSO item)
        {
            float m = PlayerUpgrades.StatMultiplier(EffectiveTier(item));
            var s = item.statModifiers;
            return new CharacterStats(
                Mathf.RoundToInt(s.str * m), Mathf.RoundToInt(s.dex * m),
                Mathf.RoundToInt(s.vig * m), Mathf.RoundToInt(s.intel * m));
        }
    }
}
