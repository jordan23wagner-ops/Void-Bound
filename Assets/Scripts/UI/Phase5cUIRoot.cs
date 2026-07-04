using UnityEngine;

namespace VoidBound.UI
{
    // Visibility manager for the Phase 5c UI group (UIRoot5c).
    // Equipment and Inventory are two distinct windows; the root stays active
    // while at least one of them is open.
    public class Phase5cUIRoot : MonoBehaviour
    {
        [SerializeField] private GameObject equipmentPanel;
        [SerializeField] private GameObject inventoryPanel;

        private void EnsureRefs()
        {
            if (equipmentPanel == null)
            {
                var t = transform.Find("EquipmentPanel");
                if (t != null) equipmentPanel = t.gameObject;
                else Debug.LogWarning("[Phase5cUIRoot] EquipmentPanel not found under UIRoot5c.");
            }
            if (inventoryPanel == null)
            {
                var t = transform.Find("InventoryPanel");
                if (t != null) inventoryPanel = t.gameObject;
                else Debug.LogWarning("[Phase5cUIRoot] InventoryPanel not found under UIRoot5c.");
            }
        }

        public void ToggleEquipment() => TogglePanel(equipment: true);
        public void ToggleInventory() => TogglePanel(equipment: false);

        private void TogglePanel(bool equipment)
        {
            EnsureRefs();
            var target = equipment ? equipmentPanel : inventoryPanel;
            if (target == null) return;

            bool visible = gameObject.activeSelf && target.activeSelf;
            if (visible)
            {
                target.SetActive(false);
            }
            else
            {
                if (!gameObject.activeSelf)
                {
                    // Fresh open: show only the requested panel.
                    if (equipmentPanel != null) equipmentPanel.SetActive(false);
                    if (inventoryPanel != null) inventoryPanel.SetActive(false);
                    gameObject.SetActive(true);
                }
                target.SetActive(true);
            }
            UpdateRootActive();
        }

        public void ClosePanel(GameObject panel)
        {
            if (panel != null) panel.SetActive(false);
            UpdateRootActive();
        }

        public void CloseAll()
        {
            EnsureRefs();
            if (equipmentPanel != null) equipmentPanel.SetActive(false);
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
            gameObject.SetActive(false);
        }

        private void UpdateRootActive()
        {
            bool any = (equipmentPanel != null && equipmentPanel.activeSelf)
                    || (inventoryPanel != null && inventoryPanel.activeSelf);
            gameObject.SetActive(any);
        }
    }
}
