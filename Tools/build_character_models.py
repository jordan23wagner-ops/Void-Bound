# Void Bound — low-poly goblin + hero model generator.
# Run headless:
#   & "C:\Program Files\Blender Foundation\Blender 5.1\blender.exe" -b -P Tools\build_character_models.py
# Exports Assets/Art/Models/Goblin.fbx and Hero.fbx using the FBX settings
# mandated by CODING_STANDARDS.md (bake_space_transform=True etc.).
# Blender is Z-up; models are built with feet at z=0 so the Unity pivot
# lands at the feet. Materials are placeholders — Unity reassigns real URP
# materials by slot NAME, so the names below are the contract:
#   Goblin: GoblinSkin, GoblinCloth
#   Hero:   HeroSkin, HeroArmor, HeroHair

import bpy
import math
import os

OUT_DIR = r"C:\Users\Jordon\Void Bound\Assets\Art\Models"

FBX_SETTINGS = dict(
    use_selection=True,
    apply_scale_options='FBX_SCALE_ALL',
    axis_forward='-Z',
    axis_up='Y',
    bake_space_transform=True,   # CRITICAL - bakes axis conversion into vertices
    mesh_smooth_type='OFF',
    use_mesh_modifiers=True,
    bake_anim=False,
)

def rad(deg):
    return math.radians(deg)

def make_mat(name, rgba):
    m = bpy.data.materials.get(name)
    if m is None:
        m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    if bsdf is not None:
        bsdf.inputs["Base Color"].default_value = rgba
    m.diffuse_color = rgba
    return m

def sphere(mat, radius, location, scale=(1, 1, 1), segments=10, rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=segments, ring_count=rings, radius=radius,
        location=location, scale=scale)
    ob = bpy.context.active_object
    ob.data.materials.append(mat)
    return ob

def cyl(mat, radius, depth, location, rotation=(0, 0, 0), vertices=8):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices, radius=radius, depth=depth,
        location=location, rotation=rotation)
    ob = bpy.context.active_object
    ob.data.materials.append(mat)
    return ob

def cone(mat, r1, r2, depth, location, rotation=(0, 0, 0), vertices=8):
    bpy.ops.mesh.primitive_cone_add(
        vertices=vertices, radius1=r1, radius2=r2, depth=depth,
        location=location, rotation=rotation)
    ob = bpy.context.active_object
    ob.data.materials.append(mat)
    return ob

def box(mat, location, scale):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location, scale=scale)
    ob = bpy.context.active_object
    ob.data.materials.append(mat)
    return ob

def join_and_export(parts, name, filename):
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts:
        o.select_set(True)
    # parts[0] is the join target — its material becomes slot 0
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    ob = bpy.context.active_object
    ob.name = name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    path = os.path.join(OUT_DIR, filename)
    bpy.ops.export_scene.fbx(filepath=path, **FBX_SETTINGS)
    print(f"[Models] Exported {path}")


