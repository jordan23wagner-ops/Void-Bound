using UnityEngine;

namespace VoidBound.Inventory
{
    // Obsidian: polished black volcanic glass. A faint, slow rainbow sheen drifts
    // across the near-black surface (labradorite / oil-on-water), punctuated by
    // sharp cold-white glints as if light catches an edge — the muted, darker
    // cousin of the Radiant shimmer.
    public class RarityObsidianSheen : RarityAnimBase
    {
        public override RarityAnim AnimType => RarityAnim.ObsidianSheen;

        [SerializeField] private float sheenSpeed = 0.22f;   // slow hue drift
        [SerializeField] private float sheenLevel = 0.16f;   // faint base sheen
        [SerializeField] private float glintSpeed = 1.6f;    // glint frequency
        [SerializeField] private float glintPower = 0.9f;    // glint brightness

        protected override void Animate(float t)
        {
            // Faint, low-saturation rainbow sheen on the dark glass.
            float hue = Mathf.Repeat(t * sheenSpeed + phase, 1f);
            Color sheen = Color.HSVToRGB(hue, 0.45f, 1f) * sheenLevel;

            // Sharp, occasional cold-white glint (light catching the surface).
            float g = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * glintSpeed + phase * 3f)), 12f);
            Color glint = new Color(0.85f, 0.9f, 1f) * (g * glintPower);

            SetEmission(sheen + glint);
        }
    }
}
