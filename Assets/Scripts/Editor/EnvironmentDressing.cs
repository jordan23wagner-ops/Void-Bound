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
                ("Thatch",    new Color(0.72f, 0.60f, 0.30f), null),
                ("Path",      new Color(0.42f, 0.35f, 0.26f), null),
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

        private static GameObject Place(Transform root, Dictionary<string, Material> mats,
            string prop, float x, float z, float rotY, float scale,
            Dictionary<string, string> remap = null)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>($"{PropDir}/{prop}.fbx");
            if (fbx == null) { Debug.LogWarning($"[EnvDressing] Missing prop {prop}"); return null; }

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
            return go;
        }

        // A worn dirt path strip from the bonfire out toward a building (a thin
        // stretched quad laid on the ground, trimmed at both ends).
        private static void Path(Transform root, Material mat, Vector2 target)
        {
            float dist = target.magnitude;
            float start = 2.6f, end = dist - 2.0f;
            if (end <= start + 0.5f) return;
            Vector2 dir = target / dist;
            float len = end - start;
            Vector2 mid = dir * (start + len * 0.5f);

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            go.name = "Path";
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(mid.x, 0.04f, mid.y);
            go.transform.localRotation = Quaternion.Euler(0f, Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg, 0f);
            go.transform.localScale = new Vector3(1.6f, 0.06f, len);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.isStatic = true;
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

            var ring = HomesteadLayout.WorldPositions();          // 12 interactive buildings
            var buildings = new List<Vector2>(ring) { new Vector2(0f, -4.5f) }; // + player spawn
            var taken = new List<Vector2>();
            var rng = new System.Random(1337);

            // Central bonfire (bigger focal landmark, solid + animated) + radial paths.
            var bonfire = Place(root, mats, "Bonfire", 0f, 0f, 0f, 1.3f);
            taken.Add(Vector2.zero);
            if (bonfire != null)
            {
                var col = bonfire.AddComponent<CapsuleCollider>();
                col.radius = 1.15f; col.height = 2.4f; col.center = new Vector3(0f, 1.0f, 0f);
                bonfire.AddComponent<VoidBound.Homestead.BonfireEffect>();
            }
            foreach (var b in ring) Path(root, mats["Path"], b);

            // Residential nook (NE): homes spread into a small neighbourhood,
            // doors turned toward the fire (varied).
            var homes = new (string prop, float x, float z, float jit)[] {
                ("House", 2.5f, 6.0f, 10f), ("Cottage", 6.0f, 6.0f, -12f),
                ("Cottage", 6.5f, 2.5f, 8f), ("Cottage", 2.5f, 2.5f, -8f),
            };
            foreach (var h in homes)
            {
                var p = new Vector2(h.x, h.z);
                Place(root, mats, h.prop, h.x, h.z, HomesteadLayout.FaceCentreYaw(p) + h.jit, 1f);
                buildings.Add(p); taken.Add(p);
            }

            // Lamp-lit square corners + a village well.
            foreach (var ang in new[] { 40f, 130f, 220f, 310f })
            { var p = HomesteadLayout.PosOf(ang, 5.5f); Place(root, mats, "Lamppost", p.x, p.y, 0f, 1f); taken.Add(p); }
            var wp = new Vector2(-4.5f, 4.6f);
            Place(root, mats, "Well", wp.x, wp.y, HomesteadLayout.FaceCentreYaw(wp), 1f); taken.Add(wp);

            // Barrels & crates by the working buildings (Merchant/Storage/Forge).
            foreach (var idx in new[] { 0, 4, 7 })
            {
                Vector2 bp = ring[idx];
                Vector2 inward = (-bp).normalized;
                Vector2 side = new Vector2(-inward.y, inward.x);
                var bpos = bp + inward * 2.2f + side * 0.9f;
                var cpos = bp + inward * 2.2f - side * 0.9f;
                Place(root, mats, "Barrel", bpos.x, bpos.y, 0f, 1f); taken.Add(bpos);
                Place(root, mats, "Crate", cpos.x, cpos.y, 30f, 1f); taken.Add(cpos);
            }

            // Fences flanking the south entrance (toward the player spawn).
            foreach (var f in new (float x, float z, float r)[] { (-4, -14, 80), (-2, -15, 80), (2, -15, 100), (4, -14, 100) })
            { Place(root, mats, "Fence", f.x, f.z, f.r, 1f); taken.Add(new Vector2(f.x, f.z)); }

            // Trees: sparse around the fire, a denser forest ring on the outskirts.
            var treeKeep = new List<Vector2>(buildings) { Vector2.zero }.ToArray();
            Scatter(root, mats, "Tree", 6, 0.85f, 1.25f, rng, treeKeep, 4.5f, taken, 5f, 7.5f, 13.5f);
            Scatter(root, mats, "Tree", 34, 0.9f, 1.5f, rng, treeKeep, 3.0f, taken, 3f, 16f, 26f);
            Scatter(root, mats, "Bush", 16, 0.8f, 1.2f, rng, buildings.ToArray(), 3.0f, taken, 2.2f, 5f, 24f);
            Scatter(root, mats, "Rock", 9, 0.7f, 1.3f, rng, buildings.ToArray(), 3.0f, taken, 3f, 6f, 24f);
            Scatter(root, mats, "Flowers", 12, 0.9f, 1.3f, rng, buildings.ToArray(), 2.6f, taken, 1.8f, 4f, 20f);
            Scatter(root, mats, "GrassTuft", 30, 0.8f, 1.4f, rng, buildings.ToArray(), 2.4f, taken, 1.3f, 4f, 25f);

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
