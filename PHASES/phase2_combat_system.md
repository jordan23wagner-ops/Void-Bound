# Phase 2 — Combat System

**Read `CONTEXT.md` before starting. Phases 0-1 are complete and committed — extend, don't refactor.**

## Goal
Player can attack a basic enemy using STR/DEX/VIG/INT-driven damage, enemy has simple AI (aggro, approach, attack back, die), all wired end-to-end and self-verified in Play Mode before reporting complete.

## Tasks

### 1. Attack Input
- Add an `Attack` action to `PlayerInputActions.inputactions` (Button type)
- Bind to: Spacebar / Left Mouse Click (desktop), and a UI button (mobile — add to `MobileControls.prefab`, bottom-right, opposite the joystick per GDD Section 1 input layout)

### 2. Character Stats
- Create `Scripts/Data/CharacterStatsSO.cs` (or component, your call — document which in CONTEXT.md) implementing the simplified RPG stat block: **STR, DEX, VIG, INT**
- Derive starting formulas (flag as tunable, not final):
  - Max HP = base value + (VIG × multiplier)
  - Physical damage = base weapon damage + (STR × multiplier)
  - Attack speed/crit chance influenced by DEX (keep simple for now — e.g. flat crit % per DEX point)
  - INT reserved for magic damage (not used yet, no spells in Phase 2 — just include the field)
- Attach to Player as a component reading from a default `CharacterStatsSO` asset

### 3. Enemy Definition & Prefab
- Flesh out `EnemyDefinitionSO.cs` (stubbed in Phase 0): tier (use `EnemyTier` enum), base HP, base damage, aggro range, attack range
- Create ONE test enemy instance for Phase 2: **Weak tier**, simple stats (low HP, low damage) — just enough to validate the combat loop works
- Generate a low-poly placeholder enemy model via Blender MCP, similar process to PlayerPlaceholder.fbx from Phase 1
  - **IMPORTANT — avoid repeating the Phase 1 bug:** explicitly set Blender's FBX export axis to Forward: -Z, Up: Y before exporting. Verify on import that the GameObject's Transform Rotation reads (0,0,0) with no manual correction needed. Confirm this before attaching any AI/physics components.
- Enemy prefab needs: CharacterController or simple Collider, the EnemyController script (Task 4), a basic material (avoid magenta — assign URP/Lit explicitly)

### 4. EnemyController Script
- New script: `Scripts/Combat/EnemyController.cs`
- Simple state machine: **Idle → Aggro → Attack → Dead**
  - Idle: enemy stands still until player enters aggro range (read from `EnemyDefinitionSO`)
  - Aggro: enemy moves toward player (reuse movement logic pattern from PlayerController where sensible)
  - Attack: when within attack range, deals damage to player on an interval (don't spam every frame)
  - Dead: when HP ≤ 0, play a simple death state (can be as basic as disabling the GameObject for now — full death animation is later polish)

### 5. PlayerCombat Script
- New script: `Scripts/Combat/PlayerCombat.cs`
- On `Attack` action triggered: detect enemies in range (simple `Physics.OverlapSphere` or trigger collider in front of player is fine for now)
- Apply damage using the STR-derived formula from Task 2
- Enemy takes damage, HP decrements, dies at 0 (triggers Dead state in EnemyController)

### 6. Minimal Combat Feedback
- Simple on-screen text or console log showing damage dealt/taken (full UI/VFX polish is a later phase — this is just enough to visually confirm combat is working)
- Player should also be able to take damage from the enemy and see HP decrease (basic text display is fine)

### 7. Self-Test Before Reporting Complete (per standing protocol)
Using Unity MCP editor control tools:
1. Enter Play Mode programmatically
2. Move player toward the test enemy (simulate input if possible, or position player near enemy directly)
3. Trigger an attack, confirm enemy HP decreases in logs/state
4. Confirm enemy AI activates (aggro triggers when player approaches) and enemy attacks back
5. Confirm player HP decreases when hit
6. Confirm enemy dies and deactivates when HP reaches 0
7. Check Console for any errors throughout
8. Exit Play Mode
9. Only report Phase 2 complete if all of the above passes. If anything fails, diagnose and fix first.

### 8. Commit & Update CONTEXT.md
- Commit as: `[Phase 2] Combat system - attack input, STR/DEX/VIG/INT stats, enemy AI, damage loop`
- Update CONTEXT.md: log the damage formula decisions (flag as tunable), confirm self-test passed, point "Current Phase" to Phase 3

## Do NOT
- Do not build the full 8-tier enemy system yet — one Weak-tier test enemy is enough to validate the loop
- Do not build gear/inventory yet (Phase 3) — damage formula uses base stats only, no weapon items yet
- Do not build animations — death/attack can be state changes without visual animation for now
- Do not guess at unresolved GDD items — flag in CONTEXT.md
