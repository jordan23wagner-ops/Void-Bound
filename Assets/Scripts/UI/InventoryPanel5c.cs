using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoidBound.Data;
using VoidBound.Inventory;

namespace VoidBound.UI
{
    // Runtime data binding for the Phase 5c Inventory panel built by Phase5cUIBuilder.
    // Rebuilds the grid from PlayerInventory.Backpack: identical items are grouped
    // into stacks with a ×N badge; remaining cells render as empty slots.
    public class InventoryPanel5c : MonoBehaviour
    {
        private const int MaxSlots = 24;
        private const float BorderWidth = 2f;

        private static readonly Color32 SlotBg      = new(0x0a, 0x0d, 0x0a, 255);
        private static readonly Color32 BorderFaint = new(0x1e, 0x22, 0x1e, 255);

        private PlayerInventory inventory;
        private PlayerCurrency currency;

        private Transform grid;
        private TextMeshProUGUI capacityTMP;
        private TextMeshProUGUI goldTMP;
        private TextMeshProUGUI shardTMP;
        private ItemDetailView5c detailView;
        private Phase5cUIRoot root;
        private bool initialized;

        private void OnEnable()
        {
            EnsureInit();
            if (inventory != null) inventory.OnInventoryChanged += Refresh;
            if (currency != null) currency.OnCurrencyChanged += RefreshCurrency;
            if (detailView != null) detailView.Hide();
            Refresh();
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.OnInventoryChanged -= Refresh;
            if (currency != null) currency.OnCurrencyChanged -= RefreshCurrency;
        }

        private void EnsureInit()
        {
            ResolvePlayer();
            if (initialized) return;

            root = GetComponentInParent<Phase5cUIRoot>(true);

            // Grid may be nested under a scroll viewport (added by the builder patch)
            // or sit directly under the panel (original mockup layout).
            var viewport = transform.Find("GridViewport");
            grid = viewport != null ? viewport.Find("Grid") : transform.Find("Grid");
            if (grid == null)
            {
                Debug.LogError("[InventoryPanel5c] Grid not found — was the panel built by Phase5cUIBuilder?");
                return;
            }

            var capacity = transform.Find("Header/Capacity");
            if (capacity != null) capacityTMP = capacity.GetComponent<TextMeshProUGUI>();

            var footer = transform.Find("Footer");
            if (footer != null)
            {
                goldTMP = footer.Find("Gold")?.GetComponent<TextMeshProUGUI>();
                shardTMP = footer.Find("Shards")?.GetComponent<TextMeshProUGUI>();
            }

            WireCloseButton();
            detailView = ItemDetailView5c.Create((RectTransform)transform);
            initialized = true;
        }

        private void ResolvePlayer()
        {
            if (inventory != null) return;
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("[InventoryPanel5c] No Player found — panel will be empty.");
                return;
            }
            inventory = player.GetComponent<PlayerInventory>();
            currency = player.GetComponent<PlayerCurrency>();
        }

