# Void Bound — 12 distinct low-poly Homestead building models (polish-bar pass).
# Run headless:
#   & "C:\Program Files\Blender Foundation\Blender 5.1\blender.exe" -b -P Tools\build_homestead_buildings.py
# Exports one FBX per building to Assets/Art/Models/Buildings/ using the
# CODING_STANDARDS.md FBX settings. Feet at z=0 (Unity pivot at ground).
# Material NAMES are the contract with HomesteadBuildingSwap.cs — Unity
# reassigns real URP materials by imported slot name:
#   WoodDark, WoodLight, Stone, StoneDark, Metal, Thatch, Gold, Fire, Water,
#   Crystal, GemCyan, ClothRed, ClothGreen, ClothBlue, ClothWhite, Leaf, Soil
# Polish language (matches the gear/enemy bar): every station sits on a planted
# stone dais with a gold rim; gold trim/finials tie the set together; the
# magical stations (Pool/Shrine/Portal/Mages Guild) carry emissive GemCyan /
# Crystal focal points; the working stations carry warm Fire glow.

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
    "Gold":       (0.82, 0.63, 0.20, 1),
    "Fire":       (1.00, 0.45, 0.10, 1),
    "Water":      (0.25, 0.60, 0.90, 1),
    "Crystal":    (0.65, 0.35, 0.90, 1),
    "GemCyan":    (0.40, 0.85, 1.00, 1),
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

def torus(mname, major, minor, location, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, location=location,
                                     rotation=rotation, major_segments=16, minor_segments=6)
    ob = bpy.context.active_object
    ob.data.materials.append(mat(mname))
    return ob

# ── Shared "planted" foundations: a dark base course + a thin gold rim + a
# stone cap, so every building reads as deliberately placed, not floating. ──
def dais_round(radius):
    r = radius
    return [
        cyl("StoneDark", r + 0.20, 0.10, (0, 0, 0.05), vertices=12),
        cyl("Gold",      r + 0.08, 0.035, (0, 0, 0.10), vertices=12),
        cyl("Stone",     r,        0.09, (0, 0, 0.14), vertices=12),
    ]

def dais_square(sx, sy):
    return [
        box("StoneDark", (0, 0, 0.05), (sx + 0.40, sy + 0.40, 0.10)),
        box("Gold",      (0, 0, 0.10), (sx + 0.16, sy + 0.16, 0.035)),
        box("Stone",     (0, 0, 0.14), (sx, sy, 0.09)),
    ]

def gold_finial(z, r=0.07):
    return [
        cyl("Gold", 0.05, 0.16, (0, 0, z), vertices=6),
        sphere("Gold", r, (0, 0, z + 0.11)),
    ]

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
    p = dais_square(1.25, 1.05)
    p.append(box("Stone", (0, 0, 0.58), (1.2, 1.0, 0.9)))                       # furnace body
    p.append(box("StoneDark", (0, 0, 0.26), (1.26, 1.06, 0.22)))               # base course
    p.append(box("Gold", (0, 0.52, 0.60), (0.64, 0.04, 0.10)))                 # gold lintel
    p.append(box("Fire", (0, 0.52, 0.46), (0.5, 0.06, 0.34)))                  # glowing mouth
    p.append(box("StoneDark", (0, -0.15, 1.55), (0.44, 0.44, 1.1)))            # chimney
    p.append(box("Gold", (0, -0.15, 2.10), (0.50, 0.50, 0.06)))               # gold chimney cap
    p.append(cone("Fire", 0.20, 0.02, 0.40, (0, -0.15, 2.28)))                 # ember plume
    p.append(cyl("WoodDark", 0.20, 0.42, (0.95, 0.25, 0.35), vertices=10))     # anvil stump
    p.append(box("Metal", (0.95, 0.25, 0.66), (0.54, 0.20, 0.14)))            # anvil body
    p.append(box("Metal", (0.95, 0.25, 0.55), (0.24, 0.16, 0.08)))           # anvil waist
    p.append(box("WoodDark", (-0.98, 0.20, 0.85), (0.08, 0.5, 1.1)))          # tool rack
    p.append(box("Metal", (-0.98, 0.42, 1.05), (0.16, 0.04, 0.5), rotation=(rad(10), 0, 0)))  # hammer
    export(p, "Forge")

