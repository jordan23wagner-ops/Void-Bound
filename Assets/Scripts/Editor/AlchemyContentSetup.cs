#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoidBound.Data;
using VoidBound.Skilling;

namespace VoidBound.Editor
{
    // Alchemy slice (GDD §5.3): Gathering → Alchemy. 9 sickle-gated flora → the
    // 8 potion families × 9 tiers, brewed at the Garden. Health + the four stat
    // buffs (Warrior/Ranger/Mage/Warding) are functional now; Swiftness/Antidote/
    // Prospector's are defined but inert until their systems land. Idempotent.
    public static class AlchemyContentSetup
    {
        private const string FloraDir = "Assets/ScriptableObjects/Materials/Flora";
        private const string PotDir = "Assets/ScriptableObjects/Materials/Potions";
        private const string RecDir = "Assets/ScriptableObjects/Recipes/Alchemy";

        private static readonly string[] Flora =
        {
            "Clover", "Sage", "Bramble", "Foxglove", "Nightshade",
            "Moonpetal", "Obsidian Bloom", "Radiant Lotus", "Voidflower",
        };
        private static readonly string[] TierName =
        {
            "Common", "Uncommon", "Magic", "Rare", "Epic", "Legendary", "Obsidian", "Radiant", "Void",
        };

        private struct Fam { public string id, name; public ConsumableEffect effect; public int baseMag, perTier; public float dur; }
        private static readonly Fam[] Fams =
        {
            new Fam { id="health",     name="Health",      effect=ConsumableEffect.Heal,    baseMag=20, perTier=15, dur=0 },
            new Fam { id="warrior",    name="Warrior",     effect=ConsumableEffect.BuffSTR, baseMag=2,  perTier=2,  dur=60 },
            new Fam { id="ranger",     name="Ranger",      effect=ConsumableEffect.BuffDEX, baseMag=2,  perTier=2,  dur=60 },
            new Fam { id="mage",       name="Mage",        effect=ConsumableEffect.BuffINT, baseMag=2,  perTier=2,  dur=60 },
            new Fam { id="warding",    name="Warding",     effect=ConsumableEffect.BuffVIG, baseMag=2,  perTier=2,  dur=60 },
            new Fam { id="swiftness",  name="Swiftness",   effect=ConsumableEffect.Swiftness,  baseMag=10, perTier=3, dur=30 },
            new Fam { id="antidote",   name="Antidote",    effect=ConsumableEffect.CurePoison, baseMag=0,  perTier=0, dur=0 },
            new Fam { id="prospector", name="Prospector's",effect=ConsumableEffect.Luck,       baseMag=10, perTier=3, dur=60 },
        };

        private static string Slug(string s) => s.ToLower().Replace(" ", "_").Replace("'", "");

        [MenuItem("VoidBound/Setup Alchemy Content")]
        public static void Run()
        {
            EnsureFolder(FloraDir); EnsureFolder(PotDir); EnsureFolder(RecDir);

            for (int t = 0; t < Flora.Length; t++)
                CreateFlora("flora_" + Slug(Flora[t]), Flora[t], (RarityTier)t);

            foreach (var fam in Fams)
                for (int t = 0; t < 9; t++)
                {
                    var potion = CreatePotion(fam, t);
                    CreateBrewRecipe($"brew_{fam.id}_{t}", $"Brew {TierName[t]} {fam.name}",
                        "flora_" + Slug(Flora[t]), potion);
                }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Homestead.unity");
            WireOrCreateHerbNode("Herb Patch", new Vector3(-12f, 0f, 6f));
            WireOrCreateHerbNode("Herb Patch 2", new Vector3(-9.5f, 0f, 6.5f));
            WireGarden();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Alchemy] 9 flora + 72 potions + recipes wired to the Garden; herb patches created.");
        }

        public static void RunFromBatch() => Run();

        // ── asset creation ─────────────────────────────────────────────

