#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoidBound.Data;
using VoidBound.Homestead;
using VoidBound.Inventory;
using VoidBound.UI;

namespace VoidBound.Editor
{
    // Wires the Enchanted Chest (GDD §5.6): swaps its PlaceholderStation for the
    // real EnchantedChestStation, points its upgrade materials at the 9 bars,
    // and adds PlayerUpgrades + EnchantedChestUI. Idempotent.
    public static class EnchantedChestSetup
    {
        private static readonly string[] BarRanks =
        {
            "copper", "tin", "iron", "silver", "gold", "mithril", "obsidian", "radiant", "void",
        };

        [MenuItem("VoidBound/Setup Enchanted Chest")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Homestead.unity");

            var bars = new MaterialItemSO[BarRanks.Length];
            for (int i = 0; i < BarRanks.Length; i++)
                bars[i] = AssetDatabase.LoadAssetAtPath<MaterialItemSO>(
                    $"Assets/ScriptableObjects/Materials/Bars/bar_{BarRanks[i]}.asset");

            var chest = GameObject.Find("Enchanted Chest");
            if (chest == null) { Debug.LogWarning("[EnchantedChest] 'Enchanted Chest' not found."); return; }

            var placeholder = chest.GetComponent<PlaceholderStation>();
            if (placeholder != null) Object.DestroyImmediate(placeholder);

            var station = chest.GetComponent<EnchantedChestStation>() ?? chest.AddComponent<EnchantedChestStation>();
            var so = new SerializedObject(station);
            so.FindProperty("interactPrompt").stringValue = "Upgrade";
            so.FindProperty("interactRange").floatValue = 3f;
            so.FindProperty("tooltipDescription").stringValue =
                "Upgrade untradables up the rarity ladder (time-vs-risk).";
            so.FindProperty("costPerUpgrade").intValue = 2;
            var arr = so.FindProperty("upgradeMaterials");
            arr.arraySize = bars.Length;
            for (int i = 0; i < bars.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = bars[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            if (chest.GetComponent<Collider>() == null)
            {
                var bc = chest.AddComponent<BoxCollider>();
                bc.center = new Vector3(0f, 0.75f, 0f);
                bc.size = new Vector3(2f, 1.5f, 2f);
                bc.isTrigger = true;
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && player.GetComponent<PlayerUpgrades>() == null)
                player.AddComponent<PlayerUpgrades>();

            var hud = Object.FindAnyObjectByType<HUDManager>();
            if (hud != null && hud.GetComponent<EnchantedChestUI>() == null)
                hud.gameObject.AddComponent<EnchantedChestUI>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[EnchantedChest] Station swapped, bars wired, PlayerUpgrades + UI added.");
        }

        public static void RunFromBatch() => Run();
    }
}
#endif
