using System;
using UnityEngine;

namespace VoidBound.Data
{
    // Data-driven merchant stock (Critical Rule 3). One asset per merchant;
    // adding stock later is a data change, not code.
    [CreateAssetMenu(fileName = "New Shop Inventory", menuName = "VoidBound/Shop Inventory")]
    public class ShopInventorySO : ScriptableObject
    {
        [Serializable]
        public class ShopEntry
        {
            public GearItemSO gear;         // set ONE of gear/material
            public MaterialItemSO material;
            public int price;               // explicit buy price in gold
            public int materialQuantity = 1; // only used for material entries

            public string DisplayName => gear != null ? gear.displayName
                : material != null ? material.displayName : "(empty)";
            public bool IsValid => (gear != null) ^ (material != null);
        }

        public ShopEntry[] stock;
    }
}
