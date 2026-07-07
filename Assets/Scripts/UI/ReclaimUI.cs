using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoidBound.Combat;
using VoidBound.Data;
using VoidBound.Homestead;
using VoidBound.Inventory;

namespace VoidBound.UI
{
    // Reclaimer panel (GDD §4A): lists untradable gear from abandoned graves and
    // buys each back for a gold fee. Lives on HUDCanvas; self-builds at first
    // Open() via Panel5cFactory, like the other station panels.
    public class ReclaimUI : MonoBehaviour
    {
        private RectTransform panel;
        private RectTransform list;
        private TextMeshProUGUI goldLabel;

        private ReclaimerStation station;
        private GameObject player;
        private PlayerInventory inventory;
        private PlayerCurrency currency;

        public void Open(ReclaimerStation s, GameObject instigator)
        {
            station = s;
            player = instigator;
            inventory = instigator.GetComponent<PlayerInventory>();
            currency = instigator.GetComponent<PlayerCurrency>();

            Panel5cFactory.CloseOtherHomesteadPanels(gameObject, this);
            EnsureBuilt();
            panel.gameObject.SetActive(true);
            StationProximityCloser.Track(gameObject, this, station, Close);
            Refresh();
        }

        public void Close()
        {
            if (panel != null) panel.gameObject.SetActive(false);
            StationProximityCloser.Untrack(gameObject, this);
        }

        private void EnsureBuilt()
        {
            if (panel != null) return;

            panel = Panel5cFactory.CreatePanel(transform, "ReclaimPanel5c", "RECLAIMER",
                420f, 380f, out var content, out var closeBtn);
            closeBtn.onClick.AddListener(Close);

            var listArea = Panel5cFactory.MakeRect("ListArea", content);
            Panel5cFactory.SetAnchor(listArea, new Vector2(0, 0), new Vector2(1, 1));
            listArea.offsetMin = new Vector2(0, 26);
            listArea.offsetMax = new Vector2(0, -4);
            list = Panel5cFactory.CreateScrollList(listArea, "ReclaimList");
            Panel5cFactory.SetAnchor((RectTransform)list.parent, Vector2.zero, Vector2.one);

            goldLabel = Panel5cFactory.CreateLabel(content, "GoldLabel", "Gold 0",
                12f, Panel5cFactory.Gold);
            Panel5cFactory.SetAnchor(goldLabel.rectTransform, new Vector2(0, 0), new Vector2(1, 0));
            goldLabel.rectTransform.pivot = new Vector2(0.5f, 0f);
            goldLabel.rectTransform.sizeDelta = new Vector2(0, 22);

            panel.gameObject.SetActive(false);
        }

        private void Refresh()
        {
            ClearChildren(list);

            int gold = currency != null ? currency.Gold : 0;
            goldLabel.text = $"Gold {gold}";

            var items = GraveManager.Reclaimable;
            if (items.Count == 0)
            {
                Panel5cFactory.CreateListRow(list, "Nothing to reclaim", "",
                    Panel5cFactory.TextMuted, Panel5cFactory.TextMuted, interactable: false);
                return;
            }

            foreach (var item in items)
            {
                if (item == null) continue;
                var captured = item;
                int fee = station.FeeFor(item);
                bool affordable = gold >= fee;
                var row = Panel5cFactory.CreateListRow(list, item.displayName, $"{fee}g",
                    affordable ? RarityVisualEffects.GetRarityColor(item.rarity) : (Color)Panel5cFactory.TextMuted,
                    affordable ? (Color)Panel5cFactory.Gold : (Color)Panel5cFactory.TextMuted,
                    affordable);
                row.onClick.AddListener(() => Reclaim(captured, fee));
            }
        }

        private void Reclaim(GearItemSO item, int fee)
        {
            if (inventory == null || currency == null) return;
            if (!currency.SpendGold(fee)) return;
            if (!GraveManager.ReclaimItem(item)) { currency.AddGold(fee); return; } // already gone: refund

            inventory.AddItem(item);
            FloatingDamageNumber.SpawnText(player.transform.position,
                $"Reclaimed: {item.displayName} -{fee}g", Panel5cFactory.Gold);
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
