using UnityEngine;
using UnityEngine.UI;
using VoidBound.Data;
using VoidBound.Inventory;

namespace VoidBound.UI
{
    public class EquipmentPanelUI : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private Transform slotsContainer;
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private Text detailName;
        [SerializeField] private Text detailRarity;
        [SerializeField] private Text detailSlot;
        [SerializeField] private Text detailStats;
        [SerializeField] private Text detailSet;
        [SerializeField] private Button unequipButton;

        private EquipmentSlot selectedSlot;

        public void Refresh()
        {
            ClearChildren(slotsContainer);
            if (detailPanel != null)
                detailPanel.SetActive(false);

            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
            {
                var item = inventory != null ? inventory.GetEquipped(slot) : null;
                var capturedSlot = slot;
                CreateSlotButton(slot.ToString(), item, () => ShowDetail(capturedSlot));
            }
        }

        private void ShowDetail(EquipmentSlot slot)
        {
            selectedSlot = slot;
            var item = inventory?.GetEquipped(slot);

            if (detailPanel != null)
                detailPanel.SetActive(true);

            if (item != null)
            {
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
                if (detailSet != null)
                    detailSet.text = string.IsNullOrEmpty(item.setId) ? "" : $"Set: {item.setId}";
                if (unequipButton != null)
                {
                    unequipButton.gameObject.SetActive(true);
                    unequipButton.onClick.RemoveAllListeners();
                    unequipButton.onClick.AddListener(() =>
                    {
                        inventory?.UnequipItem(selectedSlot);
                        Refresh();
                    });
                }
            }
            else
            {
                if (detailName != null) detailName.text = "Empty";
                if (detailRarity != null) { detailRarity.text = ""; detailRarity.color = Color.white; }
                if (detailSlot != null) detailSlot.text = slot.ToString();
                if (detailStats != null) detailStats.text = "---";
                if (detailSet != null) detailSet.text = "";
                if (unequipButton != null) unequipButton.gameObject.SetActive(false);
            }
        }

        private void CreateSlotButton(string label, GearItemSO item, System.Action onClick)
        {
            if (slotsContainer == null) return;

            var btnObj = new GameObject(label);
            btnObj.transform.SetParent(slotsContainer, false);
            var le = btnObj.AddComponent<LayoutElement>();
            le.preferredHeight = 38f;

            var image = btnObj.AddComponent<Image>();
            image.color = item != null
                ? RarityVisualEffects.GetRarityColor(item.rarity) * 0.8f
                : new Color(0.25f, 0.25f, 0.3f, 0.9f);

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
            text.text = item != null ? $"{label}: {item.displayName}" : $"{label}: ---";
        }

        private void ClearChildren(Transform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }
    }
}
