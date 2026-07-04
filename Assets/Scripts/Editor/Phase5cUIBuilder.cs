using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// ============================================================
// Phase 5c UI Builder — HARDCODED from approved mockup.
// DO NOT modify sizes, colors, or layout without Jordon's approval.
// Run via: VoidBound > Build Phase 5c UI
// ============================================================
namespace VoidBound.Editor
{
    public static class Phase5cUIBuilder
    {
        // ── EXACT COLORS FROM APPROVED MOCKUP ──────────────────
        static readonly Color32 C_OUTER_BG      = new Color32(0x1a, 0x1f, 0x1a, 255);
        static readonly Color32 C_PANEL_BG       = new Color32(0x11, 0x14, 0x11, 255);
        static readonly Color32 C_HEADER_BG      = new Color32(0x0d, 0x11, 0x0d, 255);
        static readonly Color32 C_SLOT_BG        = new Color32(0x0a, 0x0d, 0x0a, 255);
        static readonly Color32 C_BORDER_EMPTY   = new Color32(0x3a, 0x3d, 0x3a, 255);
        static readonly Color32 C_BORDER_FAINT   = new Color32(0x1e, 0x22, 0x1e, 255);
        static readonly Color32 C_BORDER_PANEL   = new Color32(0x2a, 0x2f, 0x2a, 255);
        static readonly Color32 C_TEXT_PRIMARY   = new Color32(0xd4, 0xd8, 0xd0, 255);
        static readonly Color32 C_TEXT_MUTED     = new Color32(0x88, 0x8d, 0x84, 255);
        static readonly Color32 C_ICON_EMPTY     = new Color32(0x5a, 0x60, 0x55, 255);
        static readonly Color32 C_LABEL          = new Color32(0x3a, 0x3d, 0x3a, 255);
        static readonly Color32 C_X_BG           = new Color32(0x2a, 0x0d, 0x0d, 255);
        static readonly Color32 C_X_ICON         = new Color32(0xe2, 0x4b, 0x4a, 255);
        static readonly Color32 C_DIVIDER        = new Color32(0x1a, 0x1f, 0x1a, 255);
        static readonly Color32 C_INNER_CARD     = new Color32(0x0d, 0x11, 0x0d, 255);

        // ── STAT COLORS ─────────────────────────────────────────
        static readonly Color32 C_VIG = new Color32(0xe2, 0x4b, 0x4a, 255);
        static readonly Color32 C_STR = new Color32(0xfa, 0xc7, 0x75, 255);
        static readonly Color32 C_DEX = new Color32(0x97, 0xc4, 0x59, 255);
        static readonly Color32 C_INT = new Color32(0x37, 0x8a, 0xdd, 255);

        // ── RARITY COLORS ────────────────────────────────────────
        static readonly Color32 C_COMMON     = new Color32(0x9a, 0x9d, 0x92, 255);
        static readonly Color32 C_RARE       = new Color32(0x37, 0x8a, 0xdd, 255);
        static readonly Color32 C_EPIC       = new Color32(0x9b, 0x59, 0xb6, 255);
        static readonly Color32 C_LEGENDARY  = new Color32(0xfa, 0xc7, 0x75, 255);
        static readonly Color32 C_VOIDFORGED = new Color32(0xd4, 0x53, 0x7e, 255);

        // ── CURRENCY COLORS ──────────────────────────────────────
        static readonly Color32 C_GOLD  = new Color32(0xfa, 0xc7, 0x75, 255);
        static readonly Color32 C_SHARD = new Color32(0x9b, 0x59, 0xb6, 255);

        // ── SIZES (all in pixels, 1920x1080 reference) ───────────
        const float SLOT_SIZE       = 64f;
        const float SLOT_GAP        = 8f;
        const float PANEL_PAD       = 18f;
        const float HEADER_H        = 52f;
        const float FOOTER_H        = 44f;
        const float EQUIP_W         = 340f;
        const float INV_W           = 320f;
        const float PANEL_GAP       = 16f;
        const float BORDER_W        = 2f;
        const float CORNER_R        = 10f;
        const float SLOT_CORNER     = 6f;
        const float CENTER_COL_W    = 90f;
        const float SIDE_COL_W      = 64f;

        // ── FONT SIZES ───────────────────────────────────────────
        const float FS_HEADER  = 13f;
        const float FS_BODY    = 11f;
        const float FS_STAT    = 12f;
        const float FS_LEVEL   = 14f;
        const float FS_LABEL   = 8f;
        const float FS_ICON    = 22f;
        const float FS_CURRENCY= 13f;

