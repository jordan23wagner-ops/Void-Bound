#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VoidBound.Editor
{
    // Developer play-mode conveniences (editor-only, toggles persisted per-user
    // via EditorPrefs — nothing ships in a build):
    //   1. Play always boots the Homestead scene, so pressing Play from Ashfields
    //      (or any zone that has no camera of its own) still works — the persisted
    //      Player/Camera/HUD come from Homestead's GameBootstrap.
    //   2. Auto-equip the dev test-gear kit on play start (honored by
    //      DevToolsPanel) so you don't re-equip every piece by hand each session.
    // Toggle either from the VoidBound menu.
    [InitializeOnLoad]
    public static class DevPlaySetup
    {
        private const string HomesteadPath = "Assets/Scenes/Homestead.unity";
        private const string PlayFromHomesteadKey = "VoidBound.PlayFromHomestead";
        public const string AutoEquipKey = "VoidBound.AutoEquipTestGear";

        private const string PlayMenu = "VoidBound/Dev - Play From Homestead";
        private const string AutoEquipMenu = "VoidBound/Dev - Auto-Equip Test Gear on Play";

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
        // player + panel exist, equips the kit once, then stops.
        private static void AutoEquipTick()
        {
            if (!Application.isPlaying) { EditorApplication.update -= AutoEquipTick; return; }
            var dev = Object.FindAnyObjectByType<VoidBound.UI.DevToolsPanel>(FindObjectsInactive.Include);
            var player = GameObject.FindGameObjectWithTag("Player");
            if (dev == null || player == null) return; // not ready yet — keep polling
            dev.EquipTestGearKit();
            EditorApplication.update -= AutoEquipTick;
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
    }
}
#endif
