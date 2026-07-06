namespace VoidBound.Data
{
    public enum EquipmentSlot
    {
        Weapon,
        Shield,
        Helm,
        Body,
        Legs,
        Boots,
        Gloves,
        Amulet,
        Ring,
        Ammo,
        Cape
    }

    public enum WeaponType
    {
        None,
        Sword,
        Sword2H,
        Dagger,
        Mace,
        Bow,
        Crossbow,
        Staff,
        Wand
    }

    // Canonical rarity ladder (ascending). Colours in RarityVisualEffects:
    // Common grey · Uncommon white · Magic blue · Rare yellow · Epic purple ·
    // Legendary orange · Obsidian silver(blackish-white) · Radiant rose(reddish-
    // white) · Void purple/black.
    public enum RarityTier
    {
        Common,
        Uncommon,
        Magic,
        Rare,
        Epic,
        Legendary,
        Obsidian,
        Radiant,
        Void
    }

    public enum EnemyTier
    {
        Weak,
        Standard,
        Elite,
        RareElite,
        NamedElite,
        MiniBoss,
        NamedBoss,
        WorldBoss
    }

    public enum SkillType
    {
        Fishing,
        Gathering,
        Mining,
        Smithing,
        Crafting,
        Cooking,
        Alchemy,
        CombatVIG,
        CombatSTR,
        CombatDEX,
        CombatINT,
        Woodcutting // added last to preserve existing serialized indices
    }
}
