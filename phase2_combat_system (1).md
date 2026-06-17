# Phase 2 — Combat System (Auto-Attack + Interaction)

**Read `CONTEXT.md` before starting. Phases 0-1 are complete and committed — extend, don't refactor.**

**Design change from original plan:** No manual attack button. Combat is automatic (proximity-based), and all interactables (enemies, resource nodes, NPCs, objects) are activated by tapping them directly or by the player walking into/near them. This simplifies mobile UI — joystick is now the only persistent on-screen control.

## Goal
Player auto-attacks enemies that come within range while moving (no button press needed), and a general-purpose Interactable system lets other objects respond to tap or proximity/collision — laying groundwork for resource nodes, NPCs, and crafting stations in later phases.

## Tasks

### 1. Character Stats
- Create `Scripts/Data/CharacterStatsSO.cs` implementing the simplified RPG stat block: **STR, DEX, VIG, INT**
- Derive starting formulas (flag as tunable, not final):
  - Max HP = base value + (VIG × multiplier)
  - Physical damage = base weapon damage + (STR × multiplier)
  - Attack speed (time between auto-attacks) influenced by DEX — higher DEX = faster attack cooldown
  - INT reserved for magic damage (not used yet, no spells in Phase 2 — just include the field)
- Attach to Player as a component reading from a default `CharacterStatsSO` asset

### 2. Enemy Definition & Prefab
- Flesh out `EnemyDefinitionSO.cs` (stubbed in Phase 0): tier (use `EnemyTier` enum), base HP, base damage, aggro range, attack range
- Create ONE test enemy instance for Phase 2: **Weak tier**, simple stats (low HP, low damage)
- Generate a low-poly placeholder enemy model via Blender MCP
  - **Avoid repeating the Phase 1 FBX bug:** explicitly set Blender's FBX export axis to Forward: -Z, Up: Y before exporting. Verify on import that the GameObject's Transform Rotation reads (0,0,0) before attaching AI/physics components.
- Enemy prefab needs: Collider (trigger, for proximity detection), the EnemyController script (Task 4), URP/Lit material (avoid magenta)

### 3. Auto-Combat System (PlayerCombat.cs)
- New script: `Scripts/Combat/PlayerCombat.cs`
- **No input action for attacking.** Instead, continuously check for enemies within attack range (e.g. `Physics.OverlapSphere` each frame or on a short interval, OR a trigger collider on the player)
- When an enemy is in range: auto-attack on a cooldown timer (cooldown derived from DEX-based attack speed). Apply damage using the STR-derived formula from Task 1.
- If multiple enemies are in range, attack the nearest one first (simple targeting — full targeting priority can be refined later)
- Attacks stop automatically when no enemy is in range (player walks away)

### 4. EnemyController Script
- New script: `Scripts/Combat/EnemyController.cs`
- Simple state machine: **Idle → Aggro → Attack → Dead**
  - Idle: enemy stands still until player enters aggro range
  - Aggro: enemy moves toward player
  - Attack: when within attack range, deals damage to player on an interval (same cooldown-based pattern as player's auto-attack)
  - Dead: HP ≤ 0 → disable GameObject (full death state/animation is later polish)

### 5. General-Purpose Interactable System
This is new groundwork for Phase 5/6 (resource nodes, NPCs, crafting stations) — build the base now since combat needs it too.
- New script: `Scripts/Core/Interactable.cs` (base component or interface — your call, document the choice)
- Two trigger methods, both should call the same `OnInteract()` logic:
  1. **Tap/click detection**: raycast from screen touch/mouse position, if it hits an object with `Interactable`, trigger it
  2. **Proximity/collision detection**: trigger collider on the player (or on the interactable) fires `OnInteract()` when player walks into/near it
- Enemies can implement `Interactable` too if it simplifies things, OR stay fully separate from this system since combat already auto-triggers on proximity (your call — document the decision in CONTEXT.md). The Interactable system is primarily for non-combat objects going forward.

### 6. Minimal Combat Feedback
- Simple on-screen text or console log showing damage dealt/taken (full UI/VFX polish is a later phase)
- Player HP visibly decreases when hit by enemy; enemy HP visibly decreases when auto-attacked

### 7. Self-Test Before Reporting Complete (per standing protocol)
Using Unity MCP editor control tools:
1. Enter Play Mode programmatically
2. Move player toward the test enemy
3. Confirm auto-attack triggers automatically when in range (no input simulated for attacking — only movement)
4. Confirm enemy HP decreases, enemy AI aggros and attacks back, player HP decreases
5. Confirm enemy dies and deactivates at 0 HP
6. Confirm walking away from the enemy stops the auto-attack
7. Check Console for any errors throughout
8. Exit Play Mode
9. Only report Phase 2 complete if all of the above passes. If anything fails, diagnose and fix first.

### 8. Commit & Update CONTEXT.md
- Commit as: `[Phase 2] Auto-attack combat system + base Interactable framework`
- Update CONTEXT.md: log the damage/attack-speed formula decisions (flag as tunable), the Interactable architecture decision, confirm self-test passed, point "Current Phase" to Phase 3

## Do NOT
- Do not add any manual attack button or input action for attacking — combat is fully automatic/proximity-based
- Do not build the full 8-tier enemy system yet — one Weak-tier test enemy is enough to validate the loop
- Do not build gear/inventory yet (Phase 3)
- Do not build animations — state changes without visual animation are fine for now
- Do not guess at unresolved GDD items — flag in CONTEXT.md
