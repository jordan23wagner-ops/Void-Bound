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

        public IReadOnlyDictionary<EquipmentSlot, GearItemSO> Equipped => equipped;
        public IReadOnlyList<GearItemSO> Backpack => backpack;

        public event Action OnInventoryChanged;

        private void Awake()
        {
            stats = GetComponent<StatsComponent>();
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
            ApplyModifiers(item.statModifiers, 1);

            OnInventoryChanged?.Invoke();
            Debug.Log($"Equipped: {item.displayName} ({item.rarity}) → {slot}");
            return true;
        }

        public bool UnequipItem(EquipmentSlot slot)
        {
            if (!equipped.TryGetValue(slot, out var item)) return false;

            equipped.Remove(slot);
            backpack.Add(item);
            ApplyModifiers(item.statModifiers, -1);

            OnInventoryChanged?.Invoke();
            Debug.Log($"Unequipped: {item.displayName} from {slot}");
            return true;
        }

        public GearItemSO GetEquipped(EquipmentSlot slot)
        {
            equipped.TryGetValue(slot, out var item);
            return item;
        }

        private void ApplyModifiers(CharacterStats mods, int sign)
        {
            if (stats == null) return;

            if (sign > 0)
                stats.AddGearBonus(mods);
            else
                stats.RemoveGearBonus(mods);
        }
    }
}
