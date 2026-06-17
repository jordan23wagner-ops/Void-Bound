# Phase 0 — Project Setup

**Read `CONTEXT.md` and `CODING_STANDARDS.md` before starting.**

## Goal
Establish a clean, working Unity project foundation: correct render pipeline config, isometric camera rig, folder structure, and base ScriptableObject architecture. No gameplay yet — this phase is pure scaffolding.

## Tasks

### 1. URP Configuration
- Confirm URP is correctly assigned as the active render pipeline (Project Settings → Graphics)
- Set up a basic URP Asset with mobile-friendly settings (disable expensive post-processing for now, we'll tune later)
- Confirm Built-in Render Pipeline references are fully removed/unused (Unity 6.5 deprecates it — don't fight this)

### 2. Isometric Camera Rig
- Create a `CameraRig` prefab with:
  - Orthographic camera
  - Fixed rotation matching classic isometric angle (test 30°/45° tilt combinations, recommend starting at the standard isometric: rotate X 30°, Y 45°)
  - No player-controlled rotation (locked, per GDD Section 1)
  - Script: `IsometricCameraFollow.cs` — smoothly follows a target transform (the player) on X/Z, maintains fixed offset and angle
- Test in the existing SampleScene with a placeholder cube as the "player" to confirm the angle feels right

### 3. Folder Structure
Create the full folder structure as specified in `CONTEXT.md`:
```
Assets/Scripts/Core/
Assets/Scripts/Data/
Assets/Scripts/Combat/
Assets/Scripts/Inventory/
Assets/Scripts/Skilling/
Assets/Scripts/UI/
Assets/Prefabs/
Assets/ScriptableObjects/
Assets/Scenes/
Assets/Art/Models/
Assets/Art/Materials/
Assets/Art/Textures/
```

### 4. Base ScriptableObject Architecture
Create the enum definitions and base SO classes referenced in `CODING_STANDARDS.md`:
- `Scripts/Data/Enums.cs` — `EquipmentSlot`, `WeaponType`, `RarityTier`, `EnemyTier`, `SkillType` (use exact values from GDD Sections 2, 4, 5)
- `Scripts/Data/GearItemSO.cs`
- `Scripts/Data/EnemyDefinitionSO.cs`
- `Scripts/Data/ZoneDefinitionSO.cs`
- `Scripts/Data/SkillDefinitionSO.cs`
- `Scripts/Data/RecipeDefinitionSO.cs`
- `Scripts/Data/LootTableSO.cs`

These can be minimal/stub implementations for now — full fields will be filled in during Phases 3-5. Goal here is just to establish the `[CreateAssetMenu]` pattern works and compiles clean.

### 5. Git Setup
- Create a Unity-appropriate `.gitignore` (Library/, Temp/, Obj/, Build/, Logs/, *.csproj, *.sln, etc.)
- Commit this phase as: `[Phase 0] Project setup - URP config, isometric camera, folder structure, base SO architecture`

## Verification Before Marking Complete
1. Run the batch-mode compile check (see CODING_STANDARDS.md) — confirm zero errors
2. Confirm the isometric camera rig looks correct in the editor with a placeholder object
3. Confirm all ScriptableObject types appear correctly in the Create Asset menu under `VoidBound/`
4. Update `CONTEXT.md` "Current Phase" section to point to Phase 1, and log what was built here

## Do NOT
- Do not write any player movement, combat, or inventory logic yet — that's Phase 1+
- Do not generate any Blender models yet — we'll do that once the camera/scale reference is confirmed
- Do not guess at unresolved GDD items (Section 11) — flag them in CONTEXT.md instead
