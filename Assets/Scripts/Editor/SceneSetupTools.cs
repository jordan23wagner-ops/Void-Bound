#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VoidBound.Combat;
using VoidBound.Core;
using VoidBound.Data;
using VoidBound.Inventory;
using VoidBound.UI;

namespace VoidBound.Editor
{
    public static class SceneSetupTools
    {
        [MenuItem("VoidBound/Setup Homestead Scene")]
        public static void SetupHomesteadScene()
        {
            const string scenePath = "Assets/Scenes/Homestead.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, scenePath);

            SetupLighting();
            var player = SetupPlayer();
            SetupCamera(player.transform);
            SetupGround();
            SetupMobileControls();
            CreateLootAndEnemyAssets();
            SpawnTestEnemies();
            CreateTestGearAssets(player);
            SetupHUD(player);

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[Setup] Homestead scene built and saved to {scenePath}.");
        }

        private static void SetupLighting()
        {
            var light = GameObject.Find("Directional Light");
            if (light != null)
            {
                light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                var lightComp = light.GetComponent<Light>();
                if (lightComp != null)
                {
                    lightComp.intensity = 2f;
                    lightComp.useColorTemperature = true;
                    lightComp.colorTemperature = 5500f;
                }
            }
        }

        private static GameObject SetupPlayer()
        {
            var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Art/Models/PlayerPlaceholder.fbx");

            GameObject player;
            if (modelPrefab != null)
            {
                player = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
                player.name = "Player";
            }
            else
            {
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = "Player";
                Debug.LogWarning("PlayerPlaceholder.fbx not found — using capsule fallback.");
            }

            player.transform.position = new Vector3(0f, 0.1f, 0f);
            player.tag = "Player";

            var cc = player.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 0.95f, 0f);
            cc.height = 1.9f;
            cc.radius = 0.3f;

            var pc = player.AddComponent<PlayerController>();

            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/InputSystem_Actions.inputactions");
            if (inputActions != null)
            {
                var moveAction = inputActions.FindActionMap("Player")?.FindAction("Move");
                if (moveAction != null)
                {
                    var actionRef = InputActionReference.Create(moveAction);
                    var so = new SerializedObject(pc);
                    var moveField = so.FindProperty("moveAction");
                    if (moveField != null)
                    {
                        moveField.objectReferenceValue = actionRef;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }
            else
            {
                Debug.LogWarning("InputSystem_Actions.inputactions not found.");
            }

            player.AddComponent<StatsComponent>();
            player.AddComponent<Health>();
            player.AddComponent<PlayerCombat>();
            player.AddComponent<PlayerInventory>();
            player.AddComponent<PlayerCurrency>();
            AddHealthBar(player);

            ApplyPlayerMaterial(player);
            return player;
        }

        private static void ApplyPlayerMaterial(GameObject player)
        {
            var renderers = player.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var mat = new Material(shader);
            mat.name = "PlayerMaterial";
            mat.color = new Color(0.76f, 0.60f, 0.42f, 1f);
            mat.SetFloat("_Smoothness", 0.15f);

            string matPath = "Assets/Art/Materials/PlayerMaterial.mat";
            var existingMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existingMat != null)
            {
                mat = existingMat;
            }
            else
            {
                AssetDatabase.CreateAsset(mat, matPath);
            }

            foreach (var r in renderers)
                r.sharedMaterial = mat;
        }

