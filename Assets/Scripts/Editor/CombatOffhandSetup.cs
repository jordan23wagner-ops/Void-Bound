#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoidBound.Data;
using VoidBound.Skilling;

namespace VoidBound.Editor
{
    // Ammo-saver offhands (GDD §5.6): a Quiver (ranger) and Mage's Book (mage) —
    // untradable offhand items that give a tier-scaled chance to fire without
    // spending ammo. Crafted at the Crafting Bench. Placeholder visuals reuse the
    // shield mesh for now. Idempotent.
    public static class CombatOffhandSetup
    {
        private const string GearDir = "Assets/ScriptableObjects/Gear/Offhand";
        private const string RecDir = "Assets/ScriptableObjects/Recipes/Crafting";
        private const string ShieldSrc = "Assets/ScriptableObjects/Gear/wooden_shield.asset";

        [MenuItem("VoidBound/Setup Combat Offhands")]
        public static void Run()
        {
            EnsureFolder(GearDir);

            var quiver = CopyOffhand(GearDir + "/quiver.asset", "quiver", "Quiver");
            var book = CopyOffhand(GearDir + "/mages_book.asset", "mages_book", "Mage's Book");
            CreateGearRecipe("craft_quiver", "Craft Quiver", quiver);
            CreateGearRecipe("craft_mages_book", "Bind Mage's Book", book);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Homestead.unity");
            AppendBenchRecipes("craft_quiver", "craft_mages_book");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Offhands] Quiver + Mage's Book created and added to the Crafting Bench.");
        }

        public static void RunFromBatch() => Run();

        private static GearItemSO CopyOffhand(string dst, string id, string name)
        {
            if (AssetDatabase.LoadAssetAtPath<GearItemSO>(dst) == null)
                AssetDatabase.CopyAsset(ShieldSrc, dst);
            var g = AssetDatabase.LoadAssetAtPath<GearItemSO>(dst);
            g.itemId = id;
            g.displayName = name;
            g.slot = EquipmentSlot.Shield; // the offhand slot
            g.untradable = true;
            g.ammoSaver = true;
            g.rarity = RarityTier.Uncommon;
            g.goldValue = 0;
            EditorUtility.SetDirty(g);
            return g;
        }

        private static void CreateGearRecipe(string id, string name, GearItemSO gear)
        {
            string path = $"{RecDir}/{id}.asset";
            var r = AssetDatabase.LoadAssetAtPath<RecipeDefinitionSO>(path);
            if (r == null) { r = ScriptableObject.CreateInstance<RecipeDefinitionSO>(); AssetDatabase.CreateAsset(r, path); }
            r.recipeId = id;
            r.displayName = name;
            r.requiredSkill = SkillType.Crafting;
            r.requiredToolTier = RarityTier.Common;
            r.requiredSkillLevel = 0;
            r.requiredStation = "crafting_bench";
            r.ingredients = new RecipeIngredient[0]; // free for now (material costs deferred)
            r.outputType = RecipeOutputType.Gear;
            r.outputGear = gear;
            r.outputMaterial = null; r.outputTool = null;
            r.outputQuantity = 1; r.xpReward = 0;
            EditorUtility.SetDirty(r);
        }

        private static void AppendBenchRecipes(params string[] ids)
        {
            var bench = GameObject.Find("Crafting Bench");
            var cs = bench != null ? bench.GetComponent<CraftingStation>() : null;
            if (cs == null) { Debug.LogWarning("[Offhands] Crafting Bench not found."); return; }

            var have = new HashSet<string>();
            foreach (var r in cs.AvailableRecipes) if (r != null) have.Add(r.recipeId);

            var toAdd = new List<RecipeDefinitionSO>();
            foreach (var id in ids)
                if (!have.Contains(id))
                {
                    var r = AssetDatabase.LoadAssetAtPath<RecipeDefinitionSO>($"{RecDir}/{id}.asset");
                    if (r != null) toAdd.Add(r);
                }
            if (toAdd.Count == 0) return;

            var so = new SerializedObject(cs);
            var arr = so.FindProperty("availableRecipes");
            int start = arr.arraySize;
            arr.arraySize = start + toAdd.Count;
            for (int i = 0; i < toAdd.Count; i++)
                arr.GetArrayElementAtIndex(start + i).objectReferenceValue = toAdd[i];
            so.ApplyModifiedPropertiesWithoutUndo();
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
