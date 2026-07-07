#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoidBound.Data;
using VoidBound.Skilling;

namespace VoidBound.Editor
{
    // Woodcutting slice (GDD §5.2): 9 axe-gated logs + choppable tree nodes.
    // Logs are the primary material Crafting turns into tools (wired as the tool
    // recipe cost in CraftingContentSetup). Mirrors Fishing/Mining. Idempotent.
    public static class WoodcuttingContentSetup
    {
        private const string LogDir = "Assets/ScriptableObjects/Materials/Logs";

        // index = rank (Common..Void)
        private static readonly string[] LogNames =
        {
            "Kindling", "Pinewood", "Oak", "Birch", "Ashwood",
            "Yew", "Blackwood", "Radiantwood", "Voidwood",
        };

        // A small choppable grove in the open north-central woodland — clear of the
        // (spread-out) buildings, blending with the decorative tree scatter.
        private static readonly Vector3[] TreeSpots =
        {
            new Vector3(-8f, 0f, 11f), new Vector3(-11f, 0f, 8f), new Vector3(-5f, 0f, 14f),
        };

        [MenuItem("VoidBound/Setup Woodcutting Content")]
        public static void Run()
        {
            EnsureFolder(LogDir);
            for (int i = 0; i < LogNames.Length; i++)
                CreateLog("log_" + i, LogNames[i], (RarityTier)i);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Homestead.unity");
            for (int i = 0; i < TreeSpots.Length; i++)
                WireOrCreateTree("Chop Tree " + (i + 1), TreeSpots[i]);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Woodcutting] 9 logs + " + TreeSpots.Length + " choppable trees created.");
        }

        public static void RunFromBatch() => Run();

        private static void CreateLog(string id, string name, RarityTier tier)
        {
            string path = $"{LogDir}/{id}.asset";
            var m = AssetDatabase.LoadAssetAtPath<MaterialItemSO>(path);
            if (m == null) { m = ScriptableObject.CreateInstance<MaterialItemSO>(); AssetDatabase.CreateAsset(m, path); }
            m.itemId = id; m.displayName = name; m.tier = tier; m.goldValue = 3;
            EditorUtility.SetDirty(m);
        }

        private static void WireOrCreateTree(string name, Vector3 pos)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/Props/Tree.fbx");
                if (model != null) { go = (GameObject)PrefabUtility.InstantiatePrefab(model); go.name = name; }
                else { go = GameObject.CreatePrimitive(PrimitiveType.Capsule); go.name = name; }
            }
            go.transform.position = pos;
            RestyleTree(go); // give it the foliage materials (raw Tree.fbx renders white)

            if (go.GetComponent<Collider>() == null)
            {
                var sc = go.AddComponent<SphereCollider>();
                sc.radius = 1.4f;
                sc.isTrigger = true;
            }

            var node = go.GetComponent<ResourceNode>() ?? go.AddComponent<ResourceNode>();
            var so = new SerializedObject(node);
            var arr = so.FindProperty("tieredMaterials");
            arr.arraySize = LogNames.Length;
            for (int i = 0; i < LogNames.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<MaterialItemSO>($"{LogDir}/log_{i}.asset");
            so.FindProperty("gatherSkill").enumValueIndex = (int)SkillType.Woodcutting;
            so.FindProperty("gatherQuantity").intValue = 1;
            so.FindProperty("respawnTime").floatValue = 4f;
            so.FindProperty("interactRange").floatValue = 2.5f;
            so.FindProperty("interactPrompt").stringValue = "Chop";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // The raw Tree.fbx ships with unassigned (white) materials; the decorative
        // trees get remapped in EnvironmentDressing. Do the same here by slot name
        // so a choppable tree matches the scenery (Leaf/Wood from Art/Materials/Env).
        private static void RestyleTree(GameObject go)
        {
            const string envDir = "Assets/Art/Materials/Env";
            var fallback = AssetDatabase.LoadAssetAtPath<Material>($"{envDir}/Leaf.mat");
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
            {
                var slots = r.sharedMaterials;
                var swapped = new Material[slots.Length];
                for (int i = 0; i < slots.Length; i++)
                {
                    string n = slots[i] != null ? slots[i].name : "Leaf";
                    swapped[i] = AssetDatabase.LoadAssetAtPath<Material>($"{envDir}/{n}.mat") ?? fallback;
                }
                r.sharedMaterials = swapped;
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
