#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VoidBound.Combat;
using VoidBound.Data;
using VoidBound.Skilling;

namespace VoidBound.Editor
{
    public static class Phase5HomesteadSetup
    {
        [MenuItem("VoidBound/Setup Phase 5 - Homestead Buildings")]
        public static void Setup()
        {
            CreateSkillAssets();
            CreateMaterialAssets();
            CreateRecipeAssets();
            SpawnBuildings();
            SpawnResourceNodes();
            SetupCraftingUI();
            AddPlayerComponents();
            Debug.Log("[Phase 5] Homestead buildings, skills, and crafting set up. Save the scene.");
        }

        private static void CreateSkillAssets()
        {
            string dir = "Assets/ScriptableObjects/Skills";
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Skills");

            CreateSkillDef(dir, "Gathering", SkillType.Gathering);
            CreateSkillDef(dir, "Cooking", SkillType.Cooking);
            CreateSkillDef(dir, "Smithing", SkillType.Smithing);
            CreateSkillDef(dir, "Fishing", SkillType.Fishing);
            CreateSkillDef(dir, "Mining", SkillType.Mining);
            CreateSkillDef(dir, "Alchemy", SkillType.Alchemy);
            CreateSkillDef(dir, "Crafting", SkillType.Crafting);
            CreateSkillDef(dir, "CombatVIG", SkillType.CombatVIG);
            CreateSkillDef(dir, "CombatSTR", SkillType.CombatSTR);
            CreateSkillDef(dir, "CombatDEX", SkillType.CombatDEX);
            CreateSkillDef(dir, "CombatINT", SkillType.CombatINT);
        }

        private static void CreateSkillDef(string dir, string name, SkillType type)
        {
            string path = $"{dir}/{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>(path) != null) return;
            var s = ScriptableObject.CreateInstance<SkillDefinitionSO>();
            s.skillId = name.ToLower();
            s.displayName = name;
            s.skillType = type;
            s.xpMultiplier = 3f;
            s.maxLevel = 99;
            AssetDatabase.CreateAsset(s, path);
        }

        private static void CreateMaterialAssets()
        {
            string dir = "Assets/ScriptableObjects/Materials";
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Materials");

            CreateMat(dir, "herb", "Wild Herb");
            CreateMat(dir, "raw_fish", "Raw Fish");
            CreateMat(dir, "iron_ore", "Iron Ore");
            CreateMat(dir, "cooked_fish", "Cooked Fish");
            CreateMat(dir, "iron_ingot", "Iron Ingot");
        }

        private static void CreateMat(string dir, string id, string name)
        {
            string path = $"{dir}/{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<MaterialItemSO>(path) != null) return;
            var m = ScriptableObject.CreateInstance<MaterialItemSO>();
            m.itemId = id;
            m.displayName = name;
            AssetDatabase.CreateAsset(m, path);
        }

        private static void CreateRecipeAssets()
        {
            string dir = "Assets/ScriptableObjects/Recipes";
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Recipes");

            string matDir = "Assets/ScriptableObjects/Materials";
            var rawFish = AssetDatabase.LoadAssetAtPath<MaterialItemSO>($"{matDir}/raw_fish.asset");
            var cookedFish = AssetDatabase.LoadAssetAtPath<MaterialItemSO>($"{matDir}/cooked_fish.asset");
            var ironOre = AssetDatabase.LoadAssetAtPath<MaterialItemSO>($"{matDir}/iron_ore.asset");
            var ironIngot = AssetDatabase.LoadAssetAtPath<MaterialItemSO>($"{matDir}/iron_ingot.asset");

            var rustySword = AssetDatabase.LoadAssetAtPath<GearItemSO>(
                "Assets/ScriptableObjects/TestGear/Rusty_Sword_Common.asset");

            CreateRecipe(dir, "cook_fish", "Cook Fish", SkillType.Cooking, 1, "Campfire",
                new[] { new RecipeIngredient { material = rawFish, quantity = 1 } },
                RecipeOutputType.Material, cookedFish, null, 1, 15);

            CreateRecipe(dir, "smelt_iron", "Smelt Iron Ore", SkillType.Smithing, 1, "Forge",
                new[] { new RecipeIngredient { material = ironOre, quantity = 2 } },
                RecipeOutputType.Material, ironIngot, null, 1, 20);

            CreateRecipe(dir, "forge_sword", "Forge Iron Sword", SkillType.Smithing, 3, "Forge",
                new[] { new RecipeIngredient { material = ironIngot, quantity = 2 } },
                RecipeOutputType.Gear, null, rustySword, 1, 35);
        }

