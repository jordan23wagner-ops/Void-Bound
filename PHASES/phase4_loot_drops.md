# Phase 4 — Loot & Drop Tables

**Read `CONTEXT.md` first (git pull per Critical Rule #0). Builds on Phase 2 (combat/enemy death) and Phase 3 (gear/inventory) — extend, don't refactor.**

## Goal
Enemies drop loot on death — gear (rarity-weighted by enemy tier) and currency (Gold + Void Shards) — using the existing Interactable system from Phase 2 for pickup, with rarity visual effects from Phase 3 applied to dropped items in the world.

## Tasks

### 1. Complete LootTableSO
Flesh out the stub from Phase 0 (`Scripts/Data/LootTableSO.cs`):
- Rarity drop-chance weights, scaling by `EnemyTier` (higher tier = better odds at higher rarities)
- Reference to a pool of possible `GearItemSO` drops (can reuse Phase 3's 4 test items for now — full item variety comes later)
- Gold drop range (min/max)
- Void Shard drop range (min/max) — should be rare/small amounts from lower tiers, more common from higher tiers
- Include a `zoneModifier` field (float multiplier affecting rarity odds) even though we only have one test scene right now — this is architected for Phase 7 (Ashfields) when real zone-based modifiers come into play. Don't try to fully test zone-modifier behavior this phase, just make sure the field exists and is read by the roll logic.

### 2. Expand Enemy Tier Test Coverage
Currently only one Weak-tier TestEnemy exists. To meaningfully test tier-scaled loot:
- Create 2 additional `EnemyDefinitionSO` instances: one **Standard** tier, one **Elite** tier
- Reuse the existing placeholder enemy model (no new Blender generation needed) — differentiate visually with a simple color tint per tier if easy to do, otherwise stats-only difference is fine for this phase
- Each should reference an appropriate `LootTableSO` (or the same table with tier-based weighting handling the difference — your call on architecture, document in CONTEXT.md)

### 3. Loot Drop on Death
- Extend `EnemyController.cs`'s Dead state (from Phase 2): on death, roll the loot table and spawn results
- Currency (Gold/Void Shards) can be added directly to a player currency total (no pickup object needed — instant)
- Gear drops should spawn as a **world pickup object** at the enemy's death position (not auto-added to inventory) — this uses Phase 2's Interactable system: tap the dropped item OR walk into/near it to pick it up

### 4. Loot Pickup Visual
- Dropped gear pickups should show the same rarity visual treatment from Phase 3 (`RarityVisualEffects`) — a Common drop looks plain, a Legendary+ drop has the particle accent, so item quality is readable on the ground before even picking it up
- Simple representation is fine (doesn't need the full equipped-item model — a glowing icon, gem, or simplified shape works) — your call, document the choice

### 5. Currency System & Display
- New simple component/script: `Scripts/Inventory/PlayerCurrency.cs` (or extend `PlayerInventory.cs` if that's cleaner — your call)
- Tracks Gold and Void Shards totals
- Add a small currency display to the HUD (Phase 3b's top-left stats panel is the natural home — Gold and Void Shard counts with simple icons)

### 6. Pickup Confirmation Feedback
- When a gear item or currency is picked up, show simple feedback (reuse the `FloatingDamageNumber`-style pattern from Phase 2 if convenient — e.g. "+15 Gold" or item name briefly appearing)

### 7. Self-Test Before Reporting Complete (per standing protocol)
Using Unity MCP editor control tools:
1. `git pull origin main` first
2. Enter Play Mode programmatically
3. Kill the Weak-tier enemy, confirm loot drops appear (gear pickup in world + currency added)
4. Kill the Standard and Elite tier enemies, confirm loot quality trends better at higher tiers (won't be guaranteed every single roll since it's weighted, but run enough kills to see the trend, or temporarily force a 100% legendary roll just to confirm the spawning/pickup mechanics work end-to-end)
5. Walk into / tap a dropped gear item, confirm it's added to inventory and removed from the world
6. Confirm currency display in HUD updates correctly on pickup
7. Check Console for errors throughout
8. Exit Play Mode
9. Only report Phase 4 complete if all of the above passes

### 8. Commit & Update CONTEXT.md
- Commit as: `[Phase 4] Loot & drop tables - tier-weighted rarity, currency, pickup system`
- Update CONTEXT.md: log the loot table architecture decision (per-enemy vs shared table), confirm self-test passed, point "Current Phase" to Phase 5

## Do NOT
- Do not fully test zone-modifier behavior — just architect the field, real testing comes in Phase 7
- Do not generate new 3D models for Standard/Elite test enemies — reuse the placeholder, tint if easy
- Do not build the full 8-tier enemy roster — 3 test tiers (Weak/Standard/Elite) is enough to validate the loot scaling logic
- Do not polish pickup visuals/animations beyond functional — basic glow/particle per rarity is sufficient for now
