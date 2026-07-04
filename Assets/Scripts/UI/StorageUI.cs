using System.Collections.Generic;
using UnityEngine;
using TMPro;
using VoidBound.Data;
using VoidBound.Inventory;

namespace VoidBound.UI
{
    // Storage Chest (bank) panel: storage list and backpack list side by side,
    // tap an item to transfer it. Stacks identical items with ×N like the
    // Phase 5c inventory grid.
    public class StorageUI : MonoBehaviour
    {
        private RectTransform panel;
        private RectTransform storageList;
        private RectTransform backpackList;
        private TextMeshProUGUI capacityLabel;

        private PlayerStorage storage;
        private PlayerInventory inventory;

        public void Open(GameObject instigator)
        {
            storage = instigator.GetComponent<PlayerStorage>();
            inventory = instigator.GetComponent<PlayerInventory>();
            if (storage == null)
            {
                Debug.LogWarning("[StorageUI] Player has no PlayerStorage component.");
                return;
            }

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

            panel = Panel5cFactory.CreatePanel(transform, "StoragePanel", "STORAGE",
                520f, 420f, out var content, out var closeBtn);
            closeBtn.onClick.AddListener(Close);

            var storageHeader = Panel5cFactory.CreateLabel(content, "StorageHeader", "STORED",
                11f, Panel5cFactory.TextMuted);
            Panel5cFactory.SetAnchor(storageHeader.rectTransform, new Vector2(0, 1), new Vector2(0.5f, 1));
            storageHeader.rectTransform.pivot = new Vector2(0.5f, 1f);
            storageHeader.rectTransform.sizeDelta = new Vector2(-8, 18);
            storageHeader.characterSpacing = 3f;

            var packHeader = Panel5cFactory.CreateLabel(content, "PackHeader", "BACKPACK",
                11f, Panel5cFactory.TextMuted);
            Panel5cFactory.SetAnchor(packHeader.rectTransform, new Vector2(0.5f, 1), new Vector2(1, 1));
            packHeader.rectTransform.pivot = new Vector2(0.5f, 1f);
            packHeader.rectTransform.sizeDelta = new Vector2(-8, 18);
            packHeader.characterSpacing = 3f;

            var storageArea = Panel5cFactory.MakeRect("StorageArea", content);
            Panel5cFactory.SetAnchor(storageArea, new Vector2(0, 0), new Vector2(0.5f, 1));
            storageArea.offsetMin = new Vector2(0, 28);
            storageArea.offsetMax = new Vector2(-4, -22);
            storageList = Panel5cFactory.CreateScrollList(storageArea, "StorageList");
            Panel5cFactory.SetAnchor((RectTransform)storageList.parent, Vector2.zero, Vector2.one);

            var packArea = Panel5cFactory.MakeRect("PackArea", content);
            Panel5cFactory.SetAnchor(packArea, new Vector2(0.5f, 0), new Vector2(1, 1));
            packArea.offsetMin = new Vector2(4, 28);
            packArea.offsetMax = new Vector2(0, -22);
            backpackList = Panel5cFactory.CreateScrollList(packArea, "BackpackList");
            Panel5cFactory.SetAnchor((RectTransform)backpackList.parent, Vector2.zero, Vector2.one);

            capacityLabel = Panel5cFactory.CreateLabel(content, "CapacityLabel", "",
                11f, Panel5cFactory.TextMuted);
            Panel5cFactory.SetAnchor(capacityLabel.rectTransform, new Vector2(0, 0), new Vector2(1, 0));
            capacityLabel.rectTransform.pivot = new Vector2(0.5f, 0f);
            capacityLabel.rectTransform.sizeDelta = new Vector2(0, 22);
        }

        private void Refresh()
        {
            ClearChildren(storageList);
            ClearChildren(backpackList);

            BuildStackedList(storageList, storage.Stored, item =>
            {
                if (!storage.Withdraw(item))
                    Combat.FloatingDamageNumber.SpawnText(storage.transform.position,
                        "Backpack full!", Panel5cFactory.XIcon);
                Refresh();
            });

            BuildStackedList(backpackList, inventory != null ? inventory.Backpack : null, item =>
            {
                if (!storage.Deposit(item))
                    Combat.FloatingDamageNumber.SpawnText(storage.transform.position,
                        "Storage full!", Panel5cFactory.XIcon);
                Refresh();
            });

            int packCount = inventory != null ? inventory.Backpack.Count : 0;
            capacityLabel.text = $"Stored {storage.Stored.Count} / {PlayerStorage.StorageCap}    " +
                                 $"Backpack {packCount} / {PlayerStorage.BackpackCap}";
        }

        private static void BuildStackedList(RectTransform list, IReadOnlyList<GearItemSO> items,
                                             System.Action<GearItemSO> onClick)
        {
            if (items == null) return;

            var order = new List<GearItemSO>();
            var counts = new Dictionary<GearItemSO, int>();
            foreach (var item in items)
            {
                if (item == null) continue;
                if (counts.TryGetValue(item, out int c)) counts[item] = c + 1;
                else { counts[item] = 1; order.Add(item); }
            }

            foreach (var item in order)
            {
                var captured = item;
                string name = counts[item] > 1 ? $"{item.displayName} ×{counts[item]}" : item.displayName;
                var row = Panel5cFactory.CreateListRow(list, name, "",
                    RarityVisualEffects.GetRarityColor(item.rarity), Panel5cFactory.TextMuted);
                row.onClick.AddListener(() => onClick(captured));
            }
        }

        private static void ClearChildren(Transform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }
    }
}
