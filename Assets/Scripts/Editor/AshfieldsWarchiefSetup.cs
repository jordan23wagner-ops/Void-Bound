#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VoidBound.Combat;
using VoidBound.Data;
using VoidBound.UI;

namespace VoidBound.Editor
{
    // Phase 2: the Ashfields mini-boss encounter. Authors the boss's unique loot
    // (Warchief's Fang @ ~1/10, Warchief's Cleaver @ ~1/25), promotes the
    // goblin_warchief definition to boss stats, and plants a single set-piece
    // Warchief (GoblinWarchiefBoss charge-hunter AI + rigged model) in a northern
    // arena, plus the on-screen boss health bar. Idempotent. Run with Ashfields open,
    // or use RunFromBatch.
    public static class AshfieldsWarchiefSetup
    {
        private const string ScenePath = "Assets/Scenes/Ashfields.unity";
        private const string MatDir  = "Assets/ScriptableObjects/Materials/Trophies";
        private const string GearDir = "Assets/ScriptableObjects/Gear/Boss";
        private const string LootDir = "Assets/ScriptableObjects/LootTables";
        private const string EnemyDir = "Assets/ScriptableObjects/Enemies";
        private const string BossName = "Ashfields Warchief Boss";
        private static readonly Vector3 ArenaPos = new Vector3(0f, 0.1f, 16f);

        [MenuItem("VoidBound/Setup Phase 2 - Warchief Boss")]
        public static void Setup()
        {
            AuthorLoot(out var fang, out var cleaver, out var bossTable);
            PromoteWarchiefDefinition(bossTable);

            if (SceneManager.GetActiveScene().name != "Ashfields")
            {
                Debug.LogError("[Phase2] Open Ashfields.unity first (loot + definition were still authored).");
                return;
            }
            PlaceBoss(bossTable);
            AddBossHealthBar();
            ItemRegistryBaker.Bake();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[Phase2] Warchief boss encounter wired. Save the scene (Ctrl+S). Registry baked.");
        }

