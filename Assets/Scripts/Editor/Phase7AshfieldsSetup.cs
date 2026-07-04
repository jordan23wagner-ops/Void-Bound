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
using VoidBound.UI;

namespace VoidBound.Editor
{
    // Phase 7: cross-scene persistence bootstrap in Homestead (Player/Camera/
    // HUDCanvas/EventSystem via GameBootstrap), data-driven Portal destinations,
    // and the new Ashfields.unity zone (ground, light, spawn point, portal,
    // Weak/Standard enemies reusing Phase 4 goblin defs). Idempotent.
    public static class Phase7AshfieldsSetup
    {
        private const string ZoneFolder = "Assets/ScriptableObjects/Zones";
        private const string AshfieldsScenePath = "Assets/Scenes/Ashfields.unity";

        [MenuItem("VoidBound/Setup Phase 7 - Ashfields")]
        public static void Setup()
        {
            var homestead = CreateOrLoadZone("homestead", "Homestead", "Homestead", true);
            var ashfields = CreateOrLoadZone("ashfields", "Ashfields", "Ashfields", true);
            CreateOrLoadZone("bleakwood", "Bleakwood", "", false);
            AssetDatabase.SaveAssets();

            SetupHomestead();
            SetupAshfields();
            EnsureBuildScenes();

            Debug.Log("[Phase7] Ashfields setup complete.");
        }

        // Batch entry point: runs setup, no scene arg needed (opens both internally).
        public static void SetupFromBatch()
        {
            Setup();
        }

