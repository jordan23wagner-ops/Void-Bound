using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoidBound.Homestead;

namespace VoidBound.UI
{
    // Active-buff readout under the Player Info Bar. Each buff renders as a
    // gold-trimmed pill with a depleting fill bar behind the buff name and a
    // live "Ns" countdown on the right — so a timed boost (e.g. the Pool of
    // Refreshment) is clearly visible and legible, not a faint green tag.
    public class BuffIndicatorUI : MonoBehaviour
    {
        private const float RowW = 240f, RowH = 28f, Gap = 6f;

        private static readonly Color32 BarFill  = new(0x6f, 0xc4, 0x66, 0xcc); // lively green
        private static readonly Color32 BarTrack = new(0x0d, 0x18, 0x0d, 0xe6); // dark track

        private class Row
        {
            public RectTransform root;
            public RectTransform fill;
            public TextMeshProUGUI name;
            public TextMeshProUGUI time;
        }

        private TimedBuff playerBuffs;
        private RectTransform container;
        private readonly Dictionary<string, Row> rows = new();

        private void Start()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerBuffs = player.GetComponent<TimedBuff>();

            container = Panel5cFactory.MakeRect("BuffContainer", transform);
            container.anchorMin = container.anchorMax = new Vector2(0, 1);
            container.pivot = new Vector2(0, 1);
            container.anchoredPosition = new Vector2(14, -122); // just below the PlayerInfoBar card
            container.sizeDelta = new Vector2(RowW, RowH);

            if (playerBuffs != null) playerBuffs.OnBuffsChanged += Rebuild;
            Rebuild();
        }

        private void OnDestroy()
        {
            if (playerBuffs != null) playerBuffs.OnBuffsChanged -= Rebuild;
        }

        // Recreate the row set when the active buffs change (buffs are few and
        // change rarely, so a full rebuild is simplest).
        private void Rebuild()
        {
            foreach (var r in rows.Values) if (r.root != null) Destroy(r.root.gameObject);
            rows.Clear();
            if (playerBuffs == null) return;

            int i = 0;
            foreach (var buff in playerBuffs.Buffs)
                rows[buff.id] = MakeRow(buff, i++);
        }

        private Row MakeRow(TimedBuff.ActiveBuff buff, int index)
        {
            var root = Panel5cFactory.MakeRect("Buff_" + buff.id, container);
            root.anchorMin = root.anchorMax = new Vector2(0, 1);
            root.pivot = new Vector2(0, 1);
            root.anchoredPosition = new Vector2(0, -index * (RowH + Gap));
            root.sizeDelta = new Vector2(RowW, RowH);
            Panel5cFactory.AddPanelBg(root.gameObject, BarTrack, raycast: false);
            Panel5cFactory.AddDropShadow(root.gameObject);
            Panel5cFactory.AddOutline(root.gameObject, new Color32(Panel5cFactory.Gold.r, Panel5cFactory.Gold.g, Panel5cFactory.Gold.b, 190));

            // Depleting fill bar (behind the text).
            var fill = Panel5cFactory.MakeRect("Fill", root);
            fill.anchorMin = new Vector2(0, 0.5f);
            fill.anchorMax = new Vector2(0, 0.5f);
            fill.pivot = new Vector2(0, 0.5f);
            fill.anchoredPosition = new Vector2(3, 0);
            fill.sizeDelta = new Vector2(RowW - 6, RowH - 6);
            Panel5cFactory.AddButtonBg(fill.gameObject, BarFill, raycast: false);

            // Buff name (left).
            var name = Panel5cFactory.MakeTMP("Name", root);
            name.rectTransform.anchorMin = new Vector2(0, 0);
            name.rectTransform.anchorMax = new Vector2(1, 1);
            name.rectTransform.offsetMin = new Vector2(12, 0);
            name.rectTransform.offsetMax = new Vector2(-46, 0);
            name.text = buff.displayName;
            name.fontSize = 12f;
            name.fontStyle = FontStyles.Bold;
            name.color = Color.white;
            name.alignment = TextAlignmentOptions.MidlineLeft;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.textWrappingMode = TextWrappingModes.NoWrap;

            // Countdown (right).
            var time = Panel5cFactory.MakeTMP("Time", root);
            time.rectTransform.anchorMin = new Vector2(1, 0);
            time.rectTransform.anchorMax = new Vector2(1, 1);
            time.rectTransform.pivot = new Vector2(1, 0.5f);
            time.rectTransform.offsetMin = new Vector2(-44, 0);
            time.rectTransform.offsetMax = new Vector2(-8, 0);
            time.fontSize = 12f;
            time.fontStyle = FontStyles.Bold;
            time.color = Panel5cFactory.Gold;
            time.alignment = TextAlignmentOptions.MidlineRight;

            return new Row { root = root, fill = fill, name = name, time = time };
        }

        private void Update()
        {
            if (playerBuffs == null) return;

            foreach (var buff in playerBuffs.Buffs)
            {
                if (!rows.TryGetValue(buff.id, out var row)) continue;
                float w = Mathf.Max(0f, (RowW - 6f) * buff.Fraction);
                row.fill.sizeDelta = new Vector2(w, RowH - 6f);
                row.time.text = $"{Mathf.CeilToInt(buff.Remaining)}s";
            }
        }
    }
}
