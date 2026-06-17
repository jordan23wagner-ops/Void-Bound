# Void Bound — CONTEXT.md
*Claude Code: Read this file FIRST, every session, before touching any code.*

## Project State
- **Engine:** Unity 6.5 (6000.5.0f1), URP, mobile-first
- **Status:** Fresh project, no gameplay code written yet
- **GDD:** See `Void_Bound_GDD.md` in repo root — full design spec, all systems locked
- **MCP connections live:** Unity MCP (scene/asset control) + Blender MCP (procedural low-poly model generation)

## Lineage
Void Bound evolved from RunePortal (a Three.js browser ARPG). All gameplay systems (gear tiers, drop tables, skilling, Homestead hub) carry over conceptually — rebuilt natively in Unity/C#, not ported code.

## Critical Rules
1. **Never refactor existing systems — extend only**, unless explicitly told otherwise.
2. **Verify compilation after every change** using Unity batch-mode:
   ```
   Unity.exe -batchmode -projectPath "C:\Users\Jordon\Void Bound" -quit -logFile compile_check.log
   ```
   Check the log for errors before proceeding to the next task.
3. **ScriptableObject-driven architecture** for all data (gear, enemies, zones, skills, recipes, loot tables) — see Section 9 of the GDD.
4. **No deprecated Unity APIs.** Use the new Input System (not legacy Input class). Confirm current Unity 6.x best practices if uncertain — do not assume from older training data.
5. **Commit after every completed task**, with a clear message referencing the phase/task (e.g. "Phase 0: URP config + isometric camera rig").
6. **Update this CONTEXT.md** after each phase completes — log what was built, file locations, and any decisions made.

## Current Phase
**Phase 0: Project Setup** — see `PHASES/phase0_setup.md`

## Open Items (resolve as encountered)
See Section 11 of `Void_Bound_GDD.md` for unresolved design questions (exact weapon type list, top rarity tier name, remaining Homestead buildings, XP curve values). Flag these to Jordon rather than guessing.

## Folder Structure (target)
```
Assets/
├── Scripts/
│   ├── Core/          (GameManager, SceneLoader, etc.)
│   ├── Data/           (ScriptableObject definitions)
│   ├── Combat/
│   ├── Inventory/
│   ├── Skilling/
│   └── UI/
├── Prefabs/
├── ScriptableObjects/  (actual data assets, instances of the SO classes)
├── Scenes/
│   ├── Homestead.unity
│   ├── Ashfields.unity
│   └── Bleakwood.unity
└── Art/
    ├── Models/
    ├── Materials/
    └── Textures/
```

## Communication Style
Jordon prefers: concise updates, no fluff, full copy-paste-ready code when manual edits are needed, one clear next step at the end of each session. Flag issues immediately rather than working around them silently.
