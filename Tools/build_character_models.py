# Void Bound — rigged + animated Hero and Goblin (headless Blender).
# Run:
#   & "C:\Program Files\Blender Foundation\Blender 5.1\blender.exe" -b -P Tools\build_character_models.py
# Each character is built from low-poly primitives, each primitive rigidly
# weighted (1.0) to one bone's vertex group — a "wooden puppet" skin that suits
# the blocky style and avoids bone-heat failures on primitive-union meshes.
# A shared 11-bone skeleton (Hips/Chest/Neck/Head, UpperArm+Hand L/R,
# UpperLeg+Foot L/R) carries 5 baked animation clips: Idle, Walk, Attack, Hit,
# Death. Exported FBX imports to Unity as a SkinnedMeshRenderer + Generic rig +
# 5 clips. Material slot names (HeroSkin/HeroArmor/HeroHair, GoblinSkin/
# GoblinCloth) are preserved so CharacterModelSwap's per-slot tinting still works.

import bpy
import math

def rad(d):
    return math.radians(d)

def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def mat(name, rgba):
    m = bpy.data.materials.get(name)
    if m is None:
        m = bpy.data.materials.new(name)
        m.diffuse_color = rgba
    return m

# ── Primitive builders that also assign the whole part to a bone group ──
def _finish(ob, m, group):
    ob.data.materials.append(m)
    vg = ob.vertex_groups.new(name=group)
    vg.add(range(len(ob.data.vertices)), 1.0, 'REPLACE')
    return ob

def sph(m, group, r, loc, scale=(1, 1, 1), seg=8, ring=6):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=ring, radius=r, location=loc, scale=scale)
    return _finish(bpy.context.active_object, m, group)

def box(m, group, loc, scale, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, scale=scale, rotation=rot)
    return _finish(bpy.context.active_object, m, group)

def cyl(m, group, r, d, loc, rot=(0, 0, 0), v=8):
    bpy.ops.mesh.primitive_cylinder_add(vertices=v, radius=r, depth=d, location=loc, rotation=rot)
    return _finish(bpy.context.active_object, m, group)

def cone(m, group, r1, r2, d, loc, rot=(0, 0, 0), v=8):
    bpy.ops.mesh.primitive_cone_add(vertices=v, radius1=r1, radius2=r2, depth=d, location=loc, rotation=rot)
    return _finish(bpy.context.active_object, m, group)

def join(parts, name):
    bpy.ops.object.select_all(action='DESELECT')
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    ob = bpy.context.active_object
    ob.name = name
    return ob

def build_armature(bones):
    arm_data = bpy.data.armatures.new("Rig")
    arm = bpy.data.objects.new("Rig", arm_data)
    bpy.context.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='EDIT')
    eb = arm_data.edit_bones
    created = {}
    for name, head, tail, parent in bones:
        b = eb.new(name)
        b.head = head
        b.tail = tail
        if parent:
            b.parent = created[parent]
        created[name] = b
    bpy.ops.object.mode_set(mode='OBJECT')
    return arm

def bind(body, arm):
    body.parent = arm
    m = body.modifiers.new("Armature", 'ARMATURE')
    m.object = arm
    m.use_vertex_groups = True

