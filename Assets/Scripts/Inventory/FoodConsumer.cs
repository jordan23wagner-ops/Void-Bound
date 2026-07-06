using UnityEngine;
using VoidBound.Combat;
using VoidBound.Data;
using VoidBound.Homestead;
using VoidBound.Skilling;

namespace VoidBound.Inventory
{
    // Uses consumables (§5.1/§5.3): food applies its heal-over-time; potions
    // apply their ConsumableEffect (burst heal, or a timed stat buff via
    // TimedBuff). EatLowest keeps the quick "eat weakest food" behaviour for the
    // HUD button; the consumables panel calls Use for a specific item.
    public class FoodConsumer : MonoBehaviour
    {
        private MaterialInventory matInv;
        private HealOverTime hot;
        private Health health;
        private TimedBuff buffs;

        public event System.Action<MaterialItemSO> OnAte;

        private void Awake()
        {
            matInv = GetComponent<MaterialInventory>();
            hot = GetComponent<HealOverTime>();
            health = GetComponent<Health>();
            buffs = GetComponent<TimedBuff>();
        }

        // Uses one of a specific consumable, applying its effect. Returns false if
        // none owned or it isn't consumable.
        public bool Use(MaterialItemSO item)
        {
            if (item == null || !item.isConsumable || matInv == null) return false;
            if (matInv.GetCount(item.itemId) <= 0) return false;
            if (!matInv.ConsumeMaterial(item.itemId, 1)) return false;
            Apply(item);
            OnAte?.Invoke(item);
            return true;
        }

        private void Apply(MaterialItemSO item)
        {
            switch (item.effect)
            {
                case ConsumableEffect.Heal:
                    if (health != null) health.Heal(item.effectMagnitude);
                    break;
                case ConsumableEffect.BuffSTR: Buff(item, new CharacterStats(item.effectMagnitude, 0, 0, 0)); break;
                case ConsumableEffect.BuffDEX: Buff(item, new CharacterStats(0, item.effectMagnitude, 0, 0)); break;
                case ConsumableEffect.BuffVIG: Buff(item, new CharacterStats(0, 0, item.effectMagnitude, 0)); break;
                case ConsumableEffect.BuffINT: Buff(item, new CharacterStats(0, 0, 0, item.effectMagnitude)); break;
                // Swiftness / CurePoison / Luck: defined but inert until their
                // systems (movement, status effects, loot) are wired.
                default:
                    if (item.healOverTime > 0 && hot != null) hot.AddHot(item.healOverTime, item.hotDuration);
                    break;
            }
        }

        private void Buff(MaterialItemSO item, CharacterStats bonus)
        {
            if (buffs != null)
                buffs.Apply("potion_" + item.effect, item.displayName, bonus, item.effectDuration);
        }

        // ── weakest-food quick eat (HUD button) ────────────────────────

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
            return food != null && Use(food);
        }
    }
}
