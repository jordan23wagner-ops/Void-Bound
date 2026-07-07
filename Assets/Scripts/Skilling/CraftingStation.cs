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

        // Open the panel once per approach — without this the base default (true)
        // re-fires OnInteract every proximity tick, re-Open()ing the panel and
        // making it flicker/rebuild. Matches every other station.
        public override bool RepeatOnProximity => false;

        public override void OnInteract(GameObject instigator)
        {
            var ui = Object.FindAnyObjectByType<CraftingUI>();
            if (ui != null)
                ui.Open(this, instigator);
        }
    }
}
