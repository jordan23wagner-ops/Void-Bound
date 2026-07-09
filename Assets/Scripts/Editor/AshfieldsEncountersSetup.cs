#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoidBound.Combat;
using VoidBound.Data;

namespace VoidBound.Editor
{
    // Gives Ashfields a reason to exist beyond mining: a respawning enemy
    // encounter loop. Phase7AshfieldsSetup places 4 static goblins that clear
    // once; this adds EnemySpawner points that keep a small pocket of foes alive
    // so the zone stays a fight (enemies chase, PlayerCombat kills them,
    // LootDropper drops gold/shards/gear on death — all existing systems).
    //
    // Matches AshfieldsResourcesSetup's conventions: opens/saves Ashfields.unity
    // via EditorSceneManager, idempotent by rebuilding under one named root
    // ("Ashfields Encounters") each run, positions chosen inside the ~±20 ground
    // and clear of the spawn/portal (0,-5..-2.5) and the five ore nodes.
    public static class AshfieldsEncountersSetup
    {
        private const string ScenePath = "Assets/Scenes/Ashfields.unity";
        private const string EnemyDir = "Assets/ScriptableObjects/Enemies";
        private const string LootDir = "Assets/ScriptableObjects/LootTables";
        private const string PlaceholderMesh = "Assets/Art/Models/EnemyPlaceholder.fbx";
        private const string RootName = "Ashfields Encounters";

        // Reuses the four goblin-encounter coordinates already documented in
        // AshfieldsResourcesSetup (kept clear of ore nodes + spawn/portal), plus
        // one extra pack. Each entry: label, enemy def id, loot table, tier,
        // pack size. Weak scouts spawn in pairs; heavier warriors solo/pair.
        private struct SpawnerSpec
        {
            public string Label;
            public string EnemyId;
            public string LootId;
            public EnemyTier Tier;
            public int MaxAlive;
            public Vector3 Position;
        }

        private static readonly SpawnerSpec[] Specs =
        {
            new SpawnerSpec { Label = "Scout Camp",   EnemyId = "Goblin_Scout",   LootId = "WeakLoot",     Tier = EnemyTier.Weak,     MaxAlive = 2, Position = new Vector3( 4f, 0.1f,  3f) },
            new SpawnerSpec { Label = "Scout Patrol",  EnemyId = "Goblin_Scout",   LootId = "WeakLoot",     Tier = EnemyTier.Weak,     MaxAlive = 2, Position = new Vector3(-6f, 0.1f,  5f) },
            new SpawnerSpec { Label = "Warrior Post",  EnemyId = "Goblin_Warrior", LootId = "StandardLoot", Tier = EnemyTier.Standard, MaxAlive = 1, Position = new Vector3( 8f, 0.1f, -3f) },
            new SpawnerSpec { Label = "War Band",      EnemyId = "Goblin_Warrior", LootId = "StandardLoot", Tier = EnemyTier.Standard, MaxAlive = 2, Position = new Vector3(-8f, 0.1f, -7f) },
        };

        [MenuItem("VoidBound/Setup Ashfields Encounters")]
        public static void Run()
        {
            var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/GoblinAnimator.controller");
            if (ctrl == null)
                Debug.LogWarning("[AshfieldsEncounters] GoblinAnimator.controller not found — spawned goblins won't animate.");

            var scene = EditorSceneManager.OpenScene(ScenePath);

            // Idempotent: nuke and rebuild the named root each run.
            var existing = GameObject.Find(RootName);
            if (existing != null) Object.DestroyImmediate(existing);
            var root = new GameObject(RootName);

            int made = 0;
            foreach (var spec in Specs)
            {
                var def = AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>($"{EnemyDir}/{spec.EnemyId}.asset");
                var loot = AssetDatabase.LoadAssetAtPath<LootTableSO>($"{LootDir}/{spec.LootId}.asset");
                if (def == null || loot == null)
                {
                    Debug.LogWarning($"[AshfieldsEncounters] Missing def/loot for '{spec.Label}' " +
                                     $"({spec.EnemyId}/{spec.LootId}) — skipped.");
                    continue;
                }

                var go = new GameObject($"Spawner - {spec.Label}");
                go.transform.SetParent(root.transform, false);
                go.transform.position = spec.Position;

                var spawner = go.AddComponent<EnemySpawner>();
                var so = new SerializedObject(spawner);
                so.FindProperty("definition").objectReferenceValue = def;
                so.FindProperty("lootTable").objectReferenceValue = loot;
                so.FindProperty("tier").enumValueIndex = (int)spec.Tier;
                // Rigged model so spawned goblins match the placed enemies + boss,
                // with per-slot materials so the sculpted armour renders in colour.
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/Models/{spec.EnemyId}.fbx");
                so.FindProperty("modelFbx").objectReferenceValue = fbx;
                so.FindProperty("animatorController").objectReferenceValue = ctrl;
                var mats = BuildGoblinSlotMaterials(fbx, spec.Tier);
                var arr = so.FindProperty("slotMaterials");
                arr.arraySize = mats.Length;
                for (int i = 0; i < mats.Length; i++)
                    arr.GetArrayElementAtIndex(i).objectReferenceValue = mats[i];
                so.FindProperty("modelScale").floatValue = ScaleFor(spec.Tier);
                so.FindProperty("maxAlive").intValue = spec.MaxAlive;
                so.ApplyModifiedPropertiesWithoutUndo();
                made++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[AshfieldsEncounters] {made} enemy spawners placed under '{RootName}' in Ashfields.");
        }

        public static void RunFromBatch() => Run();

        // Ordered per-submesh materials for the goblin FBX: baked-gear slots get their
        // palette material, skin slots get the tier skin (mirrors CharacterModelSwap).
        private static Material[] BuildGoblinSlotMaterials(GameObject fbx, EnemyTier tier)
        {
            if (fbx == null) return System.Array.Empty<Material>();
            var smr = fbx.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr == null) return System.Array.Empty<Material>();
            var skin = SkinFor(tier);
            var slots = smr.sharedMaterials;
            var result = new Material[slots.Length];
            for (int i = 0; i < slots.Length; i++)
                result[i] = MapGoblinSlot(slots[i] != null ? slots[i].name : "", skin);
            return result;
        }

        private static Material MapGoblinSlot(string slot, Material skin)
        {
            if (slot.Contains("Cloth")) return Mat("GoblinCloth");
            if (slot.Contains("Dark"))  return Mat("GoblinDark");
            if (slot.Contains("Gold"))  return Mat("GoblinGold");
            if (slot.Contains("Gem"))   return Mat("GoblinGem");
            if (slot.Contains("Bone"))  return Mat("GoblinBone");
            return skin;
        }

        private static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>($"Assets/Art/Materials/{name}.mat");

        // Per-tier skin + world scale, matching CharacterModelSwap's goblin variants.
        private static Material SkinFor(EnemyTier tier)
        {
            string name = tier switch
            {
                EnemyTier.Weak     => "GoblinSkin_Weak",
                EnemyTier.Standard => "GoblinSkin_Standard",
                EnemyTier.Elite    => "GoblinSkin_Elite",
                _                  => "GoblinSkin_RareElite",
            };
            return AssetDatabase.LoadAssetAtPath<Material>($"Assets/Art/Materials/{name}.mat");
        }

        private static float ScaleFor(EnemyTier tier) => tier switch
        {
            EnemyTier.Weak     => 0.92f,
            EnemyTier.Standard => 1.00f,
            EnemyTier.Elite    => 1.18f,
            _                  => 1.50f,
        };
    }
}
#endif
