# Void Bound — Visual Style Guide

**This is the quality bar for all visual work.** The **Archmage set** (deep-purple robe, gold trim, glowing gems, drooping wizard hat) set the standard; everything else — melee & ranger gear, enemies, the Homestead, and every UI — should be brought up to it. When you build or touch something visual, ship it at this level or flag that it's a placeholder.

The look is **stylized low-poly**, not realism. Detail comes from *shape language + a rich named-material palette*, not from polycount or textures.

---

## 1. The material system (colour comes from Unity, not the FBX)

Blender's `diffuse_color` **does not export to FBX** — every imported material lands flat gray. So colour is assigned at runtime in `EquipmentVisuals.TintMain` **by material name**. Author models using exactly these four material names and you get a full palette for free:

| Material name | Result in-game | Use for |
|---|---|---|
| `Main` | **Rarity tint** (Common gray … Epic purple … Legendary gold) | The primary fabric/metal of the piece |
| `Gold` | Warm gold `(0.85, 0.66, 0.20)` | Trim, rims, buckles, filigree |
| `Gem` | **Glowing cyan** `(0.40, 0.85, 1.0)`, emissive | Gemstones, runes, arcane focus points |
| `Accent` | Dark charcoal-violet `(0.16, 0.13, 0.20)` | Linings, plackets, straps, dark under-layers |

- **Rarity drives the hero colour.** Make showpiece gear `Epic` (purple) or `Legendary` (gold) so `Main` reads rich, not gray. Rarity is set on the `GearItemSO` in `StarterGearGenerator`.
- Need a new fixed colour (e.g. a red cloak)? Add a named branch to `TintMain` — don't rely on the FBX colour.
- `Gem` is emissive; use it sparingly (2–4 focal points) so it stays special.

## 2. Modelling principles (headless Blender, `Tools/build_equipment_models.py`)

1. **Silhouette first.** A piece must be recognizable in one glance as a black shape. The Archmage hat reads as "wizard" from its drooping cone alone; the robe from its flare. Get the silhouette right before adding detail.
2. **Layer primitives for richness.** Combine spheres/cones/boxes/cyls/tori into one piece: a base form + trim (`Gold` torus/box) + a gem or two + a dark `Accent` layer. ~8–18 primitives per showpiece piece is the right range.
3. **Faceted gems.** Low-segment spheres (`segments=4, rings=3`) read as cut gemstones. Use the `Gem` material.
4. **Curves via segments.** No smooth bends in primitives — fake them by stacking short cone segments with increasing tilt/offset (see the hat's 3-segment droop).
5. **Rig per-bone.** Armour spans limbs, so split each piece into one merged sub-mesh **per bone it covers** via `export_armor([(obj, "BoneName"), …])`. Name sub-parts for their bone (Chest, UpperArm_R/L, Hips, UpperLeg_R/L, Head, Hand_R/L, Foot_R/L). This makes it deform with the animation instead of clipping. Anything that spans the legs (skirts, long coats) → split panels on `UpperLeg_R/L` so it parts as you stride.
6. **Grip gear faces the model's forward.** Weapons/shields are grip-space on the hand bone; their pose is tuned per weapon type in `EquipmentVisuals` (live-tunable Inspector fields).

## 3. Applying the bar

### Gear (melee, ranger — bring up to Archmage level)
- **Melee / plate:** give it the same treatment — `Gold` edging on the plates, a `Gem` in the chestpiece/pommel, `Accent` under-layers, a bolder pauldron/helm silhouette. Make the showcase pieces `Epic`/`Legendary`.
- **Ranger / leather:** `Accent`-heavy leather with `Gold` buckles/clasps, a `Gem` on the hood or a quiver detail, a stronger hood silhouette. Class silhouette should differ clearly from plate and robe.
- Each class must be **identifiable at a glance by silhouette + palette**, not just by weapon.

### Enemies
- **Distinct species silhouettes** (not recoloured heroes). Goblins already differ; new species need their own build in `build_character_models.py`.
- **Tier flavour:** Weak → Standard → Elite should escalate visually — more gear, darker/richer tints, `Gold`/`Gem` accents on elites (a champion should *look* like a champion). Tie tint to `EnemyTier`.
- Bosses get the full showpiece treatment (unique silhouette, emissive `Gem` focal points).

### Homestead
- Buildings should share one **cohesive palette and trim language** (a warm base + `Gold`/`Accent` trim, `Gem`/emissive on magical stations — Pool, Shrine, Portal). Each of the 12 stations reads as a distinct, hand-crafted structure — no shared placeholder meshes.
- Props (resource nodes, decor) get the same low-poly-with-trim polish.

### UI (all panels — bring every screen up together)
- **One visual language via `Panel5cFactory`:** rounded 9-slice panels, soft drop shadows, a gold accent strip under bold gold titles, rounded rows/buttons with hover/pressed `ColorBlock` states.
- **Match the gear palette:** gold trim, dark-violet/charcoal panel fills, `Gem`-cyan glow for highlights and important callouts; **rarity-coloured borders** on item slots (reuse `RarityVisualEffects.GetRarityColor`).
- **Real iconography** — sprite/`Texture2D` icons, never Unicode symbols in TMP (they don't render). ASCII placeholders are a to-do, not a ship state.
- Clear hierarchy, generous spacing, legible at the 1280×720 reference (1.5× scale). Every panel gets the same care — no "functional but ugly" screens.

## 4. Ship checklist (before calling visual work done)

- [ ] Reads correctly as a **silhouette**.
- [ ] Uses the named-material palette (`Main`/`Gold`/`Gem`/`Accent`); rarity set so `Main` isn't gray.
- [ ] Rigged **per-bone** (armour) — verified it doesn't clip during Walk / a full stride.
- [ ] `Gem` glow present but restrained (focal points only).
- [ ] **Screenshot-verified in Play Mode** per [`VISUAL_VERIFICATION_PROTOCOL.md`](VISUAL_VERIFICATION_PROTOCOL.md) — checked against this guide, not just "it compiles."
- [ ] For UI: consistent `Panel5cFactory` language, real icons, rarity borders, legible.