# ── Shared 5-clip animation set (deltas from rest; bone names shared) ──
def author_animations(arm):
    arm.animation_data_create()
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='POSE')
    for pb in arm.pose.bones:
        pb.rotation_mode = 'XYZ'

    def new_action(name):
        act = bpy.data.actions.new(name)
        arm.animation_data.action = act
        for pb in arm.pose.bones:
            pb.rotation_euler = (0, 0, 0)
            pb.location = (0, 0, 0)
        return act

    def key(bone, frame, ex=0.0, ey=0.0, ez=0.0):
        pb = arm.pose.bones[bone]
        pb.rotation_euler = (rad(ex), rad(ey), rad(ez))
        pb.keyframe_insert("rotation_euler", frame=frame)

    def key_loc(bone, frame, z):
        pb = arm.pose.bones[bone]
        pb.location = (0, 0, z)
        pb.keyframe_insert("location", frame=frame)

    # IDLE — gentle breathing bob, 60f loop
    new_action("Idle")
    for b in ("Chest", "UpperArm_L", "UpperArm_R"):
        key(b, 1, 0); key(b, 30, 3 if b == "Chest" else 4); key(b, 60, 0)

    # WALK — legs swing opposite, arms counter-swing, hip bob, 30f loop
    new_action("Walk")
    key("UpperLeg_L", 1, 30);  key("UpperLeg_L", 15, -30); key("UpperLeg_L", 30, 30)
    key("UpperLeg_R", 1, -30); key("UpperLeg_R", 15, 30);  key("UpperLeg_R", 30, -30)
    key("UpperArm_L", 1, -22); key("UpperArm_L", 15, 22);  key("UpperArm_L", 30, -22)
    key("UpperArm_R", 1, 22);  key("UpperArm_R", 15, -22); key("UpperArm_R", 30, 22)
    key_loc("Hips", 1, 0); key_loc("Hips", 8, -0.05); key_loc("Hips", 22, -0.05); key_loc("Hips", 30, 0)

    # ATTACK — wind up, torso twist, diagonal overhead chop with follow-through
    # and recover, 26f once. Off-hand counter-swings and the hips drop weight on
    # impact so it reads as a committed strike rather than a limp arm wave.
    new_action("Attack")
    key("UpperArm_R", 1, 0); key("UpperArm_R", 5, -95); key("UpperArm_R", 12, 105); key("UpperArm_R", 16, 98); key("UpperArm_R", 26, 0)
    key("Hand_R", 1, 0); key("Hand_R", 5, -25); key("Hand_R", 12, 45); key("Hand_R", 26, 0)
    key("UpperArm_L", 1, 0); key("UpperArm_L", 12, -26); key("UpperArm_L", 26, 0)
    key("Chest", 1, 0); key("Chest", 5, 0, 0, 16); key("Chest", 12, -12, 0, -26); key("Chest", 26, 0)
    key("Head", 1, 0); key("Head", 12, -8); key("Head", 26, 0)
    key_loc("Hips", 1, 0); key_loc("Hips", 12, -0.05); key_loc("Hips", 26, 0)

    # HIT — sharp recoil: head + chest snap back, arms fling, a flinch crouch,
    # then a small forward overshoot before settling, 18f once.
    new_action("Hit")
    key("Chest", 1, 0); key("Chest", 3, -30, 0, 8); key("Chest", 8, 8, 0, -3); key("Chest", 18, 0)
    key("Head", 1, 0); key("Head", 3, -38); key("Head", 9, 10); key("Head", 18, 0)
    key("UpperArm_L", 1, 0); key("UpperArm_L", 3, -30); key("UpperArm_L", 18, 0)
    key("UpperArm_R", 1, 0); key("UpperArm_R", 3, -24); key("UpperArm_R", 18, 0)
    key_loc("Hips", 1, 0); key_loc("Hips", 3, -0.07); key_loc("Hips", 18, 0)

    # DEATH — topple backward and hold, 32f once
    new_action("Death")
    key("Hips", 1, 0); key("Hips", 20, -88); key("Hips", 32, -88)
    key("Chest", 1, 0); key("Chest", 20, -20); key("Chest", 32, -20)
    key("UpperArm_L", 1, 0); key("UpperArm_L", 20, -40); key("UpperArm_L", 32, -40)
    key("UpperArm_R", 1, 0); key("UpperArm_R", 20, -40); key("UpperArm_R", 32, -40)

    # SHOOT — raise the bow arm to aim, off-hand draws the string then releases
    # forward, with a slight torso twist toward the target, 24f once.
    new_action("Shoot")
    key("UpperArm_R", 1, 0); key("UpperArm_R", 6, 82); key("UpperArm_R", 20, 80); key("UpperArm_R", 24, 0)
    key("UpperArm_L", 1, 0); key("UpperArm_L", 6, 68); key("UpperArm_L", 12, 52); key("UpperArm_L", 14, 92); key("UpperArm_L", 24, 0)
    key("Hand_L", 1, 0); key("Hand_L", 12, -25); key("Hand_L", 14, 8); key("Hand_L", 24, 0)
    key("Chest", 1, 0); key("Chest", 8, 0, 0, 10); key("Chest", 24, 0)

    # CAST — raise the staff hand back, then thrust it forward to release the
    # spell; the torso leans back then drives into the cast, 26f once.
    new_action("Cast")
    key("UpperArm_R", 1, 0); key("UpperArm_R", 7, -72); key("UpperArm_R", 15, 55); key("UpperArm_R", 20, 46); key("UpperArm_R", 26, 0)
    key("Hand_R", 1, 0); key("Hand_R", 15, 35); key("Hand_R", 26, 0)
    key("UpperArm_L", 1, 0); key("UpperArm_L", 10, -26); key("UpperArm_L", 26, 0)
    key("Chest", 1, 0); key("Chest", 9, -10); key("Chest", 16, 8); key("Chest", 26, 0)

    bpy.ops.object.mode_set(mode='OBJECT')

