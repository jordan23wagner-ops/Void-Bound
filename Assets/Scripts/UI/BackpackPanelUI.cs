using UnityEngine;
using UnityEngine.UI;
using VoidBound.Data;
using VoidBound.Inventory;

namespace VoidBound.UI
{
    public class BackpackPanelUI : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private Transform itemsContainer;
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private Text detailName;
        [SerializeField] private Text detailRarity;
        [SerializeField] private Text detailSlot;
        [SerializeField] private Text detailStats;
        [SerializeField] private Button equipButton;

        private GearItemSO selectedItem;

        private void OnEnable()
        {
            if (inventory != null)
                inventory.OnInventoryChanged += Refresh;
        }

        private void OnDisable()
        {
            if (inventory != null)
                inventory.OnInventoryChanged -= Refresh;
        }

        public void Refresh()
        {
            ClearChildren(itemsContainer);
            if (detailPanel != null)
                detailPanel.SetActive(false);

            if (inventory == null) return;

            foreach (var item in inventory.Backpack)
            {
                var captured = item;
                CreateItemButton(item, () => ShowDetail(captured));
            }
        }

        private void ShowDetail(GearItemSO item)
        {
            selectedItem = item;
            if (detailPanel != null) detailPanel.SetActive(true);
            if (detailName != null) detailName.text = item.displayName;
            if (detailRarity != null)
            {
                detailRarity.text = item.rarity.ToString();
                detailRarity.color = RarityVisualEffects.GetRarityColor(item.rarity);
            }
            if (detailSlot != null)
                detailSlot.text = item.slot == EquipmentSlot.Weapon
                    ? $"{item.slot} ({item.weaponType})"
                    : item.slot.ToString();
            if (detailStats != null)
            {
                var m = item.statModifiers;
                detailStats.text = $"STR +{m.str}  DEX +{m.dex}\nVIG +{m.vig}  INT +{m.intel}";
                if (item.baseDamage > 0)
                    detailStats.text += $"\nDamage: {item.baseDamage}";
            }
            if (equipButton != null)
            {
                equipButton.onClick.RemoveAllListeners();
                equipButton.onClick.AddListener(() =>
                {
                    inventory?.EquipItem(selectedItem);
                    Refresh();
                });
            }
        }

        private void CreateItemButton(GearItemSO item, System.Action onClick)
        {
            if (itemsContainer == null) return;

            var btnObj = new GameObject(item.displayName);
            btnObj.transform.SetParent(itemsContainer, false);
            var le = btnObj.AddComponent<LayoutElement>();
            le.preferredHeight = 38f;

            var image = btnObj.AddComponent<Image>();
            image.color = RarityVisualEffects.GetRarityColor(item.rarity) * 0.8f;

            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(6f, 0f);
            textRect.offsetMax = new Vector2(-6f, 0f);

            var text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 13;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.text = $"{item.displayName} [{item.rarity}]";
        }

        private void ClearChildren(Transform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }
    }
}
