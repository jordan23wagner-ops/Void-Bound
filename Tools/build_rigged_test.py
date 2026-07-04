# PROTOTYPE — validate the headless rig -> Unity pipeline before the full build.
# Builds a minimal box figure (torso, head, 2 legs), rigid-skins it to a small
# armature (one bone per part, weight 1.0), authors a Walk action swinging the
# legs, and exports a rigged+animated FBX.
#   & "C:\Program Files\Blender Foundation\Blender 5.1\blender.exe" -b -P Tools\build_rigged_test.py
# Success criteria (checked in Unity): asset imports as SkinnedMeshRenderer +
# Generic Avatar + a playable "Walk" AnimationClip.

import bpy
import math
import os

OUT = r"C:\Users\Jordon\Void Bound\Assets\Art\Models\RigTest.fbx"

def rad(d):
    return math.radians(d)

def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def add_box(name, location, scale, group):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location, scale=scale)
    ob = bpy.context.active_object
    ob.name = name
    vg = ob.vertex_groups.new(name=group)
    vg.add(range(len(ob.data.vertices)), 1.0, 'REPLACE')
    return ob

def main():
    reset()

    # ── Mesh parts, each fully weighted to its bone's vertex group ──
    parts = [
        add_box("Torso", (0, 0, 0.85), (0.5, 0.3, 0.7), "Hips"),
        add_box("Head",  (0, 0, 1.45), (0.35, 0.35, 0.35), "Head"),
        add_box("LegL",  (0.18, 0, 0.30), (0.18, 0.18, 0.6), "Leg_L"),
        add_box("LegR",  (-0.18, 0, 0.30), (0.18, 0.18, 0.6), "Leg_R"),
    ]

    # Join into one mesh (same-named vertex groups merge)
    bpy.ops.object.select_all(action='DESELECT')
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    body = bpy.context.active_object
    body.name = "RigTestMesh"

    # ── Armature ──
    arm_data = bpy.data.armatures.new("Rig")
    arm = bpy.data.objects.new("Rig", arm_data)
    bpy.context.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='EDIT')
    eb = arm_data.edit_bones

    hips = eb.new("Hips"); hips.head = (0, 0, 0.60); hips.tail = (0, 0, 1.10)
    head = eb.new("Head"); head.head = (0, 0, 1.10); head.tail = (0, 0, 1.60); head.parent = hips
    legL = eb.new("Leg_L"); legL.head = (0.18, 0, 0.60); legL.tail = (0.18, 0, 0.05); legL.parent = hips
    legR = eb.new("Leg_R"); legR.head = (-0.18, 0, 0.60); legR.tail = (-0.18, 0, 0.05); legR.parent = hips
    bpy.ops.object.mode_set(mode='OBJECT')

    # ── Bind: parent mesh to armature, keep existing vertex groups ──
    body.parent = arm
    mod = body.modifiers.new("Armature", 'ARMATURE')
    mod.object = arm
    mod.use_vertex_groups = True

    # ── Walk action: swing the two legs opposite each other ──
    arm.animation_data_create()
    action = bpy.data.actions.new("Walk")
    arm.animation_data.action = action
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='POSE')
    pb = arm.pose.bones
    for b in ("Leg_L", "Leg_R", "Hips"):
        pb[b].rotation_mode = 'XYZ'

    def key(bone, frame, ex):
        pb[bone].rotation_euler = (rad(ex), 0, 0)
        pb[bone].keyframe_insert("rotation_euler", frame=frame)

    # 30-frame loop: legs alternate +-25 deg, hips small bob
    key("Leg_L", 1, 25);  key("Leg_R", 1, -25)
    key("Leg_L", 15, -25); key("Leg_R", 15, 25)
    key("Leg_L", 30, 25);  key("Leg_R", 30, -25)
    key("Hips", 1, 0); key("Hips", 8, -4); key("Hips", 22, -4); key("Hips", 30, 0)
    bpy.ops.object.mode_set(mode='OBJECT')

    # Placeholder material
    m = bpy.data.materials.new("Main")
    m.diffuse_color = (0.5, 0.6, 0.7, 1)
    body.data.materials.append(m)

    # ── Export armature + mesh ──
    bpy.ops.object.select_all(action='DESELECT')
    arm.select_set(True)
    body.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.export_scene.fbx(
        filepath=OUT,
        use_selection=True,
        add_leaf_bones=False,
        axis_forward='-Z',
        axis_up='Y',
        bake_space_transform=False,   # keep off for rigged meshes (armature safety)
        mesh_smooth_type='OFF',
        use_armature_deform_only=True,
        bake_anim=True,
        bake_anim_use_all_actions=True,
        bake_anim_use_nla_strips=False,
    )
    print(f"[RigTest] Exported {OUT}")

if __name__ == "__main__":
    main()