        private static void CreateRecipe(string dir, string id, string name, SkillType skill, int level,
            string station, RecipeIngredient[] ingredients, RecipeOutputType outType,
            MaterialItemSO outMat, GearItemSO outGear, int outQty, int xp)
        {
            string path = $"{dir}/{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<RecipeDefinitionSO>(path) != null) return;
            var r = ScriptableObject.CreateInstance<RecipeDefinitionSO>();
            r.recipeId = id;
            r.displayName = name;
            r.requiredSkill = skill;
            r.requiredSkillLevel = level;
            r.requiredStation = station;
            r.ingredients = ingredients;
            r.outputType = outType;
            r.outputMaterial = outMat;
            r.outputGear = outGear;
            r.outputQuantity = outQty;
            r.xpReward = xp;
            AssetDatabase.CreateAsset(r, path);
        }

        private static void SpawnBuildings()
        {
            string recDir = "Assets/ScriptableObjects/Recipes";
            var cookFish = AssetDatabase.LoadAssetAtPath<RecipeDefinitionSO>($"{recDir}/cook_fish.asset");
            var smeltIron = AssetDatabase.LoadAssetAtPath<RecipeDefinitionSO>($"{recDir}/smelt_iron.asset");
            var forgeSword = AssetDatabase.LoadAssetAtPath<RecipeDefinitionSO>($"{recDir}/forge_sword.asset");

            SpawnBuilding("Forge", "Building_Hut", new Vector3(-8f, 0, 8f), new Color(0.6f, 0.35f, 0.2f),
                SkillType.Smithing, "Forge", new[] { smeltIron, forgeSword });
            SpawnBuilding("Campfire", "Building_Stall", new Vector3(0f, 0, 10f), new Color(0.65f, 0.4f, 0.2f),
                SkillType.Cooking, "Campfire", new[] { cookFish });
            SpawnBuilding("Garden", "Building_Garden", new Vector3(8f, 0, 8f), new Color(0.3f, 0.55f, 0.2f),
                SkillType.Gathering, "Garden", null);
            SpawnBuilding("Warriors Guild", "Building_Hut", new Vector3(-10f, 0, -2f), new Color(0.7f, 0.25f, 0.2f),
                default, null, null);
            SpawnBuilding("Rangers Guild", "Building_Hut", new Vector3(-10f, 0, -8f), new Color(0.2f, 0.5f, 0.4f),
                default, null, null);
            SpawnBuilding("Mages Guild", "Building_Hut", new Vector3(-10f, 0, -14f), new Color(0.4f, 0.25f, 0.6f),
                default, null, null);
            SpawnBuilding("Watchtower", "Building_Tower", new Vector3(10f, 0, -2f), new Color(0.5f, 0.45f, 0.4f),
                default, null, null);
            SpawnBuilding("Merchant", "Building_Stall", new Vector3(10f, 0, -8f), new Color(0.6f, 0.5f, 0.25f),
                default, null, null);
            SpawnBuilding("Shrine", "Building_Shrine", new Vector3(0f, 0, -10f), new Color(0.55f, 0.35f, 0.6f),
                default, null, null);
            SpawnBuilding("Pool of Refreshment", "Building_Pool", new Vector3(0f, 0, -15f), new Color(0.2f, 0.4f, 0.65f),
                default, null, null);
            SpawnBuilding("Fast Travel Portal", "Building_Portal", new Vector3(12f, 0, 4f), new Color(0.35f, 0.5f, 0.8f),
                default, null, null);
            SpawnBuilding("Storage Chest", "Building_Chest", new Vector3(-6f, 0, 4f), new Color(0.55f, 0.4f, 0.15f),
                default, null, null);
        }

        private static void SpawnBuilding(string name, string modelName, Vector3 pos, Color tint,
            SkillType skill, string stationId, RecipeDefinitionSO[] recipes)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Art/Models/{modelName}.fbx");

            GameObject go;
            if (model != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(model);
                go.name = name;
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
            }

            go.transform.position = pos;
            go.isStatic = true;

            var collider = go.GetComponent<Collider>();
            if (collider == null)
            {
                var bc = go.AddComponent<BoxCollider>();
                bc.center = new Vector3(0f, 0.75f, 0f);
                bc.size = new Vector3(2f, 1.5f, 2f);
                bc.isTrigger = true;
            }