        // Batch-mode entry point: opens Homestead, builds, saves.
        // Unity.exe -batchmode -projectPath ... -executeMethod VoidBound.Editor.Phase5cUIBuilder.BuildFromBatch -quit
        public static void BuildFromBatch()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Homestead.unity");
            BuildUI();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        [MenuItem("VoidBound/Build Phase 5c UI")]
        public static void BuildUI()
        {
            // ── Verify we're in Homestead scene ──────────────────
            if (SceneManager.GetActiveScene().name != "Homestead")
            {
                Debug.LogError("[Phase5c] Wrong scene. Open Homestead.unity first.");
                return;
            }

            // ── Find HUDCanvas ────────────────────────────────────
            var hudCanvasGO = GameObject.Find("HUDCanvas");
            if (hudCanvasGO == null)
            {
                Debug.LogError("[Phase5c] HUDCanvas not found in scene.");
                return;
            }
            var hudCanvas = hudCanvasGO.GetComponent<Canvas>();
            if (hudCanvas == null)
            {
                Debug.LogError("[Phase5c] HUDCanvas has no Canvas component.");
                return;
            }

            // ── Ensure Canvas Scaler is correct ─────────────────────
            var scaler = hudCanvasGO.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = hudCanvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // ── Remove old panels if they exist ───────────────────
            DestroyIfExists(hudCanvasGO, "EquipmentPanel");
            DestroyIfExists(hudCanvasGO, "InventoryPanel");
            DestroyIfExists(hudCanvasGO, "UIRoot5c");
            // Retire the Phase 3b panels (replaced by this UI; DevToolsPanel stays)
            DestroyIfExists(hudCanvasGO, "InventoryPanelGroup");
            DestroyIfExists(hudCanvasGO, "BackpackPanel");
            DestroyIfExists(hudCanvasGO, "PlayerInfoBar");

            // ── Root container (centers both panels) ─────────────
            var root = MakeRect("UIRoot5c", hudCanvasGO.transform);
            SetAnchor(root, Vector2.one * 0.5f, Vector2.one * 0.5f);
            root.sizeDelta = new Vector2(EQUIP_W + INV_W + PANEL_GAP, 600f);
            root.anchoredPosition = Vector2.zero;

            // ── Build panels ──────────────────────────────────────
            var equipPanel = BuildEquipmentPanel(root);
            var invPanel   = BuildInventoryPanel(root);

            // Position side by side (SetAnchor zeroes the offsets, so restore
            // each panel's size after re-anchoring)
            var equipSize = equipPanel.sizeDelta;
            SetAnchor(equipPanel, new Vector2(0, 0.5f), new Vector2(0, 0.5f));
            equipPanel.pivot = new Vector2(0, 0.5f);
            equipPanel.sizeDelta = equipSize;
            equipPanel.anchoredPosition = new Vector2(0, 0);

            var invSize = invPanel.sizeDelta;
            SetAnchor(invPanel, new Vector2(0, 0.5f), new Vector2(0, 0.5f));
            invPanel.pivot = new Vector2(0, 0.5f);
            invPanel.sizeDelta = invSize;
            invPanel.anchoredPosition = new Vector2(EQUIP_W + PANEL_GAP, 0);

            // ── Attach runtime controllers (Phase 5c data binding) ─
            var rootCtrl = root.gameObject.AddComponent<VoidBound.UI.Phase5cUIRoot>();
            var rootSO = new SerializedObject(rootCtrl);
            rootSO.FindProperty("equipmentPanel").objectReferenceValue = equipPanel.gameObject;
            rootSO.FindProperty("inventoryPanel").objectReferenceValue = invPanel.gameObject;
            rootSO.ApplyModifiedPropertiesWithoutUndo();
            equipPanel.gameObject.AddComponent<VoidBound.UI.EquipmentPanel5c>();
            invPanel.gameObject.AddComponent<VoidBound.UI.InventoryPanel5c>();

            // ── Player info bar (portrait + name + HP) ────────────
            BuildPlayerInfoBar(hudCanvasGO);

            // ── Both panels start hidden ──────────────────────────
            root.gameObject.SetActive(false);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[Phase5c] UI built with runtime controllers. Save the scene (Ctrl+S).");
        }

