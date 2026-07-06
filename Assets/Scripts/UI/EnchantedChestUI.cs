using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoidBound.Combat;
using VoidBound.Data;
using VoidBound.Homestead;
using VoidBound.Inventory;
using VoidBound.Skilling;

namespace VoidBound.UI
{
    // Enchanted Chest panel: lists owned untradables, and upgrades the selected
    // one up the rarity ladder via the time-vs-risk timer (GDD §5.6). Start
    // consumes bars; Complete Now rolls success = current progress; waiting the
    // full timer is guaranteed. Self-builds from Panel5cFactory.
    public class EnchantedChestUI : MonoBehaviour
    {
        private RectTransform panel;
        private RectTransform list;
        private TextMeshProUGUI detail;
        private Button actionBtn;
        private TextMeshProUGUI actionLabel;

        private EnchantedChestStation station;
        private GameObject player;
        private PlayerInventory inv;
        private MaterialInventory matInv;
        private PlayerUpgrades upg;
        private GearItemSO selected;

        public void Open(EnchantedChestStation s, GameObject instigator)
        {
            station = s;
            player = instigator;
            inv = instigator.GetComponent<PlayerInventory>();
            matInv = instigator.GetComponent<MaterialInventory>();
            upg = instigator.GetComponent<PlayerUpgrades>();

            Panel5cFactory.CloseOtherHomesteadPanels(gameObject, this);
            EnsureBuilt();
            panel.gameObject.SetActive(true);
            StationProximityCloser.Track(gameObject, this, station, Close);
            RefreshList();
        }

        public void Close()
        {
            if (panel != null) panel.gameObject.SetActive(false);
            StationProximityCloser.Untrack(gameObject, this);
            selected = null;
        }