def campfire():
    p = dais_round(0.78)
    for i in range(8):
        a = rad(i * 45)
        p.append(box("StoneDark", (0.58 * math.cos(a), 0.58 * math.sin(a), 0.22),
                     (0.16, 0.13, 0.16), rotation=(0, 0, a)))                   # ring stones
    p.append(cyl("WoodDark", 0.07, 0.9, (0, 0, 0.34), rotation=(0, rad(80), rad(45))))
    p.append(cyl("WoodDark", 0.07, 0.9, (0, 0, 0.34), rotation=(0, rad(80), rad(-45))))
    p.append(cone("Fire", 0.20, 0.02, 0.55, (0, 0, 0.58)))                     # main flame
    p.append(cone("Fire", 0.12, 0.02, 0.38, (0.13, 0.06, 0.50)))
    p.append(cone("Fire", 0.10, 0.02, 0.30, (-0.11, -0.07, 0.46)))
    p.append(cyl("WoodDark", 0.04, 1.15, (0.72, 0, 0.72), vertices=6))         # spit post R
    p.append(cyl("WoodDark", 0.04, 1.15, (-0.72, 0, 0.72), vertices=6))        # spit post L
    p.append(cyl("Metal", 0.03, 1.55, (0, 0, 1.22), rotation=(0, rad(90), 0), vertices=6))  # spit bar
    p.append(cyl("Metal", 0.19, 0.24, (0, 0, 0.86), vertices=10))             # cook pot
    p.append(cyl("Gold", 0.20, 0.05, (0, 0, 1.00), vertices=10))              # pot gold rim
    export(p, "Campfire")

def garden():
    p = []
    p.append(box("Soil", (0, 0, 0.06), (2.25, 1.45, 0.12)))                   # plot base
    p.append(box("Gold", (0, 0, 0.12), (2.3, 1.5, 0.03)))                     # thin gold edge
    for sx in (-0.6, 0.6):
        p.append(box("WoodLight", (sx, 0, 0.20), (0.95, 0.55, 0.24)))         # bed frame
        p.append(box("Soil", (sx, 0, 0.30), (0.85, 0.45, 0.12)))             # soil
        for i, dy in enumerate((-0.14, 0.14)):
            for j, dx in enumerate((-0.28, 0.0, 0.28)):
                if (i + j) % 2 == 0:
                    p.append(sphere("Leaf", 0.11, (sx + dx, dy, 0.42)))
                else:
                    p.append(cone("Leaf", 0.08, 0.01, 0.24, (sx + dx, dy, 0.46), vertices=6))
    p.append(box("WoodDark", (0, -0.72, 0.85), (1.7, 0.05, 1.2)))             # trellis panel
    for gx in (-0.55, 0.0, 0.55):
        p.append(box("Leaf", (gx, -0.70, 1.05), (0.06, 0.04, 0.95)))          # climbing vines
    p.append(sphere("GemCyan", 0.06, (-0.55, -0.68, 1.35), segments=4, rings=3))  # glowing bud
    p.append(sphere("ClothRed", 0.08, (0.55, -0.68, 1.25)))                   # flower
    p.append(cyl("WoodDark", 0.24, 0.5, (1.15, 0.42, 0.31), vertices=10))     # water barrel
    p.append(cyl("Water", 0.20, 0.04, (1.15, 0.42, 0.57), vertices=10))       # barrel water
    p.append(cyl("Gold", 0.25, 0.04, (1.15, 0.42, 0.46), vertices=10))        # barrel band
    export(p, "Garden")

