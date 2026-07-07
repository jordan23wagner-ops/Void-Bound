using UnityEngine;
using VoidBound.Core;
using VoidBound.Data;

namespace VoidBound.Homestead
{
    // The Reclaimer (GDD §4A): buys back untradable gear lost when a grave was
    // abandoned (you died again before recovering it) for a gold fee. The safety
    // net that stops death from bricking progression, and a gold sink (§8).
    // Opens ReclaimUI.
    public class ReclaimerStation : Interactable
    {
        [SerializeField] private int baseFee = 25;
        [SerializeField, Range(0f, 2f)] private float valueFeeRatio = 0.5f;

        // Fee scales with item value (§4A). The flat base keeps even
        // goldValue-0 crafted untradables a meaningful gold sink.
        public int FeeFor(GearItemSO item) =>
            baseFee + Mathf.FloorToInt(Mathf.Max(0, item != null ? item.goldValue : 0) * valueFeeRatio);

        public override bool RepeatOnProximity => false;

        public override void OnInteract(GameObject instigator)
        {
            var ui = Object.FindAnyObjectByType<VoidBound.UI.ReclaimUI>();
            if (ui != null) ui.Open(this, instigator);
        }
    }
}