# ═══════════════════════════════════════════════════════════
# GOBLIN — hunched, potbelly, big head, long pointy ears,
# jutting snout, thin limbs, crude club. ~1.3 units tall.
# ═══════════════════════════════════════════════════════════
def build_goblin():
    skin = make_mat("GoblinSkin", (0.35, 0.55, 0.25, 1.0))
    cloth = make_mat("GoblinCloth", (0.35, 0.25, 0.15, 1.0))
    p = []

    # Belly first — skin becomes slot 0 (potbelly: wide squat sphere)
    p.append(sphere(skin, 0.30, (0, 0.02, 0.60), scale=(1.0, 0.95, 0.85)))
    # Chest/shoulders, hunched forward
    p.append(sphere(skin, 0.22, (0, 0.08, 0.86), scale=(1.15, 0.9, 0.75)))
    # Oversized head, jutting forward off the hunch
    p.append(sphere(skin, 0.23, (0, 0.16, 1.06), scale=(1.05, 1.0, 0.95)))
    # Snout — cone pointing forward (+Y)
    p.append(cone(skin, 0.08, 0.02, 0.18, (0, 0.42, 1.00), rotation=(rad(-90), 0, 0)))
    # Brow ridge over the eyes
    p.append(box(skin, (0, 0.30, 1.14), (0.30, 0.10, 0.06)))
    # Long pointy ears, swept out and up
    p.append(cone(skin, 0.065, 0.005, 0.34, (0.30, 0.10, 1.16), rotation=(0, rad(65), 0), vertices=6))
    p.append(cone(skin, 0.065, 0.005, 0.34, (-0.30, 0.10, 1.16), rotation=(0, rad(-65), 0), vertices=6))
    # Thin arms, hanging out from the hunch
    p.append(cyl(skin, 0.05, 0.46, (0.31, 0.06, 0.68), rotation=(0, rad(12), 0)))
    p.append(cyl(skin, 0.05, 0.46, (-0.31, 0.06, 0.68), rotation=(0, rad(-12), 0)))
    # Hands
    p.append(sphere(skin, 0.07, (0.36, 0.08, 0.44), segments=8, rings=6))
    p.append(sphere(skin, 0.07, (-0.36, 0.08, 0.44), segments=8, rings=6))
    # Thin legs
    p.append(cyl(skin, 0.065, 0.36, (0.12, 0, 0.20)))
    p.append(cyl(skin, 0.065, 0.36, (-0.12, 0, 0.20)))
    # Feet, poking forward
    p.append(box(skin, (0.12, 0.07, 0.035), (0.11, 0.20, 0.07)))
    p.append(box(skin, (-0.12, 0.07, 0.035), (0.11, 0.20, 0.07)))
    # Loincloth (cloth slot)
    p.append(box(cloth, (0, 0.0, 0.40), (0.36, 0.30, 0.16)))
    # NOTE: no built-in club — the weapon slot is authoritative now, so the
    # right hand stays empty and EquipmentVisuals attaches whatever gear the
    # EnemyDefinitionSO specifies (or nothing).

    join_and_export(p, "Goblin", "Goblin.fbx")


# ═══════════════════════════════════════════════════════════
# HERO — upright, broad shoulders, tapered torso, hair. ~1.8 units tall.
# No built-in weapon; the equip system renders the held weapon.
# ═══════════════════════════════════════════════════════════
def build_hero():
    skin = make_mat("HeroSkin", (0.85, 0.65, 0.50, 1.0))
    armor = make_mat("HeroArmor", (0.35, 0.40, 0.50, 1.0))
    hair = make_mat("HeroHair", (0.25, 0.15, 0.10, 1.0))
    p = []

    # Head first — skin becomes slot 0
    p.append(sphere(skin, 0.14, (0, 0, 1.60), scale=(1.0, 0.95, 1.1)))
    # Neck
    p.append(cyl(skin, 0.055, 0.10, (0, 0, 1.44)))
    # Hands
    p.append(sphere(skin, 0.06, (0.30, 0, 0.76), segments=8, rings=6))
    p.append(sphere(skin, 0.06, (-0.30, 0, 0.76), segments=8, rings=6))
    # Hair cap, slightly back
    p.append(sphere(hair, 0.155, (0, -0.02, 1.67), scale=(1.05, 1.05, 0.75)))
    # Torso — cone frustum widening to the shoulders
    p.append(cone(armor, 0.17, 0.25, 0.52, (0, 0, 1.14), vertices=8))
    # Shoulder pads
    p.append(sphere(armor, 0.11, (0.28, 0, 1.37), segments=8, rings=6))
    p.append(sphere(armor, 0.11, (-0.28, 0, 1.37), segments=8, rings=6))
    # Arms (armored sleeves), hanging straight
    p.append(cyl(armor, 0.055, 0.52, (0.30, 0, 1.06)))
    p.append(cyl(armor, 0.055, 0.52, (-0.30, 0, 1.06)))
    # Hips
    p.append(box(armor, (0, 0, 0.84), (0.32, 0.22, 0.14)))
    # Legs
    p.append(cyl(armor, 0.075, 0.72, (0.11, 0, 0.44)))
    p.append(cyl(armor, 0.075, 0.72, (-0.11, 0, 0.44)))
    # Boots
    p.append(box(armor, (0.11, 0.05, 0.045), (0.13, 0.24, 0.09)))
    p.append(box(armor, (-0.11, 0.05, 0.045), (0.13, 0.24, 0.09)))
    # NOTE: no built-in hip sword — the weapon slot is authoritative now, so an
    # unarmed hero shows empty hands and EquipmentVisuals renders the equipped
    # weapon in the right hand.

    join_and_export(p, "Hero", "Hero.fbx")


if __name__ == "__main__":
    bpy.ops.wm.read_factory_settings(use_empty=True)
    build_goblin()
    bpy.ops.wm.read_factory_settings(use_empty=True)
    build_hero()
    print("[Models] Done.")
