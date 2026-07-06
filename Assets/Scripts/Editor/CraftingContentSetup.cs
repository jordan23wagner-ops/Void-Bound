#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoidBound.Data;
using VoidBound.Skilling;

namespace VoidBound.Editor
{
    // Crafting Bench slice (GDD §5.2/§5.5): the 4×9 tool ladder + basic ammo.
    // Crafting a tool raises the player's tool tier for its skill (which gates
    // gathering), so each recipe is gated by the PREVIOUS tier of the same
    // line — a climbable ladder. Ammo (Arrows/Runes) craft as stackable items.
    // Material costs are deferred until the resource slices (logs/ore/flora)
    // land; for now the ladder is the gate. Idempotent.
    public static class CraftingContentSetup
    {
        private const string ToolDir = "Assets/ScriptableObjects/Tools";
        private const string RecDir = "Assets/ScriptableObjects/Recipes/Crafting";
        private const string AmmoDir = "Assets/ScriptableObjects/Materials/Ammo";

        private static readonly string[] Runes = { "fire", "water", "air", "earth" };
        private static string Cap(string s) => char.ToUpper(s[0]) + s.Substring(1);

        private struct Line { public SkillType skill; public string[] names; }

        private static readonly Line[] Lines =
        {
            new Line { skill = SkillType.Woodcutting, names = new[] {
                "Flimsy Axe","Woodcutting Axe","Hunter's Axe","Master Woodcutting Axe","Magic Axe",
                "Enchanted Woodcutting Axe","Obsidian Woodcutting Axe","Radiant Woodcutting Axe","Void Axe" } },
            new Line { skill = SkillType.Fishing, names = new[] {
                "Old Fishing Rod","Fishing Rod","Expert's Fishing Rod","Master Fishing Pole","Magic Fishing Pole",
                "Enchanted Fishing Rod","Obsidian Rod","Radiant Rod","Void Pole" } },
            new Line { skill = SkillType.Mining, names = new[] {
                "Flimsy Pickaxe","Pickaxe","Miner's Pickaxe","Master Pickaxe","Magic Pickaxe",
                "Enchanted Pickaxe","Obsidian Pickaxe","Radiant Pickaxe","Void Pickaxe" } },
            new Line { skill = SkillType.Gathering, names = new[] {
                "Flimsy Sickle","Sickle","Herbalist's Sickle","Master Sickle","Magic Sickle",
                "Enchanted Sickle","Obsidian Sickle","Radiant Sickle","Void Sickle" } },
        };

        [MenuItem("VoidBound/Setup Crafting Content")]
        public static void Run()
        {
            EnsureFolder(ToolDir);
            EnsureFolder(RecDir);
            EnsureFolder(AmmoDir);

            foreach (var line in Lines)
            {
                string sk = line.skill.ToString().ToLower();
                for (int t = 0; t < line.names.Length; t++)
                {
                    var tool = CreateTool($"tool_{sk}_{t}", line.names[t], line.skill, (RarityTier)t);
                    CreateToolRecipe($"craft_{sk}_{t}", "Craft " + line.names[t], line.skill, tool, t);
                }
            }
            CreateAmmoRecipe("craft_arrows", "Fletch Arrows", CreateAmmo("arrows", "Arrows"), 10);
            foreach (var e in Runes)
                CreateAmmoRecipe("craft_rune_" + e, "Inscribe " + Cap(e) + " Runes",
                    CreateAmmo("rune_" + e, Cap(e) + " Rune"), 10);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Homestead.unity");
            WireBench();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Crafting] 36 tools + ammo created and wired to the Crafting Bench.");
        }

        public static void RunFromBatch() => Run();

        // ── asset creation ─────────────────────────────────────────────

        private static ToolItemSO CreateTool(string id, string name, SkillType skill, RarityTier tier)
        {
            string path = $"{ToolDir}/{id}.asset";
            var t = AssetDatabase.LoadAssetAtPath<ToolItemSO>(path);
            if (t == null) { t = ScriptableObject.CreateInstance<ToolItemSO>(); AssetDatabase.CreateAsset(t, path); }
            t.itemId = id; t.displayName = name; t.skill = skill; t.tier = tier;
            EditorUtility.SetDirty(t);
            return t;
        }

        private static void CreateToolRecipe(string id, string name, SkillType skill, ToolItemSO tool, int tier)
        {
            string path = $"{RecDir}/{id}.asset";
            var r = AssetDatabase.LoadAssetAtPath<RecipeDefinitionSO>(path);
            if (r == null) { r = ScriptableObject.CreateInstance<RecipeDefinitionSO>(); AssetDatabase.CreateAsset(r, path); }
            r.recipeId = id;
            r.displayName = name;
            r.requiredSkill = skill;                          // gate uses this skill's tool tier
            r.requiredToolTier = (RarityTier)Mathf.Max(0, tier - 1); // need the previous tool
            r.requiredSkillLevel = 0;
            r.requiredStation = "crafting_bench";
            // Each tool tier costs logs of the previous rank — gathered with the
            // tool you already have (the Woodcutting bootstrap). Common = starter.
            if (tier >= 1)
            {
                var log = AssetDatabase.LoadAssetAtPath<MaterialItemSO>(
                    $"Assets/ScriptableObjects/Materials/Logs/log_{tier - 1}.asset");
                r.ingredients = log != null
                    ? new[] { new RecipeIngredient { material = log, quantity = 2 } }
                    : new RecipeIngredient[0];
            }
            else r.ingredients = new RecipeIngredient[0];
            r.outputType = RecipeOutputType.Tool;
            r.outputTool = tool;
            r.outputMaterial = null;
            r.outputGear = null;
            r.outputQuantity = 1;
            r.xpReward = 0;
            EditorUtility.SetDirty(r);
        }

        private static MaterialItemSO CreateAmmo(string id, string name)
        {
            string path = $"{AmmoDir}/{id}.asset";
            var m = AssetDatabase.LoadAssetAtPath<MaterialItemSO>(path);
            if (m == null) { m = ScriptableObject.CreateInstance<MaterialItemSO>(); AssetDatabase.CreateAsset(m, path); }
            m.itemId = id; m.displayName = name; m.tier = RarityTier.Common; m.goldValue = 1;
            EditorUtility.SetDirty(m);
            return m;
        }

        private static void CreateAmmoRecipe(string id, string name, MaterialItemSO ammo, int qty)
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
            r.ingredients = new RecipeIngredient[0];
            r.outputType = RecipeOutputType.Material;
            r.outputMaterial = ammo;
            r.outputTool = null;
            r.outputGear = null;
            r.outputQuantity = qty;
            r.xpReward = 0;
            EditorUtility.SetDirty(r);
        }

        // ── scene wiring (load recipes from disk → valid refs) ──────────

        private static void WireBench()
        {
            var bench = GameObject.Find("Crafting Bench");
            var cs = bench != null ? bench.GetComponent<CraftingStation>() : null;
            if (cs == null) { Debug.LogWarning("[Crafting] Crafting Bench station not found."); return; }

            var ids = new System.Collections.Generic.List<string>();
            foreach (var line in Lines)
            {
                string sk = line.skill.ToString().ToLower();
                for (int t = 0; t < line.names.Length; t++) ids.Add($"craft_{sk}_{t}");
            }
            ids.Add("craft_arrows");
            foreach (var e in Runes) ids.Add("craft_rune_" + e);

            var so = new SerializedObject(cs);
            var arr = so.FindProperty("availableRecipes");
            arr.arraySize = ids.Count;
            for (int i = 0; i < ids.Count; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<RecipeDefinitionSO>($"{RecDir}/{ids[i]}.asset");
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
