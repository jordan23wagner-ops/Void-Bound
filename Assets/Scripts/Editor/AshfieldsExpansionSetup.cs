#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VoidBound.Combat;
using VoidBound.Core;
using VoidBound.Data;
using VoidBound.Homestead;

namespace VoidBound.Editor
{
    // Ashfields expansion: grows the zone from ±20 to ±40 and gives the empty
    // plain a ring of landmark POIs so there's a reason to explore corners —
    // a War Camp around the Warchief, a Ruined Watchtower with a cache, an Ashen
    // Quarry framing the ore, a Cinder Grove of dead trees, and a lone Fallen
    // Obelisk in the deep north (a mystery hook / groundwork for a future feature).
    // Subtle molten LavaAccents are scattered to warm the ash (~1-2/10 prominence).
    // Also removes the leftover "Ashfields Goblin Warchief" test enemy.
    // Idempotent: rebuilds everything under one root each run. Iterate positions
    // visually — this is a first pass.
    public static class AshfieldsExpansionSetup
    {
        private const string ScenePath = "Assets/Scenes/Ashfields.unity";
        private const string PropDir = "Assets/Art/Models/Props";
        private const string LootDir = "Assets/ScriptableObjects/LootTables";
        private const string RootName = "Ashfields Expansion";
        private const float GroundHalf = 40f; // new half-extent (was 20)

        [MenuItem("VoidBound/Setup Ashfields Expansion")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);

            EnlargeGround();
            TuneGround();
            TuneFog();
            RemoveTestWarchief();

            var existing = GameObject.Find(RootName);
            if (existing != null) Object.DestroyImmediate(existing);
            var root = new GameObject(RootName);

            BuildWarCamp(root.transform);
            BuildWatchtower(root.transform);
            BuildQuarry(root.transform);
            BuildCinderGrove(root.transform);
            BuildObelisk(root.transform);
            ScatterLava(root.transform);
            ScatterAshRocks(root.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[AshfieldsExpansion] Ground grown to ±40, 5 POIs + lava accents placed, test Warchief removed.");
        }

        public static void RunFromBatch() => Run();

        // ── Zone size ──
        private static void EnlargeGround()
        {
            var ground = GameObject.Find("Ground");
            if (ground == null) { Debug.LogWarning("[AshfieldsExpansion] No 'Ground' found."); return; }
            ground.transform.localScale = new Vector3(GroundHalf * 2f, 1f, GroundHalf * 2f);
        }

