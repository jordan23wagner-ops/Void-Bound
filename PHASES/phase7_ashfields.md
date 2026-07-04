# Phase 7 — Zone 2: Ashfields

**Read `CONTEXT.md` first (git pull per Critical Rule #0). Builds on Phase 6's Interactable/Portal patterns — extend, don't refactor.**

## Goal
Turn Ashfields into the game's first real combat zone (Weak/Standard enemies, intro loot loop) and make the Fast Travel Portal actually travel between scenes. Doing this required solving a problem that didn't exist before: the project had exactly one real scene (`Homestead.unity`), so `Player`/`Main Camera`/`HUDCanvas`/`EventSystem` had no cross-scene persistence — a second zone meant building that first, or gold/inventory/skills would reset on travel.

## Tasks

### 1. Cross-scene persistence (`GameBootstrap`)
`Scripts/Core/GameBootstrap.cs`, one instance living in `Homestead.unity`. On first load, marks `Player`, `Main Camera`, `HUDCanvas`, and `EventSystem` `DontDestroyOnLoad`. Returning to Homestead later reloads a *fresh* copy of that scene's own Player/Camera/HUD/EventSystem from disk — this instance detects the already-persisted singleton and destroys the fresh duplicates instead, keeping the original persisted set. Subscribes to `SceneManager.sceneLoaded` and repositions the persisted Player to a `PlayerSpawnPoint` object in whichever scene just loaded (CharacterController-safe: disable → set position → re-enable).

### 2. Data-driven zone destinations
Extended `Scripts/Data/ZoneDefinitionSO.cs` with `sceneName` and `isUnlocked`. Three assets in `Assets/ScriptableObjects/Zones/`: `homestead` (unlocked), `ashfields` (unlocked), `bleakwood` (locked, no scene yet). Extended `Scripts/UI/PortalUI.cs` (Phase 6 stub) to build its destination list from a `ZoneDefinitionSO[]` instead of hardcoded rows: current zone shows "HERE", unlocked-other zones are clickable and call `SceneManager.LoadScene`, locked zones stay "Coming soon". Since `PortalUI` lives on the now-persisted `HUDCanvas`, the same instance serves both scenes — Ashfields needed no UI code of its own, only a `PortalStation` trigger reusing the exact Homestead building setup (`Building_Portal.fbx` + trigger `BoxCollider`).

### 3. Ashfields scene
New `Assets/Scenes/Ashfields.unity`: Ground (scaled cube, matches Homestead's Phase 0 recipe) recolored toward the GDD's "warm oranges/sandy browns" (fallback palette — no RunePortal art reference available, tunable), a Directional Light, a `PlayerSpawnPoint`, a Fast Travel Portal building, and 4 hand-placed enemies (2× `Goblin_Scout`/Weak/`WeakLoot`, 2× `Goblin_Warrior`/Standard/`StandardLoot`) — reusing the exact `EnemyDefinitionSO`/`LootTableSO` assets from Phase 4 and the identical GameObject component recipe already used in `Homestead.unity` (`CharacterController` + `StatsComponent` + `Health` + `EnemyAI` + `LootDropper`, no new code). Enemy count/placement is a fallback per unavailable RunePortal source — flagged tunable.

### 4. Editor setup script
`Scripts/Editor/Phase7AshfieldsSetup.cs` (idempotent, `VoidBound/Setup Phase 7 - Ashfields` menu item + `SetupFromBatch` entry point): creates/updates the 3 zone assets, adds `PlayerSpawnPoint` + `GameBootstrap` to Homestead and wires `PortalUI.destinations`, builds all of Ashfields' scene content.

## Do NOT
- Do not build a save-to-disk system — this is in-memory persistence across scene loads within one play session only.
- Do not build a generic spawner/wave framework — enemies are hand-placed, matching Homestead's existing pattern.
- Do not implement Bleakwood — its Portal row stays locked.
- Do not do a new low-poly art/enemy-species pass — reuses existing placeholder mesh and goblin definitions.

## Self-Test (completed 2026-07-04)
Verified via Unity MCP direct invocation (Editor unfocused during automation, same methodology as Phase 6): entered Play Mode in Homestead, gave test gold/gear, confirmed `GameBootstrap` marked the 4 objects `DontDestroyOnLoad`, traveled to Ashfields via the Portal (`SceneManager.LoadScene`), confirmed gold/inventory/skills were unchanged (same Player instance, not reset) and the player spawned at Ashfields' `PlayerSpawnPoint` rather than the origin, killed a goblin (loot/gold/XP granted via the unmodified Phase 2/4/5b pipeline), returned via Ashfields' Portal, confirmed state persisted again and Homestead's own `PlayerSpawnPoint` positioning worked. Console clean both directions.

- **Bug found & fixed:** `SceneManager.LoadScene("Ashfields")` failed silently ("couldn't be loaded because it has not been added to the active build profile") — `Ashfields.unity` wasn't registered in Unity's Build Settings scene list. Fixed by adding both scenes and encoding it into `Phase7AshfieldsSetup.EnsureBuildScenes()` so re-running the setup script keeps it correct without a manual step.
- **Testing caveat (same root cause as Phase 6):** the unfocused Editor stalls the Player Loop, so newly-loaded-scene objects' `Start()` doesn't fire and `Destroy()` calls stay pending indefinitely during automated MCP testing — this affected the Ashfields enemies (verified real HP/init by manually setting `currentHP = MaxHP` to simulate `Start()`) and made the duplicate-prevention `GameBootstrap` destroy calls appear not to have run (verified via the static `instance` field that they *did* run correctly — the "duplicate" objects were just queued, not yet swept). Neither is a functional bug; a normally-focused play session ticks frames and never hits either condition.

## Commit
`[Phase 7] Zone 2: Ashfields - cross-scene player persistence, real portal travel, first combat zone`
