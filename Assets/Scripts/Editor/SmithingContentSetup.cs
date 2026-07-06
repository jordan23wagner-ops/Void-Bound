#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoidBound.Data;
using VoidBound.Skilling;

namespace VoidBound.Editor
{
    // Smithing slice (GDD §5.4): Mining → Smithing. 9 pickaxe-gated ores → smelt
    // to 9 bars at the Forge → forge untradable gear. Mirrors the Fishing→Cooking
    // pattern. Base untradables use Copper bars so the whole loop is testable
    // with the starting Common pickaxe. Idempotent.
    public static class SmithingContentSetup
    {
        private const string OreDir = "Assets/ScriptableObjects/Materials/Ore";
        private const string BarDir = "Assets/ScriptableObjects/Materials/Bars";
        private const string RecDir = "Assets/ScriptableObjects/Recipes/Smithing";
        private const string GearDir = "Assets/ScriptableObjects/Gear/Smithed";

        // rank name (Copper..Void)
        private static readonly string[] Ranks =
        {
            "Copper", "Tin", "Iron", "Silver", "Gold", "Mithril", "Obsidian", "Radiant", "Void",
        };

        [MenuItem("VoidBound/Setup Smithing Content")]
        public static void Run()
        {
            EnsureFolder(OreDir); EnsureFolder(BarDir); EnsureFolder(RecDir); EnsureFolder(GearDir);

            for (int i = 0; i < Ranks.Length; i++)
            {
                string id = Ranks[i].ToLower();
                var tier = (RarityTier)i;
                var ore = CreateMat(OreDir, "ore_" + id, Ranks[i] + " Ore", tier, 4);
                var bar = CreateMat(BarDir, "bar_" + id, Ranks[i] + " Bar", tier, 8);
                CreateSmeltRecipe("smelt_" + id, "Smelt " + Ranks[i], ore, bar);
            }

            // Untradable gear forged from Copper bars (base tier, reuses existing meshes)
            var sword = CopyGear("Assets/ScriptableObjects/TestGear/Rusty_Sword_Common.asset",
                GearDir + "/smithed_sword.asset", "smithed_sword", "Smithed Sword");
            var helm = CopyGear("Assets/ScriptableObjects/Gear/iron_helm.asset",
                GearDir + "/smithed_helm.asset", "smithed_helm", "Smithed Helm");
            var chest = CopyGear("Assets/ScriptableObjects/Gear/iron_chestplate.asset",
                GearDir + "/smithed_chestplate.asset", "smithed_chestplate", "Smithed Chestplate");
            CreateForgeRecipe("forge_smithed_sword", "Forge Smithed Sword", "bar_copper", 2, sword);
            CreateForgeRecipe("forge_smithed_helm", "Forge Smithed Helm", "bar_copper", 2, helm);
            CreateForgeRecipe("forge_smithed_chestplate", "Forge Smithed Chestplate", "bar_copper", 3, chest);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Homestead.unity");
            WireOrCreateOreNode("Ore Deposit", new Vector3(-14f, 0f, 2f));
            WireOrCreateOreNode("Ore Deposit 2", new Vector3(-13f, 0f, -1f));
            WireForge();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Smithing] 9 ore + 9 bars + smelt/forge recipes wired to the Forge; ore node tiered.");
        }

        public static void RunFromBatch() => Run();

        // ── asset creation ─────────────────────────────────────────────

        private static MaterialItemSO CreateMat(string dir, string id, string name, RarityTier tier, int gold)
        {
            string path = $"{dir}/{id}.asset";
            var m = AssetDatabase.LoadAssetAtPath<MaterialItemSO>(path);
            if (m == null) { m = ScriptableObject.CreateInstance<MaterialItemSO>(); AssetDatabase.CreateAsset(m, path); }
            m.itemId = id; m.displayName = name; m.tier = tier; m.goldValue = gold;
            EditorUtility.SetDirty(m);
            return m;
        }

