#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoidBound.Data;
using VoidBound.Skilling;

namespace VoidBound.Editor
{
    // Fishing vertical slice (GDD §5.1): wires the lake fishing spots to yield
    // the 9 raw fish, gated by the player's rod tier (PlayerTools → Fishing).
    // With the placeholder Common rod only Minnow bites; better rods (from the
    // Crafting slice) unlock rarer fish. Idempotent.
    public static class FishingContentSetup
    {
        private const string MatDir = "Assets/ScriptableObjects/Materials/Fish";
        private static readonly string[] Ids =
        {
            "raw_minnow", "raw_sardine", "raw_trout", "raw_bass", "raw_pike",
            "raw_salmon", "raw_obsidian_eel", "raw_radiant_koi", "raw_voidfin",
        };

        [MenuItem("VoidBound/Setup Fishing Content")]
        public static void Run()
        {
            var fish = new MaterialItemSO[Ids.Length];
            for (int i = 0; i < Ids.Length; i++)
                fish[i] = AssetDatabase.LoadAssetAtPath<MaterialItemSO>($"{MatDir}/{Ids[i]}.asset");

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Homestead.unity");

            var lake = GameObject.Find("Lake");
            Vector3 c = lake != null ? lake.transform.position : new Vector3(18f, 0f, 15f);

            WireOrCreateSpot("Fishing Spot", c + new Vector3(-3.5f, 0f, -1.5f), fish);
            WireOrCreateSpot("Fishing Spot 2", c + new Vector3(2.5f, 0f, -3f), fish);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Fishing] Wired 2 tiered fishing spots at the lake.");
        }

        public static void RunFromBatch() => Run();

        private static void WireOrCreateSpot(string name, Vector3 pos, MaterialItemSO[] fish)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Art/Models/ResourceNode_FishSpot.fbx");
                if (model != null) { go = (GameObject)PrefabUtility.InstantiatePrefab(model); go.name = name; }
                else { go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name = name; go.transform.localScale = Vector3.one * 0.6f; }
            }
            go.transform.position = pos;

            if (go.GetComponent<Collider>() == null)
            {
                var sc = go.AddComponent<SphereCollider>();
                sc.radius = 1.2f;
                sc.isTrigger = true;
            }

            if (go.GetComponent<VoidBound.Homestead.FishingSpotEffect>() == null)
                go.AddComponent<VoidBound.Homestead.FishingSpotEffect>();

            var node = go.GetComponent<ResourceNode>() ?? go.AddComponent<ResourceNode>();
            var so = new SerializedObject(node);
            var arr = so.FindProperty("tieredMaterials");
            arr.arraySize = fish.Length;
            for (int i = 0; i < fish.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = fish[i];
            so.FindProperty("gatherSkill").enumValueIndex = (int)SkillType.Fishing;
            so.FindProperty("gatherQuantity").intValue = 1;
            so.FindProperty("respawnTime").floatValue = 4f;
            so.FindProperty("interactRange").floatValue = 2.5f;
            so.FindProperty("interactPrompt").stringValue = "Fish";
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
