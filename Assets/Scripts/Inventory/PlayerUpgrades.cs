using System.Collections.Generic;
using UnityEngine;
using VoidBound.Data;

namespace VoidBound.Inventory
{
    // Tracks per-item upgrade tier for untradables and runs the single active
    // Enchanted-Chest upgrade (GDD §5.6). Time-vs-risk: starting an upgrade
    // begins a timer; success chance rises with elapsed time (0 → 1), so
    // completing early is a gamble and waiting the full timer is guaranteed.
    // The item is never lost — only the materials the UI already consumed.
    //
    // Tier is kept in a side-table keyed by itemId (base = the SO's rarity) so
    // the shared GearItemSO asset is never mutated.
    public class PlayerUpgrades : MonoBehaviour
    {
        private readonly Dictionary<string, RarityTier> tiers = new();

        private bool active;
        private string activeId;
        private RarityTier target;
        private float startTime, duration;

        public bool IsActive => active;
        public string ActiveItemId => activeId;
        public RarityTier ActiveTarget => target;
        public float Progress => active ? Mathf.Clamp01((Time.time - startTime) / duration) : 0f;
        public float SuccessChance => Progress; // rises 0 → 1 over the timer
        public float Remaining => active ? Mathf.Max(0f, duration - (Time.time - startTime)) : 0f;

        public RarityTier GetTier(GearItemSO g) =>
            g == null ? RarityTier.Common : (tiers.TryGetValue(g.itemId, out var t) ? t : g.rarity);

        // Gear stat/damage scaling by tier: +20% per rarity step (Common ×1 → Void ×2.6).
        public static float StatMultiplier(RarityTier t) => 1f + (int)t * 0.2f;

        public bool CanUpgrade(GearItemSO g) =>
            !active && g != null && g.untradable && GetTier(g) < RarityTier.Void;

        // Begins the upgrade (materials are checked/consumed by the caller/UI).
        public bool StartUpgrade(GearItemSO g)
        {
            if (!CanUpgrade(g)) return false;
            activeId = g.itemId;
            target = (RarityTier)((int)GetTier(g) + 1);
            startTime = Time.time;
            duration = 10f + (int)target * 5f; // higher tiers take longer
            active = true;
            return true;
        }

        // Attempts to finish now: success chance = current Progress. On success
        // the item climbs a tier; either way the slot frees. Returns success.
        public bool CompleteNow()
        {
            if (!active) return false;
            bool success = Random.value <= SuccessChance;
            if (success) tiers[activeId] = target;
            active = false;
            return success;
        }
    }
}
