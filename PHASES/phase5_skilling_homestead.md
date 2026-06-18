# Phase 5 — Skilling Systems & Homestead Hub

**Read `CONTEXT.md` first (git pull per Critical Rule #0). Builds on Phase 4's Interactable/pickup patterns — extend, don't refactor.**

## Goal
Prove the skilling + crafting architecture works end-to-end with a focused subset (not all 7 skills fully fleshed out, not all 36 RunePortal recipes) — the Homestead scene gets its 12 real buildings as interactable structures, at least 2-3 skills have a working gather→craft loop, and the system is architected so expanding content later is just adding data (more `SkillDefinitionSO`/`RecipeDefinitionSO` assets), not new code.

## Tasks

### 1. Resolve Open Item: Skill-to-Building Mapping
- Check RunePortal source (same pattern as Phase 0/3 — read `CONTEXT.md` or `runeportal_phase5.html` in the old project if accessible) for the definitive mapping of which Homestead building serves which skill/purpose
- We know from memory: Garden was RunePortal's potion-crafting location (Gathering + Alchemy). Confirm Forge = Smithing, Campfire = Cooking. Investigate whether the 3 Guild buildings (Warriors'/Rangers'/Mages') serve as Crafting stations split by gear archetype (melee/ranged/magic) — this would explain why there are three of them.
- Fishing and Mining may be field-based (resource nodes out in zones, not fixed Homestead buildings) rather than tied to a structure — confirm from source if possible.
- If RunePortal source isn't accessible or doesn't clarify, use this fallback mapping and flag it clearly in CONTEXT.md as unconfirmed:
  - Forge → Smithing
  - Campfire → Cooking
  - Garden → Gathering + Alchemy
  - Warriors'/Rangers'/Mages' Guild → Crafting (split by gear category)
  - Fishing, Mining → field-based resource nodes (not Homestead buildings)
- Update `Void_Bound_GDD.md` Section 6 with the resolved mapping.

### 2. Complete SkillDefinitionSO
Flesh out the stub from Phase 0 (`Scripts/Data/SkillDefinitionSO.cs`):
- Skill name (one of the 7: Fishing, Gathering, Mining, Smithing, Crafting, Cooking, Alchemy)
- XP curve (Level 99 cap, 3x multiplier — locked in Phase 0, still flagged tunable)
- List of unlockable `RecipeDefinitionSO` references, each with a minimum level requirement

### 3. Complete RecipeDefinitionSO
Flesh out the stub from Phase 0 (`Scripts/Data/RecipeDefinitionSO.cs`):
- Input items (reference existing `GearItemSO` or create simple new "material" item types if needed — raw materials don't need to be full gear items, consider a lighter `MaterialItemSO` if that's cleaner, your call, document in CONTEXT.md)
- Output item (what you get from crafting)
- Required skill + minimum level
- Required station/building (which Homestead structure this recipe needs)

### 4. Homestead Scene — Real Buildings
- Build out `Assets/Scenes/Homestead.unity` with all 12 structures from the resolved building list (Task 1), positioned reasonably in the world (doesn't need to be a polished layout yet — functional spacing is fine)
- Generate simple low-poly placeholder structures via Blender MCP for each (reuse the proven FBX export process from Phase 1/2 — Forward: -Z, Up: Y axis, verify (0,0,0) rotation on import every time)
- Each building that serves a skill should have an `Interactable` component (Phase 2 system) — tap or walk into it to open that skill's crafting UI (Task 6)
- Buildings that don't serve a skill yet (Watchtower, Merchant, Fast Travel Portal, Storage Chest) can be placed as simple structures without functionality — flag them as stubs for later phases in CONTEXT.md

### 5. Field Resource Nodes (if Fishing/Mining are field-based per Task 1)
- Simple resource node objects (e.g. a fishing spot, a mining rock) placed in the Homestead scene or a small test area
- Uses the Interactable system — tap or walk into a node to gather (adds raw material to inventory, grants skill XP)
- Nodes should respawn after a short cooldown (not one-time pickups like loot drops)

### 6. Basic Crafting/Gathering UI
- Simple panel (functional first pass, not polished) that opens when interacting with a skill station
- Shows: current skill level/XP, available recipes (locked ones grayed out if level requirement not met), required materials for selected recipe, a "Craft" button
- Crafting consumes input materials, grants output item to inventory, grants skill XP

### 7. Test Content (keep scope small)
Build just enough to prove the loop works end-to-end:
- **Gathering** (at Garden): 1-2 raw materials gatherable
- **Cooking** (at Campfire): 1 recipe using a gathered material → a simple food item (could grant a temporary stat buff, or just exist as a craftable item for now — your call)
- **Smithing** (at Forge): 1 recipe using a raw material → a basic gear item (ties back into Phase 3's gear system)
- This is intentionally minimal — full recipe content (RunePortal had 36) is a data-entry task for later, not part of this phase

### 8. Self-Test Before Reporting Complete (per standing protocol)
Using Unity MCP editor control tools:
1. `git pull origin main` first
2. Enter Play Mode programmatically
3. Walk to a resource node, gather a raw material, confirm it's added to inventory and skill XP increases
4. Walk to Campfire, open Cooking UI, craft the test food recipe, confirm output item received and materials consumed
5. Walk to Forge, open Smithing UI, craft the test gear recipe, confirm the resulting gear item works correctly with Phase 3's equip system
6. Check Console for errors throughout
7. Exit Play Mode
8. Only report Phase 5 complete if all of the above passes

### 9. Commit & Update CONTEXT.md
- Commit as: `[Phase 5] Skilling systems & Homestead hub - 12 buildings, gather/craft loop`
- Update CONTEXT.md: log the resolved building mapping, flag any buildings left as stubs, confirm self-test passed, point "Current Phase" to Phase 6

## Do NOT
- Do not build all 7 skills fully — Gathering, Cooking, Smithing proven end-to-end is the bar for this phase
- Do not build all 36 RunePortal recipes — 2-3 test recipes total is sufficient to validate architecture
- Do not give every Homestead building full functionality — stub the ones not tied to our 3 test skills (Watchtower, Merchant, Fast Travel Portal, Storage Chest) and flag clearly
- Do not guess at the skill-to-building mapping without checking source first — flag clearly if using the fallback
