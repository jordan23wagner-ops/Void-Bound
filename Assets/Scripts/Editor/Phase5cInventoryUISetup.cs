#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VoidBound.Combat;
using VoidBound.Inventory;
using VoidBound.UI;

namespace VoidBound.Editor
{
    public static class Phase5cInventoryUISetup
    {
        [MenuItem("VoidBound/Setup Phase 5c - Inventory UI")]
        public static void Setup()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) { Debug.LogError("No Player found."); return; }

            var hudCanvas = Object.FindAnyObjectByType<HUDManager>()?.GetComponent<Canvas>();
            if (hudCanvas == null) { Debug.LogError("No HUD Canvas found. Run Setup Homestead Scene first."); return; }
            var canvasObj = hudCanvas.gameObject;

            DestroyExisting(canvasObj, "EquipmentPanel");
            DestroyExisting(canvasObj, "BackpackPanel");
            DestroyExisting(canvasObj, "PlayerInfoBar");

            BuildPlayerInfoBar(canvasObj.transform, player);
            var equipPanel = BuildEquipmentPanel(canvasObj.transform, player);
            var invPanel = BuildInventoryPanel(canvasObj.transform, player);

            var hm = canvasObj.GetComponent<HUDManager>();
            if (hm != null)
            {
                var hmSO = new SerializedObject(hm);
                hmSO.FindProperty("equipmentPanel").objectReferenceValue = equipPanel;
                hmSO.FindProperty("backpackPanel").objectReferenceValue = invPanel;
                hmSO.ApplyModifiedPropertiesWithoutUndo();
            }

