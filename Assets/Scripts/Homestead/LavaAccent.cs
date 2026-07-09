using System.Collections.Generic;
using UnityEngine;

namespace VoidBound.Homestead
{
    // A small, deliberately-subtle lava VEIN etched into the Ashfields ground: a
    // branching set of thin dark cracks with a molten line glowing inside each, so
    // it reads as part of the terrain (the ground split open) rather than an object
    // sitting on top. Prominence is intentionally low (~1-2/10 vs OSRS's TzHaar) —
    // it warms the ash, it isn't a lava zone. Self-building from primitives at
    // runtime (no assets, no baked scene meshes — matches BonfireEffect); breathes
    // gently; destroys its own materials in OnDestroy.
    public class LavaAccent : MonoBehaviour
    {
        [SerializeField, Tooltip("Overall footprint radius (metres) the vein web spreads across.")]
        private float radius = 1.3f;
        [SerializeField, Tooltip("Peak emissive strength of the magma. Low = subtle.")]
        private float glow = 0.85f;
        [SerializeField, Tooltip("Faint light this casts on nearby ground/props. Keep dim.")]
        private float lightIntensity = 0.3f;
        [SerializeField] private float pulseSpeed = 0.8f;

        private static readonly Color Magma = new Color(0.95f, 0.28f, 0.05f); // deep red-orange, not yellow

        private readonly List<Material> magmaMats = new List<Material>();
        private Light glowLight;
        private float phase;
        private bool built;
        private readonly List<Material> created = new List<Material>();

        private void OnEnable() { if (!built) Build(); }

        private System.Random rng;
        private float Rand(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        private void Build()
        {
            built = true;

            // Seed from world position so each accent is unique but stable across
            // sessions — no two vein webs look alike (size, shape, orientation).
            var p = transform.position;
            int seed = Mathf.RoundToInt(p.x * 127.1f) * 73856093 ^ Mathf.RoundToInt(p.z * 311.7f) * 19349663;
            rng = new System.Random(seed == 0 ? 1 : seed);

            float r = radius * Rand(0.55f, 1.55f);           // variable overall size
            var start = new Vector3(Rand(-0.2f, 0.2f) * r, 0f, Rand(-0.2f, 0.2f) * r);
            BuildCrack(start, Rand(0f, 360f), 3 + rng.Next(0, 3), r, 0);

            // Very faint warm light so the seams just kiss the ground around them.
            var lightGO = new GameObject("Glow");
            lightGO.transform.SetParent(transform, false);
            lightGO.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            glowLight = lightGO.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.color = new Color(1f, 0.42f, 0.12f);
            glowLight.range = r * 2.6f;
            glowLight.intensity = lightIntensity * Rand(0.8f, 1.15f);
        }

        // Wandering crack: lay chained seams that turn slightly each step, with the
        // occasional branch — an organic fissure rather than a fixed rosette.
        private void BuildCrack(Vector3 pos, float dir, int segs, float r, int depth)
        {
            for (int i = 0; i < segs; i++)
            {
                float len = Rand(0.4f, 0.95f) * r;
                float width = Mathf.Max(0.035f * r, Rand(0.06f, 0.12f) * r * (1f - 0.08f * i));
                var fwd = Quaternion.Euler(0f, dir, 0f) * Vector3.right;
                AddSeam(pos + fwd * (len * 0.5f), dir, len, width);
                pos += fwd * len;
                dir += Rand(-35f, 35f);                       // gentle wander
                if (depth < 2 && rng.NextDouble() < 0.4)       // occasional branch
                    BuildCrack(pos, dir + (rng.NextDouble() < 0.5 ? 55f : -55f), 1 + rng.Next(0, 2), r * 0.7f, depth + 1);
            }
        }

        // One glowing crack segment: an ultra-flat emissive sliver laid on the
        // ground surface, so it reads as lava seeping through a seam, not a bar.
        private void AddSeam(Vector3 center, float yaw, float length, float width)
        {
            var molten = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DestroyCollider(molten);
            molten.name = "Seam";
            molten.transform.SetParent(transform, false);
            molten.transform.localPosition = center + new Vector3(0f, 0.012f, 0f); // hug the surface
            molten.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            molten.transform.localScale = new Vector3(length, 0.01f, width);
            var m = MakeMat(Magma, true);
            molten.GetComponent<Renderer>().sharedMaterial = m;
            magmaMats.Add(m);
        }

        private void Update()
        {
            // Slow, shallow breathing so it flickers like cooling magma, not a strobe.
            phase += Time.deltaTime * pulseSpeed;
            float p = 0.7f + 0.3f * Mathf.Sin(phase);
            foreach (var m in magmaMats)
                if (m != null) m.SetColor("_EmissionColor", Magma * (glow * p));
            if (glowLight != null)
                glowLight.intensity = lightIntensity * p;
        }

        private Material MakeMat(Color color, bool emissive)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            { color = color };
            if (emissive)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * 1.4f);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            created.Add(m);
            return m;
        }

        private static void DestroyCollider(GameObject go)
        {
            var c = go.GetComponent<Collider>();
            if (c != null)
            {
                if (Application.isPlaying) Destroy(c); else DestroyImmediate(c);
            }
        }

        private void OnDestroy()
        {
            foreach (var m in created)
            {
                if (m == null) continue;
                if (Application.isPlaying) Destroy(m); else DestroyImmediate(m);
            }
            created.Clear();
        }
    }
}
