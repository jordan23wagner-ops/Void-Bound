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
Weapon, **Offhand**, Head, Cape, Neck, Body, Legs, Hands, Feet, Ring, Ammo
*(Offhand by class: **Shield** (melee) · **Quiver** (ranger) · **Mage's Book** (mage) — the ranger/mage offhands are untradables that reduce ammo use, §5.6.)*

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

**Ammo slot:** **Arrows** (bow/crossbow) and **Runes** (staff/wand) are consumed **per shot / cast**, crafted (§5.2), and stocked as prep (§5.7). The offhand **Quiver** / **Mage's Book** reduce that consumption (§5.6).

---

## 2A. Inventory & Backpack

- **Base inventory: 8 slots.**
- Expanded by **Backpack Upgrade Kits** — rare drops from *any* gather action (fishing, woodcutting, gathering, mining) or *any* monster kill.
- **Four kits, each rarer than the last, each +4 slots:**

| Kit | Grade | Slots after |
|---|---|---|
| Backpack Upgrade Kit I | Magic | 12 |
| Backpack Upgrade Kit II | Rare | 16 |
| Backpack Upgrade Kit III | Epic | 20 |
| Backpack Upgrade Kit IV | Legendary | 24 |

Max inventory is **24 slots** (8 + 4×4). Because nearly everything you carry drops on death (§4A), a bigger pack is *both* more carrying power *and* more to lose — every upgrade raises your ceiling and your risk.

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

**Status effects:** for now the only status is **Poison** — a damage-over-time effect some enemies inflict, cured by **Antidote** potions (§5.3). *(Bleed / curse / others may join later.)* Which enemies apply Poison is TBD.

Loot quality scales with tier. Drop tables remain zone-tiered (per RunePortal architecture) — deeper zones shift rarity odds upward.

**Monster drops (all tiers):** equipment · gold · **Void Shards** (Elite+) · crafting materials & the 20 Alchemy secondaries (§5.2) · and *items* — bigger backpacks (inventory expansion), recipes, tools, and ready-made consumables (raw fish, potions). A lucky drop can let a light-prepped player snowball (§5.5). Drop tables are ScriptableObject-driven (§9).

---

## 4A. Death & Recovery

Death is the teeth behind the prep loop (§5.5): punishing enough that a run's haul genuinely matters, but structured so you can never brick your own progression.

**On death:**
- **Respawn at the Homestead.**
- A **gravestone** appears where you fell, **instanced to you** — this is PvE, so no other player can loot it. The only barrier to recovery is the monsters you must fight back through.
- You **auto-keep your 3 most valuable items** (tunable). A "kept on death" preview shows exactly what you'd keep, so you can weigh a risky push *before* committing.
- **Everything else you carry drops to the grave** — including equipped gear, loot, materials, consumables, and unbanked gold / Void Shards.

**Recovery — the death-run:**
- Fast-travel back to the zone and fight your way to the grave to reclaim everything.
- **No real-time timer** — the grave persists, take as long as you need.
- **The catch:** only **one grave** exists at a time. **Die again before recovering it, and the previous grave's contents are lost for good.** Greed on the recovery run is how you actually lose things — the further/deeper the grave, the deadlier the trek back.

**Safety net — no soft-locks:**
- **Tools and untradables** lost this way are **reclaimable at a Homestead NPC for a gold fee** (scales with item value / zone depth). You can never permanently lose the ability to skill or fight — you just pay for the mistake. Doubles as a **gold sink** (§8).
- **Tradable** gear, loot, materials, and currency lost this way are **genuinely gone.**

**Always safe, regardless:** everything **banked** at the Homestead (stored gear, bank gold / Void Shards) and your **3 auto-kept items**.

This makes depth a deliberate wager: the more you carry, the longer and deadlier the recovery — so *bank often vs. press your luck* becomes a moment-to-moment decision (§5.5).

---

## 5. Skilling System — tool-gated, no levels

**Skills are not leveled.** There is no skilling XP, level cap, or 99 grind. What you can catch, chop, gather, mine, or make is gated entirely by your **tool tier**: each gather skill has its own **9-tier tool ladder** (one grade per rarity, Common → Void), and each tool tier unlocks the **next of 9 resource ranks** — a **1:1 ladder**, tools and resources sharing the rarity spine. *(The four **combat** stats — VIG/STR/DEX/INT — still level via combat XP per §3; that is the game's one leveling axis. "No levels" applies only to the skills below.)*

> Supersedes the earlier "banded / 10-rank" note — it is now **9 ranks, 1:1** with the 9-tier tool ladders.

**Eight skills, four gather → produce pairs.** Tools are made and upgraded through **Crafting**, which makes Crafting the spine that advances every gather skill.

### 5.1 Fishing → Cooking
- **Fishing** — the Rod ladder gates which of 9 fish bite. Field-based water nodes (lake / dock).
- **Cooking** — cook raw fish at the **Campfire** into **heal-over-time** food. HoT scales **5 → 21** (+2 per fish rank). Backbone of the prep loop.

| # | Fish *(placeholder names)* | HoT |
|---|---|---|
| 1 | Minnow | 5 |
| 2 | Sardine | 7 |
| 3 | Trout | 9 |
| 4 | Bass | 11 |
| 5 | Pike | 13 |
| 6 | Salmon | 15 |
| 7 | Obsidian Eel | 17 |
| 8 | Radiant Koi | 19 |
| 9 | Voidfin | 21 |

### 5.2 Woodcutting → Crafting
- **Woodcutting** — the Axe ladder gates which of 9 logs you can fell.
- **Crafting** — the hub skill. From logs (+ metal bars from Smithing + refined materials):
  - **Tools** — every gather ladder (axes, rods, pickaxes, sickles); this is how all skill tiers advance.
  - **Ranger bows** and **Arrows** (ammo); **Runes** (mage ammo — §5.6).
  - **Untradables** (via crafting recipes), **planks / parts**.
  - **Refined materials** — inputs to Alchemy, and the fuel for **upgrading untradables** (§5.6).

### 5.3 Gathering → Alchemy
- **Gathering** — the Sickle ladder gates which of 9 flora (flowers, roots, plants) you can harvest. Field nodes + the **Garden**.
- **Secondaries** — 20 reagents from gathering **and** monster drops, in two bands: **~8 staples** drop broadly (gathering + common monsters) and feed base / low-tier potions; **~12 premium** secondaries are **zone-tiered** — dropped by tougher enemies in deeper zones — and gate the top-tier potions. Early potions are easy to keep stocked; the best ones demand pushing deeper. *(Full 20-name list + per-zone mapping TBD — resolves as zones are built.)*
- **Alchemy** — brew potions at the **Garden** from flora + secondaries (+ refined materials). Potions are **portable, stackable** field consumables (unlike the home-only Shrine / Pool buffs, §6). Each family scales **9 tiers** on the flora/rarity spine with **flat temporary boosts**.

**Potion families:**

| Family | Effect |
|---|---|
| **Health** | Burst / emergency heal (vs food's slow HoT) |
| **Warrior** | Temporary **STR** (melee damage) boost |
| **Ranger** | Temporary **DEX** (ranged damage) boost |
| **Mage** | Temporary **INT** (magic damage) boost |
| **Warding** | Temporary defense / damage resistance |
| **Swiftness** | Temporary movement speed (kiting + death-runs) |
| **Antidote** | Cures **Poison** (§4) |
| **Prospector's** | Temporarily boosts drop rate / Void Shard find |

*(No Recall / teleport potion — dying in the field keeps its full weight, §4A. Magnitudes and durations are tuning TBD; Antidote may not need the full 9-tier scaling.)*

### 5.4 Mining → Smithing
- **Mining** — the Pickaxe ladder gates which of 9 ore nodes you can mine. Field nodes.
- **Smithing** — smelt ore into bars at the **Furnace / Forge**, then forge **untradable** equipment (the base items that later climb the ladder, §5.6) and upgrade components.

### 5.5 Tool ladders (the 9 grades)

| Rarity grade | Woodcutting (Axe) | Fishing (Rod) | Mining (Pickaxe) | Gathering (Sickle) |
|---|---|---|---|---|
| Common | Flimsy Axe | Old Fishing Rod | Flimsy Pickaxe | Flimsy Sickle |
| Uncommon | Woodcutting Axe | Fishing Rod | Pickaxe | Sickle |
| Magic | Hunter's Axe | Expert's Fishing Rod | Miner's Pickaxe | Herbalist's Sickle |
| Rare | Master Woodcutting Axe | Master Fishing Pole | Master Pickaxe | Master Sickle |
| Epic | Magic Axe | Magic Fishing Pole | Magic Pickaxe | Magic Sickle |
| Legendary | Enchanted Woodcutting Axe | Enchanted Fishing Rod | Enchanted Pickaxe | Enchanted Sickle |
| Obsidian | Obsidian Woodcutting Axe | Obsidian Rod | Obsidian Pickaxe | Obsidian Sickle |
| Radiant | Radiant Woodcutting Axe | Radiant Rod | Radiant Pickaxe | Radiant Sickle |
| Void | Void Axe | Void Pole | Void Pickaxe | Void Sickle |

**Resource ranks** *(placeholder names — the 9 harvestables each tool ladder unlocks):*

| Rank | Log (Woodcutting) | Ore (Mining) | Flora (Gathering) |
|---|---|---|---|
| Common | Kindling | Copper | Clover |
| Uncommon | Pinewood | Tin | Sage |
| Magic | Oak | Iron | Bramble |
| Rare | Birch | Silver | Foxglove |
| Epic | Ashwood | Gold | Nightshade |
| Legendary | Yew | Mithril | Moonpetal |
| Obsidian | Blackwood | Obsidian Ore | Obsidian Bloom |
| Radiant | Radiantwood | Radiant Ore | Radiant Lotus |
| Void | Voidwood | Void Ore | Voidflower |

*(Fish ranks are in §5.1; the 20 Alchemy secondaries are a separate pool, §5.3.)*

### 5.6 Untradables, upgrades & ammo

**Two gear tracks.** *Tradable* gear is found loot — it drops at whatever rarity it rolls, is tradable, and is fully at risk on death (the **gamble** track). *Untradable* gear is player-made, upgradeable, and reclaimable-for-a-fee on death (§4A) — your reliable **investment** backbone. Upgrading lives entirely on the untradable track; found gear is fixed at its rolled rarity.

**Untradables** are forged by **Smithing** (base item + bars) with **refined materials** from **Crafting**, and cannot be traded.

**Upgrading — ladder climb, time-vs-risk.** At the **Enchanted Chest** (§6) an untradable climbs the **9-tier rarity ladder** one tier per upgrade (Common → Void), visibly transforming via the rarity material treatment and gaining power. Each upgrade consumes **tier-appropriate refined materials** (higher target tier → higher-tier mats), gating progression behind your skilling:
- Starting an upgrade begins a **timer** (longer for higher tiers), running in **real time** (offline / idle-friendly).
- **Complete now** for an instant result, at a **success chance that rises the longer the timer has run** — early = riskier. On **failure you lose the materials** — never the item; it stays at its current tier, retry freely.
- **Wait for the timer to finish** for a guaranteed **100% success**, no material risk.
- The item is **never lost or downgraded** — only materials are ever at stake (no soft-locks).

*(Open knobs: concurrent-upgrade limit, whether gold / Void Shards can skip the timer, exact timer lengths + odds curve — tuning TBD.)*

**Ammo & offhands.** **Arrows** (bow/crossbow) and **Runes** (staff/wand) are **consumed per shot / cast** — crafted (§5.2), stocked as prep. Each class's **offhand** softens the drain: an untradable **Quiver** (ranger) or **Mage's Book** (mage) reduces ammo consumption and — being untradables — climbs the same upgrade ladder (higher tier = saves more). Melee uses the offhand for a **Shield**.

### 5.7 The prep-for-adventuring loop *(core tension)*
The heart of the game. Before a run the player chooses **how much to risk**:
- **Play it however you want — risky, safe, or balanced.** Prep is a *strong advantage, never a hard requirement.* You can push a deeper zone underprepared and simply be more likely to die — or get lucky with drops and snowball off them with little prep at all.
- **Underprepared → you die → you prep & plan → you survive and push deeper.** That loop is the point: time spent cooking food, brewing potions, stocking ammo, and upgrading tools/gear is what buys survival and depth.
- **Shortcut:** the **Merchant** at the Homestead sells low-tier **raw fish** and **healing potions** — **expensive**, but it lets you spend gold instead of time when you'd rather just go.

Fishing, Woodcutting, Gathering, and Mining are field-based resource nodes, not Homestead buildings (Phase 5 resolution).

---

## 6. Homestead Hub

Full 12-building hub carried from RunePortal `HS_BUILDINGS` (list resolved Phase 0, count corrected Phase 6 — this section previously said 9), re-skinned in low-poly Unity style. Three stations are being added for the new systems and **spread across the town** rather than clustered — the **Crafting Bench** (industry corner by the Forge), the **Enchanted Chest** (mystic quarter near the Shrine / Pool), and the **death-reclaim NPC** (by the Fast Travel Portal / respawn):

- **Forge** → Smithing (functional Phase 5)
- **Crafting Bench** → Crafting: tools, refined materials, bows, arrows, Runes, untradables (§5.2); industry corner by the Forge — *new, to build*
- **Campfire** → Cooking (functional Phase 5)
- **Garden** → Gathering + Alchemy (functional Phase 5)
- **Merchant** → buy/sell shop, data-driven stock (functional Phase 6). Also stocks low-tier **raw fish** and **healing potions** — expensive, a gold-for-time shortcut to self-prepping (§5.5)
- **Storage Chest** → bank: 48-slot gear storage with deposit/withdraw (functional Phase 6)
- **Enchanted Chest** → upgrades untradables via the time-vs-risk timer using refined materials (§5.6); mystic quarter near the Shrine / Pool — *new, to build*
- **Pool of Refreshment** → full heal + timed all-stat buff, cooldown; 4 upgrade tiers as data, tier 1 live (functional Phase 6)
- **Shrine** → gold offering for a timed STR/INT blessing, cooldown (functional Phase 6; +% damage from RunePortal adapted to flat stats)
- **Warriors' / Rangers' / Mages' Guilds** → combat stat training (STR / DEX / INT + 50% VIG side-XP), gold cost scaling with level (functional Phase 6)
- **Fast Travel Portal** → destination UI in place; actual travel lands with the zone phases
- **Reclaimer** (death-reclaim NPC) → buy back lost tools / untradables for a gold fee (§4A); by the Portal / respawn point — *new, to build*
- **Watchtower** → flavor stub; real function TBD in a future phase (RunePortal source unavailable)

Fishing, Woodcutting, Gathering, and Mining are field-based resource nodes (Phase 5 resolution), not Homestead buildings.

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
- **Gold** — primary currency, dropped by enemies. Sinks: vendor trades/repairs, prep restocks (§6), and death-reclaim fees for lost tools/untradables (§4A)
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
| 5 | Skilling systems (8 skills, tool-gated) + Homestead crafting stations |
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
