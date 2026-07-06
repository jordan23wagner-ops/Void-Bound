using UnityEngine;

namespace VoidBound.Inventory
{
    // Radiant: a diamond-bright surface catching shifting prismatic light —
    // cycles the emission through the full hue wheel.
    public class RarityShimmer : RarityAnimBase
    {
        public override RarityAnim AnimType => RarityAnim.Shimmer;

        [SerializeField] private float speed = 0.45f;
        [SerializeField] private float intensity = 0.6f;
        [SerializeField] private float saturation = 0.5f;

        protected override void Animate(float t)
        {
            float hue = Mathf.Repeat(t * speed + phase, 1f);
            SetEmission(Color.HSVToRGB(hue, saturation, 1f) * intensity);
        }
    }
}
