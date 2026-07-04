# Phase 6 — Homestead Full Build-out

**Read `CONTEXT.md` first (git pull per Critical Rule #0). Builds on Phase 5's Interactable/CraftingStation patterns and Phase 5c's panel UI — extend, don't refactor.**

## Goal
Turn the 7 stubbed Homestead structures (Watchtower, Merchant, Shrine, Pool of Refreshment, Fast Travel Portal, Storage Chest, 3 Guilds) into functional systems, so the Homestead is a real hub: sell loot, bank items, buy supplies, heal/buff between runs, and train combat stats. Every building's behavior is data-driven per Critical Rule 3 — adding a shop item or pool tier later is a data change, not code.

**Scope note:** the GDD Section 6/roadmap says "9 buildings" — that's stale; the resolved Phase 0 list is 12 (Campfire, Forge, Shrine, Garden, Watchtower, Merchant, 3 Guilds, Pool of Refreshment, Fast Travel Portal, Storage Chest). Correct the GDD as part of Task 1. Forge/Campfire/Garden are already functional from Phase 5 — do not touch them beyond visual polish.

## Tasks

### 1. Resolve Open Behaviors from RunePortal Source
Same pattern as Phase 5 Task 1 — check RunePortal source for the definitive behavior of: Shrine, Watchtower, Pool tier effects/costs, and exactly how Guild stat training worked (cost model, XP amounts, whether VIG is trainable and where). If source isn't accessible, use the fallback designs below and flag them clearly in CONTEXT.md as unconfirmed. Update `Void_Bound_GDD.md` Section 6 with the resolved behaviors AND the 9→12 building count correction.

### 2. Merchant — Buy/Sell Shop
- Add `goldValue` field to `GearItemSO` and `MaterialItemSO` (extend, default 0 = unsellable)
- `ShopInventorySO` (`Scripts/Data/`): list of purchasable items (gear or material) with prices; one asset for the Homestead merchant
- `MerchantStation` (`Scripts/Homestead/`): Interactable subclass on the Merchant building → opens Shop UI
- Shop UI: two tabs or two lists — BUY (from ShopInventorySO, disabled if unaffordable) and SELL (player backpack items with a sell price, e.g. 40% of goldValue — tunable constant, flag it). Uses `PlayerCurrency.SpendGold`/`AddGold`. Match Phase 5c panel visual language (colors/header/X-close from `Phase5cUIBuilder` palette).
- Test content: merchant sells 2-3 basic items (e.g. a Common weapon, some cooking materials); all existing test gear gets sensible goldValues

### 3. Storage Chest — Bank
- `PlayerStorage` (`Scripts/Homestead/` or `Scripts/Inventory/`): separate item list (48 slots), Deposit/Withdraw API mirroring PlayerInventory's add/remove, `OnStorageChanged` event
- `StorageStation`: Interactable on the chest → opens Bank UI
- Bank UI: two grids side by side (storage + backpack) reusing the Phase 5c inventory grid/stack-badge approach; tap item → deposit or withdraw
- Respect backpack's 24-slot capacity concept on withdraw

### 4. Pool of Refreshment — Heal + Buff Station
- Interactable → immediately heals player to full HP and applies a timed buff (fallback design: +2 all stats for 120s), then goes on cooldown (fallback: 60s) with a visible "not ready" state
- **Buff mechanism (new, minimal):** `TimedBuff` component on the player that applies a `CharacterStats` bonus via the existing `StatsComponent.AddGearBonus`/`RemoveGearBonus` API on start/expiry — reuses the proven stat pipeline, no StatsComponent changes
- 4 upgrade tiers per GDD: implement tier 1 functionally; store tier definitions (heal %, buff strength/duration, cooldown, upgrade cost) in a `PoolTierSO` or serialized array so tiers 2-4 are data + an upgrade-purchase hook (upgrade purchasing itself can be stubbed with a clear TODO if it drags — flag in CONTEXT.md)

### 5. Shrine — Blessing (fallback design, confirm vs source)
- Interactable → spend gold offering (e.g. 25g) → receive a timed combat buff distinct from the Pool's (e.g. +10% damage for 180s), with its own cooldown
- Reuses the same `TimedBuff` component from Task 4 — if this creates buff-stacking questions, simplest rule: Pool and Shrine buffs stack with each other, re-activating one refreshes it (document the rule in CONTEXT.md)

### 6. Guilds — Combat Stat Training (Warriors'/Rangers'/Mages')
- Per Phase 5 finding: Guilds = stat training. Confirm cost/XP model from source; fallback: pay gold (scaling with current level, e.g. 10g × current stat level) → gain a fixed chunk of combat XP — Warriors' → CombatSTR, Rangers' → CombatDEX, Mages' → CombatINT, via the existing `PlayerSkills.AddXP`
- VIG: confirm from source where VIG trains; fallback: each guild session also grants 50% VIG XP, mirroring the combat formula in `CombatXPCalculator`
- One shared Training UI (station name, stat, current level/XP, cost, Train button) driven by a per-guild config (`GuildDefinitionSO` or serialized fields) — one script, three configured instances
- This is a deliberate gold sink — flag all numbers as tunable in CONTEXT.md

### 7. Fast Travel Portal — UI Stub Only
- Interactable → opens a destination list panel: Homestead (current), Ashfields (locked), Bleakwood (locked) — locked entries grayed with "Coming soon"
- NO actual scene loading this phase — real travel lands with the zone phases. Keep the panel so the interaction pattern and UI exist.

### 8. Watchtower — Confirm or Defer
- Check RunePortal source for what the Watchtower did. If it maps to a zone/wave/scouting system that doesn't exist yet, leave it as a visual stub with an Interactable that shows a one-line "The wastes are quiet." flavor popup, and flag its real function for the appropriate future phase. Do not invent a system for it.

### 9. Low-Poly Art Pass (time-boxed)
- Replace any remaining primitive/placeholder building meshes with proper low-poly Blender MCP models (existing FBX pipeline: `bake_space_transform=True`, verify (0,0,0) rotation on import)
- Time-box this — functional buildings matter more than pretty ones. If Blender MCP is unavailable or this drags, defer with a flag in CONTEXT.md.

### 10. HUD Touch-ups (rides along per standing flag)
- Fix the legacy StatsPanel legibility issue flagged in the Phase 5c log (text not readable in Game view) — this phase adds buff states worth showing, so the HUD is being touched anyway
- Add a minimal active-buff indicator near the Player Info Bar (icon/label + remaining seconds is enough; ASCII labels per the standing icon-rendering rule)

### 11. Self-Test Before Reporting Complete (per standing protocol)
Using Unity MCP editor control tools (now connected for Claude Code), following VISUAL_VERIFICATION_PROTOCOL.md for all new UI:
1. `git pull origin main` first
2. Enter Play Mode; give test gear + gold via DevTools as needed
3. Merchant: sell an item (gold increases, item leaves backpack), buy an item (gold decreases, item arrives)
4. Storage: deposit an item, confirm it's in storage and out of backpack; withdraw it back
5. Pool: use it, confirm full heal + buff applied (stat readout in Equipment panel reflects it) and cooldown blocks immediate reuse; confirm buff expires and stats revert
6. Shrine: offering deducts gold, buff applies and expires correctly
7. Guild: train a stat, confirm gold deducted and XP/level increases (visible in Equipment panel readout)
8. Portal: panel opens, locked destinations grayed
9. Screenshot each new panel at Fixed 1920x1080 and check against the Phase 5c visual language point by point
10. Console clean throughout; exit Play Mode
11. Only report complete if all of the above passes

### 12. Commit & Update CONTEXT.md
- Commit as: `[Phase 6] Homestead full build-out - merchant, bank, pool, shrine, guilds`
- Update CONTEXT.md: Phase 6 log (per-building resolution vs fallback, all tunable numbers, anything deferred), point Current Phase at the next phase (zones — Ashfields — unless directed otherwise)

## Do NOT
- Do not refactor Phase 5's CraftingStation/Interactable or Phase 5c's panels — new stations follow the same patterns
- Do not implement actual scene travel for the Portal — UI stub only
- Do not invent a Watchtower system — confirm from source or defer
- Do not balance the economy — pick sensible placeholder numbers, mark ALL of them tunable in CONTEXT.md
- Do not let the art pass (Task 9) eat the phase — functionality first, defer art with a flag if needed
- Do not build a general buff/status-effect framework — one `TimedBuff` component reusing the gear-bonus API is the ceiling for this phase
