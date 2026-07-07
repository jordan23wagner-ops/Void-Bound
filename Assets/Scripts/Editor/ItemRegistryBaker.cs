#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using VoidBound.Data;

namespace VoidBound.Editor
{
    // Scans the project for every GearItemSO + MaterialItemSO and bakes them into
    // Assets/Resources/ItemRegistry.asset, so the save system can resolve saved
    // itemIds back to their assets at runtime. Re-run after adding new items.
    public static class ItemRegistryBaker
    {
        private const string Dir = "Assets/Resources";
        private const string Path = Dir + "/ItemRegistry.asset";

        [MenuItem("VoidBound/Bake Item Registry")]
        public static void Bake()
        {
            if (!Directory.Exists(Dir)) Directory.CreateDirectory(Dir);

            var reg = AssetDatabase.LoadAssetAtPath<ItemRegistrySO>(Path);
            if (reg == null)
            {
                reg = ScriptableObject.CreateInstance<ItemRegistrySO>();
                AssetDatabase.CreateAsset(reg, Path);
            }

            reg.gear = LoadAll<GearItemSO>();
            reg.materials = LoadAll<MaterialItemSO>();
            EditorUtility.SetDirty(reg);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ItemRegistry] Baked {reg.gear.Length} gear + {reg.materials.Length} materials → {Path}");
        }

        public static void RunFromBatch() => Bake();

        private static T[] LoadAll<T>() where T : Object
        {
            var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            var list = new List<T>();
            foreach (var guid in guids)
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) list.Add(asset);
            }
            return list.ToArray();
        }
    }
}
#endif
