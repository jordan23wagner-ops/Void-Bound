using System;
using UnityEngine;
using VoidBound.Combat;
using VoidBound.Core;
using VoidBound.Data;
using VoidBound.Inventory;

namespace VoidBound.Homestead
{
    // Pool of Refreshment: full heal + timed all-stat buff, then a cooldown.
    // 4 tiers held as data (GDD §6); stronger tiers are bought with gold at the
    // Pool. Interacting opens PoolUI to refresh or purchase the next tier.
    // Tier is session state — persisting it across zone travel awaits a save
    // system (Homestead reloads reset it, like other station state).
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

        // ── State the PoolUI reads ──────────────────────────────────────
        public int TierCount => tiers != null ? tiers.Length : 0;
        public int CurrentTier => Mathf.Clamp(currentTier, 0, Mathf.Max(0, TierCount - 1));
        public PoolTier Current => tiers[CurrentTier];
        public bool IsMaxTier => CurrentTier >= TierCount - 1;
        public int NextUpgradeCost => IsMaxTier ? 0 : tiers[CurrentTier].upgradeCost;
        public bool IsReady => Time.time >= readyAt;
        public float CooldownRemaining => Mathf.Max(0f, readyAt - Time.time);

        public override void OnInteract(GameObject instigator)
        {
            var ui = FindOrCreateUI();
            if (ui != null) ui.Open(this, instigator);
        }

        // Apply this tier's heal + timed all-stat buff and start the cooldown.
        // Returns false if still on cooldown (nothing happens).
        public bool TryRefresh(GameObject instigator)
        {
            if (tiers == null || tiers.Length == 0 || !IsReady) return false;
            var tier = Current;

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
            return true;
        }

        // Buy the next tier with gold. Returns false if already max tier or the
        // player can't afford it.
        public bool TryUpgrade(GameObject instigator)
        {
            if (IsMaxTier) return false;
            int cost = tiers[CurrentTier].upgradeCost;

            var currency = instigator.GetComponent<PlayerCurrency>();
            if (currency == null || !currency.SpendGold(cost)) return false;

            currentTier = CurrentTier + 1;
            FloatingDamageNumber.SpawnText(instigator.transform.position,
                $"Pool upgraded to Tier {CurrentTier + 1}!", new Color(0.4f, 0.8f, 1f));
            return true;
        }

        // The panel lives on the persisted HUDCanvas; create it on first use so no
        // scene wiring is needed (matches how the station finds its UI at runtime).
        private VoidBound.UI.PoolUI FindOrCreateUI()
        {
            var ui = UnityEngine.Object.FindAnyObjectByType<VoidBound.UI.PoolUI>();
            if (ui != null) return ui;
            var hud = UnityEngine.Object.FindAnyObjectByType<VoidBound.UI.HUDManager>();
            return hud != null ? hud.gameObject.AddComponent<VoidBound.UI.PoolUI>() : null;
        }
    }
}