def export(arm, body, filename):
    import os
    out = os.path.join(r"C:\Users\Jordon\Void Bound\Assets\Art\Models", filename)
    bpy.ops.object.select_all(action='DESELECT')
    arm.select_set(True)
    body.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.export_scene.fbx(
        filepath=out,
        use_selection=True,
        add_leaf_bones=False,
        axis_forward='-Z',
        axis_up='Y',
        bake_space_transform=False,   # off for rigged meshes (armature safety)
        mesh_smooth_type='OFF',
        use_armature_deform_only=True,
        bake_anim=True,
        bake_anim_use_all_actions=True,
        bake_anim_use_nla_strips=False,
        bake_anim_simplify_factor=0.0,
    )
    print(f"[Models] Exported {out}")

# Shared skeleton (values overridden per body via a scale/offset closure)
def hero_bones():
    return [
        ("Hips", (0, 0, 0.84), (0, 0, 1.00), None),
        ("Chest", (0, 0, 1.00), (0, 0, 1.40), "Hips"),
        ("Neck", (0, 0, 1.40), (0, 0, 1.52), "Chest"),
        ("Head", (0, 0, 1.52), (0, 0, 1.78), "Neck"),
        ("UpperArm_R", (0.28, 0, 1.37), (0.30, 0, 0.90), "Chest"),
        ("Hand_R", (0.30, 0, 0.90), (0.30, 0, 0.70), "UpperArm_R"),
        ("UpperArm_L", (-0.28, 0, 1.37), (-0.30, 0, 0.90), "Chest"),
        ("Hand_L", (-0.30, 0, 0.90), (-0.30, 0, 0.70), "UpperArm_L"),
        ("UpperLeg_R", (0.11, 0, 0.84), (0.11, 0, 0.15), "Hips"),
        ("Foot_R", (0.11, 0, 0.15), (0.11, 0.20, 0.02), "UpperLeg_R"),
        ("UpperLeg_L", (-0.11, 0, 0.84), (-0.11, 0, 0.15), "Hips"),
        ("Foot_L", (-0.11, 0, 0.15), (-0.11, 0.20, 0.02), "UpperLeg_L"),
    ]