            if (!string.IsNullOrEmpty(stationId))
            {
                var station = go.AddComponent<CraftingStation>();
                var so = new SerializedObject(station);
                so.FindProperty("stationId").stringValue = stationId;
                so.FindProperty("skillType").enumValueIndex = (int)skill;
                so.FindProperty("interactRange").floatValue = 3f;
                so.FindProperty("interactPrompt").stringValue = $"Use {name}";
                if (recipes != null)
                {
                    var arr = so.FindProperty("availableRecipes");
                    arr.arraySize = recipes.Length;
                    for (int i = 0; i < recipes.Length; i++)
                        arr.GetArrayElementAtIndex(i).objectReferenceValue = recipes[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            ApplyTint(go, tint);
        }

        private static void SpawnResourceNodes()
        {
            string matDir = "Assets/ScriptableObjects/Materials";
            var herb = AssetDatabase.LoadAssetAtPath<MaterialItemSO>($"{matDir}/herb.asset");
            var rawFish = AssetDatabase.LoadAssetAtPath<MaterialItemSO>($"{matDir}/raw_fish.asset");
            var ironOre = AssetDatabase.LoadAssetAtPath<MaterialItemSO>($"{matDir}/iron_ore.asset");

            SpawnResourceNode("Herb Patch 1", null, new Vector3(9f, 0, 10f), herb, SkillType.Gathering, 15,
                new Color(0.3f, 0.65f, 0.2f));
            SpawnResourceNode("Herb Patch 2", null, new Vector3(11f, 0, 9f), herb, SkillType.Gathering, 15,
                new Color(0.25f, 0.6f, 0.2f));
            SpawnResourceNode("Fishing Spot", "ResourceNode_FishSpot", new Vector3(14f, 0, 0f), rawFish,
                SkillType.Fishing, 20, new Color(0.3f, 0.45f, 0.6f));
            SpawnResourceNode("Iron Deposit", "ResourceNode_Rock", new Vector3(-14f, 0, 2f), ironOre,
                SkillType.Mining, 20, new Color(0.5f, 0.42f, 0.35f));
        }

        private static void SpawnResourceNode(string name, string modelName, Vector3 pos,
            MaterialItemSO mat, SkillType skill, int xp, Color tint)
        {
            GameObject go;
            if (!string.IsNullOrEmpty(modelName))
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"Assets/Art/Models/{modelName}.fbx");
                if (model != null)
                {
                    go = (GameObject)PrefabUtility.InstantiatePrefab(model);
                    go.name = name;
                }
                else
                {
                    go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    go.name = name;
                    go.transform.localScale = Vector3.one * 0.6f;
                }
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = name;
                go.transform.localScale = Vector3.one * 0.5f;
            }

            go.transform.position = pos;

            var collider = go.GetComponent<Collider>();
            if (collider == null)
            {
                var sc = go.AddComponent<SphereCollider>();
                sc.radius = 1f;
                sc.isTrigger = true;
            }

            var node = go.AddComponent<ResourceNode>();
            var so = new SerializedObject(node);
            so.FindProperty("gatherMaterial").objectReferenceValue = mat;
            so.FindProperty("gatherSkill").enumValueIndex = (int)skill;
            so.FindProperty("xpPerGather").intValue = xp;
            so.FindProperty("respawnTime").floatValue = 8f;
            so.FindProperty("interactRange").floatValue = 2f;
            so.FindProperty("interactPrompt").stringValue = $"Gather {name}";
            so.ApplyModifiedPropertiesWithoutUndo();

            ApplyTint(go, tint);
        }

        private static void SetupCraftingUI()
        {
            var existing = Object.FindAnyObjectByType<CraftingUI>();
            if (existing != null) return;

            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;
            var canvasObj = canvas.gameObject;

            var panel = new GameObject("CraftingPanel");
            panel.transform.SetParent(canvasObj.transform, false);
            panel.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.08f, 0.92f);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.1f);
            panelRect.anchorMax = new Vector2(0.9f, 0.9f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var title = MakeText(panel.transform, "Title", "Station", 20,
                new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(10f, -30f), new Vector2(-10f, -5f));
            var skillInfo = MakeText(panel.transform, "SkillInfo", "Skill Lv1 XP: 0/30", 14,
                new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(10f, -50f), new Vector2(-10f, -32f));

            var recipeListObj = new GameObject("RecipeList");
            recipeListObj.transform.SetParent(panel.transform, false);
            var rlRect = recipeListObj.AddComponent<RectTransform>();
            rlRect.anchorMin = new Vector2(0.02f, 0.05f);
            rlRect.anchorMax = new Vector2(0.45f, 0.88f);
            rlRect.offsetMin = Vector2.zero;
            rlRect.offsetMax = Vector2.zero;
            var rlLayout = recipeListObj.AddComponent<VerticalLayoutGroup>();
            rlLayout.spacing = 4f;
            rlLayout.childForceExpandWidth = true;
            rlLayout.childForceExpandHeight = false;
            rlLayout.childControlHeight = true;
            rlLayout.childControlWidth = true;

            var detailText = MakeText(panel.transform, "RecipeDetail", "Select a recipe", 13,
                new Vector2(0.48f, 0.15f), new Vector2(0.98f, 0.88f), Vector2.zero, Vector2.zero);

            var craftBtn = new GameObject("CraftButton");
            craftBtn.transform.SetParent(panel.transform, false);
            var cbRect = craftBtn.AddComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.55f, 0.05f);
            cbRect.anchorMax = new Vector2(0.9f, 0.13f);
            cbRect.offsetMin = Vector2.zero;
            cbRect.offsetMax = Vector2.zero;
            craftBtn.AddComponent<Image>().color = new Color(0.2f, 0.5f, 0.2f, 1f);
            craftBtn.AddComponent<Button>();
            MakeText(craftBtn.transform, "Label", "Craft", 16,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero)
                .GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

