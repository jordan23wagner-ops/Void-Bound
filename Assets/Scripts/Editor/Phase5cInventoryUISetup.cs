#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoidBound.Combat;
using VoidBound.Data;
using VoidBound.Inventory;
using VoidBound.Skilling;
using VoidBound.UI;

namespace VoidBound.Editor
{
    public static class Phase5cInventoryUISetup
    {
        private static readonly Color32 PanelBgOuter = new(0x1a, 0x1f, 0x1a, 0xFF);
        private static readonly Color32 PanelBgInner = new(0x11, 0x14, 0x11, 0xFF);
        private static readonly Color32 HeaderBg = new(0x0d, 0x11, 0x0d, 0xFF);
        private static readonly Color32 SlotBg = new(0x0a, 0x0d, 0x0a, 0xFF);
        private static readonly Color32 SlotBorderEmpty = new(0x3a, 0x3d, 0x3a, 0xFF);
        private static readonly Color32 SlotBorderFaint = new(0x1e, 0x22, 0x1e, 0xFF);
        private static readonly Color32 IconEmpty = new(0x5a, 0x60, 0x55, 0xFF);
        private static readonly Color32 LabelColor = new(0x3a, 0x3d, 0x3a, 0xFF);
        private static readonly Color32 PanelBorder = new(0x2a, 0x2f, 0x2a, 0xFF);
        private static readonly Color32 TextPrimary = new(0xd4, 0xd8, 0xd0, 0xFF);
        private static readonly Color32 TextMuted = new(0x88, 0x8d, 0x84, 0xFF);
        private static readonly Color32 XBtnBg = new(0x2a, 0x0d, 0x0d, 0xFF);
        private static readonly Color32 XBtnIcon = new(0xe2, 0x4b, 0x4a, 0xFF);

        private static Texture2D helmIcon, bodyIcon, legsIcon, bootsIcon, capeIcon;
        private static Texture2D swordIcon, shieldIcon, gloveIcon, amuletIcon, ringIcon;

        [MenuItem("VoidBound/Setup Phase 5c - Inventory UI")]
        public static void Setup()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) { Debug.LogError("No Player found."); return; }

            var hudCanvas = Object.FindAnyObjectByType<HUDManager>()?.GetComponent<Canvas>();
            if (hudCanvas == null) { Debug.LogError("No HUD Canvas. Run Setup Homestead Scene first."); return; }
            var root = hudCanvas.gameObject.transform;

            DestroyNamed(root, "EquipmentPanel");
            DestroyNamed(root, "BackpackPanel");
            DestroyNamed(root, "InventoryPanelGroup");

            GenerateIcons();

            var group = new GameObject("InventoryPanelGroup");
            group.transform.SetParent(root, false);
            var gRect = group.AddComponent<RectTransform>();
            gRect.anchorMin = new Vector2(0.5f, 0.5f);
            gRect.anchorMax = new Vector2(0.5f, 0.5f);
            gRect.pivot = new Vector2(0.5f, 0.5f);
            gRect.anchoredPosition = Vector2.zero;
            gRect.sizeDelta = new Vector2(340 + 16 + 320, 520);
            var hl = group.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 16f;
            hl.childForceExpandWidth = false;
            hl.childForceExpandHeight = true;
            hl.childControlWidth = false;
            hl.childControlHeight = false;
            hl.childAlignment = TextAnchor.MiddleCenter;

            var equipPanel = BuildEquipmentPanel(group.transform, player);
            var invPanel = BuildInventoryPanel(group.transform, player);

            group.SetActive(false);

            var hm = hudCanvas.GetComponent<HUDManager>();
            if (hm != null)
            {
                var so = new SerializedObject(hm);
                so.FindProperty("equipmentPanel").objectReferenceValue = group;
                so.FindProperty("backpackPanel").objectReferenceValue = group;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            Debug.Log("[Phase 5c] Inventory UI rebuilt with exact mockup layout.");
        }

        private static void GenerateIcons()
        {
            helmIcon = SlotIconGenerator.GenerateHelm();
            bodyIcon = SlotIconGenerator.GenerateBody();
            legsIcon = SlotIconGenerator.GenerateLegs();
            bootsIcon = SlotIconGenerator.GenerateBoots();
            capeIcon = SlotIconGenerator.GenerateCape();
            swordIcon = SlotIconGenerator.GenerateSword();
            shieldIcon = SlotIconGenerator.GenerateShield();
            gloveIcon = SlotIconGenerator.GenerateGlove();
            amuletIcon = SlotIconGenerator.GenerateAmulet();
            ringIcon = SlotIconGenerator.GenerateRing();
        }