        // ═══════════════════════════════════════════════════════════
        // ZONE ASSETS
        // ═══════════════════════════════════════════════════════════
        private static ZoneDefinitionSO CreateOrLoadZone(string id, string display, string scene, bool unlocked)
        {
            var path = $"{ZoneFolder}/{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ZoneDefinitionSO>(path);
            if (existing != null)
            {
                existing.zoneId = id;
                existing.displayName = display;
                existing.sceneName = scene;
                existing.isUnlocked = unlocked;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            if (!AssetDatabase.IsValidFolder(ZoneFolder))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Zones");

            var zone = ScriptableObject.CreateInstance<ZoneDefinitionSO>();
            zone.zoneId = id;
            zone.displayName = display;
            zone.sceneName = scene;
            zone.isUnlocked = unlocked;
            AssetDatabase.CreateAsset(zone, path);
            return zone;
        }

        // ═══════════════════════════════════════════════════════════
        // HOMESTEAD: bootstrap + spawn point + portal wiring
        // ═══════════════════════════════════════════════════════════
        private static void SetupHomestead()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Homestead.unity");

            var player = GameObject.FindGameObjectWithTag("Player");
            var camera = GameObject.FindGameObjectWithTag("MainCamera");
            var hud = GameObject.Find("HUDCanvas");
            var eventSystem = GameObject.Find("EventSystem");
            if (player == null || camera == null || hud == null || eventSystem == null)
            {
                Debug.LogError("[Phase7] Homestead missing Player/Camera/HUDCanvas/EventSystem.");
                return;
            }

            EnsureSpawnPointAt(player.transform.position);

            var bootstrapGO = GameObject.Find("GameBootstrap");
            if (bootstrapGO == null) bootstrapGO = new GameObject("GameBootstrap");
            var bootstrap = bootstrapGO.GetComponent<GameBootstrap>();
            if (bootstrap == null) bootstrap = bootstrapGO.AddComponent<GameBootstrap>();

            var bootstrapSO = new SerializedObject(bootstrap);
            bootstrapSO.FindProperty("player").objectReferenceValue = player;
            bootstrapSO.FindProperty("mainCamera").objectReferenceValue = camera;
            bootstrapSO.FindProperty("hudCanvas").objectReferenceValue = hud;
            bootstrapSO.FindProperty("eventSystem").objectReferenceValue = eventSystem;
            bootstrapSO.ApplyModifiedPropertiesWithoutUndo();

            WirePortalDestinations(hud);

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void WirePortalDestinations(GameObject hud)
        {
            var portalUI = hud.GetComponent<PortalUI>();
            if (portalUI == null)
            {
                Debug.LogWarning("[Phase7] HUDCanvas has no PortalUI — was Phase 6 setup run?");
                return;
            }

            var homestead = AssetDatabase.LoadAssetAtPath<ZoneDefinitionSO>($"{ZoneFolder}/homestead.asset");
            var ashfields = AssetDatabase.LoadAssetAtPath<ZoneDefinitionSO>($"{ZoneFolder}/ashfields.asset");
            var bleakwood = AssetDatabase.LoadAssetAtPath<ZoneDefinitionSO>($"{ZoneFolder}/bleakwood.asset");

            var so = new SerializedObject(portalUI);
            var prop = so.FindProperty("destinations");
            prop.arraySize = 3;
            prop.GetArrayElementAtIndex(0).objectReferenceValue = homestead;
            prop.GetArrayElementAtIndex(1).objectReferenceValue = ashfields;
            prop.GetArrayElementAtIndex(2).objectReferenceValue = bleakwood;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ═══════════════════════════════════════════════════════════
        // ASHFIELDS SCENE
        // ═══════════════════════════════════════════════════════════
        private static void SetupAshfields()
        {
            Scene scene = File.Exists(AshfieldsScenePath)
                ? EditorSceneManager.OpenScene(AshfieldsScenePath)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            EnsureGround();
            EnsureLight();
            EnsureSpawnPointAt(new Vector3(0f, 0.08f, -5f));
            EnsurePortalBuilding();
            EnsureEnemies();

            if (!File.Exists(AshfieldsScenePath))
                EditorSceneManager.SaveScene(scene, AshfieldsScenePath);
            else
                EditorSceneManager.SaveScene(scene);
        }

        private static void EnsureSpawnPointAt(Vector3 position)
        {
            var spawn = GameObject.Find("PlayerSpawnPoint");
            if (spawn == null) spawn = new GameObject("PlayerSpawnPoint");
            spawn.transform.position = position;
        }

        private static void EnsureGround()
        {
            if (GameObject.Find("Ground") != null) return;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(40f, 1f, 40f);

            const string matDir = "Assets/Art/Materials";
            const string matPath = matDir + "/AshfieldsGround.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                if (!AssetDatabase.IsValidFolder(matDir))
                    AssetDatabase.CreateFolder("Assets/Art", "Materials");
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                // Ashen sandy-orange fallback per GDD's "warm oranges/sandy browns" —
                // no RunePortal art reference available; tunable.
                mat.color = new Color(0.72f, 0.5f, 0.32f);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            ground.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void EnsureLight()
        {
            if (GameObject.Find("Directional Light") != null) return;

            var lightGO = new GameObject("Directional Light");
            lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.color = new Color(1f, 0.93f, 0.82f); // warm ashen tint, tunable
        }

        private static void EnsurePortalBuilding()
        {
            if (GameObject.Find("Fast Travel Portal") != null) return;

            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/Building_Portal.fbx");
            GameObject portal = fbx != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(fbx)
                : GameObject.CreatePrimitive(PrimitiveType.Cube);
            portal.name = "Fast Travel Portal";
            portal.transform.position = new Vector3(0f, 0f, -2.5f);

            var box = portal.GetComponent<BoxCollider>();
            if (box == null) box = portal.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.75f, 0f);
            box.size = new Vector3(2f, 1.5f, 2f);
            box.isTrigger = true;

            var station = portal.AddComponent<PortalStation>();
            var so = new SerializedObject(station);
            so.FindProperty("interactPrompt").stringValue = "Fast Travel";
            so.FindProperty("interactRange").floatValue = 3f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureEnemies()
        {
            var scout = AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>("Assets/ScriptableObjects/Enemies/Goblin_Scout.asset");
            var warrior = AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>("Assets/ScriptableObjects/Enemies/Goblin_Warrior.asset");
            var weakLoot = AssetDatabase.LoadAssetAtPath<LootTableSO>("Assets/ScriptableObjects/LootTables/WeakLoot.asset");
            var standardLoot = AssetDatabase.LoadAssetAtPath<LootTableSO>("Assets/ScriptableObjects/LootTables/StandardLoot.asset");

            // Fallback placement — no RunePortal source for exact Ashfields density; tunable.
            SpawnEnemyIfMissing("Ashfields Goblin Scout 1", scout, EnemyTier.Weak, weakLoot, new Vector3(4f, 0.1f, 3f));
            SpawnEnemyIfMissing("Ashfields Goblin Scout 2", scout, EnemyTier.Weak, weakLoot, new Vector3(-6f, 0.1f, 5f));
            SpawnEnemyIfMissing("Ashfields Goblin Warrior 1", warrior, EnemyTier.Standard, standardLoot, new Vector3(8f, 0.1f, -3f));
            SpawnEnemyIfMissing("Ashfields Goblin Warrior 2", warrior, EnemyTier.Standard, standardLoot, new Vector3(-8f, 0.1f, -7f));
        }

        // SceneManager.LoadScene(string) silently fails if the scene isn't in
        // Build Settings — required for PortalUI's real travel to work at all.
        private static void EnsureBuildScenes()
        {
            bool HasScene(string path) =>
                System.Array.Exists(EditorBuildSettings.scenes, s => s.path == path);

            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!HasScene("Assets/Scenes/Homestead.unity"))
                scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/Homestead.unity", true));
            if (!HasScene(AshfieldsScenePath))
                scenes.Add(new EditorBuildSettingsScene(AshfieldsScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void SpawnEnemyIfMissing(string name, EnemyDefinitionSO definition, EnemyTier tier,
                                                 LootTableSO lootTable, Vector3 position)
        {
            if (GameObject.Find(name) != null) return;
            if (definition == null || lootTable == null)
            {
                Debug.LogWarning($"[Phase7] Missing definition/loot table for '{name}' — skipped.");
                return;
            }

            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/EnemyPlaceholder.fbx");
            GameObject go = fbx != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(fbx)
                : GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = position;

            var cc = go.AddComponent<CharacterController>();
            cc.radius = 0.35f;
            cc.height = 1.4f;
            cc.center = new Vector3(0f, 0.7f, 0f);

            go.AddComponent<StatsComponent>();
            go.AddComponent<Health>();

            var ai = go.AddComponent<EnemyAI>();
            var aiSO = new SerializedObject(ai);
            aiSO.FindProperty("definition").objectReferenceValue = definition;
            aiSO.ApplyModifiedPropertiesWithoutUndo();

            var dropper = go.AddComponent<LootDropper>();
            dropper.SetLootTable(lootTable, tier);
        }
    }
}
#endif
