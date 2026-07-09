#if UNITY_EDITOR
using System.IO;
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
    // The major feature: Bleakwood, the zone beyond Ashfields. A dead forest where a
    // great battle was lost — the ground is dark and blood-soaked, the fallen (goblins
    // AND heroes) lie strewn about, and their corpses have risen. Builds the whole
    // scene: dark forest-floor terrain, gloomy fog/light, dead trees, corpse props,
    // blood patches, risen-dead spawners (pallid reskins of the goblin/hero rigs), a
    // return portal, and unlocks the zone for Fast Travel. Idempotent.
    public static class BleakwoodSetup
    {
        private const string ScenePath = "Assets/Scenes/Bleakwood.unity";
        private const string PropDir = "Assets/Art/Models/Props";
        private const string MatDir = "Assets/Art/Materials";
        private const string EnemyDir = "Assets/ScriptableObjects/Enemies";
        private const string LootDir = "Assets/ScriptableObjects/LootTables";
        private const string RootName = "Bleakwood Content";
        private const float Half = 40f;

        // Corpse palette — sickly green-grey rot so the risen read as clearly dead,
        // not living villagers.
        private static readonly Color FleshCol = new Color(0.40f, 0.47f, 0.36f); // rotted green-grey
        private static readonly Color HairCol  = new Color(0.07f, 0.075f, 0.07f);
        private static readonly Color RobeCol  = new Color(0.13f, 0.13f, 0.145f); // tattered dark

        [MenuItem("VoidBound/Setup Bleakwood")]
        public static void Setup()
        {
            AuthorUndead(out var risenGoblin, out var fallenHero, out var bleakLoot);
            UnlockZone();

            var scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Reload the just-authored assets AFTER the scene exists, else the refs
            // serialise as null on the scene's spawners (pre-scene-creation gotcha).
            risenGoblin = AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>($"{EnemyDir}/Risen_Goblin.asset");
            fallenHero = AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>($"{EnemyDir}/Fallen_Hero.asset");
            bleakLoot = AssetDatabase.LoadAssetAtPath<LootTableSO>($"{LootDir}/BleakwoodLoot.asset");

            BuildGround();
            BuildLightingFog();
            EnsureSpawnPoint(new Vector3(0f, 0.08f, -5f));
            EnsurePortal(new Vector3(0f, 0f, -2.5f));

            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(RootName).transform;

            Random.InitState(90210);
            BuildDeadForest(root);
            ScatterCorpses(root);
            ScatterBlood(root);
            BuildEncounters(root, risenGoblin, fallenHero, bleakLoot);

            if (!File.Exists(ScenePath)) EditorSceneManager.SaveScene(scene, ScenePath);
            else EditorSceneManager.SaveScene(scene);
            RegisterBuildScene();
            ItemRegistryBaker.Bake();

            Debug.Log("[Bleakwood] Zone built + unlocked. Save if prompted; travel via the Fast Travel portal.");
        }

        public static void RunFromBatch() => Setup();

        // ── Undead content ──
        private static void AuthorUndead(out EnemyDefinitionSO risenGoblin, out EnemyDefinitionSO fallenHero, out LootTableSO loot)
        {
            // Loot: a step up from Ashfields — more gold/shards + a shot at dread gear + bleak logs.
            loot = LoadOrCreate<LootTableSO>($"{LootDir}/BleakwoodLoot.asset");
            loot.tableId = "BleakwoodLoot";
            loot.displayName = "Bleakwood Loot";
            var dread = new System.Collections.Generic.List<GearItemSO>();
            foreach (var id in new[] { "dread_helm", "dread_chest", "dread_legs", "dread_boots", "dread_gauntlets" })
            {
                var g = FindGear(id); if (g != null) dread.Add(g);
            }
            loot.gearPool = dread.ToArray();
            loot.gearDropChance = 0.10f;
            loot.rarityWeights = new[] { new RarityWeight { rarity = RarityTier.Rare, weight = 1f } };
            var bleakLog = FindMaterial("log_6"); // Bleak Logs
            loot.materialDrops = bleakLog != null
                ? new[] { new MaterialDrop { material = bleakLog, chance = 0.35f, minQuantity = 1, maxQuantity = 2 } }
                : new MaterialDrop[0];
            loot.goldMin = 30; loot.goldMax = 90;
            loot.voidShardMin = 1; loot.voidShardMax = 4;
            loot.zoneModifier = 1.5f;
            EditorUtility.SetDirty(loot);

            risenGoblin = LoadOrCreate<EnemyDefinitionSO>($"{EnemyDir}/Risen_Goblin.asset");
            risenGoblin.enemyId = "risen_goblin";
            risenGoblin.displayName = "Risen Goblin";
            risenGoblin.tier = EnemyTier.Elite;
            risenGoblin.baseStats = new CharacterStats(16, 6, 28, 5); // HP 380
            risenGoblin.baseDamage = 12; risenGoblin.moveSpeed = 2.0f; // shamble
            risenGoblin.aggroRange = 14f; risenGoblin.attackRange = 2.2f;
            risenGoblin.appliesPoison = true; risenGoblin.poisonChance = 0.4f;
            risenGoblin.poisonDamage = 10; risenGoblin.poisonDuration = 6f;
            risenGoblin.lootTable = loot;
            EditorUtility.SetDirty(risenGoblin);

            fallenHero = LoadOrCreate<EnemyDefinitionSO>($"{EnemyDir}/Fallen_Hero.asset");
            fallenHero.enemyId = "fallen_hero";
            fallenHero.displayName = "Fallen Hero";
            fallenHero.tier = EnemyTier.NamedElite;
            fallenHero.baseStats = new CharacterStats(22, 8, 38, 8); // HP 480
            fallenHero.baseDamage = 18; fallenHero.moveSpeed = 2.2f;
            fallenHero.aggroRange = 15f; fallenHero.attackRange = 2.4f;
            fallenHero.appliesPoison = false;
            fallenHero.lootTable = loot;
            EditorUtility.SetDirty(fallenHero);

            AssetDatabase.SaveAssets();
            Debug.Log("[Bleakwood] Authored Risen Goblin + Fallen Hero + BleakwoodLoot.");
        }

        private static void UnlockZone()
        {
            var zone = AssetDatabase.LoadAssetAtPath<ZoneDefinitionSO>("Assets/ScriptableObjects/Zones/bleakwood.asset");
            if (zone == null) { Debug.LogWarning("[Bleakwood] bleakwood zone asset not found."); return; }
            zone.sceneName = "Bleakwood";
            zone.isUnlocked = true;
            EditorUtility.SetDirty(zone);
            AssetDatabase.SaveAssets();
        }

        // ── Terrain + atmosphere ──
        private static void BuildGround()
        {
            var ground = GameObject.Find("Ground");
            if (ground == null)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.name = "Ground";
            }
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(Half * 2f, 1f, Half * 2f);

            const string matPath = MatDir + "/BleakwoodGround.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, matPath); }
            var tex = BuildBleakTexture(256);
            if (!Directory.Exists("Assets/Art/Textures")) Directory.CreateDirectory("Assets/Art/Textures");
            const string texPath = "Assets/Art/Textures/BleakwoodGround.asset";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(texPath) != null) AssetDatabase.DeleteAsset(texPath);
            AssetDatabase.CreateAsset(tex, texPath);
            mat.SetColor("_BaseColor", Color.white); mat.color = Color.white;
            mat.mainTexture = tex; mat.SetTexture("_BaseMap", tex);
            mat.mainTextureScale = new Vector2(11f, 11f); mat.SetTextureScale("_BaseMap", new Vector2(11f, 11f));
            mat.SetFloat("_Smoothness", 0.08f);
            EditorUtility.SetDirty(mat);
            ground.GetComponent<MeshRenderer>().sharedMaterial = mat;
            AssetDatabase.SaveAssets();
        }

        private static Texture2D BuildBleakTexture(int s)
        {
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, name = "BleakwoodGround" };
            var earth = new Color(0.14f, 0.13f, 0.115f);   // dark cold mud
            var mud   = new Color(0.08f, 0.075f, 0.065f);
            var leaf  = new Color(0.24f, 0.19f, 0.11f);     // dull dead leaf
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float u = x / (float)s, v = y / (float)s;
                    float mottle = Fbm(u, v, 5, 4);
                    var c = earth * (0.8f + mottle * 0.45f);
                    float blot = Fbm(u + 0.31f, v + 0.6f, 3, 3);
                    if (blot < 0.4f) c = Color.Lerp(c, mud, (0.4f - blot) * 2.2f);   // damp mud
                    float fleck = Fbm(u + 0.8f, v + 0.2f, 22, 2);
                    if (fleck > 0.8f) c = Color.Lerp(c, leaf, (fleck - 0.8f) * 2.2f); // dead leaves
                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            return tex;
        }

        private static void BuildLightingFog()
        {
            var lightGO = GameObject.Find("Directional Light");
            if (lightGO == null) lightGO = new GameObject("Directional Light");
            var light = lightGO.GetComponent<Light>();
            if (light == null) light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGO.transform.rotation = Quaternion.Euler(58f, 20f, 0f);
            light.intensity = 0.7f;
            light.color = new Color(0.72f, 0.76f, 0.78f); // cold overcast

            RenderSettings.ambientLight = new Color(0.20f, 0.22f, 0.21f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.12f, 0.14f, 0.13f); // cold gloom
            RenderSettings.fogStartDistance = 18f;
            RenderSettings.fogEndDistance = 72f; // gloomy but the fight area stays readable
        }

        private static void EnsureSpawnPoint(Vector3 pos)
        {
            var sp = GameObject.Find("PlayerSpawnPoint");
            if (sp == null) sp = new GameObject("PlayerSpawnPoint");
            sp.transform.position = pos;
        }

        private static void EnsurePortal(Vector3 pos)
        {
            var portal = GameObject.Find("Fast Travel Portal");
            if (portal == null)
            {
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/Building_Portal.fbx");
                portal = fbx != null ? (GameObject)PrefabUtility.InstantiatePrefab(fbx) : GameObject.CreatePrimitive(PrimitiveType.Cube);
                portal.name = "Fast Travel Portal";
            }
            portal.transform.position = pos;
            var box = portal.GetComponent<BoxCollider>();
            if (box == null) box = portal.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.75f, 0f); box.size = new Vector3(2f, 1.5f, 2f); box.isTrigger = true;
            var station = portal.GetComponent<PortalStation>();
            if (station == null) station = portal.AddComponent<PortalStation>();
            var so = new SerializedObject(station);
            so.FindProperty("interactPrompt").stringValue = "Fast Travel";
            so.FindProperty("interactRange").floatValue = 3f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Population ──
        private static void BuildDeadForest(Transform root)
        {
            var g = new GameObject("Dead Forest").transform; g.SetParent(root, false);
            var bark = LoadOrCreateMat("BleakBark", new Color(0.10f, 0.09f, 0.08f)); // dead grey-brown
            for (int i = 0; i < 62; i++)
                BuildDeadTree(g, "Dead Tree " + i, RingPoint(7f, Half - 3f), bark, Random.Range(1, int.MaxValue));
        }

        // A gnarled bare dead tree: a leaning tapered trunk forking into thinning,
        // crooked branches. Built from cylinders, seeded so each is unique.
        private static void BuildDeadTree(Transform parent, string name, Vector3 pos, Material bark, int seed)
        {
            var tree = new GameObject(name).transform;
            tree.SetParent(parent, false);
            tree.position = pos;
            tree.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var rng = new System.Random(seed);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            float scale = R(0.8f, 1.5f);
            var up = Quaternion.Euler(R(-9f, 9f), 0f, R(-9f, 9f)) * Vector3.up; // slight lean
            Branch(tree, bark, Vector3.zero, up, R(2.2f, 3.4f) * scale, R(0.14f, 0.2f) * scale, 2, rng);
        }

        private static void Branch(Transform parent, Material bark, Vector3 start, Vector3 dir,
            float length, float thick, int depth, System.Random rng)
        {
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            dir = dir.normalized;

            var seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var col = seg.GetComponent<Collider>(); if (col != null) Object.DestroyImmediate(col);
            seg.name = "Limb";
            seg.transform.SetParent(parent, false);
            seg.transform.localPosition = start + dir * (length * 0.5f);
            seg.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
            seg.transform.localScale = new Vector3(thick, length * 0.5f, thick); // cylinder is 2 units tall
            seg.GetComponent<Renderer>().sharedMaterial = bark;

            if (depth <= 0) return;
            var end = start + dir * length;
            int kids = rng.Next(2, 4);
            for (int k = 0; k < kids; k++)
            {
                var nd = Quaternion.Euler(R(18f, 46f), R(0f, 360f), 0f) * dir;
                nd = Vector3.Slerp(nd, Vector3.up, 0.12f);          // still reach upward
                Branch(parent, bark, end, nd, length * R(0.55f, 0.72f), thick * R(0.5f, 0.66f), depth - 1, rng);
            }
        }

        private static void ScatterCorpses(Transform root)
        {
            var g = new GameObject("The Fallen").transform; g.SetParent(root, false);
            var flesh = LoadOrCreateMat("UndeadFlesh", FleshCol);
            var hair = LoadOrCreateMat("UndeadHair", HairCol);
            var robe = LoadOrCreateMat("UndeadRobe", RobeCol);

            // Fallen heroes (Hero rig, laid flat, pallid).
            for (int i = 0; i < 7; i++)
                Corpse(g, "Fallen Hero Corpse " + i, "Assets/Art/Models/Hero.fbx", RingPoint(6f, Half - 5f),
                    new[] { flesh, hair, robe });
            // Dead goblins (Warrior/Scout rig, laid flat, greyed).
            for (int i = 0; i < 10; i++)
            {
                string fbx = i % 2 == 0 ? "Assets/Art/Models/Goblin_Warrior.fbx" : "Assets/Art/Models/Goblin_Scout.fbx";
                Corpse(g, "Goblin Corpse " + i, fbx, RingPoint(6f, Half - 5f), GoblinDeadMats(fbx, flesh));
            }
        }

        private static void ScatterBlood(Transform root)
        {
            var g = new GameObject("Blood").transform; g.SetParent(root, false);
            for (int i = 0; i < 16; i++)
            {
                var go = new GameObject("Blood Patch " + i);
                go.transform.SetParent(g, false);
                go.transform.position = RingPoint(4f, Half - 4f);
                go.AddComponent<BloodPatch>();
            }
        }

        private static void BuildEncounters(Transform root, EnemyDefinitionSO risenGoblin, EnemyDefinitionSO fallenHero, LootTableSO loot)
        {
            var g = new GameObject("Encounters").transform; g.SetParent(root, false);
            var goblinCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/GoblinAnimator.controller");
            var heroCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/HeroAnimator.controller");
            var flesh = LoadOrCreateMat("UndeadFlesh", FleshCol);
            var hair = LoadOrCreateMat("UndeadHair", HairCol);
            var robe = LoadOrCreateMat("UndeadRobe", RobeCol);

            // Risen goblins — several packs.
            var gobFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/Goblin_Warrior.fbx");
            var gobMats = GoblinDeadMats("Assets/Art/Models/Goblin_Warrior.fbx", flesh);
            foreach (var pos in new[] { new Vector3(10f, 0.1f, 8f), new Vector3(-12f, 0.1f, 6f), new Vector3(6f, 0.1f, 20f), new Vector3(-9f, 0.1f, -14f) })
                Spawner(g, "Risen Pack", risenGoblin, loot, gobFbx, goblinCtrl, gobMats, 1.0f, 2, pos);

            // Fallen heroes — fewer, tougher.
            var heroFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/Hero.fbx");
            var heroMats = new[] { flesh, hair, robe };
            foreach (var pos in new[] { new Vector3(0f, 0.1f, 24f), new Vector3(16f, 0.1f, -8f) })
                Spawner(g, "Fallen Hero", fallenHero, loot, heroFbx, heroCtrl, heroMats, 1.0f, 1, pos);
        }

        // ── helpers ──
        private static Vector3 RingPoint(float inner, float outer)
        {
            // Anywhere on the ground but clear of the spawn/portal pocket in the south.
            for (int tries = 0; tries < 20; tries++)
            {
                var p = new Vector3(Random.Range(-outer, outer), 0f, Random.Range(-outer, outer));
                if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(0f, -4f)) >= inner) return p;
            }
            return new Vector3(Random.Range(-outer, outer), 0f, outer);
        }

        private static GameObject Prop(Transform parent, string name, string propFile, Vector3 pos, float yaw, float scale, Color tint)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>($"{PropDir}/{propFile}.fbx");
            GameObject go = fbx != null ? (GameObject)PrefabUtility.InstantiatePrefab(fbx) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            if (tint.a > 0f)
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                    r.sharedMaterial = LoadOrCreateMat("BleakBark", tint);
            return go;
        }

        // A rigged model laid flat on the ground as a corpse (no animator).
        private static void Corpse(Transform parent, string name, string fbxPath, Vector3 pos, Material[] mats)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbx == null) return;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos + new Vector3(0f, 0.15f, 0f);
            go.transform.localRotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f); // lie on the ground
            var smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null && mats != null)
            {
                int nn = smr.sharedMaterials.Length;
                var m = new Material[nn];
                for (int i = 0; i < nn; i++) m[i] = mats[Mathf.Min(i, mats.Length - 1)];
                smr.sharedMaterials = m;
            }
        }

        private static void Spawner(Transform parent, string label, EnemyDefinitionSO def, LootTableSO loot,
            GameObject fbx, RuntimeAnimatorController ctrl, Material[] slotMats, float scale, int maxAlive, Vector3 pos)
        {
            var go = new GameObject($"Spawner - {label}");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var spawner = go.AddComponent<EnemySpawner>();
            var so = new SerializedObject(spawner);
            so.FindProperty("definition").objectReferenceValue = def;
            so.FindProperty("lootTable").objectReferenceValue = loot;
            so.FindProperty("tier").enumValueIndex = (int)def.tier;
            so.FindProperty("modelFbx").objectReferenceValue = fbx;
            so.FindProperty("animatorController").objectReferenceValue = ctrl;
            var arr = so.FindProperty("slotMaterials");
            arr.arraySize = slotMats.Length;
            for (int i = 0; i < slotMats.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = slotMats[i];
            so.FindProperty("modelScale").floatValue = scale;
            so.FindProperty("maxAlive").intValue = maxAlive;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Goblin FBX per-slot materials, but the skin slot swapped for pallid undead flesh.
        private static Material[] GoblinDeadMats(string fbxPath, Material flesh)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var smr = fbx != null ? fbx.GetComponentInChildren<SkinnedMeshRenderer>() : null;
            if (smr == null) return new[] { flesh };
            var slots = smr.sharedMaterials;
            var result = new Material[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                string n = slots[i] != null ? slots[i].name : "";
                result[i] = n.Contains("Cloth") ? LoadMat("GoblinCloth")
                    : n.Contains("Dark") ? LoadMat("GoblinDark")
                    : n.Contains("Gold") ? LoadMat("GoblinGold")
                    : n.Contains("Gem")  ? LoadMat("GoblinGem")
                    : n.Contains("Bone") ? LoadMat("GoblinBone")
                    : flesh;
            }
            return result;
        }

        private static void RegisterBuildScene()
        {
            bool has = System.Array.Exists(EditorBuildSettings.scenes, s => s.path == ScenePath);
            if (has) return;
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes)
            { new EditorBuildSettingsScene(ScenePath, true) };
            EditorBuildSettings.scenes = list.ToArray();
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) { a = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(a, path); }
            return a;
        }

        private static Material LoadOrCreateMat(string name, Color color)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.color = color; m.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material LoadMat(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/{name}.mat");
        private static GearItemSO FindGear(string id) => FindAsset<GearItemSO>(id, g => g.itemId == id);
        private static MaterialItemSO FindMaterial(string id) => FindAsset<MaterialItemSO>(id, m => m.itemId == id);

        private static T FindAsset<T>(string id, System.Func<T, bool> match) where T : ScriptableObject
        {
            foreach (var guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
            {
                var a = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (a != null && match(a)) return a;
            }
            return null;
        }

        // Seamless tiling value noise (shared with the Ashfields texture gen).
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
            float sum = 0f, amp = 1f, norm = 0f; int f = baseFreq;
            for (int o = 0; o < octaves; o++) { sum += SeamlessPerlin(u * f, v * f, f) * amp; norm += amp; amp *= 0.5f; f *= 2; }
            return sum / norm;
        }
    }
}
#endif