        private static GameObject BuildEquipmentPanel(Transform parent, GameObject player)
        {
            var panel = CreatePanelFrame(parent, "EquipmentPanel", 340, 520);

            CreateHeader(panel.transform, "EQUIPMENT", 340);

            var body = new GameObject("Body");
            body.transform.SetParent(panel.transform, false);
            var bodyRect = body.AddComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(18f, 90f);
            bodyRect.offsetMax = new Vector2(-18f, -52f);

            var leftCol = CreateSlotColumn(body.transform, "LeftCol", 0f, 64f);
            CreateSlot(leftCol, "HELM", helmIcon);
            CreateSlot(leftCol, "BODY", bodyIcon);
            CreateSlot(leftCol, "LEGS", legsIcon);
            CreateSlot(leftCol, "BOTS", bootsIcon);
            CreateSlot(leftCol, "GLVS", gloveIcon);

            float centerX = 76f;
            float centerW = 340 - 36 - 64 - 64 - 24;
            var centerCol = new GameObject("CenterStats");
            centerCol.transform.SetParent(body.transform, false);
            var cRect = centerCol.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0f, 0f);
            cRect.anchorMax = new Vector2(0f, 1f);
            cRect.pivot = new Vector2(0f, 0.5f);
            cRect.anchoredPosition = new Vector2(centerX, 0f);
            cRect.sizeDelta = new Vector2(centerW, 0f);

            var statReadout = CreateTMP(centerCol.transform, "StatReadout",
                "Level 1\n\nDamage  10\nDefense  0\n\nVIG 1\nSTR 1\nDEX 1\nINT 1",
                12, (Color)TextPrimary, TextAlignmentOptions.TopLeft);
            statReadout.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            statReadout.GetComponent<RectTransform>().anchorMax = Vector2.one;
            statReadout.GetComponent<RectTransform>().offsetMin = new Vector2(4f, 4f);
            statReadout.GetComponent<RectTransform>().offsetMax = new Vector2(-4f, -4f);
            statReadout.GetComponent<TMP_Text>().richText = true;

            var rightCol = CreateSlotColumn(body.transform, "RightCol", 340 - 36 - 64, 64f);
            CreateSlot(rightCol, "AMLT", amuletIcon);
            CreateSlot(rightCol, "RING", ringIcon);
            CreateSlot(rightCol, "RING", ringIcon);
            CreateSlot(rightCol, "CAPE", capeIcon);

            var dock = new GameObject("WeaponDock");
            dock.transform.SetParent(panel.transform, false);
            var dRect = dock.AddComponent<RectTransform>();
            dRect.anchorMin = new Vector2(0.5f, 0f);
            dRect.anchorMax = new Vector2(0.5f, 0f);
            dRect.pivot = new Vector2(0.5f, 0f);
            dRect.anchoredPosition = new Vector2(0f, 18f);
            dRect.sizeDelta = new Vector2(138f, 64f);
            var dhl = dock.AddComponent<HorizontalLayoutGroup>();
            dhl.spacing = 10f;
            dhl.childAlignment = TextAnchor.MiddleCenter;
            dhl.childForceExpandWidth = false;
            dhl.childForceExpandHeight = false;
            dhl.childControlWidth = false;
            dhl.childControlHeight = false;

            CreateSlot(dock.transform, "WPGN", swordIcon);
            CreateSlot(dock.transform, "SHLD", shieldIcon);

            var eqUI = panel.AddComponent<EquipmentPanelUI>();
            var inv = player.GetComponent<PlayerInventory>();
            var so = new SerializedObject(eqUI);
            so.FindProperty("inventory").objectReferenceValue = inv;
            so.FindProperty("leftColumn").objectReferenceValue = leftCol.transform;
            so.FindProperty("rightColumn").objectReferenceValue = rightCol.transform;
            so.FindProperty("weaponDock").objectReferenceValue = dock.transform;
            so.FindProperty("statReadout").objectReferenceValue = statReadout.GetComponent<TMP_Text>() as Text;
            so.ApplyModifiedPropertiesWithoutUndo();

