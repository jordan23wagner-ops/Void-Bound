using System;
using System.Collections.Generic;
using UnityEngine;
using VoidBound.Data;

namespace VoidBound.Inventory
{
    // Homestead bank (Storage Chest). Separate gear list mirroring the
    // backpack's add/remove semantics; transfers respect the backpack cap.
    public class PlayerStorage : MonoBehaviour
    {
        public const int StorageCap = 48;
        public const int BackpackCap = 24; // matches InventoryPanel5c grid capacity

        private readonly List<GearItemSO> stored = new();
        private PlayerInventory inventory;

        public IReadOnlyList<GearItemSO> Stored => stored;
        public event Action OnStorageChanged;

        private void Awake()
        {
            inventory = GetComponent<PlayerInventory>();
        }

        public bool Deposit(GearItemSO item)
        {
            if (item == null || inventory == null) return false;
            if (stored.Count >= StorageCap) return false;
            if (!inventory.RemoveItem(item)) return false;

            stored.Add(item);
            OnStorageChanged?.Invoke();
            return true;
        }

        public bool Withdraw(GearItemSO item)
        {
            if (item == null || inventory == null) return false;
            if (!stored.Contains(item)) return false;
            if (inventory.Backpack.Count >= BackpackCap) return false;

            stored.Remove(item);
            inventory.AddItem(item);
            OnStorageChanged?.Invoke();
            return true;
        }
    }
}
