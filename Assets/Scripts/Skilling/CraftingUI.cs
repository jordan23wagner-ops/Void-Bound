using UnityEngine;
using TMPro;
using VoidBound.Data;
using VoidBound.Inventory;
using VoidBound.UI;

namespace VoidBound.Skilling
{
    // Crafting panel for Forge/Campfire/Garden stations. Polish pass 2:
    // self-builds from Panel5cFactory (rounded panels, hover rows) on the
    // HUDCanvas it lives on — the Phase 5 scene-wired legacy-Text panel is
    // retired. Craft logic unchanged.
    public class CraftingUI : MonoBehaviour
    {
        private RectTransform panel;
        private TextMeshProUGUI title;
        private TextMeshProUGUI skillInfo;
        private RectTransform recipeArea;   // list column; shrinks to make room for tabs
        private RectTransform recipeList;
        private RectTransform tabBar;
        private TextMeshProUGUI detailText;
        private UnityEngine.UI.Button craftButton;

        private CraftingStation currentStation;
        private GameObject currentInstigator;
        private RecipeDefinitionSO selectedRecipe;
        private RecipeOutputType currentCategory = RecipeOutputType.Tool;

        private struct TabEntry { public UnityEngine.UI.Button btn; public TextMeshProUGUI label; public RecipeOutputType cat; }
        private readonly System.Collections.Generic.List<TabEntry> tabs = new System.Collections.Generic.List<TabEntry>();

        public void Open(CraftingStation station, GameObject instigator)
        {
            currentStation = station;
            currentInstigator = instigator;

            Panel5cFactory.CloseOtherHomesteadPanels(gameObject, this);
            EnsureBuilt();
            panel.gameObject.SetActive(true);
            StationProximityCloser.Track(gameObject, this, station, Close);
            title.text = station.StationId.ToUpperInvariant();
            Refresh();
        }

        public void Close()
        {
            if (panel != null) panel.gameObject.SetActive(false);
            StationProximityCloser.Untrack(gameObject, this);
            currentStation = null;
            selectedRecipe = null;
        }

