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
        public event Action<int, int> OnHealthChanged;
        public event Action OnDeath;

        private void Start()
        {
            var stats = GetComponent<StatsComponent>();
            if (stats != null)
                maxHP = stats.MaxHP;
            else if (maxHP <= 0)
                maxHP = 100;

            currentHP = maxHP;
        }

        public void TakeDamage(int damage)
        {
            if (IsDead) return;

            currentHP = Mathf.Max(0, currentHP - damage);
            OnHealthChanged?.Invoke(currentHP, maxHP);

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
    }
}