            return panel;
        }

        private static GameObject BuildInventoryPanel(Transform parent, GameObject player)
        {
            var panel = CreatePanelFrame(parent, "BackpackPanel", 320, 520);

            var header = CreateHeader(panel.transform, "INVENTORY", 320);
            var capacityText = CreateTMP(header.transform, "Capacity", "4 / 24", 11,
                (Color)IconEmpty, TextAlignmentOptions.MidlineRight);
            var capRect = capacityText.GetComponent<RectTransform>();
            capRect.anchorMin = new Vector2(0f, 0f);
            capRect.anchorMax = new Vector2(1f, 1f);
            capRect.offsetMin = new Vector2(0f, 0f);
            capRect.offsetMax = new Vector2(-40f, 0f);

            var grid = new GameObject("Grid");
            grid.transform.SetParent(panel.transform, false);
            var gRect = grid.AddComponent<RectTransform>();
            gRect.anchorMin = new Vector2(0f, 0f);
            gRect.anchorMax = new Vector2(1f, 1f);
            gRect.offsetMin = new Vector2(16f, 60f);
            gRect.offsetMax = new Vector2(-16f, -52f);
            var gl = grid.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(64f, 64f);
            gl.spacing = new Vector2(8f, 8f);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = 4;
            gl.childAlignment = TextAnchor.UpperLeft;

            for (int i = 0; i < 12; i++)
                CreateEmptyInvSlot(grid.transform);

            var footer = new GameObject("Footer");
            footer.transform.SetParent(panel.transform, false);
            footer.AddComponent<Image>().color = Color.clear;
            var fRect = footer.GetComponent<RectTransform>();
            fRect.anchorMin = new Vector2(0f, 0f);
            fRect.anchorMax = new Vector2(1f, 0f);
            fRect.pivot = new Vector2(0f, 0f);
            fRect.anchoredPosition = new Vector2(0f, 0f);
            fRect.sizeDelta = new Vector2(0f, 52f);

            CreateTMP(footer.transform, "GoldText", "54 Gold", 13,
                new Color32(0xFA, 0xC7, 0x75, 0xFF), TextAlignmentOptions.MidlineLeft)
                .GetComponent<RectTransform>().offsetMin = new Vector2(16f, 0f);
            CreateTMP(footer.transform, "ShardText", "0 Shards", 13,
                new Color32(0x9B, 0x59, 0xB6, 0xFF), TextAlignmentOptions.MidlineRight)
                .GetComponent<RectTransform>().offsetMax = new Vector2(-16f, 0f);

            var bpUI = panel.AddComponent<BackpackPanelUI>();
            var inv = player.GetComponent<PlayerInventory>();
            var cur = player.GetComponent<PlayerCurrency>();
            var bpSO = new SerializedObject(bpUI);
            bpSO.FindProperty("inventory").objectReferenceValue = inv;
            bpSO.FindProperty("gridContainer").objectReferenceValue = grid.transform;
            bpSO.FindProperty("currency").objectReferenceValue = cur;
            bpSO.ApplyModifiedPropertiesWithoutUndo();

            return panel;
        }

        private static GameObject CreatePanelFrame(Transform parent, string name, float w, float h)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var img = panel.AddComponent<Image>();
            img.color = PanelBgInner;
            var outline = panel.AddComponent<Outline>();
            outline.effectColor = PanelBorder;
            outline.effectDistance = new Vector2(1f, 1f);
            var rect = panel.AddComponent<RectTransform>();
            var le = panel.AddComponent<LayoutElement>();
            le.preferredWidth = w;
            le.preferredHeight = h;
            rect.sizeDelta = new Vector2(w, h);
            return panel;
        }

        private static GameObject CreateHeader(Transform parent, string title, float panelWidth)
        {
            var header = new GameObject("Header");
            header.transform.SetParent(parent, false);
            header.AddComponent<Image>().color = HeaderBg;
            var hRect = header.GetComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0f, 1f);
            hRect.anchorMax = new Vector2(1f, 1f);
            hRect.pivot = new Vector2(0.5f, 1f);
            hRect.anchoredPosition = Vector2.zero;
            hRect.sizeDelta = new Vector2(0f, 52f);

            CreateTMP(header.transform, "Title", title, 13,
                (Color)TextPrimary, TextAlignmentOptions.MidlineLeft)
                .GetComponent<RectTransform>().offsetMin = new Vector2(16f, 0f);

            var xBtn = new GameObject("XButton");
            xBtn.transform.SetParent(header.transform, false);
            var xRect = xBtn.AddComponent<RectTransform>();
            xRect.anchorMin = new Vector2(1f, 0.5f);
            xRect.anchorMax = new Vector2(1f, 0.5f);
            xRect.pivot = new Vector2(1f, 0.5f);
            xRect.anchoredPosition = new Vector2(-12f, 0f);
            xRect.sizeDelta = new Vector2(22f, 22f);
            xBtn.AddComponent<Image>().color = XBtnBg;
            var closeBtn = xBtn.AddComponent<Button>();
            CreateTMP(xBtn.transform, "X", "✕", 14,
                (Color)XBtnIcon, TextAlignmentOptions.Center);

            var panelGroup = parent.parent?.gameObject ?? parent.gameObject;
            closeBtn.onClick.AddListener(() => { });

            var wirer = xBtn.AddComponent<CloseGroupWirer>();
            var wSO = new SerializedObject(wirer);
            wSO.FindProperty("targetGroup").objectReferenceValue = panelGroup;
            wSO.FindProperty("button").objectReferenceValue = closeBtn;
            wSO.ApplyModifiedPropertiesWithoutUndo();

            return header;
        }

        private static Transform CreateSlotColumn(Transform parent, string name, float x, float slotSize)
        {
            var col = new GameObject(name);
            col.transform.SetParent(parent, false);
            var cRect = col.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0f, 0f);
            cRect.anchorMax = new Vector2(0f, 1f);
            cRect.pivot = new Vector2(0f, 1f);
            cRect.anchoredPosition = new Vector2(x, 0f);
            cRect.sizeDelta = new Vector2(slotSize, 0f);
            var vl = col.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 8f;
            vl.childAlignment = TextAnchor.UpperCenter;
            vl.childForceExpandWidth = false;
            vl.childForceExpandHeight = false;
            vl.childControlWidth = false;
            vl.childControlHeight = false;
            return col.transform;
        }

        private static void CreateSlot(Transform parent, string label, Texture2D icon)
        {
            var slot = new GameObject(label);
            slot.transform.SetParent(parent, false);
            var le = slot.AddComponent<LayoutElement>();
            le.preferredWidth = 64f;
            le.preferredHeight = 64f;
            slot.AddComponent<RectTransform>().sizeDelta = new Vector2(64f, 64f);
            var bg = slot.AddComponent<Image>();
            bg.color = SlotBg;
            var outline = slot.AddComponent<Outline>();
            outline.effectColor = SlotBorderEmpty;
            outline.effectDistance = new Vector2(2f, 2f);

            if (icon != null)
            {
                var iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(slot.transform, false);
                var iRect = iconObj.AddComponent<RectTransform>();
                iRect.anchorMin = new Vector2(0.15f, 0.25f);
                iRect.anchorMax = new Vector2(0.85f, 0.9f);
                iRect.offsetMin = Vector2.zero;
                iRect.offsetMax = Vector2.zero;
                var iImg = iconObj.AddComponent<RawImage>();
                iImg.texture = icon;
                iImg.color = (Color)IconEmpty;
            }

            CreateTMP(slot.transform, "Label", label, 9,
                (Color)LabelColor, TextAlignmentOptions.BottomRight)
                .GetComponent<RectTransform>().offsetMax = new Vector2(-4f, 2f);

            slot.AddComponent<Button>();
        }

        private static void CreateEmptyInvSlot(Transform parent)
        {
            var slot = new GameObject("Empty");
            slot.transform.SetParent(parent, false);
            slot.AddComponent<Image>().color = SlotBg;
            var outline = slot.AddComponent<Outline>();
            outline.effectColor = SlotBorderFaint;
            outline.effectDistance = new Vector2(2f, 2f);
        }

        private static GameObject CreateTMP(Transform parent, string name, string text,
            int fontSize, Color color, TextAlignmentOptions align)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = align;
            tmp.richText = true;
            tmp.overflowMode = TextOverflowModes.Overflow;
            return obj;
        }

        private static void DestroyNamed(Transform root, string name)
        {
            var t = root.Find(name);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }
    }

    public class CloseGroupWirer : MonoBehaviour
    {
        [SerializeField] private GameObject targetGroup;
        [SerializeField] private Button button;

        private void Start()
        {
            if (button != null && targetGroup != null)
                button.onClick.AddListener(() => targetGroup.SetActive(false));
        }
    }
}
#endif
