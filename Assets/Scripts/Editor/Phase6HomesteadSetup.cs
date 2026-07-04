#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VoidBound.Data;
using VoidBound.Homestead;
using VoidBound.Inventory;
using VoidBound.UI;

namespace VoidBound.Editor
{
    // Phase 6: attaches station components to the stubbed Homestead buildings,
    // adds the player/HUD components, creates the merchant stock asset, sets
    // goldValues on existing test items, and repairs the StatsPanel rects.
    // Idempotent — safe to re-run.
    public static class Phase6HomesteadSetup
    {
        private const string ShopAssetPath = "Assets/ScriptableObjects/Shop/HomesteadShop.asset";

        [MenuItem("VoidBound/Setup Phase 6 - Homestead Stations")]
        public static void Setup()
        {
            if (SceneManager.GetActiveScene().name != "Homestead")
            {
                Debug.LogError("[Phase6] Wrong scene. Open Homestead.unity first.");
                return;
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) { Debug.LogError("[Phase6] No Player found."); return; }

            var hudCanvas = Object.FindAnyObjectByType<HUDManager>()?.gameObject;
            if (hudCanvas == null) { Debug.LogError("[Phase6] No HUD canvas found."); return; }

            // ── Player components ─────────────────────────────────
            EnsureComponent<TimedBuff>(player);
            EnsureComponent<PlayerStorage>(player);

            // ── HUD UI hosts (panels self-build at runtime) ───────
            EnsureComponent<MerchantUI>(hudCanvas);
            EnsureComponent<StorageUI>(hudCanvas);
            EnsureComponent<TrainingUI>(hudCanvas);
            EnsureComponent<PortalUI>(hudCanvas);
            EnsureComponent<BuffIndicatorUI>(hudCanvas);

            // ── Item economy: goldValues + shop stock asset ───────
            AssignTestItemGoldValues();
            var shop = CreateOrLoadShopAsset();

            // ── Stations on buildings (found by scene name) ───────
            var merchant = EnsureStation<MerchantStation>("Merchant", "Trade", 3f);
            if (merchant != null)
            {
                var so = new SerializedObject(merchant);
                so.FindProperty("shopInventory").objectReferenceValue = shop;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EnsureStation<StorageStation>("Storage Chest", "Open Storage", 3f);
            EnsureStation<PoolStation>("Pool of Refreshment", "Refresh", 3f);
            EnsureStation<ShrineStation>("Shrine", "Make Offering", 3f);
            EnsureStation<PortalStation>("Fast Travel Portal", "Fast Travel", 3f);
            EnsureStation<WatchtowerStation>("Watchtower", "Look Out", 3f);

            ConfigureGuild("Warriors Guild", SkillType.CombatSTR);
            ConfigureGuild("Rangers Guild", SkillType.CombatDEX);
            ConfigureGuild("Mages Guild", SkillType.CombatINT);

            // ── HUD repairs & additions ───────────────────────────
            FixStatsPanelRects(hudCanvas);
            AddGiveGoldDevButton(hudCanvas);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[Phase6] Homestead stations wired. Save the scene (Ctrl+S).");
        }

        // Batch entry point: opens Homestead, runs setup, saves.
        public static void SetupFromBatch()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Homestead.unity");
            Setup();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        // ═══════════════════════════════════════════════════════════

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null) c = go.AddComponent<T>();
            return c;
        }

