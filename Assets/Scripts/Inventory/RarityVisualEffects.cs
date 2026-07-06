using UnityEngine;
using VoidBound.Data;

namespace VoidBound.Inventory
{
    public static class RarityVisualEffects
    {
        // Canonical rarity colours.
        private static readonly Color CommonColor    = new(0.72f, 0.72f, 0.72f); // grey
        private static readonly Color UncommonColor  = new(0.93f, 0.93f, 0.93f); // white
        private static readonly Color MagicColor     = new(0.24f, 0.55f, 1.00f); // blue
        private static readonly Color RareColor      = new(1.00f, 0.84f, 0.14f); // yellow
        private static readonly Color EpicColor      = new(0.66f, 0.30f, 0.90f); // purple
        private static readonly Color LegendaryColor = new(1.00f, 0.55f, 0.10f); // orange
        private static readonly Color ObsidianColor  = new(0.78f, 0.80f, 0.86f); // blackish-white (cool silver)
        private static readonly Color RadiantColor   = new(0.98f, 0.82f, 0.82f); // reddish-white (rose)
        private static readonly Color VoidColor      = new(0.40f, 0.10f, 0.50f); // purple/black

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
