#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoidBound.Data;
using VoidBound.Skilling;

namespace VoidBound.Editor
{
    // Alchemy slice (GDD §5.3): Gathering → Alchemy. Each of the 8 potion families
    // has its OWN 9-tier flora line (72 flora), gathered from that family's herb
    // patch in the home herb garden, brewed at the Garden. Health + the four stat
    // buffs (Warrior/Ranger/Mage/Warding) are functional now; Swiftness/Antidote/
    // Prospector's are defined but inert until their systems land. Idempotent.
    public static class AlchemyContentSetup
    {
        private const string FloraDir = "Assets/ScriptableObjects/Materials/Flora";
        private const string PotDir = "Assets/ScriptableObjects/Materials/Potions";
        private const string RecDir = "Assets/ScriptableObjects/Recipes/Alchemy";

        // One 9-tier flora line per potion family (ids flora_{famId}_{tier}), each
        // gathered from its own dedicated herb patch. Replaces the old shared
        // per-tier flora so a family's potions require that family's herb.
        private static readonly Dictionary<string, string[]> FamilyFlora = new()
        {
            { "health",     new[] { "Bloodroot", "Redclover", "Mendleaf", "Sanguine Rose", "Heartbloom", "Lifevine", "Grave Lily", "Seraph's Tear", "Undying Lotus" } },
            { "warrior",    new[] { "Ironweed", "Thornwhistle", "Bloodthorn", "Rageblossom", "Warbrand", "Titan's Root", "Obsidian Bramble", "Radiant Warthorn", "Voidfury Bloom" } },
            { "ranger",     new[] { "Fleetleaf", "Hawkweed", "Windwort", "Keeneye Blossom", "Falconbloom", "Whisperfern", "Shadowfrond", "Radiant Quill", "Voidsight Bloom" } },
            { "mage",       new[] { "Wispcap", "Manaroot", "Starflax", "Runeblossom", "Arcanthus", "Seer's Sage", "Obsidian Wisp", "Astral Bloom", "Voidmind Flower" } },
            { "warding",    new[] { "Stoneleaf", "Bulwark Moss", "Wardbloom", "Ironbark Fern", "Aegis Blossom", "Bastion Root", "Obsidian Ward", "Radiant Wardlily", "Voidshield Bloom" } },
            { "swiftness",  new[] { "Gustweed", "Swiftroot", "Zephyr Blossom", "Galewort", "Stormpetal", "Tempest Lily", "Obsidian Gale", "Radiant Windflower", "Voidstep Bloom" } },
            { "antidote",   new[] { "Bitterroot", "Cleanleaf", "Purgemint", "Venombane", "Clearbloom", "Panacea Lily", "Obsidian Purgewort", "Radiant Cleansebloom", "Voidpurge Flower" } },
            { "prospector", new[] { "Goldweed", "Fortune Clover", "Gleambloom", "Luckthistle", "Gilded Blossom", "Fortune's Lily", "Obsidian Glimmer", "Radiant Goldbloom", "Voidfortune Flower" } },
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

        [MenuItem("VoidBound/Setup Alchemy Content")]
        public static void Run()
        {
            EnsureFolder(FloraDir); EnsureFolder(PotDir); EnsureFolder(RecDir);

            // 8 families × 9 tiers: each family gets its own flora line, its
            // potions, and brew recipes that require that family's flora.
            foreach (var fam in Fams)
            {
                var names = FamilyFlora[fam.id];
                for (int t = 0; t < 9; t++)
                {
                    CreateFlora($"flora_{fam.id}_{t}", names[t], (RarityTier)t);
                    var potion = CreatePotion(fam, t);
                    CreateBrewRecipe($"brew_{fam.id}_{t}", $"Brew {TierName[t]} {fam.name}",
                        $"flora_{fam.id}_{t}", potion);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Homestead.unity");
            BuildHerbGarden();
            WireGarden();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Alchemy] 72 flora (8 families × 9) + 72 potions + recipes wired to the Garden; 8-patch herb garden built.");
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

        // Rebuilds the home herb garden: one patch per family in a 2×4 grid in the
        // open west-central pocket. Patches sit ~4 apart with a tight 1.8 interact
        // range so each is gathered individually (no cross-family cycling).
        // Idempotent under a "Herb Garden" root; retires the old shared patches.
        private static void BuildHerbGarden()
        {
            foreach (var legacy in new[] { "Herb Patch", "Herb Patch 2" })
            {
                var old = GameObject.Find(legacy);
                if (old != null) Object.DestroyImmediate(old);
            }
            var oldRoot = GameObject.Find("Herb Garden");
            if (oldRoot != null) Object.DestroyImmediate(oldRoot);
            var root = new GameObject("Herb Garden").transform;

            float[] xs = { -9f, -13f };
            float[] zs = { -1f, 3f, 7f, 11f };
            for (int i = 0; i < Fams.Length; i++)
                CreateHerbPatch(root, Fams[i], new Vector3(xs[i % 2], 0f, zs[i / 2]));
        }

        private static void CreateHerbPatch(Transform root, Fam fam, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = fam.name + " Herbs";
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.6f;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
                mr.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.3f, 0.6f, 0.25f) };
            var col = go.GetComponent<SphereCollider>();
            if (col != null) { col.radius = 0.9f; col.isTrigger = true; }

            var node = go.AddComponent<ResourceNode>();
            var so = new SerializedObject(node);
            var arr = so.FindProperty("tieredMaterials");
            arr.arraySize = 9;
            for (int t = 0; t < 9; t++)
                arr.GetArrayElementAtIndex(t).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<MaterialItemSO>($"{FloraDir}/flora_{fam.id}_{t}.asset");
            so.FindProperty("gatherSkill").enumValueIndex = (int)SkillType.Gathering;
            so.FindProperty("gatherQuantity").intValue = 1;
            so.FindProperty("respawnTime").floatValue = 4f;
            so.FindProperty("interactRange").floatValue = 1.8f;
            so.FindProperty("interactPrompt").stringValue = "Gather " + fam.name + " Herbs";
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
