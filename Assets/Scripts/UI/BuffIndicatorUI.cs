using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoidBound.Combat;
using VoidBound.Homestead;

namespace VoidBound.UI
{
    // Active-status readout under the Player Info Bar. Each status renders as a
    // pill with a depleting fill bar behind the name and a live "Ns" countdown on
    // the right. Timed buffs (e.g. the Pool of Refreshment) show gold-trimmed and
    // green; the Poison debuff (§4) shows red-trimmed and toxic-green so a harmful
    // status reads apart from a boon. Buffs and poison share one stack.
    public class BuffIndicatorUI : MonoBehaviour
    {
        private const float RowW = 240f, RowH = 28f, Gap = 6f;

        private static readonly Color32 BarFill  = new(0x6f, 0xc4, 0x66, 0xcc); // lively green
        private static readonly Color32 BarTrack = new(0x0d, 0x18, 0x0d, 0xe6); // dark track
        private static readonly Color32 BuffTrim = new(Panel5cFactory.Gold.r, Panel5cFactory.Gold.g, Panel5cFactory.Gold.b, 190);

        private static readonly Color32 PoisonFill = new(0x74, 0xa8, 0x3c, 0xe0); // toxic green
        private static readonly Color32 PoisonTrim = new(0xd8, 0x4a, 0x40, 0xff); // red outline = harmful
        private static readonly Color32 PoisonText = new(0xe6, 0xc2, 0xbc, 0xff);

        private class Row
        {
            public RectTransform root;
            public RectTransform fill;
            public TextMeshProUGUI name;
            public TextMeshProUGUI time;
        }

        private TimedBuff playerBuffs;
        private PoisonStatus playerPoison;
        private RectTransform container;
        private readonly Dictionary<string, Row> rows = new();
        private Row poisonRow;

        private void Start()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerBuffs = player.GetComponent<TimedBuff>();
                playerPoison = player.GetComponent<PoisonStatus>();
            }

            container = Panel5cFactory.MakeRect("BuffContainer", transform);
            container.anchorMin = container.anchorMax = new Vector2(0, 1);
            container.pivot = new Vector2(0, 1);
            container.anchoredPosition = new Vector2(14, -122); // just below the PlayerInfoBar card
            container.sizeDelta = new Vector2(RowW, RowH);

            if (playerBuffs != null) playerBuffs.OnBuffsChanged += Rebuild;
            if (playerPoison != null) playerPoison.OnPoisonChanged += Rebuild;
            Rebuild();
        }

        private void OnDestroy()
        {
            if (playerBuffs != null) playerBuffs.OnBuffsChanged -= Rebuild;
            if (playerPoison != null) playerPoison.OnPoisonChanged -= Rebuild;
        }

        // Recreate the row set when statuses change (they are few and change
        // rarely, so a full rebuild is simplest). Poison sits after the buffs.
        private void Rebuild()
        {
            foreach (var r in rows.Values) if (r.root != null) Destroy(r.root.gameObject);
            rows.Clear();
            if (poisonRow != null && poisonRow.root != null) Destroy(poisonRow.root.gameObject);
            poisonRow = null;

            int i = 0;
            if (playerBuffs != null)
                foreach (var buff in playerBuffs.Buffs)
                    rows[buff.id] = MakeRow("Buff_" + buff.id, buff.displayName, i++,
                        BarFill, BuffTrim, Color.white);

            if (playerPoison != null && playerPoison.IsPoisoned)
                poisonRow = MakeRow("Poison", "Poisoned", i++, PoisonFill, PoisonTrim, PoisonText);
        }

        private Row MakeRow(string key, string label, int index, Color32 fill, Color32 trim, Color32 textColor)
        {
            var root = Panel5cFactory.MakeRect(key, container);
            root.anchorMin = root.anchorMax = new Vector2(0, 1);
            root.pivot = new Vector2(0, 1);
            root.anchoredPosition = new Vector2(0, -index * (RowH + Gap));
            root.sizeDelta = new Vector2(RowW, RowH);
            Panel5cFactory.AddPanelBg(root.gameObject, BarTrack, raycast: false);
            Panel5cFactory.AddDropShadow(root.gameObject);
            Panel5cFactory.AddOutline(root.gameObject, trim);

            // Depleting fill bar (behind the text).
            var bar = Panel5cFactory.MakeRect("Fill", root);
            bar.anchorMin = new Vector2(0, 0.5f);
            bar.anchorMax = new Vector2(0, 0.5f);
            bar.pivot = new Vector2(0, 0.5f);
            bar.anchoredPosition = new Vector2(3, 0);
            bar.sizeDelta = new Vector2(RowW - 6, RowH - 6);
            Panel5cFactory.AddButtonBg(bar.gameObject, fill, raycast: false);

            var name = Panel5cFactory.MakeTMP("Name", root);
            name.rectTransform.anchorMin = new Vector2(0, 0);
            name.rectTransform.anchorMax = new Vector2(1, 1);
            name.rectTransform.offsetMin = new Vector2(12, 0);
            name.rectTransform.offsetMax = new Vector2(-46, 0);
            name.text = label;
            name.fontSize = 12f;
            name.fontStyle = FontStyles.Bold;
            name.color = textColor;
            name.alignment = TextAlignmentOptions.MidlineLeft;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.textWrappingMode = TextWrappingModes.NoWrap;

            var time = Panel5cFactory.MakeTMP("Time", root);
            time.rectTransform.anchorMin = new Vector2(1, 0);
            time.rectTransform.anchorMax = new Vector2(1, 1);
            time.rectTransform.pivot = new Vector2(1, 0.5f);
            time.rectTransform.offsetMin = new Vector2(-44, 0);
            time.rectTransform.offsetMax = new Vector2(-8, 0);
            time.fontSize = 12f;
            time.fontStyle = FontStyles.Bold;
            time.color = trim;
            time.alignment = TextAlignmentOptions.MidlineRight;

            return new Row { root = root, fill = bar, name = name, time = time };
        }

        private void Update()
        {
            if (playerBuffs != null)
                foreach (var buff in playerBuffs.Buffs)
                {
                    if (!rows.TryGetValue(buff.id, out var row)) continue;
                    row.fill.sizeDelta = new Vector2(Mathf.Max(0f, (RowW - 6f) * buff.Fraction), RowH - 6f);
                    row.time.text = $"{Mathf.CeilToInt(buff.Remaining)}s";
                }

            if (poisonRow != null && playerPoison != null)
            {
                poisonRow.fill.sizeDelta = new Vector2(Mathf.Max(0f, (RowW - 6f) * playerPoison.Fraction), RowH - 6f);
                poisonRow.time.text = $"{Mathf.CeilToInt(playerPoison.SecondsLeft)}s";
            }
        }
    }
}
