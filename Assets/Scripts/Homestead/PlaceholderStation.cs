using UnityEngine;
using VoidBound.Core;

namespace VoidBound.Homestead
{
    // A station that's placed in the world but whose full UI isn't built yet
    // (Enchanted Chest untradable-upgrades, Reclaimer death-recovery). The hover
    // tooltip (BuildingTooltipUI) shows its name + description + prompt; a
    // proximity/tap interact just logs a "coming soon" note until the real
    // system lands. Fires once per approach.
    public class PlaceholderStation : Interactable
    {
        [SerializeField] private string comingSoonNote = "Coming soon.";

        public override bool RepeatOnProximity => false;

        public override void OnInteract(GameObject instigator)
        {
            Debug.Log($"[{gameObject.name}] {comingSoonNote}");
        }
    }
}
