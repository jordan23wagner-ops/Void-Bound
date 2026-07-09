using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoidBound.Data;
using VoidBound.Quests;

namespace VoidBound.UI
{
    // Always-on quest tracker in the top-right of the HUD: the active quest's
    // title plus a live objective checklist. Hidden when no quest is active.
    // Subscribes to PlayerQuests.OnQuestChanged and redraws. Self-builds on
    // HUDCanvas; finds the (persisted) player lazily so it survives scene loads.
    public class QuestTrackerUI : MonoBehaviour
    {
        private const int MaxRows = 6;

        private RectTransform root;
        private TextMeshProUGUI titleLabel;
        private readonly List<TextMeshProUGUI> rows = new();

        private PlayerQuests quests;

        private void Update()
        {
            if (quests != null) return;
            // Bind to the player's quest log once it exists, then stop polling.
            var pq = Object.FindAnyObjectByType<PlayerQuests>();
            if (pq == null) return;
            quests = pq;
            quests.OnQuestChanged += Redraw;
            EnsureBuilt();
            Redraw();
        }

        private void OnDestroy()
        {
            if (quests != null) quests.OnQuestChanged -= Redraw;
        }

        private void EnsureBuilt()
        {
            if (root != null) return;

            root = Panel5cFactory.MakeRect("QuestTracker", transform);
            root.anchorMin = new Vector2(1, 1);
            root.anchorMax = new Vector2(1, 1);
            root.pivot = new Vector2(1, 1);
            root.anchoredPosition = new Vector2(-16, -132); // below the top-right HUD cluster
            root.sizeDelta = new Vector2(272, 24 + MaxRows * 20 + 16);
            var bg = Panel5cFactory.AddPanelBg(root.gameObject, new Color(0.06f, 0.07f, 0.06f, 0.82f), false);

            titleLabel = Panel5cFactory.CreateLabel(root, "TrackerTitle", "",
                12f, Panel5cFactory.Gold);
            titleLabel.rectTransform.anchorMin = new Vector2(0, 1);
            titleLabel.rectTransform.anchorMax = new Vector2(1, 1);
            titleLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            titleLabel.rectTransform.offsetMin = new Vector2(12, 0);
            titleLabel.rectTransform.offsetMax = new Vector2(-12, 0);
            titleLabel.rectTransform.anchoredPosition = new Vector2(0, -8);
            titleLabel.rectTransform.sizeDelta = new Vector2(titleLabel.rectTransform.sizeDelta.x, 20);
            titleLabel.fontStyle = FontStyles.Bold;
            titleLabel.characterSpacing = 2f;

            for (int i = 0; i < MaxRows; i++)
            {
                var row = Panel5cFactory.CreateLabel(root, "ObjRow" + i, "",
                    11f, Panel5cFactory.TextPrimary);
                row.rectTransform.anchorMin = new Vector2(0, 1);
                row.rectTransform.anchorMax = new Vector2(1, 1);
                row.rectTransform.pivot = new Vector2(0.5f, 1f);
                row.rectTransform.offsetMin = new Vector2(14, 0);
                row.rectTransform.offsetMax = new Vector2(-12, 0);
                row.rectTransform.anchoredPosition = new Vector2(0, -30 - i * 20);
                row.rectTransform.sizeDelta = new Vector2(row.rectTransform.sizeDelta.x, 18);
                row.textWrappingMode = TextWrappingModes.NoWrap;
                row.overflowMode = TextOverflowModes.Ellipsis;
                rows.Add(row);
            }
        }

        private void Redraw()
        {
            EnsureBuilt();

            var q = quests != null ? quests.Active : null;
            if (q == null || q.objectives == null)
            {
                root.gameObject.SetActive(false);
                return;
            }
            root.gameObject.SetActive(true);

            titleLabel.text = q.title;
            int n = Mathf.Min(MaxRows, q.objectives.Length);
            int shown = 0;
            for (int i = 0; i < MaxRows; i++)
            {
                if (i < n)
                {
                    var obj = q.objectives[i];
                    int have = quests.GetProgress(i);
                    bool done = have >= obj.required;
                    rows[i].text = (done ? "[x] " : "[ ] ") + $"{obj.label}  {have}/{obj.required}";
                    rows[i].color = done ? (Color)Panel5cFactory.Green : (Color)Panel5cFactory.TextPrimary;
                    rows[i].gameObject.SetActive(true);
                    shown++;
                }
                else rows[i].gameObject.SetActive(false);
            }

            root.sizeDelta = new Vector2(272, 24 + shown * 20 + 16);
        }
    }
}