            var closeBtn = new GameObject("CloseButton");
            closeBtn.transform.SetParent(panel.transform, false);
            var clRect = closeBtn.AddComponent<RectTransform>();
            clRect.anchorMin = new Vector2(0.85f, 0.92f);
            clRect.anchorMax = new Vector2(0.98f, 0.99f);
            clRect.offsetMin = Vector2.zero;
            clRect.offsetMax = Vector2.zero;
            closeBtn.AddComponent<Image>().color = new Color(0.6f, 0.15f, 0.15f, 1f);
            var closeBtnComp = closeBtn.AddComponent<Button>();
            MakeText(closeBtn.transform, "Label", "X", 16,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero)
                .GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

            var craftUI = panel.AddComponent<CraftingUI>();
            var so = new SerializedObject(craftUI);
            so.FindProperty("panel").objectReferenceValue = panel;
            so.FindProperty("titleText").objectReferenceValue = title.GetComponent<Text>();
            so.FindProperty("skillInfoText").objectReferenceValue = skillInfo.GetComponent<Text>();
            so.FindProperty("recipeList").objectReferenceValue = recipeListObj.transform;
            so.FindProperty("recipeDetailText").objectReferenceValue = detailText.GetComponent<Text>();
            so.FindProperty("craftButton").objectReferenceValue = craftBtn.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();

            closeBtnComp.onClick.AddListener(() => { });
            var closeWirer = panel.AddComponent<CraftingCloseWirer>();
            var cwSO = new SerializedObject(closeWirer);
            cwSO.FindProperty("closeButton").objectReferenceValue = closeBtnComp;
            cwSO.FindProperty("craftingUI").objectReferenceValue = craftUI;
            cwSO.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false);
        }

        private static void AddPlayerComponents()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            if (player.GetComponent<MaterialInventory>() == null)
                player.AddComponent<MaterialInventory>();

            if (player.GetComponent<PlayerSkills>() == null)
            {
                var ps = player.AddComponent<PlayerSkills>();
                string skillDir = "Assets/ScriptableObjects/Skills";
                var defs = new[] {
                    AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>($"{skillDir}/Gathering.asset"),
                    AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>($"{skillDir}/Cooking.asset"),
                    AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>($"{skillDir}/Smithing.asset"),
                    AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>($"{skillDir}/Fishing.asset"),
                    AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>($"{skillDir}/Mining.asset"),
                    AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>($"{skillDir}/Alchemy.asset"),
                    AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>($"{skillDir}/Crafting.asset"),
                    AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>($"{skillDir}/CombatVIG.asset"),
                    AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>($"{skillDir}/CombatSTR.asset"),
                    AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>($"{skillDir}/CombatDEX.asset"),
                    AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>($"{skillDir}/CombatINT.asset"),
                };
                var so = new SerializedObject(ps);
                var arr = so.FindProperty("skillDefinitions");
                arr.arraySize = defs.Length;
                for (int i = 0; i < defs.Length; i++)
                    arr.GetArrayElementAtIndex(i).objectReferenceValue = defs[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            if (player.GetComponent<PlayerInteractor>() == null)
                player.AddComponent<PlayerInteractor>();
        }

        private static void ApplyTint(GameObject go, Color tint)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            mat.color = tint;
            mat.SetFloat("_Smoothness", 0.15f);
            foreach (var r in renderers)
                r.sharedMaterial = mat;
        }

        private static GameObject MakeText(Transform parent, string name, string text, int fontSize,
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
            t.alignment = TextAnchor.UpperLeft;
            return obj;
        }
    }
}
#endif
