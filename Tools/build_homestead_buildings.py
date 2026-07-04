# Void Bound — 12 distinct low-poly Homestead building models.
# Run headless:
#   & "C:\Program Files\Blender Foundation\Blender 5.1\blender.exe" -b -P Tools\build_homestead_buildings.py
# Exports one FBX per building to Assets/Art/Models/Buildings/ using the
# CODING_STANDARDS.md FBX settings. Feet at z=0 (Unity pivot at ground).
# Material NAMES are the contract with HomesteadBuildingSwap.cs — Unity
# reassigns real URP materials by imported slot name:
#   WoodDark, WoodLight, Stone, StoneDark, Metal, Thatch, Fire, Water,
#   Crystal, ClothRed, ClothGreen, ClothBlue, ClothWhite, Leaf, Soil

import bpy
import math
import os

OUT_DIR = r"C:\Users\Jordon\Void Bound\Assets\Art\Models\Buildings"

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

PALETTE = {
    "WoodDark":   (0.30, 0.20, 0.12, 1),
    "WoodLight":  (0.55, 0.40, 0.25, 1),
    "Stone":      (0.58, 0.57, 0.53, 1),
    "StoneDark":  (0.36, 0.36, 0.35, 1),
    "Metal":      (0.45, 0.47, 0.50, 1),
    "Thatch":     (0.72, 0.60, 0.30, 1),
    "Fire":       (1.00, 0.45, 0.10, 1),
    "Water":      (0.25, 0.60, 0.90, 1),
    "Crystal":    (0.65, 0.35, 0.90, 1),
    "ClothRed":   (0.70, 0.20, 0.18, 1),
    "ClothGreen": (0.25, 0.55, 0.25, 1),
    "ClothBlue":  (0.25, 0.40, 0.75, 1),
    "ClothWhite": (0.85, 0.82, 0.75, 1),
    "Leaf":       (0.30, 0.60, 0.25, 1),
    "Soil":       (0.30, 0.22, 0.15, 1),
}

def rad(d):
    return math.radians(d)

def mat(name):
    m = bpy.data.materials.get(name)
    if m is None:
        m = bpy.data.materials.new(name)
        m.diffuse_color = PALETTE[name]
    return m

def box(mname, location, scale, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location, scale=scale, rotation=rotation)
    ob = bpy.context.active_object
    ob.data.materials.append(mat(mname))
    return ob

def cyl(mname, radius, depth, location, rotation=(0, 0, 0), vertices=8):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth,
                                        location=location, rotation=rotation)
    ob = bpy.context.active_object
    ob.data.materials.append(mat(mname))
    return ob

def cone(mname, r1, r2, depth, location, rotation=(0, 0, 0), vertices=8):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=r1, radius2=r2, depth=depth,
                                    location=location, rotation=rotation)
    ob = bpy.context.active_object
    ob.data.materials.append(mat(mname))
    return ob

def sphere(mname, radius, location, scale=(1, 1, 1), segments=8, rings=6):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, radius=radius,
                                         location=location, scale=scale)
    ob = bpy.context.active_object
    ob.data.materials.append(mat(mname))
    return ob

