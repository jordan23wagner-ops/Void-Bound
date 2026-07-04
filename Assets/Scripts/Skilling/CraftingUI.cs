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
        private RectTransform recipeList;
        private TextMeshProUGUI detailText;
        private UnityEngine.UI.Button craftButton;

        private CraftingStation currentStation;
        private GameObject currentInstigator;
        private RecipeDefinitionSO selectedRecipe;

        public void Open(CraftingStation station, GameObject instigator)
        {
            currentStation = station;
            currentInstigator = instigator;

            Panel5cFactory.CloseOtherHomesteadPanels(gameObject, this);
            EnsureBuilt();
            panel.gameObject.SetActive(true);
            title.text = station.StationId.ToUpperInvariant();
            Refresh();
        }

        public void Close()
        {
            if (panel != null) panel.gameObject.SetActive(false);
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

            var listArea = Panel5cFactory.MakeRect("RecipeArea", content);
            Panel5cFactory.SetAnchor(listArea, new Vector2(0, 0), new Vector2(0.46f, 1));
            listArea.offsetMin = new Vector2(0, 0);
            listArea.offsetMax = new Vector2(-4, -24);
            recipeList = Panel5cFactory.CreateScrollList(listArea, "RecipeList");
            Panel5cFactory.SetAnchor((RectTransform)recipeList.parent, Vector2.zero, Vector2.one);

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

            var skills = currentInstigator.GetComponent<PlayerSkills>();
            int level = skills?.GetLevel(currentStation.StationType) ?? 1;
            int xp = skills?.GetXP(currentStation.StationType) ?? 0;
            int xpNext = skills?.GetXPToNext(currentStation.StationType) ?? 100;

            skillInfo.text = $"{currentStation.StationType}  Lv {level}    XP {xp} / {xpNext}";

            for (int i = recipeList.childCount - 1; i >= 0; i--)
                Destroy(recipeList.GetChild(i).gameObject);

            if (currentStation.AvailableRecipes != null)
            {
                foreach (var recipe in currentStation.AvailableRecipes)
                {
                    if (recipe == null) continue;
                    var captured = recipe;
                    bool locked = level < recipe.requiredSkillLevel;

                    var row = Panel5cFactory.CreateListRow(recipeList,
                        recipe.displayName,
                        locked ? $"Lv {recipe.requiredSkillLevel}" : "",
                        locked ? (Color)Panel5cFactory.TextMuted : (Color)Panel5cFactory.TextPrimary,
                        Panel5cFactory.TextMuted,
                        interactable: !locked);
                    row.onClick.AddListener(() => SelectRecipe(captured));
                }
            }

            selectedRecipe = null;
            detailText.text = "Select a recipe";
            craftButton.gameObject.SetActive(false);
        }

        private void SelectRecipe(RecipeDefinitionSO recipe)
        {
            selectedRecipe = recipe;

            var matInv = currentInstigator?.GetComponent<MaterialInventory>();
            string detail = $"<b>{recipe.displayName}</b>\n";
            detail += $"<color=#888d84>{recipe.requiredSkill} Lv {recipe.requiredSkillLevel}   +{recipe.xpReward} XP</color>\n\nIngredients:\n";

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
            if (recipe.outputType == RecipeOutputType.Gear && recipe.outputGear != null)
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
            var skills = currentInstigator.GetComponent<PlayerSkills>();

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

            if (selectedRecipe.outputType == RecipeOutputType.Gear && selectedRecipe.outputGear != null)
                gearInv?.AddItem(selectedRecipe.outputGear);
            else if (selectedRecipe.outputMaterial != null)
                matInv?.AddMaterial(selectedRecipe.outputMaterial, selectedRecipe.outputQuantity);

            skills?.AddXP(selectedRecipe.requiredSkill, selectedRecipe.xpReward);

            Combat.FloatingDamageNumber.SpawnText(currentInstigator.transform.position,
                $"Crafted: {selectedRecipe.displayName}", new Color(0.3f, 0.8f, 1f));

            var kept = selectedRecipe;
            Refresh();
            SelectRecipe(kept);
        }
    }
}
