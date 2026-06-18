# Phase 3b — HUD & Menu System

**Read `CONTEXT.md` first (git pull per Critical Rule #0). Builds directly on Phase 3's gear/inventory logic — extend, don't refactor the underlying equip/unequip system, just expose it properly through real UI.**

## Goal
A persistent, mobile-first HUD with on-screen buttons (no keyboard-only access) for Backpack and Equipment menus, a proper Equipment screen showing item stats/affixes, a basic minimap, and a Dev Tools panel for faster QA — matching the layout structure from the original RunePortal (Three.js) HUD.

## HUD Layout Spec

```
┌─────────────────────────────────────┐
│ [Lvl/XP/Stats]           [Minimap]   │
│                          [Backpack]  │
│                          [Equipment] │
│                          [Dev Tools] │
│                                       │
│                                       │
│                                       │
│ [Joystick]                           │
└─────────────────────────────────────┘
```

- **Top-left:** Player level, XP bar, HP bar, core stats (STR/DEX/VIG/INT) readout
- **Top-right:** Minimap, with Backpack/Equipment/Dev Tools icon buttons stacked directly below it
- **Bottom-left:** Joystick (already exists from Phase 1 — do not modify, just confirm no overlap with new elements)

## Tasks

### 1. Persistent HUD Canvas
- New Canvas: `Assets/Prefabs/UI/HUD.prefab`, Screen Space - Overlay, always active during gameplay
- Use anchor presets correctly (top-left, top-right, bottom-left) so layout holds across different aspect ratios/resolutions — this matters for both mobile and desktop testing
- Touch targets minimum 44×44px per standard mobile UI guidelines (matches RunePortal's established mobile standard)

### 2. Top-Left: Level / XP / Stats Panel
- Level number display
- XP bar (current XP / XP to next level) — pull from the leveling system (if not built yet, stub with placeholder values and flag in CONTEXT.md)
- HP bar (screen-space, in addition to the existing Phase 2 world-space bar above the player — this one is always visible without needing to look at the character)
- Compact STR/DEX/VIG/INT readout (small text or icons, doesn't need to be elaborate)

### 3. Top-Right: Minimap
- New script: `Scripts/UI/Minimap.cs`
- Simple approach: secondary camera positioned above the player looking straight down (orthographic), rendering to a `RenderTexture`, displayed in a small circular or square UI panel
- Minimap camera follows player position (X/Z only, fixed height above)
- Basic version is fine — no fog of war, zone names, or icons yet (that's later polish)

### 4. Backpack / Equipment / Dev Tools Buttons
- Three icon buttons stacked directly below the minimap, each 44×44px minimum
- **Backpack button** — toggles the Backpack panel (Task 6)
- **Equipment button** — toggles the Equipment panel (Task 5)
- **Dev Tools button** — toggles the Dev Tools panel (Task 7)
- Tab key can remain as a secondary/desktop shortcut for Equipment if convenient, but on-screen buttons are the primary access method (mobile-first)

### 5. Equipment Menu (full panel, not a stub)
- Shows all 11 equipment slots laid out clearly (Weapon, Shield, Head, Cape, Neck, Body, Legs, Hands, Feet, Ring, Ammo)
- Each slot shows the equipped item's icon with a rarity-colored border (matching Phase 3's rarity visual tiers — Common gray, Rare cyan-ish, Legendary+ gold-ish, etc.)
- **Tapping/clicking a slot** opens a detail view showing:
  - Item name
  - Rarity (with color)
  - Slot/weapon type
  - Stat modifiers (STR/DEX/VIG/INT bonuses — i.e. the "affixes")
  - Set name if part of a set (just display the `setId` for now, full set bonus text can come later)
  - An "Unequip" button
- Empty slots show a placeholder icon and are tappable to open the Backpack filtered to compatible items (nice-to-have — if too complex for this phase, just make empty slots show clearly as empty and flag the filtering as a future refinement)

### 6. Backpack Menu (full panel, not a stub)
- Scrollable list/grid of unequipped inventory items
- Each item icon shows rarity-colored border
- Tapping/clicking an item shows the same stat/affix detail view as Task 5, with an "Equip" button instead of "Unequip"
- Equipping from here uses the existing Phase 3 `PlayerInventory.EquipItem()` logic — no duplicate logic, just call into it

### 7. Dev Tools Panel
- Simple testing utility panel, hidden behind its own toggle button (not visible to a normal player, but accessible during this dev phase)
- Minimum useful set of buttons:
  - "Give Test Gear" (spawns one of each rarity tier from Phase 3's test items directly into inventory)
  - "Add 100 XP" (or whatever increment is useful once leveling exists)
  - "Kill All Enemies" (for quick combat loop testing)
  - "Toggle God Mode" (player takes no damage — useful for testing without dying constantly)
- Keep this simple — it's a QA convenience tool, not a polished feature

### 8. Mobile-First, Desktop-Testable
- Build and test primarily against mobile aspect ratios/touch input first
- Confirm the same layout doesn't break when tested in Unity's Game view at typical desktop/laptop resolutions (16:9, mouse click instead of touch should work identically since Unity's Input System UI module handles both)

### 9. Self-Test Before Reporting Complete (per standing protocol)
Using Unity MCP editor control tools:
1. `git pull origin main` first
2. Enter Play Mode programmatically
3. Confirm HUD elements render in correct positions (top-left stats, top-right minimap + buttons, bottom-left joystick) with no overlap
4. Open Equipment panel via button (not Tab), confirm all 11 slots display, confirm tapping a slot shows stat/affix detail correctly
5. Open Backpack panel via button, confirm items list correctly with rarity colors, confirm equipping from Backpack works and reflects in Equipment panel
6. Open Dev Tools panel, test at least one utility (e.g. Give Test Gear) and confirm it works
7. Check Console for errors throughout
8. Exit Play Mode
9. Only report complete if all of the above passes

### 10. Commit & Update CONTEXT.md
- Commit as: `[Phase 3b] HUD & menu system - equipment/backpack panels, minimap, dev tools`
- Update CONTEXT.md: note any stubbed systems (e.g. if XP/leveling doesn't exist yet and was placeholder-valued), confirm self-test passed, point "Current Phase" to Phase 4

## Do NOT
- Do not build the full leveling/XP system if it doesn't already exist — stub the bar with placeholder values and flag clearly, real leveling logic comes with combat/progression refinement later
- Do not build zone names, fog of war, or icons on the minimap yet — basic top-down view is sufficient
- Do not over-polish visually — functional, mobile-friendly, and correctly laid out is the bar for this phase. Full art pass is later.
- Do not remove the Tab key shortcut if it's already wired — just ensure on-screen buttons are the PRIMARY method, not the only method