        private static void SetupCamera(Transform playerTransform)
        {
            var camObj = GameObject.Find("Main Camera");
            if (camObj == null)
            {
                camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }

            var cam = camObj.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;

            camObj.transform.rotation = Quaternion.Euler(30f, 45f, 0f);
            camObj.transform.position = new Vector3(7f, 10f, -7f);

            var follow = camObj.GetComponent<IsometricCameraFollow>();
            if (follow == null)
                follow = camObj.AddComponent<IsometricCameraFollow>();

            var so = new SerializedObject(follow);
            var targetProp = so.FindProperty("target");
            if (targetProp != null)
            {
                targetProp.objectReferenceValue = playerTransform;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetupGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(40f, 1f, 40f);
            ground.tag = "Untagged";
            ground.isStatic = true;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var mat = new Material(shader);
            mat.name = "GroundMaterial";
            mat.color = new Color(0.45f, 0.55f, 0.35f, 1f);
            mat.SetFloat("_Smoothness", 0.1f);

            string groundMatPath = "Assets/Art/Materials/GroundMaterial.mat";
            var existingGroundMat = AssetDatabase.LoadAssetAtPath<Material>(groundMatPath);
            if (existingGroundMat != null)
                mat = existingGroundMat;
            else
                AssetDatabase.CreateAsset(mat, groundMatPath);
            ground.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void SetupMobileControls()
        {
            var canvasObj = new GameObject("MobileControls");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            var bgObj = new GameObject("JoystickBackground");
            bgObj.transform.SetParent(canvasObj.transform, false);
            var bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(1f, 1f, 1f, 0.3f);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0f);
            bgRect.anchorMax = new Vector2(0f, 0f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.anchoredPosition = new Vector2(180f, 180f);
            bgRect.sizeDelta = new Vector2(200f, 200f);

            var handleObj = new GameObject("JoystickHandle");
            handleObj.transform.SetParent(bgObj.transform, false);
            var handleImage = handleObj.AddComponent<Image>();
            handleImage.color = new Color(1f, 1f, 1f, 0.6f);
            var handleRect = handleObj.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(80f, 80f);

            var stick = handleObj.AddComponent<OnScreenStick>();
            stick.controlPath = "<Gamepad>/leftStick";
            stick.movementRange = 60f;

            var eventSystem = GameObject.Find("EventSystem");
            if (eventSystem == null)
            {
                eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }
        private static void CreateLootAndEnemyAssets()
        {
            string ltDir = "Assets/ScriptableObjects/LootTables";
            if (!AssetDatabase.IsValidFolder(ltDir))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "LootTables");

            string edDir = "Assets/ScriptableObjects/Enemies";
            if (!AssetDatabase.IsValidFolder(edDir))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Enemies");

            string gearDir = "Assets/ScriptableObjects/TestGear";
            var testGear = new[] {
                AssetDatabase.LoadAssetAtPath<GearItemSO>($"{gearDir}/Rusty_Sword_Common.asset"),
                AssetDatabase.LoadAssetAtPath<GearItemSO>($"{gearDir}/Arcane_Blade_Rare.asset"),
                AssetDatabase.LoadAssetAtPath<GearItemSO>($"{gearDir}/Flamecleaver_Legendary.asset"),
                AssetDatabase.LoadAssetAtPath<GearItemSO>($"{gearDir}/Voidreaver_Voidforged.asset")
            };

            CreateLootTable(ltDir, "WeakLoot", testGear, 0.3f, 2, 8, 0, 0,
                new[] {
                    new RarityWeight { rarity = RarityTier.Common, weight = 70f },
                    new RarityWeight { rarity = RarityTier.Uncommon, weight = 25f },
                    new RarityWeight { rarity = RarityTier.Rare, weight = 5f }
                });
            CreateLootTable(ltDir, "StandardLoot", testGear, 0.5f, 5, 15, 0, 1,
                new[] {
                    new RarityWeight { rarity = RarityTier.Common, weight = 40f },
                    new RarityWeight { rarity = RarityTier.Uncommon, weight = 35f },
                    new RarityWeight { rarity = RarityTier.Rare, weight = 20f },
                    new RarityWeight { rarity = RarityTier.Epic, weight = 5f }
                });
            CreateLootTable(ltDir, "EliteLoot", testGear, 0.7f, 10, 30, 1, 3,
                new[] {
                    new RarityWeight { rarity = RarityTier.Uncommon, weight = 20f },
                    new RarityWeight { rarity = RarityTier.Rare, weight = 35f },
                    new RarityWeight { rarity = RarityTier.Epic, weight = 25f },
                    new RarityWeight { rarity = RarityTier.Legendary, weight = 15f },
                    new RarityWeight { rarity = RarityTier.Mythic, weight = 5f }
                });

            var weakLT = AssetDatabase.LoadAssetAtPath<LootTableSO>($"{ltDir}/WeakLoot.asset");
            var stdLT = AssetDatabase.LoadAssetAtPath<LootTableSO>($"{ltDir}/StandardLoot.asset");
            var eliteLT = AssetDatabase.LoadAssetAtPath<LootTableSO>($"{ltDir}/EliteLoot.asset");

            CreateEnemyDef(edDir, "Goblin Scout", EnemyTier.Weak,
                new CharacterStats(3, 3, 3, 1), 4, 2.5f, 7f, 1.8f, weakLT);
            CreateEnemyDef(edDir, "Goblin Warrior", EnemyTier.Standard,
                new CharacterStats(6, 5, 8, 2), 7, 3f, 9f, 2f, stdLT);
            CreateEnemyDef(edDir, "Goblin Champion", EnemyTier.Elite,
                new CharacterStats(10, 8, 15, 4), 12, 3.5f, 12f, 2.2f, eliteLT);
        }

        private static void CreateLootTable(string dir, string name, GearItemSO[] pool,
            float dropChance, int goldMin, int goldMax, int shardMin, int shardMax, RarityWeight[] weights)
        {
            string path = $"{dir}/{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<LootTableSO>(path) != null) return;

            var lt = ScriptableObject.CreateInstance<LootTableSO>();
            lt.tableId = name.ToLower();
            lt.displayName = name;
            lt.gearPool = pool;
            lt.gearDropChance = dropChance;
            lt.goldMin = goldMin;
            lt.goldMax = goldMax;
            lt.voidShardMin = shardMin;
            lt.voidShardMax = shardMax;
            lt.rarityWeights = weights;
            lt.zoneModifier = 1f;
            AssetDatabase.CreateAsset(lt, path);
        }

        private static void CreateEnemyDef(string dir, string name, EnemyTier tier,
            CharacterStats stats, int dmg, float speed, float aggro, float atkRange, LootTableSO loot)
        {
            string path = $"{dir}/{name.Replace(" ", "_")}.asset";
            if (AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>(path) != null) return;

            var ed = ScriptableObject.CreateInstance<EnemyDefinitionSO>();
            ed.enemyId = name.ToLower().Replace(" ", "_");
            ed.displayName = name;
            ed.tier = tier;
            ed.baseStats = stats;
            ed.baseDamage = dmg;
            ed.moveSpeed = speed;
            ed.aggroRange = aggro;
            ed.attackRange = atkRange;
            ed.lootTable = loot;
            AssetDatabase.CreateAsset(ed, path);
        }

        private static void SpawnTestEnemies()
        {
            string edDir = "Assets/ScriptableObjects/Enemies";
            SpawnEnemy("Goblin Scout", new Vector3(5f, 0.1f, 5f),
                AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>($"{edDir}/Goblin_Scout.asset"),
                new Color(0.65f, 0.25f, 0.18f));
            SpawnEnemy("Goblin Warrior", new Vector3(-5f, 0.1f, 6f),
                AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>($"{edDir}/Goblin_Warrior.asset"),
                new Color(0.5f, 0.35f, 0.15f));
            SpawnEnemy("Goblin Champion", new Vector3(7f, 0.1f, -4f),
                AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>($"{edDir}/Goblin_Champion.asset"),
                new Color(0.4f, 0.12f, 0.12f));
        }

        private static void SpawnEnemy(string name, Vector3 pos, EnemyDefinitionSO def, Color tintColor)
        {
            var enemyModel = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Art/Models/EnemyPlaceholder.fbx");

            GameObject enemy;
            if (enemyModel != null)
            {
                enemy = (GameObject)PrefabUtility.InstantiatePrefab(enemyModel);
                enemy.name = name;
            }
            else
            {
                enemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
                enemy.name = name;
            }

            enemy.transform.position = pos;

            var cc = enemy.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 0.7f, 0f);
            cc.height = 1.4f;
            cc.radius = 0.35f;

            enemy.AddComponent<StatsComponent>();
            enemy.AddComponent<Health>();

            var ai = enemy.AddComponent<EnemyAI>();
            if (def != null)
            {
                var aiSO = new SerializedObject(ai);
                aiSO.FindProperty("definition").objectReferenceValue = def;
                aiSO.ApplyModifiedPropertiesWithoutUndo();
            }

            var dropper = enemy.AddComponent<LootDropper>();
            if (def != null && def.lootTable != null)
                dropper.SetLootTable(def.lootTable, def.tier);

            AddHealthBar(enemy, new Vector3(0f, 1.6f, 0f));

            ApplyEnemyMaterial(enemy, tintColor);
        }

        private static void AddHealthBar(GameObject target, Vector3 offset = default)
        {
            var hbObj = new GameObject("HealthBar");
            hbObj.transform.SetParent(target.transform, false);
            var hb = hbObj.AddComponent<HealthBar>();
            if (offset != default)
            {
                var so = new SerializedObject(hb);
                var offsetProp = so.FindProperty("offset");
                if (offsetProp != null)
                {
                    offsetProp.vector3Value = offset;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static void ApplyEnemyMaterial(GameObject enemy, Color tint = default)
        {
            var renderers = enemy.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            if (tint == default) tint = new Color(0.65f, 0.25f, 0.18f, 1f);

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader);
            mat.color = tint;
            mat.SetFloat("_Smoothness", 0.15f);

            foreach (var r in renderers)
                r.sharedMaterial = mat;
        }

        private static void SetupHUD(GameObject player)
        {
            var canvasObj = new GameObject("HUDCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // === TOP-LEFT: Stats panel ===
            var statsPanel = CreateAnchoredPanel(canvasObj.transform, "StatsPanel",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -10f), new Vector2(220f, 130f));
            statsPanel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.75f);

            var levelText = CreateTextElement(statsPanel.transform, "LevelText", "Lv 1", 18,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(5f, -5f), new Vector2(-5f, -25f));
            var xpBg = CreateFillBar(statsPanel.transform, "XPBar", new Color(0.2f, 0.2f, 0.3f), new Color(0.3f, 0.5f, 1f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(5f, -28f), new Vector2(-5f, -40f));
            var hpBg = CreateFillBar(statsPanel.transform, "HPBar", new Color(0.3f, 0.1f, 0.1f), new Color(0.2f, 0.8f, 0.2f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(5f, -44f), new Vector2(-5f, -62f));
            var hpText = CreateTextElement(statsPanel.transform, "HPText", "200/200", 12,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -44f), new Vector2(-8f, -62f));
            var statsText = CreateTextElement(statsPanel.transform, "StatsText", "STR 10  DEX 10\nVIG 10  INT 10", 12,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(5f, -66f), new Vector2(-5f, -100f));
            var currencyText = CreateTextElement(statsPanel.transform, "CurrencyText", "Gold: 0  Shards: 0", 12,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(5f, -104f), new Vector2(-5f, -130f));

            // === TOP-RIGHT: Minimap + buttons ===
            var minimapPanel = CreateAnchoredPanel(canvasObj.transform, "MinimapPanel",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-140f, -10f), new Vector2(130f, 130f));
            var minimapBg = minimapPanel.AddComponent<Image>();
            minimapBg.color = new Color(0.15f, 0.12f, 0.1f, 1f);
            var minimapComp = minimapPanel.AddComponent<Minimap>();
            var minimapDisplay = new GameObject("MinimapDisplay");
            minimapDisplay.transform.SetParent(minimapPanel.transform, false);
            var mmDisplayRect = minimapDisplay.AddComponent<RectTransform>();
            mmDisplayRect.anchorMin = Vector2.zero;
            mmDisplayRect.anchorMax = Vector2.one;
            mmDisplayRect.offsetMin = new Vector2(3f, 3f);
            mmDisplayRect.offsetMax = new Vector2(-3f, -3f);
            var mmRawImage = minimapDisplay.AddComponent<RawImage>();

            // Buttons below minimap (44px each, stacked)
            var hudManager = canvasObj.AddComponent<HUDManager>();
            var equipBtn = CreateHUDButton(canvasObj.transform, "EquipBtn", "Equip",
                new Vector2(1f, 1f), new Vector2(-140f, -148f), new Vector2(130f, 44f));
            var bpBtn = CreateHUDButton(canvasObj.transform, "BagBtn", "Bag",
                new Vector2(1f, 1f), new Vector2(-140f, -196f), new Vector2(130f, 44f));
            var devBtn = CreateHUDButton(canvasObj.transform, "DevBtn", "Dev",
                new Vector2(1f, 1f), new Vector2(-140f, -244f), new Vector2(130f, 44f));

            // === DEV TOOLS PANEL ===
            var devPanel = CreateMenuPanel(canvasObj.transform, "DevToolsPanel");
            var devContent = CreateScrollableList(devPanel.transform, "DevContent",
                new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f));
            var devComp = devPanel.AddComponent<DevToolsPanel>();

            // Create dev buttons inside panel
            CreateDevButton(devContent.transform, "Give Test Gear", 0);
            CreateDevButton(devContent.transform, "Kill All Enemies", 1);
            CreateDevButton(devContent.transform, "Toggle God Mode", 2);

            // === Wire everything via SerializedObject ===
            var inv = player.GetComponent<PlayerInventory>();
            var health = player.GetComponent<Health>();
            var stats = player.GetComponent<StatsComponent>();

            var hmSO = new SerializedObject(hudManager);
            hmSO.FindProperty("inventory").objectReferenceValue = inv;
            hmSO.FindProperty("playerStats").objectReferenceValue = stats;
            hmSO.FindProperty("playerHealth").objectReferenceValue = health;
            hmSO.FindProperty("levelText").objectReferenceValue = levelText.GetComponent<Text>();
            hmSO.FindProperty("xpFill").objectReferenceValue = xpBg.transform.Find("Fill")?.GetComponent<Image>();
            hmSO.FindProperty("hpFill").objectReferenceValue = hpBg.transform.Find("Fill")?.GetComponent<Image>();
            hmSO.FindProperty("hpText").objectReferenceValue = hpText.GetComponent<Text>();
            hmSO.FindProperty("statsText").objectReferenceValue = statsText.GetComponent<Text>();
            hmSO.FindProperty("currencyText").objectReferenceValue = currencyText.GetComponent<Text>();
            hmSO.FindProperty("playerCurrency").objectReferenceValue = player.GetComponent<PlayerCurrency>();
            hmSO.FindProperty("devToolsPanel").objectReferenceValue = devPanel;
            hmSO.ApplyModifiedPropertiesWithoutUndo();

            var devSO = new SerializedObject(devComp);
            devSO.FindProperty("inventory").objectReferenceValue = inv;
            devSO.FindProperty("playerHealth").objectReferenceValue = health;
            var poolProp = devSO.FindProperty("testGearPool");
            string soDir = "Assets/ScriptableObjects/TestGear";
            var allGear = new[] {
                AssetDatabase.LoadAssetAtPath<GearItemSO>($"{soDir}/Rusty_Sword_Common.asset"),
                AssetDatabase.LoadAssetAtPath<GearItemSO>($"{soDir}/Arcane_Blade_Rare.asset"),
                AssetDatabase.LoadAssetAtPath<GearItemSO>($"{soDir}/Flamecleaver_Legendary.asset"),
                AssetDatabase.LoadAssetAtPath<GearItemSO>($"{soDir}/Voidreaver_Voidforged.asset")
            };
            poolProp.arraySize = allGear.Length;
            for (int i = 0; i < allGear.Length; i++)
                poolProp.GetArrayElementAtIndex(i).objectReferenceValue = allGear[i];
            var devBtns = devContent.GetComponentsInChildren<Button>();
            devSO.FindProperty("giveGearButton").objectReferenceValue = devBtns.Length > 0 ? devBtns[0] : null;
            devSO.FindProperty("killAllButton").objectReferenceValue = devBtns.Length > 1 ? devBtns[1] : null;
            devSO.FindProperty("godModeButton").objectReferenceValue = devBtns.Length > 2 ? devBtns[2] : null;
            devSO.ApplyModifiedPropertiesWithoutUndo();

            // Wire HUD button references (serialized — wired at runtime in Start)
            hmSO = new SerializedObject(hudManager);
            hmSO.FindProperty("equipButton").objectReferenceValue = equipBtn.GetComponent<Button>();
            hmSO.FindProperty("backpackButton").objectReferenceValue = bpBtn.GetComponent<Button>();
            hmSO.FindProperty("devToolsButton").objectReferenceValue = devBtn.GetComponent<Button>();
            hmSO.ApplyModifiedPropertiesWithoutUndo();

            // Wire minimap RenderTexture after Start runs (deferred)
            var mmWirer = minimapDisplay.AddComponent<MinimapWirer>();
            var mmSO = new SerializedObject(mmWirer);
            mmSO.FindProperty("minimap").objectReferenceValue = minimapComp;
            mmSO.FindProperty("display").objectReferenceValue = mmRawImage;
            mmSO.ApplyModifiedPropertiesWithoutUndo();

            // Build inventory panels via Phase5c (owns the current panel layout)
            Phase5cInventoryUISetup.Setup();
        }

        private static GameObject CreateAnchoredPanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;
            return obj;
        }

