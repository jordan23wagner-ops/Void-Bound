using UnityEngine;
using UnityEngine.UI;
using VoidBound.Data;
using VoidBound.Inventory;

namespace VoidBound.Skilling
{
    public class CraftingUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text skillInfoText;
        [SerializeField] private Transform recipeList;
        [SerializeField] private Text recipeDetailText;
        [SerializeField] private Button craftButton;

        private CraftingStation currentStation;
        private GameObject currentInstigator;
        private RecipeDefinitionSO selectedRecipe;

        private void Start()
        {
            if (panel != null) panel.SetActive(false);
            if (craftButton != null) craftButton.onClick.AddListener(DoCraft);
        }

        public void Open(CraftingStation station, GameObject instigator)
        {
            currentStation = station;
            currentInstigator = instigator;
            if (panel != null) panel.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            currentStation = null;
            selectedRecipe = null;
        }

        private void Refresh()
        {
            if (currentStation == null || currentInstigator == null) return;

            var skills = currentInstigator.GetComponent<PlayerSkills>();
            int level = skills?.GetLevel(currentStation.StationType) ?? 1;
            int xp = skills?.GetXP(currentStation.StationType) ?? 0;
            int xpNext = skills?.GetXPToNext(currentStation.StationType) ?? 100;

            if (titleText != null) titleText.text = currentStation.StationId;
            if (skillInfoText != null)
                skillInfoText.text = $"{currentStation.StationType} Lv{level}  XP: {xp}/{xpNext}";

            ClearChildren(recipeList);
            if (currentStation.AvailableRecipes == null) return;

            foreach (var recipe in currentStation.AvailableRecipes)
            {
                if (recipe == null) continue;
                var captured = recipe;
                bool locked = level < recipe.requiredSkillLevel;

                var btn = new GameObject(recipe.displayName);
                btn.transform.SetParent(recipeList, false);
                btn.AddComponent<LayoutElement>().preferredHeight = 36f;
                var img = btn.AddComponent<Image>();
                img.color = locked ? new Color(0.3f, 0.2f, 0.2f, 0.8f) : new Color(0.2f, 0.3f, 0.2f, 0.8f);
                var b = btn.AddComponent<Button>();
                b.interactable = !locked;
                b.onClick.AddListener(() => SelectRecipe(captured));

                var textObj = new GameObject("Text");
                textObj.transform.SetParent(btn.transform, false);
                var textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(6f, 0f);
                textRect.offsetMax = new Vector2(-6f, 0f);
                var t = textObj.AddComponent<Text>();
                t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                t.fontSize = 13;
                t.alignment = TextAnchor.MiddleLeft;
                t.color = locked ? Color.gray : Color.white;
                t.text = locked ? $"{recipe.displayName} (Lv{recipe.requiredSkillLevel})" : recipe.displayName;
            }

            selectedRecipe = null;
            if (recipeDetailText != null) recipeDetailText.text = "Select a recipe";
            if (craftButton != null) craftButton.gameObject.SetActive(false);
        }

        private void SelectRecipe(RecipeDefinitionSO recipe)
        {
            selectedRecipe = recipe;
            if (recipeDetailText == null) return;

            var matInv = currentInstigator?.GetComponent<MaterialInventory>();
            string detail = $"{recipe.displayName}\n";
            detail += $"Skill: {recipe.requiredSkill} Lv{recipe.requiredSkillLevel}\n";
            detail += $"XP: +{recipe.xpReward}\n\nIngredients:\n";

            bool canCraft = true;
            if (recipe.ingredients != null)
            {
                foreach (var ing in recipe.ingredients)
                {
                    if (ing.material == null) continue;
                    int have = matInv?.GetCount(ing.material.itemId) ?? 0;
                    bool enough = have >= ing.quantity;
                    if (!enough) canCraft = false;
                    detail += $"  {ing.material.displayName}: {have}/{ing.quantity}" +
                              (enough ? " ✓" : " ✗") + "\n";
                }
            }

            detail += $"\nOutput: ";
            if (recipe.outputType == RecipeOutputType.Gear && recipe.outputGear != null)
                detail += recipe.outputGear.displayName;
            else if (recipe.outputMaterial != null)
                detail += $"{recipe.outputMaterial.displayName} x{recipe.outputQuantity}";

            recipeDetailText.text = detail;
            if (craftButton != null) craftButton.gameObject.SetActive(canCraft);
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

            Refresh();
            if (selectedRecipe != null) SelectRecipe(selectedRecipe);
        }

        private void ClearChildren(Transform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }
    }
}