        private void WireCloseButton()
        {
            var closeBtn = transform.Find("Header/CloseBtn");
            if (closeBtn == null) return;
            var button = closeBtn.GetComponent<Button>();
            if (button == null) button = closeBtn.gameObject.AddComponent<Button>();
            var img = closeBtn.GetComponent<Image>();
            if (img != null) img.raycastTarget = true;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (root != null) root.ClosePanel(gameObject);
                else gameObject.SetActive(false);
            });
        }

        public void Refresh()
        {
            if (!initialized) return;

            for (int i = grid.childCount - 1; i >= 0; i--)
                Destroy(grid.GetChild(i).gameObject);

            int itemCount = 0;
            if (inventory != null)
            {
                // Group identical items (same GearItemSO asset) into stacks, first-seen order.
                var stackOrder = new List<GearItemSO>();
                var stackCounts = new Dictionary<GearItemSO, int>();
                foreach (var item in inventory.Backpack)
                {
                    if (item == null) continue;
                    itemCount++;
                    if (stackCounts.TryGetValue(item, out int count))
                        stackCounts[item] = count + 1;
                    else
                    {
                        stackCounts[item] = 1;
                        stackOrder.Add(item);
                    }
                }

                foreach (var item in stackOrder)
                    CreateItemSlot(item, stackCounts[item]);

                for (int i = stackOrder.Count; i < MaxSlots; i++)
                    CreateEmptySlot();
            }

            if (capacityTMP != null)
                capacityTMP.text = $"{itemCount} / {MaxSlots}";

            RefreshCurrency();
        }

        private void RefreshCurrency()
        {
            if (goldTMP != null)
                goldTMP.text = $"Gold {(currency != null ? currency.Gold : 0)}";
            if (shardTMP != null)
                shardTMP.text = $"Shards {(currency != null ? currency.VoidShards : 0)}";
        }

        private void CreateItemSlot(GearItemSO item, int count)
        {
            Color rarityColor = RarityVisualEffects.GetRarityColor(item.rarity);
            var slot = CreateSlotBase("InvSlot_" + item.name, rarityColor);

            var icon = MakeTMP("Icon", slot);
            var iconRT = icon.rectTransform;
            iconRT.anchorMin = new Vector2(0.5f, 0.5f);
            iconRT.anchorMax = new Vector2(0.5f, 0.5f);
            iconRT.anchoredPosition = new Vector2(0, 4);
            iconRT.sizeDelta = new Vector2(56, 56);
            icon.text = IconChar(item);
            icon.fontSize = 22f;
            icon.color = rarityColor;
            icon.alignment = TextAlignmentOptions.Center;

            var label = MakeTMP("Label", slot);
            var labelRT = label.rectTransform;
            labelRT.anchorMin = new Vector2(0, 0);
            labelRT.anchorMax = new Vector2(1, 0);
            labelRT.pivot = new Vector2(0.5f, 0);
            labelRT.anchoredPosition = new Vector2(0, 2);
            labelRT.sizeDelta = new Vector2(-4, 10);
            label.text = item.displayName;
            label.fontSize = 7f;
            label.color = rarityColor;
            label.alignment = TextAlignmentOptions.Center;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.textWrappingMode = TextWrappingModes.NoWrap;

            if (count > 1)
            {
                var badge = MakeTMP("Badge", slot);
                var badgeRT = badge.rectTransform;
                badgeRT.anchorMin = new Vector2(1, 0);
                badgeRT.anchorMax = new Vector2(1, 0);
                badgeRT.pivot = new Vector2(1, 0);
                badgeRT.anchoredPosition = new Vector2(-3, 12);
                badgeRT.sizeDelta = new Vector2(36, 12);
                badge.text = $"×{count}";
                badge.fontSize = 9f;
                badge.fontStyle = FontStyles.Bold;
                badge.color = Color.white;
                badge.alignment = TextAlignmentOptions.Right;
            }

            var button = slot.gameObject.AddComponent<Button>();
            var captured = item;
            var capturedCount = count;
            button.onClick.AddListener(() => ShowDetail(captured, capturedCount));
        }

        private void CreateEmptySlot()
        {
            CreateSlotBase("InvSlot_Empty", BorderFaint);
        }

        private RectTransform CreateSlotBase(string name, Color borderColor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(grid, false);
            var img = go.AddComponent<Image>();
            img.color = SlotBg;
            img.raycastTarget = true;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(BorderWidth, -BorderWidth);
            return (RectTransform)go.transform;
        }

        private static TextMeshProUGUI MakeTMP(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.raycastTarget = false;
            return tmp;
        }

        private static string IconChar(GearItemSO item) => item.slot switch
        {
            EquipmentSlot.Weapon => "W",
            EquipmentSlot.Shield => "S",
            EquipmentSlot.Helm   => "H",
            EquipmentSlot.Body   => "B",
            EquipmentSlot.Legs   => "L",
            EquipmentSlot.Boots  => "Bo",
            EquipmentSlot.Gloves => "G",
            EquipmentSlot.Amulet => "A",
            EquipmentSlot.Ring   => "R",
            EquipmentSlot.Ammo   => "Am",
            EquipmentSlot.Cape   => "C",
            _ => "-"
        };

        private void ShowDetail(GearItemSO item, int count)
        {
            if (detailView == null) return;
            detailView.Show(item, "EQUIP", () => inventory.EquipItem(item), count);
        }
    }
}
