using UnityEngine;
using VoidBound.Combat;
using VoidBound.Core;
using VoidBound.Data;
using VoidBound.Inventory;

namespace VoidBound.Homestead
{
    // Shrine: gold offering for a timed blessing, on a cooldown.
    // FALLBACK design pending RunePortal source confirmation. The GDD-style
    // "+% damage" isn't representable in the CharacterStats pipeline, so the
    // blessing grants +STR/+INT instead (damage scales off both) — deliberate
    // deviation to stay inside the TimedBuff ceiling; numbers tunable.
    public class ShrineStation : Interactable
    {
        [SerializeField] private int offeringCost = 25;
        [SerializeField] private int blessStrBonus = 5;
        [SerializeField] private int blessIntBonus = 5;
        [SerializeField] private float blessDuration = 180f;
        [SerializeField] private float cooldown = 120f;

        private float readyAt;

        // One offering per approach — never drain gold by standing nearby
        public override bool RepeatOnProximity => false;

        public override void OnInteract(GameObject instigator)
        {
            if (Time.time < readyAt)
            {
                FloatingDamageNumber.SpawnText(instigator.transform.position,
                    $"Shrine ready in {Mathf.CeilToInt(readyAt - Time.time)}s", Color.gray);
                return;
            }

            var currency = instigator.GetComponent<PlayerCurrency>();
            var buff = instigator.GetComponent<TimedBuff>();
            if (currency == null || buff == null) return;

            if (!currency.SpendGold(offeringCost))
            {
                FloatingDamageNumber.SpawnText(instigator.transform.position,
                    $"Offering costs {offeringCost}g", Color.gray);
                return;
            }

            buff.Apply("shrine_blessing", "Blessed",
                new CharacterStats(blessStrBonus, 0, 0, blessIntBonus), blessDuration);
            readyAt = Time.time + cooldown;
            FloatingDamageNumber.SpawnText(instigator.transform.position,
                $"Blessed! (-{offeringCost}g)", new Color(1f, 0.85f, 0.4f));
        }
    }
}
