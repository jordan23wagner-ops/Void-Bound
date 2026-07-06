using System.Collections.Generic;
using UnityEngine;

namespace VoidBound.Combat
{
    // Applies heal-over-time to the player's Health (cooked food, §5.1). Each
    // AddHot enqueues a pool that drains at total/duration HP per second;
    // multiple stack. Fractional heals accumulate so small/slow HoTs still land.
    [RequireComponent(typeof(Health))]
    public class HealOverTime : MonoBehaviour
    {
        private class Hot { public float remaining; public float rate; }

        private readonly List<Hot> hots = new();
        private Health health;
        private float bucket;

        public bool Active => hots.Count > 0;

        private void Awake() => health = GetComponent<Health>();

        public void AddHot(int totalHeal, float duration)
        {
            if (totalHeal <= 0 || duration <= 0f) return;
            hots.Add(new Hot { remaining = totalHeal, rate = totalHeal / duration });
        }

        private void Update()
        {
            if (hots.Count == 0 || health == null) return;

            float healedThisFrame = 0f;
            for (int i = hots.Count - 1; i >= 0; i--)
            {
                float h = Mathf.Min(hots[i].rate * Time.deltaTime, hots[i].remaining);
                hots[i].remaining -= h;
                healedThisFrame += h;
                if (hots[i].remaining <= 0.001f) hots.RemoveAt(i);
            }

            bucket += healedThisFrame;
            int whole = Mathf.FloorToInt(bucket);
            if (whole > 0)
            {
                health.Heal(whole);
                bucket -= whole;
            }
        }
    }
}
