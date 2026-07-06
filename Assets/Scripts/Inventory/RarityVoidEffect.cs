using UnityEngine;

namespace VoidBound.Inventory
{
    // Void — the showpiece. Corrupted void energy bound into the gear: a deep
    // violet glow that BREATHES, its hue drifting across the void band, punctuated
    // by dramatic power-SURGES where the whole set flares, and constant CRACKLE —
    // sharp, unstable sparks of hot magenta as raw void energy arcs across it.
    public class RarityVoidEffect : RarityAnimBase
    {
        public override RarityAnim AnimType => RarityAnim.Void;

        [SerializeField] private float breatheSpeed = 1.2f;
        [SerializeField] private float driftSpeed = 0.35f;
        [SerializeField] private float surgeSpeed = 0.5f;   // big periodic flares
        [SerializeField] private float crackleSpeed = 7f;   // fast instability

        protected override void Animate(float t)
        {
            // Breathing base glow.
            float breathe = 0.55f + 0.45f * Mathf.Sin(t * breatheSpeed + phase);
            // Hue drifting through the void band (violet ↔ magenta ↔ indigo).
            float hue = 0.80f + 0.09f * Mathf.Sin(t * driftSpeed + phase);
            Color voidGlow = Color.HSVToRGB(hue, 0.92f, 1f) * (0.35f + breathe * 0.8f);

            // Periodic power surge — the whole set flares every few seconds.
            float surge = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * surgeSpeed + phase)), 8f);
            voidGlow *= 1f + surge * 1.9f;

            // Crackling instability — sharp noise spikes to hot magenta.
            float n = Mathf.PerlinNoise(t * crackleSpeed + phase * 13f, phase);
            float crackle = Mathf.Pow(Mathf.Max(0f, n - 0.62f) * 2.7f, 3f);
            Color spark = new Color(1f, 0.45f, 1f) * (crackle * 2.4f);

            SetEmission(voidGlow + spark);
        }
    }
}
