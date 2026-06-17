using UnityEngine;

namespace VoidBound.Data
{
    [CreateAssetMenu(fileName = "New GearItem", menuName = "VoidBound/Gear Item")]
    public class GearItemSO : ScriptableObject
    {
        public string itemId;
        public string displayName;
        public EquipmentSlot slot;
        public WeaponType weaponType;
        public RarityTier rarity;
        public CharacterStats statModifiers;
        public GameObject visualPrefab;
        public string setId;

        [Header("Combat")]
        public int baseDamage;
    }
}
