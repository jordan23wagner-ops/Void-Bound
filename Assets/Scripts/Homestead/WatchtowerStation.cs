using UnityEngine;
using VoidBound.Combat;
using VoidBound.Core;

namespace VoidBound.Homestead
{
    // Watchtower — flavor stub per Phase 6 spec (RunePortal source for its real
    // function was not found; do not invent a system). Real behavior lands in
    // whichever future phase defines it (likely zone scouting/wave defense).
    public class WatchtowerStation : Interactable
    {
        [SerializeField] private string flavorText = "The wastes are quiet.";
        [SerializeField] private float repeatDelay = 4f;

        private float nextAt;

        public override bool RepeatOnProximity => false;

        public override void OnInteract(GameObject instigator)
        {
            if (Time.time < nextAt) return;
            nextAt = Time.time + repeatDelay;
            FloatingDamageNumber.SpawnText(instigator.transform.position,
                flavorText, new Color(0.8f, 0.8f, 0.7f));
        }
    }
}
