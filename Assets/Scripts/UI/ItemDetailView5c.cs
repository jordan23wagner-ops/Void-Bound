using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoidBound.Data;
using VoidBound.Inventory;

namespace VoidBound.UI
{
    // Tap-to-detail overlay for the Phase 5c panels. Built entirely at runtime
    // (same pattern as the Phase 3b panels) so no scene wiring is required.
    // Shows item name/rarity/slot/stat modifiers/set plus one action button
    // (Equip or Unequip) supplied by the owning panel.
    public class ItemDetailView5c : MonoBehaviour
    {
        private static readonly Color32 BgColor      = new(0x0d, 0x11, 0x0d, 250);
        private static readonly Color32 BorderColor  = new(0x2a, 0x2f, 0x2a, 255);
        private static readonly Color32 TextPrimary  = new(0xd4, 0xd8, 0xd0, 255);
        private static readonly Color32 TextMuted    = new(0x88, 0x8d, 0x84, 255);
        private static readonly Color32 ActionBg     = new(0x1e, 0x22, 0x1e, 255);
        private static readonly Color32 CloseBg      = new(0x2a, 0x0d, 0x0d, 255);
        private static readonly Color32 CloseIcon    = new(0xe2, 0x4b, 0x4a, 255);

        private TextMeshProUGUI nameTMP;
        private TextMeshProUGUI rarityTMP;
        private TextMeshProUGUI slotTMP;
        private TextMeshProUGUI statsTMP;
        private TextMeshProUGUI setTMP;
        private Button actionButton;
        private TextMeshProUGUI actionLabel;
        private Action onAction;

        public static ItemDetailView5c Create(RectTransform panel)
        {
            var go = new GameObject("DetailView5c", typeof(RectTransform));
            go.transform.SetParent(panel, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(1, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(14, 0);
            rt.offsetMax = new Vector2(-14, 0);
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, 190f);

            var bg = go.AddComponent<Image>();
            bg.color = BgColor;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = BorderColor;
            outline.effectDistance = new Vector2(2f, -2f);

            var view = go.AddComponent<ItemDetailView5c>();
            view.BuildContent(rt);
            go.SetActive(false);
            return view;
        }

        private void BuildContent(RectTransform rt)
        {
            nameTMP   = MakeText(rt, "Name",   -12f, 20f, 13f, TextPrimary, FontStyles.Bold);
            rarityTMP = MakeText(rt, "Rarity", -34f, 16f, 11f, TextMuted, FontStyles.Normal);
            slotTMP   = MakeText(rt, "Slot",   -52f, 16f, 10f, TextMuted, FontStyles.Normal);
            statsTMP  = MakeText(rt, "Stats",  -86f, 34f, 10f, TextPrimary, FontStyles.Normal);
            setTMP    = MakeText(rt, "Set",   -122f, 14f, 9f,  TextMuted, FontStyles.Italic);

            // Action button (Equip / Unequip)
            var btnGO = new GameObject("ActionBtn", typeof(RectTransform));
            btnGO.transform.SetParent(rt, false);
            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.5f, 0);
            btnRT.anchorMax = new Vector2(0.5f, 0);
            btnRT.pivot = new Vector2(0.5f, 0);
            btnRT.anchoredPosition = new Vector2(0, 12f);
            btnRT.sizeDelta = new Vector2(120f, 30f);
            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = ActionBg;
            actionButton = btnGO.AddComponent<Button>();
            actionButton.onClick.AddListener(() => { onAction?.Invoke(); Hide(); });

            var lblGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            lblGO.transform.SetParent(btnRT, false);
            var lblRT = lblGO.GetComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero;
            lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = Vector2.zero;
            lblRT.offsetMax = Vector2.zero;
            actionLabel = lblGO.GetComponent<TextMeshProUGUI>();
            actionLabel.fontSize = 11f;
            actionLabel.color = TextPrimary;
            actionLabel.alignment = TextAlignmentOptions.Center;
            actionLabel.characterSpacing = 2f;

            // Close (X) button
            var xGO = new GameObject("CloseBtn", typeof(RectTransform));
            xGO.transform.SetParent(rt, false);
            var xRT = xGO.GetComponent<RectTransform>();
            xRT.anchorMin = new Vector2(1, 1);
            xRT.anchorMax = new Vector2(1, 1);
            xRT.pivot = new Vector2(1, 1);
            xRT.anchoredPosition = new Vector2(-6f, -6f);
            xRT.sizeDelta = new Vector2(20f, 20f);
            var xImg = xGO.AddComponent<Image>();
            xImg.color = CloseBg;
            var xBtn = xGO.AddComponent<Button>();
            xBtn.onClick.AddListener(Hide);

            var xLblGO = new GameObject("X", typeof(RectTransform), typeof(TextMeshProUGUI));
            xLblGO.transform.SetParent(xRT, false);
            var xLblRT = xLblGO.GetComponent<RectTransform>();
            xLblRT.anchorMin = Vector2.zero;
            xLblRT.anchorMax = Vector2.one;
            xLblRT.offsetMin = Vector2.zero;
            xLblRT.offsetMax = Vector2.zero;
            var xLbl = xLblGO.GetComponent<TextMeshProUGUI>();
            xLbl.text = "X";
            xLbl.fontSize = 11f;
            xLbl.color = CloseIcon;
            xLbl.alignment = TextAlignmentOptions.Center;
        }

        private static TextMeshProUGUI MakeText(RectTransform parent, string name, float y, float height,
                                                float fontSize, Color32 color, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, y);
            rt.offsetMin = new Vector2(14, rt.offsetMin.y);
            rt.offsetMax = new Vector2(-14, rt.offsetMax.y);
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            return tmp;
        }

        public void Show(GearItemSO item, string actionText, Action action, int stackCount = 1)
        {
            if (item == null) { Hide(); return; }

            onAction = action;
            nameTMP.text = stackCount > 1 ? $"{item.displayName}  ×{stackCount}" : item.displayName;
            rarityTMP.text = item.rarity.ToString();
            rarityTMP.color = RarityVisualEffects.GetRarityColor(item.rarity);
            slotTMP.text = item.slot == EquipmentSlot.Weapon
                ? $"{item.slot} ({item.weaponType})"
                : (item.slot == EquipmentSlot.Ring2 ? "Ring" : item.slot.ToString());

            var m = item.statModifiers;
            statsTMP.text = $"STR +{m.str}   DEX +{m.dex}\nVIG +{m.vig}   INT +{m.intel}";
            if (item.baseDamage > 0) statsTMP.text += $"\nDamage: {item.baseDamage}";

            setTMP.text = string.IsNullOrEmpty(item.setId) ? "" : $"Set: {item.setId}";

            bool hasAction = action != null && !string.IsNullOrEmpty(actionText);
            actionButton.gameObject.SetActive(hasAction);
            if (hasAction) actionLabel.text = actionText;

            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        public void ShowEmpty(string slotName)
        {
            onAction = null;
            nameTMP.text = slotName;
            rarityTMP.text = "Empty";
            rarityTMP.color = Color.gray;
            slotTMP.text = "";
            statsTMP.text = "---";
            setTMP.text = "";
            actionButton.gameObject.SetActive(false);
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