def merchant():
    p = dais_square(1.7, 0.55)
    p.append(box("WoodLight", (0, 0.15, 0.52), (1.6, 0.42, 0.72)))            # counter base
    p.append(box("WoodDark", (0, 0.15, 0.95), (1.7, 0.55, 0.10)))            # counter top
    p.append(box("Gold", (0, 0.44, 0.95), (1.7, 0.04, 0.12)))               # gold counter edge
    for px in (-0.75, 0.75):
        for py in (-0.35, 0.42):
            p.append(cyl("WoodDark", 0.05, 2.0, (px, py, 1.12), vertices=6))  # posts
    for i in range(6):                                                        # striped awning
        x = -0.68 + i * 0.272
        cloth = "ClothRed" if i % 2 == 0 else "ClothWhite"
        p.append(box(cloth, (x, 0.05, 2.20), (0.27, 1.05, 0.05), rotation=(rad(12), 0, 0)))
    p.append(box("Gold", (0, 0.52, 2.28), (1.68, 0.05, 0.05)))               # awning trim
    p.append(box("WoodDark", (1.10, -0.18, 0.40), (0.36, 0.36, 0.36), rotation=(0, 0, rad(15))))  # crate
    p.append(cyl("Gold", 0.12, 0.08, (-1.05, 0.15, 1.05), vertices=8))       # coin stack
    p.append(cyl("Gold", 0.11, 0.06, (-1.05, 0.15, 1.13), vertices=8))
    p.append(sphere("ClothBlue", 0.13, (0.70, 0.15, 1.06)))                  # pot
    p.append(cyl("Gold", 0.05, 0.16, (-0.75, 0.42, 1.72), vertices=6))       # lantern hook
    p.append(sphere("GemCyan", 0.08, (-0.75, 0.42, 1.56), segments=4, rings=3))  # lantern glow
    export(p, "Merchant")

def storage_chest():
    p = dais_square(0.95, 0.62)
    p.append(box("WoodLight", (0, 0, 0.44), (0.95, 0.60, 0.46)))             # body
    p.append(cyl("WoodDark", 0.31, 0.93, (0, 0, 0.68), rotation=(0, rad(90), 0)))  # curved lid
    for bx in (-0.28, 0.28):
        p.append(box("Gold", (bx, 0, 0.58), (0.08, 0.66, 0.62)))            # gold bands
    p.append(box("Gold", (0, 0.33, 0.60), (0.16, 0.06, 0.18)))              # gold lock
    for cx in (-0.44, 0.44):
        for cy in (-0.27, 0.27):
            p.append(box("Gold", (cx, cy, 0.24), (0.08, 0.08, 0.12)))       # corner caps
    p.append(sphere("ClothWhite", 0.24, (0.92, 0.22, 0.36), scale=(1, 1, 1.1)))  # sack
    export(p, "StorageChest")

def pool():
    p = dais_round(1.18)
    p.append(cyl("Stone", 1.05, 0.44, (0, 0, 0.40), vertices=12))            # basin
    p.append(torus("Gold", 1.05, 0.055, (0, 0, 0.60)))                      # gold rim ring
    p.append(cyl("Water", 0.90, 0.42, (0, 0, 0.42), vertices=12))           # water
    for i in range(4):
        a = rad(45 + i * 90)
        p.append(sphere("GemCyan", 0.10, (1.0 * math.cos(a), 1.0 * math.sin(a), 0.68),
                        segments=4, rings=3))                                # rim gems
    p.append(cyl("Stone", 0.12, 0.62, (0, 0, 0.66), vertices=6))            # fountain column
    p.append(cyl("Gold", 0.14, 0.05, (0, 0, 0.96), vertices=8))            # gold collar
    p.append(sphere("GemCyan", 0.16, (0, 0, 1.12), segments=4, rings=3))    # glowing orb
    export(p, "Pool")

