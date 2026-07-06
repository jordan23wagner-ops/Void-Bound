using UnityEngine;
using VoidBound.Core;
using VoidBound.Data;

namespace VoidBound.Homestead
{
    // The Enchanted Chest: upgrades untradables up the rarity ladder via the
    // time-vs-risk timer (GDD §5.6). Holds the per-tier upgrade materials
    // (bars, indexed by target tier) the UI consumes. Opens EnchantedChestUI.
    public class EnchantedChestStation : Interactable
    {
        [SerializeField] private MaterialItemSO[] upgradeMaterials; // indexed by target tier
        [SerializeField] private int costPerUpgrade = 2;

        public MaterialItemSO[] UpgradeMaterials => upgradeMaterials;
        public int CostPerUpgrade => costPerUpgrade;

        public override bool RepeatOnProximity => false;

        public override void OnInteract(GameObject instigator)
        {
            var ui = Object.FindAnyObjectByType<VoidBound.UI.EnchantedChestUI>();
            if (ui != null) ui.Open(this, instigator);
        }
    }
}