def goblin_bones():
    return [
        ("Hips", (0, 0, 0.35), (0, 0.03, 0.60), None),
        ("Chest", (0, 0.03, 0.60), (0, 0.08, 0.88), "Hips"),
        ("Neck", (0, 0.08, 0.88), (0, 0.10, 0.96), "Chest"),
        ("Head", (0, 0.10, 0.96), (0, 0.16, 1.28), "Neck"),
        ("UpperArm_R", (0.28, 0.05, 0.82), (0.36, 0.08, 0.46), "Chest"),
        ("Hand_R", (0.36, 0.08, 0.46), (0.36, 0.08, 0.36), "UpperArm_R"),
        ("UpperArm_L", (-0.28, 0.05, 0.82), (-0.36, 0.08, 0.46), "Chest"),
        ("Hand_L", (-0.36, 0.08, 0.46), (-0.36, 0.08, 0.36), "UpperArm_L"),
        ("UpperLeg_R", (0.12, 0, 0.40), (0.12, 0, 0.02), "Hips"),
        ("Foot_R", (0.12, 0, 0.02), (0.12, 0.18, 0.02), "UpperLeg_R"),
        ("UpperLeg_L", (-0.12, 0, 0.40), (-0.12, 0, 0.02), "Hips"),
        ("Foot_L", (-0.12, 0, 0.02), (-0.12, 0.18, 0.02), "UpperLeg_L"),
    ]

# ═══════════════════════════════════════════════════════════
def build_hero():
    reset()
    skin = mat("HeroSkin", (0.85, 0.65, 0.50, 1))
    armor = mat("HeroArmor", (0.35, 0.40, 0.50, 1))
    hair = mat("HeroHair", (0.25, 0.15, 0.10, 1))
    p = [
        sph(skin, "Head", 0.14, (0, 0, 1.60), scale=(1, 0.95, 1.1)),
        sph(hair, "Head", 0.155, (0, -0.02, 1.67), scale=(1.05, 1.05, 0.75)),
        cyl(skin, "Neck", 0.055, 0.10, (0, 0, 1.44)),
        cone(armor, "Chest", 0.17, 0.25, 0.52, (0, 0, 1.14)),
        sph(armor, "Chest", 0.11, (0.28, 0, 1.37)),
        sph(armor, "Chest", 0.11, (-0.28, 0, 1.37)),
        cyl(armor, "UpperArm_R", 0.055, 0.52, (0.30, 0, 1.06)),
        cyl(armor, "UpperArm_L", 0.055, 0.52, (-0.30, 0, 1.06)),
        sph(skin, "Hand_R", 0.06, (0.30, 0, 0.76)),
        sph(skin, "Hand_L", 0.06, (-0.30, 0, 0.76)),
        box(armor, "Hips", (0, 0, 0.84), (0.32, 0.22, 0.14)),
        cyl(armor, "UpperLeg_R", 0.075, 0.72, (0.11, 0, 0.44)),
        cyl(armor, "UpperLeg_L", 0.075, 0.72, (-0.11, 0, 0.44)),
        box(armor, "Foot_R", (0.11, 0.05, 0.045), (0.13, 0.24, 0.09)),
        box(armor, "Foot_L", (-0.11, 0.05, 0.045), (0.13, 0.24, 0.09)),
    ]
    body = join(p, "Hero")
    arm = build_armature(hero_bones())
    bind(body, arm)
    author_animations(arm)
    export(arm, body, "Hero.fbx")

# ── Goblin material palette (names are the slot contract; colours come from
# CharacterModelSwap at runtime — FBX flattens diffuse to gray). ──
def goblin_mats():
    return {
        "skin":  mat("GoblinSkin",  (0.38, 0.54, 0.26, 1)),
        "cloth": mat("GoblinCloth", (0.32, 0.24, 0.16, 1)),
        "dark":  mat("GoblinDark",  (0.10, 0.10, 0.12, 1)),  # blackened iron scrap
        "gold":  mat("GoblinGold",  (0.82, 0.63, 0.20, 1)),  # war-trophy trim
        "gem":   mat("GoblinGem",   (0.35, 0.95, 0.45, 1)),  # sickly emissive green
        "bone":  mat("GoblinBone",  (0.85, 0.82, 0.72, 1)),  # tusks / claws
    }

