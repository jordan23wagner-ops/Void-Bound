using UnityEngine;

namespace VoidBound.Data
{
    [CreateAssetMenu(fileName = "New Material Item", menuName = "VoidBound/Material Item")]
    public class MaterialItemSO : ScriptableObject
    {
        public string itemId;
        public string displayName;
        public string description;
        public Sprite icon;
        public RarityTier tier; // resource / food rank on the rarity spine

        [Header("Economy")]
        public int goldValue; // 0 = unsellable; merchant sell price is a ratio of this

        [Header("Consumable (food / potion)")]
        public bool isConsumable;
        public int healOverTime;      // food: total HP healed over the duration (0 = none)
        public float hotDuration = 8f; // food: seconds the heal-over-time lasts

        [Header("Potion effect (food leaves this None)")]
        public ConsumableEffect effect;
        public int effectMagnitude;      // heal amount, or stat bonus for a buff
        public float effectDuration = 30f; // buff duration (seconds)
    }
}
