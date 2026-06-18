# Void Bound — Game Design Document

**Engine:** Unity 6.5 (URP) | **Platform:** Mobile-first, desktop-tested | **Style:** Low-poly 3D, fixed isometric, warm color palette
**Lineage:** Evolved from RunePortal (Three.js) — full system depth carried forward, visual style rebuilt for Unity

---

## 1. Core Pillars

| Pillar | Decision |
|---|---|
| Camera | Fixed isometric, orthographic, no rotation |
| Input | Virtual joystick (mobile) for movement; keyboard equivalent for desktop testing. Combat is automatic — player auto-attacks enemies in range on an attack-speed cooldown, no manual attack button. Interactables (resources, NPCs, objects) are activated by tapping them directly or walking into them. |
| Visual style | Low-poly 3D, unlit/simple diffuse shaders, warm oranges/sandy browns + bright green/purple UI accents |
| Core loop | Explore zone → fight → loot → return to Homestead → craft/upgrade → push deeper |

---

## 2. Gear & Equipment System

**Equipment Slots (11 — full RunePortal depth):**
Weapon, Shield, Head, Cape, Neck, Body, Legs, Hands, Feet, Ring, Ammo

**Weapon Types (8, confirmed from RunePortal WEAPON_TYPES):**
Sword, Sword2H, Dagger, Mace, Bow, Crossbow, Staff, Wand

**Rarity Tiers (9, full depth):**
Common → Uncommon → Rare → Epic → Legendary → Mythic → Ascended → Eternal → Voidforged

**Visual rarity language (Unity translation of Three.js glow system):**
- Common/Uncommon: flat color, no emission
- Rare+: emissive material trim (URP Emission channel), color-coded per tier
- Legendary+: particle accent (simple URP particle system — sparkle/aura), subtle idle animation on equipped item

**Set system:** Carried from RunePortal — multi-piece bonuses for themed gear sets.

---

## 3. Character Stats

Simplified RPG stat block (Portal Quest style):
- **STR** — physical damage, carry capacity
- **DEX** — attack speed, crit chance
- **VIG** — health pool, defense
- **INT** — magic damage, mana pool

Stats scale via gear, skill levels, and zone-based progression. Level cap: **99 per skill** (OSRS-style, matches RunePortal lineage). XP curve: **3x multiplier** as starting value (tunable, not final).

---

## 4. Combat & Enemy Tiers

