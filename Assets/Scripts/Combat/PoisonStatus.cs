using System;
using UnityEngine;

namespace VoidBound.Combat
{
    // Poison damage-over-time status (GDD §4). Some enemies apply it on hit; the
    // Antidote potion (§5.3) clears it. Drains `remaining` damage at a fixed rate,
    // dealing whole HP through Health.TakeDamage — so poison can bring you down
    // like any other source. Cleared automatically on death so you respawn clean.
    [RequireComponent(typeof(Health))]
    public class PoisonStatus : MonoBehaviour
    {
        private static readonly Color PoisonColor = new(0.55f, 0.85f, 0.25f);

        private Health health;
        private float remaining;   // damage still to be dealt
        private float rate;        // damage per second
        private float startAmount; // `remaining` at last (re)application — for the UI fraction
        private float bucket;      // fractional-damage accumulator (whole HP only hits Health)

        public bool IsPoisoned => remaining > 0.001f;
        public float Remaining => remaining;
        public float SecondsLeft => rate > 0f ? remaining / rate : 0f;
        public float Fraction => startAmount > 0f ? Mathf.Clamp01(remaining / startAmount) : 0f;
        public event Action OnPoisonChanged; // apply / cure / expire

        private void Awake() => health = GetComponent<Health>();
        private void OnEnable() { if (health != null) health.OnDeath += Cure; }
        private void OnDisable() { if (health != null) health.OnDeath -= Cure; }

        // Apply a poison dealing `totalDamage` over `duration` seconds. Re-applying
        // tops up the remaining damage and takes the stronger tick rate, so repeated
        // hits keep the poison going rather than diluting it.
        public void Apply(int totalDamage, float duration)
        {
            if (totalDamage <= 0 || duration <= 0f) return;
            remaining = Mathf.Max(remaining, totalDamage);
            rate = Mathf.Max(rate, totalDamage / duration);
            startAmount = remaining;
            OnPoisonChanged?.Invoke();
        }

        public void Cure()
        {
            if (!IsPoisoned) return;
            remaining = 0f;
            rate = 0f;
            bucket = 0f;
            OnPoisonChanged?.Invoke();
        }

        private void Update()
        {
            if (remaining <= 0f || health == null || health.IsDead) return;

            float dmg = Mathf.Min(rate * Time.deltaTime, remaining);
            remaining -= dmg;
            bucket += dmg;

            int whole = Mathf.FloorToInt(bucket);
            if (whole > 0)
            {
                bucket -= whole;
                FloatingDamageNumber.SpawnText(transform.position + Vector3.up * 1.6f, $"-{whole}", PoisonColor);
                health.TakeDamage(whole); // routes through the normal damage path (can kill)
            }

            if (remaining <= 0.001f)
            {
                remaining = 0f;
                rate = 0f;
                OnPoisonChanged?.Invoke();
            }
        }
    }
}
