using System;
using System.Collections.Generic;
using UnityEngine;
using VoidBound.Combat;
using VoidBound.Data;

namespace VoidBound.Homestead
{
    // Minimal timed stat buff on the player. Applies a CharacterStats bonus
    // through the existing StatsComponent gear-bonus API and reverts it on
    // expiry — deliberately NOT a general status-effect framework (Phase 6
    // spec caps it here). Re-applying the same buffId refreshes it.
    public class TimedBuff : MonoBehaviour
    {
        public class ActiveBuff
        {
            public string id;
            public string displayName;
            public CharacterStats bonus;
            public float expiresAt;
            public float totalDuration;

            public float Remaining => Mathf.Max(0f, expiresAt - Time.time);
            public float Fraction => totalDuration > 0f ? Mathf.Clamp01(Remaining / totalDuration) : 0f;
        }

        private readonly List<ActiveBuff> buffs = new();
        private StatsComponent stats;
        private Health health;

        public IReadOnlyList<ActiveBuff> Buffs => buffs;
        public event Action OnBuffsChanged;

        private void Awake()
        {
            stats = GetComponent<StatsComponent>();
            health = GetComponent<Health>();
        }

        public bool HasBuff(string id) => buffs.Exists(b => b.id == id);

        public void Apply(string id, string displayName, CharacterStats bonus, float duration)
        {
            if (stats == null || string.IsNullOrEmpty(id) || duration <= 0f) return;

            var existing = buffs.Find(b => b.id == id);
            if (existing != null)
            {
                stats.RemoveGearBonus(existing.bonus);
                buffs.Remove(existing);
            }

            stats.AddGearBonus(bonus);
            buffs.Add(new ActiveBuff
            {
                id = id,
                displayName = displayName,
                bonus = bonus,
                expiresAt = Time.time + duration,
                totalDuration = duration
            });
            if (health != null) health.RefreshMaxHP();
            OnBuffsChanged?.Invoke();
        }

        private void Update()
        {
            bool changed = false;
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                if (Time.time >= buffs[i].expiresAt)
                {
                    stats.RemoveGearBonus(buffs[i].bonus);
                    buffs.RemoveAt(i);
                    changed = true;
                }
            }
            if (changed)
            {
                if (health != null) health.RefreshMaxHP();
                OnBuffsChanged?.Invoke();
            }
        }
    }
}
