#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using VoidBound.UI;

namespace VoidBound.Editor
{
    // Adds a "New Game" dev button to the DevTools panel that wipes progress and
    // deletes the save (DevToolsPanel.NewGame). Reddish to flag it destructive.
    // Idempotent. Mirrors Phase6's Give-Gold button wiring.
    public static class DevNewGameButtonSetup
    {
        [MenuItem("VoidBound/Setup Dev New Game Button")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Homestead.unity");

            var hud = GameObject.Find("HUDCanvas");
            var devTools = hud != null ? hud.GetComponentInChildren<DevToolsPanel>(true) : null;
            if (devTools == null) { Debug.LogWarning("[DevNewGame] DevToolsPanel not found."); return; }

            var devContent = devTools.transform.Find("DevContent");
            if (devContent == null) { Debug.LogWarning("[DevNewGame] DevContent not found."); return; }

            var existing = devContent.Find("New Game");
            Button button;
            if (existing != null)
            {
                button = existing.GetComponent<Button>();
            }
            else
            {
                var btnGO = new GameObject("New Game");
                btnGO.transform.SetParent(devContent, false);
                btnGO.AddComponent<LayoutElement>().preferredHeight = 44f;
                btnGO.AddComponent<Image>().color = new Color(0.4f, 0.2f, 0.2f, 0.92f); // destructive red
                button = btnGO.AddComponent<Button>();

                var textGO = new GameObject("Label");
                textGO.transform.SetParent(btnGO.transform, false);
                var rect = textGO.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(8f, 0f);
                rect.offsetMax = new Vector2(-8f, 0f);
                var text = textGO.AddComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 15;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;
                text.text = "New Game (wipe + delete save)";
            }

            var so = new SerializedObject(devTools);
            so.FindProperty("newGameButton").objectReferenceValue = button;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[DevNewGame] New Game button added to DevTools and wired.");
        }

        public static void RunFromBatch() => Run();
    }
}
#endif
