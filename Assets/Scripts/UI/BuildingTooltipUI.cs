using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using VoidBound.Core;

namespace VoidBound.UI
{
    // Hover tooltip for Interactables (Homestead buildings, resource nodes).
    // Raycasts from the mouse each tick; shows name + description + interact
    // prompt near the cursor. Mouse-only by design — touch devices never
    // hover, and the proximity-interact flow is unchanged there.
    public class BuildingTooltipUI : MonoBehaviour
    {
        private const float CheckInterval = 0.1f;
        private const float RayDistance = 200f;
        private static readonly Vector2 CursorOffset = new(18f, -12f);

        private RectTransform panel;
        private TextMeshProUGUI titleTMP;
        private TextMeshProUGUI descTMP;
        private TextMeshProUGUI promptTMP;

        private Interactable current;
        private float nextCheck;

        private void Update()
        {
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + CheckInterval;

            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse == null || cam == null)
            {
                Hide();
                return;
            }

            Vector2 mousePos = mouse.position.ReadValue();
            Interactable hovered = null;
            if (Physics.Raycast(cam.ScreenPointToRay(mousePos), out var hit, RayDistance))
                hovered = hit.collider.GetComponentInParent<Interactable>();

            if (hovered == null || string.IsNullOrEmpty(hovered.TooltipDescription))
            {
                Hide();
                return;
            }

            if (hovered != current)
            {
                current = hovered;
                EnsureBuilt();
                titleTMP.text = hovered.gameObject.name.ToUpperInvariant();
                descTMP.text = hovered.TooltipDescription;
                promptTMP.text = hovered.InteractPrompt;
                panel.gameObject.SetActive(true);
            }

            PositionAtCursor(mousePos);
        }

        private void Hide()
        {
            current = null;
            if (panel != null) panel.gameObject.SetActive(false);
        }

        private void PositionAtCursor(Vector2 screenPos)
        {
            if (panel == null) return;
            var canvasRect = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPos + CursorOffset, null, out var local);
            panel.anchoredPosition = local;
        }

        private void EnsureBuilt()
        {
            if (panel != null) return;

            panel = Panel5cFactory.MakeRect("BuildingTooltip", transform);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0f, 1f); // hangs below-right of the cursor
            panel.sizeDelta = new Vector2(230f, 86f);
            Panel5cFactory.AddImage(panel.gameObject, Panel5cFactory.PanelBg, raycast: false);
            Panel5cFactory.AddOutline(panel.gameObject, Panel5cFactory.PanelBorder);

            titleTMP = MakeText("Title", 12f, Panel5cFactory.TextPrimary, -8f, 18f);
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.characterSpacing = 2f;
            descTMP = MakeText("Desc", 10f, Panel5cFactory.TextPrimary, -28f, 36f);
            descTMP.textWrappingMode = TextWrappingModes.Normal;
            promptTMP = MakeText("Prompt", 9f, Panel5cFactory.Gold, -66f, 14f);
            promptTMP.fontStyle = FontStyles.Italic;

            panel.gameObject.SetActive(false);
        }

        private TextMeshProUGUI MakeText(string name, float size, Color color, float y, float height)
        {
            var tmp = Panel5cFactory.MakeTMP(name, panel); // raycastTarget already false
            var rt = tmp.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, y);
            rt.offsetMin = new Vector2(10, rt.offsetMin.y);
            rt.offsetMax = new Vector2(-10, rt.offsetMax.y);
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            return tmp;
        }
    }
}
