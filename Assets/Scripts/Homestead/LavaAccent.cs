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
        [SerializeField, Tooltip("Overall footprint radius (metres). Keep small — this is an accent.")]
        private float radius = 0.9f;
        [SerializeField, Tooltip("Peak emissive strength of the magma. Low = subtle.")]
        private float glow = 0.7f;
        [SerializeField, Tooltip("Faint light this casts on nearby ground/props. Keep dim.")]
        private float lightIntensity = 0.28f;
        [SerializeField] private float pulseSpeed = 0.8f;

        private static readonly Color CrackDark = new Color(0.04f, 0.03f, 0.03f); // the split in the ground
        private static readonly Color Magma = new Color(0.95f, 0.30f, 0.06f);     // deep red-orange, not yellow

        private readonly List<Material> magmaMats = new List<Material>();
        private Light glowLight;
        private float phase;
        private bool built;
        private readonly List<Material> created = new List<Material>();

        private void OnEnable() { if (!built) Build(); }

        private void Build()
        {
            built = true;

            // A branching vein: a few thin crack segments radiating from a rough
            // centre at varied angles/lengths, each a dark gap with a molten line.
            AddVein(new Vector3(0f, 0f, 0f),                          12f,  radius * 1.9f, radius * 0.16f);
            AddVein(new Vector3(radius * 0.55f, 0f, radius * 0.12f),  52f,  radius * 1.1f, radius * 0.12f);
            AddVein(new Vector3(-radius * 0.5f, 0f, -radius * 0.08f), -38f, radius * 0.95f, radius * 0.11f);
            AddVein(new Vector3(radius * 0.22f, 0f, -radius * 0.4f),  96f,  radius * 0.7f, radius * 0.09f);

            // Very faint warm light so the vein just kisses the ground around it.
            var lightGO = new GameObject("Glow");
            lightGO.transform.SetParent(transform, false);
            lightGO.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            glowLight = lightGO.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.color = new Color(1f, 0.45f, 0.15f);
            glowLight.range = radius * 3.5f;
            glowLight.intensity = lightIntensity;
        }

        // One crack segment: a dark gap flush with the ground (the split), with a
        // thinner molten line glowing inside it. Both hug the surface.
        private void AddVein(Vector3 localPos, float yaw, float length, float width)
        {
            var rot = Quaternion.Euler(0f, yaw, 0f);

            var crack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DestroyCollider(crack);
            crack.name = "Crack";
            crack.transform.SetParent(transform, false);
            crack.transform.localPosition = localPos + new Vector3(0f, 0.015f, 0f);
            crack.transform.localRotation = rot;
            crack.transform.localScale = new Vector3(length, 0.02f, width);
            crack.GetComponent<Renderer>().sharedMaterial = MakeMat(CrackDark, false);

            var molten = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DestroyCollider(molten);
            molten.name = "Molten";
            molten.transform.SetParent(transform, false);
            molten.transform.localPosition = localPos + new Vector3(0f, 0.028f, 0f);
            molten.transform.localRotation = rot;
            molten.transform.localScale = new Vector3(length * 0.9f, 0.015f, width * 0.4f);
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