            Debug.Log("[Phase 5c] Inventory UI rebuilt. Save the scene.");
        }

        private static void DestroyExisting(GameObject canvas, string name)
        {
            var t = canvas.transform.Find(name);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        private static void BuildPlayerInfoBar(Transform parent, GameObject player)
        {
            var bar = new GameObject("PlayerInfoBar");
            bar.transform.SetParent(parent, false);
            bar.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.85f);
            var barRect = bar.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 1f);
            barRect.anchorMax = new Vector2(0f, 1f);
            barRect.pivot = new Vector2(0f, 1f);
            barRect.anchoredPosition = new Vector2(10f, -10f);
            barRect.sizeDelta = new Vector2(200f, 50f);

            var portrait = new GameObject("Portrait");
            portrait.transform.SetParent(bar.transform, false);
            var pRect = portrait.AddComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0f, 0f);
            pRect.anchorMax = new Vector2(0f, 1f);
            pRect.offsetMin = new Vector2(5f, 5f);
            pRect.offsetMax = new Vector2(45f, -5f);
            portrait.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.35f, 1f);

            MakeText(bar.transform, "NameLabel", "PLAYER", 13,
                new Vector2(50f, -5f), new Vector2(195f, -20f), TextAnchor.MiddleLeft);

            var hpBg = new GameObject("HPBarBG");
            hpBg.transform.SetParent(bar.transform, false);
            var hpBgRect = hpBg.AddComponent<RectTransform>();
            hpBgRect.anchorMin = new Vector2(0f, 0f);
            hpBgRect.anchorMax = new Vector2(1f, 0f);
            hpBgRect.offsetMin = new Vector2(50f, 5f);
            hpBgRect.offsetMax = new Vector2(-5f, 22f);
            hpBg.AddComponent<Image>().color = new Color(0.25f, 0.08f, 0.08f, 1f);

            var hpFill = new GameObject("HPFill");
            hpFill.transform.SetParent(hpBg.transform, false);
            var hpFillRect = hpFill.AddComponent<RectTransform>();
            hpFillRect.anchorMin = Vector2.zero;
            hpFillRect.anchorMax = Vector2.one;
            hpFillRect.offsetMin = Vector2.zero;
            hpFillRect.offsetMax = Vector2.zero;
            var hpFillImg = hpFill.AddComponent<Image>();
            hpFillImg.color = new Color(0.2f, 0.75f, 0.2f, 1f);
            hpFillImg.type = Image.Type.Filled;
            hpFillImg.fillMethod = Image.FillMethod.Horizontal;

            var hpText = MakeText(hpBg.transform, "HPText", "200/200", 10,
                new Vector2(4f, 0f), new Vector2(-4f, 0f), TextAnchor.MiddleCenter);
            hpText.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            hpText.GetComponent<RectTransform>().anchorMax = Vector2.one;

            var hm = parent.GetComponent<HUDManager>();
            if (hm != null)
            {
                var so = new SerializedObject(hm);
                so.FindProperty("hpFill").objectReferenceValue = hpFillImg;
                so.FindProperty("hpText").objectReferenceValue = hpText.GetComponent<Text>();
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static GameObject BuildEquipmentPanel(Transform parent, GameObject player)
        {
            var panel = CreatePanel(parent, "EquipmentPanel", "EQUIPMENT",
                new Vector2(0.03f, 0.08f), new Vector2(0.48f, 0.92f));

            var leftCol = CreateLayoutGroup(panel.transform, "LeftColumn",
                new Vector2(0.02f, 0.18f), new Vector2(0.22f, 0.9f), true);
            var rightCol = CreateLayoutGroup(panel.transform, "RightColumn",
                new Vector2(0.78f, 0.18f), new Vector2(0.98f, 0.9f), true);

            var statReadout = MakeText(panel.transform, "StatReadout", "Loading...", 13,
                new Vector2(0.24f, 0.18f), new Vector2(0.76f, 0.9f), TextAnchor.UpperCenter);
            statReadout.GetComponent<RectTransform>().anchorMin = new Vector2(0.24f, 0.18f);
            statReadout.GetComponent<RectTransform>().anchorMax = new Vector2(0.76f, 0.9f);
            statReadout.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            statReadout.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            var weaponDock = CreateLayoutGroup(panel.transform, "WeaponDock",
                new Vector2(0.25f, 0.02f), new Vector2(0.75f, 0.16f), false);

            var detailPanel = BuildDetailPanel(panel.transform, true);

            var eqUI = panel.AddComponent<EquipmentPanelUI>();
            var inv = player.GetComponent<PlayerInventory>();
            var so = new SerializedObject(eqUI);
            so.FindProperty("inventory").objectReferenceValue = inv;
            so.FindProperty("leftColumn").objectReferenceValue = leftCol.transform;
            so.FindProperty("rightColumn").objectReferenceValue = rightCol.transform;
            so.FindProperty("weaponDock").objectReferenceValue = weaponDock.transform;
            so.FindProperty("statReadout").objectReferenceValue = statReadout.GetComponent<Text>();
            so.FindProperty("detailPanel").objectReferenceValue = detailPanel;
            so.FindProperty("detailName").objectReferenceValue = detailPanel.transform.Find("DetailName")?.GetComponent<Text>();
            so.FindProperty("detailRarity").objectReferenceValue = detailPanel.transform.Find("DetailRarity")?.GetComponent<Text>();
            so.FindProperty("detailSlot").objectReferenceValue = detailPanel.transform.Find("DetailSlot")?.GetComponent<Text>();
            so.FindProperty("detailStats").objectReferenceValue = detailPanel.transform.Find("DetailStats")?.GetComponent<Text>();
            so.FindProperty("detailSet").objectReferenceValue = detailPanel.transform.Find("DetailSet")?.GetComponent<Text>();
            so.FindProperty("unequipButton").objectReferenceValue = detailPanel.transform.Find("ActionButton")?.GetComponent<Button>();
            so.FindProperty("closeButton").objectReferenceValue = panel.transform.Find("CloseButton")?.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false);
            return panel;
        }

        private static GameObject BuildInventoryPanel(Transform parent, GameObject player)
        {
            var panel = CreatePanel(parent, "BackpackPanel", "INVENTORY",
                new Vector2(0.52f, 0.08f), new Vector2(0.97f, 0.92f));

            var grid = new GameObject("Grid");
            grid.transform.SetParent(panel.transform, false);
            var gridRect = grid.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.02f, 0.12f);
            gridRect.anchorMax = new Vector2(0.98f, 0.88f);
            gridRect.offsetMin = Vector2.zero;
            gridRect.offsetMax = Vector2.zero;
            var gridLayout = grid.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(52f, 52f);
            gridLayout.spacing = new Vector2(4f, 4f);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 5;
            gridLayout.childAlignment = TextAnchor.UpperLeft;

            var capacityText = MakeText(panel.transform, "CapacityText", "0/24", 12,
                new Vector2(0.6f, 0.89f), new Vector2(0.95f, 0.97f), TextAnchor.MiddleRight);
            capacityText.GetComponent<RectTransform>().anchorMin = new Vector2(0.6f, 0.89f);
            capacityText.GetComponent<RectTransform>().anchorMax = new Vector2(0.95f, 0.97f);
            capacityText.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            capacityText.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            var currencyText = MakeText(panel.transform, "CurrencyText", "Gold: 0    Shards: 0", 12,
                new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.1f), TextAnchor.MiddleLeft);
            currencyText.GetComponent<RectTransform>().anchorMin = new Vector2(0.02f, 0.02f);
            currencyText.GetComponent<RectTransform>().anchorMax = new Vector2(0.98f, 0.1f);
            currencyText.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            currencyText.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            var detailPanel = BuildDetailPanel(panel.transform, false);

            var bpUI = panel.AddComponent<BackpackPanelUI>();
            var inv = player.GetComponent<PlayerInventory>();
            var cur = player.GetComponent<PlayerCurrency>();
            var so = new SerializedObject(bpUI);
            so.FindProperty("inventory").objectReferenceValue = inv;
            so.FindProperty("gridContainer").objectReferenceValue = grid.transform;
            so.FindProperty("capacityText").objectReferenceValue = capacityText.GetComponent<Text>();
            so.FindProperty("currencyText").objectReferenceValue = currencyText.GetComponent<Text>();
            so.FindProperty("currency").objectReferenceValue = cur;
            so.FindProperty("detailPanel").objectReferenceValue = detailPanel;
            so.FindProperty("detailName").objectReferenceValue = detailPanel.transform.Find("DetailName")?.GetComponent<Text>();
            so.FindProperty("detailRarity").objectReferenceValue = detailPanel.transform.Find("DetailRarity")?.GetComponent<Text>();
            so.FindProperty("detailSlot").objectReferenceValue = detailPanel.transform.Find("DetailSlot")?.GetComponent<Text>();
            so.FindProperty("detailStats").objectReferenceValue = detailPanel.transform.Find("DetailStats")?.GetComponent<Text>();
            so.FindProperty("equipButton").objectReferenceValue = detailPanel.transform.Find("ActionButton")?.GetComponent<Button>();
            so.FindProperty("closeButton").objectReferenceValue = panel.transform.Find("CloseButton")?.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false);
            return panel;
        }

        private static GameObject CreatePanel(Transform parent, string name, string title,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            panel.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.1f, 0.94f);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            MakeText(panel.transform, "Title", title, 18,
                new Vector2(0.05f, 0.92f), new Vector2(0.8f, 0.99f), TextAnchor.MiddleLeft)
                .GetComponent<RectTransform>().offsetMin = Vector2.zero;

            var closeBtn = new GameObject("CloseButton");
            closeBtn.transform.SetParent(panel.transform, false);
            var cbRect = closeBtn.AddComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.88f, 0.92f);
            cbRect.anchorMax = new Vector2(0.98f, 0.99f);
            cbRect.offsetMin = Vector2.zero;
            cbRect.offsetMax = Vector2.zero;
            closeBtn.AddComponent<Image>().color = new Color(0.5f, 0.12f, 0.12f, 1f);
            closeBtn.AddComponent<Button>();
            MakeText(closeBtn.transform, "X", "X", 16,
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter)
                .GetComponent<RectTransform>().anchorMax = Vector2.one;

            return panel;
        }

        private static GameObject CreateLayoutGroup(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, bool vertical)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            if (vertical)
            {
                var vl = obj.AddComponent<VerticalLayoutGroup>();
                vl.spacing = 4f;
                vl.childForceExpandWidth = true;
                vl.childForceExpandHeight = false;
                vl.childControlWidth = true;
                vl.childControlHeight = true;
                vl.childAlignment = TextAnchor.UpperCenter;
            }
            else
            {
                var hl = obj.AddComponent<HorizontalLayoutGroup>();
                hl.spacing = 8f;
                hl.childForceExpandWidth = false;
                hl.childForceExpandHeight = true;
                hl.childControlWidth = true;
                hl.childControlHeight = true;
                hl.childAlignment = TextAnchor.MiddleCenter;
            }
            return obj;
        }

        private static GameObject BuildDetailPanel(Transform parent, bool isUnequip)
        {
            var dp = new GameObject("DetailPanel");
            dp.transform.SetParent(parent, false);
            dp.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.96f);
            var dpRect = dp.GetComponent<RectTransform>();
            dpRect.anchorMin = new Vector2(0.05f, 0.05f);
            dpRect.anchorMax = new Vector2(0.95f, 0.5f);
            dpRect.offsetMin = Vector2.zero;
            dpRect.offsetMax = Vector2.zero;

            float y = -8f;
            MakeAnchText(dp.transform, "DetailName", "Item", 16, 8f, y, -8f, y - 22f);
            y -= 26f;
            MakeAnchText(dp.transform, "DetailRarity", "Rarity", 12, 8f, y, -8f, y - 18f);
            y -= 20f;
            MakeAnchText(dp.transform, "DetailSlot", "Slot", 12, 8f, y, -8f, y - 18f);
            y -= 22f;
            MakeAnchText(dp.transform, "DetailStats", "Stats", 11, 8f, y, -8f, y - 40f);
            y -= 42f;
            MakeAnchText(dp.transform, "DetailSet", "", 10, 8f, y, -8f, y - 16f);

            var ab = new GameObject("ActionButton");
            ab.transform.SetParent(dp.transform, false);
            var abRect = ab.AddComponent<RectTransform>();
            abRect.anchorMin = new Vector2(0.15f, 0.02f);
            abRect.anchorMax = new Vector2(0.85f, 0.14f);
            abRect.offsetMin = Vector2.zero;
            abRect.offsetMax = Vector2.zero;
            ab.AddComponent<Image>().color = isUnequip
                ? new Color(0.6f, 0.15f, 0.15f, 1f)
                : new Color(0.15f, 0.5f, 0.15f, 1f);
            ab.AddComponent<Button>();
            MakeText(ab.transform, "Label", isUnequip ? "Unequip" : "Equip", 14,
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter)
                .GetComponent<RectTransform>().anchorMax = Vector2.one;

            dp.SetActive(false);
            return dp;
        }

        private static GameObject MakeAnchText(Transform parent, string name, string text,
            int size, float left, float top, float right, float bottom)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(right, top);
            var t = obj.AddComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.color = Color.white;
            t.alignment = TextAnchor.UpperLeft;
            return obj;
        }

        private static GameObject MakeText(Transform parent, string name, string text,
            int size, Vector2 offsetMin, Vector2 offsetMax, TextAnchor anchor)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var t = obj.AddComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.color = Color.white;
            t.alignment = anchor;
            return obj;
        }
    }
}
#endif