def shrine():
    p = dais_square(1.15, 1.15)
    p.append(box("StoneDark", (0, 0, 0.30), (1.0, 1.0, 0.16)))             # bottom step
    p.append(box("Stone", (0, 0, 0.46), (0.8, 0.8, 0.16)))               # top step
    p.append(box("Stone", (0, 0, 1.02), (0.34, 0.34, 1.0)))              # pillar
    p.append(box("Gold", (0, 0, 0.64), (0.40, 0.40, 0.05)))              # gold band low
    p.append(box("Gold", (0, 0, 1.42), (0.40, 0.40, 0.05)))              # gold band high
    p.append(box("StoneDark", (0, 0, 1.57), (0.52, 0.52, 0.10)))          # cap
    p.append(cone("Crystal", 0.18, 0.01, 0.34, (0, 0, 2.02), vertices=6))  # crystal top
    p.append(cone("Crystal", 0.18, 0.01, 0.34, (0, 0, 1.72),
                  rotation=(rad(180), 0, 0), vertices=6))                   # crystal bottom
    p.append(sphere("GemCyan", 0.09, (0, 0, 1.87), segments=4, rings=3))    # glowing core
    for cx in (-0.52, 0.52):                                               # corner braziers
        p.append(cyl("Metal", 0.06, 0.5, (cx, 0, 0.62), vertices=6))
        p.append(cone("Fire", 0.10, 0.02, 0.22, (cx, 0, 0.92)))
    export(p, "Shrine")

def _hut(banner_cloth, sign_parts, window="Fire"):
    p = dais_square(1.5, 1.3)
    p.append(box("WoodLight", (0, 0, 0.78), (1.45, 1.25, 1.2)))            # walls
    p.append(box("WoodDark", (0, 0.64, 0.55), (0.42, 0.06, 0.82)))         # door
    p.append(box("Gold", (0, 0.66, 0.98), (0.5, 0.04, 0.06)))            # gold door lintel
    for wx in (-0.52, 0.52):
        p.append(box(window, (wx, 0.64, 1.0), (0.22, 0.05, 0.24)))       # hearth-lit windows
    p.append(cone("Thatch", 1.2, 0.04, 1.05, (0, 0, 1.92)))              # roof
    p.extend(gold_finial(2.48))                                          # gold roof finial
    p.append(cyl("WoodDark", 0.03, 1.5, (0.86, 0.55, 0.95), vertices=6))  # banner pole
    p.append(box(banner_cloth, (0.86, 0.55, 1.25), (0.03, 0.30, 0.5)))    # banner cloth
    p.extend(sign_parts())
    return p

def warriors_guild():
    def sign():
        return [
            box("Metal", (-0.42, 0.66, 1.12), (0.05, 0.04, 0.42), rotation=(0, rad(40), 0)),
            box("Metal", (-0.42, 0.66, 1.12), (0.05, 0.04, 0.42), rotation=(0, rad(-40), 0)),
            box("Gold", (-0.42, 0.66, 0.96), (0.24, 0.05, 0.06)),          # crossed-swords guard
        ]
    export(_hut("ClothRed", sign), "WarriorsGuild")

def rangers_guild():
    def sign():
        return [
            box("WoodDark", (-0.42, 0.66, 1.05), (0.05, 0.04, 0.5), rotation=(0, rad(45), 0)),
            box("WoodDark", (-0.42, 0.66, 1.05), (0.05, 0.04, 0.5), rotation=(0, rad(-45), 0)),
            cone("Gold", 0.05, 0.01, 0.1, (-0.27, 0.66, 1.22), rotation=(0, rad(45), 0), vertices=5),
            cone("Gold", 0.05, 0.01, 0.1, (-0.57, 0.66, 1.22), rotation=(0, rad(-45), 0), vertices=5),
        ]
    export(_hut("ClothGreen", sign), "RangersGuild")

