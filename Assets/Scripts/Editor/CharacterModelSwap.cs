#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoidBound.Combat;
using VoidBound.Core;
using VoidBound.Data;
using VoidBound.Skilling;
using VoidBound.UI;

namespace VoidBound.Editor
{
    // Polish pass: swaps the blocky placeholder meshes for the Blender-built
    // Goblin/Hero models (Tools/build_character_models.py) in both scenes,
    // creates the URP materials (goblin skin tinted per enemy tier), and
    // wires the building hover tooltips. Idempotent.
    public static class CharacterModelSwap
    {
        private const string GoblinFbx = "Assets/Art/Models/Goblin.fbx";
        private const string HeroFbx = "Assets/Art/Models/Hero.fbx";
        private const string MatDir = "Assets/Art/Materials";

        // Per-tier goblin body variant (distinct baked silhouette + world scale so
        // toughness reads at a glance). Keyed by enemyId, fallback by tier.
        private static (string fbx, float scale) GoblinVariant(EnemyDefinitionSO def)
        {
            string id = def != null ? def.enemyId : "";
            switch (id)
            {
                case "goblin_scout":    return ("Assets/Art/Models/Goblin_Scout.fbx", 0.92f);
                case "goblin_warrior":  return ("Assets/Art/Models/Goblin_Warrior.fbx", 1.00f);
                case "goblin_champion": return ("Assets/Art/Models/Goblin_Champion.fbx", 1.18f);
                case "goblin_warchief": return ("Assets/Art/Models/Goblin_Warchief.fbx", 1.50f);
            }
            var tier = def != null ? def.tier : EnemyTier.Weak;
            return tier switch
            {
                EnemyTier.Weak     => ("Assets/Art/Models/Goblin_Scout.fbx", 0.92f),
                EnemyTier.Standard => ("Assets/Art/Models/Goblin_Warrior.fbx", 1.00f),
                EnemyTier.Elite    => ("Assets/Art/Models/Goblin_Champion.fbx", 1.18f),
                _                  => ("Assets/Art/Models/Goblin_Warchief.fbx", 1.50f),
            };
        }

        private static string GoblinSkinFor(EnemyTier tier) => tier switch
        {
            EnemyTier.Weak     => "GoblinSkin_Weak",
            EnemyTier.Standard => "GoblinSkin_Standard",
            EnemyTier.Elite    => "GoblinSkin_Elite",
            _                  => "GoblinSkin_RareElite",
        };

        // Map a Blender material-slot name to its URP material by palette convention.
        private static Material MapGoblinSlot(string slot, string skinName, Dictionary<string, Material> mats)
        {
            if (slot.Contains("Cloth")) return mats["GoblinCloth"];
            if (slot.Contains("Dark"))  return mats["GoblinDark"];
            if (slot.Contains("Gold"))  return mats["GoblinGold"];
            if (slot.Contains("Gem"))   return mats["GoblinGem"];
            if (slot.Contains("Bone"))  return mats["GoblinBone"];
            return mats[skinName];
        }

        private static readonly Dictionary<string, string> BuildingTooltips = new()
        {
            { "Merchant", "Buy supplies and sell your loot for gold." },
            { "Storage Chest", "Bank up to 48 items, safe between runs." },
            { "Pool of Refreshment", "Full heal and a temporary all-stats boost." },
            { "Shrine", "Make a gold offering for a combat blessing." },
            { "Warriors Guild", "Pay gold to train Strength." },
            { "Rangers Guild", "Pay gold to train Dexterity." },
            { "Mages Guild", "Pay gold to train Intellect." },
            { "Fast Travel Portal", "Travel between discovered zones." },
            { "Watchtower", "Keep an eye on the wastes." },
            { "Forge", "Smelt ore and forge gear. (Smithing)" },
            { "Campfire", "Cook your catch into food. (Cooking)" },
            { "Garden", "Tend herbs and brew mixtures. (Gathering, Alchemy)" },
        };

        private const string HeroController = "Assets/Animation/HeroAnimator.controller";
        private const string GoblinController = "Assets/Animation/GoblinAnimator.controller";

