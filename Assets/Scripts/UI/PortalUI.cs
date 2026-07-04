using UnityEngine;
using VoidBound.UI;

namespace VoidBound.UI
{
    // Fast Travel Portal destination list — UI STUB per Phase 6 spec.
    // Real scene travel lands with the zone phases; locked rows are visual only.
    public class PortalUI : MonoBehaviour
    {
        private RectTransform panel;
        private RectTransform list;

        public void Open()
        {
            Panel5cFactory.CloseOtherHomesteadPanels(gameObject, this);
            EnsureBuilt();
            panel.gameObject.SetActive(true);
        }

        public void Close()
        {
            if (panel != null) panel.gameObject.SetActive(false);
        }

        private void EnsureBuilt()
        {
            if (panel != null) return;

            panel = Panel5cFactory.CreatePanel(transform, "PortalPanel", "FAST TRAVEL",
                300f, 220f, out var content, out var closeBtn);
            closeBtn.onClick.AddListener(Close);

            list = Panel5cFactory.CreateScrollList(content, "DestinationList");
            Panel5cFactory.SetAnchor((RectTransform)list.parent, Vector2.zero, Vector2.one);

            Panel5cFactory.CreateListRow(list, "Homestead", "HERE",
                Panel5cFactory.Green, Panel5cFactory.TextMuted, interactable: false);
            Panel5cFactory.CreateListRow(list, "Ashfields", "Coming soon",
                Panel5cFactory.TextMuted, Panel5cFactory.TextMuted, interactable: false);
            Panel5cFactory.CreateListRow(list, "Bleakwood", "Coming soon",
                Panel5cFactory.TextMuted, Panel5cFactory.TextMuted, interactable: false);
        }
    }
}
