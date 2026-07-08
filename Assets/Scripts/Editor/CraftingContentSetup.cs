#if UNITY_EDITOR
using System.Collections.Generic;
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
    // line — a climbable ladder.
    //
    // Tool recipes now cost real materials (logs + ore/bars, plus monster-drop
    // heads at high tiers). Rank maps 0..8 to copper..void. Ore/bar assets are
    // loaded by path and any missing one is skipped (with a warning) so a gap in
    // the resource set never nulls a whole recipe. Idempotent.
    public static class CraftingContentSetup
    {
        private const string ToolDir = "Assets/ScriptableObjects/Tools";
        private const string RecDir = "Assets/ScriptableObjects/Recipes/Crafting";
        private const string AmmoDir = "Assets/ScriptableObjects/Materials/Ammo";
        private const string HeadDir = "Assets/ScriptableObjects/Materials/Heads";
        private const string LogDir = "Assets/ScriptableObjects/Materials/Logs";
        private const string OreDir = "Assets/ScriptableObjects/Materials/Ore";
        private const string BarDir = "Assets/ScriptableObjects/Materials/Bars";

        private static readonly string[] Runes = { "fire", "water", "air", "earth" };
        private static string Cap(string s) => char.ToUpper(s[0]) + s.Substring(1);

        // rank 0..8 → metal suffix, shared by ore_{suffix} and bar_{suffix}
        private static readonly string[] MetalByRank =
        {
            "copper", "tin", "iron", "silver", "gold",
            "mithril", "obsidian", "radiant", "void",
        };

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

        // Monster-drop heads, indexed by tool tier t = 4..8.
        private struct Head { public string id; public string name; }
        private static readonly Dictionary<int, Head> ToothByTier = new Dictionary<int, Head>
        {
            { 4, new Head { id = "head_tooth_4", name = "Sharp Tooth" } },
            { 5, new Head { id = "head_tooth_5", name = "Jagged Fang" } },
            { 6, new Head { id = "head_tooth_6", name = "Obsidian Fang" } },
            { 7, new Head { id = "head_tooth_7", name = "Radiant Fang" } },
            { 8, new Head { id = "head_tooth_8", name = "Void Fang" } },
        };
        private static readonly Dictionary<int, Head> ScaleByTier = new Dictionary<int, Head>
        {
            { 4, new Head { id = "head_scale_4", name = "Dragon Scale" } },
            { 5, new Head { id = "head_scale_5", name = "Wyrm Scale" } },
            { 6, new Head { id = "head_scale_6", name = "Obsidian Scale" } },
            { 7, new Head { id = "head_scale_7", name = "Radiant Scale" } },
            { 8, new Head { id = "head_scale_8", name = "Void Scale" } },
        };

        [MenuItem("VoidBound/Setup Crafting Content")]
        public static void Run()
        {
            EnsureFolder(ToolDir);
            EnsureFolder(RecDir);
            EnsureFolder(AmmoDir);
            EnsureFolder(HeadDir);

            CreateHeads();

            foreach (var line in Lines)
            {
                string sk = line.skill.ToString().ToLower();
                for (int t = 0; t < line.names.Length; t++)
                {
                    var tool = CreateTool($"tool_{sk}_{t}", line.names[t], line.skill, (RarityTier)t);
                    CreateToolRecipe($"craft_{sk}_{t}", "Craft " + line.names[t], line.skill, tool, t);
                }
            }
            CreateAmmoRecipe("craft_arrows", "Fletch Arrows", CreateAmmo("arrows", "Arrows"), 10,
                ArrowIngredients());
            foreach (var e in Runes)
                CreateAmmoRecipe("craft_rune_" + e, "Inscribe " + Cap(e) + " Runes",
                    CreateAmmo("rune_" + e, Cap(e) + " Rune"), 10, new RecipeIngredient[0]);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Homestead.unity");
            WireBench();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Crafting] 36 tools + heads + ammo created and wired to the Crafting Bench.");
        }

        public static void RunFromBatch() => Run();

        // ── material loading helpers ───────────────────────────────────

        private static MaterialItemSO LoadMaterial(string dir, string id)
            => AssetDatabase.LoadAssetAtPath<MaterialItemSO>($"{dir}/{id}.asset");

        // Adds material×qty to the list; skips + warns if the asset is missing so
        // a resource gap never nulls the whole recipe.
        private static void Add(List<RecipeIngredient> list, MaterialItemSO mat, string label, int qty)
        {
            if (mat == null)
            {
                Debug.LogWarning($"[Crafting] Missing material '{label}' — ingredient skipped.");
                return;
            }
            list.Add(new RecipeIngredient { material = mat, quantity = qty });
        }

        // Tool-recipe ingredients for tier t (t>=1). r = t-1 is the material rank.
        private static RecipeIngredient[] ToolIngredients(SkillType skill, int t)
        {
            var list = new List<RecipeIngredient>();
            int r = t - 1;
            string metal = MetalByRank[r];
            int woodQty = t >= 5 ? 3 : 2; // gentle high-tier scaling

            var log = LoadMaterial(LogDir, $"log_{r}");
            Add(list, log, $"log_{r}", woodQty);

            switch (skill)
            {
                case SkillType.Woodcutting: // axe
                    if (t <= 3)
                        Add(list, LoadMaterial(OreDir, $"ore_{metal}"), $"ore_{metal}", 2);
                    else
                    {
                        Add(list, LoadMaterial(BarDir, $"bar_{metal}"), $"bar_{metal}", 2);
                        AddHead(list, ScaleByTier, t);
                    }
                    break;

                case SkillType.Mining: // pickaxe
                    if (t <= 3)
                        Add(list, LoadMaterial(OreDir, $"ore_{metal}"), $"ore_{metal}", 2);
                    else
                    {
                        Add(list, LoadMaterial(BarDir, $"bar_{metal}"), $"bar_{metal}", 2);
                        AddHead(list, ToothByTier, t);
                    }
                    break;

                case SkillType.Fishing: // rod — no monster drops
                    if (t <= 3)
                        Add(list, LoadMaterial(OreDir, $"ore_{metal}"), $"ore_{metal}", 2);
                    else
                        Add(list, LoadMaterial(BarDir, $"bar_{metal}"), $"bar_{metal}", 2);
                    break;

                case SkillType.Gathering: // sickle — always ore, no monster drops
                default:
                    Add(list, LoadMaterial(OreDir, $"ore_{metal}"), $"ore_{metal}", 2);
                    break;
            }
            return list.ToArray();
        }

        private static void AddHead(List<RecipeIngredient> list, Dictionary<int, Head> table, int t)
        {
            if (!table.TryGetValue(t, out var h)) return;
            Add(list, LoadMaterial(HeadDir, h.id), h.id, 1);
        }

        private static RecipeIngredient[] ArrowIngredients()
        {
            var list = new List<RecipeIngredient>();
            Add(list, LoadMaterial(LogDir, "log_0"), "log_0", 2); // Rough Logs
            return list.ToArray();
        }

        // ── asset creation ─────────────────────────────────────────────

        private static void CreateHeads()
        {
            foreach (var kv in ToothByTier)
                CreateHead(kv.Value.id, kv.Value.name, kv.Key, "pickaxe teeth");
            foreach (var kv in ScaleByTier)
                CreateHead(kv.Value.id, kv.Value.name, kv.Key, "axe heads");
        }

        // Drop-head material. tier = (RarityTier)t; goldValue scales with tier.
        private static MaterialItemSO CreateHead(string id, string name, int t, string kind)
        {
            string path = $"{HeadDir}/{id}.asset";
            var m = AssetDatabase.LoadAssetAtPath<MaterialItemSO>(path);
            if (m == null) { m = ScriptableObject.CreateInstance<MaterialItemSO>(); AssetDatabase.CreateAsset(m, path); }
            m.itemId = id;
            m.displayName = name;
            m.description = "Dropped by beasts / found in chests (wired later). Used to forge high-tier " + kind + ".";
            m.tier = (RarityTier)t;
            m.goldValue = 50 * (t - 3); // T4=50, T5=100, ... T8=250
            EditorUtility.SetDirty(m);
            return m;
        }

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

            if (tier == 0)
            {
                // Starter tool: free + ungated. KEEP.
                r.requiredToolTier = RarityTier.Common;
                r.ingredients = new RecipeIngredient[0];
            }
            else
            {
                r.requiredToolTier = (RarityTier)(tier - 1); // need the previous tool
                r.ingredients = ToolIngredients(skill, tier);
            }

            r.requiredSkillLevel = 0;
            r.requiredStation = "crafting_bench";
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

        private static void CreateAmmoRecipe(string id, string name, MaterialItemSO ammo, int qty,
            RecipeIngredient[] ingredients)
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
            r.ingredients = ingredients ?? new RecipeIngredient[0];
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

            var ids = new List<string>();
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
