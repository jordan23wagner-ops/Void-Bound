using UnityEngine;
using VoidBound.Core;
using VoidBound.UI;

namespace VoidBound.Homestead
{
    // Fast Travel Portal — opens the destination list UI STUB (Phase 6 spec).
    // Actual scene travel arrives with the zone phases.
    public class PortalStation : Interactable
    {
        public override bool RepeatOnProximity => false;

        public override void OnInteract(GameObject instigator)
        {
            var ui = Object.FindAnyObjectByType<PortalUI>();
            if (ui != null)
                ui.Open();
        }
    }
}
