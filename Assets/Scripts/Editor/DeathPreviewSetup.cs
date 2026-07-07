#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoidBound.UI;

namespace VoidBound.Editor
{
    // Adds the "kept on death" preview (GDD §4A) to the persisted HUDCanvas.
    // Toggle in-game with K. Idempotent.
    public static class DeathPreviewSetup
    {
        [MenuItem("VoidBound/Setup Death Preview")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Homestead.unity");

            var hud = Object.FindAnyObjectByType<HUDManager>();
            if (hud == null) { Debug.LogWarning("[DeathPreview] HUDManager not found."); return; }

            if (hud.GetComponent<DeathPreviewUI>() == null)
                hud.gameObject.AddComponent<DeathPreviewUI>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[DeathPreview] DeathPreviewUI added to HUD (toggle with K).");
        }

        public static void RunFromBatch() => Run();
    }
}
#endif