        // ═══════════════════════════════════════════════════════════
        // PLAYER INFO BAR (top-left: portrait, name, HP bar)
        // ═══════════════════════════════════════════════════════════
        static void BuildPlayerInfoBar(GameObject hudCanvas)
        {
            var bar = MakeRect("PlayerInfoBar", hudCanvas.transform);
            SetAnchor(bar, new Vector2(0, 1), new Vector2(0, 1));
            bar.pivot = new Vector2(0, 1);
            bar.anchoredPosition = new Vector2(12, -12);
            bar.sizeDelta = new Vector2(230, 54);

            // Portrait (round placeholder)
            var portrait = MakeRect("Portrait", bar);
            SetAnchor(portrait, new Vector2(0, 0.5f), new Vector2(0, 0.5f));
            portrait.pivot = new Vector2(0, 0.5f);
            portrait.anchoredPosition = new Vector2(2, 0);
            portrait.sizeDelta = new Vector2(42, 42);
            var pImg = AddImage(portrait.gameObject, C_ICON_EMPTY);
            pImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            var pLetter = MakeTMP("Letter", portrait);
            SetFullStretch(pLetter.rectTransform);
            pLetter.text = "P";
            pLetter.fontSize = 18f;
            pLetter.fontStyle = FontStyles.Bold;
            pLetter.color = C_HEADER_BG;
            pLetter.alignment = TextAlignmentOptions.Center;

            // Name label
            var nameTMP = MakeTMP("Name", bar);
            SetAnchor(nameTMP.rectTransform, new Vector2(0, 1), new Vector2(0, 1));
            nameTMP.rectTransform.pivot = new Vector2(0, 1);
            nameTMP.rectTransform.anchoredPosition = new Vector2(52, -4);
            nameTMP.rectTransform.sizeDelta = new Vector2(170, 16);
            nameTMP.text = "PLAYER";
            nameTMP.fontSize = 12f;
            nameTMP.fontStyle = FontStyles.Bold;
            nameTMP.color = C_TEXT_PRIMARY;
            nameTMP.characterSpacing = 3f;
            nameTMP.alignment = TextAlignmentOptions.MidlineLeft;

            // HP bar (green fill + current/max text)
            var hpBar = MakeRect("HPBar", bar);
            SetAnchor(hpBar, new Vector2(0, 0), new Vector2(0, 0));
            hpBar.pivot = new Vector2(0, 0);
            hpBar.anchoredPosition = new Vector2(52, 6);
            hpBar.sizeDelta = new Vector2(170, 16);
            AddImage(hpBar.gameObject, C_SLOT_BG);
            AddOutline(hpBar.gameObject, C_BORDER_PANEL, 1f);

            var fill = MakeRect("Fill", hpBar);
            SetFullStretch(fill);
            var fillImg = AddImage(fill.gameObject, C_DEX); // palette green
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 1f;

            // Legacy Text so HUDManager's existing hpText binding keeps working
            var hpTextGO = new GameObject("HPText", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            hpTextGO.transform.SetParent(hpBar, false);
            var hpTextRT = hpTextGO.GetComponent<RectTransform>();
            SetFullStretch(hpTextRT);
            var hpText = hpTextGO.GetComponent<UnityEngine.UI.Text>();
            hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hpText.fontSize = 10;
            hpText.alignment = TextAnchor.MiddleCenter;
            hpText.color = Color.white;
            hpText.text = "100/100";

            // Migrate off the old Phase 3b flat HP bar inside StatsPanel:
            // delete it and repoint HUDManager's serialized refs at the new bar.
            var hudManager = hudCanvas.GetComponent<VoidBound.UI.HUDManager>();
            var statsPanel = hudCanvas.transform.Find("StatsPanel");
            bool migrated = false;
            if (statsPanel != null)
            {
                var oldHpBar = FindDeep(statsPanel, "HPBar");
                var oldHpText = FindDeep(statsPanel, "HPText");
                if (oldHpBar != null) { Object.DestroyImmediate(oldHpBar.gameObject); migrated = true; }
                if (oldHpText != null && oldHpText.gameObject != null)
                    Object.DestroyImmediate(oldHpText.gameObject);

                // Make room for the info bar (only on first migration)
                if (migrated)
                {
                    var spRect = statsPanel.GetComponent<RectTransform>();
                    if (spRect != null)
                        spRect.anchoredPosition += new Vector2(0, -60);
                }
            }

            if (hudManager != null)
            {
                var so = new SerializedObject(hudManager);
                so.FindProperty("hpFill").objectReferenceValue = fillImg;
                so.FindProperty("hpText").objectReferenceValue = hpText;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform child in root)
            {
                if (child.name == name) return child;
                var found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }

        // ═══════════════════════════════════════════════════════════
        // EQUIPMENT PANEL
        // ═══════════════════════════════════════════════════════════
        static RectTransform BuildEquipmentPanel(RectTransform parent)
        {
            // Panel background
            var panel = MakeRect("EquipmentPanel", parent);
            panel.sizeDelta = new Vector2(EQUIP_W, 0); // height auto from children
            AddImage(panel.gameObject, C_PANEL_BG);
            AddOutline(panel.gameObject, C_BORDER_PANEL, BORDER_W);

            // ── Header ────────────────────────────────────────────
            var header = MakeRect("Header", panel);
            SetAnchor(header, new Vector2(0,1), new Vector2(1,1));
            header.sizeDelta = new Vector2(0, HEADER_H);
            header.anchoredPosition = Vector2.zero;
            header.pivot = new Vector2(0.5f, 1f);
            AddImage(header.gameObject, C_HEADER_BG);
            AddBottomBorder(header.gameObject, C_BORDER_PANEL);

            var titleText = MakeTMP("Title", header);
            SetAnchor(titleText.rectTransform, new Vector2(0,0), new Vector2(1,1));
            titleText.rectTransform.offsetMin = new Vector2(18, 0);
            titleText.rectTransform.offsetMax = new Vector2(-50, 0);
            titleText.text = "EQUIPMENT";
            titleText.fontSize = FS_HEADER;
            titleText.color = C_TEXT_PRIMARY;
            titleText.fontStyle = FontStyles.Normal;
            titleText.alignment = TextAlignmentOptions.MidlineLeft;
            titleText.characterSpacing = 4f;

            // X button
            var xBtn = MakeRect("CloseBtn", header);
            SetAnchor(xBtn, new Vector2(1,0.5f), new Vector2(1,0.5f));
            xBtn.sizeDelta = new Vector2(22, 22);
            xBtn.anchoredPosition = new Vector2(-14, 0);
            xBtn.pivot = new Vector2(0.5f, 0.5f);
            AddImage(xBtn.gameObject, C_X_BG);
            AddRoundedCorner(xBtn.gameObject, 4f);
            var xLabel = MakeTMP("X", xBtn);
            SetFullStretch(xLabel.rectTransform);
            xLabel.text = "X"; // ASCII per standing icon-rendering rule (TMP default font lacks U+2715)
            xLabel.fontSize = 12f;
            xLabel.color = C_X_ICON;
            xLabel.alignment = TextAlignmentOptions.Center;

            // ── Body (3-column flex) ──────────────────────────────
            var body = MakeRect("Body", panel);
            SetAnchor(body, new Vector2(0,0), new Vector2(1,1));
            body.offsetMin = new Vector2(PANEL_PAD, PANEL_PAD + 80f); // leave room for dock
            body.offsetMax = new Vector2(-PANEL_PAD, -HEADER_H);

            // Left column
            var leftCol = MakeRect("LeftCol", body);
            SetAnchor(leftCol, new Vector2(0,0), new Vector2(0,1));
            leftCol.sizeDelta = new Vector2(SIDE_COL_W, 0);
            leftCol.anchoredPosition = Vector2.zero;
            leftCol.pivot = new Vector2(0, 0.5f);

            string[] leftSlots  = { "HELM", "BODY", "LEGS", "BOOTS", "GLVS" };
            BuildSlotColumn(leftCol, leftSlots, 5);

            // Right column
            var rightCol = MakeRect("RightCol", body);
            SetAnchor(rightCol, new Vector2(1,0), new Vector2(1,1));
            rightCol.sizeDelta = new Vector2(SIDE_COL_W, 0);
            rightCol.anchoredPosition = Vector2.zero;
            rightCol.pivot = new Vector2(1, 0.5f);

            string[] rightSlots = { "AMLT", "RING", "RING", "CAPE" };
            BuildSlotColumn(rightCol, rightSlots, 4);

            // Center stat card
            var center = MakeRect("StatCenter", body);
            SetAnchor(center, new Vector2(0,0), new Vector2(1,1));
            center.offsetMin = new Vector2(SIDE_COL_W + SLOT_GAP, 0);
            center.offsetMax = new Vector2(-(SIDE_COL_W + SLOT_GAP), 0);
            BuildStatCenter(center);

            // ── Bottom dock (Weapon + Shield) ─────────────────────
            var dock = MakeRect("Dock", panel);
            SetAnchor(dock, new Vector2(0,0), new Vector2(1,0));
            dock.sizeDelta = new Vector2(0, SLOT_SIZE + PANEL_PAD * 1.5f);
            dock.anchoredPosition = Vector2.zero;
            dock.pivot = new Vector2(0.5f, 0);
            BuildDock(dock);

            // Size the panel height to fit
            float colHeight = 5 * SLOT_SIZE + 4 * SLOT_GAP;
            float panelH = HEADER_H + PANEL_PAD + colHeight + PANEL_PAD + SLOT_SIZE + PANEL_PAD * 1.5f;
            panel.sizeDelta = new Vector2(EQUIP_W, panelH);

            return panel;
        }

        static void BuildSlotColumn(RectTransform parent, string[] labels, int count)
        {
            var vLayout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = SLOT_GAP;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;
            vLayout.childAlignment = TextAnchor.UpperCenter;

            var fitter = parent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            for (int i = 0; i < count; i++)
                BuildSlot(parent, labels[i], C_BORDER_EMPTY, C_ICON_EMPTY, false);
        }

        static void BuildDock(RectTransform parent)
        {
            var hLayout = parent.gameObject.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = SLOT_GAP;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = false;
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.padding = new RectOffset((int)PANEL_PAD, (int)PANEL_PAD, (int)(PANEL_PAD * 0.5f), (int)(PANEL_PAD * 0.5f));

            // Weapon slot — shown as filled/Legendary as example
            BuildSlot(parent, "WPGN", C_LEGENDARY, C_LEGENDARY, true);
            // Shield slot — empty
            BuildSlot(parent, "SHLD", C_BORDER_EMPTY, C_ICON_EMPTY, false);
        }

        static void BuildSlot(RectTransform parent, string label, Color32 borderColor, Color32 iconColor, bool filled)
        {
            var slot = MakeRect("Slot_" + label, parent);
            slot.sizeDelta = new Vector2(SLOT_SIZE, SLOT_SIZE);
            AddImage(slot.gameObject, C_SLOT_BG);
            AddOutline(slot.gameObject, borderColor, BORDER_W);

            // Icon label (abbreviated slot name as stand-in)
            var iconTMP = MakeTMP("Icon", slot);
            SetAnchor(iconTMP.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            iconTMP.rectTransform.anchoredPosition = new Vector2(0, 4f);
            iconTMP.rectTransform.sizeDelta = new Vector2(SLOT_SIZE - 8, SLOT_SIZE - 8);
            iconTMP.text = SlotIconChar(label);
            iconTMP.fontSize = FS_ICON;
            iconTMP.color = iconColor;
            iconTMP.alignment = TextAlignmentOptions.Center;

            // Corner label
            var lblTMP = MakeTMP("Label", slot);
            SetAnchor(lblTMP.rectTransform, new Vector2(1,0), new Vector2(1,0));
            lblTMP.rectTransform.anchoredPosition = new Vector2(-4, 4);
            lblTMP.rectTransform.sizeDelta = new Vector2(40, 12);
            lblTMP.rectTransform.pivot = new Vector2(1, 0);
            lblTMP.text = label;
            lblTMP.fontSize = FS_LABEL;
            lblTMP.color = filled ? (Color)iconColor : (Color)C_LABEL;
            lblTMP.alignment = TextAlignmentOptions.Right;

            // Layout element so LayoutGroup respects slot size
            var le = slot.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = SLOT_SIZE;
            le.preferredHeight = SLOT_SIZE;
            le.minWidth = SLOT_SIZE;
            le.minHeight = SLOT_SIZE;
        }

        static void BuildStatCenter(RectTransform parent)
        {
            // Fixed manual layout: a ContentSizeFitter nested inside a parent
            // LayoutGroup is unsupported by uGUI and mis-sizes the card.

            // Level header (fixed at top)
            var levelTMP = MakeTMP("Level", parent);
            SetAnchor(levelTMP.rectTransform, new Vector2(0, 1), new Vector2(1, 1));
            levelTMP.rectTransform.pivot = new Vector2(0.5f, 1f);
            levelTMP.rectTransform.sizeDelta = new Vector2(0, 22f);
            levelTMP.rectTransform.anchoredPosition = Vector2.zero;
            levelTMP.text = "Level 1";
            levelTMP.fontSize = FS_LEVEL;
            levelTMP.color = C_TEXT_PRIMARY;
            levelTMP.fontStyle = FontStyles.Bold;
            levelTMP.alignment = TextAlignmentOptions.Center;

            // Inner card bg (fixed height, below the level header)
            var card = MakeRect("StatCard", parent);
            SetAnchor(card, new Vector2(0, 1), new Vector2(1, 1));
            card.pivot = new Vector2(0.5f, 1f);
            card.sizeDelta = new Vector2(0, 150f);
            card.anchoredPosition = new Vector2(0, -26f);
            AddImage(card.gameObject, C_INNER_CARD);
            AddRoundedCorner(card.gameObject, 4f);

            var cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(8, 8, 8, 8);
            cardLayout.spacing = 4f;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;

            // Damage / Defense rows
            AddStatRow(card, "Damage",  "10", C_TEXT_MUTED, C_TEXT_PRIMARY, FS_BODY);
            AddStatRow(card, "Defense", "0",  C_TEXT_MUTED, C_TEXT_PRIMARY, FS_BODY);

            // Divider
            var div = MakeRect("Divider", card);
            AddImage(div.gameObject, C_DIVIDER);
            AddLayoutElement(div.gameObject, -1, 1f);

            // Stat rows
            AddStatRow(card, "VIG", "1", C_VIG, C_TEXT_PRIMARY, FS_STAT, true);
            AddStatRow(card, "STR", "1", C_STR, C_TEXT_PRIMARY, FS_STAT, true);
            AddStatRow(card, "DEX", "1", C_DEX, C_TEXT_PRIMARY, FS_STAT, true);
            AddStatRow(card, "INT", "1", C_INT, C_TEXT_PRIMARY, FS_STAT, true);
        }

        // ═══════════════════════════════════════════════════════════
        // INVENTORY PANEL
        // ═══════════════════════════════════════════════════════════
        static RectTransform BuildInventoryPanel(RectTransform parent)
        {
            var panel = MakeRect("InventoryPanel", parent);
            AddImage(panel.gameObject, C_PANEL_BG);
            AddOutline(panel.gameObject, C_BORDER_PANEL, BORDER_W);

            // ── Header ────────────────────────────────────────────
            var header = MakeRect("Header", panel);
            SetAnchor(header, new Vector2(0,1), new Vector2(1,1));
            header.sizeDelta = new Vector2(0, HEADER_H);
            header.anchoredPosition = Vector2.zero;
            header.pivot = new Vector2(0.5f, 1f);
            AddImage(header.gameObject, C_HEADER_BG);
            AddBottomBorder(header.gameObject, C_BORDER_PANEL);

            var titleTMP = MakeTMP("Title", header);
            SetAnchor(titleTMP.rectTransform, new Vector2(0,0), new Vector2(1,1));
            titleTMP.rectTransform.offsetMin = new Vector2(18, 0);
            titleTMP.rectTransform.offsetMax = new Vector2(-80, 0);
            titleTMP.text = "INVENTORY";
            titleTMP.fontSize = FS_HEADER;
            titleTMP.color = C_TEXT_PRIMARY;
            titleTMP.alignment = TextAlignmentOptions.MidlineLeft;
            titleTMP.characterSpacing = 4f;

            var capacityTMP = MakeTMP("Capacity", header);
            SetAnchor(capacityTMP.rectTransform, new Vector2(1,0), new Vector2(1,1));
            capacityTMP.rectTransform.sizeDelta = new Vector2(60, 0);
            capacityTMP.rectTransform.anchoredPosition = new Vector2(-40, 0);
            capacityTMP.rectTransform.pivot = new Vector2(1, 0.5f);
            capacityTMP.text = "0 / 24";
            capacityTMP.fontSize = FS_BODY;
            capacityTMP.color = C_ICON_EMPTY;
            capacityTMP.alignment = TextAlignmentOptions.Right;

            var xBtn = MakeRect("CloseBtn", header);
            SetAnchor(xBtn, new Vector2(1,0.5f), new Vector2(1,0.5f));
            xBtn.sizeDelta = new Vector2(22, 22);
            xBtn.anchoredPosition = new Vector2(-14, 0);
            xBtn.pivot = new Vector2(0.5f, 0.5f);
            AddImage(xBtn.gameObject, C_X_BG);
            var xLabel = MakeTMP("X", xBtn);
            SetFullStretch(xLabel.rectTransform);
            xLabel.text = "X"; // ASCII per standing icon-rendering rule (TMP default font lacks U+2715)
            xLabel.fontSize = 12f;
            xLabel.color = C_X_ICON;
            xLabel.alignment = TextAlignmentOptions.Center;

            // ── Grid (4 columns, scrollable viewport) ─────────────
            var viewport = MakeRect("GridViewport", panel);
            SetAnchor(viewport, new Vector2(0,0), new Vector2(1,1));
            viewport.offsetMin = new Vector2(PANEL_PAD, FOOTER_H + PANEL_PAD * 0.5f);
            viewport.offsetMax = new Vector2(-PANEL_PAD, -HEADER_H - PANEL_PAD * 0.5f);
            viewport.gameObject.AddComponent<RectMask2D>();

            var grid = MakeRect("Grid", viewport);
            SetAnchor(grid, new Vector2(0,1), new Vector2(1,1));
            grid.pivot = new Vector2(0.5f, 1f);
            grid.anchoredPosition = Vector2.zero;

            var gridFitter = grid.gameObject.AddComponent<ContentSizeFitter>();
            gridFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = grid;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;

            var gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(SLOT_SIZE, SLOT_SIZE);
            gridLayout.spacing = new Vector2(SLOT_GAP, SLOT_GAP);
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.UpperLeft;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 4;

            // Example filled slots (rarity demo)
            BuildInvSlot(grid, "ti-sword", C_RARE,       "RARE",  true);
            BuildInvSlot(grid, "ti-sword", C_LEGENDARY,  "LEG",   true);
            BuildInvSlot(grid, "ti-flask", C_EPIC,       "×3",    true);
            BuildInvSlot(grid, "ti-sword", C_VOIDFORGED, "VOID",  true);
            // Empty slots
            for (int i = 0; i < 8; i++)
                BuildInvSlot(grid, "", C_BORDER_FAINT, "", false);

            // ── Footer ────────────────────────────────────────────
            var footer = MakeRect("Footer", panel);
            SetAnchor(footer, new Vector2(0,0), new Vector2(1,0));
            footer.sizeDelta = new Vector2(0, FOOTER_H);
            footer.anchoredPosition = Vector2.zero;
            footer.pivot = new Vector2(0.5f, 0);
            AddTopBorder(footer.gameObject, C_BORDER_FAINT);

            var footerLayout = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
            footerLayout.padding = new RectOffset((int)PANEL_PAD, (int)PANEL_PAD, 0, 0);
            footerLayout.spacing = 20f;
            footerLayout.childAlignment = TextAnchor.MiddleLeft;
            footerLayout.childControlWidth = true;
            footerLayout.childControlHeight = true;
            footerLayout.childForceExpandWidth = false;
            footerLayout.childForceExpandHeight = false;

            var goldTMP = MakeTMP("Gold", footer);
            goldTMP.text = "Gold 54";
            goldTMP.fontSize = FS_CURRENCY;
            goldTMP.color = C_GOLD;
            AddLayoutElement(goldTMP.gameObject, -1, FOOTER_H);

            var shardTMP = MakeTMP("Shards", footer);
            shardTMP.text = "Shards 0";
            shardTMP.fontSize = FS_CURRENCY;
            shardTMP.color = C_SHARD;
            AddLayoutElement(shardTMP.gameObject, -1, FOOTER_H);

            // Panel height
            int rows = 3;
            float gridH = rows * SLOT_SIZE + (rows - 1) * SLOT_GAP;
            float panelH = HEADER_H + PANEL_PAD + gridH + PANEL_PAD * 0.5f + FOOTER_H;
            panel.sizeDelta = new Vector2(INV_W, panelH);

            return panel;
        }

        static void BuildInvSlot(RectTransform parent, string iconKey, Color32 borderColor, string badge, bool filled)
        {
            var slot = MakeRect("InvSlot", parent);
            AddImage(slot.gameObject, C_SLOT_BG);
            AddOutline(slot.gameObject, borderColor, BORDER_W);

            if (filled && iconKey.Length > 0)
            {
                var iconTMP = MakeTMP("Icon", slot);
                SetAnchor(iconTMP.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                iconTMP.rectTransform.anchoredPosition = new Vector2(0, 4);
                iconTMP.rectTransform.sizeDelta = new Vector2(SLOT_SIZE - 8, SLOT_SIZE - 8);
                iconTMP.text = InvIconChar(iconKey);
                iconTMP.fontSize = 22f;
                iconTMP.color = borderColor;
                iconTMP.alignment = TextAlignmentOptions.Center;

                if (badge.Length > 0)
                {
                    var badgeTMP = MakeTMP("Badge", slot);
                    SetAnchor(badgeTMP.rectTransform, new Vector2(1,0), new Vector2(1,0));
                    badgeTMP.rectTransform.anchoredPosition = new Vector2(-3, 3);
                    badgeTMP.rectTransform.sizeDelta = new Vector2(36, 12);
                    badgeTMP.rectTransform.pivot = new Vector2(1, 0);
                    badgeTMP.text = badge;
                    badgeTMP.fontSize = FS_LABEL;
                    badgeTMP.fontStyle = FontStyles.Bold;
                    badgeTMP.color = borderColor;
                    badgeTMP.alignment = TextAlignmentOptions.Right;
                }
            }
        }

        // NOTE: HUD button wiring happens at runtime in HUDManager (Equip/Bag →
        // Phase5cUIRoot toggles). Editor-time onClick.AddListener calls are
        // non-persistent and would not survive scene serialization.

        // ═══════════════════════════════════════════════════════════
        // HELPER UTILITIES
        // ═══════════════════════════════════════════════════════════
        static string SlotIconChar(string label) => label switch
        {
            "HELM"  => "H",
            "BODY"  => "B",
            "LEGS"  => "L",
            "BOOTS" => "Bo",
            "GLVS"  => "G",
            "AMLT"  => "A",
            "RING"  => "R",
            "CAPE"  => "C",
            "WPGN"  => "W",
            "SHLD"  => "S",
            _ => "-"
        };

        static string InvIconChar(string key) => key switch
        {
            "ti-sword" => "W",
            "ti-flask" => "F",
            _ => "-"
        };

        static RectTransform MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        static TextMeshProUGUI MakeTMP(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            return go.GetComponent<TextMeshProUGUI>();
        }

        static Image AddImage(GameObject go, Color32 color)
        {
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static void AddOutline(GameObject go, Color32 color, float width)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(width, -width);
        }

        static void AddBottomBorder(GameObject go, Color32 color)
        {
            var border = MakeRect("BottomBorder", go.transform);
            SetAnchor(border, new Vector2(0,0), new Vector2(1,0));
            border.sizeDelta = new Vector2(0, 1);
            border.anchoredPosition = Vector2.zero;
            border.pivot = new Vector2(0.5f, 0);
            AddImage(border.gameObject, color);
        }

        static void AddTopBorder(GameObject go, Color32 color)
        {
            var border = MakeRect("TopBorder", go.transform);
            SetAnchor(border, new Vector2(0,1), new Vector2(1,1));
            border.sizeDelta = new Vector2(0, 1);
            border.anchoredPosition = Vector2.zero;
            border.pivot = new Vector2(0.5f, 1);
            AddImage(border.gameObject, color);
        }

        static void AddRoundedCorner(GameObject go, float radius)
        {
            // Note: true rounded corners require a sprite with border or UI Toolkit.
            // This is a no-op placeholder — visual rounding is approximated by the dark bg.
        }

        static void SetAnchor(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void SetFullStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void AddStatRow(RectTransform parent, string lbl, string val,
                               Color32 lblColor, Color32 valColor, float fontSize, bool bold = false)
        {
            var row = MakeRect("Row_" + lbl, parent);
            AddLayoutElement(row.gameObject, -1, fontSize + 6f);

            var rowH = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowH.childForceExpandWidth = true;
            rowH.childForceExpandHeight = true;
            rowH.childControlWidth = true;
            rowH.childControlHeight = true;

            var lblTMP = MakeTMP("Lbl", row);
            lblTMP.text = lbl;
            lblTMP.fontSize = fontSize;
            lblTMP.color = lblColor;
            lblTMP.alignment = TextAlignmentOptions.MidlineLeft;
            if (bold) lblTMP.fontStyle = FontStyles.Bold;

            var valTMP = MakeTMP("Val", row);
            valTMP.text = val;
            valTMP.fontSize = fontSize;
            valTMP.color = valColor;
            valTMP.alignment = TextAlignmentOptions.MidlineRight;
        }

        static void AddLayoutElement(GameObject go, float preferredWidth, float preferredHeight)
        {
            var le = go.AddComponent<LayoutElement>();
            if (preferredWidth > 0)  le.preferredWidth  = preferredWidth;
            if (preferredHeight > 0) le.preferredHeight = preferredHeight;
        }

        static void DestroyIfExists(GameObject parent, string name)
        {
            var existing = parent.transform.Find(name);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            // Also search UIRoot5c child
            var root = parent.transform.Find("UIRoot5c");
            if (root != null)
            {
                var child = root.Find(name);
                if (child != null) Object.DestroyImmediate(child.gameObject);
            }
        }
    }
}
