using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VoidBound.UI
{
    // Shared runtime scaffolding for Homestead panels so every station UI
    // speaks the approved Phase 5c visual language (palette, header, X close)
    // without duplicating it. Scaffolding only — behavior lives in each UI.
    public static class Panel5cFactory
    {
        // Palette mirrors Phase5cUIBuilder's approved mockup colors
        public static readonly Color32 PanelBg     = new(0x11, 0x14, 0x11, 255);
        public static readonly Color32 HeaderBg    = new(0x0d, 0x11, 0x0d, 255);
        public static readonly Color32 PanelBorder = new(0x2a, 0x2f, 0x2a, 255);
        public static readonly Color32 SlotBg      = new(0x0a, 0x0d, 0x0a, 255);
        public static readonly Color32 RowBg       = new(0x1e, 0x22, 0x1e, 255);
        public static readonly Color32 TextPrimary = new(0xd4, 0xd8, 0xd0, 255);
        public static readonly Color32 TextMuted   = new(0x88, 0x8d, 0x84, 255);
        public static readonly Color32 XBg         = new(0x2a, 0x0d, 0x0d, 255);
        public static readonly Color32 XIcon       = new(0xe2, 0x4b, 0x4a, 255);
        public static readonly Color32 Gold        = new(0xfa, 0xc7, 0x75, 255);
        public static readonly Color32 Green       = new(0x97, 0xc4, 0x59, 255);

        const float HeaderH = 52f;

        // Centered panel with header bar, title, X button, and a content area.
        public static RectTransform CreatePanel(Transform canvas, string name, string title,
                                                float width, float height, out RectTransform content,
                                                out Button closeButton)
        {
            var panel = MakeRect(name, canvas);
            SetAnchor(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(width, height);
            panel.anchoredPosition = Vector2.zero;
            AddImage(panel.gameObject, PanelBg, raycast: true);
            AddOutline(panel.gameObject, PanelBorder);

            var header = MakeRect("Header", panel);
            SetAnchor(header, new Vector2(0, 1), new Vector2(1, 1));
            header.pivot = new Vector2(0.5f, 1f);
            header.sizeDelta = new Vector2(0, HeaderH);
            header.anchoredPosition = Vector2.zero;
            AddImage(header.gameObject, HeaderBg, raycast: false);

            var titleTMP = MakeTMP("Title", header);
            SetAnchor(titleTMP.rectTransform, new Vector2(0, 0), new Vector2(1, 1));
            titleTMP.rectTransform.offsetMin = new Vector2(18, 0);
            titleTMP.rectTransform.offsetMax = new Vector2(-50, 0);
            titleTMP.text = title;
            titleTMP.fontSize = 13f;
            titleTMP.color = TextPrimary;
            titleTMP.characterSpacing = 4f;
            titleTMP.alignment = TextAlignmentOptions.MidlineLeft;

            var xBtn = MakeRect("CloseBtn", header);
            SetAnchor(xBtn, new Vector2(1, 0.5f), new Vector2(1, 0.5f));
            xBtn.pivot = new Vector2(0.5f, 0.5f);
            xBtn.sizeDelta = new Vector2(22, 22);
            xBtn.anchoredPosition = new Vector2(-14, 0);
            AddImage(xBtn.gameObject, XBg, raycast: true);
            closeButton = xBtn.gameObject.AddComponent<Button>();
            var xLabel = MakeTMP("X", xBtn);
            SetAnchor(xLabel.rectTransform, Vector2.zero, Vector2.one);
            xLabel.text = "X"; // ASCII per standing icon-rendering rule
            xLabel.fontSize = 12f;
            xLabel.color = XIcon;
            xLabel.alignment = TextAlignmentOptions.Center;

            content = MakeRect("Content", panel);
            SetAnchor(content, new Vector2(0, 0), new Vector2(1, 1));
            content.offsetMin = new Vector2(14, 14);
            content.offsetMax = new Vector2(-14, -HeaderH - 6);

            return panel;
        }

        // Vertical scrollable list; returns the content transform rows go into.
        public static RectTransform CreateScrollList(RectTransform parent, string name)
        {
            var viewport = MakeRect(name, parent);
            viewport.gameObject.AddComponent<RectMask2D>();
            AddImage(viewport.gameObject, SlotBg, raycast: true);

            var listContent = MakeRect("ListContent", viewport);
            SetAnchor(listContent, new Vector2(0, 1), new Vector2(1, 1));
            listContent.pivot = new Vector2(0.5f, 1f);
            listContent.anchoredPosition = Vector2.zero;

            var layout = listContent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            var fitter = listContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = listContent;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;

            return listContent;
        }

        // Clickable list row: left label + right-aligned value (e.g. price).
        public static Button CreateListRow(Transform listContent, string leftText, string rightText,
                                           Color leftColor, Color rightColor, bool interactable = true)
        {
            var row = MakeRect("Row", listContent);
            AddImage(row.gameObject, RowBg, raycast: true);
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 30f;
            le.minHeight = 30f;

            var left = MakeTMP("Left", row);
            SetAnchor(left.rectTransform, new Vector2(0, 0), new Vector2(1, 1));
            left.rectTransform.offsetMin = new Vector2(8, 0);
            left.rectTransform.offsetMax = new Vector2(-80, 0);
            left.text = leftText;
            left.fontSize = 11f;
            left.color = leftColor;
            left.alignment = TextAlignmentOptions.MidlineLeft;
            left.overflowMode = TextOverflowModes.Ellipsis;
            left.textWrappingMode = TextWrappingModes.NoWrap;

            var right = MakeTMP("Right", row);
            SetAnchor(right.rectTransform, new Vector2(1, 0), new Vector2(1, 1));
            right.rectTransform.pivot = new Vector2(1, 0.5f);
            right.rectTransform.sizeDelta = new Vector2(76, 0);
            right.rectTransform.anchoredPosition = new Vector2(-8, 0);
            right.text = rightText;
            right.fontSize = 11f;
            right.color = rightColor;
            right.alignment = TextAlignmentOptions.MidlineRight;

            var btn = row.gameObject.AddComponent<Button>();
            btn.interactable = interactable;
            return btn;
        }

        public static TextMeshProUGUI CreateLabel(RectTransform parent, string name, string text,
                                                  float fontSize, Color color)
        {
            var tmp = MakeTMP(name, parent);
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            return tmp;
        }

        public static Button CreateActionButton(RectTransform parent, string label)
        {
            var btnRT = MakeRect("ActionBtn", parent);
            btnRT.sizeDelta = new Vector2(120, 30);
            AddImage(btnRT.gameObject, RowBg, raycast: true);
            AddOutline(btnRT.gameObject, PanelBorder);
            var btn = btnRT.gameObject.AddComponent<Button>();

            var lbl = MakeTMP("Label", btnRT);
            SetAnchor(lbl.rectTransform, Vector2.zero, Vector2.one);
            lbl.text = label;
            lbl.fontSize = 11f;
            lbl.color = TextPrimary;
            lbl.characterSpacing = 2f;
            lbl.alignment = TextAlignmentOptions.Center;
            return btn;
        }

        public static RectTransform MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static TextMeshProUGUI MakeTMP(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.raycastTarget = false;
            return tmp;
        }

        public static Image AddImage(GameObject go, Color32 color, bool raycast)
        {
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        public static void AddOutline(GameObject go, Color32 color)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(2f, -2f);
        }

        public static void SetAnchor(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // The 4 Homestead overlay panels (Merchant/Storage/Training/Portal) all
        // render as a single centered window via CreatePanel, so unlike Phase5c's
        // deliberate side-by-side Equipment/Inventory layout, only one of these
        // should ever be visible at once. Call from each Open() before showing.
        public static void CloseOtherHomesteadPanels(GameObject host, MonoBehaviour except)
        {
            var merchant = host.GetComponent<MerchantUI>();
            if (merchant != null && (object)merchant != except) merchant.Close();
            var storage = host.GetComponent<StorageUI>();
            if (storage != null && (object)storage != except) storage.Close();
            var training = host.GetComponent<TrainingUI>();
            if (training != null && (object)training != except) training.Close();
            var portal = host.GetComponent<PortalUI>();
            if (portal != null && (object)portal != except) portal.Close();
        }
    }
}