        private static GameObject CreateTextElement(Transform parent, string name, string text, int fontSize,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var t = obj.AddComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleLeft;
            return obj;
        }

        private static GameObject CreateFillBar(Transform parent, string name, Color bgColor, Color fillColor,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var bg = new GameObject(name);
            bg.transform.SetParent(parent, false);
            var bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = anchorMin;
            bgRect.anchorMax = anchorMax;
            bgRect.offsetMin = offsetMin;
            bgRect.offsetMax = offsetMax;
            bg.AddComponent<Image>().color = bgColor;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(bg.transform, false);
            var fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = fillColor;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 1f;
            return bg;
        }

        private static GameObject CreateHUDButton(Transform parent, string name, string label,
            Vector2 anchor, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;
            obj.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 0.85f);
            obj.AddComponent<Button>();
            var textObj = CreateTextElement(obj.transform, "Label", label, 16,
                Vector2.zero, Vector2.one, new Vector2(4f, 0f), new Vector2(-4f, 0f));
            textObj.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            return obj;
        }

        private static GameObject CreateMenuPanel(Transform parent, string name)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            panel.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.92f);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.08f, 0.08f);
            rect.anchorMax = new Vector2(0.92f, 0.92f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            CreateTextElement(panel.transform, "Title", name.Replace("Panel", ""), 22,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, -35f), new Vector2(-10f, -5f))
                .GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

            return panel;
        }

        private static GameObject CreateScrollableList(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var layout = obj.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            obj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return obj;
        }

        private static GameObject CreateDetailPanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, bool isUnequip)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            panel.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.18f, 0.95f);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            float y = -10f;
            CreateTextElement(panel.transform, "DetailName", "Item Name", 18,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, y - 22f), new Vector2(-8f, y));
            y -= 28f;
            CreateTextElement(panel.transform, "DetailRarity", "Rarity", 14,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, y - 20f), new Vector2(-8f, y));
            y -= 24f;
            CreateTextElement(panel.transform, "DetailSlot", "Slot", 14,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, y - 20f), new Vector2(-8f, y));
            y -= 24f;
            CreateTextElement(panel.transform, "DetailStats", "Stats", 13,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, y - 50f), new Vector2(-8f, y));
            y -= 54f;
            CreateTextElement(panel.transform, "DetailSet", "", 12,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, y - 18f), new Vector2(-8f, y));
            y -= 28f;

            var actionBtn = new GameObject("ActionButton");
            actionBtn.transform.SetParent(panel.transform, false);
            var abRect = actionBtn.AddComponent<RectTransform>();
            abRect.anchorMin = new Vector2(0.1f, 0f);
            abRect.anchorMax = new Vector2(0.9f, 0f);
            abRect.pivot = new Vector2(0.5f, 0f);
            abRect.anchoredPosition = new Vector2(0f, 12f);
            abRect.sizeDelta = new Vector2(0f, 40f);
            actionBtn.AddComponent<Image>().color = isUnequip
                ? new Color(0.7f, 0.2f, 0.2f, 1f)
                : new Color(0.2f, 0.6f, 0.2f, 1f);
            actionBtn.AddComponent<Button>();
            var abText = CreateTextElement(actionBtn.transform, "Label", isUnequip ? "Unequip" : "Equip", 16,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            abText.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

            panel.SetActive(false);
            return panel;
        }

        private static void CreateDevButton(Transform parent, string label, int index)
        {
            var btn = new GameObject(label);
            btn.transform.SetParent(parent, false);
            btn.AddComponent<LayoutElement>().preferredHeight = 44f;
            btn.AddComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f, 0.9f);
            btn.AddComponent<Button>();
            var text = CreateTextElement(btn.transform, "Label", label, 15,
                Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f));
            text.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
        }

        private static GameObject CreateUIText(Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax)
        {
            var obj = new GameObject(text + "Header");
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var t = obj.AddComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 22;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            return obj;
        }

        private static void CreateTestGearAssets(GameObject player)
        {
            string soDir = "Assets/ScriptableObjects/TestGear";
            if (!AssetDatabase.IsValidFolder(soDir))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "TestGear");

            var common = CreateGearAsset(soDir, "Rusty Sword", EquipmentSlot.Weapon, WeaponType.Sword,
                RarityTier.Common, new CharacterStats(2, 0, 0, 0), 8);
            var rare = CreateGearAsset(soDir, "Arcane Blade", EquipmentSlot.Weapon, WeaponType.Sword,
                RarityTier.Rare, new CharacterStats(5, 3, 0, 2), 14);
            var legendary = CreateGearAsset(soDir, "Flamecleaver", EquipmentSlot.Weapon, WeaponType.Sword2H,
                RarityTier.Legendary, new CharacterStats(12, 5, 3, 0), 22);
            var voidforged = CreateGearAsset(soDir, "Voidreaver", EquipmentSlot.Weapon, WeaponType.Sword2H,
                RarityTier.Voidforged, new CharacterStats(20, 10, 8, 5), 35);

            var inv = player.GetComponent<PlayerInventory>();
            if (inv == null) return;

            var so = new SerializedObject(inv);
            so.ApplyModifiedPropertiesWithoutUndo();

            var startup = player.AddComponent<TestGearStartup>();
            var startupSO = new SerializedObject(startup);
            var arr = startupSO.FindProperty("testItems");
            arr.arraySize = 4;
            arr.GetArrayElementAtIndex(0).objectReferenceValue = common;
            arr.GetArrayElementAtIndex(1).objectReferenceValue = rare;
            arr.GetArrayElementAtIndex(2).objectReferenceValue = legendary;
            arr.GetArrayElementAtIndex(3).objectReferenceValue = voidforged;
            startupSO.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GearItemSO CreateGearAsset(string dir, string name, EquipmentSlot slot,
            WeaponType weapon, RarityTier rarity, CharacterStats mods, int baseDmg)
        {
            string path = $"{dir}/{name.Replace(" ", "_")}_{rarity}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<GearItemSO>(path);
            if (existing != null) return existing;

            var item = ScriptableObject.CreateInstance<GearItemSO>();
            item.itemId = name.ToLower().Replace(" ", "_");
            item.displayName = name;
            item.slot = slot;
            item.weaponType = weapon;
            item.rarity = rarity;
            item.statModifiers = mods;
            item.baseDamage = baseDmg;
            item.setId = "";

            AssetDatabase.CreateAsset(item, path);
            return item;
        }
    }
}
#endif
