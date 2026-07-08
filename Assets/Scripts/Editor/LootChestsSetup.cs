#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoidBound.Combat;
using VoidBound.Data;

namespace VoidBound.Editor
{
    // Wires the T4 tool-head drops into the world and places lootable chests in
    // Ashfields (the entry-zone slice of the reward loop). T4 heads (Sharp Tooth /
    // Dragon Scale) drop from Standard mobs at a low rate and from chests at a
    // better rate — completing the T4 axe/pickaxe recipes. T5-8 heads stay unbound
    // until Bleakwood + tougher enemies land. Idempotent.
    public static class LootChestsSetup
    {
        private const string HeadDir = "Assets/ScriptableObjects/Materials/Heads";
        private const string TableDir = "Assets/ScriptableObjects/LootTables";
        private const string ScenePath = "Assets/Scenes/Ashfields.unity";
        private const string RootName = "Ashfields Chests";

        // Clear of spawners (4,3)/(-6,5)/(8,-3)/(-8,-7), ore nodes and the spawn/portal.
        private static readonly Vector3[] ChestPositions =
        {
            new Vector3(12f, 0f, 12f),
            new Vector3(-13f, 0f, -11f),
            new Vector3(15f, 0f, -6f),
        };

        [MenuItem("VoidBound/Setup Ashfields Loot Chests")]
        public static void Run()
        {
            var tooth4 = Head("head_tooth_4");
            var scale4 = Head("head_scale_4");
            if (tooth4 == null || scale4 == null)
            {
                Debug.LogWarning("[LootChests] T4 head materials not found — run Setup Crafting Content first.");
                return;
            }

            // 1. Dedicated chest table: gold + a good shot at a T4 head.
            var chestTable = CreateOrLoadTable("AshfieldsChestLoot", "Ashfields Chest");
            chestTable.goldMin = 20; chestTable.goldMax = 60;
            chestTable.gearDropChance = 0f;
            chestTable.materialDrops = new[] { Drop(tooth4, 0.5f), Drop(scale4, 0.5f) };
            EditorUtility.SetDirty(chestTable);

            // 2. Low-rate T4 head drops on Standard mobs (Ashfields warriors).
            var std = AssetDatabase.LoadAssetAtPath<LootTableSO>($"{TableDir}/StandardLoot.asset");
            if (std != null)
            {
                std.materialDrops = new[] { Drop(tooth4, 0.12f), Drop(scale4, 0.12f) };
                EditorUtility.SetDirty(std);
            }
            AssetDatabase.SaveAssets();

            // 3. Place the chests in Ashfields. Reload the table from disk AFTER
            // OpenScene — in-memory asset refs from before the scene load serialize
            // as null on scene objects (same gotcha as CookingContentSetup).
            var scene = EditorSceneManager.OpenScene(ScenePath);
            chestTable = AssetDatabase.LoadAssetAtPath<LootTableSO>($"{TableDir}/AshfieldsChestLoot.asset");
            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(RootName).transform;

            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/Buildings/StorageChest.fbx");
            var wood = LoadMat("WoodLight");
            var gold = LoadMat("Gold");
            for (int i = 0; i < ChestPositions.Length; i++)
                PlaceChest(root, fbx, ChestPositions[i], chestTable, wood, gold, i + 1);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[LootChests] {ChestPositions.Length} chests placed in Ashfields; T4 heads wired to chest + Standard mob loot.");
        }

        public static void RunFromBatch() => Run();

        private static void PlaceChest(Transform root, GameObject fbx, Vector3 pos,
            LootTableSO table, Material wood, Material gold, int n)
        {
            GameObject go = fbx != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(fbx)
                : GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Loot Chest " + n;
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, (n * 47) % 360, 0f);

            if (wood != null)
                foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
                {
                    var slots = mr.sharedMaterials;
                    var outM = new Material[slots.Length];
                    for (int j = 0; j < slots.Length; j++)
                    {
                        string nm = slots[j] != null ? slots[j].name : "";
                        outM[j] = (gold != null && nm.Contains("Gold")) ? gold : wood;
                    }
                    mr.sharedMaterials = outM;
                }

            if (go.GetComponent<Collider>() == null)
            {
                var bc = go.AddComponent<BoxCollider>();
                bc.center = new Vector3(0f, 0.5f, 0f);
                bc.size = new Vector3(1.3f, 1f, 1.3f);
                bc.isTrigger = true;
            }

            var chest = go.GetComponent<LootChest>() ?? go.AddComponent<LootChest>();
            var so = new SerializedObject(chest);
            so.FindProperty("lootTable").objectReferenceValue = table;
            so.FindProperty("interactPrompt").stringValue = "Open";
            so.FindProperty("interactRange").floatValue = 2.5f;
            so.FindProperty("tooltipDescription").stringValue = "A chest — open it for loot.";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static MaterialItemSO Head(string id) =>
            AssetDatabase.LoadAssetAtPath<MaterialItemSO>($"{HeadDir}/{id}.asset");

        private static MaterialDrop Drop(MaterialItemSO m, float chance) =>
            new MaterialDrop { material = m, chance = chance, minQuantity = 1, maxQuantity = 1 };

        private static LootTableSO CreateOrLoadTable(string id, string name)
        {
            string path = $"{TableDir}/{id}.asset";
            var t = AssetDatabase.LoadAssetAtPath<LootTableSO>(path);
            if (t == null) { t = ScriptableObject.CreateInstance<LootTableSO>(); AssetDatabase.CreateAsset(t, path); }
            t.tableId = id; t.displayName = name;
            return t;
        }

        private static Material LoadMat(string name) =>
            AssetDatabase.LoadAssetAtPath<Material>($"Assets/Art/Materials/Buildings/{name}.mat");
    }
}
#endif
