#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoidBound.Combat;
using VoidBound.Data;
using VoidBound.Inventory;
using VoidBound.Skilling;
using VoidBound.UI;

namespace VoidBound.Editor
{
    // Cooking vertical slice (GDD §5.1): 9 raw fish + 9 cooked-fish HoT foods +
    // 9 cook recipes (tool-gated, ungated at Common), wired to the Campfire.
    // Adds the player's food/HoT/tool components and a HUD "Eat Food" button,
    // and repoints the Fishing Spot to raw Minnow so the loop is playable.
    // Idempotent.
    public static class CookingContentSetup
    {
        private const string MatDir = "Assets/ScriptableObjects/Materials/Fish";
        private const string RecDir = "Assets/ScriptableObjects/Recipes/Cooking";

        // id, display name, heal-over-time total (5 → 21, +2 per rank)
        private static readonly (string id, string name, int hot)[] Fish =
        {
            ("minnow", "Minnow", 5), ("sardine", "Sardine", 7), ("trout", "Trout", 9),
            ("bass", "Bass", 11), ("pike", "Pike", 13), ("salmon", "Salmon", 15),
            ("obsidian_eel", "Obsidian Eel", 17), ("radiant_koi", "Radiant Koi", 19),
            ("voidfin", "Voidfin", 21),
        };

        [MenuItem("VoidBound/Setup Cooking Content")]
        public static void Run()
        {
            EnsureFolder(MatDir);
            EnsureFolder(RecDir);

            for (int i = 0; i < Fish.Length; i++)
            {
                var f = Fish[i];
                var tier = (RarityTier)i;
                var raw = CreateMat("raw_" + f.id, "Raw " + f.name, tier, false, 0, 3);
                var cooked = CreateMat("cooked_" + f.id, f.name, tier, true, f.hot, 5 + i);
                CreateRecipe("cook_" + f.id, "Cook " + f.name, raw, cooked);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(); // ensure the new assets are imported before we reference them

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Homestead.unity");
            WireCampfire();
            RepointFishingSpot();
            AddPlayerComponents();
            AddConsumablesHUD();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[Cooking] 9 fish + recipes created; Campfire, Fishing Spot, player & HUD wired.");
        }

        public static void RunFromBatch() => Run();

        // ── asset creation ─────────────────────────────────────────────

        private static MaterialItemSO CreateMat(string id, string name, RarityTier tier,
            bool consumable, int hot, int gold)
        {
            string path = $"{MatDir}/{id}.asset";
            var m = AssetDatabase.LoadAssetAtPath<MaterialItemSO>(path);
            if (m == null) { m = ScriptableObject.CreateInstance<MaterialItemSO>(); AssetDatabase.CreateAsset(m, path); }
            m.itemId = id;
            m.displayName = name;
            m.tier = tier;
            m.isConsumable = consumable;
            m.healOverTime = hot;
            m.hotDuration = 8f;
            m.goldValue = gold;
            EditorUtility.SetDirty(m);
            return m;
        }

        private static RecipeDefinitionSO CreateRecipe(string id, string name,
            MaterialItemSO raw, MaterialItemSO cooked)
        {
            string path = $"{RecDir}/{id}.asset";
            var r = AssetDatabase.LoadAssetAtPath<RecipeDefinitionSO>(path);
            if (r == null) { r = ScriptableObject.CreateInstance<RecipeDefinitionSO>(); AssetDatabase.CreateAsset(r, path); }
            r.recipeId = id;
            r.displayName = name;
            r.requiredSkill = SkillType.Cooking;
            r.requiredToolTier = RarityTier.Common; // cooking gated by having the fish, not a tool
            r.requiredSkillLevel = 0;
            r.requiredStation = "Campfire";
            r.ingredients = new[] { new RecipeIngredient { material = raw, quantity = 1 } };
            r.outputType = RecipeOutputType.Material;
            r.outputMaterial = cooked;
            r.outputGear = null;
            r.outputQuantity = 1;
            r.xpReward = 0;
            EditorUtility.SetDirty(r);
            return r;
        }

        // ── scene wiring ───────────────────────────────────────────────

        private static void WireCampfire()
        {
            var campfire = GameObject.Find("Campfire");
            var cs = campfire != null ? campfire.GetComponent<CraftingStation>() : null;
            if (cs == null) { Debug.LogWarning("[Cooking] Campfire CraftingStation not found."); return; }

            // Load the recipes back from disk so the scene stores valid asset
            // references (in-memory refs from before OpenScene serialize as null).
            var so = new SerializedObject(cs);
            var arr = so.FindProperty("availableRecipes");
            arr.arraySize = Fish.Length;
            for (int i = 0; i < Fish.Length; i++)
            {
                var recipe = AssetDatabase.LoadAssetAtPath<RecipeDefinitionSO>(
                    $"{RecDir}/cook_{Fish[i].id}.asset");
                arr.GetArrayElementAtIndex(i).objectReferenceValue = recipe;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RepointFishingSpot()
        {
            var spot = GameObject.Find("Fishing Spot");
            var node = spot != null ? spot.GetComponent<ResourceNode>() : null;
            if (node == null) return;
            var raw = AssetDatabase.LoadAssetAtPath<MaterialItemSO>($"{MatDir}/raw_minnow.asset");
            if (raw == null) return;
            var so = new SerializedObject(node);
            so.FindProperty("gatherMaterial").objectReferenceValue = raw;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddPlayerComponents()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            if (player.GetComponent<PlayerTools>() == null) player.AddComponent<PlayerTools>();
            if (player.GetComponent<HealOverTime>() == null) player.AddComponent<HealOverTime>();
            if (player.GetComponent<FoodConsumer>() == null) player.AddComponent<FoodConsumer>();
        }

        private static void AddConsumablesHUD()
        {
            var hud = Object.FindAnyObjectByType<HUDManager>();
            if (hud != null && hud.GetComponent<ConsumablesHUD>() == null)
                hud.gameObject.AddComponent<ConsumablesHUD>();
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
