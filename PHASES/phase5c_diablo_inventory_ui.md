# Phase 5c — Equipment & Inventory UI (Reference-Matched)

**Read `CONTEXT.md` first (git pull per Critical Rule #0). Replaces Phase 3b's separate Equipment/Backpack panels. Dev Tools panel stays separate and unchanged.**

**This replaces the previous Phase 5c plan (combined single-screen "stats on left, inventory on bottom" layout) — Jordon provided a concrete visual reference and wants THIS layout instead. Follow the reference description below precisely, not a generic "Diablo-style" interpretation.**

## Reference Image Description
Jordon shared a screenshot of a polished ARPG inventory UI (likely a Unity asset/template example) as the exact visual target. Since you can't see the image, here's a precise breakdown:

**Top-left corner (small, always visible, separate from the two main panels below):**
- A circular/square player portrait icon
- "PLAYER" name label next to it
- A horizontal HP bar beneath the name (green fill, shows "100/100" style text)

**Left panel — "EQUIPMENT" (title bar with X close button, top of panel):**
- Two vertical columns of equipment slot icons flanking a center stat readout, like a paperdoll silhouette without an actual 3D model in the panel itself
- **Left column** (top to bottom): ~5 slot icons stacked — head slot, hand/glove slot, two more slots, foot slot — each slot is a square icon with a colored border matching item rarity (the reference shows green and orange borders on filled slots, gray on empty)
- **Right column** (top to bottom): ~5 more slot icons mirroring the left side — head/cape slot, body/chest slot, neck/shoulder slot, another slot, ring/accessory slot — same colored-border-by-rarity treatment
- **Center, between the two columns:** plain text stat readout, one stat per line: "Damage", "Armor", "Strength", "Dexterity", "Intelligence" (numbers next to each)
- **Bottom-center of this panel:** 2 horizontally-placed slots for Weapon and Shield (main-hand/off-hand), visually separated from the two side columns above them
- Small chevron arrow on the outer-left edge of this panel (likely for paging — not critical, can omit if it adds unnecessary complexity)

**Right panel — "INVENTORY" (separate floating panel, title bar with X close button):**
- A grid of inventory slots (roughly 6 columns visible, multiple rows, scrollable) — empty slots are plain dark squares
- Filled slots show an item icon; if the item is stackable, a small quantity number badge appears in the bottom-right corner of that slot (reference shows things like a drumstick ×8, a red gem/potion ×3, an apple ×2)
- A scrollbar on the right edge of the grid for additional rows
- **Bottom of this panel:** a gold coin icon with the current currency total next to it (reference shows "27")
- Small chevron arrow on the outer-right edge (same paging note as above — optional)

**Center background (behind/between both panels):** A 3D character preview showing currently equipped gear. This is a nice-to-have, not required for this phase if it adds significant complexity — flag it as deferred if so, the two functional panels matter more than the live character preview.

## Slot Mapping (adapt reference to our exact 11 slots)
Reference shows ~12 slots total (5 left + 5 right + 2 bottom); we have exactly 11: **Weapon, Shield, Head, Cape, Neck, Body, Legs, Hands, Feet, Ring, Ammo**. Distribute these across the two-column-plus-bottom-dock structure sensibly — exact arrangement is your call, the STRUCTURE (two flanking columns + stat readout center + weapon/shield dock at bottom) is what needs to match, not an exact 1:1 icon-for-icon copy.

## Tasks

### 1. Top-Left Player Info Bar
- New small UI element: player portrait icon (placeholder is fine — simple circular icon), "PLAYER" name label (or pull actual player name if one exists), HP bar with current/max text
- This can coexist with or replace part of the existing Phase 3b/5b HUD stats panel — your call on exact integration, document in CONTEXT.md, but don't lose the Character Level/XP info from Phase 5b, it needs a home somewhere (could live in the Equipment panel's stat readout area instead, see Task 3)

