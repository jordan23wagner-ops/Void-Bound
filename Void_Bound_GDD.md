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
| Core loop | Prep loadout → explore → fight → loot → return to Homestead → cook / brew / craft / upgrade / restock → push deeper. **Risk is chosen, not forced** (§5.5) |

---

## 2. Gear & Equipment System

**Equipment Slots (11 — full RunePortal depth):**
Weapon, Shield, Head, Cape, Neck, Body, Legs, Hands, Feet, Ring, Ammo

**Weapon Types (8, carried from RunePortal):**
Sword, Axe, Spear, Mace, Bow, Crossbow, Staff, Wand *(confirm/adjust during Phase 3)*

**Rarity Tiers (9, canonical — CONFIRMED):**
Common → Uncommon → Magic → Rare → Epic → Legendary → Obsidian → Radiant → Void

**Rarity colours (canonical, source of truth):**
Common grey · Uncommon white · Magic blue · Rare yellow · Epic purple · Legendary orange · Obsidian blackish-white (cool silver) · Radiant reddish-white (rose) · Void purple/black. Implemented in `RarityVisualEffects.GetRarityColor`.

**Visual rarity language (Unity translation of Three.js glow system):**
- Common/Uncommon: flat color, no emission
- Magic+: emissive material trim (URP Emission channel), color-coded per tier
- Legendary+: particle accent (simple URP particle system — sparkle/aura), subtle idle animation on equipped item

**Set system:** Carried from RunePortal — multi-piece bonuses for themed gear sets.

---

## 3. Character Stats

