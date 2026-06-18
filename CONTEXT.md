# Void Bound — CONTEXT.md
*Claude Code: Read this file FIRST, every session, before touching any code.*

## Project State
- **Engine:** Unity 6.5 (6000.5.0f1), URP, mobile-first
- **Status:** Phase 3b (HUD & Menus) complete
- **GDD:** See `Void_Bound_GDD.md` in repo root — full design spec, all systems locked
- **MCP connections live:** Unity MCP (scene/asset control) + Blender MCP (procedural low-poly model generation)

## Lineage
Void Bound evolved from RunePortal (a Three.js browser ARPG). All gameplay systems (gear tiers, drop tables, skilling, Homestead hub) carry over conceptually — rebuilt natively in Unity/C#, not ported code.

## Critical Rules
0. **At the start of every session, before reading any other file, run `git pull origin main`** to ensure you're working from the latest committed design docs. If GDD or PHASES files appear to conflict with what you're about to build, the freshly-pulled version wins — flag the discrepancy to Jordon rather than proceeding on stale assumptions.
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
**Phase 4: Loot & Drop Tables** — see `PHASES/phase4_loot_drops.md`

## Phase 3b Log (completed 2026-06-17)
- **HUDManager:** `Scripts/UI/HUDManager.cs` — persistent Screen Space Overlay canvas, manages all panel toggles
- **Layout:** Top-left: level (stubbed Lv 1), XP bar (stubbed at 0%), HP bar (live from Health component), STR/DEX/VIG/INT readout. Top-right: minimap + 3 buttons (Equip/Bag/Dev, 44px each). Bottom-left: existing joystick (no overlap).
- **EquipmentPanelUI:** `Scripts/UI/EquipmentPanelUI.cs` — shows all 11 slots with rarity-colored borders, tap slot → detail view showing name/rarity/slot/stat modifiers/set, Unequip button
- **BackpackPanelUI:** `Scripts/UI/BackpackPanelUI.cs` — scrollable item list with rarity colors, tap item → detail view, Equip button
- **DevToolsPanel:** `Scripts/UI/DevToolsPanel.cs` — Give Test Gear, Kill All Enemies, Toggle God Mode (god mode blocks damage via GodModeFlag in Health.cs)
- **Minimap:** `Scripts/UI/Minimap.cs` — orthographic camera above player (30 units up), renders to 256x256 RenderTexture, displayed in top-right UI panel
- **Stubbed systems:** Level/XP bar shows placeholder values — no leveling system built yet, flagged for future phase
- **Keyboard shortcuts:** Tab = Equipment, B = Backpack (secondary to on-screen buttons)

## Phase 3 Log (completed 2026-06-17)
- **Weapon Types Resolved:** Sword, Sword2H, Dagger, Mace, Bow, Crossbow, Staff, Wand (from RunePortal `WEAPON_TYPES`). Replaced Axe/Spear with Sword2H/Dagger per source.
- **GearItemSO:** `Scripts/Data/GearItemSO.cs` — full fields: itemId, displayName, slot, weaponType, rarity, statModifiers (CharacterStats), visualPrefab, setId, baseDamage
- **PlayerInventory:** `Scripts/Inventory/PlayerInventory.cs` — 11-slot equip map + backpack list, EquipItem/UnequipItem applies stat modifiers to StatsComponent at runtime
- **RarityVisualEffects:** `Scripts/Inventory/RarityVisualEffects.cs` — static utility, 9-tier color mapping, emission for Rare+, particle accent for Legendary+
- **InventoryUI:** `Scripts/UI/InventoryUI.cs` — Tab key toggles, left panel = 11 equipment slots, right panel = backpack. Click to equip/unequip. Rarity-colored buttons.
- **TestGearStartup:** `Scripts/Inventory/TestGearStartup.cs` — adds 4 test weapons to backpack at Start
- **Test Gear Assets:** `ScriptableObjects/TestGear/` — Rusty Sword (Common, +2 STR), Arcane Blade (Rare, +5 STR/+3 DEX), Flamecleaver (Legendary, +12 STR), Voidreaver (Voidforged, +20 STR)
- **Stat Application:** Equipping gear adds statModifiers to StatsComponent.BaseStats, directly affecting auto-attack damage via the Phase 2 DamageCalculator pipeline