### 2. Equipment Panel — Two-Column Layout
- New/replacement panel: `Assets/Prefabs/UI/EquipmentPanel.prefab`
- Title bar "EQUIPMENT" with close (X) button
- Two vertical columns of slot icons (per the slot mapping above), each showing equipped item with rarity-colored border (Phase 3's rarity visual system — reuse those colors for border tinting here)
- Empty slots show a neutral gray/placeholder border

### 3. Equipment Panel — Center Stat Readout
- Between the two columns: plain text list showing current derived combat stats — Damage, Armor/Defense (new derived value, can be a simple formula off VIG/gear for now), and the 4 leveled stats from Phase 5b: Strength, Dexterity, Intelligence, Vigor
- These should reflect REAL current values (base stat level + equipped gear bonuses), not static placeholders
- Character Level can also live here (e.g. small header above the stat list) since this panel is now the natural home for detailed character info

### 4. Equipment Panel — Weapon/Shield Dock
- Bottom-center of the Equipment panel: 2 slots specifically for Weapon and Shield, visually separated from the two side columns (matches reference layout)

### 5. Inventory Panel — Grid Layout
- New/replacement panel: `Assets/Prefabs/UI/InventoryPanel.prefab` (separate floating panel from Equipment, per reference — these are two distinct windows, not one combined screen)
- Title bar "INVENTORY" with close (X) button
- Grid layout (not a vertical list like the old Phase 3b version) — reasonable column count (5-6) with scrollable rows
- Each filled slot shows item icon with rarity-colored border; stackable items show a quantity badge (small number, bottom-right corner of the icon)
- Respect the `MaxCarryCapacity` concept from the previous Phase 5c plan — show total slots used vs max somewhere on this panel (reference doesn't show this explicitly but it's still needed per Jordon's earlier request — add it near the title bar or bottom, your call)

### 6. Inventory Panel — Currency Display
- Bottom of the Inventory panel: gold coin icon + current Gold total (from Phase 4's currency system)
- If Void Shards should also display, add a second small currency readout near it — your call on exact placement, document in CONTEXT.md

### 7. Item Interaction
- Tapping any item (in either panel) shows its stats/affixes (reuse Phase 3b's existing detail-view logic) — exact placement of this detail view is your call (could be a tooltip, could be a side panel) since the reference image doesn't show this interaction state clearly
- Equipping from Inventory and unequipping from Equipment both work as before (Phase 3's underlying logic unchanged, just the visual presentation is new)

### 8. Character Preview (optional, time-permitting)
- If feasible without major scope creep: a simple camera angle showing the player's equipped 3D model in the space between/behind the two panels
- If this adds significant complexity, SKIP IT and flag clearly in CONTEXT.md as deferred — the two functional panels are the priority for this phase

### 9. Self-Test Before Reporting Complete (per standing protocol)
Using Unity MCP editor control tools:
1. `git pull origin main` first
2. Enter Play Mode programmatically
3. Open Equipment panel, confirm two-column slot layout, center stat readout with real values, and weapon/shield dock all display correctly
4. Open Inventory panel, confirm grid layout, stack count badges on stackable items, and currency display all work
5. Tap an item in Inventory, equip it, confirm it now shows correctly in the Equipment panel's appropriate slot AND the inventory grid count updates
6. Check Console for errors throughout
7. Exit Play Mode
8. Only report complete if all of the above passes

### 10. Commit & Update CONTEXT.md
- Commit as: `[Phase 5c] Equipment & Inventory UI - reference-matched two-panel layout`
- Update CONTEXT.md: note whether character preview (Task 8) was implemented or deferred, confirm self-test passed

## Do NOT
- Do not build this as one single combined screen — the reference shows TWO separate panels (Equipment, Inventory), keep them as distinct windows
- Do not redesign the Dev Tools panel — leave it as-is
- Do not skip the rarity-colored slot borders — this is a key visual signal from the reference and from our existing Phase 3 rarity system
- Do not over-invest in the character preview (Task 8) if it threatens the rest of the phase — it's explicitly optional