        public static void RunFromBatch()
        {
            AuthorLoot(out _, out _, out var bossTable);
            PromoteWarchiefDefinition(bossTable);
            EditorSceneManager.OpenScene(ScenePath);
            PlaceBoss(bossTable);
            AddBossHealthBar();
            ItemRegistryBaker.Bake();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        // ── Unique loot ──
        private static void AuthorLoot(out MaterialItemSO fang, out GearItemSO cleaver, out LootTableSO table)
        {
            EnsureDir(MatDir); EnsureDir(GearDir); EnsureDir(LootDir);

            fang = LoadOrCreate<MaterialItemSO>($"{MatDir}/warchief_fang.asset");
            fang.itemId = "warchief_fang";
            fang.displayName = "Warchief's Fang";
            fang.description = "A jagged tusk torn from the Ashfields Warchief. A trophy — and, they say, a key to something yet to come.";
            fang.tier = RarityTier.Epic;
            fang.goldValue = 45;
            fang.isConsumable = false;
            EditorUtility.SetDirty(fang);

            cleaver = LoadOrCreate<GearItemSO>($"{GearDir}/warchief_cleaver.asset");
            cleaver.itemId = "warchief_cleaver";
            cleaver.displayName = "Warchief's Cleaver";
            cleaver.slot = EquipmentSlot.Weapon;
            cleaver.weaponType = WeaponType.Sword; // melee style
            cleaver.rarity = RarityTier.Epic;
            cleaver.statModifiers = new CharacterStats(14, 2, 4, 0);
            cleaver.baseDamage = 26;
            cleaver.goldValue = 400;
            cleaver.untradable = false;
            EditorUtility.SetDirty(cleaver);

            table = LoadOrCreate<LootTableSO>($"{LootDir}/WarchiefLoot.asset");
            table.tableId = "WarchiefLoot";
            table.displayName = "Warchief Loot";
            table.gearPool = new[] { cleaver };
            table.gearDropChance = 0.04f;                 // ~1/25 Cleaver
            table.rarityWeights = new[] { new RarityWeight { rarity = RarityTier.Epic, weight = 1f } };
            table.materialDrops = new[]
            {
                new MaterialDrop { material = fang, chance = 0.10f, minQuantity = 1, maxQuantity = 1 }, // ~1/10 Fang
            };
            table.goldMin = 80; table.goldMax = 160;
            table.voidShardMin = 1; table.voidShardMax = 3;
            table.zoneModifier = 1f;
            EditorUtility.SetDirty(table);

            AssetDatabase.SaveAssets();
            Debug.Log("[Phase2] Authored Warchief's Fang (1/10) + Cleaver (1/25) + WarchiefLoot table.");
        }

        // ── Boss stats on the shared definition ──
        private static void PromoteWarchiefDefinition(LootTableSO bossTable)
        {
            var def = AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>($"{EnemyDir}/Goblin_Warchief.asset");
            if (def == null) { Debug.LogWarning("[Phase2] Goblin_Warchief.asset not found — boss will use component defaults."); return; }
            def.tier = EnemyTier.NamedElite;
            def.baseStats = new CharacterStats(22, 10, 45, 5); // MaxHP = 100 + 45*10 = 550
            def.baseDamage = 16;
            def.moveSpeed = 3.2f;
            def.aggroRange = 12f;
            def.attackRange = 2.4f;
            def.appliesPoison = true;
            def.poisonChance = 0.5f;
            def.poisonDamage = 12;
            def.poisonDuration = 6f;
            def.lootTable = bossTable;
            EditorUtility.SetDirty(def);
            AssetDatabase.SaveAssets();
            Debug.Log("[Phase2] Promoted goblin_warchief to boss stats (MaxHP 550).");
        }

        // ── Set-piece boss in the arena ──
        private static void PlaceBoss(LootTableSO bossTable)
        {
            var def = AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>($"{EnemyDir}/Goblin_Warchief.asset");

            var existing = GameObject.Find(BossName);
            if (existing != null) Object.DestroyImmediate(existing);

            var go = new GameObject(BossName);
            go.transform.position = ArenaPos;

            var cc = go.AddComponent<CharacterController>();
            cc.radius = 0.5f; cc.height = 2.4f; cc.center = new Vector3(0f, 1.2f, 0f);

            go.AddComponent<StatsComponent>();
            go.AddComponent<Health>();
            go.AddComponent<CharacterAnimation>();

            var boss = go.AddComponent<GoblinWarchiefBoss>();
            if (def != null) boss.SetDefinition(def);

            var dropper = go.AddComponent<LootDropper>();
            dropper.SetLootTable(bossTable, EnemyTier.NamedElite);

            AttachWarchiefModel(go);

            Debug.Log($"[Phase2] Placed '{BossName}' at {ArenaPos}.");
        }

        // Rigged Warchief model as a "Model" child (mirrors CharacterModelSwap:
        // -Z-facing FBX turned 180°, GoblinAnimator, crimson RareElite skin).
        private static void AttachWarchiefModel(GameObject root)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/Goblin_Warchief.fbx");
            var ctrl = FindAsset<RuntimeAnimatorController>("GoblinAnimator");
            if (fbx == null) { Debug.LogWarning("[Phase2] Goblin_Warchief.fbx not found — boss has no mesh."); return; }

            var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            model.name = "Model";
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            model.transform.localScale = Vector3.one * 1.5f;

            var anim = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
            if (ctrl != null) anim.runtimeAnimatorController = ctrl;
            anim.applyRootMotion = false;

            // Per-slot so the sculpted armour shows (crimson skin + gear palette),
            // not a flat single tint.
            var skin = FindAsset<Material>("GoblinSkin_RareElite");
            var smr = model.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null)
            {
                var slots = smr.sharedMaterials;
                var mats = new Material[slots.Length];
                for (int i = 0; i < slots.Length; i++)
                {
                    string n = slots[i] != null ? slots[i].name : "";
                    mats[i] = n.Contains("Cloth") ? FindAsset<Material>("GoblinCloth")
                        : n.Contains("Dark") ? FindAsset<Material>("GoblinDark")
                        : n.Contains("Gold") ? FindAsset<Material>("GoblinGold")
                        : n.Contains("Gem")  ? FindAsset<Material>("GoblinGem")
                        : n.Contains("Bone") ? FindAsset<Material>("GoblinBone")
                        : skin;
                }
                smr.sharedMaterials = mats;
            }
        }

        // The HUDCanvas is persisted from Homestead (DontDestroyOnLoad), so it isn't
        // in the Ashfields scene file — add the boss bar to Homestead's copy, then
        // reopen Ashfields. Saves the Ashfields boss placement first so it isn't lost.
        private static void AddBossHealthBar()
        {
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            EditorSceneManager.OpenScene("Assets/Scenes/Homestead.unity");
            var hud = GameObject.Find("HUDCanvas");
            if (hud == null) Debug.LogWarning("[Phase2] HUDCanvas not found in Homestead — boss bar not added.");
            else
            {
                if (hud.GetComponent<BossHealthBarUI>() == null) hud.AddComponent<BossHealthBarUI>();
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }
            EditorSceneManager.OpenScene(ScenePath);
        }

        // ── helpers ──
        private static void EnsureDir(string dir)
        {
            if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) { a = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(a, path); }
            return a;
        }

        private static T FindAsset<T>(string name) where T : Object
        {
            foreach (var guid in AssetDatabase.FindAssets($"{name} t:{typeof(T).Name}"))
            {
                var a = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (a != null) return a;
            }
            return null;
        }
    }
}
#endif