        private void EnsureBuilt()
        {
            if (panel != null) return;

            panel = Panel5cFactory.CreatePanel(transform, "CraftingPanel5c", "CRAFTING",
                560f, 400f, out var content, out var closeBtn);
            closeBtn.onClick.AddListener(Close);
            title = panel.Find("Header/Title").GetComponent<TextMeshProUGUI>();

            skillInfo = Panel5cFactory.CreateLabel(content, "SkillInfo", "", 11f, Panel5cFactory.TextMuted);
            Panel5cFactory.SetAnchor(skillInfo.rectTransform, new Vector2(0, 1), new Vector2(1, 1));
            skillInfo.rectTransform.pivot = new Vector2(0.5f, 1f);
            skillInfo.rectTransform.sizeDelta = new Vector2(-8, 18);

            recipeArea = Panel5cFactory.MakeRect("RecipeArea", content);
            Panel5cFactory.SetAnchor(recipeArea, new Vector2(0, 0), new Vector2(0.46f, 1));
            recipeArea.offsetMin = new Vector2(0, 0);
            recipeArea.offsetMax = new Vector2(-4, -24);
            recipeList = Panel5cFactory.CreateScrollList(recipeArea, "RecipeList");
            Panel5cFactory.SetAnchor((RectTransform)recipeList.parent, Vector2.zero, Vector2.one);

            // Category tab strip (Tools / Gear / Ammo…) above the recipe list.
            // Populated per-station in BuildTabs; hidden for single-category stations.
            tabBar = Panel5cFactory.MakeRect("TabBar", content);
            tabBar.anchorMin = new Vector2(0, 1);
            tabBar.anchorMax = new Vector2(0.46f, 1);
            tabBar.pivot = new Vector2(0.5f, 1f);
            tabBar.sizeDelta = new Vector2(-4, 20);
            tabBar.anchoredPosition = new Vector2(-2, -24);
            var tabLayout = tabBar.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            tabLayout.spacing = 4f;
            tabLayout.childForceExpandWidth = true;
            tabLayout.childForceExpandHeight = true;
            tabLayout.childControlWidth = true;
            tabLayout.childControlHeight = true;

            var detailArea = Panel5cFactory.MakeRect("DetailArea", content);
            Panel5cFactory.SetAnchor(detailArea, new Vector2(0.46f, 0), new Vector2(1, 1));
            detailArea.offsetMin = new Vector2(4, 0);
            detailArea.offsetMax = new Vector2(0, -24);
            Panel5cFactory.AddPanelBg(detailArea.gameObject, Panel5cFactory.SlotBg, raycast: false);

            detailText = Panel5cFactory.CreateLabel(detailArea, "Detail", "Select a recipe", 11f,
                Panel5cFactory.TextPrimary);
            Panel5cFactory.SetAnchor(detailText.rectTransform, Vector2.zero, Vector2.one);
            detailText.rectTransform.offsetMin = new Vector2(12, 52);
            detailText.rectTransform.offsetMax = new Vector2(-12, -10);
            detailText.alignment = TextAlignmentOptions.TopLeft;
            detailText.textWrappingMode = TextWrappingModes.Normal;

            craftButton = Panel5cFactory.CreateActionButton(detailArea, "CRAFT");
            var btnRT = (RectTransform)craftButton.transform;
            Panel5cFactory.SetAnchor(btnRT, new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            btnRT.pivot = new Vector2(0.5f, 0f);
            btnRT.sizeDelta = new Vector2(130, 32);
            btnRT.anchoredPosition = new Vector2(0, 10);
            craftButton.onClick.AddListener(DoCraft);
            craftButton.gameObject.SetActive(false);

            panel.gameObject.SetActive(false);
        }

        private void Refresh()
        {
            if (currentStation == null || currentInstigator == null) return;
            skillInfo.text = currentStation.StationType.ToString();
            BuildTabs();
            RefreshList();
        }

        // Rebuild the category tabs for whatever this station offers. Tabs show
        // only when a station has more than one output category (e.g. the Crafting
        // Bench's Tools / Gear / Ammo); single-category stations skip the strip.
        private void BuildTabs()
        {
            foreach (var t in tabs) if (t.btn != null) Destroy(t.btn.gameObject);
            tabs.Clear();

            var present = new System.Collections.Generic.List<RecipeOutputType>();
            AddCategoryIfPresent(present, RecipeOutputType.Tool);
            AddCategoryIfPresent(present, RecipeOutputType.Gear);
            AddCategoryIfPresent(present, RecipeOutputType.Material);

            bool showTabs = present.Count > 1;
            tabBar.gameObject.SetActive(showTabs);
            recipeArea.offsetMax = new Vector2(-4, showTabs ? -48 : -24);

            if (!present.Contains(currentCategory))
                currentCategory = present.Count > 0 ? present[0] : RecipeOutputType.Tool;

            if (!showTabs) return;

            foreach (var cat in present)
            {
                var capturedCat = cat;
                var entry = MakeTab(CategoryLabel(cat), capturedCat);
                entry.btn.onClick.AddListener(() =>
                {
                    currentCategory = capturedCat;
                    UpdateTabVisuals();
                    RefreshList();
                });
                tabs.Add(entry);
            }
            UpdateTabVisuals();
        }

        private void AddCategoryIfPresent(System.Collections.Generic.List<RecipeOutputType> list, RecipeOutputType cat)
        {
            if (list.Contains(cat) || currentStation.AvailableRecipes == null) return;
            foreach (var r in currentStation.AvailableRecipes)
                if (r != null && r.outputType == cat) { list.Add(cat); return; }
        }

        private TabEntry MakeTab(string text, RecipeOutputType cat)
        {
            var rt = Panel5cFactory.MakeRect("Tab", tabBar);
            var img = Panel5cFactory.AddButtonBg(rt.gameObject, Color.white, true);
            rt.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 1f;
            var lbl = Panel5cFactory.MakeTMP("Label", rt);
            Panel5cFactory.SetAnchor(lbl.rectTransform, Vector2.zero, Vector2.one);
            lbl.text = text;
            lbl.fontSize = 10f;
            lbl.fontStyle = FontStyles.Bold;
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.color = Panel5cFactory.TextMuted;
            var btn = rt.gameObject.AddComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = img;
            btn.colors = Panel5cFactory.MakeColors(Panel5cFactory.RowBg, Panel5cFactory.RowHover, Panel5cFactory.RowPressed);
            return new TabEntry { btn = btn, label = lbl, cat = cat };
        }

        private void UpdateTabVisuals()
        {
            foreach (var t in tabs)
                if (t.label != null)
                    t.label.color = t.cat == currentCategory ? (Color)Panel5cFactory.Gold : (Color)Panel5cFactory.TextMuted;
        }

        // The station-appropriate name for a Material tab (bench = Ammo, campfire
        // = Food, garden = Potions, forge = Bars); Tools/Gear are universal.
        private string CategoryLabel(RecipeOutputType type)
        {
            if (type == RecipeOutputType.Tool) return "Tools";
            if (type == RecipeOutputType.Gear) return "Gear";
            switch (currentStation.StationType)
            {
                case SkillType.Crafting: return "Ammo";
                case SkillType.Cooking:  return "Food";
                case SkillType.Alchemy:  return "Potions";
                case SkillType.Smithing: return "Bars";
                default:                 return "Items";
            }
        }

        private void RefreshList()
        {
            var tools = currentInstigator.GetComponent<PlayerTools>();

            for (int i = recipeList.childCount - 1; i >= 0; i--)
                Destroy(recipeList.GetChild(i).gameObject);

            if (currentStation.AvailableRecipes != null)
            {
                foreach (var recipe in currentStation.AvailableRecipes)
                {
                    if (recipe == null || recipe.outputType != currentCategory) continue;
                    var captured = recipe;
                    RarityTier toolTier = tools != null ? tools.GetToolTier(recipe.requiredSkill) : RarityTier.Common;
                    bool locked = (int)toolTier < (int)recipe.requiredToolTier;

                    var row = Panel5cFactory.CreateListRow(recipeList,
                        recipe.displayName,
                        OutputTierLabel(recipe),
                        locked ? (Color)Panel5cFactory.TextMuted : (Color)Panel5cFactory.TextPrimary,
                        locked ? (Color)Panel5cFactory.TextMuted : OutputTierColor(recipe),
                        interactable: !locked);
                    row.onClick.AddListener(() => SelectRecipe(captured));
                }
            }

            selectedRecipe = null;
            detailText.text = "Select a recipe";
            craftButton.gameObject.SetActive(false);
        }

        // The crafted item's OWN tier/rarity (accurate), not the tool tier
        // required to make it. Ammo/materials show their stack size instead.
        private string OutputTierLabel(RecipeDefinitionSO r)
        {
            if (r.outputType == RecipeOutputType.Tool && r.outputTool != null) return r.outputTool.tier.ToString();
            if (r.outputType == RecipeOutputType.Gear && r.outputGear != null) return r.outputGear.rarity.ToString();
            if (r.outputType == RecipeOutputType.Material && r.outputQuantity > 1) return "x" + r.outputQuantity;
            return "";
        }

        private Color OutputTierColor(RecipeDefinitionSO r)
        {
            if (r.outputType == RecipeOutputType.Tool && r.outputTool != null) return RarityVisualEffects.GetRarityColor(r.outputTool.tier);
            if (r.outputType == RecipeOutputType.Gear && r.outputGear != null) return RarityVisualEffects.GetRarityColor(r.outputGear.rarity);
            return Panel5cFactory.TextPrimary;
        }

        private void SelectRecipe(RecipeDefinitionSO recipe)
        {
            selectedRecipe = recipe;

            var matInv = currentInstigator?.GetComponent<MaterialInventory>();
            string detail = $"<b>{recipe.displayName}</b>\n";
            string gate = recipe.requiredToolTier > RarityTier.Common
                ? $"Requires {recipe.requiredToolTier} tool"
                : recipe.requiredSkill.ToString();
            detail += $"<color=#888d84>{gate}</color>\n\nIngredients:\n";

            bool canCraft = true;
            if (recipe.ingredients != null)
            {
                foreach (var ing in recipe.ingredients)
                {
                    if (ing.material == null) continue;
                    int have = matInv?.GetCount(ing.material.itemId) ?? 0;
                    bool enough = have >= ing.quantity;
                    if (!enough) canCraft = false;
                    string color = enough ? "#97c459" : "#e24b4a";
                    detail += $"  <color={color}>{ing.material.displayName}  {have}/{ing.quantity}</color>\n";
                }
            }

            detail += "\nOutput: ";
            if (recipe.outputType == RecipeOutputType.Tool && recipe.outputTool != null)
                detail += recipe.outputTool.displayName;
            else if (recipe.outputType == RecipeOutputType.Gear && recipe.outputGear != null)
                detail += recipe.outputGear.displayName;
            else if (recipe.outputMaterial != null)
                detail += $"{recipe.outputMaterial.displayName} x{recipe.outputQuantity}";

            detailText.text = detail;
            craftButton.gameObject.SetActive(canCraft);
        }

        private void DoCraft()
        {
            if (selectedRecipe == null || currentInstigator == null) return;

            var matInv = currentInstigator.GetComponent<MaterialInventory>();
            var gearInv = currentInstigator.GetComponent<PlayerInventory>();

            if (selectedRecipe.ingredients != null)
            {
                foreach (var ing in selectedRecipe.ingredients)
                {
                    if (ing.material == null) continue;
                    if (!matInv.ConsumeMaterial(ing.material.itemId, ing.quantity))
                    {
                        Debug.LogWarning("Not enough materials!");
                        return;
                    }
                }
            }

            if (selectedRecipe.outputType == RecipeOutputType.Tool && selectedRecipe.outputTool != null)
            {
                var tools = currentInstigator.GetComponent<PlayerTools>();
                tools?.SetToolTier(selectedRecipe.outputTool.skill, selectedRecipe.outputTool.tier);
            }
            else if (selectedRecipe.outputType == RecipeOutputType.Gear && selectedRecipe.outputGear != null)
                gearInv?.AddItem(selectedRecipe.outputGear);
            else if (selectedRecipe.outputMaterial != null)
                matInv?.AddMaterial(selectedRecipe.outputMaterial, selectedRecipe.outputQuantity);

            Combat.FloatingDamageNumber.SpawnText(currentInstigator.transform.position,
                $"Crafted: {selectedRecipe.displayName}", new Color(0.3f, 0.8f, 1f));

            var kept = selectedRecipe;
            Refresh();
            SelectRecipe(kept);
        }
    }
}
