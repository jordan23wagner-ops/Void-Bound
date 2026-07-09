using UnityEngine;

namespace VoidBound.Combat
{
    // Global "hit-stop": a micro-freeze of time when the player lands a blow, so
    // hits feel weighty. Triggered from PlayerCombat (melee) and Projectile
    // (ranged/magic impact). Uses unscaled time to recover, so it always releases
    // even though the game clock is slowed.
    public class HitStop : MonoBehaviour
    {
        private static HitStop instance;
        private float timer;

        public static void Punch(float seconds = 0.05f, float scale = 0.15f)
        {
            if (instance == null)
            {
                var go = new GameObject("HitStop");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<HitStop>();
            }
            instance.timer = seconds;
            Time.timeScale = scale;
        }

        private void Update()
        {
            if (timer <= 0f) return;
            timer -= Time.unscaledDeltaTime;
            if (timer <= 0f) Time.timeScale = 1f;
        }
    }
}
