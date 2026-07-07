#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoidBound.UI;

namespace VoidBound.Editor
{
    // Adds the "YOU DIED" overlay (GDD §4A) to the persisted HUDCanvas. Shown
    // during the respawn delay with the kept-on-death preview. Idempotent.
    public static class DeathScreenSetup
    {
        [MenuItem("VoidBound/Setup Death Screen")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Homestead.unity");

            var hud = Object.FindAnyObjectByType<HUDManager>();
            if (hud == null) { Debug.LogWarning("[DeathScreen] HUDManager not found."); return; }

            if (hud.GetComponent<DeathScreenUI>() == null)
                hud.gameObject.AddComponent<DeathScreenUI>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[DeathScreen] DeathScreenUI added to HUD.");
        }

        public static void RunFromBatch() => Run();
    }
}
#endif