        private static T EnsureStation<T>(string buildingName, string prompt, float range)
            where T : VoidBound.Core.Interactable
        {
            var building = GameObject.Find(buildingName);
            if (building == null)
            {
                Debug.LogWarning($"[Phase6] Building '{buildingName}' not found in scene — skipped.");
                return null;
            }

            var station = EnsureComponent<T>(building);
            var so = new SerializedObject(station);
            so.FindProperty("interactPrompt").stringValue = prompt;
            so.FindProperty("interactRange").floatValue = range;
            so.ApplyModifiedPropertiesWithoutUndo();

            // PlayerInteractor's OverlapSphere needs a collider on the SAME
            // GameObject as the station component (Phase 5 pattern)
            if (building.GetComponent<Collider>() == null)
            {
                var box = building.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 0.75f, 0f);
                box.size = new Vector3(2f, 1.5f, 2f);
                box.isTrigger = true;
                Debug.Log($"[Phase6] Added trigger collider to '{buildingName}'.");
            }
            return station;
        }

        private static void ConfigureGuild(string buildingName, SkillType stat)
        {
            var station = EnsureStation<GuildStation>(buildingName, "Train", 3f);
            if (station == null) return;

            var so = new SerializedObject(station);
            so.FindProperty("guildName").stringValue = buildingName;
            so.FindProperty("trainedStat").enumValueIndex = (int)stat;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignTestItemGoldValues()
        {
            // Gear: value scales with rarity (placeholder economy — tunable)
            foreach (var guid in AssetDatabase.FindAssets("t:GearItemSO"))
            {
                var gear = AssetDatabase.LoadAssetAtPath<GearItemSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (gear == null || gear.goldValue > 0) continue;
                gear.goldValue = gear.rarity switch
                {
                    RarityTier.Common => 20,
                    RarityTier.Uncommon => 45,
                    RarityTier.Rare => 120,
                    RarityTier.Epic => 300,
                    RarityTier.Legendary => 600,
                    _ => 2000
                };
                EditorUtility.SetDirty(gear);
            }

            // Materials: flat low values (placeholder — tunable)
            foreach (var guid in AssetDatabase.FindAssets("t:MaterialItemSO"))
            {
                var mat = AssetDatabase.LoadAssetAtPath<MaterialItemSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (mat == null || mat.goldValue > 0) continue;
                mat.goldValue = 5;
                EditorUtility.SetDirty(mat);
            }
            AssetDatabase.SaveAssets();
        }

        private static ShopInventorySO CreateOrLoadShopAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<ShopInventorySO>(ShopAssetPath);
            if (existing != null) return existing;

            var folder = System.IO.Path.GetDirectoryName(ShopAssetPath).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Shop");

            var shop = ScriptableObject.CreateInstance<ShopInventorySO>();

            // Test stock: cheapest gear item + two materials if they exist
            GearItemSO cheapGear = null;
            foreach (var guid in AssetDatabase.FindAssets("t:GearItemSO"))
            {
                var gear = AssetDatabase.LoadAssetAtPath<GearItemSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (gear != null && gear.rarity == RarityTier.Common &&
                    (cheapGear == null || gear.goldValue < cheapGear.goldValue))
                    cheapGear = gear;
            }

            var stock = new System.Collections.Generic.List<ShopInventorySO.ShopEntry>();
            if (cheapGear != null)
                stock.Add(new ShopInventorySO.ShopEntry { gear = cheapGear, price = 30 });

            int matCount = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:MaterialItemSO"))
            {
                if (matCount >= 2) break;
                var mat = AssetDatabase.LoadAssetAtPath<MaterialItemSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (mat == null) continue;
                stock.Add(new ShopInventorySO.ShopEntry { material = mat, price = 15, materialQuantity = 5 });
                matCount++;
            }

            shop.stock = stock.ToArray();
            AssetDatabase.CreateAsset(shop, ShopAssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Phase6] Created {ShopAssetPath} with {shop.stock.Length} entries.");
            return shop;
        }

        // Phase 3b's CreateTextElement passed inverted Y offsets (top edge below
        // bottom edge → negative height → invisible text). Repair in place.
        private static void FixStatsPanelRects(GameObject hudCanvas)
        {
            var statsPanel = hudCanvas.transform.Find("StatsPanel");
            if (statsPanel == null) return;

            int fixedCount = 0;
            foreach (var rt in statsPanel.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt == statsPanel.transform) continue;
                if (rt.offsetMin.y > rt.offsetMax.y)
                {
                    (rt.offsetMin, rt.offsetMax) = (
                        new Vector2(rt.offsetMin.x, rt.offsetMax.y),
                        new Vector2(rt.offsetMax.x, rt.offsetMin.y));
                    fixedCount++;
                }
            }
            if (fixedCount > 0)
                Debug.Log($"[Phase6] Repaired {fixedCount} inverted StatsPanel rects.");
        }

        private static void AddGiveGoldDevButton(GameObject hudCanvas)
        {
            var devTools = hudCanvas.GetComponentInChildren<DevToolsPanel>(true);
            if (devTools == null) return;

            var devContent = devTools.transform.Find("DevContent");
            if (devContent == null) return;
            if (devContent.Find("Give 500 Gold") != null) return; // idempotent

            var btnGO = new GameObject("Give 500 Gold");
            btnGO.transform.SetParent(devContent, false);
            btnGO.AddComponent<LayoutElement>().preferredHeight = 44f;
            btnGO.AddComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f, 0.9f);
            var button = btnGO.AddComponent<Button>();

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(btnGO.transform, false);
            var rect = textGO.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8f, 0f);
            rect.offsetMax = new Vector2(-8f, 0f);
            var text = textGO.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 15;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = "Give 500 Gold";

            var so = new SerializedObject(devTools);
            so.FindProperty("giveGoldButton").objectReferenceValue = button;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