        [MenuItem("VoidBound/Polish - Swap Character Models + Tooltips")]
        public static void Run()
        {
            var heroFbx = AssetDatabase.LoadAssetAtPath<GameObject>(HeroFbx);
            var goblinFbx = AssetDatabase.LoadAssetAtPath<GameObject>(GoblinFbx);
            var heroCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(HeroController);
            var goblinCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(GoblinController);
            if (heroFbx == null || goblinFbx == null)
            {
                Debug.LogError("[ModelSwap] Hero.fbx / Goblin.fbx not found — run Tools/build_character_models.py first.");
                return;
            }
            if (heroCtrl == null || goblinCtrl == null)
            {
                Debug.LogError("[ModelSwap] Animator controllers missing — run VoidBound/Animation - Setup Rigs + Controllers first.");
                return;
            }

            var mats = CreateMaterials();
            string[] goblinSlots = SlotNames(GoblinFbx);
            string[] heroSlots = SlotNames(HeroFbx);

            ProcessScene("Assets/Scenes/Homestead.unity", goblinFbx, heroFbx, goblinCtrl, heroCtrl, goblinSlots, heroSlots, mats, wireTooltips: true);
            ProcessScene("Assets/Scenes/Ashfields.unity", goblinFbx, heroFbx, goblinCtrl, heroCtrl, goblinSlots, heroSlots, mats, wireTooltips: true);

            Debug.Log("[ModelSwap] Rigged character models swapped and tooltips wired in both scenes.");
        }

        public static void RunFromBatch() => Run();

        // ═══════════════════════════════════════════════════════════

        // Imported material names define the slot contract with Blender.
        private static string[] SlotNames(string fbxPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var renderer = prefab.GetComponentInChildren<Renderer>();
            return renderer.sharedMaterials.Select(m => m != null ? m.name : "").ToArray();
        }

        private static Dictionary<string, Material> CreateMaterials()
        {
            var defs = new (string name, Color color)[]
            {
                ("GoblinSkin_Weak",      new Color(0.45f, 0.62f, 0.30f)), // olive green scout
                ("GoblinSkin_Standard",  new Color(0.55f, 0.45f, 0.24f)), // sallow ochre warrior
                ("GoblinSkin_Elite",     new Color(0.58f, 0.30f, 0.26f)), // ruddy red champion
                ("GoblinSkin_RareElite", new Color(0.40f, 0.18f, 0.22f)), // near-black crimson warchief
                ("GoblinCloth",          new Color(0.32f, 0.24f, 0.16f)),
                ("GoblinDark",           new Color(0.10f, 0.10f, 0.12f)), // blackened iron scrap
                ("GoblinGold",           new Color(0.82f, 0.63f, 0.20f)), // war-trophy trim
                ("GoblinBone",           new Color(0.86f, 0.82f, 0.70f)), // tusks / claws
                ("HeroSkin",             new Color(0.85f, 0.66f, 0.50f)),
                ("HeroArmor",            new Color(0.36f, 0.42f, 0.52f)),
                ("HeroHair",             new Color(0.26f, 0.16f, 0.10f)),
            };

            var result = new Dictionary<string, Material>();
            foreach (var (name, color) in defs)
            {
                string path = $"{MatDir}/{name}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(mat, path);
                }
                mat.color = color; // keep tints in sync if this runs again after a tweak
                EditorUtility.SetDirty(mat);
                result[name] = mat;
            }

            // Emissive sickly-green gem (goblin eyes / totem crystal focal points).
            {
                string path = $"{MatDir}/GoblinGem.mat";
                var gem = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (gem == null)
                {
                    gem = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(gem, path);
                }
                var glow = new Color(0.35f, 0.95f, 0.45f);
                gem.color = glow;
                gem.EnableKeyword("_EMISSION");
                gem.SetColor("_EmissionColor", glow * 1.6f);
                gem.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                EditorUtility.SetDirty(gem);
                result["GoblinGem"] = gem;
            }

            AssetDatabase.SaveAssets();
            return result;
        }

