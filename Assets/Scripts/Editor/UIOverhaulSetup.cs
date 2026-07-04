#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VoidBound.Skilling;
using VoidBound.UI;

namespace VoidBound.Editor
{
    // Polish pass 2 orchestrator: generates the rounded UI sprites, retires
    // the legacy scene-wired CraftingPanel (CraftingUI now self-builds on
    // HUDCanvas like the other station panels), restyles the HUD buttons and
    // minimap frame, and re-runs the Phase 5c builder (which now produces the
    // rounded panels + merged PlayerInfoBar). Idempotent.
    public static class UIOverhaulSetup
    {
        [MenuItem("VoidBound/Polish - UI Overhaul Setup")]
        public static void Run()
        {
            UISpriteGenerator.Generate();

            EditorSceneManager.OpenScene("Assets/Scenes/Homestead.unity");
            var hudCanvas = GameObject.Find("HUDCanvas");
            if (hudCanvas == null) { Debug.LogError("[UIOverhaul] HUDCanvas not found."); return; }

            RetireLegacyCraftingPanel(hudCanvas);
            RestyleHudButtons(hudCanvas);
            FrameMinimap(hudCanvas);

            // Rebuilds Equipment/Inventory panels + PlayerInfoBar (rounded,
            // StatsPanel absorbed) and saves ref repoints. Marks scene dirty.
            Phase5cUIBuilder.BuildUI();

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UIOverhaul] UI overhaul applied.");
        }

        public static void RunFromBatch() => Run();

        private static void RetireLegacyCraftingPanel(GameObject hudCanvas)
        {
            // Old Phase 5 panel lived at MobileControls/CraftingPanel with the
            // CraftingUI component on it. The rewritten CraftingUI self-builds,
            // so the whole legacy object goes; the component moves to HUDCanvas.
            var old = Object.FindAnyObjectByType<CraftingUI>(FindObjectsInactive.Include);
            if (old != null && old.gameObject != hudCanvas)
                Object.DestroyImmediate(old.gameObject);

            if (hudCanvas.GetComponent<CraftingUI>() == null)
                hudCanvas.AddComponent<CraftingUI>();
        }

        private static void RestyleHudButtons(GameObject hudCanvas)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/button_rounded.png");
            foreach (var name in new[] { "EquipBtn", "BagBtn", "DevBtn" })
            {
                var t = FindDeep(hudCanvas.transform, name);
                if (t == null) continue;

                var img = t.GetComponent<Image>();
                if (img != null)
                {
                    if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Sliced; }
                    img.color = Color.white;
                }
                var btn = t.GetComponent<Button>();
                if (btn != null)
                {
                    btn.targetGraphic = img;
                    btn.colors = Panel5cFactory.MakeColors(
                        Panel5cFactory.RowBg, Panel5cFactory.RowHover, Panel5cFactory.RowPressed);
                }
                var label = t.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.color = Panel5cFactory.Gold;
                    label.fontStyle = FontStyle.Bold;
                }
            }
        }

        private static void FrameMinimap(GameObject hudCanvas)
        {
            var raw = hudCanvas.GetComponentInChildren<RawImage>(true);
            if (raw == null) return;
            var rt = (RectTransform)raw.transform;
            if (rt.parent.Find("MinimapFrame") != null) return; // idempotent

            var frame = new GameObject("MinimapFrame", typeof(RectTransform), typeof(Image));
            var frt = (RectTransform)frame.transform;
            frt.SetParent(rt.parent, false);
            frt.anchorMin = rt.anchorMin;
            frt.anchorMax = rt.anchorMax;
            frt.pivot = rt.pivot;
            frt.anchoredPosition = rt.anchoredPosition;
            frt.sizeDelta = rt.sizeDelta + new Vector2(10, 10);
            frt.SetSiblingIndex(rt.GetSiblingIndex()); // behind the map

            var img = frame.GetComponent<Image>();
            img.color = Panel5cFactory.PanelBg;
            img.raycastTarget = false;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/panel_rounded.png");
            if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Sliced; }
            var shadow = frame.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(4f, -4f);
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform child in root)
            {
                if (child.name == name) return child;
                var found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
#endif