# A hunched, menacing low-poly goblin torso/head/limbs shared by every tier.
# Silhouette: potbelly + forward hunch + heavy brow + underbite tusks + big
# hooked ears + glowing eyes + clawed hands/feet. Tier extras layer on top.
def goblin_base(M):
    skin, cloth, bone, gem = M["skin"], M["cloth"], M["bone"], M["gem"]
    p = [
        # Torso — potbelly hips + hunched chest + a back hump
        sph(skin, "Hips", 0.30, (0, 0.02, 0.58), scale=(1.05, 0.95, 0.9)),
        sph(skin, "Chest", 0.23, (0, 0.10, 0.84), scale=(1.2, 0.95, 0.8)),
        sph(skin, "Chest", 0.13, (0, -0.11, 0.90), scale=(1.0, 1.0, 0.85)),
        # Head — cranium, heavy brow, jutting underbite jaw, hooked nose
        sph(skin, "Head", 0.23, (0, 0.15, 1.05), scale=(1.05, 1.05, 0.98)),
        box(skin, "Head", (0, 0.31, 1.11), (0.34, 0.09, 0.07)),
        box(skin, "Head", (0, 0.29, 0.96), (0.26, 0.12, 0.06)),
        cone(skin, "Head", 0.075, 0.02, 0.20, (0, 0.42, 1.03), rot=(rad(-90), 0, 0)),
        # Tusks (bone) jutting up from the lower jaw
        cone(bone, "Head", 0.028, 0.004, 0.11, (0.10, 0.34, 1.00), rot=(rad(18), 0, 0), v=6),
        cone(bone, "Head", 0.028, 0.004, 0.11, (-0.10, 0.34, 1.00), rot=(rad(18), 0, 0), v=6),
        # Big hooked ears
        cone(skin, "Head", 0.07, 0.005, 0.36, (0.30, 0.08, 1.15), rot=(0, rad(72), 0), v=6),
        cone(skin, "Head", 0.07, 0.005, 0.36, (-0.30, 0.08, 1.15), rot=(0, rad(-72), 0), v=6),
        # Glowing sunken eyes (faceted emissive gems)
        sph(gem, "Head", 0.033, (0.095, 0.29, 1.10), seg=4, ring=3),
        sph(gem, "Head", 0.033, (-0.095, 0.29, 1.10), seg=4, ring=3),
        # Sinewy arms
        cyl(skin, "UpperArm_R", 0.05, 0.44, (0.31, 0.06, 0.68), rot=(0, rad(12), 0)),
        cyl(skin, "UpperArm_L", 0.05, 0.44, (-0.31, 0.06, 0.68), rot=(0, rad(-12), 0)),
        sph(skin, "Hand_R", 0.07, (0.36, 0.08, 0.44)),
        sph(skin, "Hand_L", 0.07, (-0.36, 0.08, 0.44)),
        # Short bandy legs + splayed feet
        cyl(skin, "UpperLeg_R", 0.07, 0.36, (0.12, 0, 0.20)),
        cyl(skin, "UpperLeg_L", 0.07, 0.36, (-0.12, 0, 0.20)),
        box(skin, "Foot_R", (0.12, 0.07, 0.035), (0.12, 0.22, 0.07)),
        box(skin, "Foot_L", (-0.12, 0.07, 0.035), (0.12, 0.22, 0.07)),
        # Ragged loincloth
        box(cloth, "Hips", (0, 0.0, 0.40), (0.36, 0.30, 0.16)),
    ]
    # Finger claws (bone) on each hand
    for dx in (-0.045, 0.0, 0.045):
        p.append(cone(bone, "Hand_R", 0.011, 0.002, 0.06, (0.36 + dx, 0.14, 0.44), rot=(rad(-75), 0, 0), v=4))
        p.append(cone(bone, "Hand_L", 0.011, 0.002, 0.06, (-0.36 + dx, 0.14, 0.44), rot=(rad(-75), 0, 0), v=4))
    return p

