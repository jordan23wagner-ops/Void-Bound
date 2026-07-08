#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoidBound.Data;
using VoidBound.Skilling;

namespace VoidBound.Editor
{
    // Zone resources (GDD §5.4): ore is mined in the zones, not in town. Places
    // pickaxe-gated mining nodes in Ashfields, tiered with the 9 ores so the
    // player's pickaxe tier caps which rank drops (same as the home tree grove).
    // Mirrors SmithingContentSetup's ore wiring + WoodcuttingContentSetup's
    // material restyle (raw prop FBX renders white otherwise). Idempotent.
    public static class AshfieldsResourcesSetup
    {
        private const string OreDir = "Assets/ScriptableObjects/Materials/Ore";
        private const string EnvDir = "Assets/Art/Materials/Env";
        private const string ScenePath = "Assets/Scenes/Ashfields.unity";

        // rank 0..8 = Common..Void
        private static readonly string[] OreIds =
        {
            "ore_copper", "ore_tin", "ore_iron", "ore_silver", "ore_gold",
            "ore_mithril", "ore_obsidian", "ore_radiant", "ore_void",
        };

        // Spread across the ~±20 ground, clear of the spawn/portal (0,-5..-2.5)
        // and the four goblin encounters at (4,3)/(-6,5)/(8,-3)/(-8,-7). Tunable.
        private static readonly Vector3[] OrePositions =
        {
            new Vector3(-12f, 0f,  9f),
            new Vector3( 13f, 0f,  8f),
            new Vector3(-15f, 0f, -4f),
            new Vector3(  9f, 0f, -13f),
            new Vector3( 15f, 0f,  3f),
        };

        [MenuItem("VoidBound/Setup Ashfields Resources")]
        public static void Run()
        {
            var ores = new MaterialItemSO[OreIds.Length];
            for (int i = 0; i < OreIds.Length; i++)
                ores[i] = AssetDatabase.LoadAssetAtPath<MaterialItemSO>($"{OreDir}/{OreIds[i]}.asset");
            if (System.Array.Exists(ores, o => o == null))
            {
                Debug.LogWarning("[AshfieldsResources] Ore materials not found — run Setup Smithing Content first.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath);
            for (int i = 0; i < OrePositions.Length; i++)
                WireOrCreateOreNode("Ore Node " + (i + 1), OrePositions[i], ores);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[AshfieldsResources] {OrePositions.Length} pickaxe-gated ore nodes placed in Ashfields.");
        }

        public static void RunFromBatch() => Run();

        private static void WireOrCreateOreNode(string name, Vector3 pos, MaterialItemSO[] ores)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/Props/Boulder.fbx");
                if (model != null) { go = (GameObject)PrefabUtility.InstantiatePrefab(model); go.name = name; }
                else { go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; }
            }
            go.transform.position = pos;
            RestyleRock(go); // raw Boulder.fbx renders white; give it the Env stone materials

            if (go.GetComponent<Collider>() == null)
            {
                var sc = go.AddComponent<SphereCollider>();
                sc.radius = 1.4f;
                sc.isTrigger = true;
            }

            var node = go.GetComponent<ResourceNode>() ?? go.AddComponent<ResourceNode>();
            var so = new SerializedObject(node);
            var arr = so.FindProperty("tieredMaterials");
            arr.arraySize = ores.Length;
            for (int i = 0; i < ores.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = ores[i];
            so.FindProperty("gatherSkill").enumValueIndex = (int)SkillType.Mining;
            so.FindProperty("gatherQuantity").intValue = 1;
            so.FindProperty("respawnTime").floatValue = 4f;
            so.FindProperty("interactRange").floatValue = 2.5f;
            so.FindProperty("interactPrompt").stringValue = "Mine";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RestyleRock(GameObject go)
        {
            var fallback = AssetDatabase.LoadAssetAtPath<Material>($"{EnvDir}/Stone.mat");
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
            {
                var slots = r.sharedMaterials;
                var swapped = new Material[slots.Length];
                for (int i = 0; i < slots.Length; i++)
                {
                    string n = slots[i] != null ? slots[i].name : "Stone";
                    swapped[i] = AssetDatabase.LoadAssetAtPath<Material>($"{EnvDir}/{n}.mat") ?? fallback;
                }
                r.sharedMaterials = swapped;
            }
        }
    }
}
#endif
