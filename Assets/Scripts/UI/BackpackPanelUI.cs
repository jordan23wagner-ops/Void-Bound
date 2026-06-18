using UnityEngine;
using UnityEngine.UI;
using VoidBound.Data;
using VoidBound.Inventory;
using VoidBound.Skilling;

namespace VoidBound.UI
{
    public class BackpackPanelUI : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private Transform gridContainer;
        [SerializeField] private Text capacityText;
        [SerializeField] private Text currencyText;
        [SerializeField] private PlayerCurrency currency;
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private Text detailName;
        [SerializeField] private Text detailRarity;
        [SerializeField] private Text detailSlot;
        [SerializeField] private Text detailStats;
        [SerializeField] private Button equipButton;
        [SerializeField] private Button closeButton;

        [SerializeField] private int maxSlots = 30;

        private GearItemSO selectedItem;

        private void Start()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        }

        private void OnEnable()
        {
            if (inventory != null) inventory.OnInventoryChanged += Refresh;
            if (currency != null) currency.OnCurrencyChanged += RefreshCurrency;
            Refresh();
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.OnInventoryChanged -= Refresh;
            if (currency != null) currency.OnCurrencyChanged -= RefreshCurrency;
        }

        public void Refresh()
        {
            ClearChildren(gridContainer);
            if (detailPanel != null) detailPanel.SetActive(false);

            if (inventory == null) return;

            int index = 0;
            foreach (var item in inventory.Backpack)
            {
                var captured = item;
                CreateGridSlot(item, () => ShowDetail(captured));
                index++;
            }

            for (int i = index; i < maxSlots; i++)
                CreateEmptySlot();

            if (capacityText != null)
                capacityText.text = $"{inventory.Backpack.Count}/{maxSlots}";

            RefreshCurrency();
        }

        private void RefreshCurrency()
        {
            if (currencyText == null) return;
            if (currency != null)
                currencyText.text = $"Gold: {currency.Gold}    Shards: {currency.VoidShards}";
            else
                currencyText.text = "Gold: 0";
        }

        private void CreateGridSlot(GearItemSO item, System.Action onClick)
        {
            if (gridContainer == null) return;

            var obj = new GameObject(item.displayName);
            obj.transform.SetParent(gridContainer, false);
            var le = obj.AddComponent<LayoutElement>();
            le.preferredHeight = 52f;
            le.preferredWidth = 52f;

            var bg = obj.AddComponent<Image>();
            Color rarityColor = RarityVisualEffects.GetRarityColor(item.rarity);
            bg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

            var border = new GameObject("Border");
            border.transform.SetParent(obj.transform, false);
            var bRect = border.AddComponent<RectTransform>();
            bRect.anchorMin = Vector2.zero;
            bRect.anchorMax = Vector2.one;
            bRect.offsetMin = Vector2.zero;
            bRect.offsetMax = Vector2.zero;
            border.AddComponent<Image>().color = rarityColor;
            var outline = border.AddComponent<Outline>();
            outline.effectColor = rarityColor;
            outline.effectDistance = new Vector2(2f, 2f);

            var inner = new GameObject("Inner");
            inner.transform.SetParent(border.transform, false);
            var iRect = inner.AddComponent<RectTransform>();
            iRect.anchorMin = Vector2.zero;
            iRect.anchorMax = Vector2.one;
            iRect.offsetMin = new Vector2(3f, 3f);
            iRect.offsetMax = new Vector2(-3f, -3f);
            inner.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.16f, 1f);

            var label = new GameObject("Label");
            label.transform.SetParent(obj.transform, false);
            var lRect = label.AddComponent<RectTransform>();
            lRect.anchorMin = Vector2.zero;
            lRect.anchorMax = Vector2.one;
            lRect.offsetMin = new Vector2(2f, 2f);
            lRect.offsetMax = new Vector2(-2f, -2f);
            var t = label.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 8;
            t.alignment = TextAnchor.LowerCenter;
            t.color = Color.white;
            t.text = item.displayName;

            var btn = obj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());
        }

        private void CreateEmptySlot()
        {
            if (gridContainer == null) return;

            var obj = new GameObject("Empty");
            obj.transform.SetParent(gridContainer, false);
            var le = obj.AddComponent<LayoutElement>();
            le.preferredHeight = 52f;
            le.preferredWidth = 52f;
            obj.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.15f, 0.6f);
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
                    ? $"{item.slot} ({item.weaponType})" : item.slot.ToString();
            if (detailStats != null)
            {
                var m = item.statModifiers;
                detailStats.text = $"STR +{m.str}  DEX +{m.dex}\nVIG +{m.vig}  INT +{m.intel}";
                if (item.baseDamage > 0) detailStats.text += $"\nDamage: {item.baseDamage}";
            }
            if (equipButton != null)
            {
                equipButton.onClick.RemoveAllListeners();
                equipButton.onClick.AddListener(() => { inventory?.EquipItem(selectedItem); Refresh(); });
            }
        }

        private void ClearChildren(Transform p)
        {
            if (p == null) return;
            for (int i = p.childCount - 1; i >= 0; i--) Destroy(p.GetChild(i).gameObject);
        }
    }
}
