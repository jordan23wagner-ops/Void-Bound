using UnityEngine;
using VoidBound.Core;
using VoidBound.Data;

namespace VoidBound.Skilling
{
    public class CraftingStation : Interactable
    {
        [SerializeField] private string stationId;
        [SerializeField] private SkillType skillType;
        [SerializeField] private RecipeDefinitionSO[] availableRecipes;

        public string StationId => stationId;
        public SkillType StationType => skillType;
        public RecipeDefinitionSO[] AvailableRecipes => availableRecipes;

        public override void OnInteract(GameObject instigator)
        {
            var ui = FindAnyObjectByType<CraftingUI>();
            if (ui != null)
                ui.Open(this, instigator);
        }
    }
}