        // Push the ashen haze out to match the bigger zone: near ground stays clear,
        // the far edges (and the empty mid-field) fade into haze so ±40 reads as a
        // vast, moody field rather than a bare plain. Warm ash tint.
        private static void TuneFog()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.22f, 0.17f, 0.15f); // dark ash haze to match the darker ground
            RenderSettings.fogStartDistance = 26f;
            RenderSettings.fogEndDistance = 95f;
        }

        // Give the ground a charred-ash look with real surface detail: a generated,
        // seamless texture (mottled ash + soot blotches + hairline dark cracks +
        // faint ember flecks) instead of one flat colour — so it reads as cracked
        // volcanic rock and lava veins look like part of it.
        private static void TuneGround()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/AshfieldsGround.mat");
            if (mat == null) { Debug.LogWarning("[AshfieldsExpansion] AshfieldsGround.mat not found — ground unchanged."); return; }

            var tex = BuildAshTexture(256);
            if (!System.IO.Directory.Exists("Assets/Art/Textures")) System.IO.Directory.CreateDirectory("Assets/Art/Textures");
            const string texPath = "Assets/Art/Textures/AshfieldsGround.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (existing != null) AssetDatabase.DeleteAsset(texPath);
            AssetDatabase.CreateAsset(tex, texPath);

            mat.SetColor("_BaseColor", Color.white); // texture carries the colour now
            mat.color = Color.white;
            mat.mainTexture = tex;
            mat.SetTexture("_BaseMap", tex);
            mat.mainTextureScale = new Vector2(10f, 10f);
            mat.SetTextureScale("_BaseMap", new Vector2(10f, 10f));
            mat.SetFloat("_Smoothness", 0.05f); // dry ash, not shiny
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
        }

        private static Texture2D BuildAshTexture(int s)
        {
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, name = "AshfieldsGround" };
            var ash  = new Color(0.18f, 0.15f, 0.135f);
            var soot = new Color(0.085f, 0.07f, 0.065f);
            var ember = new Color(0.34f, 0.17f, 0.09f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float u = x / (float)s, v = y / (float)s;
                    float mottle = Fbm(u, v, 4, 4);                 // broad value variation
                    var c = ash * (0.78f + mottle * 0.5f);
                    float blot = Fbm(u + 0.37f, v + 0.19f, 3, 3);   // dark soot patches
                    if (blot < 0.42f) c = Color.Lerp(c, soot, (0.42f - blot) * 2f);
                    float crack = 1f - Mathf.Abs(Fbm(u, v, 9, 2) * 2f - 1f); // ridged -> hairline cracks
                    if (crack > 0.93f) c *= 0.5f;
                    float fleck = Fbm(u + 0.7f, v + 0.5f, 34, 2);    // rare warm ember specks
                    if (fleck > 0.86f) c = Color.Lerp(c, ember, (fleck - 0.86f) * 3f);
                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            return tex;
        }

        // Seamless (tiling) value noise: bilinear blend of a Perlin sample with its
        // period-shifted neighbours so the texture wraps without a visible seam.
        private static float SeamlessPerlin(float x, float y, float period)
        {
            float wx = (x % period) / period, wy = (y % period) / period;
            float a = Mathf.PerlinNoise(x, y);
            float b = Mathf.PerlinNoise(x - period, y);
            float c = Mathf.PerlinNoise(x, y - period);
            float d = Mathf.PerlinNoise(x - period, y - period);
            return a * (1 - wx) * (1 - wy) + b * wx * (1 - wy) + c * (1 - wx) * wy + d * wx * wy;
        }

        private static float Fbm(float u, float v, int baseFreq, int octaves)
        {
            float sum = 0f, amp = 1f, norm = 0f;
            int f = baseFreq;
            for (int o = 0; o < octaves; o++)
            {
                sum += SeamlessPerlin(u * f, v * f, f) * amp;
                norm += amp; amp *= 0.5f; f *= 2;
            }
            return sum / norm;
        }

        private static void RemoveTestWarchief()
        {
            var test = GameObject.Find("Ashfields Goblin Warchief");
            if (test != null) { Object.DestroyImmediate(test); Debug.Log("[AshfieldsExpansion] Removed test 'Ashfields Goblin Warchief'."); }
        }

        // ── POIs ──

        // Around the Warchief arena (boss at ~0,16): a goblin encampment.
        private static void BuildWarCamp(Transform root)
        {
            var poi = Group("POI - Goblin War Camp", root, new Vector3(0f, 0f, 23f));
            Campfire(poi, new Vector3(0f, 0f, 0f));
            Prop(poi, "Totem L", "BrokenPillar", new Vector3(-3.5f, 0f, 1f), 15f, 1.6f);
            Prop(poi, "Totem R", "BrokenPillar", new Vector3(3.5f, 0f, 1f), -15f, 1.6f);
            // supply clutter
            Prop(poi, "Crate 1", "Crate", new Vector3(-4.5f, 0f, -2f), 20f, 1f);
            Prop(poi, "Crate 2", "Crate", new Vector3(-3.8f, 0.6f, -2.3f), 60f, 0.9f);
            Prop(poi, "Barrel 1", "Barrel", new Vector3(4.2f, 0f, -1.8f), 0f, 1f);
            Prop(poi, "Brazier L", "Brazier", new Vector3(-6f, 0f, 2f), 0f, 1f);
            Prop(poi, "Brazier R", "Brazier", new Vector3(6f, 0f, 2f), 0f, 1f);
            Prop(poi, "Bones", "Bones", new Vector3(2f, 0f, -3f), 40f, 1f);
            // rough palisade at the camp mouth (south side, facing the player's approach)
            for (int i = -2; i <= 2; i++)
                Prop(poi, "Palisade " + i, "Fence", new Vector3(i * 2.2f, 0f, -5f), 0f, 1f);
            Prop(poi, "Spikes", "Spikes", new Vector3(-1.5f, 0f, -5.4f), 0f, 1f);
        }

        // SE corner: a broken lookout tower + a lootable cache.
        private static void BuildWatchtower(Transform root)
        {
            var poi = Group("POI - Ruined Watchtower", root, new Vector3(29f, 0f, -25f));
            Prop(poi, "Tower Base", "BrokenPillar", new Vector3(0f, 0f, 0f), 0f, 3.8f);
            Prop(poi, "Tower Lean", "BrokenPillar", new Vector3(1.6f, 0f, 0.8f), 22f, 2.6f);
            Prop(poi, "Rubble 1", "Boulder", new Vector3(-2.5f, 0f, 1.5f), 30f, 1.1f);
            Prop(poi, "Rubble 2", "Rock", new Vector3(2.2f, 0f, -2f), 70f, 1f);
            Prop(poi, "Bones", "Bones", new Vector3(-1f, 0f, -1.5f), 10f, 1f);
            Chest(poi, new Vector3(-2f, 0f, -0.5f), 25f, "StandardLoot");
        }

        // E, past the ore cluster: a dug quarry.
        private static void BuildQuarry(Transform root)
        {
            var poi = Group("POI - Ashen Quarry", root, new Vector3(30f, 0f, 8f));
            Prop(poi, "Winch Post", "BrokenPillar", new Vector3(0f, 0f, 4f), 5f, 2.3f); // tall silhouette
            Prop(poi, "Pit Rock 1", "Boulder", new Vector3(-2f, 0f, 2f), 0f, 1.4f);
            Prop(poi, "Pit Rock 2", "Boulder", new Vector3(3f, 0f, -1f), 40f, 1.6f);
            Prop(poi, "Support 1", "Fence", new Vector3(-3f, 0f, -2f), 90f, 1f);
            Prop(poi, "Support 2", "Fence", new Vector3(3.5f, 0f, 2.5f), 90f, 1f);
            Prop(poi, "Cart Crate", "Crate", new Vector3(0f, 0f, -3f), 15f, 1f);
            Prop(poi, "Barrel", "Barrel", new Vector3(1.2f, 0f, -3.4f), 0f, 0.9f);
            Prop(poi, "Rubble", "Rock", new Vector3(-1.5f, 0f, 3.2f), 55f, 1.1f);
        }

        // W/NW: a grove of dead, ash-blasted trees.
        private static void BuildCinderGrove(Transform root)
        {
            var poi = Group("POI - Cinder Grove", root, new Vector3(-29f, 0f, 15f));
            var offsets = new[]
            {
                new Vector3(0f,0f,0f), new Vector3(3.5f,0f,1.5f), new Vector3(-2.5f,0f,2.5f),
                new Vector3(1.5f,0f,-3f), new Vector3(-3.5f,0f,-2f), new Vector3(4.5f,0f,-2.5f),
            };
            for (int i = 0; i < offsets.Length; i++)
                Prop(poi, "Dead Tree " + i, "DeadTree", offsets[i], i * 47f, 0.9f + (i % 3) * 0.2f);
            Prop(poi, "Ash Mound 1", "Boulder", new Vector3(-1f, 0f, 0.5f), 0f, 0.7f);
            Prop(poi, "Bush", "Bush", new Vector3(2f, 0f, 2f), 0f, 0.9f);
        }

        // Deep north, isolated: the Fallen Obelisk — a mystery landmark, ringed in
        // molten cracks. (Groundwork focal point for a future feature.)
        private static void BuildObelisk(Transform root)
        {
            var poi = Group("POI - Fallen Obelisk", root, new Vector3(0f, 0f, 35f));
            Prop(poi, "Monolith", "BrokenPillar", new Vector3(0f, 0f, 0f), 8f, 3.4f);
            Prop(poi, "Shard 1", "BrokenPillar", new Vector3(-2.5f, 0f, 1.5f), 40f, 1.1f);
            Prop(poi, "Shard 2", "BrokenPillar", new Vector3(2.2f, 0f, -1.8f), -30f, 0.9f);
            Prop(poi, "Bones", "Bones", new Vector3(1.5f, 0f, 2f), 0f, 1f);
            // lava ring — the obelisk runs a touch hotter than the rest of the field
            for (int i = 0; i < 5; i++)
            {
                float a = i / 5f * Mathf.PI * 2f;
                Lava(poi, new Vector3(Mathf.Cos(a) * 3.2f, 0f, Mathf.Sin(a) * 3.2f));
            }
        }

        // Sparse molten accents across the field — weighted toward the deeper
        // (northern) reaches, kept clear of the spawn/portal in the south.
        private static void ScatterLava(Transform root)
        {
            var g = new GameObject("Lava Accents");
            g.transform.SetParent(root, false);
            var spots = new[]
            {
                new Vector3(6f,0f,10f), new Vector3(-9f,0f,6f), new Vector3(16f,0f,20f),
                new Vector3(-20f,0f,12f), new Vector3(22f,0f,-6f), new Vector3(-16f,0f,26f),
                new Vector3(11f,0f,30f), new Vector3(-24f,0f,-4f), new Vector3(3f,0f,18f),
            };
            foreach (var s in spots) Lava(g.transform, s);
        }

        // Boulders + dead trees + bone piles spread across the field so the wide
        // ±40 ground reads as a populated wasteland, not a bare plain (fake relief).
        private static void ScatterAshRocks(Transform root)
        {
            var g = new GameObject("Ash Scatter");
            g.transform.SetParent(root, false);
            var rocks = new[]
            {
                new Vector3(-18f,0f,-18f), new Vector3(20f,0f,26f), new Vector3(-26f,0f,-14f),
                new Vector3(26f,0f,14f), new Vector3(-12f,0f,32f), new Vector3(14f,0f,-26f),
                new Vector3(-34f,0f,4f), new Vector3(34f,0f,-8f), new Vector3(-30f,0f,30f),
                new Vector3(30f,0f,-30f), new Vector3(-8f,0f,-30f), new Vector3(10f,0f,38f),
                new Vector3(-36f,0f,-24f), new Vector3(36f,0f,24f), new Vector3(0f,0f,-28f),
                new Vector3(-22f,0f,-2f),
            };
            for (int i = 0; i < rocks.Length; i++)
                Prop(g.transform, "Ash Rock " + i, i % 2 == 0 ? "Boulder" : "Rock", rocks[i], i * 41f, 1.3f + (i % 3) * 0.5f);

            // Lone dead trees + bones dotted through the mid-field for silhouette variety.
            var dead = new[]
            {
                new Vector3(-16f,0f,10f), new Vector3(18f,0f,-12f), new Vector3(-24f,0f,20f),
                new Vector3(8f,0f,-20f), new Vector3(24f,0f,32f),
            };
            for (int i = 0; i < dead.Length; i++)
                Prop(g.transform, "Lone Tree " + i, "DeadTree", dead[i], i * 63f, 1.1f);
            Prop(g.transform, "Bones A", "Bones", new Vector3(-6f, 0f, -24f), 20f, 1f);
            Prop(g.transform, "Bones B", "Bones", new Vector3(22f, 0f, 4f), 70f, 1f);
        }

        // ── helpers ──
        private static Transform Group(string name, Transform parent, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            return go.transform;
        }

        private static GameObject Prop(Transform parent, string name, string propFile, Vector3 localPos, float yaw, float scale)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>($"{PropDir}/{propFile}.fbx");
            GameObject go = fbx != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(fbx)
                : GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        private static void Campfire(Transform parent, Vector3 localPos)
        {
            var go = new GameObject("Campfire");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.AddComponent<BonfireEffect>();
        }

        private static void Lava(Transform parent, Vector3 localPos)
        {
            var go = new GameObject("Lava Accent");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.AddComponent<LavaAccent>();
        }

        private static void Chest(Transform parent, Vector3 localPos, float yaw, string lootId)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>($"{PropDir}/Crate.fbx");
            GameObject go = fbx != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(fbx)
                : GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Cache (Loot Chest)";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            var col = go.GetComponent<Collider>();
            if (col == null) { var box = go.AddComponent<BoxCollider>(); box.isTrigger = true; box.center = new Vector3(0, 0.5f, 0); box.size = new Vector3(1.6f, 1.5f, 1.6f); }
            else col.isTrigger = true;

            var chest = go.AddComponent<LootChest>();
            // Load AFTER OpenScene so the ref serialises onto the scene object (the
            // documented pre-OpenScene null-serialisation gotcha).
            var loot = AssetDatabase.LoadAssetAtPath<LootTableSO>($"{LootDir}/{lootId}.asset");
            if (loot != null) chest.SetLootTable(loot);
            else Debug.LogWarning($"[AshfieldsExpansion] Loot table '{lootId}' not found for the watchtower cache.");

            var so = new SerializedObject(chest);
            so.FindProperty("interactPrompt").stringValue = "Loot";
            so.FindProperty("interactRange").floatValue = 2.5f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
