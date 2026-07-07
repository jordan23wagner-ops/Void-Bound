using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoidBound.Homestead;
using VoidBound.Inventory;

namespace VoidBound.UI
{
    // Pool of Refreshment panel (GDD §6): REFRESH applies the current tier's heal
    // + timed all-stat buff (on a cooldown); UPGRADE buys the next tier with gold.
    // Created on the HUDCanvas on first use by PoolStation; self-builds via
    // Panel5cFactory.
    public class PoolUI : MonoBehaviour
    {
        private RectTransform panel;
        private TextMeshProUGUI detail;
        private Button refreshBtn;
        private TextMeshProUGUI refreshLabel;
        private Button upgradeBtn;
        private TextMeshProUGUI upgradeLabel;

        private PoolStation station;
        private GameObject player;
        private PlayerCurrency currency;

        public void Open(PoolStation s, GameObject instigator)
        {
            station = s;
            player = instigator;
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

            panel = Panel5cFactory.CreatePanel(transform, "PoolPanel5c", "POOL OF REFRESHMENT",
                380f, 280f, out var content, out var closeBtn);
            closeBtn.onClick.AddListener(Close);

            detail = Panel5cFactory.CreateLabel(content, "Detail", "", 12f, Panel5cFactory.TextPrimary);
            Panel5cFactory.SetAnchor(detail.rectTransform, Vector2.zero, Vector2.one);
            detail.rectTransform.offsetMin = new Vector2(6, 52);
            detail.rectTransform.offsetMax = new Vector2(-6, -6);
            detail.alignment = TextAlignmentOptions.TopLeft;
            detail.textWrappingMode = TextWrappingModes.Normal;

            refreshBtn = Panel5cFactory.CreateActionButton(content, "REFRESH");
            var r = (RectTransform)refreshBtn.transform;
            Panel5cFactory.SetAnchor(r, new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            r.pivot = new Vector2(1f, 0f);
            r.sizeDelta = new Vector2(168, 34);
            r.anchoredPosition = new Vector2(-6, 10);
            refreshLabel = refreshBtn.GetComponentInChildren<TextMeshProUGUI>();
            refreshBtn.onClick.AddListener(OnRefresh);

            upgradeBtn = Panel5cFactory.CreateActionButton(content, "UPGRADE");
            var u = (RectTransform)upgradeBtn.transform;
            Panel5cFactory.SetAnchor(u, new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            u.pivot = new Vector2(0f, 0f);
            u.sizeDelta = new Vector2(168, 34);
            u.anchoredPosition = new Vector2(6, 10);
            upgradeLabel = upgradeBtn.GetComponentInChildren<TextMeshProUGUI>();
            upgradeBtn.onClick.AddListener(OnUpgrade);

            panel.gameObject.SetActive(false);
        }

        private void OnRefresh()
        {
            station.TryRefresh(player);
            Refresh();
        }

        private void OnUpgrade()
        {
            station.TryUpgrade(player);
            Refresh();
        }

        private void Refresh()
        {
            if (station == null) return;
            var t = station.Current;
            int gold = currency != null ? currency.Gold : 0;

            string cd = station.IsReady
                ? "<color=#97c459>Ready now</color>"
                : $"<color=#e2a24b>Cooling down… {station.CooldownRemaining:0}s</color>";
            detail.text =
                $"<b>Tier {station.CurrentTier + 1} / {station.TierCount}</b>\n\n" +
                $"Heal: <b>{t.healPercent}%</b> of max HP\n" +
                $"Buff: <b>+{t.buffAllStats}</b> to all stats for {t.buffDuration:0}s\n" +
                $"Cooldown: {t.cooldown:0}s\n\n" +
                $"{cd}\n\n" +
                $"<color=#888d84>Gold: {gold}</color>";

            UpdateRefreshButton();

            if (station.IsMaxTier)
            {
                upgradeLabel.text = "MAX TIER";
                upgradeBtn.interactable = false;
            }
            else
            {
                int cost = station.NextUpgradeCost;
                upgradeLabel.text = $"TIER {station.CurrentTier + 2}  ·  {cost}g";
                upgradeBtn.interactable = gold >= cost;
            }
        }

        private void UpdateRefreshButton()
        {
            refreshLabel.text = station.IsReady ? "REFRESH" : $"{station.CooldownRemaining:0}s";
            refreshBtn.interactable = station.IsReady;
        }

        // Keep the cooldown readout live while open.
        private void Update()
        {
            if (panel != null && panel.gameObject.activeSelf && station != null)
                UpdateRefreshButton();
        }
    }
}
