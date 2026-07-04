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

        [MenuItem("VoidBound/Polish - Swap Character Models + Tooltips")]
        public static void Run()
        {
            var goblinMesh = LoadMesh(GoblinFbx);
            var heroMesh = LoadMesh(HeroFbx);
            if (goblinMesh == null || heroMesh == null)
            {
                Debug.LogError("[ModelSwap] Goblin.fbx / Hero.fbx not found — run Tools/build_character_models.py first.");
                return;
            }
            if (!CheckImportRotation(GoblinFbx) || !CheckImportRotation(HeroFbx)) return;

            var mats = CreateMaterials();
            string[] goblinSlots = SlotNames(GoblinFbx);
            string[] heroSlots = SlotNames(HeroFbx);

            ProcessScene("Assets/Scenes/Homestead.unity", goblinMesh, heroMesh, goblinSlots, heroSlots, mats, wireTooltips: true);
            ProcessScene("Assets/Scenes/Ashfields.unity", goblinMesh, heroMesh, goblinSlots, heroSlots, mats, wireTooltips: true);

            Debug.Log("[ModelSwap] Character models swapped and tooltips wired in both scenes.");
        }

        public static void RunFromBatch() => Run();

        // ═══════════════════════════════════════════════════════════

        private static Mesh LoadMesh(string fbxPath) =>
            AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Mesh>().FirstOrDefault();

        // Standing FBX rule: root must import at (0,0,0) or the export was wrong.
        private static bool CheckImportRotation(string fbxPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (prefab == null) return false;
            var euler = prefab.transform.rotation.eulerAngles;
            if (euler.sqrMagnitude > 0.01f)
            {
                Debug.LogError($"[ModelSwap] {fbxPath} imports with rotation {euler} — re-export, don't correct the instance.");
                return false;
            }
            return true;
        }

        // Imported material names define the slot order contract with Blender.
        private static string[] SlotNames(string fbxPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var renderer = prefab.GetComponentInChildren<MeshRenderer>();
            return renderer.sharedMaterials.Select(m => m != null ? m.name : "").ToArray();
        }

        private static Dictionary<string, Material> CreateMaterials()
        {
            var defs = new (string name, Color color)[]
            {
                ("GoblinSkin_Weak",     new Color(0.45f, 0.62f, 0.30f)), // olive green scout
                ("GoblinSkin_Standard", new Color(0.62f, 0.42f, 0.28f)), // red-brown warrior
                ("GoblinSkin_Elite",    new Color(0.58f, 0.26f, 0.34f)), // deep red champion
                ("GoblinCloth",         new Color(0.32f, 0.24f, 0.16f)),
                ("HeroSkin",            new Color(0.85f, 0.66f, 0.50f)),
                ("HeroArmor",           new Color(0.36f, 0.42f, 0.52f)),
                ("HeroHair",            new Color(0.26f, 0.16f, 0.10f)),
            };

            var result = new Dictionary<string, Material>();
            foreach (var (name, color) in defs)
            {
                string path = $"{MatDir}/{name}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = color;
                    AssetDatabase.CreateAsset(mat, path);
                }
                result[name] = mat;
            }
            AssetDatabase.SaveAssets();
            return result;
        }

        private static void ProcessScene(string scenePath, Mesh goblinMesh, Mesh heroMesh,
            string[] goblinSlots, string[] heroSlots, Dictionary<string, Material> mats, bool wireTooltips)
        {
            var scene = EditorSceneManager.OpenScene(scenePath);

            foreach (var ai in Object.FindObjectsByType<EnemyAI>(FindObjectsInactive.Include))
            {
                var tier = GetTier(ai);
                string skinName = tier switch
                {
                    EnemyTier.Weak => "GoblinSkin_Weak",
                    EnemyTier.Standard => "GoblinSkin_Standard",
                    _ => "GoblinSkin_Elite",
                };
                var slotMats = goblinSlots.Select(slot =>
                    slot.Contains("Cloth") ? mats["GoblinCloth"] : mats[skinName]).ToArray();
                ApplyModel(ai.gameObject, goblinMesh, slotMats);
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var slotMats = heroSlots.Select(slot =>
                    slot.Contains("Armor") ? mats["HeroArmor"]
                    : slot.Contains("Hair") ? mats["HeroHair"]
                    : mats["HeroSkin"]).ToArray();
                ApplyModel(player, heroMesh, slotMats);
            }

            if (wireTooltips)
                WireTooltips();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static EnemyTier GetTier(EnemyAI ai)
        {
            var so = new SerializedObject(ai);
            var def = so.FindProperty("definition").objectReferenceValue as EnemyDefinitionSO;
            return def != null ? def.tier : EnemyTier.Weak;
        }

        private static void ApplyModel(GameObject go, Mesh mesh, Material[] materials)
        {
            var filter = go.GetComponent<MeshFilter>();
            var renderer = go.GetComponent<MeshRenderer>();
            if (filter == null || renderer == null) return;
            filter.sharedMesh = mesh;
            renderer.sharedMaterials = materials;
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
