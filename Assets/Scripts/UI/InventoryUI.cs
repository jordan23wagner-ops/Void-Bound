using UnityEngine;
using UnityEngine.UI;
using VoidBound.Data;
using VoidBound.Inventory;

namespace VoidBound.UI
{
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private GameObject panel;
        [SerializeField] private Transform slotsParent;
        [SerializeField] private Transform backpackParent;
        [SerializeField] private GameObject slotButtonPrefab;

        private bool isOpen;

        private void Start()
        {
            if (panel != null)
                panel.SetActive(false);

            if (inventory != null)
                inventory.OnInventoryChanged += Refresh;
        }

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.tabKey.wasPressedThisFrame)
            {
                Toggle();
            }
        }

        public void Toggle()
        {
            isOpen = !isOpen;
            if (panel != null)
                panel.SetActive(isOpen);

            if (isOpen)
                Refresh();
        }

        private void Refresh()
        {
            if (!isOpen || inventory == null) return;

            ClearChildren(slotsParent);
            ClearChildren(backpackParent);

            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
            {
                var item = inventory.GetEquipped(slot);
                CreateSlotButton(slotsParent, slot.ToString(), item, () =>
                {
                    if (item != null) inventory.UnequipItem(slot);
                });
            }

            foreach (var item in inventory.Backpack)
            {
                var capturedItem = item;
                CreateSlotButton(backpackParent, item.displayName, item, () =>
                {
                    inventory.EquipItem(capturedItem);
                });
            }
        }

        private void CreateSlotButton(Transform parent, string label, GearItemSO item, System.Action onClick)
        {
            if (parent == null) return;

            var btnObj = new GameObject(label);
            btnObj.transform.SetParent(parent, false);

            var rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(140f, 36f);

            var image = btnObj.AddComponent<Image>();
            if (item != null)
                image.color = RarityVisualEffects.GetRarityColor(item.rarity);
            else
                image.color = new Color(0.3f, 0.3f, 0.3f, 0.7f);

            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4f, 2f);
            textRect.offsetMax = new Vector2(-4f, -2f);

            var text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.text = item != null ? $"{label}: {item.displayName}" : $"{label}: ---";
        }

        private void ClearChildren(Transform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }

        private void OnDestroy()
        {
            if (inventory != null)
                inventory.OnInventoryChanged -= Refresh;
        }
    }
}
