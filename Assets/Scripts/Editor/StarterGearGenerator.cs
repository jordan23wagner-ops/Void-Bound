#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VoidBound.Data;

namespace VoidBound.Editor
{
    // Creates the starter armor/gear GearItemSO assets, wires each one's
    // visualPrefab to its equipment model, back-fills visualPrefab on the
    // existing swords, and assigns tier-appropriate visible gear to the goblin
    // enemy definitions. Idempotent. All stat/gold numbers are placeholders — tunable.
    public static class StarterGearGenerator
    {
        private const string GearDir = "Assets/ScriptableObjects/Gear";
        private const string ModelDir = "Assets/Resources/Equipment";

        [MenuItem("VoidBound/Gear - Generate Starter Set")]
        public static void Generate()
        {
            if (!AssetDatabase.IsValidFolder(GearDir))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Gear");

            // ── Full Iron set (Common) — one per visible body slot ──
            Gear("iron_helm", "Iron Helm", EquipmentSlot.Helm, RarityTier.Common, "Helm", vig: 2, gold: 40);
            Gear("iron_chestplate", "Iron Chestplate", EquipmentSlot.Body, RarityTier.Common, "Body", vig: 4, gold: 80);
            Gear("iron_greaves", "Iron Greaves", EquipmentSlot.Legs, RarityTier.Common, "Legs", vig: 3, gold: 60);
            Gear("iron_boots", "Iron Boots", EquipmentSlot.Boots, RarityTier.Common, "Boots", vig: 1, dex: 1, gold: 35);
            Gear("iron_gauntlets", "Iron Gauntlets", EquipmentSlot.Gloves, RarityTier.Common, "Gloves", str: 1, gold: 35);
            Gear("travelers_cape", "Traveler's Cape", EquipmentSlot.Cape, RarityTier.Common, "Cape", dex: 1, gold: 30);
            Gear("wooden_shield", "Wooden Shield", EquipmentSlot.Shield, RarityTier.Common, "Shield", vig: 3, gold: 45);
            Gear("iron_amulet", "Iron Amulet", EquipmentSlot.Amulet, RarityTier.Common, "Amulet", intel: 1, gold: 40);

            // ── Higher-rarity showcase pieces (exercise the rarity tint) ──
            Gear("runic_chestplate", "Runic Chestplate", EquipmentSlot.Body, RarityTier.Rare, "Body", vig: 8, intel: 3, gold: 300);
            Gear("dragoncrest_helm", "Dragoncrest Helm", EquipmentSlot.Helm, RarityTier.Epic, "Helm", vig: 6, str: 4, gold: 600);
            Gear("cape_of_kings", "Cape of Kings", EquipmentSlot.Cape, RarityTier.Legendary, "Cape", dex: 6, vig: 4, gold: 900);

            // ── Back-fill weapon models on the existing swords ──
            AssignVisual("Assets/ScriptableObjects/TestGear/Rusty_Sword_Common.asset", "Sword");
            AssignVisual("Assets/ScriptableObjects/TestGear/Arcane_Blade_Rare.asset", "Sword");
            AssignVisual("Assets/ScriptableObjects/TestGear/Flamecleaver_Legendary.asset", "Sword");
            AssignVisual("Assets/ScriptableObjects/TestGear/Voidreaver_Voidforged.asset", "Sword");

            // ── Ranged + mage weapons (models already exist; DEX/INT-flavored) ──
            Gear("hunters_bow", "Hunter's Bow", EquipmentSlot.Weapon, RarityTier.Common, "Bow",
                weaponType: WeaponType.Bow, dex: 4, gold: 60);
            Gear("oak_crossbow", "Oak Crossbow", EquipmentSlot.Weapon, RarityTier.Uncommon, "Crossbow",
                weaponType: WeaponType.Crossbow, dex: 5, gold: 90);
            Gear("willow_staff", "Willow Staff", EquipmentSlot.Weapon, RarityTier.Common, "Staff",
                weaponType: WeaponType.Staff, intel: 4, gold: 60);
            Gear("apprentice_wand", "Apprentice Wand", EquipmentSlot.Weapon, RarityTier.Common, "Wand",
                weaponType: WeaponType.Wand, intel: 3, gold: 45);

            // ── Enemy gear ──
            var goblinClub = Gear("goblin_club", "Goblin Club", EquipmentSlot.Weapon, RarityTier.Common, "Mace",
                weaponType: WeaponType.Mace, str: 1, gold: 5);
            var goblinHelm = Gear("goblin_helm", "Goblin Helm", EquipmentSlot.Helm, RarityTier.Common, "Helm", vig: 1, gold: 8);
            var goblinPlate = Gear("goblin_plate", "Goblin Scrap Plate", EquipmentSlot.Body, RarityTier.Uncommon, "Body", vig: 2, gold: 15);

            AssetDatabase.SaveAssets();

            AssignEnemyGear("Assets/ScriptableObjects/Enemies/Goblin_Scout.asset", goblinClub, null);
            AssignEnemyGear("Assets/ScriptableObjects/Enemies/Goblin_Warrior.asset", goblinClub, new[] { goblinHelm });
            AssignEnemyGear("Assets/ScriptableObjects/Enemies/Goblin_Champion.asset", goblinClub, new[] { goblinHelm, goblinPlate });

            AssetDatabase.SaveAssets();
            Debug.Log("[StarterGear] Generated gear set, wired visuals, assigned enemy gear.");
        }

        public static void GenerateFromBatch() => Generate();

        private static GearItemSO Gear(string id, string name, EquipmentSlot slot, RarityTier rarity,
            string model, WeaponType weaponType = WeaponType.None,
            int str = 0, int dex = 0, int vig = 0, int intel = 0, int gold = 0)
        {
            string path = $"{GearDir}/{id}.asset";
            var gear = AssetDatabase.LoadAssetAtPath<GearItemSO>(path);
            bool created = gear == null;
            if (created) gear = ScriptableObject.CreateInstance<GearItemSO>();

            gear.itemId = id;
            gear.displayName = name;
            gear.slot = slot;
            gear.weaponType = weaponType;
            gear.rarity = rarity;
            gear.statModifiers = new CharacterStats(str, dex, vig, intel);
            gear.goldValue = gold;
            gear.visualPrefab = LoadModel(model);

            if (created) AssetDatabase.CreateAsset(gear, path);
            else EditorUtility.SetDirty(gear);
            return gear;
        }

        private static GameObject LoadModel(string model)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelDir}/{model}.fbx");
            if (go == null) Debug.LogWarning($"[StarterGear] Model {model}.fbx not found.");
            return go;
        }

        private static void AssignVisual(string gearPath, string model)
        {
            var gear = AssetDatabase.LoadAssetAtPath<GearItemSO>(gearPath);
            if (gear == null) { Debug.LogWarning($"[StarterGear] {gearPath} not found."); return; }
            gear.visualPrefab = LoadModel(model);
            EditorUtility.SetDirty(gear);
        }

        private static void AssignEnemyGear(string defPath, GearItemSO weapon, GearItemSO[] armor)
        {
            var def = AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>(defPath);
            if (def == null) { Debug.LogWarning($"[StarterGear] {defPath} not found."); return; }
            def.weapon = weapon;
            def.armor = armor;
            EditorUtility.SetDirty(def);
        }
    }
}
#endif