# A crude club baked into the right hand (shaft + spiked head).
def goblin_club(M, big=False):
    dark, bone, gold = M["dark"], M["bone"], M["gold"]
    s = 1.35 if big else 1.0
    p = [
        cyl(bone, "Hand_R", 0.022, 0.34 * s, (0.42, 0.20, 0.42), rot=(rad(90), 0, 0)),
        sph(dark, "Hand_R", 0.075 * s, (0.42, 0.40 * s + 0.02, 0.42)),
    ]
    # protruding spikes on the head
    for ang in (0, 90, 180, 270):
        p.append(cone(dark, "Hand_R", 0.02 * s, 0.002, 0.09 * s,
                      (0.42, 0.40 * s + 0.02, 0.42), rot=(rad(90), 0, rad(ang)), v=4))
    if big:  # champion club gets a gold band
        p.append(cyl(gold, "Hand_R", 0.03, 0.03, (0.42, 0.28, 0.42), rot=(rad(90), 0, 0)))
    return p

# A curved horn (stacked cone segments) rooted at base_loc, curling up/out.
def goblin_horn(M, base_loc, side, material="gold"):
    m = M[material]
    x, y, z = base_loc
    return [
        cone(m, "Head", 0.05, 0.036, 0.10, (x, y, z), rot=(rad(-15), rad(28 * side), 0), v=6),
        cone(m, "Head", 0.036, 0.022, 0.10, (x + 0.06 * side, y + 0.02, z + 0.09), rot=(rad(-42), rad(40 * side), 0), v=6),
        cone(m, "Head", 0.022, 0.003, 0.09, (x + 0.14 * side, y + 0.03, z + 0.15), rot=(rad(-68), rad(52 * side), 0), v=6),
    ]

