using UnityEngine;
using VoidBound.Core;
using VoidBound.Data;

namespace VoidBound.Homestead
{
    // Watchtower — the Homestead lookout. Scouting board: reports each zone's
    // danger, recommended combat level (vs yours), unlock status, and an intel
    // blurb, so you can plan a run before committing at the Portal (GDD §6 —
    // "zone scouting"). Reads the same zone list the Portal uses.
    public class WatchtowerStation : Interactable
    {
        public override bool RepeatOnProximity => false;

        // The zones to scout — the Portal's destination list (one source of truth).
        public ZoneDefinitionSO[] Zones
        {
            get
            {
                var portal = Object.FindAnyObjectByType<VoidBound.UI.PortalUI>();
                return portal != null ? portal.Destinations : System.Array.Empty<ZoneDefinitionSO>();
            }
        }

        public override void OnInteract(GameObject instigator)
        {
            var ui = FindOrCreateUI();
            if (ui != null) ui.Open(this, instigator);
        }

        // The board lives on the persisted HUDCanvas; create it on first use so no
        // scene wiring is needed (matches how PoolStation finds its UI).
        private VoidBound.UI.WatchtowerUI FindOrCreateUI()
        {
            var ui = Object.FindAnyObjectByType<VoidBound.UI.WatchtowerUI>();
            if (ui != null) return ui;
            var hud = Object.FindAnyObjectByType<VoidBound.UI.HUDManager>();
            return hud != null ? hud.gameObject.AddComponent<VoidBound.UI.WatchtowerUI>() : null;
        }
    }
}
