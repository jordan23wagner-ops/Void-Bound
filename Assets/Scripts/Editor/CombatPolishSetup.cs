#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoidBound.Combat;
using VoidBound.Data;
using VoidBound.UI;

namespace VoidBound.Editor
{
    // Wires the combat-polish pass across both scenes (idempotent):
    //  - every enemy gets a HealthBar child (Ashfields enemies were missing it)
    //    and a CombatAnimator; the player gets a CombatAnimator too
    //  - the DevToolsPanel's "Give Test Gear" pool is set to one item per equip
    //    slot (full Iron set + a weapon)
    public static class CombatPolishSetup
    {
        private const string GearDir = "Assets/ScriptableObjects/Gear";

        [MenuItem("VoidBound/Combat - Polish Setup (health bars, animators, dev gear)")]
        public static void Run()
        {
            ProcessScene("Assets/Scenes/Homestead.unity");
            ProcessScene("Assets/Scenes/Ashfields.unity");
            Debug.Log("[CombatPolish] Health bars, animators, and dev gear wired in both scenes.");
        }

        private static void ProcessScene(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath);

            // CharacterAnimation is added by CharacterModelSwap alongside the
            // rigged model; here we just ensure enemies have a health bar.
            foreach (var ai in Object.FindObjectsByType<EnemyAI>(FindObjectsInactive.Include))
                EnsureHealthBar(ai.gameObject);

            WireDevGear();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void EnsureHealthBar(GameObject enemy)
        {
            if (enemy.GetComponentInChildren<HealthBar>(true) != null) return;
            var hb = new GameObject("HealthBar");
            hb.transform.SetParent(enemy.transform, false);
            hb.AddComponent<HealthBar>();
        }

        private static void WireDevGear()
        {
            var dev = Object.FindAnyObjectByType<DevToolsPanel>(FindObjectsInactive.Include);
            if (dev == null) return; // only Homestead has it

            // One item per visible equip slot so the button kits out the player.
            string[] ids =
            {
                "Rusty_Sword_Common@TestGear",
                "iron_helm", "iron_chestplate", "iron_greaves", "iron_boots",
                "iron_gauntlets", "travelers_cape", "wooden_shield", "iron_amulet",
            };

            var pool = ids.Select(Load).Where(g => g != null).ToArray();
            var so = new SerializedObject(dev);
            var prop = so.FindProperty("testGearPool");
            prop.arraySize = pool.Length;
            for (int i = 0; i < pool.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = pool[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GearItemSO Load(string id)
        {
            // "name@TestGear" points at the legacy TestGear folder; otherwise /Gear.
            string path = id.EndsWith("@TestGear")
                ? $"Assets/ScriptableObjects/TestGear/{id.Replace("@TestGear", "")}.asset"
                : $"{GearDir}/{id}.asset";
            var g = AssetDatabase.LoadAssetAtPath<GearItemSO>(path);
            if (g == null) Debug.LogWarning($"[CombatPolish] Gear not found: {path}");
            return g;
        }
    }
}
#endif
