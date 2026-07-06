using System;
using UnityEngine;

namespace VoidBound.Data
{
    [Serializable]
    public struct RecipeIngredient
    {
        public MaterialItemSO material;
        public int quantity;
    }

    public enum RecipeOutputType { Material, Gear }

    [CreateAssetMenu(fileName = "New RecipeDefinition", menuName = "VoidBound/Recipe Definition")]
    public class RecipeDefinitionSO : ScriptableObject
    {
        public string recipeId;
        public string displayName;
        public SkillType requiredSkill;
        public RarityTier requiredToolTier; // tool-tier gate for gather/craft (Common = ungated)
        public int requiredSkillLevel;      // legacy; no longer gates (skills are tool-gated)
        public string requiredStation;

        [Header("Inputs")]
        public RecipeIngredient[] ingredients;

        [Header("Output")]
        public RecipeOutputType outputType;
        public MaterialItemSO outputMaterial;
        public GearItemSO outputGear;
        public int outputQuantity = 1;

        [Header("XP")]
        public int xpReward = 10;
    }
}
