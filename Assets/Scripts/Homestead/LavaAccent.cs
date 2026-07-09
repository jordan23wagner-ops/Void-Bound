using System.Collections.Generic;
using UnityEngine;

namespace VoidBound.Homestead
{
    // A small, deliberately-subtle molten accent for the Ashfields ground: a dark
    // basalt crust with a faint glowing crack of magma that slowly "breathes."
    // Prominence is intentionally low (~1-2 out of 10 vs OSRS's TzHaar) — it's a
    // flavour detail that warms the ash, not a lava zone. Self-building from
    // primitives at runtime (no assets, no baked scene meshes — matches
    // BonfireEffect/PoolMysticEffect); destroys its own materials in OnDestroy.
    public class LavaAccent : MonoBehaviour
    {
        [SerializeField, Tooltip("Overall footprint radius (metres). Keep small — this is an accent.")]
        private float radius = 0.9f;
        [SerializeField, Tooltip("Peak emissive strength of the magma. Low = subtle.")]
        private float glow = 0.7f;
        [SerializeField, Tooltip("Faint light this casts on nearby ground/props. Keep dim.")]
        private float lightIntensity = 0.28f;
        [SerializeField] private float pulseSpeed = 0.8f;

        private static readonly Color Crust = new Color(0.09f, 0.07f, 0.07f);
        private static readonly Color Magma = new Color(0.95f, 0.30f, 0.06f); // deep red-orange, not yellow

        private readonly List<Material> magmaMats = new List<Material>();
        private Light glowLight;
        private float phase;
        private bool built;
        private readonly List<Material> created = new List<Material>();

        private void OnEnable() { if (!built) Build(); }

        private void Build()
        {
            built = true;

            // Dark basalt crust disc, flush with the ground.
            MakeDisc("Crust", 0.03f, radius, Crust, false);

            // A couple of thin magma cracks — offset and roughly parallel so they read
            // as an irregular fissure, not a symmetric marker cross.
            AddCrack(new Vector3(0f, 0.035f, 0f), 20f, radius * 1.35f, radius * 0.15f);
            AddCrack(new Vector3(radius * 0.28f, 0.035f, radius * 0.12f), 44f, radius * 0.8f, radius * 0.11f);

            // Faint warm point light so it kisses the surrounding ground.
            var lightGO = new GameObject("Glow");
            lightGO.transform.SetParent(transform, false);
            lightGO.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            glowLight = lightGO.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.color = new Color(1f, 0.45f, 0.15f);
            glowLight.range = radius * 5f;
            glowLight.intensity = lightIntensity;
        }

        private void AddCrack(Vector3 localPos, float yaw, float length, float width)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DestroyCollider(go);
            go.name = "MagmaCrack";
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = new Vector3(length, 0.02f, width);
            var m = MakeMat(Magma, true);
            go.GetComponent<Renderer>().sharedMaterial = m;
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

        private GameObject MakeDisc(string name, float thickness, float r, Color color, bool emissive)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            DestroyCollider(go);
            go.name = name;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, thickness * 0.5f, 0f);
            go.transform.localScale = new Vector3(r * 2f, thickness, r * 2f);
            go.GetComponent<Renderer>().sharedMaterial = MakeMat(color, emissive);
            return go;
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