        private static void CreateSmeltRecipe(string id, string name, MaterialItemSO ore, MaterialItemSO bar)
        {
            var r = LoadOrCreateRecipe(id);
            r.displayName = name;
            r.requiredSkill = SkillType.Smithing;
            r.requiredToolTier = RarityTier.Common; // gated by having the ore
            r.requiredStation = "Forge";
            r.ingredients = new[] { new RecipeIngredient { material = ore, quantity = 2 } };
            r.outputType = RecipeOutputType.Material;
            r.outputMaterial = bar;
            r.outputTool = null; r.outputGear = null;
            r.outputQuantity = 1; r.xpReward = 0;
            EditorUtility.SetDirty(r);
        }

        private static void CreateForgeRecipe(string id, string name, string barId, int qty, GearItemSO gear)
        {
            var bar = AssetDatabase.LoadAssetAtPath<MaterialItemSO>($"{BarDir}/{barId}.asset");
            var r = LoadOrCreateRecipe(id);
            r.displayName = name;
            r.requiredSkill = SkillType.Smithing;
            r.requiredToolTier = RarityTier.Common;
            r.requiredStation = "Forge";
            r.ingredients = new[] { new RecipeIngredient { material = bar, quantity = qty } };
            r.outputType = RecipeOutputType.Gear;
            r.outputGear = gear;
            r.outputTool = null; r.outputMaterial = null;
            r.outputQuantity = 1; r.xpReward = 0;
            EditorUtility.SetDirty(r);
        }

        private static RecipeDefinitionSO LoadOrCreateRecipe(string id)
        {
            string path = $"{RecDir}/{id}.asset";
            var r = AssetDatabase.LoadAssetAtPath<RecipeDefinitionSO>(path);
            if (r == null) { r = ScriptableObject.CreateInstance<RecipeDefinitionSO>(); AssetDatabase.CreateAsset(r, path); }
            r.recipeId = id;
            r.requiredSkillLevel = 0;
            return r;
        }

        private static GearItemSO CopyGear(string src, string dst, string id, string name)
        {
            if (AssetDatabase.LoadAssetAtPath<GearItemSO>(dst) == null)
                AssetDatabase.CopyAsset(src, dst);
            var g = AssetDatabase.LoadAssetAtPath<GearItemSO>(dst);
            g.itemId = id; g.displayName = name; g.rarity = RarityTier.Uncommon;
            g.untradable = true; g.goldValue = 0;
            EditorUtility.SetDirty(g);
            return g;
        }

        // ── scene wiring ───────────────────────────────────────────────

        private static void WireOrCreateOreNode(string name, Vector3 pos)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/ResourceNode_Rock.fbx");
                if (model != null) { go = (GameObject)PrefabUtility.InstantiatePrefab(model); go.name = name; }
                else { go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name = name; go.transform.localScale = Vector3.one * 0.7f; }
            }
            go.transform.position = pos;

            if (go.GetComponent<Collider>() == null)
            {
                var sc = go.AddComponent<SphereCollider>();
                sc.radius = 1.2f;
                sc.isTrigger = true;
            }

            var node = go.GetComponent<ResourceNode>() ?? go.AddComponent<ResourceNode>();
            var so = new SerializedObject(node);
            var arr = so.FindProperty("tieredMaterials");
            arr.arraySize = Ranks.Length;
            for (int i = 0; i < Ranks.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<MaterialItemSO>($"{OreDir}/ore_{Ranks[i].ToLower()}.asset");
            so.FindProperty("gatherSkill").enumValueIndex = (int)SkillType.Mining;
            so.FindProperty("gatherQuantity").intValue = 1;
            so.FindProperty("respawnTime").floatValue = 4f;
            so.FindProperty("interactRange").floatValue = 2.5f;
            so.FindProperty("interactPrompt").stringValue = "Mine";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireForge()
        {
            var forge = GameObject.Find("Forge");
            var cs = forge != null ? forge.GetComponent<CraftingStation>() : null;
            if (cs == null) { Debug.LogWarning("[Smithing] Forge station not found."); return; }

            var ids = new List<string>();
            foreach (var r in Ranks) ids.Add("smelt_" + r.ToLower());
            ids.Add("forge_smithed_sword");
            ids.Add("forge_smithed_helm");
            ids.Add("forge_smithed_chestplate");

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
