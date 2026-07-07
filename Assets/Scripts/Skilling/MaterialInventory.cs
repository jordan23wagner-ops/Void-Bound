using System;
using System.Collections.Generic;
using UnityEngine;
using VoidBound.Data;

namespace VoidBound.Skilling
{
    public class MaterialInventory : MonoBehaviour
    {
        // A material def + how many are held; snapshot used for death drops (§4A).
        public readonly struct Stack
        {
            public readonly MaterialItemSO material;
            public readonly int quantity;
            public Stack(MaterialItemSO material, int quantity)
            {
                this.material = material;
                this.quantity = quantity;
            }
        }

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

        // Removes and returns every held material stack (death drop, §4A).
        // Restore by feeding each stack back through AddMaterial.
        public List<Stack> TakeAll()
        {
            var taken = new List<Stack>();
            foreach (var kv in materials)
                if (kv.Value > 0 && registry.TryGetValue(kv.Key, out var def) && def != null)
                    taken.Add(new Stack(def, kv.Value));

            bool had = materials.Count > 0;
            materials.Clear();
            registry.Clear();
            if (had) OnMaterialsChanged?.Invoke();
            return taken;
        }

        public IReadOnlyDictionary<string, int> GetAllMaterials() => materials;
        public MaterialItemSO GetMaterialDef(string itemId) => registry.TryGetValue(itemId, out var m) ? m : null;
    }
}
