#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VoidBound.Editor
{
    // Polish pass 3: dresses the Homestead + Ashfields terrains with low-poly
    // props (Tools/build_environment_props.py) so the zones feel alive, and tunes
    // the ground/light/fog per zone (Homestead = warm & lush, Ashfields = grim &
    // ashen). All placed props live under a single "EnvironmentDressing" root so
    // the pass is idempotent — re-running clears and rebuilds. Materials are
    // assigned by imported slot name, same contract as the building swap.
    public static class EnvironmentDressing
    {
        private const string PropDir = "Assets/Art/Models/Props";
        private const string MatDir = "Assets/Art/Materials/Env";
        private const string RootName = "EnvironmentDressing";

        [MenuItem("VoidBound/Polish - Dress Environments")]
        public static void Run()
        {
            var mats = CreateMaterials();
            DressHomestead(mats);
            DressAshfields(mats);
            Debug.Log("[EnvDressing] Homestead + Ashfields dressed.");
        }

        public static void RunFromBatch() => Run();

        // ─────────────────────────── materials ───────────────────────────
        private static Dictionary<string, Material> CreateMaterials()
        {
            if (!AssetDatabase.IsValidFolder(MatDir))
                AssetDatabase.CreateFolder("Assets/Art/Materials", "Env");

            var defs = new (string name, Color col, Color? emis)[]
            {
                ("WoodDark",  new Color(0.30f, 0.20f, 0.12f), null),
                ("WoodLight", new Color(0.55f, 0.40f, 0.25f), null),
                ("Stone",     new Color(0.58f, 0.57f, 0.53f), null),
                ("StoneDark", new Color(0.36f, 0.36f, 0.35f), null),
                ("Metal",     new Color(0.45f, 0.47f, 0.50f), null),
                ("Gold",      new Color(0.82f, 0.63f, 0.20f), null),
                ("Water",     new Color(0.25f, 0.60f, 0.90f), null),
                ("ClothRed",  new Color(0.70f, 0.20f, 0.18f), null),
                ("Leaf",      new Color(0.28f, 0.55f, 0.24f), null),
                ("Soil",      new Color(0.30f, 0.22f, 0.15f), null),
                ("Grass",     new Color(0.42f, 0.62f, 0.30f), null),
                ("GrassDry",  new Color(0.55f, 0.50f, 0.30f), null),
                ("DeadWood",  new Color(0.26f, 0.24f, 0.22f), null),
                ("Bone",      new Color(0.86f, 0.83f, 0.72f), null),
                ("Ash",       new Color(0.34f, 0.33f, 0.35f), null),
                ("Fire",      new Color(1.00f, 0.45f, 0.10f), new Color(1.0f, 0.35f, 0.05f) * 1.6f),
                ("GemCyan",   new Color(0.40f, 0.85f, 1.00f), new Color(0.40f, 0.85f, 1.00f) * 1.5f),
            };

            var result = new Dictionary<string, Material>();
            foreach (var (name, col, emis) in defs)
            {
                string path = $"{MatDir}/{name}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null)
                {
                    m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(m, path);
                }
                m.color = col;
                if (emis.HasValue)
                {
                    m.EnableKeyword("_EMISSION");
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    m.SetColor("_EmissionColor", emis.Value);
                }
                EditorUtility.SetDirty(m);
                result[name] = m;
            }
            AssetDatabase.SaveAssets();
            return result;
        }

        // ─────────────────────────── placement core ───────────────────────────
        private struct Placed { public string prop; public float x, z, rotY, scale; }

        private static Transform RebuildRoot(UnityEngine.SceneManagement.Scene scene)
        {
            var existing = GameObject.Find(RootName);
            if (existing != null) Object.DestroyImmediate(existing);
            var root = new GameObject(RootName);
            return root.transform;
        }

        private static void Place(Transform root, Dictionary<string, Material> mats,
            string prop, float x, float z, float rotY, float scale,
            Dictionary<string, string> remap = null)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>($"{PropDir}/{prop}.fbx");
            if (fbx == null) { Debug.LogWarning($"[EnvDressing] Missing prop {prop}"); return; }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(x, 0f, z);
            go.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            go.transform.localScale = Vector3.one * scale;
            go.isStatic = true;

            foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
            {
                r.sharedMaterials = r.sharedMaterials.Select(sm =>
                {
                    string n = sm != null ? sm.name : "Stone";
                    if (remap != null && remap.TryGetValue(n, out var to)) n = to;
                    return mats.TryGetValue(n, out var m) ? m : mats["Stone"];
                }).ToArray();
            }
        }

        // Seeded scatter of a prop across an annulus, rejecting keep-out zones.
        private static void Scatter(Transform root, Dictionary<string, Material> mats, string prop,
            int count, float minScale, float maxScale, System.Random rng,
            Vector2[] keepouts, float keepR, List<Vector2> taken, float spacing,
            float ringMin, float ringMax, Dictionary<string, string> remap = null)
        {
            int placed = 0, attempts = 0;
            while (placed < count && attempts < count * 40)
            {
                attempts++;
                float ang = (float)(rng.NextDouble() * Mathf.PI * 2);
                float rad = Mathf.Lerp(ringMin, ringMax, (float)rng.NextDouble());
                float x = Mathf.Cos(ang) * rad, z = Mathf.Sin(ang) * rad;
                var p = new Vector2(x, z);
                if (keepouts.Any(k => Vector2.Distance(k, p) < keepR)) continue;
                if (taken.Any(t => Vector2.Distance(t, p) < spacing)) continue;
                taken.Add(p);
                float rotY = (float)(rng.NextDouble() * 360);
                float scale = Mathf.Lerp(minScale, maxScale, (float)rng.NextDouble());
                Place(root, mats, prop, x, z, rotY, scale, remap);
                placed++;
            }
        }

        // ─────────────────────────── Homestead ───────────────────────────
        private static void DressHomestead(Dictionary<string, Material> mats)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Homestead.unity");
            var root = RebuildRoot(scene);

            var buildings = new[]
            {
                new Vector2(-8, 8), new Vector2(0, 10), new Vector2(8, 8), new Vector2(10, -8),
                new Vector2(-6, 4), new Vector2(0, -15), new Vector2(0, -10), new Vector2(-10, -2),
                new Vector2(-10, -8), new Vector2(-10, -14), new Vector2(10, -2), new Vector2(12, 4),
                new Vector2(0, 0), // player spawn
            };
            var taken = new List<Vector2>();
            var rng = new System.Random(1337);

            // Hero props (hand-placed).
            Place(root, mats, "Well", 5f, -4f, 20f, 1f);
            taken.Add(new Vector2(5, -4));
            foreach (var l in new[] { new Vector2(5, 0.5f), new Vector2(-4, -5), new Vector2(7.5f, 2.5f),
                                      new Vector2(-6.5f, -4.5f), new Vector2(2.5f, -8.5f) })
            { Place(root, mats, "Lamppost", l.x, l.y, 0f, 1f); taken.Add(l); }
            // Perimeter fence arcs (leave gaps for entry).
            foreach (var f in new (float x, float z, float r)[] {
                (-6,18,0),(-2,18.5f,0),(2,18.5f,0),(6,18,0),
                (18,6,90),(18.5f,2,90),(18.5f,-2,90),(18,-6,90),
                (-18,6,90),(-18.5f,2,90),(-18,-6,90) })
            { Place(root, mats, "Fence", f.x, f.z, f.r, 1f); taken.Add(new Vector2(f.x, f.z)); }
            // Barrels & crates clustered by working buildings.
            foreach (var b in new[] { new Vector2(8.6f, -9.2f), new Vector2(-9.4f, 6.6f), new Vector2(-4.4f, 5.6f) })
            { Place(root, mats, "Barrel", b.x, b.y, 0f, 1f); taken.Add(b); }
            foreach (var c in new[] { new Vector2(9.4f, -8.4f), new Vector2(-7f, 5.4f), new Vector2(11.2f, -1.2f) })
            { Place(root, mats, "Crate", c.x, c.y, 30f, 1f); taken.Add(c); }

            // Scattered nature.
            Scatter(root, mats, "Tree", 16, 0.85f, 1.35f, rng, buildings, 4.0f, taken, 4.0f, 12f, 19f);
            Scatter(root, mats, "Bush", 14, 0.8f, 1.2f, rng, buildings, 3.2f, taken, 2.4f, 5f, 18f);
            Scatter(root, mats, "Rock", 8, 0.7f, 1.3f, rng, buildings, 3.2f, taken, 3f, 5f, 18f);
            Scatter(root, mats, "Flowers", 10, 0.9f, 1.3f, rng, buildings, 3.0f, taken, 1.8f, 4f, 16f);
            Scatter(root, mats, "GrassTuft", 26, 0.8f, 1.4f, rng, buildings, 2.6f, taken, 1.4f, 4f, 19f);

            TuneLighting(warm: true);
            SetGround("Homestead", new Color(0.40f, 0.52f, 0.30f), new Color(0.33f, 0.45f, 0.25f),
                new Color(0.42f, 0.34f, 0.22f), new Color(0.30f, 0.32f, 0.26f), new Color(0.54f, 0.60f, 0.42f), 20, 10f);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // ─────────────────────────── Ashfields ───────────────────────────
        private static void DressAshfields(Dictionary<string, Material> mats)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Ashfields.unity");
            var root = RebuildRoot(scene);

            var keep = new[]
            {
                new Vector2(4, 3), new Vector2(-6, 5), new Vector2(8, -3), new Vector2(-8, -7),
                new Vector2(-2, -13), new Vector2(3, -18), new Vector2(12, 4), new Vector2(0, 0),
            };
            var taken = new List<Vector2>();
            var rng = new System.Random(661);
            var dry = new Dictionary<string, string> { { "Grass", "GrassDry" }, { "Leaf", "DeadWood" } };

            // War-camp fires + ancient ruins + palisade (hand-placed).
            foreach (var b in new[] { new Vector2(0, -8), new Vector2(-5.5f, -12f), new Vector2(5, -16f), new Vector2(6.5f, -3f) })
            { Place(root, mats, "Brazier", b.x, b.y, 0f, 1f); taken.Add(b); }
            foreach (var p in new (float x, float z, float s)[] { (-12,7,1.1f),(11,-10,1f),(2.5f,14,1.2f),(-9,-16,0.9f) })
            { Place(root, mats, "BrokenPillar", p.x, p.z, rng.Next(360), p.s); taken.Add(new Vector2(p.x, p.z)); }
            foreach (var s in new (float x, float z, float r)[] { (-1,-20,4),(3,-20,-6),(-5,-20,10),(9,-16,20),(-9,-13,-14),(12,-19,4) })
            { Place(root, mats, "Spikes", s.x, s.z, s.r, 1f); taken.Add(new Vector2(s.x, s.z)); }

            // Scattered waste.
            Scatter(root, mats, "DeadTree", 13, 0.85f, 1.4f, rng, keep, 3.4f, taken, 3.6f, 6f, 19f);
            Scatter(root, mats, "Boulder", 9, 0.7f, 1.4f, rng, keep, 3.0f, taken, 3.2f, 5f, 18f);
            Scatter(root, mats, "Bones", 7, 0.8f, 1.2f, rng, keep, 2.6f, taken, 2.6f, 4f, 17f);
            Scatter(root, mats, "GrassTuft", 18, 0.7f, 1.2f, rng, keep, 2.4f, taken, 1.6f, 4f, 19f, dry);

            TuneLighting(warm: false);
            SetGround("Ashfields", new Color(0.46f, 0.41f, 0.35f), new Color(0.37f, 0.33f, 0.30f),
                new Color(0.31f, 0.29f, 0.29f), new Color(0.25f, 0.24f, 0.25f), new Color(0.58f, 0.54f, 0.48f), 51, 8f);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // ─────────────────────────── ground / light / fog ───────────────────────────
        // Bakes a mottled ground texture (base variation + dirt patches + gravel
        // specks) so the terrain isn't a flat colour, then tiles it on the ground.
        private static void SetGround(string zone, Color baseA, Color baseB, Color dirt,
            Color gravelD, Color gravelL, int seed, float tiling)
        {
            var ground = GameObject.Find("Ground");
            if (ground == null) return;

            var tex = GenerateGroundTexture(baseA, baseB, dirt, gravelD, gravelL, seed);
            const string dir = "Assets/Art/Textures";
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/Art", "Textures");
            string path = $"{dir}/Ground_{zone}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) { EditorUtility.CopySerialized(tex, existing); tex = existing; }
            else AssetDatabase.CreateAsset(tex, path);

            var r = ground.GetComponent<Renderer>();
            if (r != null && r.sharedMaterial != null)
            {
                var m = r.sharedMaterial;
                m.color = Color.white;                 // full colour lives in the texture
                m.mainTexture = tex;
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                m.mainTextureScale = new Vector2(tiling, tiling);
                if (m.HasProperty("_BaseMap")) m.SetTextureScale("_BaseMap", new Vector2(tiling, tiling));
                EditorUtility.SetDirty(m);
            }
            // Widen the ground so its edge sits out past the fog.
            ground.transform.localScale = new Vector3(56f, ground.transform.localScale.y, 56f);
        }

        private static Texture2D GenerateGroundTexture(Color a, Color b, Color dirt,
            Color gravelD, Color gravelL, int seed)
        {
            const int S = 256;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var rng = new System.Random(seed);

            // Dirt patch centres (toroidal so the texture tiles).
            int patchN = 16;
            var pcx = new float[patchN]; var pcy = new float[patchN]; var pr = new float[patchN];
            for (int i = 0; i < patchN; i++)
            {
                pcx[i] = (float)rng.NextDouble() * S;
                pcy[i] = (float)rng.NextDouble() * S;
                pr[i] = (float)(rng.NextDouble() * 26 + 12);
            }
            float off = (float)rng.NextDouble() * 100f;

            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float n = Mathf.PerlinNoise(x * 0.025f + off, y * 0.025f + off);
                    float fine = Mathf.PerlinNoise(x * 0.18f, y * 0.18f) * 0.6f + Mathf.PerlinNoise(x * 0.5f + 40, y * 0.5f + 40) * 0.4f;
                    Color c = Color.Lerp(b, a, n);
                    c *= Mathf.Lerp(0.92f, 1.08f, fine);

                    float dmin = 99f;
                    for (int i = 0; i < patchN; i++)
                    {
                        float dx = Mathf.Abs(x - pcx[i]); dx = Mathf.Min(dx, S - dx);
                        float dy = Mathf.Abs(y - pcy[i]); dy = Mathf.Min(dy, S - dy);
                        float d = Mathf.Sqrt(dx * dx + dy * dy) / pr[i];
                        if (d < dmin) dmin = d;
                    }
                    if (dmin < 1f) c = Color.Lerp(c, dirt, (1f - dmin) * 0.75f);
                    c.a = 1f;
                    px[y * S + x] = c;
                }

            // Gravel specks (2x2 so they read at tiled scale).
            var grng = new System.Random(seed * 7 + 3);
            int specks = S * S / 90;
            for (int i = 0; i < specks; i++)
            {
                int gx = grng.Next(S), gy = grng.Next(S);
                Color g = grng.NextDouble() < 0.4 ? gravelL : gravelD;
                for (int oy = 0; oy < 2; oy++)
                    for (int ox = 0; ox < 2; ox++)
                    {
                        int ix = (gx + ox) % S, iy = (gy + oy) % S;
                        px[iy * S + ix] = Color.Lerp(px[iy * S + ix], g, 0.6f);
                    }
            }

            tex.SetPixels(px);
            tex.Apply(true);
            return tex;
        }

        private static void TuneLighting(bool warm)
        {
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (l.type != LightType.Directional) continue;
                if (warm)
                {
                    l.color = new Color(1.00f, 0.95f, 0.84f);
                    l.intensity = 1.9f;
                    l.transform.rotation = Quaternion.Euler(48f, 325f, 0f);
                }
                else
                {
                    l.color = new Color(0.86f, 0.82f, 0.80f);
                    l.intensity = 1.0f;
                    l.transform.rotation = Quaternion.Euler(42f, 340f, 0f);
                }
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            if (warm)
            {
                RenderSettings.fogColor = new Color(0.74f, 0.80f, 0.72f);
                RenderSettings.fogStartDistance = 22f;
                RenderSettings.fogEndDistance = 60f;
                RenderSettings.ambientLight = new Color(0.52f, 0.54f, 0.50f);
            }
            else
            {
                RenderSettings.fogColor = new Color(0.50f, 0.47f, 0.45f);
                RenderSettings.fogStartDistance = 14f;
                RenderSettings.fogEndDistance = 48f;
                RenderSettings.ambientLight = new Color(0.42f, 0.40f, 0.40f);
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        }
    }
}
#endif
