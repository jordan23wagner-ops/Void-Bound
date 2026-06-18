using System;
using System.Collections.Generic;
using UnityEngine;
using VoidBound.Data;

namespace VoidBound.Skilling
{
    public class MaterialInventory : MonoBehaviour
    {
        private readonly Dictionary<string, int> materials = new();
        private readonly Dictionary<string, MaterialItemSO> registry = new();

        public event Action OnMaterialsChanged;

        public void AddMaterial(MaterialItemSO mat, int quantity)
        {
            if (mat == null || quantity <= 0) return;

            if (!materials.ContainsKey(mat.itemId))
            {
                materials[mat.itemId] = 0;
                registry[mat.itemId] = mat;
            }
            materials[mat.itemId] += quantity;
            OnMaterialsChanged?.Invoke();
        }

        public bool HasMaterial(string itemId, int quantity)
        {
            return materials.TryGetValue(itemId, out int count) && count >= quantity;
        }

        public bool ConsumeMaterial(string itemId, int quantity)
        {
            if (!HasMaterial(itemId, quantity)) return false;
            materials[itemId] -= quantity;
            if (materials[itemId] <= 0)
            {
                materials.Remove(itemId);
                registry.Remove(itemId);
            }
            OnMaterialsChanged?.Invoke();
            return true;
        }

        public int GetCount(string itemId) => materials.TryGetValue(itemId, out int c) ? c : 0;

        public IReadOnlyDictionary<string, int> GetAllMaterials() => materials;
        public MaterialItemSO GetMaterialDef(string itemId) => registry.TryGetValue(itemId, out var m) ? m : null;
    }
}
