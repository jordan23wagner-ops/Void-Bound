using UnityEngine;
using VoidBound.Combat;
using VoidBound.Data;
using VoidBound.Skilling;

namespace VoidBound.Inventory
{
    // Eats cooked food from the MaterialInventory and applies its heal-over-time
    // (§5.1). EatLowest consumes the weakest available food first, so the good
    // stuff is saved for when it's needed — the prep-loop staple.
    public class FoodConsumer : MonoBehaviour
    {
        private MaterialInventory matInv;
        private HealOverTime hot;

        public event System.Action<MaterialItemSO> OnAte;

        private void Awake()
        {
            matInv = GetComponent<MaterialInventory>();
            hot = GetComponent<HealOverTime>();
        }

        // Returns the weakest owned consumable food (lowest healOverTime), or null.
        public MaterialItemSO LowestFood()
        {
            if (matInv == null) return null;
            MaterialItemSO best = null;
            foreach (var kv in matInv.GetAllMaterials())
            {
                var def = matInv.GetMaterialDef(kv.Key);
                if (def == null || !def.isConsumable || def.healOverTime <= 0) continue;
                if (best == null || def.healOverTime < best.healOverTime) best = def;
            }
            return best;
        }

        public bool EatLowest()
        {
            var food = LowestFood();
            if (food == null) return false;
            if (!matInv.ConsumeMaterial(food.itemId, 1)) return false;
            if (hot != null) hot.AddHot(food.healOverTime, food.hotDuration);
            OnAte?.Invoke(food);
            return true;
        }
    }
}