        private void EnsureBuilt()
        {
            if (panel != null) return;
            panel = Panel5cFactory.CreatePanel(transform, "EnchantedChestPanel5c", "ENCHANTED CHEST",
                540f, 380f, out var content, out var closeBtn);
            closeBtn.onClick.AddListener(Close);

            var listArea = Panel5cFactory.MakeRect("ListArea", content);
            Panel5cFactory.SetAnchor(listArea, new Vector2(0, 0), new Vector2(0.46f, 1));
            listArea.offsetMin = Vector2.zero; listArea.offsetMax = new Vector2(-4, 0);
            list = Panel5cFactory.CreateScrollList(listArea, "UntradableList");
            Panel5cFactory.SetAnchor((RectTransform)list.parent, Vector2.zero, Vector2.one);

            var detailArea = Panel5cFactory.MakeRect("DetailArea", content);
            Panel5cFactory.SetAnchor(detailArea, new Vector2(0.46f, 0), new Vector2(1, 1));
            detailArea.offsetMin = new Vector2(4, 0); detailArea.offsetMax = Vector2.zero;
            Panel5cFactory.AddPanelBg(detailArea.gameObject, Panel5cFactory.SlotBg, raycast: false);

            detail = Panel5cFactory.CreateLabel(detailArea, "Detail", "Select an untradable", 11f,
                Panel5cFactory.TextPrimary);
            Panel5cFactory.SetAnchor(detail.rectTransform, Vector2.zero, Vector2.one);
            detail.rectTransform.offsetMin = new Vector2(12, 52);
            detail.rectTransform.offsetMax = new Vector2(-12, -10);
            detail.alignment = TextAlignmentOptions.TopLeft;
            detail.textWrappingMode = TextWrappingModes.Normal;

            actionBtn = Panel5cFactory.CreateActionButton(detailArea, "UPGRADE");
            var brt = (RectTransform)actionBtn.transform;
            Panel5cFactory.SetAnchor(brt, new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            brt.pivot = new Vector2(0.5f, 0f);
            brt.sizeDelta = new Vector2(160, 32);
            brt.anchoredPosition = new Vector2(0, 10);
            actionLabel = actionBtn.GetComponentInChildren<TextMeshProUGUI>();
            actionBtn.onClick.AddListener(DoAction);
            actionBtn.gameObject.SetActive(false);

            panel.gameObject.SetActive(false);
        }

        private void RefreshList()
        {
            if (inv == null) return;
            for (int i = list.childCount - 1; i >= 0; i--) Destroy(list.GetChild(i).gameObject);

            var seen = new HashSet<string>();
            var items = new List<GearItemSO>();
            foreach (var kv in inv.Equipped)
                if (kv.Value != null && kv.Value.untradable && seen.Add(kv.Value.itemId)) items.Add(kv.Value);
            foreach (var g in inv.Backpack)
                if (g != null && g.untradable && seen.Add(g.itemId)) items.Add(g);

            if (items.Count == 0)
                Panel5cFactory.CreateListRow(list, "No untradables", "", Panel5cFactory.TextMuted,
                    Panel5cFactory.TextMuted, interactable: false);
            foreach (var g in items)
            {
                var captured = g;
                var row = Panel5cFactory.CreateListRow(list, g.displayName,
                    upg.GetTier(g).ToString(), Panel5cFactory.TextPrimary,
                    RarityVisualEffects.GetRarityColor(upg.GetTier(g)), interactable: true);
                row.onClick.AddListener(() => { selected = captured; UpdateDetail(); });
            }
            UpdateDetail();
        }

        private void UpdateDetail()
        {
            if (selected == null)
            {
                detail.text = "Select an untradable";
                actionBtn.gameObject.SetActive(false);
                return;
            }

            var cur = upg.GetTier(selected);
            bool activeOnThis = upg.IsActive && upg.ActiveItemId == selected.itemId;

            if (activeOnThis)
            {
                detail.text = $"<b>{selected.displayName}</b>\n" +
                    $"Upgrading → <b>{upg.ActiveTarget}</b>\n\n" +
                    $"Progress: {upg.Progress * 100f:0}%\n" +
                    $"Success chance now: <b>{upg.SuccessChance * 100f:0}%</b>\n" +
                    $"Time to guaranteed: {upg.Remaining:0}s\n\n" +
                    "<color=#888d84>Finish now to gamble, or wait for 100%.</color>";
                actionLabel.text = "COMPLETE NOW";
                actionBtn.gameObject.SetActive(true);
            }
            else if (cur >= RarityTier.Void)
            {
                detail.text = $"<b>{selected.displayName}</b>\nAlready at max tier (Void).";
                actionBtn.gameObject.SetActive(false);
            }
            else
            {
                var next = (RarityTier)((int)cur + 1);
                var (mat, qty, have) = Cost(next);
                bool canAfford = mat != null && have >= qty;
                string matLine = mat != null
                    ? $"<color={(canAfford ? "#97c459" : "#e24b4a")}>{mat.displayName}  {have}/{qty}</color>"
                    : "<color=#e24b4a>(no material)</color>";
                detail.text = $"<b>{selected.displayName}</b>\n" +
                    $"{cur} → <b>{next}</b>\n\nCost:\n  {matLine}\n\n" +
                    "<color=#888d84>Start begins a timer; finishing early is a gamble.</color>";
                actionLabel.text = "START UPGRADE";
                actionBtn.gameObject.SetActive(canAfford && !upg.IsActive);
            }
        }

        private (MaterialItemSO mat, int qty, int have) Cost(RarityTier target)
        {
            var mats = station.UpgradeMaterials;
            int idx = (int)target;
            MaterialItemSO mat = (mats != null && idx < mats.Length) ? mats[idx] : null;
            int have = mat != null && matInv != null ? matInv.GetCount(mat.itemId) : 0;
            return (mat, station.CostPerUpgrade, have);
        }

        private void DoAction()
        {
            if (selected == null) return;
            if (upg.IsActive && upg.ActiveItemId == selected.itemId)
            {
                bool ok = upg.CompleteNow();
                FloatingDamageNumber.SpawnText(player.transform.position,
                    ok ? "Upgrade success!" : "Upgrade failed",
                    ok ? new Color(0.5f, 0.9f, 0.4f) : new Color(0.9f, 0.4f, 0.35f));
                RefreshList();
                return;
            }

            var next = (RarityTier)((int)upg.GetTier(selected) + 1);
            var (mat, qty, have) = Cost(next);
            if (mat == null || have < qty || upg.IsActive) return;
            matInv.ConsumeMaterial(mat.itemId, qty);
            upg.StartUpgrade(selected);
            UpdateDetail();
        }

        private void Update()
        {
            if (panel != null && panel.gameObject.activeSelf && selected != null &&
                upg != null && upg.IsActive && upg.ActiveItemId == selected.itemId)
                UpdateDetail();
        }
    }
}
