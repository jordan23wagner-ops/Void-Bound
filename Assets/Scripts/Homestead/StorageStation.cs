using UnityEngine;
using VoidBound.Core;
using VoidBound.UI;

namespace VoidBound.Homestead
{
    public class StorageStation : Interactable
    {
        public override bool RepeatOnProximity => false;

        public override void OnInteract(GameObject instigator)
        {
            var ui = Object.FindAnyObjectByType<StorageUI>();
            if (ui != null)
                ui.Open(instigator);
        }
    }
}