Each of the 4 core stats — **VIG, STR, DEX, INT** — is an individually **leveled** skill (not a flat modifier), on a Level 99 / 3× XP curve. *(Combat is the game's one leveling axis — the gathering/crafting skills are tool-gated with no levels, §5.)*

**Display order (top to bottom): VIG, STR, DEX, INT** — matches RunePortal's original HUD layout.

- **VIG** — health pool, defense. Levels passively off combat (see XP model below)
- **STR** — physical damage with STR-style weapons (Sword, Axe, Mace — heavy melee)
- **DEX** — physical damage with DEX-style weapons (Spear, Bow, Crossbow — agile/ranged), also influences attack speed/crit chance
- **INT** — magic damage with INT-style weapons (Staff, Wand)

*(Weapon-to-stat mapping above is a proposed default — confirm against RunePortal source if accessible, since RunePortal already implemented this exact 4-stat system.)*

**XP Model — damage-based, hybrid/tribrid training:**
- Dealing damage with a STR-style weapon grants STR XP (proportional to damage dealt)
- Dealing damage with a DEX-style weapon grants DEX XP (proportional to damage dealt)
- Dealing damage with an INT-style weapon/spell grants INT XP (proportional to damage dealt)
- A player can mix weapon styles freely (hybrid/tribrid combat) — each style trains its own stat independently and simultaneously
- **VIG levels passively**: whenever STR/DEX/INT gain XP from combat, VIG gains a fraction of that same XP (default 33%, mirrors OSRS's Hitpoints ratio) — this is *why* VIG levels slower, not an independent training method
- Exact ratios/multipliers are tunable — confirm against RunePortal source if the original implementation is accessible

**Character Level:** Derived from all 4 combat stat levels combined (default formula: average of VIG/STR/DEX/INT levels, rounded — confirm against RunePortal source if a different formula was used there).

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

**Monster drops (all tiers):** equipment · gold · **Void Shards** (Elite+) · crafting materials & the 20 Alchemy secondaries (§5.2) · and *items* — bigger backpacks (inventory expansion), recipes, tools, and ready-made consumables (raw fish, potions). A lucky drop can let a light-prepped player snowball (§5.5). Drop tables are ScriptableObject-driven (§9).

---

## 5. Skilling System — tool-gated, no levels

**Skills are not leveled.** There is no skilling XP, level cap, or 99 grind. What you can catch, gather, mine, or make is gated entirely by your **tool / equipment tier** — upgrade the tool and the next rank of resource opens up. *(The four **combat** stats — VIG/STR/DEX/INT — still level via combat XP per §3. That is a separate axis; "no levels" applies only to the gathering/crafting skills below.)*

**Tool tiers — banded.** A handful of named grades (e.g. **Bronze → Iron → Steel → … → Void**), each unlocking a *band* of resource ranks rather than one at a time — clear milestones, fewer craft steps. Tools are made and upgraded through **Crafting** (§5.4), which makes Crafting the spine that advances every other skill.

The seven skills pair up — a **gather** skill feeds a **produce** skill:

### 5.1 Fishing → Cooking
- **Fishing** — 10 fish types of ascending rarity; rod tier gates what bites. Field-based water nodes (lake / dock).
- **Cooking** — cook raw fish at the **Campfire**. Cooked food grants a **heal-over-time** buff (rarer fish → stronger / longer HoT). Backbone of the prep loop.

### 5.2 Gathering → Alchemy
- **Gathering** — 10 primary flora (flowers, roots, plants); harvest gated by tool tier. Field nodes + the **Garden**.
- **Secondaries** — 20 secondary reagents, obtained **both** from gathering **and** as monster drops.
- **Alchemy** — combine primaries + secondaries (+ refined materials from Crafting) into potions.

### 5.3 Mining → Smithing
- **Mining** — 10 node types; pickaxe tier gates how deep / rare you can mine. Field nodes.
- **Smithing** — smelt ore into bars at the **Furnace / Forge**, then smith bars into **gear upgrades** and **untradable** equipment.

### 5.4 Crafting
- Crafts and **upgrades untradables**.
- Makes **tools** — fishing rods, hatchets, pickaxes, etc. — which is how you raise every other skill's tier.
- **Refines** raw materials into Alchemy inputs.

### 5.5 The prep-for-adventuring loop *(core tension)*
The heart of the game. Before a run the player chooses **how much to risk**:
- **Play it however you want — risky, safe, or balanced.** Prep is a *strong advantage, never a hard requirement.* You can push a deeper zone underprepared and simply be more likely to die — or get lucky with drops and snowball off them with little prep at all.
- **Underprepared → you die → you prep & plan → you survive and push deeper.** That loop is the point: time spent cooking food, brewing potions, and upgrading tools/gear is what buys survival and depth.
- **Shortcut:** the **Merchant** at the Homestead sells low-tier **raw fish** and **healing potions** — **expensive**, but it lets you spend gold instead of time when you'd rather just go.

Fishing and Mining are field-based resource nodes, not Homestead buildings (Phase 5 resolution).

---

## 6. Homestead Hub

Full 12-building hub carried from RunePortal `HS_BUILDINGS` (list resolved Phase 0, count corrected Phase 6 — this section previously said 9), re-skinned in low-poly Unity style:

- **Forge** → Smithing (functional Phase 5)
- **Campfire** → Cooking (functional Phase 5)
- **Garden** → Gathering + Alchemy (functional Phase 5)
- **Merchant** → buy/sell shop, data-driven stock (functional Phase 6). Also stocks low-tier **raw fish** and **healing potions** — expensive, a gold-for-time shortcut to self-prepping (§5.5)
- **Storage Chest** → bank: 48-slot gear storage with deposit/withdraw (functional Phase 6)
- **Pool of Refreshment** → full heal + timed all-stat buff, cooldown; 4 upgrade tiers as data, tier 1 live (functional Phase 6)
- **Shrine** → gold offering for a timed STR/INT blessing, cooldown (functional Phase 6; +% damage from RunePortal adapted to flat stats)
- **Warriors' / Rangers' / Mages' Guilds** → combat stat training (STR / DEX / INT + 50% VIG side-XP), gold cost scaling with level (functional Phase 6)
- **Fast Travel Portal** → destination UI in place; actual travel lands with the zone phases
- **Watchtower** → flavor stub; real function TBD in a future phase (RunePortal source unavailable)

Fishing and Mining are field-based resource nodes (Phase 5 resolution), not Homestead buildings.

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

*(Recommend, confirm during Phase 0):*
- **Gold** — primary currency, dropped by enemies, used for vendor trades/repairs
- **Void Shards** — secondary currency, earned from Elite+ tier kills and bosses, used for premium crafting/upgrades (thematically ties to "Void Bound"/"Void Throne")

---

## 9. Unity Architecture (ScriptableObject-driven)

To match Claude Code's strengths and minimize C# errors, all data-driven systems use ScriptableObjects:

- `GearItemSO` — slot, weapon type, rarity, stat modifiers, visual prefab ref, set ID
- `EnemyDefinitionSO` — tier, base stats, loot table ref, visual prefab, behavior type
- `ZoneDefinitionSO` — enemy spawn tables, ambiance, connected zones
- `SkillDefinitionSO` — tool-tier bands, resource ranks, station + recipe unlocks (no XP curve — §5)
- `RecipeDefinitionSO` — inputs, outputs, required tool tier, required station
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
| 6 | Homestead full build-out (all 12 buildings, low-poly art pass) |
| 7 | Zone 2: Ashfields (Weak/Standard enemies, first loot loop) |
| 8 | Zone 3: Bleakwood (Elite/Rare Elite tiers, first Mini Boss) |
| 9 | Polish — animation, VFX, UI/UX, audio, mobile optimization |

---

## 11. Open Items (resolve before/during Phase 0)

- Confirm exact 8 weapon types match RunePortal's GEAR_DB
- Confirm 9th rarity tier name (top of ladder)
- Confirm remaining 2-3 Homestead buildings
- Lock **combat** level cap + XP curve values (gathering/crafting skills are tool-gated, no XP — §5)
- Confirm currency names/icons (Gold + Void Shards proposed)
