using System;
using UnityEngine;

namespace VoidBound.Combat
{
    public class Health : MonoBehaviour
    {
        [SerializeField] private int currentHP;
        [SerializeField] private int maxHP;

        public int CurrentHP => currentHP;
        public int MaxHP => maxHP;
        public bool IsDead => currentHP <= 0;
        // Set true during a dodge-roll's i-frames — TakeDamage is ignored while on.
        public bool Invulnerable { get; set; }
        public event Action<int, int> OnHealthChanged;
        public event Action OnDamaged; // fired when damage is actually applied
        public event Action OnDeath;

        private void Start()
        {
            var stats = GetComponent<StatsComponent>();
            if (stats != null)
                maxHP = stats.MaxHP;
            else if (maxHP <= 0)
                maxHP = 100;

            currentHP = maxHP;

            // Every combatant gets hit feedback (damage numbers, flash, punch).
            if (GetComponent<HitFeedback>() == null) gameObject.AddComponent<HitFeedback>();
        }

        public void TakeDamage(int damage)
        {
            if (IsDead) return;
            if (Invulnerable) return;
            if (CompareTag("Player") && VoidBound.UI.GodModeFlag.IsActive) return;

            currentHP = Mathf.Max(0, currentHP - damage);
            OnHealthChanged?.Invoke(currentHP, maxHP);
            if (damage > 0) OnDamaged?.Invoke();

            if (IsDead)
            {
                Debug.Log($"{gameObject.name} died. (took {damage} final hit)");
                OnDeath?.Invoke();
            }
        }

        public void Heal(int amount)
        {
            if (IsDead) return;
            currentHP = Mathf.Min(maxHP, currentHP + amount);
            OnHealthChanged?.Invoke(currentHP, maxHP);
        }

        // Full restore after death (player respawn). Re-derives maxHP so a
        // dead entity comes back to life at full health.
        public void Revive()
        {
            var stats = GetComponent<StatsComponent>();
            if (stats != null) maxHP = stats.MaxHP;
            if (maxHP <= 0) maxHP = 100;
            currentHP = maxHP;
            OnHealthChanged?.Invoke(currentHP, maxHP);
        }

        // Re-derive maxHP from StatsComponent after a VIG change (timed buff,
        // gear swap). maxHP is otherwise only read once at Start.
        public void RefreshMaxHP()
        {
            var stats = GetComponent<StatsComponent>();
            if (stats == null) return;
            maxHP = stats.MaxHP;
            currentHP = Mathf.Min(currentHP, maxHP);
            OnHealthChanged?.Invoke(currentHP, maxHP);
        }
    }
}
