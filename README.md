# Void Bound

A dark-fantasy, low-poly action RPG built in **Unity 6.5 (URP)** — mobile-first, desktop-tested. Fixed isometric camera, automatic proximity combat, deep gear/skilling systems, and a Homestead hub you return to between zones. Evolved from *RunePortal* (a Three.js browser ARPG); the systems carry over, the visuals are rebuilt native.

> **New here?** Read [`CONTEXT.md`](CONTEXT.md) first — it's the living project rulebook (state, decisions, per-phase log). The design spec is [`Void_Bound_GDD.md`](Void_Bound_GDD.md).

---

## Current state

- **Phase 7 complete** (cross-zone travel + persistence). Phases 0–7 built and play-verified.
- **Zones:** Homestead (hub), Ashfields (Zone 2). Bleakwood (Zone 3 / Phase 8) not started.
- **Combat:** automatic, proximity-based. **Melee** strikes in reach; **ranged** (bow/crossbow) and **mage** (staff/wand) engage at a distance with **homing projectiles** (arrows / bolts of magic) and dedicated **Shoot / Cast** animations.
- **Gear:** class loadouts — **Melee** (plate + sword/shield), **Ranger** (leather hood/vest + bow), **Mage** (the Archmage set below). Rigged, per-bone equipment that moves with the animated skeleton.
- **Characters:** rigged Hero + Goblin (shared 11-bone skeleton), 7 baked clips each (Idle, Walk, Attack, Shoot, Cast, Hit, Death).

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