# ── Per-tier assembly. All share one skeleton so a single controller drives
# every variant (Generic clips retarget by bone name). ──
def build_goblin(variant, filename):
    reset()
    M = goblin_mats()
    p = goblin_base(M)
    skin, cloth, dark, gold, gem, bone = (M["skin"], M["cloth"], M["dark"], M["gold"], M["gem"], M["bone"])

    if variant == "scout":
        # Lean skirmisher: a hide baldric + a crude bone shiv.
        p += [
            box(dark, "Chest", (0.0, 0.02, 0.90), (0.5, 0.05, 0.06), rot=(0, rad(28), 0)),
            cyl(bone, "Hand_R", 0.018, 0.11, (0.40, 0.15, 0.42), rot=(rad(90), 0, 0)),
            cone(dark, "Hand_R", 0.028, 0.004, 0.18, (0.40, 0.30, 0.42), rot=(rad(-90), 0, 0), v=4),
        ]

    elif variant == "warrior":
        # Scrap-armored grunt: iron skullcap, one shoulder plate, belt + club.
        p += [
            cyl(dark, "Head", 0.21, 0.11, (0, 0.13, 1.15), rot=(rad(6), 0, 0)),
            cone(dark, "Head", 0.03, 0.004, 0.10, (0, 0.10, 1.24), v=4),
            sph(dark, "Chest", 0.13, (0.30, 0.06, 0.95), scale=(1.1, 1.0, 0.6)),
            box(dark, "Hips", (0, 0.02, 0.52), (0.38, 0.32, 0.06)),
        ]
        p += goblin_club(M, big=False)

    elif variant == "champion":
        # Bigger, gold-touched brute: horned helm, chest plate, bracers, spiked club.
        p += [
            cyl(dark, "Head", 0.22, 0.13, (0, 0.12, 1.15), rot=(rad(6), 0, 0)),
            box(dark, "Chest", (0, 0.14, 0.86), (0.42, 0.10, 0.30)),
            sph(gold, "Chest", 0.022, (0.10, 0.24, 0.90)),
            sph(gold, "Chest", 0.022, (-0.10, 0.24, 0.90)),
            sph(dark, "Chest", 0.14, (0.31, 0.06, 0.95), scale=(1.1, 1.0, 0.7)),
            sph(dark, "Chest", 0.14, (-0.31, 0.06, 0.95), scale=(1.1, 1.0, 0.7)),
            cyl(dark, "UpperArm_R", 0.06, 0.16, (0.31, 0.06, 0.55), rot=(0, rad(12), 0)),
            cyl(dark, "UpperArm_L", 0.06, 0.16, (-0.31, 0.06, 0.55), rot=(0, rad(-12), 0)),
        ]
        p += goblin_horn(M, (0.14, 0.10, 1.22), 1)
        p += goblin_horn(M, (-0.14, 0.10, 1.22), -1)
        p += goblin_club(M, big=True)

    elif variant == "warchief":
        # Hulking boss-elite: great horns, spiked pauldrons, gold trim, a glowing
        # back-totem, and a massive gold-edged cleaver.
        p += [
            cyl(dark, "Head", 0.23, 0.14, (0, 0.11, 1.15), rot=(rad(6), 0, 0)),
            box(gold, "Head", (0, 0.30, 1.17), (0.30, 0.05, 0.04)),  # brow band
            # Spiked pauldrons
            sph(dark, "Chest", 0.17, (0.33, 0.06, 0.98), scale=(1.1, 1.0, 0.8)),
            sph(dark, "Chest", 0.17, (-0.33, 0.06, 0.98), scale=(1.1, 1.0, 0.8)),
            cone(gold, "Chest", 0.04, 0.004, 0.13, (0.36, 0.06, 1.10), v=4),
            cone(gold, "Chest", 0.04, 0.004, 0.13, (-0.36, 0.06, 1.10), v=4),
            # Gold-trimmed breastplate
            box(dark, "Chest", (0, 0.15, 0.86), (0.46, 0.10, 0.34)),
            box(gold, "Chest", (0, 0.15, 0.72), (0.46, 0.11, 0.05)),
            # Back-totem: haft + skull + emissive crystal
            cyl(bone, "Chest", 0.02, 0.5, (0, -0.18, 0.95)),
            sph(bone, "Chest", 0.07, (0, -0.20, 1.22), scale=(1, 0.9, 1.05)),
            sph(gem, "Chest", 0.06, (0, -0.20, 1.34), seg=4, ring=3),
            # Bracers
            cyl(dark, "UpperArm_R", 0.065, 0.18, (0.31, 0.06, 0.55), rot=(0, rad(12), 0)),
            cyl(dark, "UpperArm_L", 0.065, 0.18, (-0.31, 0.06, 0.55), rot=(0, rad(-12), 0)),
        ]
        p += goblin_horn(M, (0.15, 0.08, 1.22), 1)
        p += goblin_horn(M, (-0.15, 0.08, 1.22), -1)
        # Massive cleaver in the right hand (dark blade, gold edge, gem rune)
        p += [
            cyl(bone, "Hand_R", 0.028, 0.30, (0.42, 0.18, 0.42), rot=(rad(90), 0, 0)),
            box(dark, "Hand_R", (0.42, 0.40, 0.47), (0.05, 0.34, 0.26)),
            box(gold, "Hand_R", (0.42, 0.40, 0.61), (0.055, 0.34, 0.03)),
            sph(gem, "Hand_R", 0.04, (0.42, 0.40, 0.44), seg=4, ring=3),
        ]

    body = join(p, "Goblin")
    arm = build_armature(goblin_bones())
    bind(body, arm)
    author_animations(arm)
    export(arm, body, filename)

if __name__ == "__main__":
    bpy.context.scene.render.fps = 30
    # Base rig = canonical clip source for the shared GoblinAnimator controller.
    build_goblin("scout", "Goblin.fbx")
    build_goblin("scout", "Goblin_Scout.fbx")
    build_goblin("warrior", "Goblin_Warrior.fbx")
    build_goblin("champion", "Goblin_Champion.fbx")
    build_goblin("warchief", "Goblin_Warchief.fbx")
    build_hero()
    print("[Models] Done — rigged + animated Hero and 4-tier Goblin warband.")
