using UnityEngine;
using TMPro;
using VoidBound.Homestead;

namespace VoidBound.UI
{
    // Minimal active-buff readout under the Player Info Bar (Phase 6 Task 10).
    // ASCII text only per the standing icon-rendering rule.
    public class BuffIndicatorUI : MonoBehaviour
    {
        private TimedBuff playerBuffs;
        private TextMeshProUGUI label;
        private float nextTick;

        private void Start()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerBuffs = player.GetComponent<TimedBuff>();

            label = Panel5cFactory.MakeTMP("BuffLabel", transform);
            var rt = label.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(14, -132); // below the (112px-tall) PlayerInfoBar card
            rt.sizeDelta = new Vector2(300, 18);
            label.fontSize = 10f;
            label.color = Panel5cFactory.Green;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.text = "";
        }

        private void Update()
        {
            if (Time.time < nextTick) return;
            nextTick = Time.time + 0.5f;

            if (playerBuffs == null || playerBuffs.Buffs.Count == 0)
            {
                if (label != null && label.text.Length > 0) label.text = "";
                return;
            }

            var parts = new System.Text.StringBuilder();
            foreach (var buff in playerBuffs.Buffs)
            {
                if (parts.Length > 0) parts.Append("   ");
                parts.Append($"{buff.displayName} {Mathf.CeilToInt(buff.Remaining)}s");
            }
            label.text = parts.ToString();
        }
    }
}
