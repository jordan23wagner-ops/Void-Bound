# Phase 3 — Gear & Inventory

**Read `CONTEXT.md` before starting (including the new git-pull-first rule). Phases 0-2 are complete and committed — extend, don't refactor.**

## Goal
Full 11-slot equipment system with working equip/unequip, stat modifiers applied to CharacterStatsSO at runtime (visibly affecting combat), and visual rarity tiers (emissive trim for Rare+, particle accent for Legendary+) so gear quality is readable at a glance.

## Tasks

### 1. Resolve Open Item: Weapon Types
- Check if you have filesystem access to the old RunePortal project (likely `C:\Users\Jordon\OneDrive\Desktop\RunePortal\`)
- If yes: read `runeportal_phase5.html` or `CONTEXT.md` there and find the definitive 8 weapon types from GEAR_DB. Use the exact list, not the tentative GDD placeholder.
- If no access: use the GDD's tentative list (Sword, Axe, Spear, Mace, Bow, Crossbow, Staff, Wand) and flag it as still-unconfirmed in CONTEXT.md.
- Update the `WeaponType` enum in `Scripts/Data/Enums.cs` with the confirmed list.
- Update `Void_Bound_GDD.md` Section 2 to mark this resolved (or still-flagged if using the placeholder).

### 2. Complete GearItemSO
Flesh out the stub from Phase 0 (`Scripts/Data/GearItemSO.cs`) with full fields per CODING_STANDARDS.md pattern:
- `itemId`, `displayName`
- `slot` (EquipmentSlot enum — all 11: Weapon, Shield, Head, Cape, Neck, Body, Legs, Hands, Feet, Ring, Ammo)
- `weaponType` (only relevant if slot == Weapon)
- `rarity` (RarityTier enum — all 9, including Voidforged)
- `statModifiers` (struct/class with STR/DEX/VIG/INT bonus fields)
- `visualPrefab` (GameObject reference — what the item looks like equipped/in inventory)
- `setId` (string, empty if not part of a set — full set bonus logic is deferred past this phase, just keep the field)

### 3. Rarity Visual System
- New script: `Scripts/Inventory/RarityVisualEffects.cs` (or similar)
- Common/Uncommon: flat color material, no emission
- Rare and above: apply emissive material trim, color-coded per tier (reuse/establish a color mapping — e.g. Rare = cyan-ish, Epic = purple-ish, Legendary+ = gold-ish, confirm exact colors feel right against the GDD's warm palette)
- Legendary and above (including Voidforged): add a simple particle accent (basic sparkle/aura via a lightweight URP particle system) when the item is equipped or displayed
- This should be a reusable function/component, not hardcoded per-item — apply based on the `rarity` field automatically

### 4. Test Gear Items
Create at least 4 test `GearItemSO` assets spanning the rarity range to verify the visual system works correctly:
- 1 Common weapon
- 1 Rare weapon (confirm emissive trim shows)
- 1 Legendary weapon (confirm particle accent shows)
- 1 Voidforged weapon (confirm top-tier visual is appropriately distinct/impressive)

### 5. Inventory System
- New script: `Scripts/Inventory/PlayerInventory.cs`
- Tracks equipped items per slot (11 slots) and a simple unequipped item list/array
- `EquipItem(GearItemSO item)` — handles slot validation (weapon goes in weapon slot, etc.), unequips whatever was there first if applicable, applies `statModifiers` to the Player's `CharacterStatsSO`/`StatsComponent`
- `UnequipItem(EquipmentSlot slot)` — removes stat modifiers, returns item to inventory list
- Equipping a weapon should also swap the `visualPrefab` so the player visibly holds/wears the new item

### 6. Basic Inventory UI
- Simple UI screen (doesn't need to be pretty — functional first pass): 11 equipment slot icons/buttons + a scrollable list of unequipped inventory items
- Tapping/clicking an inventory item equips it to the appropriate slot
- Tapping/clicking an equipped slot unequips it back to inventory
- Show rarity color/visual indicator on each item icon in the UI list too (not just the 3D world model)

### 7. Verify Stat Application Affects Combat
- Equip a test weapon with a STR bonus, confirm the player's auto-attack damage (from Phase 2) actually increases accordingly
- This is the critical end-to-end check — gear needs to meaningfully affect the existing combat system, not just exist cosmetically

### 8. Self-Test Before Reporting Complete (per standing protocol)
Using Unity MCP editor control tools:
1. `git pull origin main` first (per CONTEXT.md Critical Rule #0)
2. Enter Play Mode programmatically
3. Equip each of the 4 test gear items in turn, confirm visual rarity effects render correctly (emissive on Rare, particles on Legendary/Voidforged)
4. Confirm equipping a weapon changes auto-attack damage output (compare before/after numbers in combat against the TestEnemy)
5. Confirm unequip correctly removes stat bonuses and returns item to inventory list
6. Check Console for errors throughout
7. Exit Play Mode
8. Only report Phase 3 complete if all of the above passes

### 9. Commit & Update CONTEXT.md
- Commit as: `[Phase 3] Gear & inventory system - 11 slots, equip/unequip, rarity visual tiers`
- Update CONTEXT.md: confirm weapon type resolution status, log inventory UI approach taken, confirm self-test passed, point "Current Phase" to Phase 4

## Do NOT
- Do not build the full loot/drop table system yet (Phase 4) — test items are manually created assets for now, not dropped from enemies
- Do not build full set-bonus calculation logic — just keep the `setId` field present for future use
- Do not polish the inventory UI visually beyond functional — full UI/UX polish is a later phase
- Do not guess at the weapon type list if RunePortal source isn't accessible — use the documented placeholder and flag it clearly instead
