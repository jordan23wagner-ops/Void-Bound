using UnityEngine;
using VoidBound.Data;

namespace VoidBound.Inventory
{
    public static class RarityVisualEffects
    {
        private static readonly Color CommonColor = new(0.7f, 0.7f, 0.7f);
        private static readonly Color UncommonColor = new(0.3f, 0.8f, 0.3f);
        private static readonly Color RareColor = new(0.2f, 0.6f, 1f);
        private static readonly Color EpicColor = new(0.7f, 0.3f, 0.9f);
        private static readonly Color LegendaryColor = new(1f, 0.75f, 0.1f);
        private static readonly Color MythicColor = new(1f, 0.4f, 0.2f);
        private static readonly Color AscendedColor = new(0.4f, 1f, 0.9f);
        private static readonly Color EternalColor = new(0.9f, 0.9f, 1f);
        private static readonly Color VoidforgedColor = new(0.6f, 0.1f, 1f);

        public static Color GetRarityColor(RarityTier rarity)
        {
            return rarity switch
            {
                RarityTier.Common => CommonColor,
                RarityTier.Uncommon => UncommonColor,
                RarityTier.Rare => RareColor,
                RarityTier.Epic => EpicColor,
                RarityTier.Legendary => LegendaryColor,
                RarityTier.Mythic => MythicColor,
                RarityTier.Ascended => AscendedColor,
                RarityTier.Eternal => EternalColor,
                RarityTier.Voidforged => VoidforgedColor,
                _ => Color.white
            };
        }

        public static void ApplyToRenderers(GameObject target, RarityTier rarity)
        {
            if (target == null) return;

            var renderers = target.GetComponentsInChildren<Renderer>();
            Color rarityColor = GetRarityColor(rarity);
            bool hasEmission = rarity >= RarityTier.Rare;
            bool hasParticles = rarity >= RarityTier.Legendary;

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
