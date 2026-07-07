#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoidBound.Homestead;
using VoidBound.UI;

namespace VoidBound.Editor
{
    // Wires the Reclaimer (GDD §4A): swaps its PlaceholderStation for the real
    // ReclaimerStation and adds ReclaimUI to the HUD. Idempotent — safe to
    // re-run after Phase 8 rebuilds the NewStations root.
    public static class ReclaimerSetup
    {
        [MenuItem("VoidBound/Setup Reclaimer")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Homestead.unity");

            var reclaimer = GameObject.Find("Reclaimer");
            if (reclaimer == null)
            {
                Debug.LogWarning("[Reclaimer] 'Reclaimer' not found. Run Phase 8 setup first.");
                return;
            }

            var placeholder = reclaimer.GetComponent<PlaceholderStation>();
            if (placeholder != null) Object.DestroyImmediate(placeholder);

            var station = reclaimer.GetComponent<ReclaimerStation>() ?? reclaimer.AddComponent<ReclaimerStation>();
            var so = new SerializedObject(station);
            so.FindProperty("interactPrompt").stringValue = "Reclaim";
            so.FindProperty("interactRange").floatValue = 3f;
            so.FindProperty("tooltipDescription").stringValue =
                "Buy back tools/untradables lost to an abandoned grave, for a gold fee.";
            so.ApplyModifiedPropertiesWithoutUndo();

            if (reclaimer.GetComponent<Collider>() == null)
            {
                var bc = reclaimer.AddComponent<BoxCollider>();
                bc.center = new Vector3(0f, 0.9f, 0f);
                bc.size = new Vector3(1f, 1.8f, 1f);
                bc.isTrigger = true;
            }

            var hud = Object.FindAnyObjectByType<HUDManager>();
            if (hud != null && hud.GetComponent<ReclaimUI>() == null)
                hud.gameObject.AddComponent<ReclaimUI>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Reclaimer] Station swapped and ReclaimUI added.");
        }

        public static void RunFromBatch() => Run();
    }
}
#endif