def export(parts, name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts:
        o.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    ob = bpy.context.active_object
    ob.name = name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    path = os.path.join(OUT_DIR, name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, **FBX_SETTINGS)
    print(f"[Buildings] Exported {path}")
    bpy.ops.wm.read_factory_settings(use_empty=True)


def forge():
    p = []
    p.append(box("Stone", (0, 0, 0.45), (1.2, 1.0, 0.9)))                       # furnace body
    p.append(box("Fire", (0, 0.51, 0.40), (0.5, 0.06, 0.35)))                   # glowing mouth
    p.append(box("StoneDark", (0, -0.15, 1.45), (0.42, 0.42, 1.1)))             # chimney
    p.append(box("Fire", (0, -0.15, 2.02), (0.30, 0.30, 0.08)))                 # ember top
    p.append(cyl("WoodDark", 0.18, 0.35, (0.85, 0.1, 0.175)))                   # anvil stump
    p.append(box("Metal", (0.85, 0.1, 0.44), (0.5, 0.18, 0.13)))                # anvil body
    p.append(box("Metal", (0.85, 0.1, 0.36), (0.22, 0.14, 0.06)))               # anvil waist
    export(p, "Forge")

def campfire():
    p = []
    for i in range(6):
        a = rad(i * 60)
        p.append(box("StoneDark", (0.5 * math.cos(a), 0.5 * math.sin(a), 0.08),
                     (0.18, 0.14, 0.16), rotation=(0, 0, a)))
    p.append(cyl("WoodDark", 0.07, 0.9, (0, 0, 0.14), rotation=(0, rad(80), rad(45))))
    p.append(cyl("WoodDark", 0.07, 0.9, (0, 0, 0.14), rotation=(0, rad(80), rad(-45))))
    p.append(cone("Fire", 0.20, 0.02, 0.55, (0, 0, 0.38)))                      # main flame
    p.append(cone("Fire", 0.12, 0.02, 0.38, (0.13, 0.06, 0.30)))
    p.append(cone("Fire", 0.10, 0.02, 0.30, (-0.11, -0.07, 0.26)))
    export(p, "Campfire")

def garden():
    p = []
    for sx in (-0.55, 0.55):
        p.append(box("WoodLight", (sx, 0, 0.11), (0.95, 0.55, 0.22)))           # bed frame
        p.append(box("Soil", (sx, 0, 0.20), (0.85, 0.45, 0.10)))                # soil
        for i, dy in enumerate((-0.14, 0.14)):
            for j, dx in enumerate((-0.28, 0.0, 0.28)):
                if (i + j) % 2 == 0:
                    p.append(sphere("Leaf", 0.10, (sx + dx, dy, 0.30)))
                else:
                    p.append(cone("Leaf", 0.07, 0.01, 0.22, (sx + dx, dy, 0.34), vertices=6))
    export(p, "Garden")

def merchant():
    p = []
    p.append(box("WoodLight", (0, 0.15, 0.40), (1.6, 0.42, 0.72)))              # counter base
    p.append(box("WoodDark", (0, 0.15, 0.83), (1.7, 0.55, 0.10)))               # counter top
    for px in (-0.75, 0.75):
        for py in (-0.35, 0.42):
            p.append(cyl("WoodDark", 0.05, 2.0, (px, py, 1.0), vertices=6))     # posts
    for i in range(6):                                                          # striped awning
        x = -0.68 + i * 0.272
        cloth = "ClothRed" if i % 2 == 0 else "ClothWhite"
        p.append(box(cloth, (x, 0.05, 2.08), (0.27, 1.05, 0.05), rotation=(rad(12), 0, 0)))
    p.append(box("WoodLight", (1.10, -0.15, 0.18), (0.36, 0.36, 0.36), rotation=(0, 0, rad(15))))
    export(p, "Merchant")

def storage_chest():
    p = []
    p.append(box("WoodLight", (0, 0, 0.28), (0.95, 0.60, 0.46)))                # body
    p.append(cyl("WoodDark", 0.31, 0.93, (0, 0, 0.52), rotation=(0, rad(90), 0)))  # curved lid
    for bx in (-0.28, 0.28):
        p.append(box("Metal", (bx, 0, 0.42), (0.07, 0.64, 0.60)))               # bands
    p.append(box("Metal", (0, 0.33, 0.44), (0.14, 0.06, 0.16)))                 # lock
    export(p, "StorageChest")

def pool():
    p = []
    p.append(cyl("Stone", 1.05, 0.50, (0, 0, 0.25)))                            # basin (octagon)
    p.append(cyl("Water", 0.90, 0.46, (0, 0, 0.26)))                            # water
    for i in range(4):
        a = rad(45 + i * 90)
        p.append(box("StoneDark", (1.0 * math.cos(a), 1.0 * math.sin(a), 0.56),
                     (0.22, 0.22, 0.18), rotation=(0, 0, a)))                   # rim stones
    p.append(cyl("Stone", 0.12, 0.75, (0, 0, 0.375), vertices=6))               # fountain column
    p.append(sphere("Water", 0.12, (0, 0, 0.82)))                               # fountain top
    export(p, "Pool")

def shrine():
    p = []
    p.append(box("StoneDark", (0, 0, 0.075), (1.15, 1.15, 0.15)))               # bottom step
    p.append(box("Stone", (0, 0, 0.22), (0.85, 0.85, 0.15)))                    # top step
    p.append(box("Stone", (0, 0, 0.80), (0.35, 0.35, 1.0)))                     # pillar
    p.append(box("StoneDark", (0, 0, 1.35), (0.52, 0.52, 0.10)))                # cap
    p.append(cone("Crystal", 0.17, 0.01, 0.32, (0, 0, 1.78), vertices=6))       # crystal top
    p.append(cone("Crystal", 0.17, 0.01, 0.32, (0, 0, 1.50),
                  rotation=(rad(180), 0, 0), vertices=6))                       # crystal bottom
    export(p, "Shrine")

def _hut(banner_cloth, sign_parts):
    p = []
    p.append(box("WoodLight", (0, 0, 0.60), (1.45, 1.25, 1.2)))                 # walls
    p.append(box("WoodDark", (0, 0.64, 0.38), (0.42, 0.06, 0.76)))              # door
    p.append(cone("Thatch", 1.15, 0.04, 1.0, (0, 0, 1.70)))                     # roof
    p.append(box(banner_cloth, (0.45, 0.66, 0.85), (0.28, 0.05, 0.62)))         # banner
    p.extend(sign_parts())
    return p

def warriors_guild():
    def sign():
        return [
            box("Metal", (-0.42, 0.66, 1.02), (0.06, 0.04, 0.46), rotation=(0, rad(40), 0)),
            box("WoodDark", (-0.42, 0.66, 0.88), (0.22, 0.05, 0.06), rotation=(0, rad(40), 0)),
        ]
    export(_hut("ClothRed", sign), "WarriorsGuild")

def rangers_guild():
    def sign():
        return [
            box("WoodDark", (-0.42, 0.66, 1.0), (0.05, 0.04, 0.5), rotation=(0, rad(45), 0)),
            box("WoodDark", (-0.42, 0.66, 1.0), (0.05, 0.04, 0.5), rotation=(0, rad(-45), 0)),
            cone("Metal", 0.05, 0.01, 0.1, (-0.28, 0.66, 1.16), rotation=(0, rad(45), 0), vertices=5),
            cone("Metal", 0.05, 0.01, 0.1, (-0.56, 0.66, 1.16), rotation=(0, rad(-45), 0), vertices=5),
        ]
    export(_hut("ClothGreen", sign), "RangersGuild")

def mages_guild():
    p = []
    p.append(cyl("Stone", 0.68, 1.7, (0, 0, 0.85), vertices=10))                # tower
    p.append(box("WoodDark", (0, 0.64, 0.42), (0.40, 0.12, 0.84)))              # door
    p.append(cone("ClothBlue", 0.85, 0.03, 0.85, (0, 0, 2.12), vertices=10))    # roof
    p.append(cone("Crystal", 0.10, 0.01, 0.22, (0, 0, 2.62), vertices=6))       # finial
    p.append(box("ClothBlue", (0, 0.70, 1.15), (0.28, 0.05, 0.70)))             # banner
    export(p, "MagesGuild")

def watchtower():
    p = []
    for px in (-0.45, 0.45):
        for py in (-0.45, 0.45):
            p.append(cyl("WoodDark", 0.07, 2.3, (px, py, 1.15), vertices=6))    # legs
    p.append(box("WoodLight", (0, 0, 2.32), (1.35, 1.35, 0.12)))                # platform
    p.append(box("WoodDark", (0, 0.65, 2.55), (1.35, 0.05, 0.34)))              # railings
    p.append(box("WoodDark", (0, -0.65, 2.55), (1.35, 0.05, 0.34)))
    p.append(box("WoodDark", (0.65, 0, 2.55), (0.05, 1.35, 0.34)))
    p.append(box("WoodDark", (-0.65, 0, 2.55), (0.05, 1.35, 0.34)))
    p.append(cyl("WoodDark", 0.05, 0.6, (0, 0, 2.68), vertices=6))              # center pole
    p.append(cone("Thatch", 0.95, 0.03, 0.62, (0, 0, 3.25)))                    # roof
    export(p, "Watchtower")

def portal():
    p = []
    p.append(box("StoneDark", (0, 0, 0.10), (1.6, 0.55, 0.20)))                 # base
    for px in (-0.62, 0.62):
        p.append(box("Stone", (px, 0, 1.0), (0.32, 0.32, 1.6)))                 # pillars
    p.append(box("Stone", (0, 0, 1.90), (1.56, 0.32, 0.26)))                    # lintel
    p.append(cyl("Crystal", 0.48, 0.07, (0, 0, 1.08), rotation=(rad(90), 0, 0)))  # glowing disc
    p.append(box("StoneDark", (1.05, 0.35, 0.14), (0.16, 0.16, 0.28), rotation=(0, 0, rad(20))))
    p.append(box("StoneDark", (-1.05, -0.30, 0.12), (0.14, 0.14, 0.24), rotation=(0, 0, rad(-15))))
    export(p, "Portal")


if __name__ == "__main__":
    os.makedirs(OUT_DIR, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    forge()
    campfire()
    garden()
    merchant()
    storage_chest()
    pool()
    shrine()
    warriors_guild()
    rangers_guild()
    mages_guild()
    watchtower()
    portal()
    print("[Buildings] Done - 12 models exported.")