        private static void ProcessScene(string scenePath, GameObject goblinFbx, GameObject heroFbx,
            RuntimeAnimatorController goblinCtrl, RuntimeAnimatorController heroCtrl,
            string[] goblinSlots, string[] heroSlots, Dictionary<string, Material> mats, bool wireTooltips)
        {
            var scene = EditorSceneManager.OpenScene(scenePath);

            foreach (var ai in Object.FindObjectsByType<EnemyAI>(FindObjectsInactive.Include))
            {
                var def = GetDefinition(ai);
                var tier = def != null ? def.tier : EnemyTier.Weak;
                var (variantPath, scale) = GoblinVariant(def);
                var variantFbx = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath) ?? goblinFbx;
                string skinName = GoblinSkinFor(tier);
                var slotMats = SlotNames(variantPath)
                    .Select(slot => MapGoblinSlot(slot, skinName, mats)).ToArray();
                ApplyRiggedModel(ai.gameObject, variantFbx, goblinCtrl, slotMats, scale);
                ConfigureEquipmentVisuals(ai.gameObject, EquipmentVisuals.BodyType.Goblin, def);
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var slotMats = heroSlots.Select(slot =>
                    slot.Contains("Armor") ? mats["HeroArmor"]
                    : slot.Contains("Hair") ? mats["HeroHair"]
                    : mats["HeroSkin"]).ToArray();
                ApplyRiggedModel(player, heroFbx, heroCtrl, slotMats, 1f);
                ConfigureEquipmentVisuals(player, EquipmentVisuals.BodyType.Hero, null);
            }

            if (wireTooltips)
                WireTooltips();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static EnemyDefinitionSO GetDefinition(EnemyAI ai)
        {
            var so = new SerializedObject(ai);
            return so.FindProperty("definition").objectReferenceValue as EnemyDefinitionSO;
        }

        private static void ConfigureEquipmentVisuals(GameObject go, EquipmentVisuals.BodyType body,
            EnemyDefinitionSO enemyDef)
        {
            var visuals = go.GetComponent<EquipmentVisuals>();
            if (visuals == null) visuals = go.AddComponent<EquipmentVisuals>();
            visuals.Configure(body, enemyDef);
            EditorUtility.SetDirty(visuals);

            if (go.GetComponent<CharacterAnimation>() == null)
                go.AddComponent<CharacterAnimation>();
        }

        // Replaces the root static mesh with the rigged model as a child "Model"
        // (SkinnedMeshRenderer + Animator). Root keeps CharacterController + scripts.
        private static void ApplyRiggedModel(GameObject root, GameObject fbxPrefab,
            RuntimeAnimatorController controller, Material[] materials, float scale)
        {
            // Strip any old static mesh on the root.
            var mf = root.GetComponent<MeshFilter>();
            if (mf != null) Object.DestroyImmediate(mf);
            var mr = root.GetComponent<MeshRenderer>();
            if (mr != null) Object.DestroyImmediate(mr);

            var existing = root.transform.Find("Model");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var model = (GameObject)PrefabUtility.InstantiatePrefab(fbxPrefab);
            model.name = "Model";
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = Vector3.zero;
            // The FBX exports facing -Z, but movement/combat rotate the root so +Z
            // faces the travel/target direction — so the mesh must be turned to
            // face the root's forward, else the character walks/looks backward.
            model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            model.transform.localScale = Vector3.one * scale;

            var anim = model.GetComponent<Animator>();
            if (anim == null) anim = model.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
            anim.applyRootMotion = false;

            var smr = model.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null) smr.sharedMaterials = materials;
        }

        private static void WireTooltips()
        {
            // Building descriptions by GameObject name; resource nodes generically.
            foreach (var interactable in Object.FindObjectsByType<Interactable>(FindObjectsInactive.Include))
            {
                string desc = BuildingTooltips.TryGetValue(interactable.gameObject.name, out var d)
                    ? d
                    : interactable is ResourceNode ? "Gather resources here." : null;
                if (desc == null) continue;

                var so = new SerializedObject(interactable);
                so.FindProperty("tooltipDescription").stringValue = desc;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Tooltip UI host lives on the (persisted) HUDCanvas in Homestead.
            var hud = GameObject.Find("HUDCanvas");
            if (hud != null && hud.GetComponent<BuildingTooltipUI>() == null)
                hud.AddComponent<BuildingTooltipUI>();
        }
    }
}
#endif
