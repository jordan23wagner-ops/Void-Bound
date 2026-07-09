using System.Collections.Generic;
using UnityEngine;

namespace VoidBound.Homestead
{
    // Blood soaked into the Bleakwood ground: an irregular dark-red pool (a few
    // overlapping flat blobs) ringed by splatter droplets, laid flush so it blends
    // into the terrain rather than sitting on top — the wet counterpart to Ashfields'
    // LavaAccent (matte, no glow). Seeded from world position so each stain is unique
    // but stable. Self-building from primitives at runtime; frees its materials.
    public class BloodPatch : MonoBehaviour
    {
        [SerializeField, Tooltip("Footprint radius (m). Actual size varies per stain.")]
        private float radius = 1.1f;

        private static readonly Color Blood     = new Color(0.24f, 0.035f, 0.04f);
        private static readonly Color BloodDeep = new Color(0.14f, 0.02f, 0.03f);

        private readonly List<Material> created = new List<Material>();
        private bool built;
        private System.Random rng;
        private float layerY; // each blob sits a hair higher than the last (no z-fighting)
        private float Rand(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        private void OnEnable() { if (!built) Build(); }

        private void Build()
        {
            built = true;
            var p = transform.position;
            int seed = Mathf.RoundToInt(p.x * 92.3f) * 73856093 ^ Mathf.RoundToInt(p.z * 47.1f) * 19349663;
            rng = new System.Random(seed == 0 ? 1 : seed);

            float r = radius * Rand(0.5f, 1.6f);
            layerY = 0.02f;

            // Main pool — a few overlapping blobs so the edge is lobed, not a disc.
            int blobs = 2 + rng.Next(0, 3);
            for (int i = 0; i < blobs; i++)
            {
                var off = new Vector3(Rand(-0.35f, 0.35f) * r, 0f, Rand(-0.35f, 0.35f) * r);
                AddBlob(off, Rand(0.55f, 1f) * r, i == 0 ? BloodDeep : Blood);
            }

            // Splatter droplets flung around the pool.
            int dots = 4 + rng.Next(0, 6);
            for (int i = 0; i < dots; i++)
            {
                float a = Rand(0f, 360f) * Mathf.Deg2Rad;
                float d = Rand(0.6f, 1.6f) * r;
                AddBlob(new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d), Rand(0.07f, 0.24f) * r, Blood);
            }
        }

        private void AddBlob(Vector3 localPos, float rad, Color c)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var col = go.GetComponent<Collider>();
            if (col != null) { if (Application.isPlaying) Destroy(col); else DestroyImmediate(col); }
            go.name = "Blood";
            go.transform.SetParent(transform, false);
            // Each blob a hair higher than the last so overlapping discs never share a
            // plane (that co-planarity is what flickered as the camera moved).
            go.transform.localPosition = localPos + new Vector3(0f, layerY, 0f);
            layerY += 0.004f;
            go.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            go.transform.localScale = new Vector3(rad * 2f, 0.01f, rad * 2f);

            var m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { color = c };
            m.SetFloat("_Smoothness", 0.2f); // faint wet sheen
            go.GetComponent<Renderer>().sharedMaterial = m;
            created.Add(m);
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
