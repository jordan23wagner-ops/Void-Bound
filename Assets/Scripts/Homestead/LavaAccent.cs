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

        private void Build()
        {
            built = true;

            // A branching web of thin molten cracks laid flat on the (dark ash)
            // ground — no base plate, so the ground itself reads as the cracked
            // rock and only the lava glows through the seams. Segments chain off
            // each other so it looks like a spreading vein network, not a starburst.
            float r = radius;
            AddVein(new Vector3(-0.9f * r, 0f, -0.3f * r), 22f, 1.9f * r, 0.10f * r); // main seam
            AddVein(new Vector3(0.2f * r, 0f, 0.05f * r),  70f, 1.1f * r, 0.08f * r); // branch up
            AddVein(new Vector3(0.55f * r, 0f, 0.5f * r),  30f, 0.8f * r, 0.06f * r); // offshoot
            AddVein(new Vector3(-0.35f * r, 0f, 0.15f * r), -48f, 0.9f * r, 0.07f * r); // branch down-left
            AddVein(new Vector3(-0.7f * r, 0f, -0.55f * r), 100f, 0.7f * r, 0.06f * r); // hairline
            AddVein(new Vector3(0.75f * r, 0f, -0.35f * r), 120f, 0.6f * r, 0.05f * r); // hairline

            // Very faint warm light so the seams just kiss the ground around them.
            var lightGO = new GameObject("Glow");
            lightGO.transform.SetParent(transform, false);
            lightGO.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            glowLight = lightGO.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.color = new Color(1f, 0.42f, 0.12f);
            glowLight.range = radius * 3f;
            glowLight.intensity = lightIntensity;
        }

        // One glowing crack segment: an ultra-flat emissive sliver laid on the
        // ground surface, so it reads as lava seeping through a seam, not a bar.
        private void AddVein(Vector3 localPos, float yaw, float length, float width)
        {
            var molten = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DestroyCollider(molten);
            molten.name = "Seam";
            molten.transform.SetParent(transform, false);
            molten.transform.localPosition = localPos + new Vector3(0f, 0.012f, 0f); // hug the surface
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