## Phase 2 Log (completed 2026-06-17)
- **Design:** Combat is fully automatic/proximity-based — NO manual attack button. Player auto-attacks nearest enemy within range on DEX-based cooldown. Attack input action removed from PlayerCombat.
- **CharacterStats:** `Scripts/Data/CharacterStats.cs` — serializable struct (STR/DEX/VIG/INT) with operator+
- **StatsComponent:** `Scripts/Combat/StatsComponent.cs` — computed MaxHP (100+VIG*10), PhysicalDamage, AttackInterval, CritChance (0.5%/DEX), DefenseMultiplier
- **Health:** `Scripts/Combat/Health.cs` — TakeDamage/Heal, OnDeath event, derives maxHP from StatsComponent
- **DamageCalculator:** `Scripts/Combat/DamageCalculator.cs` — STR scaling → crit roll (DEX, 1.5x) → VIG defense reduction → min 1 → spawns FloatingDamageNumber
- **PlayerCombat:** `Scripts/Combat/PlayerCombat.cs` — continuous OverlapSphere auto-attack, targets nearest enemy, DEX-based cooldown, faces target on attack
- **EnemyAI:** `Scripts/Combat/EnemyAI.cs` — state machine (Idle→Chase→Attack→Dead), CharacterController movement, reads from EnemyDefinitionSO
- **EnemyDefinitionSO:** Extended with baseStats, baseDamage, moveSpeed, aggroRange, attackRange
- **HealthBar:** `Scripts/Combat/HealthBar.cs` — world-space health bar above entities, color shifts green→yellow→red
- **FloatingDamageNumber:** `Scripts/Combat/FloatingDamageNumber.cs` — TextMesh that spawns on hit, rises, fades over 1s, yellow bold for crits
- **Interactable:** `Scripts/Core/Interactable.cs` — abstract base for future non-combat interactions (NPCs, resource nodes, crafting stations)
- **EnemyPlaceholder:** `Art/Models/EnemyPlaceholder.fbx` — low-poly squat goblin, red-brown, bake_space_transform=True
- **Scene:** Homestead.unity — TestEnemy at (5,0.1,5), both player and enemy have HealthBar components

## Phase 1 Log (completed 2026-06-17)
- **Input System:** Using Unity template's `InputSystem_Actions.inputactions` — Move (WASD/arrows/gamepad/joystick), Attack (left mouse/gamepad west)
- **PlayerController:** `Scripts/Core/PlayerController.cs` — CharacterController-based, isometric camera-relative movement, configurable speed/gravity
- **Decision: CharacterController** over Rigidbody — simpler API, no physics jitter, standard for fixed-camera isometric
- **Placeholder Character:** Low-poly box humanoid exported from Blender (`Art/Models/PlayerPlaceholder.fbx`), sandy brown URP/Lit material
- **Homestead Scene:** `Scenes/Homestead.unity` — ground (40x1x40), orthographic camera (X=30 Y=45), IsometricCameraFollow targeting player, mobile joystick (OnScreenStick bottom-left)
- **FBX Export Fix:** `bake_space_transform=True` required to avoid -90° X rotation bug (added to CODING_STANDARDS.md)
- **Verification Protocol:** Pre-Report self-test added to CODING_STANDARDS.md

## Phase 0 Log (completed 2026-06-16)
- **URP:** Confirmed active — `Mobile_RPAsset` (render scale 0.8, 1 cascade, no soft shadows) + `PC_RPAsset` already configured; `GraphicsSettings.asset` points to the correct pipeline
- **Isometric Camera:** Main Camera in SampleScene set to orthographic (size 8), rotation X=30 Y=45; `IsometricCameraFollow.cs` in `Scripts/Core/` — smoothly follows a target transform on X/Z with configurable offset and smoothing
- **Folder Structure:** Full tree created per spec (`Scripts/{Core,Data,Combat,Inventory,Skilling,UI}`, `Prefabs/`, `ScriptableObjects/`, `Art/{Models,Materials,Textures}`)
- **Base SOs:** `Enums.cs` (EquipmentSlot 11 slots, WeaponType 8+None, RarityTier 9 tiers ending in Voidforged, EnemyTier 8, SkillType 7), `GearItemSO`, `EnemyDefinitionSO`, `ZoneDefinitionSO`, `SkillDefinitionSO`, `RecipeDefinitionSO`, `LootTableSO` — all with `[CreateAssetMenu]` under `VoidBound/`
- **Scene:** SampleScene has PlayerPlaceholder cube (tagged Player) at (0, 0.5, 0) and Ground plane (20x0.1x20) for visual reference
- **Git:** `.gitignore` updated with `UserSettings/` exclusion

## Resolved Items (Phase 0)
- **9th rarity tier:** Voidforged (ties to Void Throne endgame)
- **Currency:** Gold (primary) + Void Shards (secondary) — locked
- **Level cap:** 99 per skill, 3x XP curve multiplier (tunable)
- **Homestead:** 12 structures total (pulled from RunePortal `HS_BUILDINGS`): Campfire, Forge, Shrine, Garden, Watchtower, Merchant, Warriors'/Rangers'/Mages' Guilds, Pool of Refreshment, Fast Travel Portal, Storage Chest

## Open Items
- ~~Confirm exact 8 weapon types~~ → **Resolved Phase 3** (Sword, Sword2H, Dagger, Mace, Bow, Crossbow, Staff, Wand)
- No remaining open items at this time

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
