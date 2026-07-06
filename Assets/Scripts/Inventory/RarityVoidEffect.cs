using UnityEngine;

namespace VoidBound.Inventory
{
    // Void — the showpiece. Corrupted void energy bound into a near-black,
    // wet-obsidian surface: a deep indigo-violet glow that BREATHES (kept low so
    // the black reads through), punctuated by dramatic power-SURGES where the
    // whole set flares, and constant CRACKLE — sharp, unstable arcs of cold
    // electric-violet as raw void energy lances across it. Cold + dark on purpose,
    // to sit apart from Epic's bright lavender purple.
    public class RarityVoidEffect : RarityAnimBase
    {
        public override RarityAnim AnimType => RarityAnim.Void;

        [SerializeField] private float breatheSpeed = 1.2f;
        [SerializeField] private float driftSpeed = 0.35f;
        [SerializeField] private float surgeSpeed = 0.5f;   // big periodic flares
        [SerializeField] private float crackleSpeed = 7f;   // fast instability

        protected override void Animate(float t)
        {
            // Breathing base glow — low floor so the black base shows between pulses.
            float breathe = 0.5f + 0.5f * Mathf.Sin(t * breatheSpeed + phase);
            // Hue drifts within the cold void band (indigo ↔ violet), never magenta.
            float hue = 0.76f + 0.05f * Mathf.Sin(t * driftSpeed + phase);
            Color voidGlow = Color.HSVToRGB(hue, 0.90f, 1f) * (0.18f + breathe * 0.55f);

            // Periodic power surge — the whole set flares every few seconds.
            float surge = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * surgeSpeed + phase)), 8f);
            voidGlow *= 1f + surge * 1.5f;

            // Crackling instability — sharp arcs of cold electric-violet.
            float n = Mathf.PerlinNoise(t * crackleSpeed + phase * 13f, phase);
            float crackle = Mathf.Pow(Mathf.Max(0f, n - 0.62f) * 2.7f, 3f);
            Color spark = new Color(0.55f, 0.25f, 1f) * (crackle * 2.0f);

            SetEmission(voidGlow + spark);
        }
    }
}
