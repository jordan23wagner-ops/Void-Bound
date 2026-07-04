using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoidBound.Data;
using VoidBound.Inventory;

namespace VoidBound.UI
{
    // Merchant buy/sell panel. Lives on HUDCanvas; builds itself at first
    // Open() using the Phase 5c visual language via Panel5cFactory.
    public class MerchantUI : MonoBehaviour
    {
        // Tunable: sell price as a fraction of an item's goldValue
        public const float SellRatio = 0.4f;

        private RectTransform panel;
        private RectTransform buyList;
        private RectTransform sellList;
        private TextMeshProUGUI goldLabel;

        private ShopInventorySO shop;
        private PlayerCurrency currency;
        private PlayerInventory inventory;
        private Skilling.MaterialInventory materials;

        public void Open(ShopInventorySO shopInventory, GameObject instigator)
        {
            shop = shopInventory;
            currency = instigator.GetComponent<PlayerCurrency>();
            inventory = instigator.GetComponent<PlayerInventory>();
            materials = instigator.GetComponent<Skilling.MaterialInventory>();

            Panel5cFactory.CloseOtherHomesteadPanels(gameObject, this);
            EnsureBuilt();
            panel.gameObject.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (panel != null) panel.gameObject.SetActive(false);
        }

        private void EnsureBuilt()
        {
            if (panel != null) return;

            panel = Panel5cFactory.CreatePanel(transform, "MerchantPanel", "MERCHANT",
                520f, 420f, out var content, out var closeBtn);
            closeBtn.onClick.AddListener(Close);

            var buyHeader = Panel5cFactory.CreateLabel(content, "BuyHeader", "BUY",
                11f, Panel5cFactory.TextMuted);
            Panel5cFactory.SetAnchor(buyHeader.rectTransform, new Vector2(0, 1), new Vector2(0.5f, 1));
            buyHeader.rectTransform.pivot = new Vector2(0.5f, 1f);
            buyHeader.rectTransform.sizeDelta = new Vector2(-8, 18);
            buyHeader.characterSpacing = 3f;

            var sellHeader = Panel5cFactory.CreateLabel(content, "SellHeader", "SELL",
                11f, Panel5cFactory.TextMuted);
            Panel5cFactory.SetAnchor(sellHeader.rectTransform, new Vector2(0.5f, 1), new Vector2(1, 1));
            sellHeader.rectTransform.pivot = new Vector2(0.5f, 1f);
            sellHeader.rectTransform.sizeDelta = new Vector2(-8, 18);
            sellHeader.characterSpacing = 3f;

            var buyViewport = Panel5cFactory.MakeRect("BuyArea", content);
            Panel5cFactory.SetAnchor(buyViewport, new Vector2(0, 0), new Vector2(0.5f, 1));
            buyViewport.offsetMin = new Vector2(0, 28);
            buyViewport.offsetMax = new Vector2(-4, -22);
            buyList = Panel5cFactory.CreateScrollList(buyViewport, "BuyList");
            Panel5cFactory.SetAnchor((RectTransform)buyList.parent, Vector2.zero, Vector2.one);

            var sellViewport = Panel5cFactory.MakeRect("SellArea", content);
            Panel5cFactory.SetAnchor(sellViewport, new Vector2(0.5f, 0), new Vector2(1, 1));
            sellViewport.offsetMin = new Vector2(4, 28);
            sellViewport.offsetMax = new Vector2(0, -22);
            sellList = Panel5cFactory.CreateScrollList(sellViewport, "SellList");
            Panel5cFactory.SetAnchor((RectTransform)sellList.parent, Vector2.zero, Vector2.one);

            goldLabel = Panel5cFactory.CreateLabel(content, "GoldLabel", "Gold 0",
                12f, Panel5cFactory.Gold);
            Panel5cFactory.SetAnchor(goldLabel.rectTransform, new Vector2(0, 0), new Vector2(1, 0));
            goldLabel.rectTransform.pivot = new Vector2(0.5f, 0f);
            goldLabel.rectTransform.sizeDelta = new Vector2(0, 22);
        }

        private void Refresh()
        {
            ClearChildren(buyList);
            ClearChildren(sellList);

            int gold = currency != null ? currency.Gold : 0;
            goldLabel.text = $"Gold {gold}";

            // BUY side — from the shop asset
            if (shop != null && shop.stock != null)
            {
                foreach (var entry in shop.stock)
                {
                    if (entry == null || !entry.IsValid) continue;
                    var captured = entry;
                    bool affordable = gold >= entry.price;
                    string name = entry.material != null && entry.materialQuantity > 1
                        ? $"{entry.DisplayName} ×{entry.materialQuantity}"
                        : entry.DisplayName;
                    Color nameColor = entry.gear != null
                        ? RarityVisualEffects.GetRarityColor(entry.gear.rarity)
                        : Panel5cFactory.TextPrimary;

                    var row = Panel5cFactory.CreateListRow(buyList, name, $"{entry.price}g",
                        affordable ? nameColor : (Color)Panel5cFactory.TextMuted,
                        affordable ? (Color)Panel5cFactory.Gold : (Color)Panel5cFactory.TextMuted,
                        affordable);
                    row.onClick.AddListener(() => Buy(captured));
                }
            }

            // SELL side — sellable backpack items grouped into stacks
            if (inventory != null)
            {
                var order = new List<GearItemSO>();
                var counts = new Dictionary<GearItemSO, int>();
                foreach (var item in inventory.Backpack)
                {
                    if (item == null || item.goldValue <= 0) continue;
                    if (counts.TryGetValue(item, out int c)) counts[item] = c + 1;
                    else { counts[item] = 1; order.Add(item); }
                }

                foreach (var item in order)
                {
                    var captured = item;
                    int sellPrice = Mathf.Max(1, Mathf.FloorToInt(item.goldValue * SellRatio));
                    string name = counts[item] > 1 ? $"{item.displayName} ×{counts[item]}" : item.displayName;
                    var row = Panel5cFactory.CreateListRow(sellList, name, $"+{sellPrice}g",
                        RarityVisualEffects.GetRarityColor(item.rarity), Panel5cFactory.Green);
                    row.onClick.AddListener(() => Sell(captured, sellPrice));
                }
            }
        }

        private void Buy(ShopInventorySO.ShopEntry entry)
        {
            if (currency == null || !currency.SpendGold(entry.price)) return;

            if (entry.gear != null)
                inventory?.AddItem(entry.gear);
            else if (entry.material != null)
                materials?.AddMaterial(entry.material, Mathf.Max(1, entry.materialQuantity));

            Combat.FloatingDamageNumber.SpawnText(currency.transform.position,
                $"Bought: {entry.DisplayName}", Panel5cFactory.Gold);
            Refresh();
        }

        private void Sell(GearItemSO item, int sellPrice)
        {
            if (inventory == null || currency == null) return;
            if (!inventory.RemoveItem(item)) return;

            currency.AddGold(sellPrice);
            Combat.FloatingDamageNumber.SpawnText(currency.transform.position,
                $"Sold: {item.displayName} +{sellPrice}g", Panel5cFactory.Green);
            Refresh();
        }

        private static void ClearChildren(Transform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }
    }
}
