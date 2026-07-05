#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoidBound.Data;
using VoidBound.Inventory;

namespace VoidBound.Editor
{
    // Developer play-mode conveniences (editor-only, toggles persisted per-user
    // via EditorPrefs — nothing ships in a build):
    //   1. Play always boots the Homestead scene, so pressing Play from Ashfields
    //      (or any zone that has no camera of its own) still works — the persisted
    //      Player/Camera/HUD come from Homestead's GameBootstrap.
    //   2. Auto-equip a loadout kit (Melee / Ranged / Mage) on play start, and
    //      one-click swap kits from the menu — so you don't re-equip by hand.
    // Toggle / pick from the VoidBound menu.
    [InitializeOnLoad]
    public static class DevPlaySetup
    {
        private const string HomesteadPath = "Assets/Scenes/Homestead.unity";
        private const string PlayFromHomesteadKey = "VoidBound.PlayFromHomestead";
        public const string AutoEquipKey = "VoidBound.AutoEquipTestGear";
        private const string KitKey = "VoidBound.DevKit"; // "Melee" / "Ranged" / "Mage"

        private const string PlayMenu = "VoidBound/Dev - Play From Homestead";
        private const string AutoEquipMenu = "VoidBound/Dev - Auto-Equip Kit on Play";
        private const string MeleeMenu = "VoidBound/Dev - Equip Melee Kit";
        private const string RangedMenu = "VoidBound/Dev - Equip Ranged Kit";
        private const string MageMenu = "VoidBound/Dev - Equip Mage Kit";

        // Kits are asset paths. Armor is shared for now (weapon defines the class);
        // ranged/mage drop the shield since bow/staff are two-handed.
        private const string G = "Assets/ScriptableObjects/Gear/";
        private static readonly string[] SharedArmor =
        {
            G + "iron_helm.asset", G + "iron_chestplate.asset", G + "iron_greaves.asset",
            G + "iron_boots.asset", G + "iron_gauntlets.asset", G + "travelers_cape.asset",
            G + "iron_amulet.asset",
        };

        private static string[] KitFor(string name)
        {
            var kit = new List<string>(SharedArmor);
            switch (name)
            {
                case "Ranged": kit.Add(G + "hunters_bow.asset"); break;
                case "Mage":   kit.Add(G + "willow_staff.asset"); break;
                default:       kit.Add(G + "wooden_shield.asset");
                               kit.Add("Assets/ScriptableObjects/TestGear/Rusty_Sword_Common.asset"); break;
            }
            return kit.ToArray();
        }

        static DevPlaySetup()
        {
            // Defer: AssetDatabase isn't ready inside the static ctor during import.
            EditorApplication.delayCall += ApplyPlayFromHomestead;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode && EditorPrefs.GetBool(AutoEquipKey, true))
                EditorApplication.update += AutoEquipTick; // poll until the player exists
            else if (change == PlayModeStateChange.ExitingPlayMode)
                EditorApplication.update -= AutoEquipTick;
        }

        // Editor-driven so it doesn't depend on the (hidden) DevToolsPanel being
        // active or on game frames advancing. Retries each editor tick until the
        // player exists, equips the selected kit once, then stops.
        private static void AutoEquipTick()
        {
            if (!Application.isPlaying) { EditorApplication.update -= AutoEquipTick; return; }
            if (GameObject.FindGameObjectWithTag("Player") == null) return; // not ready yet
            EquipKit(EditorPrefs.GetString(KitKey, "Melee"));
            EditorApplication.update -= AutoEquipTick;
        }

        // Swap the player's whole loadout to the named kit (editor-side, works in
        // Play): clear equipped + backpack, then equip the kit's gear by asset path.
        private static void EquipKit(string kitName)
        {
            if (!Application.isPlaying) return;
            var player = GameObject.FindGameObjectWithTag("Player");
            var inv = player != null ? player.GetComponent<PlayerInventory>() : null;
            if (inv == null) return;

            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
                inv.UnequipItem(slot);
            foreach (var item in new List<GearItemSO>(inv.Backpack))
                inv.RemoveItem(item);
            foreach (var path in KitFor(kitName))
            {
                var gear = AssetDatabase.LoadAssetAtPath<GearItemSO>(path);
                if (gear != null) inv.EquipItem(gear);
                else Debug.LogWarning($"[Dev] Kit gear not found: {path}");
            }
            Debug.Log($"[Dev] Equipped {kitName} kit.");
        }

        private static void SelectKit(string kitName)
        {
            EditorPrefs.SetString(KitKey, kitName); // remember as the auto-equip default
            EquipKit(kitName);
        }

        private static void ApplyPlayFromHomestead()
        {
            if (EditorPrefs.GetBool(PlayFromHomesteadKey, true))
            {
                var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(HomesteadPath);
                if (scene != null) EditorSceneManager.playModeStartScene = scene;
            }
            else
            {
                EditorSceneManager.playModeStartScene = null;
            }
        }

        [MenuItem(PlayMenu)]
        private static void TogglePlayFromHomestead()
        {
            bool v = !EditorPrefs.GetBool(PlayFromHomesteadKey, true);
            EditorPrefs.SetBool(PlayFromHomesteadKey, v);
            ApplyPlayFromHomestead();
            Debug.Log($"[Dev] Play From Homestead: {(v ? "ON" : "OFF")}");
        }

        [MenuItem(PlayMenu, true)]
        private static bool TogglePlayFromHomesteadValidate()
        {
            Menu.SetChecked(PlayMenu, EditorPrefs.GetBool(PlayFromHomesteadKey, true));
            return true;
        }

        [MenuItem(AutoEquipMenu)]
        private static void ToggleAutoEquip()
        {
            bool v = !EditorPrefs.GetBool(AutoEquipKey, true);
            EditorPrefs.SetBool(AutoEquipKey, v);
            Debug.Log($"[Dev] Auto-Equip Test Gear on Play: {(v ? "ON" : "OFF")}");
        }

        [MenuItem(AutoEquipMenu, true)]
        private static bool ToggleAutoEquipValidate()
        {
            Menu.SetChecked(AutoEquipMenu, EditorPrefs.GetBool(AutoEquipKey, true));
            return true;
        }

        // ── One-click kit swap (also sets the auto-equip default) ──
        [MenuItem(MeleeMenu, false, 20)] private static void EquipMelee() => SelectKit("Melee");
        [MenuItem(RangedMenu, false, 21)] private static void EquipRanged() => SelectKit("Ranged");
        [MenuItem(MageMenu, false, 22)] private static void EquipMage() => SelectKit("Mage");

        [MenuItem(MeleeMenu, true)]
        private static bool EquipMeleeValidate() { Menu.SetChecked(MeleeMenu, Kit() == "Melee"); return Application.isPlaying; }
        [MenuItem(RangedMenu, true)]
        private static bool EquipRangedValidate() { Menu.SetChecked(RangedMenu, Kit() == "Ranged"); return Application.isPlaying; }
        [MenuItem(MageMenu, true)]
        private static bool EquipMageValidate() { Menu.SetChecked(MageMenu, Kit() == "Mage"); return Application.isPlaying; }

        private static string Kit() => EditorPrefs.GetString(KitKey, "Melee");
    }
}
#endif
