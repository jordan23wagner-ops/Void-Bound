#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VoidBound.Editor
{
    // Polish pass 2: swaps the generic shared building meshes for the 12
    // distinct Blender models (Tools/build_homestead_buildings.py). Shared
    // URP material palette assigned by imported slot name; Fire/Water/Crystal
    // are emissive. Colliders and station components untouched. Idempotent.
    public static class HomesteadBuildingSwap
    {
        private const string ModelDir = "Assets/Art/Models/Buildings";
        private const string MatDir = "Assets/Art/Materials/Buildings";

        // Building GameObject name -> FBX file name
        private static readonly Dictionary<string, string> BuildingModels = new()
        {
            { "Forge", "Forge" },
            { "Campfire", "Campfire" },
            { "Garden", "Garden" },
            { "Merchant", "Merchant" },
            { "Storage Chest", "StorageChest" },
            { "Pool of Refreshment", "Pool" },
            { "Shrine", "Shrine" },
            { "Warriors Guild", "WarriorsGuild" },
            { "Rangers Guild", "RangersGuild" },
            { "Mages Guild", "MagesGuild" },
            { "Watchtower", "Watchtower" },
            { "Fast Travel Portal", "Portal" },
        };

        // Slot name -> (base color, emission color or null)
        private static readonly Dictionary<string, (Color color, Color? emission)> Palette = new()
        {
            { "WoodDark",   (new Color(0.30f, 0.20f, 0.12f), null) },
            { "WoodLight",  (new Color(0.55f, 0.40f, 0.25f), null) },
            { "Stone",      (new Color(0.58f, 0.57f, 0.53f), null) },
            { "StoneDark",  (new Color(0.36f, 0.36f, 0.35f), null) },
            { "Metal",      (new Color(0.45f, 0.47f, 0.50f), null) },
            { "Thatch",     (new Color(0.72f, 0.60f, 0.30f), null) },
            { "Fire",       (new Color(1.00f, 0.45f, 0.10f), new Color(1.0f, 0.35f, 0.05f) * 1.6f) },
            { "Water",      (new Color(0.25f, 0.60f, 0.90f), new Color(0.10f, 0.35f, 0.70f) * 0.8f) },
            { "Crystal",    (new Color(0.65f, 0.35f, 0.90f), new Color(0.50f, 0.20f, 0.85f) * 1.4f) },
            { "ClothRed",   (new Color(0.70f, 0.20f, 0.18f), null) },
            { "ClothGreen", (new Color(0.25f, 0.55f, 0.25f), null) },
            { "ClothBlue",  (new Color(0.25f, 0.40f, 0.75f), null) },
            { "ClothWhite", (new Color(0.85f, 0.82f, 0.75f), null) },
            { "Leaf",       (new Color(0.30f, 0.60f, 0.25f), null) },
            { "Soil",       (new Color(0.30f, 0.22f, 0.15f), null) },
        };

        [MenuItem("VoidBound/Polish - Swap Homestead Building Models")]
        public static void Run()
        {
            // Validate all 12 FBXs exist and import clean before touching scenes
            foreach (var fbxName in BuildingModels.Values)
            {
                string path = $"{ModelDir}/{fbxName}.fbx";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogError($"[BuildingSwap] Missing {path} — run Tools/build_homestead_buildings.py first.");
                    return;
                }
                var euler = prefab.transform.rotation.eulerAngles;
                if (euler.sqrMagnitude > 0.01f)
                {
                    Debug.LogError($"[BuildingSwap] {path} imports with rotation {euler} — re-export.");
                    return;
                }
            }

            var mats = CreateMaterials();
            SwapInScene("Assets/Scenes/Homestead.unity", mats);
            SwapInScene("Assets/Scenes/Ashfields.unity", mats); // has its own Fast Travel Portal
            Debug.Log("[BuildingSwap] All Homestead building models swapped.");
        }

        public static void RunFromBatch() => Run();

        private static Dictionary<string, Material> CreateMaterials()
        {
            if (!AssetDatabase.IsValidFolder(MatDir))
                AssetDatabase.CreateFolder("Assets/Art/Materials", "Buildings");

            var result = new Dictionary<string, Material>();
            foreach (var (name, (color, emission)) in Palette.Select(kv => (kv.Key, kv.Value)))
            {
                string path = $"{MatDir}/{name}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null)
                {
                    m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    m.color = color;
                    if (emission.HasValue)
                    {
                        m.EnableKeyword("_EMISSION");
                        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                        m.SetColor("_EmissionColor", emission.Value);
                    }
                    AssetDatabase.CreateAsset(m, path);
                }
                result[name] = m;
            }
            AssetDatabase.SaveAssets();
            return result;
        }

        private static void SwapInScene(string scenePath, Dictionary<string, Material> mats)
        {
            var scene = EditorSceneManager.OpenScene(scenePath);
            int swapped = 0;

            foreach (var (goName, fbxName) in BuildingModels.Select(kv => (kv.Key, kv.Value)))
            {
                var go = GameObject.Find(goName);
                if (go == null) continue; // Ashfields only has the Portal

                string fbxPath = $"{ModelDir}/{fbxName}.fbx";
                var mesh = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Mesh>().FirstOrDefault();
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                var importedSlots = prefab.GetComponentInChildren<MeshRenderer>()
                    .sharedMaterials.Select(m => m != null ? m.name : "").ToArray();

                var filter = go.GetComponent<MeshFilter>();
                var renderer = go.GetComponent<MeshRenderer>();
                if (filter == null || renderer == null || mesh == null) continue;

                filter.sharedMesh = mesh;
                renderer.sharedMaterials = importedSlots
                    .Select(slot => mats.TryGetValue(slot, out var m) ? m : mats["Stone"])
                    .ToArray();
                swapped++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[BuildingSwap] {scenePath}: swapped {swapped} buildings.");
        }
    }
}
#endif