        private static void CreateFlora(string id, string name, RarityTier tier)
        {
            string path = $"{FloraDir}/{id}.asset";
            var m = AssetDatabase.LoadAssetAtPath<MaterialItemSO>(path);
            if (m == null) { m = ScriptableObject.CreateInstance<MaterialItemSO>(); AssetDatabase.CreateAsset(m, path); }
            m.itemId = id; m.displayName = name; m.tier = tier; m.goldValue = 3;
            EditorUtility.SetDirty(m);
        }

        private static MaterialItemSO CreatePotion(Fam fam, int t)
        {
            string id = $"potion_{fam.id}_{t}";
            string path = $"{PotDir}/{id}.asset";
            var m = AssetDatabase.LoadAssetAtPath<MaterialItemSO>(path);
            if (m == null) { m = ScriptableObject.CreateInstance<MaterialItemSO>(); AssetDatabase.CreateAsset(m, path); }
            m.itemId = id;
            m.displayName = $"{TierName[t]} {fam.name} Potion";
            m.tier = (RarityTier)t;
            m.isConsumable = true;
            m.healOverTime = 0;
            m.effect = fam.effect;
            m.effectMagnitude = fam.baseMag + t * fam.perTier;
            m.effectDuration = fam.dur;
            m.goldValue = 5 + t * 3;
            EditorUtility.SetDirty(m);
            return m;
        }

        private static void CreateBrewRecipe(string id, string name, string floraId, MaterialItemSO potion)
        {
            var flora = AssetDatabase.LoadAssetAtPath<MaterialItemSO>($"{FloraDir}/{floraId}.asset");
            string path = $"{RecDir}/{id}.asset";
            var r = AssetDatabase.LoadAssetAtPath<RecipeDefinitionSO>(path);
            if (r == null) { r = ScriptableObject.CreateInstance<RecipeDefinitionSO>(); AssetDatabase.CreateAsset(r, path); }
            r.recipeId = id;
            r.displayName = name;
            r.requiredSkill = SkillType.Alchemy;
            r.requiredToolTier = RarityTier.Common; // gated by having the flora
            r.requiredSkillLevel = 0;
            r.requiredStation = "Garden";
            r.ingredients = new[] { new RecipeIngredient { material = flora, quantity = 2 } };
            r.outputType = RecipeOutputType.Material;
            r.outputMaterial = potion;
            r.outputTool = null; r.outputGear = null;
            r.outputQuantity = 1; r.xpReward = 0;
            EditorUtility.SetDirty(r);
        }

        // ── scene wiring ───────────────────────────────────────────────

        private static void WireOrCreateHerbNode(string name, Vector3 pos)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = name;
                go.transform.localScale = Vector3.one * 0.6f;
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.3f, 0.6f, 0.25f) };
                    mr.sharedMaterial = mat;
                }
            }
            go.transform.position = pos;
            if (go.GetComponent<Collider>() == null) { var sc = go.AddComponent<SphereCollider>(); sc.radius = 1.2f; sc.isTrigger = true; }

            var node = go.GetComponent<ResourceNode>() ?? go.AddComponent<ResourceNode>();
            var so = new SerializedObject(node);
            var arr = so.FindProperty("tieredMaterials");
            arr.arraySize = Flora.Length;
            for (int i = 0; i < Flora.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<MaterialItemSO>($"{FloraDir}/flora_{Slug(Flora[i])}.asset");
            so.FindProperty("gatherSkill").enumValueIndex = (int)SkillType.Gathering;
            so.FindProperty("gatherQuantity").intValue = 1;
            so.FindProperty("respawnTime").floatValue = 4f;
            so.FindProperty("interactRange").floatValue = 2.5f;
            so.FindProperty("interactPrompt").stringValue = "Gather";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireGarden()
        {
            var garden = GameObject.Find("Garden");
            var cs = garden != null ? garden.GetComponent<CraftingStation>() : null;
            if (cs == null) { Debug.LogWarning("[Alchemy] Garden station not found."); return; }

            var ids = new List<string>();
            foreach (var fam in Fams) for (int t = 0; t < 9; t++) ids.Add($"brew_{fam.id}_{t}");

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
