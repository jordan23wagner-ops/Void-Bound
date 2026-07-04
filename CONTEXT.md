# Void Bound — CONTEXT.md
*Claude Code: Read this file FIRST, every session, before touching any code.*

## Project State
- **Engine:** Unity 6.5 (6000.5.0f1), URP, mobile-first
- **Status:** Phase 6 (Homestead Full Build-out) complete
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
**Phase 8: Zone 3 — Bleakwood** (not yet started). Phase 7 completed and play-mode verified 2026-07-04.

## Equipment Visuals Log (completed & play-mode verified 2026-07-04, between Phases 7 and 8)
- **Gear/item models:** `Tools/build_equipment_models.py` (headless Blender) → 20 low-poly FBX in `Assets/Resources/Equipment/`: 8 weapon types (Sword/Sword2H/Dagger/Mace/Bow/Crossbow/Staff/Wand), armor (Helm/Body/Legs/Boots/Gloves/Cape/Shield), Amulet, and 4 material props (OreChunk/Ingot/Herb/Fish). Shared material slots `Main` (rarity-tinted at runtime) + `Accent` (neutral). Weapons/shield authored in grip-space (attach at hand sockets); armor in hero-body space (attach at root socket).
- **Visible equipment:** `Scripts/Combat/EquipmentVisuals.cs` — an *observer* (no equip/stat/combat changes). Builds 3 socket transforms per body (`Socket_Root`, `Socket_HandR`, `Socket_HandL`; tuned per BodyType Hero/Goblin — goblin root downscaled 0.68 to fit hero-space armor). Slot→socket: Weapon→handR, Shield→handL, all armor→root. Player mode follows `PlayerInventory.OnInventoryChanged` live (diff Equipped vs shown, instantiate/destroy `GearItemSO.visualPrefab` at sockets, tint `Main` via `RarityVisualEffects.GetRarityColor`). Enemy mode reads the def's gear once at Start.
- **Reused the dormant `visualPrefab` field** on GearItemSO (was null everywhere) — now points each gear at its model. `StarterGearGenerator` (`VoidBound/Gear - Generate Starter Set`) creates a full Common **Iron set** (helm/chest/greaves/boots/gauntlets/cape/shield/amulet) + Rare/Epic/Legendary showcase pieces under `Assets/ScriptableObjects/Gear/`, back-fills the 4 swords, and assigns goblin gear. All stat/gold numbers placeholder — tunable.
- **Enemies:** `EnemyDefinitionSO` extended with `weapon` + `armor[]` (extend-only). Goblin Scout = club; Warrior = club+helm; Champion = club+helm+scrap plate. `CharacterModelSwap` now also attaches/configures `EquipmentVisuals` (Hero on Player, Goblin+def on each enemy) in both scenes.
- **Characters regenerated without built-in weapons** (`build_character_models.py`): hero lost its baked hip sword, goblin lost its baked club — the weapon slot is now authoritative (empty hand when unarmed). Re-run + re-swapped.
- **`WorldPickup`** now instantiates the gear's `visualPrefab` (rarity-tinted, spinning) instead of a colored sphere; sphere kept as fallback.
- **Orchestrator:** `VoidBound/Gear - Full Equipment Setup` runs gear-gen then the swap. Verified in Play Mode: hero wears a full Iron set + Rare sword in hand + shield on the off-hand at correct sockets; unequip removes the piece (internal `shown` dict drops immediately; the child `Destroy` lags only because the unfocused editor stalls frames); rarity tints confirmed exact (Rare sword 0.2/0.6/1, Epic helm 0.7/0.3/0.9, Legendary pickup 1/0.75/0.1); goblin Warrior shows helm + club; fall-through OK; console clean.
- **Known/tunable:** socket offsets are constants (armor centers within ~0.1u of body parts — good for low-poly; refine if desired). Goblin reuses hero-space armor scaled 0.68 (fits roughly; goblins mostly get weapon+helm). Ring/Ammo have no visible model by design. Material props (ore/ingot/herb/fish) are generated and ready but not yet wired to any world drop (materials still come from resource nodes directly).

