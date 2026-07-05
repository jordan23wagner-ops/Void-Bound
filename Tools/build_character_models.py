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

def build_goblin():
    reset()
    skin = mat("GoblinSkin", (0.35, 0.55, 0.25, 1))
    cloth = mat("GoblinCloth", (0.35, 0.25, 0.15, 1))
    p = [
        sph(skin, "Hips", 0.30, (0, 0.02, 0.60), scale=(1, 0.95, 0.85)),
        sph(skin, "Chest", 0.22, (0, 0.08, 0.86), scale=(1.15, 0.9, 0.75)),
        sph(skin, "Head", 0.23, (0, 0.16, 1.06), scale=(1.05, 1, 0.95)),
        cone(skin, "Head", 0.08, 0.02, 0.18, (0, 0.42, 1.00), rot=(rad(-90), 0, 0)),
        box(skin, "Head", (0, 0.30, 1.14), (0.30, 0.10, 0.06)),
        cone(skin, "Head", 0.065, 0.005, 0.34, (0.30, 0.10, 1.16), rot=(0, rad(65), 0), v=6),
        cone(skin, "Head", 0.065, 0.005, 0.34, (-0.30, 0.10, 1.16), rot=(0, rad(-65), 0), v=6),
        cyl(skin, "UpperArm_R", 0.05, 0.46, (0.31, 0.06, 0.68), rot=(0, rad(12), 0)),
        cyl(skin, "UpperArm_L", 0.05, 0.46, (-0.31, 0.06, 0.68), rot=(0, rad(-12), 0)),
        sph(skin, "Hand_R", 0.07, (0.36, 0.08, 0.44)),
        sph(skin, "Hand_L", 0.07, (-0.36, 0.08, 0.44)),
        cyl(skin, "UpperLeg_R", 0.065, 0.36, (0.12, 0, 0.20)),
        cyl(skin, "UpperLeg_L", 0.065, 0.36, (-0.12, 0, 0.20)),
        box(skin, "Foot_R", (0.12, 0.07, 0.035), (0.11, 0.20, 0.07)),
        box(skin, "Foot_L", (-0.12, 0.07, 0.035), (0.11, 0.20, 0.07)),
        box(cloth, "Hips", (0, 0.0, 0.40), (0.36, 0.30, 0.16)),
    ]
    body = join(p, "Goblin")
    arm = build_armature(goblin_bones())
    bind(body, arm)
    author_animations(arm)
    export(arm, body, "Goblin.fbx")

if __name__ == "__main__":
    bpy.context.scene.render.fps = 30
    build_goblin()
    build_hero()
    print("[Models] Done — rigged + animated Hero and Goblin.")
