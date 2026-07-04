using System;
using UnityEngine;
using VoidBound.Combat;
using VoidBound.Core;
using VoidBound.Data;

namespace VoidBound.Homestead
{
    // Pool of Refreshment: full heal + timed all-stat buff, then a cooldown.
    // 4 tiers held as data (GDD); tier 1 is functional this phase. Purchasing
    // tier upgrades is a TODO for a later phase (flagged in CONTEXT.md).
    // All numbers are FALLBACK values pending RunePortal source confirmation — tunable.
    public class PoolStation : Interactable
    {
        [Serializable]
        public struct PoolTier
        {
            public int healPercent;      // % of max HP restored
            public int buffAllStats;     // flat bonus to all 4 stats
            public float buffDuration;   // seconds
            public float cooldown;       // seconds
            public int upgradeCost;      // gold to buy the NEXT tier
        }

        [SerializeField] private PoolTier[] tiers =
        {
            new() { healPercent = 100, buffAllStats = 2, buffDuration = 120f, cooldown = 60f,  upgradeCost = 200 },
            new() { healPercent = 100, buffAllStats = 3, buffDuration = 150f, cooldown = 50f,  upgradeCost = 500 },
            new() { healPercent = 100, buffAllStats = 4, buffDuration = 180f, cooldown = 40f,  upgradeCost = 1200 },
            new() { healPercent = 100, buffAllStats = 6, buffDuration = 240f, cooldown = 30f,  upgradeCost = 0 },
        };
        [SerializeField] private int currentTier = 0;

        private float readyAt;

        // Deliberate use per approach rather than AFK auto-rebuff
        public override bool RepeatOnProximity => false;

        public override void OnInteract(GameObject instigator)
        {
            if (tiers == null || tiers.Length == 0) return;
            var tier = tiers[Mathf.Clamp(currentTier, 0, tiers.Length - 1)];

            if (Time.time < readyAt)
            {
                FloatingDamageNumber.SpawnText(instigator.transform.position,
                    $"Pool ready in {Mathf.CeilToInt(readyAt - Time.time)}s", Color.gray);
                return;
            }

            var buff = instigator.GetComponent<TimedBuff>();
            if (buff != null)
            {
                int b = tier.buffAllStats;
                buff.Apply("pool_refresh", "Refreshed",
                    new CharacterStats(b, b, b, b), tier.buffDuration);
            }

            var health = instigator.GetComponent<Health>();
            if (health != null)
                health.Heal(Mathf.CeilToInt(health.MaxHP * (tier.healPercent / 100f)));

            readyAt = Time.time + tier.cooldown;
            FloatingDamageNumber.SpawnText(instigator.transform.position,
                "Refreshed!", new Color(0.4f, 0.8f, 1f));
        }
    }
}
