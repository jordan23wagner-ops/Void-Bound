# Void Bound

A dark-fantasy, low-poly action RPG built in **Unity 6.5 (URP)** — mobile-first, desktop-tested. Fixed isometric camera, automatic proximity combat, deep gear/skilling systems, and a Homestead hub you return to between zones. Evolved from *RunePortal* (a Three.js browser ARPG); the systems carry over, the visuals are rebuilt native.

> **New here?** Read [`CONTEXT.md`](CONTEXT.md) first — it's the living project rulebook (state, decisions, per-phase log). The design spec is [`Void_Bound_GDD.md`](Void_Bound_GDD.md).

---

## Current state

Phases 0–7 built and play-verified (cross-zone travel + persistence). Since then, the full Homestead station set, the death & recovery loop, a save system, and supporting systems have been added — all committed and play-verified.

- **Zones:** Homestead (hub), Ashfields (Zone 2). Bleakwood (Zone 3) stubbed as a Portal destination.
- **Combat:** automatic, proximity-based. **Melee** strikes in reach; **ranged** (bow/crossbow) and **mage** (staff/wand) engage at a distance with **homing projectiles** and dedicated **Shoot / Cast** animations. **Poison** — some enemies inflict a damage-over-time; the **Antidote** cures it; shown as a red HUD status pill.
- **Death & recovery (§4A):** on death you keep your **3 most valuable items** (by `goldValue`); everything else — gear, materials, unbanked gold/Void Shards — drops to a single **gravestone** you fight back to. Die again before recovering it and the grave is abandoned: its **untradables** become buyable-back at the **Reclaimer** for a gold fee, its tradables are gone. A live **"kept on death" preview** (in the Fast Travel panel) and a **"YOU DIED" screen** surface the stakes.
- **Homestead stations:** Merchant, Storage/Bank, Forge, Garden, the three Guilds, Shrine, **Pool of Refreshment** (heal + timed all-stat buff, with **buyable tier upgrades**), Fast Travel Portal, **Watchtower** (zone-scouting board: danger, recommended level vs. yours, intel), **Crafting Bench**, **Enchanted Chest** (untradable upgrades), and the **Reclaimer**. Spread wide across the map so the town breathes.
- **Save system:** single-slot JSON persistence of core progression — currency, materials, inventory/equipped (with untradable upgrade tiers), bank, combat XP/levels, tool tiers, and Pool tier. Autosaves on quit + zone travel, loads on boot; item ids resolve through a baked **ItemRegistry** (`VoidBound → Bake Item Registry`). A **New Game** dev button wipes progress + deletes the save.
- **Gear:** class loadouts — **Melee** (plate + sword/shield), **Ranger** (leather hood/vest + bow), **Mage** (the Archmage set below). Rigged, per-bone equipment that moves with the animated skeleton.
- **Characters:** rigged Hero + Goblin (shared 11-bone skeleton), 7 baked clips each (Idle, Walk, Attack, Shoot, Cast, Hit, Death).

**In progress — gathering overhaul:** converting resource nodes from walk-over auto-harvest to deliberate, **tool-required** actions (fishing + woodcutting at the Homestead, mining in zones), with tool-hold and gather animations. Materials/ore move out of town into zones.

## Visual style & polish — the bar

The **Archmage set** (deep-purple robe, gold trim, glowing gems, drooping wizard hat) is the **quality bar** for everything going forward — gear, enemies, the Homestead, and all UI. See **[`STYLE_GUIDE.md`](STYLE_GUIDE.md)** for the concrete standard and how to hit it.

In one line: *layered low-poly silhouettes with a named-material palette (rarity color + gold trim + glowing gems + dark accent), rigged per-bone so it moves with the character.*

## Building & running

- Open the project in **Unity 6.5 (6000.5.0f1)** with URP.
- **Always enter Play from the Homestead scene** (Ashfields/Bleakwood have no camera of their own — the Player/Camera/HUD persist from Homestead). The editor is configured to force this via `VoidBound → Dev → Play From Homestead`.
- Dev conveniences (editor-only, `VoidBound` menu): auto-equip a kit on Play, and one-click **Equip Melee / Ranged / Mage Kit**.

## Content pipeline

Art is **procedurally generated, low-poly, headless Blender** — no hand-modeling. Scripts in [`Tools/`](Tools/) build the meshes from primitives and export FBX:

| Tool | Builds |
|---|---|
| `build_character_models.py` | Rigged Hero + Goblin + animation clips |
| `build_equipment_models.py` | All weapon, armor, and item models (per-bone rigged) |
| `build_homestead_buildings.py` | Homestead station meshes |

Run headless, e.g.: `blender.exe -b -P Tools/build_equipment_models.py`. After a bake, in Unity: reimport, then re-run the relevant `VoidBound/…` setup menu items (see `CONTEXT.md`).

## Docs

| File | Purpose |
|---|---|
| [`CONTEXT.md`](CONTEXT.md) | Living rulebook — read first every session; per-phase build log |
| [`STYLE_GUIDE.md`](STYLE_GUIDE.md) | **Visual polish standard** (the Archmage bar) for gear, enemies, Homestead, UI |
| [`Void_Bound_GDD.md`](Void_Bound_GDD.md) | Full design spec — systems, stats, zones |
| [`CODING_STANDARDS.md`](CODING_STANDARDS.md) | Code + FBX export conventions, self-test protocol |
| [`VISUAL_VERIFICATION_PROTOCOL.md`](VISUAL_VERIFICATION_PROTOCOL.md) | Screenshot-based visual QA before calling UI/visual work done |
| [`PHASES/`](PHASES/) | Per-phase task breakdowns |
