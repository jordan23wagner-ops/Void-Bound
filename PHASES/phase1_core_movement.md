# Phase 1 — Core Movement

**Read `CONTEXT.md` before starting. Phase 0 is complete and committed — extend, don't refactor.**

## Goal
Get the player moving smoothly around a basic Homestead scene using virtual joystick input (mobile) with keyboard equivalent (desktop testing), camera following correctly, on a low-poly placeholder character (not the cube anymore, not final art either).

## Tasks

### 1. Input System Setup
- Create an Input Actions asset: `Assets/Settings/PlayerInputActions.inputactions`
- Define a `Move` action (Vector2, 2D Composite) bound to:
  - WASD keys (desktop testing)
  - Arrow keys (desktop testing, secondary binding)
  - On-screen stick (mobile — see Task 2)
- Use Unity's new Input System (`UnityEngine.InputSystem`) per CODING_STANDARDS.md — no legacy `Input.GetAxis()`

### 2. Virtual Joystick (Mobile)
- Use Input System's built-in **`OnScreenStick`** component (ships with the Input System package) rather than custom-building joystick UI from scratch — less error-prone, officially supported
- Create a UI Canvas (`Assets/Prefabs/UI/MobileControls.prefab`) with the on-screen stick positioned bottom-left, sized for thumb reach
- Wire it to feed the same `Move` action defined in Task 1

### 3. PlayerController Script
- New script: `Scripts/Core/PlayerController.cs`
- Reads `Move` action's Vector2 value
- Moves the player transform along world X/Z plane (project camera's forward/right onto XZ if needed for isometric-relative movement — confirm visually it feels intuitive given our 30°/45° camera angle)
- Use a `CharacterController` or simple `Rigidbody` + `MovePosition` (your call — pick whichever is more reliable for a fixed-camera isometric game, document the choice in CONTEXT.md)
- Movement speed as a `[SerializeField]` field, not a magic number

### 4. Placeholder Low-Poly Character
- Use Blender MCP to generate a simple low-poly humanoid placeholder: basic capsule/box-composed body, rough proportions (head, torso, limbs), no rigging/animation yet — this replaces the PlayerPlaceholder cube
- Export as `.fbx` or `.glb`, import into `Assets/Art/Models/`
- Apply a basic URP/Lit material with a warm color (per GDD Section 1 palette — sandy brown or warm orange) so it doesn't default to magenta
- Replace `PlayerPlaceholder` cube in the scene with this new model, keep the GameObject name `Player` going forward

### 5. Homestead Scene
- Create `Assets/Scenes/Homestead.unity` (can start from a copy of SampleScene's lighting/camera setup)
- Ground plane sized reasonably for a hub area (confirm scale feels right against the player's size)
- Player spawn point at scene origin or a sensible default position
- Attach the `CameraRig` prefab from Phase 0, confirm `IsometricCameraFollow` targets the new Player object correctly
- This scene does NOT need the 12 Homestead buildings yet — that's Phase 6. Just the ground, player, and camera working together.

### 6. Verification
- Run the batch-mode compile check — zero errors before proceeding
- Test in Play Mode: confirm WASD/arrow keys move the player smoothly on X/Z
- Confirm camera follows correctly at the locked isometric angle, no jitter or lag
- Confirm the placeholder character renders with proper material (no magenta)
- Commit as: `[Phase 1] Core movement - input system, virtual joystick, placeholder character, Homestead scene`
- Update `CONTEXT.md`: log the CharacterController vs Rigidbody decision, confirm movement feel, point "Current Phase" to Phase 2

## Do NOT
- Do not build combat/attack logic yet (Phase 2)
- Do not build the full Homestead hub buildings yet (Phase 6) — ground + player + camera only
- Do not add animation to the placeholder character — static low-poly model is fine for now
- Do not guess at open GDD items — flag anything unresolved in CONTEXT.md
