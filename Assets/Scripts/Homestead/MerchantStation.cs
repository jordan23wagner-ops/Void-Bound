using UnityEngine;
using VoidBound.Core;
using VoidBound.Data;
using VoidBound.UI;

namespace VoidBound.Homestead
{
    public class MerchantStation : Interactable
    {
        [SerializeField] private ShopInventorySO shopInventory;

        public override bool RepeatOnProximity => false;

        public override void OnInteract(GameObject instigator)
        {
            var ui = Object.FindAnyObjectByType<MerchantUI>();
            if (ui != null)
                ui.Open(shopInventory, instigator, this);
        }
    }
}