## Polish Pass 2 Log (completed & play-mode verified 2026-07-04, between Phases 7 and 8)
- **Buildings:** all 12 Homestead stations now have distinct low-poly models (`Tools/build_homestead_buildings.py`, headless Blender — previously 4 shared one Hut mesh and the Campfire was a stall mesh). Shared URP palette in `Assets/Art/Materials/Buildings/` assigned by imported slot name; Fire/Water/Crystal are emissive. `HomesteadBuildingSwap` (menu item, idempotent) swapped both scenes; colliders/stations/tooltips untouched.
- **UI overhaul:** procedurally generated white rounded-rect 9-slice sprites (`UISpriteGenerator` → `Assets/Resources/UI/`, tinted via Image.color everywhere). `Panel5cFactory` upgraded: rounded panels + drop shadows, gold bold titles over a gold accent strip, rounded rows/buttons with hover/pressed `ColorBlock` states — Merchant/Storage/Training/Portal/Tooltip inherited automatically. `CraftingUI` rewritten to self-build on HUDCanvas from the factory (legacy scene-wired `MobileControls/CraftingPanel` deleted by `UIOverhaulSetup`); craft logic unchanged, rich-text ingredient coloring added. Phase 5c Equipment/Inventory panels restyled via `Phase5cUIBuilder` (approval recorded in its header) and rebuilt. **PlayerInfoBar** is now a single rounded card (portrait, name, Lv, HP bar, XP bar, 2-line VIG/STR/DEX/INT stats, gold/shards) — the illegible Phase 3b StatsPanel is deleted, all `HUDManager` refs repointed. HUD Equip/Bag/Dev buttons rounded; minimap framed.
- **Follow-up (same day):** all UI renders 50% larger — `CanvasScaler.referenceResolution` is 1280x720 in `Phase5cUIBuilder` (readability request; 1.25x/1536x864 was also tried and read too small — 1.5x is the settled value; the builder re-applies this on every rebuild, so don't "fix" it back to 1920x1080). Station panels now auto-close when the player walks away: `Scripts/UI/StationProximityCloser.cs` (single-slot tracker on HUDCanvas, close distance = station `InteractRange` + 2u, tunable const); all 5 station UIs (`MerchantUI`/`StorageUI`/`TrainingUI`/`PortalUI`/`CraftingUI`) Track on Open and Untrack on Close — Merchant/Storage/Portal `Open()` signatures gained a station parameter. Equipment/Inventory are player UI, not station UI — they don't auto-close.
- **Testing caveat (recurring):** uGUI `Selectable` state tints apply via transitions that need a frame tick — in the stalled unfocused-editor session, freshly built buttons screenshot as untinted white until `CrossFadeColor(..., instant)` is forced manually. Not a real bug; focused gameplay ticks normally. Verified: craft round-trip (fish→cooked fish, +15 Cooking XP), equip/unequip on the rebuilt panels, all 7 panel types + tooltip screenshotted per the Visual Protocol, fall-through check, console clean.

## Polish Pass Log (completed & play-mode verified 2026-07-04, between Phases 7 and 8)
- **Building hover tooltips:** `Interactable` extended with a `tooltipDescription` field; new `Scripts/UI/BuildingTooltipUI.cs` on the persisted HUDCanvas raycasts from the mouse (new Input System) and shows a Panel5cFactory-styled tooltip near the cursor — building name + description + interact prompt. All 12 buildings have descriptions; resource nodes get a generic one. **Mobile caveat:** hover is mouse-only — touch devices never see it; the proximity-interact flow is unchanged. Tooltip panel is fully `raycastTarget=false`, never blocks clicks.
- **Character models:** blocky `PlayerPlaceholder`/`EnemyPlaceholder` meshes replaced with proper low-poly models generated by `Tools/build_character_models.py` via **headless Blender** (`blender.exe -b -P`, Blender 5.1 — Blender MCP not needed; the CODING_STANDARDS FBX settings incl. `bake_space_transform=True` carry over verbatim and both models import at (0,0,0)). Goblin (~1.3u): hunched potbelly, oversized head, long swept ears, snout, thin limbs, loincloth, club — 2 material slots (Skin/Cloth), skin tinted per tier (`GoblinSkin_Weak` olive / `_Standard` red-brown / `_Elite` deep red). Hero (~1.8u): hair, shoulder pads, tapered armor torso, sword at hip — 3 slots (Skin/Armor/Hair). Materials assigned **by imported slot name**, not index — the Blender material names are the contract.
- **Swap tooling:** `Scripts/Editor/CharacterModelSwap.cs` (`VoidBound/Polish - Swap Character Models + Tooltips`, idempotent) — creates the URP materials, swaps mesh+materials on Player and every `EnemyAI` in both scenes (tier read from the enemy's `EnemyDefinitionSO`), and wires all tooltip descriptions + the HUDCanvas component. Old placeholder FBXs left in place (still referenced by nothing critical; removable later).
- **Verified:** close-up screenshots of hero and goblins in both scenes inspected per the Visual Protocol; tooltip detection (physics raycast → Interactable) and rendering both exercised and screenshotted; fall-through check passed; console clean.

## Phase 7 Log (completed & play-mode verified 2026-07-04)
- **Cross-scene persistence (the core new piece):** `Scripts/Core/GameBootstrap.cs`, one instance in `Homestead.unity`. Marks `Player`, `Main Camera`, `HUDCanvas`, and `EventSystem` `DontDestroyOnLoad` on first load; on later Homestead reloads, detects the already-persisted singleton (static `instance` field) and destroys the freshly-loaded scene's duplicate set instead. Repositions the persisted Player to a `PlayerSpawnPoint` object on every `SceneManager.sceneLoaded` event (CharacterController-safe: disable → set position → re-enable). This is in-memory only — no save-to-disk system exists or was attempted.
- **Data-driven zones:** `Scripts/Data/ZoneDefinitionSO.cs` extended with `sceneName`/`isUnlocked`. Three assets in `Assets/ScriptableObjects/Zones/`: `homestead`, `ashfields` (both unlocked), `bleakwood` (locked, no scene yet). `Scripts/UI/PortalUI.cs` (Phase 6 stub) now builds its rows from a `ZoneDefinitionSO[]` and actually calls `SceneManager.LoadScene` for unlocked non-current zones — since `PortalUI` lives on the now-persisted `HUDCanvas`, one instance serves every scene; Ashfields only needed its own `PortalStation` trigger (reusing `Building_Portal.fbx`), no new UI.
- **Ashfields.unity:** new zone scene — Ground (recolored sandy-orange, fallback palette per GDD's "warm oranges/sandy browns," no RunePortal art reference available, tunable), Directional Light, `PlayerSpawnPoint`, Fast Travel Portal, and 4 hand-placed enemies (2× `Goblin_Scout`/Weak, 2× `Goblin_Warrior`/Standard) reusing the exact Phase 4 `EnemyDefinitionSO`/`LootTableSO` assets and Homestead's existing enemy GameObject recipe verbatim (`CharacterController`+`StatsComponent`+`Health`+`EnemyAI`+`LootDropper`). Enemy count/placement is a fallback guess, flagged tunable.
- **Editor setup:** `Scripts/Editor/Phase7AshfieldsSetup.cs` (`VoidBound/Setup Phase 7 - Ashfields` menu item + `SetupFromBatch`), idempotent — builds/updates zone assets, wires Homestead's bootstrap + spawn point + Portal destinations, builds all of Ashfields' scene content, and registers both scenes in `EditorBuildSettings.scenes`.
- **Bug found & fixed during self-test:** `SceneManager.LoadScene` silently failed because `Ashfields.unity` wasn't in Unity's Build Settings scene list — fixed live and encoded into the setup script's `EnsureBuildScenes()` so it stays correct on re-runs.
- **Verified:** full Homestead → Ashfields → Homestead round-trip preserves gold/inventory/skill XP exactly (same persisted Player instance), spawn-point positioning correct both directions, combat/loot/XP pipeline (Phase 2/4/5b, unmodified) works identically in the new zone, `GameBootstrap`'s duplicate-prevention logic confirmed correct via the static singleton reference (Editor-unfocused testing defers `Destroy()`/`Start()` execution but doesn't invalidate the logic — same caveat as Phase 6's self-test). Console clean throughout.
- **Deferred:** Bleakwood (Phase 8) — its Portal row stays locked. No low-poly art pass for new enemy species — reuses the existing placeholder mesh.

## Phase 6 Log (completed & play-mode verified 2026-07-04)
- **Task 1 (RunePortal source resolution):** RunePortal source is no longer available locally (`C:\Users\Jordon\RunePortal` now contains only a `sprites/` folder) — Shrine, Watchtower, Pool tier effects/costs, and Guild training cost/XP model all use the documented fallback designs from `PHASES/phase6_homestead.md`, flagged as unconfirmed in code comments on each station script. GDD Section 6 corrected: 9→12 buildings (stale count from before Phase 0's RunePortal `HS_BUILDINGS` resolution), per-building status added.
- **7 stations, all `Assets/Scripts/Homestead/`, all `Interactable` subclasses (Phase 5 pattern, proximity-triggered via `PlayerInteractor`):**
  - `MerchantStation` → `MerchantUI` (buy/sell, data-driven via `ShopInventorySO`). Sell price = 40% of `goldValue` (`MerchantUI.SellRatio`, tunable). Test gold values assigned by rarity: Common 20, Uncommon 45, Rare 120, Epic 300, Legendary 600, Voidforged 2000 (materials flat 5) — all tunable, set by `Phase6HomesteadSetup.AssignTestItemGoldValues`.
  - `StorageStation` → `StorageUI` + `PlayerStorage` (48-slot bank, mirrors `PlayerInventory` add/remove semantics; withdraw respects the 24-slot backpack cap).
  - `PoolStation` → full heal + timed all-stat buff (tier 1 live: +2 all stats, 120s, 60s cooldown; tiers 2-4 stored as data in `PoolStation.tiers` with upgrade costs). **Upgrade purchasing is a TODO for a later phase** (flagged in code).
  - `ShrineStation` → 25g offering → +5 STR/+5 INT blessing, 180s, 120s cooldown. GDD's "+% damage" isn't representable in the `CharacterStats` pipeline, so this is a deliberate flat-stat deviation (documented in-code).
  - `GuildStation` (one script, 3 configured instances: Warriors'→STR, Rangers'→DEX, Mages'→INT) → `TrainingUI`. Cost = `10g × current level` (tunable), +50 stat XP + 25 VIG XP per session (50% VIG ratio mirrors `CombatXPCalculator`).
  - `PortalStation` → `PortalUI`, destination list UI only (Homestead/Ashfields/Bleakwood, latter two locked "Coming soon"). No scene travel this phase, as specified.
  - `WatchtowerStation` → flavor-text stub ("The wastes are quiet.") — real function deferred, not invented, per spec.
- **`TimedBuff`** (`Scripts/Homestead/TimedBuff.cs`): the shared buff mechanism for Pool/Shrine. Applies/reverts a `CharacterStats` bonus through the existing `StatsComponent.AddGearBonus/RemoveGearBonus` pipeline, calls `Health.RefreshMaxHP()` on change. Buffs stack by distinct id (`pool_refresh`, `shrine_blessing`); re-applying the same id refreshes rather than stacks with itself.
- **UI:** `MerchantUI`, `StorageUI`, `TrainingUI`, `PortalUI` all live as components on `HUDCanvas` and self-build their panel hierarchy at first `Open()` via a new shared helper, `Panel5cFactory` (`Scripts/UI/Panel5cFactory.cs`) — extracts the Phase 5c color palette/header/close-button/scroll-list patterns from `Phase5cUIBuilder` into reusable static methods so these 4 new panels match the approved visual language without duplicating it. `BuffIndicatorUI` adds a minimal ASCII buff-timer readout below the Player Info Bar (Task 10).
- **Bug found & fixed during self-test:** the 4 new overlay panels didn't coordinate with each other — opening one (e.g. Portal) left a previously-open one (e.g. Storage) visible underneath, since each panel is an independent full-screen overlay at the same anchor (unlike Phase 5c's deliberate side-by-side Equipment/Inventory layout). Fixed with `Panel5cFactory.CloseOtherHomesteadPanels()`, called at the top of each panel's `Open()`.
- **Self-test methodology note:** Unity Editor did not have OS focus during the automated MCP session, which throttles the Player Loop (confirmed via `editor/state`'s `is_changing` flag staying stuck) — physical player movement + `PlayerInteractor`'s proximity `OverlapSphere` detection wasn't reliable under those conditions. Verification instead called each station's `OnInteract()` directly (same code path a real interaction takes) via Unity MCP's `execute_code`, confirming: Merchant buy/sell (gold + item counts correct), Storage deposit/withdraw round-trip, Pool full heal + buff + cooldown block, Shrine offering + stacking with Pool, Guild training (gold spent, STR/VIG XP granted), Portal panel + locked destinations, Watchtower flavor popup — all with 0 console errors/warnings. Buff expiry (stat/MaxHP revert) verified by forcing `expiresAt` into the past and invoking `TimedBuff.Update()` directly, rather than waiting out the real 120-180s durations.
- **Deferred:** Task 9 (low-poly art pass for the 7 buildings) — not attempted this session, buildings remain the Phase 5 placeholder meshes. Time-boxed/deferrable per spec; revisit when picking up art work generally.

## Phase 5c Log (completed & play-mode verified 2026-07-03)
*(An earlier 5c log dated 2026-06-17 claimed completion prematurely — that build was mockup visuals only, never verified in Play Mode. The 2026-07-03 session finished the runtime wiring and ran the full self-test.)*
- **Builder:** `VoidBound > Build Phase 5c UI` (`Scripts/Editor/Phase5cUIBuilder.cs`) — builds UIRoot5c (EquipmentPanel + InventoryPanel, approved mockup visuals), PlayerInfoBar, attaches runtime controllers, retires old panels (InventoryPanelGroup, BackpackPanel), and repoints HUDManager HP refs. Batch entry point: `Phase5cUIBuilder.BuildFromBatch`. Fixed 3 builder layout bugs found via screenshot verification: SetAnchor wiping panel sizeDelta, ContentSizeFitter nested in LayoutGroup mis-sizing the stat card, and missing childControlWidth/Height flags pushing stat values outside the card.
- **Runtime controllers (new, name/order-based binding — no scene wiring needed):**
  - `Scripts/UI/Phase5cUIRoot.cs` — panel visibility manager; Equip button toggles Equipment, Bag toggles Inventory, root active while either is open.
  - `Scripts/UI/EquipmentPanel5c.cs` — 11 slots (Left: Helm/Body/Legs/Boots/Gloves, Right: Amulet/Ring/Ring2/Cape, Dock: Weapon/Shield) with rarity-colored borders from `RarityVisualEffects`; center readout: Character Level (CombatLevelCalculator), Damage (weapon baseDamage through Physical/MagicDamage by weapon style), Defense (from DefenseMultiplier), live VIG/STR/DEX/INT from `StatsComponent.EffectiveStats`; refreshes on OnInventoryChanged.
  - `Scripts/UI/InventoryPanel5c.cs` — 4-column scrollable grid (24 slots) rebuilt from `PlayerInventory.Backpack`; identical GearItemSO assets group into stacks with ×N badge; capacity X/24 in header; Gold + Void Shards footer from `PlayerCurrency`.
  - `Scripts/UI/ItemDetailView5c.cs` — shared tap-to-detail overlay (name/rarity/slot/modifiers/set/damage) with Equip/Unequip action button; reuses Phase 3 EquipItem/UnequipItem logic unchanged.
- **HUDManager:** extended (not refactored) — routes Equip/Bag toggles + Tab/B keys to Phase5cUIRoot when present, legacy fallback kept; CloseAllPanels also closes 5c root; DevToolsPanel untouched.
- **Player Info Bar:** top-left portrait placeholder + "PLAYER" + green HP bar with current/max text; replaces the old flat HP row inside StatsPanel (deleted by builder, HUDManager hpFill/hpText repointed via SerializedObject).
- **Slot mapping resolved (2026-07-04):** the `Ring2` enum value was implementation drift — every design doc (GDD, phase3, phase3b, phase5c) specifies the 11-slot list ending in `Ring, Ammo`, not two rings. Renamed `Ring2` → `Ammo` in `EquipmentSlot` (same ordinal position, no serialized asset impact) and updated all UI code/labels (`EquipmentPanel5c`, legacy `EquipmentPanelUI`, `InventoryPanel5c`, `ItemDetailView5c`, `Phase5cUIBuilder`) accordingly. Right column is now Amulet/Ring/Ammo/Cape as originally specced.
- **Character preview (Task 8):** DEFERRED — needs a secondary camera + RenderTexture; functional panels prioritized per spec.
- **Known cosmetic issue (pre-existing, flagged):** legacy StatsPanel text (Lv/XP/VIG..., top-left under the info bar) is not legible in Game view captures; character stats are now authoritative in the Equipment panel readout. Worth a look when the HUD is next touched.
- **Tooling note:** Unity MCP was NOT connected in this Claude Code session (the MCP-for-Unity plugin in the editor auto-connects to Claude Desktop only). Self-test was run by driving the open editor via computer-use; batch compile via Unity.exe -batchmode after closing the editor. Self-test passed: layout matches mockup, live stat values (STR leveled 1→2 mid-test from kills, gear bonus +2 applied on equip), stack badges ×3→×2 on equip, capacity 12/24→11/24, currency Gold 34 / Shards 4 from real drops, equip + unequip round-trip clean, X buttons close panels, console 0 errors / 0 warnings at Fixed 1920x1080.

## Phase 5b Log (completed 2026-06-17)
- **RunePortal source confirmed:** XP split is 40/40/20+vigor. Actual code: melee→STR XP, ranged→DEX XP, magic→INT XP, VIG gets `xpGain * 0.5` (50% ratio, not 33%). XP curve uses OSRS formula: `sum(i + 300 * 2^(i/7)) / 4 * 3`. Character Level = `levelFromXP(player.totalXP)`.
- **CombatStatXP:** `Scripts/Combat/CombatStatXP.cs` — WeaponStyleMap (melee/ranged/magic → STR/DEX/INT), CombatXPCalculator (damage-based XP + 50% VIG passive), CombatLevelCalculator (average of 4 stat levels)
- **SkillType enum:** Added CombatVIG, CombatSTR, CombatDEX, CombatINT to existing enum
- **StatsComponent overhaul:** Now derives stats from combat skill levels (via PlayerSkills) + gear bonuses. EffectiveStats property combines both. MaxHP from VIG level. AddGearBonus/RemoveGearBonus replaces old SetBaseStats pattern.
- **PlayerCombat:** Awards combat XP on every hit — determines weapon style from equipped weapon, calls CombatXPCalculator
- **PlayerInventory:** Uses AddGearBonus/RemoveGearBonus instead of modifying BaseStats
- **HUD:** Stats display reordered to VIG/STR/DEX/INT (top to bottom per RunePortal). Character Level derived from CombatLevelCalculator. Individual stat levels shown.
- **XP curve:** OSRS-style from RunePortal source, 3x multiplier, level 99 cap
- **VIG XP ratio:** 50% (from RunePortal source `xpGain * 0.5`)
- **Weapon-to-stat mapping:** Sword/Sword2H/Mace/Dagger → STR (melee), Bow/Crossbow → DEX (ranged), Staff/Wand → INT (magic)

## Phase 5 Log (completed 2026-06-17)
- **Building Mapping Resolved (from RunePortal source):** Guilds = stat training (not crafting). Fishing/Mining = field-based resource nodes. Forge = Smithing. Campfire = Cooking. Garden = Gathering + Alchemy.
- **SkillDefinitionSO:** `Scripts/Data/SkillDefinitionSO.cs` — XP curve (level^2 * 3x * 10), level 99 cap, recipe list
- **RecipeDefinitionSO:** `Scripts/Data/RecipeDefinitionSO.cs` — ingredients (MaterialItemSO + quantity), output (Material or Gear), required skill/level/station, XP reward
- **MaterialItemSO:** `Scripts/Data/MaterialItemSO.cs` — lighter item type for raw materials/consumables (not full GearItemSO)
- **PlayerSkills:** `Scripts/Skilling/PlayerSkills.cs` — tracks XP/level per SkillType, AddXP with auto-level-up
- **MaterialInventory:** `Scripts/Skilling/MaterialInventory.cs` — dictionary-based material tracking, Add/Has/Consume
- **CraftingStation:** `Scripts/Skilling/CraftingStation.cs` — Interactable subclass on buildings, opens CraftingUI
- **ResourceNode:** `Scripts/Skilling/ResourceNode.cs` — Interactable subclass, gather material + grant XP, respawns on cooldown (8s)
- **CraftingUI:** `Scripts/Skilling/CraftingUI.cs` — recipe list, ingredient check, craft button, skill level/XP display
- **PlayerInteractor:** `Scripts/Skilling/PlayerInteractor.cs` — proximity-based Interactable detection on player
- **12 Homestead Buildings:** All placed in scene with low-poly Blender models (Hut, Stall, Garden, Tower, Pool, Portal, Chest, Shrine). Forge/Campfire/Garden have CraftingStation components. Others are visual stubs.
- **Resource Nodes:** 2 herb patches (Gathering), 1 fishing spot, 1 iron deposit — all with ResourceNode + respawn
- **Test Recipes:** Cook Fish (Cooking Lv1), Smelt Iron Ore (Smithing Lv1), Forge Iron Sword (Smithing Lv3)
- **Test Materials:** Wild Herb, Raw Fish, Iron Ore, Cooked Fish, Iron Ingot
- **Stubbed buildings (functional later):** Watchtower, Merchant, Shrine, Pool, Fast Travel Portal, Storage Chest, 3 Guilds

## Phase 4 Log (completed 2026-06-17)
- **LootTableSO:** `Scripts/Data/LootTableSO.cs` — full implementation: rarity weights array, gear pool, gold/shard ranges, zone modifier, RollRarity/RollGear/RollGold/RollVoidShards methods
- **Architecture:** Shared loot tables referenced per EnemyDefinitionSO (WeakLoot/StandardLoot/EliteLoot). Tier scaling via different rarity weight distributions per table. Zone modifier field exists, tested at 1.0.
- **LootDropper:** `Scripts/Combat/LootDropper.cs` — attached to enemies, called on death, rolls loot table, spawns WorldPickup for gear, adds currency directly
- **WorldPickup:** `Scripts/Combat/WorldPickup.cs` — sphere primitive with rarity-colored material + RarityVisualEffects, spins, auto-pickup on proximity (1.5 units), adds to PlayerInventory
- **PlayerCurrency:** `Scripts/Inventory/PlayerCurrency.cs` — Gold + Void Shards tracking, AddGold/AddVoidShards/SpendGold, OnCurrencyChanged event
- **Currency HUD:** Added to top-left stats panel, live-updates via HUDManager
- **Pickup Feedback:** FloatingDamageNumber.SpawnText reused for "+X Gold" / "+X Void Shards" / item name on pickup
- **Enemy Tiers:** 3 EnemyDefinitionSO assets (Goblin Scout/Weak, Goblin Warrior/Standard, Goblin Champion/Elite) with color-tinted materials
- **Loot Table Assets:** WeakLoot (30% gear, 2-8 gold), StandardLoot (50% gear, 5-15 gold, 0-1 shards), EliteLoot (70% gear, 10-30 gold, 1-3 shards)

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

## Visual Self-Verification Protocol (Standing Rule)
**Applies to ANY task involving UI layout, visual styling, sizing, colors, or icons — supersedes the generic Play-Mode self-test for visual work.**

For ANY task that builds or modifies UI/visual elements, before reporting complete:
1. Use Unity MCP to capture an actual screenshot of the Game view in Play Mode (not just query object properties)
2. Look at the screenshot yourself and check it against the written spec point by point:
   - Are panels the expected approximate size relative to screen width?
   - Are all icons/text actually visible (not blank squares, not invisible due to font/color issues)?
   - Do colors match the specified hex values (rarity borders, stat colors, etc.)?
   - Is the layout structure correct (correct column order, correct grouping)?
3. If ANY checklist item fails, diagnose and fix, then re-screenshot and re-check. Repeat up to 3 times internally.
4. Only report back once the screenshot actually matches the spec. If after 3 attempts it still doesn't match, report honestly with the screenshot AND what's still wrong.

**Test Resolution:** Set Game view to FIXED 1920x1080 (not Free Aspect) before capturing verification screenshots.

**Icon Rendering — Standing Fix:** Unicode symbols (⛨ ⬡ ⚔ etc.) do NOT render in TMP's default font (Liberation Sans). For icons:
- Do NOT use Unicode symbols in TMP text fields
- Use plain ASCII abbreviations (flagged as temporary), OR actual icon sprites as Texture2D/Sprite, OR a proper TMP Sprite Asset
- Verify via screenshot that icons actually render visibly
