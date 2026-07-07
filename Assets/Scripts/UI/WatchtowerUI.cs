using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoidBound.Combat;
using VoidBound.Data;
using VoidBound.Homestead;
using VoidBound.Skilling;

namespace VoidBound.UI
{
    // Watchtower scouting board (GDD §6): lists each zone with its danger,
    // recommended combat level (vs yours), unlock status, and an intel blurb, so
    // you can plan a run before committing at the Portal. Created on the HUDCanvas
    // on first use by WatchtowerStation; self-builds via Panel5cFactory.
    public class WatchtowerUI : MonoBehaviour
    {
        private static readonly string[] DangerLabels =
            { "Safe", "Low", "Moderate", "High", "Severe", "Deadly" };
        private static readonly Color Risky = new(0.89f, 0.29f, 0.27f);

        private RectTransform panel;
        private RectTransform list;
        private TextMeshProUGUI header;
        private TextMeshProUGUI detail;

        private WatchtowerStation station;
        private PlayerSkills skills;
        private ZoneDefinitionSO selected;

        public void Open(WatchtowerStation s, GameObject instigator)
        {
            station = s;
            skills = instigator != null ? instigator.GetComponent<PlayerSkills>() : null;

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

        private int PlayerLevel => skills != null ? CombatLevelCalculator.GetCombatLevel(skills) : 1;

        private void EnsureBuilt()
        {
            if (panel != null) return;

            panel = Panel5cFactory.CreatePanel(transform, "WatchtowerPanel5c", "WATCHTOWER",
                540f, 380f, out var content, out var closeBtn);
            closeBtn.onClick.AddListener(Close);

            header = Panel5cFactory.CreateLabel(content, "Header", "", 11f, Panel5cFactory.TextMuted);
            Panel5cFactory.SetAnchor(header.rectTransform, new Vector2(0, 1), new Vector2(1, 1));
            header.rectTransform.pivot = new Vector2(0.5f, 1f);
            header.rectTransform.sizeDelta = new Vector2(-4, 18);

            var listArea = Panel5cFactory.MakeRect("ListArea", content);
            Panel5cFactory.SetAnchor(listArea, new Vector2(0, 0), new Vector2(0.46f, 1));
            listArea.offsetMin = Vector2.zero;
            listArea.offsetMax = new Vector2(-4, -22);
            list = Panel5cFactory.CreateScrollList(listArea, "ZoneList");
            Panel5cFactory.SetAnchor((RectTransform)list.parent, Vector2.zero, Vector2.one);

            var detailArea = Panel5cFactory.MakeRect("DetailArea", content);
            Panel5cFactory.SetAnchor(detailArea, new Vector2(0.46f, 0), new Vector2(1, 1));
            detailArea.offsetMin = new Vector2(4, 0);
            detailArea.offsetMax = new Vector2(0, -22);
            Panel5cFactory.AddPanelBg(detailArea.gameObject, Panel5cFactory.SlotBg, raycast: false);

            detail = Panel5cFactory.CreateLabel(detailArea, "Detail", "Select a zone to scout.", 11f,
                Panel5cFactory.TextPrimary);
            Panel5cFactory.SetAnchor(detail.rectTransform, Vector2.zero, Vector2.one);
            detail.rectTransform.offsetMin = new Vector2(12, 12);
            detail.rectTransform.offsetMax = new Vector2(-12, -10);
            detail.alignment = TextAlignmentOptions.TopLeft;
            detail.textWrappingMode = TextWrappingModes.Normal;

            panel.gameObject.SetActive(false);
        }

        private void RefreshList()
        {
            for (int i = list.childCount - 1; i >= 0; i--) Destroy(list.GetChild(i).gameObject);
            header.text = $"Scouting the wastes — your combat level: <b>{PlayerLevel}</b>";

            var zones = station.Zones;
            if (zones == null || zones.Length == 0)
            {
                Panel5cFactory.CreateListRow(list, "No zones charted", "",
                    Panel5cFactory.TextMuted, Panel5cFactory.TextMuted, interactable: false);
                UpdateDetail();
                return;
            }

            foreach (var zone in zones)
            {
                if (zone == null) continue;
                var captured = zone;
                bool ready = PlayerLevel >= zone.recommendedLevel;
                Color nameColor = zone.isUnlocked ? Panel5cFactory.TextPrimary : Panel5cFactory.TextMuted;
                Color lvColor = !zone.isUnlocked ? (Color)Panel5cFactory.TextMuted : ready ? (Color)Panel5cFactory.Green : Risky;
                var row = Panel5cFactory.CreateListRow(list, zone.displayName, $"Lv {zone.recommendedLevel}",
                    nameColor, lvColor, interactable: true);
                row.onClick.AddListener(() => { selected = captured; UpdateDetail(); });
            }
            UpdateDetail();
        }

        private void UpdateDetail()
        {
            if (selected == null)
            {
                detail.text = "Select a zone to scout.";
                return;
            }

            int rec = selected.recommendedLevel;
            int lvl = PlayerLevel;
            int d = Mathf.Clamp(selected.dangerRating, 0, DangerLabels.Length - 1);
            string status = selected.isUnlocked
                ? "<color=#97c459>Unlocked</color>"
                : "<color=#888d84>Locked</color>";
            string readiness = lvl >= rec
                ? "<color=#97c459>You're ready for this.</color>"
                : $"<color=#e24b4a>Under-levelled by {rec - lvl} — risky.</color>";
            string blurb = string.IsNullOrEmpty(selected.scoutReport)
                ? "<color=#888d84>No intel gathered.</color>"
                : selected.scoutReport;

            detail.text =
                $"<b>{selected.displayName}</b>\n" +
                $"{status}\n\n" +
                $"Danger: <b>{DangerLabels[d]}</b> ({d}/5)\n" +
                $"Recommended: <b>Lv {rec}</b>  ·  You: Lv {lvl}\n" +
                $"{readiness}\n\n" +
                $"{blurb}";
        }
    }
}
