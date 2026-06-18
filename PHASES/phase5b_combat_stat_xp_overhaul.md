# Phase 5b — Combat Stat & XP System Overhaul

**Read `CONTEXT.md` first (git pull per Critical Rule #0). This is an amendment that touches already-completed work in Phase 2 (combat/damage), Phase 3b (HUD stats panel), and Phase 5 — extend/modify those existing systems carefully, don't break what's already working.**

## Goal
VIG, STR, DEX, and INT become individually leveled stats (matching RunePortal's original design — RunePortal already had this exact 4-stat system, renamed from attack/defence to STR/DEX/VIG/INT). XP is earned from combat damage, not generic kills: each combat style (STR/DEX/INT) trains its own stat based on weapon type used, VIG levels passively off a fraction of that XP, and Character Level derives from all 4 combined.

## Critical First Step: Check RunePortal Source
Before implementing anything from scratch, check if `C:\Users\Jordon\OneDrive\Desktop\RunePortal\runeportal_phase5.html` (or its CONTEXT.md) is accessible. RunePortal's session history confirms it already had this exact stat rename (attack→dexterity, defence→vigor, strength kept, intelligence added) — meaning the actual XP formulas, weapon-to-stat mapping, and Character Level calculation may already exist in that source. If accessible, pull the real implementation rather than using the defaults below. If not accessible, use the defaults and flag clearly in CONTEXT.md as unconfirmed/placeholder.

## Tasks

### 1. Convert VIG/STR/DEX/INT to Leveled Skills
- Modify `CharacterStatsSO.cs` / `StatsComponent.cs` (from Phase 2) so each of the 4 stats has its own level (1-99) and XP value, using the same XP curve system as the 7 gathering/crafting skills (Section 5/Phase 5 — reuse that leveling logic rather than duplicating it, if it's already generic enough)
- Damage formulas (Phase 2's `DamageCalculator.cs`) should now scale off these stat LEVELS, not flat starting values

### 2. Weapon-to-Stat Mapping
- Default (use unless RunePortal source confirms otherwise):
  - **STR-style:** Sword, Axe, Mace
  - **DEX-style:** Spear, Bow, Crossbow
  - **INT-style:** Staff, Wand
- This should be a property on `WeaponType` or a lookup table — confirm exact weapon list matches whatever was resolved in Phase 3

### 3. Damage-Based XP Awarding
- When the player deals damage via `PlayerCombat.cs`, determine which stat's weapon-style was used (Task 2 mapping) and award that stat XP proportional to damage dealt (exact multiplier is tunable — start with a simple 1:1 or reasonable scaled ratio, document the chosen value in CONTEXT.md)
- **VIG passive XP:** whenever STR/DEX/INT XP is awarded from a hit, also award VIG XP equal to ~33% of that same amount (default ratio, mirrors OSRS Hitpoints — confirm/adjust if RunePortal source has a different ratio)
- Players can hybrid/tribrid — if they alternate weapon types, each style's stat trains independently and simultaneously, no penalty for mixing

### 4. Character Level Calculation
- New formula (in a sensible location — `CharacterStatsSO.cs` or a dedicated `CharacterLevelCalculator.cs`): Character Level = average of VIG/STR/DEX/INT levels, rounded (default — confirm against RunePortal source if a different formula existed there, e.g. OSRS-style combat level weighting)
- This is what displays as "Level" in the HUD (Phase 3b's top-left panel), separate from the individual per-stat levels shown below it

### 5. HUD Update (Phase 3b's Top-Left Panel)
- Reorder the stat display: **VIG, STR, DEX, INT** (top to bottom) — currently may be in a different order, fix to match
- Each stat shows its own level (not just a flat value) — e.g. "VIG 12", "STR 8", "DEX 15", "INT 3"
- The Character Level (Task 4) displays prominently above or alongside the per-stat list, with its own XP bar showing progress toward the next Character Level
- Confirm this doesn't conflict with the existing HP bar (HP is now derived from VIG's level, not a separate flat stat — make sure Phase 2's `Health.cs` reads max HP from VIG level correctly)

### 6. Self-Test Before Reporting Complete (per standing protocol)
Using Unity MCP editor control tools:
1. `git pull origin main` first
2. Enter Play Mode programmatically
3. Attack the test enemy with a STR-style weapon equipped, confirm STR XP increases and VIG XP also increases (at the reduced ratio)
4. If possible, switch to a DEX-style or INT-style weapon (Phase 3's equip system) and confirm that stat trains instead, proving hybrid/tribrid training works
5. Confirm Character Level updates correctly as the 4 stats level up
6. Confirm HUD displays stats in the correct order (VIG, STR, DEX, INT) with correct individual levels
7. Check Console for errors throughout
8. Exit Play Mode
9. Only report complete if all of the above passes

### 7. Commit & Update CONTEXT.md
- Commit as: `[Phase 5b] Combat stat overhaul - leveled VIG/STR/DEX/INT, damage-based XP, Character Level`
- Update CONTEXT.md: log whether RunePortal source was accessible and which values came from it vs defaults, log the exact VIG XP ratio and Character Level formula used, confirm self-test passed

## Do NOT
- Do not change the 7 gathering/crafting skills (Fishing, Gathering, Mining, Smithing, Crafting, Cooking, Alchemy) — those are unaffected by this amendment, this is combat stats only
- Do not break existing gear stat modifiers from Phase 3 — equipped gear should still apply STR/DEX/VIG/INT bonuses on top of the leveled base values
- Do not guess at the weapon-to-stat mapping or XP ratios without first checking RunePortal source — use defaults only if source isn't accessible, and flag clearly either way