**8-tier enemy system (expanded from RunePortal's 3-tier prototype):**

| Tier | Notes |
|---|---|
| Weak | Common trash mobs, zone-appropriate |
| Standard | Baseline threat, most common spawn |
| Elite | Tougher variant, better loot table |
| Rare Elite | Low spawn chance, notably better loot |
| Named Elite | Unique enemy with a name, fixed loot pool |
| Mini Boss | Zone gatekeeper, moderate difficulty spike |
| Named Boss | Major zone boss, full mechanic set |
| World Boss | End-game/event-tier, highest loot ceiling |

Loot quality scales with tier. Drop tables remain zone-tiered (per RunePortal architecture) — deeper zones shift rarity odds upward.

---

## 5. Skilling System (7 skills, full depth)

1. **Fishing**
2. **Gathering** (plants, flowers, secondaries for crafting/alchemy)
3. **Mining**
4. **Smithing**
5. **Crafting**
6. **Cooking**
7. **Alchemy**

Each skill needs: XP curve, level-gated recipes/resources, associated Homestead crafting station.

---

## 6. Homestead Hub

12-structure hub carried from RunePortal (9 original buildings + 3 utility structures), re-skinned in low-poly Unity style:

**Core Buildings (9, skill mapping confirmed from RunePortal source):**
- **Campfire** → Cooking station + Rest/Recover
- **Forge** → Smithing station (smelting + gear crafting)
- **Shrine** → Blessings (buffs, gold cost)
- **Garden** → Gathering + Alchemy (potion crafting, GROW/BREW tabs)
- **Watchtower** → Survey the Land (zone info/map) — stub
- **Merchant** → Browse Wares (buy/sell) — stub
- **Warriors' Guild** → STR training (confirmed: stat training, not gear crafting)
- **Rangers' Guild** → DEX training
- **Mages' Guild** → INT training

**Field-Based Skills (not tied to a building):**
- **Fishing** → Resource nodes in zones/near water
- **Mining** → Rock deposits in zones

**Utility Structures (3):**
- **Pool of Refreshment** → buff/recovery station (4 upgrade tiers, cooldown system carried over)
- **Fast Travel Portal** → Teleport to discovered waypoints
- **Storage Chest** → 20-slot persistent homestead storage

Homestead = safe zone, no combat, full crafting/progression hub. This is the player's persistent base between runs.

---

## 7. World Structure (Phase 1 scope: 3 zones)

**Starting zone arc** (pulled from RunePortal's 10-zone map, first 3):

1. **Homestead** — hub, no combat (see Section 6)
2. **Ashfields** — first combat zone, Weak/Standard tier enemies, intro loot
3. **Bleakwood** — second combat zone, introduces Elite/Rare Elite tiers, first Mini Boss

*(Remaining 7 zones from RunePortal's map — Rot Flats, Ironbone/Bleakwood Depths, Rot Spire, Veilstone/Cindermaw, Ashen Crown, Void Throne — held for post-launch expansion phases.)*

---

## 8. Currency & Economy

- **Gold** — primary currency, dropped by enemies, used for vendor trades/repairs
- **Void Shards** — secondary currency, earned from Elite+ tier kills and bosses, used for premium crafting/upgrades (thematically ties to "Void Bound"/"Void Throne")

---

## 9. Unity Architecture (ScriptableObject-driven)

To match Claude Code's strengths and minimize C# errors, all data-driven systems use ScriptableObjects:

- `GearItemSO` — slot, weapon type, rarity, stat modifiers, visual prefab ref, set ID
- `EnemyDefinitionSO` — tier, base stats, loot table ref, visual prefab, behavior type
- `ZoneDefinitionSO` — enemy spawn tables, ambiance, connected zones
- `SkillDefinitionSO` — XP curve, unlockable recipes
- `RecipeDefinitionSO` — inputs, outputs, required skill level, required station
- `LootTableSO` — tier-weighted drop chances, zone modifiers

This mirrors RunePortal's data-driven gear/drop table approach almost exactly — same logic, Unity-native implementation.

---

## 10. Development Phases (maps to repo /PHASES/ folder)

| Phase | Scope |
|---|---|
| 0 | Project setup, URP config, isometric camera rig, folder structure, ScriptableObject scaffolding |
| 1 | Core movement (joystick input), placeholder low-poly character, basic Homestead scene |
| 2 | Combat system (attack button, enemy AI base, STR/DEX/VIG/INT damage calc) |
| 3 | Gear & Inventory (11 slots, equip/unequip, stat application, rarity visual tiers) |
| 4 | Loot & Drop tables (tier-weighted, zone-modified) |
| 5 | Skilling systems (7 skills) + Homestead crafting stations |
| 6 | Homestead full build-out (all 9 buildings, low-poly art pass) |
| 7 | Zone 2: Ashfields (Weak/Standard enemies, first loot loop) |
| 8 | Zone 3: Bleakwood (Elite/Rare Elite tiers, first Mini Boss) |
| 9 | Polish — animation, VFX, UI/UX, audio, mobile optimization |

---

## 11. Open Items

- ~~Confirm 9th rarity tier name~~ → **Voidforged** (resolved Phase 0)
- ~~Confirm remaining Homestead buildings~~ → **12 structures total** (resolved Phase 0, pulled from RunePortal HS_BUILDINGS)
- ~~Lock level cap + XP curve~~ → **99 per skill, 3x curve** (resolved Phase 0, tunable)
- ~~Confirm currency~~ → **Gold + Void Shards** (resolved Phase 0)
- ~~Confirm exact 8 weapon types~~ → **Sword, Sword2H, Dagger, Mace, Bow, Crossbow, Staff, Wand** (resolved Phase 3, pulled from RunePortal WEAPON_TYPES)
