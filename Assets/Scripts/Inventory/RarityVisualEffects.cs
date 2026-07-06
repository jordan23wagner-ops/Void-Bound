using UnityEngine;
using VoidBound.Data;

namespace VoidBound.Inventory
{
    // Which runtime animation a rarity's emissive gear surface uses.
    public enum RarityAnim { None, Shimmer, ObsidianSheen, Void }

    public static class RarityVisualEffects
    {
        // Canonical rarity colours.
        private static readonly Color CommonColor    = new(0.72f, 0.72f, 0.72f); // grey
        private static readonly Color UncommonColor  = new(0.93f, 0.93f, 0.93f); // white
        private static readonly Color MagicColor     = new(0.24f, 0.55f, 1.00f); // blue
        private static readonly Color RareColor      = new(1.00f, 0.84f, 0.14f); // yellow
        private static readonly Color EpicColor      = new(0.66f, 0.30f, 0.90f); // purple
        private static readonly Color LegendaryColor = new(1.00f, 0.55f, 0.10f); // orange
        private static readonly Color ObsidianColor  = new(0.80f, 0.83f, 0.90f); // silver sheen (UI swatch)
        private static readonly Color RadiantColor   = new(0.95f, 0.97f, 1.00f); // diamond white (UI swatch)
        private static readonly Color VoidColor      = new(0.50f, 0.12f, 0.92f); // deep cold blue-violet (UI swatch)

        public static Color GetRarityColor(RarityTier rarity)
        {
            return rarity switch
            {
                RarityTier.Common => CommonColor,
                RarityTier.Uncommon => UncommonColor,
                RarityTier.Magic => MagicColor,
                RarityTier.Rare => RareColor,
                RarityTier.Epic => EpicColor,
                RarityTier.Legendary => LegendaryColor,
                RarityTier.Obsidian => ObsidianColor,
                RarityTier.Radiant => RadiantColor,
                RarityTier.Void => VoidColor,
                _ => Color.white
            };
        }

        // ── Full material treatment for a gear surface (the "Main" material) ──
        // The rarity's look is more than a tint: reflectivity (smoothness/metallic)
        // and emission define the high tiers. Obsidian = near-black reflective glass
        // with a white sheen; Radiant = diamond-bright with a runtime rainbow
        // shimmer; Void = corrupted purple-black glow.
        public struct RarityStyle
        {
            public Color albedo;
            public float smoothness;
            public float metallic;
            public Color emission;   // Color.black = no emission
            public RarityAnim anim;  // runtime animated treatment (top tiers)
        }

        public static RarityStyle GetStyle(RarityTier r)
        {
            switch (r)
            {
                case RarityTier.Common:    return Style(CommonColor, 0.08f, 0.00f, Color.black);
                case RarityTier.Uncommon:  return Style(UncommonColor, 0.18f, 0.00f, Color.black);
                case RarityTier.Magic:     return Style(MagicColor, 0.35f, 0.10f, MagicColor * 0.45f);
                case RarityTier.Rare:      return Style(RareColor, 0.42f, 0.20f, RareColor * 0.45f);
                case RarityTier.Epic:      return Style(EpicColor, 0.46f, 0.20f, EpicColor * 0.55f);
                case RarityTier.Legendary: return Style(LegendaryColor, 0.55f, 0.30f, LegendaryColor * 0.70f);
                case RarityTier.Obsidian:  return Style(new Color(0.04f, 0.04f, 0.055f), 0.93f, 0.80f, new Color(0.30f, 0.33f, 0.42f) * 0.20f, RarityAnim.ObsidianSheen);
                case RarityTier.Radiant:   return Style(new Color(0.97f, 0.98f, 1.00f), 0.96f, 0.35f, Color.white * 0.35f, RarityAnim.Shimmer);
                case RarityTier.Void:      return Style(new Color(0.045f, 0.015f, 0.075f), 0.60f, 0.55f, new Color(0.32f, 0.08f, 0.62f) * 0.6f, RarityAnim.Void);
                default:                   return Style(Color.white, 0.2f, 0f, Color.black);
            }
        }

        private static RarityStyle Style(Color a, float s, float m, Color e, RarityAnim anim = RarityAnim.None) =>
            new RarityStyle { albedo = a, smoothness = s, metallic = m, emission = e, anim = anim };

        // Apply the rarity treatment to one "Main" material (albedo + reflectivity +
        // emission). The runtime animation (if any) is wired separately via ApplyAnim.
        public static void StyleMainMaterial(Material m, RarityTier rarity)
        {
            if (m == null) return;
            var s = GetStyle(rarity);
            m.color = s.albedo;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", s.albedo);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", s.smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", s.metallic);
            if (s.emission != Color.black)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", s.emission);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else m.DisableKeyword("_EMISSION");
        }

        // Attach (or swap) the animated rarity component on a gear piece so its
        // emissive "Main" material comes alive at the top tiers.
        public static void ApplyAnim(GameObject go, RarityTier rarity)
        {
            var want = GetStyle(rarity).anim;
            var existing = go.GetComponents<RarityAnimBase>();
            if (want != RarityAnim.None && existing.Length == 1 && existing[0].AnimType == want) return;
            foreach (var a in existing) Object.Destroy(a);
            switch (want)
            {
                case RarityAnim.Shimmer:       go.AddComponent<RarityShimmer>(); break;
                case RarityAnim.ObsidianSheen: go.AddComponent<RarityObsidianSheen>(); break;
                case RarityAnim.Void:          go.AddComponent<RarityVoidEffect>(); break;
            }
        }

        public static void ApplyToRenderers(GameObject target, RarityTier rarity)
        {
            if (target == null) return;

            var renderers = target.GetComponentsInChildren<Renderer>();
            Color rarityColor = GetRarityColor(rarity);
            bool hasEmission = rarity >= RarityTier.Magic;      // Magic+ glow
            bool hasParticles = rarity >= RarityTier.Legendary; // Legendary+ aura

            foreach (var r in renderers)
            {
                foreach (var mat in r.materials)
                {
                    if (hasEmission)
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", rarityColor * 0.5f);
                        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    }
                    else
                    {
                        mat.DisableKeyword("_EMISSION");
                    }
                }
            }

            if (hasParticles)
                AddParticleAccent(target, rarityColor);
        }

        private static void AddParticleAccent(GameObject target, Color color)
        {
            var existing = target.GetComponentInChildren<ParticleSystem>();
            if (existing != null) return;

            var psObj = new GameObject("RarityParticles");
            psObj.transform.SetParent(target.transform, false);
            psObj.transform.localPosition = Vector3.up * 0.5f;

            var ps = psObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(color, color * 0.6f);
            main.startSize = 0.08f;
            main.startLifetime = 1.5f;
            main.startSpeed = 0.3f;
            main.maxParticles = 15;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;

            var emission = ps.emission;
            emission.rateOverTime = 5f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            var renderer = psObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            renderer.material.color = color;
        }
    }
}