def mages_guild():
    p = dais_round(0.82)
    p.append(cyl("Stone", 0.68, 1.7, (0, 0, 1.0), vertices=12))            # tower
    p.append(box("WoodDark", (0, 0.64, 0.60), (0.40, 0.12, 0.84)))         # door
    p.append(box("Gold", (0, 0.66, 1.05), (0.46, 0.04, 0.06)))          # gold door arch
    for wz in (1.15, 1.65):
        p.append(box("GemCyan", (0, 0.66, wz), (0.16, 0.06, 0.24)))       # glowing windows
    p.append(torus("Gold", 0.7, 0.05, (0, 0, 1.86)))                     # gold ring at eaves
    p.append(cone("ClothBlue", 0.85, 0.03, 0.9, (0, 0, 2.32), vertices=12))  # conical roof
    p.append(cyl("Gold", 0.05, 0.2, (0, 0, 2.82), vertices=6))           # finial stem
    p.append(sphere("Crystal", 0.13, (0, 0, 3.0), segments=4, rings=3))    # crystal orb finial
    p.append(box("ClothBlue", (0, 0.72, 1.35), (0.28, 0.05, 0.7)))         # banner
    export(p, "MagesGuild")

def watchtower():
    p = dais_square(1.0, 1.0)
    for px in (-0.45, 0.45):
        for py in (-0.45, 0.45):
            p.append(cyl("WoodDark", 0.08, 2.3, (px, py, 1.35), vertices=6))   # legs
            p.append(box("Stone", (px, py, 0.26), (0.26, 0.26, 0.30)))          # stone footing
    p.append(box("WoodDark", (0, 0.45, 0.95), (0.9, 0.05, 0.05)))          # cross braces
    p.append(box("WoodDark", (0, -0.45, 0.95), (0.9, 0.05, 0.05)))
    p.append(box("WoodLight", (0, 0, 2.50), (1.35, 1.35, 0.12)))           # platform
    p.append(box("Gold", (0, 0, 2.58), (1.42, 1.42, 0.04)))              # gold platform trim
    for (rx, ry, sx, sy) in [(0, 0.65, 1.35, 0.05), (0, -0.65, 1.35, 0.05),
                             (0.65, 0, 0.05, 1.35), (-0.65, 0, 0.05, 1.35)]:
        p.append(box("WoodDark", (rx, ry, 2.74), (sx, sy, 0.34)))          # railings
    p.append(cyl("Metal", 0.20, 0.30, (0, 0, 2.82), vertices=8))          # beacon brazier
    p.append(cone("Fire", 0.22, 0.02, 0.5, (0, 0, 3.18)))                # signal fire
    p.append(cyl("WoodDark", 0.03, 0.8, (0.62, 0.62, 3.1), vertices=6))    # flag pole
    p.append(box("ClothRed", (0.62, 0.48, 3.28), (0.02, 0.28, 0.30)))      # flag
    export(p, "Watchtower")

def portal():
    p = dais_round(1.25)
    p.append(box("StoneDark", (0, 0, 0.30), (1.7, 0.6, 0.28)))            # base plinth
    for px in (-0.62, 0.62):
        p.append(box("Stone", (px, 0, 1.18), (0.34, 0.34, 1.6)))         # pillars
        p.append(box("Gold", (px, 0, 1.98), (0.40, 0.40, 0.06)))        # pillar caps
        p.append(sphere("GemCyan", 0.10, (px, 0.20, 1.45), segments=4, rings=3))  # rune gems
    p.append(box("Stone", (0, 0, 2.08), (1.6, 0.34, 0.30)))             # lintel
    p.append(box("Gold", (0, 0, 2.28), (1.0, 0.40, 0.08)))             # gold keystone
    p.append(cyl("Crystal", 0.55, 0.06, (0, 0, 1.30), rotation=(rad(90), 0, 0), vertices=16))  # portal disc
    p.append(cyl("GemCyan", 0.38, 0.08, (0, -0.02, 1.30), rotation=(rad(90), 0, 0), vertices=16))  # swirl core
    for a in (0, 90, 180, 270):
        aa = rad(a)
        p.append(box("GemCyan", (0.72 * math.cos(aa), 0.72 * math.sin(aa), 0.19), (0.12, 0.12, 0.03)))  # floor runes
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
