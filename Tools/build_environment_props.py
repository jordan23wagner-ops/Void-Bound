# Void Bound — low-poly environment props for Homestead + Ashfields dressing.
# Run headless:
#   & "C:\Program Files\Blender Foundation\Blender 5.1\blender.exe" -b -P Tools\build_environment_props.py
# Exports one FBX per prop to Assets/Art/Models/Props/. Feet at z=0 (Unity pivot
# at ground). Material NAMES are the contract with EnvironmentDressing.cs, which
# assigns real URP materials by imported slot name. Reuses the building palette
# names (WoodDark/WoodLight/Stone/StoneDark/Metal/Gold/Fire/Water/GemCyan/Leaf/
# Soil/ClothRed) plus env-only names (Grass/GrassDry/DeadWood/Bone/Ash).

import bpy
import math
import os

OUT_DIR = r"C:\Users\Jordon\Void Bound\Assets\Art\Models\Props"

FBX_SETTINGS = dict(
    use_selection=True,
    apply_scale_options='FBX_SCALE_ALL',
    axis_forward='-Z',
    axis_up='Y',
    bake_space_transform=True,
    mesh_smooth_type='OFF',
    use_mesh_modifiers=True,
    bake_anim=False,
)

# diffuse_color doesn't export; names are the contract. Values are viewport-only.
PALETTE = {
    "WoodDark":  (0.30, 0.20, 0.12, 1), "WoodLight": (0.55, 0.40, 0.25, 1),
    "Stone":     (0.58, 0.57, 0.53, 1), "StoneDark": (0.36, 0.36, 0.35, 1),
    "Metal":     (0.45, 0.47, 0.50, 1), "Gold":      (0.82, 0.63, 0.20, 1),
    "Fire":      (1.00, 0.45, 0.10, 1), "Water":     (0.25, 0.60, 0.90, 1),
    "GemCyan":   (0.40, 0.85, 1.00, 1), "Leaf":      (0.28, 0.55, 0.24, 1),
    "Soil":      (0.30, 0.22, 0.15, 1), "ClothRed":  (0.70, 0.20, 0.18, 1),
    "Grass":     (0.42, 0.62, 0.30, 1), "GrassDry":  (0.55, 0.50, 0.30, 1),
    "DeadWood":  (0.26, 0.24, 0.22, 1), "Bone":      (0.86, 0.83, 0.72, 1),
    "Ash":       (0.34, 0.33, 0.35, 1), "Thatch":    (0.72, 0.60, 0.30, 1),
}

def rad(d):
    return math.radians(d)

def mat(name):
    m = bpy.data.materials.get(name)
    if m is None:
        m = bpy.data.materials.new(name)
        m.diffuse_color = PALETTE[name]
    return m

def box(mn, loc, scale, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, scale=scale, rotation=rot)
    ob = bpy.context.active_object; ob.data.materials.append(mat(mn)); return ob

def cyl(mn, r, d, loc, rot=(0, 0, 0), v=8):
    bpy.ops.mesh.primitive_cylinder_add(vertices=v, radius=r, depth=d, location=loc, rotation=rot)
    ob = bpy.context.active_object; ob.data.materials.append(mat(mn)); return ob

def cone(mn, r1, r2, d, loc, rot=(0, 0, 0), v=8):
    bpy.ops.mesh.primitive_cone_add(vertices=v, radius1=r1, radius2=r2, depth=d, location=loc, rotation=rot)
    ob = bpy.context.active_object; ob.data.materials.append(mat(mn)); return ob

def sph(mn, r, loc, scale=(1, 1, 1), seg=8, ring=6):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=ring, radius=r, location=loc, scale=scale)
    ob = bpy.context.active_object; ob.data.materials.append(mat(mn)); return ob

def export(parts, name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts:
        o.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    ob = bpy.context.active_object
    ob.name = name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT_DIR, name + ".fbx"), **FBX_SETTINGS)
    print(f"[Props] Exported {name}")
    bpy.ops.wm.read_factory_settings(use_empty=True)

# ─────────────────────────── Homestead (lush town) ───────────────────────────
def tree():
    p = [cyl("WoodDark", 0.16, 1.4, (0, 0, 0.7), v=6)]              # trunk
    p.append(sph("Leaf", 0.85, (0, 0, 1.7), scale=(1, 1, 0.9), seg=7, ring=5))
    p.append(sph("Leaf", 0.6, (0.4, 0.2, 2.1), seg=6, ring=4))
    p.append(sph("Leaf", 0.55, (-0.35, -0.2, 2.0), seg=6, ring=4))
    export(p, "Tree")

def bush():
    p = [sph("Leaf", 0.42, (0, 0, 0.34), seg=6, ring=4),
         sph("Leaf", 0.34, (0.32, 0.1, 0.30), seg=6, ring=4),
         sph("Leaf", 0.30, (-0.28, -0.12, 0.28), seg=6, ring=4)]
    export(p, "Bush")

def rock():
    p = [sph("StoneDark", 0.5, (0, 0, 0.28), scale=(1.2, 1.0, 0.75), seg=6, ring=4),
         sph("Stone", 0.28, (0.4, 0.2, 0.2), scale=(1, 1, 0.8), seg=5, ring=3)]
    export(p, "Rock")

def grass_tuft():
    p = []
    for i, (dx, dy, tilt) in enumerate([(0, 0, 0), (0.08, 0.05, 12), (-0.07, 0.06, -14), (0.05, -0.08, 8)]):
        p.append(cone("Grass", 0.05, 0.005, 0.4, (dx, dy, 0.2), rot=(rad(tilt), 0, rad(i * 30)), v=4))
    export(p, "GrassTuft")

def flowers():
    p = [sph("Leaf", 0.18, (0, 0, 0.12), seg=6, ring=4)]
    for (dx, dy, col) in [(0.12, 0.05, "ClothRed"), (-0.1, 0.08, "Gold"), (0.02, -0.12, "GemCyan")]:
        p.append(cyl("Grass", 0.02, 0.24, (dx, dy, 0.24), v=4))
        p.append(sph(col, 0.07, (dx, dy, 0.38), seg=5, ring=3))
    export(p, "Flowers")

def fence():
    p = []
    for px in (-0.8, 0.8):
        p.append(box("WoodDark", (px, 0, 0.45), (0.1, 0.1, 0.9)))       # posts
        p.append(cone("WoodDark", 0.09, 0.02, 0.12, (px, 0, 0.96), v=4))  # post cap
    for pz in (0.35, 0.68):
        p.append(box("WoodLight", (0, 0, pz), (1.7, 0.06, 0.08)))        # rails
    export(p, "Fence")

def lamppost():
    p = [box("StoneDark", (0, 0, 0.1), (0.3, 0.3, 0.2)),                 # base
         cyl("WoodDark", 0.06, 1.8, (0, 0, 1.0), v=6),                   # post
         box("Gold", (0, 0, 1.9), (0.12, 0.12, 0.06)),                  # collar
         box("GemCyan", (0, 0, 2.02), (0.16, 0.16, 0.2)),              # glowing lantern
         cone("Gold", 0.16, 0.02, 0.12, (0, 0, 2.2), v=4)]              # cap
    export(p, "Lamppost")

def well():
    p = [cyl("Stone", 0.62, 0.6, (0, 0, 0.3), v=10),                    # wall
         cyl("StoneDark", 0.5, 0.5, (0, 0, 0.34), v=10),               # shaft
         cyl("Water", 0.46, 0.05, (0, 0, 0.42), v=10),                 # water
         cyl("Gold", 0.64, 0.06, (0, 0, 0.6), v=10)]                   # gold rim
    for px in (-0.55, 0.55):
        p.append(cyl("WoodDark", 0.05, 1.1, (px, 0, 1.05), v=6))        # roof posts
    p.append(cyl("WoodDark", 0.05, 0.5, (0, 0, 0.9), rot=(0, rad(90), 0), v=6))  # windlass
    p.append(box("WoodLight", (0, 0, 1.7), (1.5, 0.9, 0.08), rot=(rad(20), 0, 0)))  # roof R
    p.append(box("WoodLight", (0, 0, 1.7), (1.5, 0.9, 0.08), rot=(rad(-20), 0, 0)))  # roof L
    export(p, "Well")

def barrel():
    p = [cyl("WoodLight", 0.28, 0.7, (0, 0, 0.35), v=10),
         cyl("Metal", 0.29, 0.05, (0, 0, 0.18), v=10),
         cyl("Metal", 0.29, 0.05, (0, 0, 0.52), v=10),
         cyl("Gold", 0.29, 0.04, (0, 0, 0.35), v=10)]
    export(p, "Barrel")

def crate():
    p = [box("WoodLight", (0, 0, 0.3), (0.6, 0.6, 0.6)),
         box("WoodDark", (0, 0, 0.3), (0.64, 0.08, 0.64)),
         box("WoodDark", (0, 0, 0.3), (0.08, 0.64, 0.64))]
    export(p, "Crate")

# ─── Central town bonfire (focal landmark the village circles around) ───
def bonfire():
    p = []
    for i in range(10):                                                # stone ring
        a = rad(i * 36)
        p.append(box("Stone", (0.95 * math.cos(a), 0.95 * math.sin(a), 0.18),
                     (0.3, 0.22, 0.34), rot=(0, 0, a)))
    p.append(cyl("Ash", 0.98, 0.08, (0, 0, 0.05), v=12))               # ash bed
    for a in (0, 60, 120):                                             # crisscrossed logs
        aa = rad(a)
        p.append(cyl("WoodDark", 0.1, 1.5, (0, 0, 0.36), rot=(rad(90), 0, aa), v=6))
    for a in (30, 90, 150):
        aa = rad(a)
        p.append(cyl("DeadWood", 0.09, 1.4, (0, 0, 0.56), rot=(rad(78), 0, aa), v=6))
    p.append(cone("Fire", 0.46, 0.02, 1.4, (0, 0, 0.95)))              # tall layered flames
    p.append(cone("Fire", 0.32, 0.02, 1.0, (0.16, 0.1, 0.85)))
    p.append(cone("Fire", 0.26, 0.02, 0.8, (-0.14, -0.1, 0.8)))
    p.append(cone("Fire", 0.16, 0.02, 0.55, (0.05, -0.12, 0.72)))
    for a in (35, 155, 275):                                           # seating logs
        aa = rad(a)
        p.append(cyl("WoodLight", 0.16, 0.9, (1.75 * math.cos(aa), 1.75 * math.sin(aa), 0.16),
                     rot=(0, rad(90), aa), v=8))
    export(p, "Bonfire")

# ─── Homestead homes (non-interactive cottages that flesh out the village) ───
def cottage():
    p = [box("Stone", (0, 0, 0.2), (2.0, 1.7, 0.4))]                    # foundation
    p.append(box("WoodLight", (0, 0, 1.0), (1.9, 1.6, 1.2)))            # walls
    for sx in (-0.9, 0.9):                                              # corner beams
        p.append(box("WoodDark", (sx, 0.81, 1.0), (0.1, 0.06, 1.2)))
    p.append(box("WoodDark", (0, 0.81, 0.42), (1.9, 0.06, 0.1)))        # sill
    p.append(box("WoodDark", (0, 0.81, 1.58), (1.9, 0.06, 0.1)))        # header
    p.append(box("WoodDark", (0, 0.81, 1.0), (0.08, 0.06, 1.2)))        # center stud
    p.append(box("WoodDark", (0.5, 0.82, 0.7), (0.5, 0.06, 0.9)))       # door
    p.append(box("Gold", (0.5, 0.84, 1.18), (0.56, 0.05, 0.06)))        # lintel
    p.append(box("Fire", (-0.5, 0.82, 1.05), (0.42, 0.05, 0.42)))       # lit window
    p.append(box("Thatch", (0, -0.55, 2.25), (2.25, 1.25, 0.16), rot=(rad(36), 0, 0)))  # roof slabs
    p.append(box("Thatch", (0, 0.55, 2.25), (2.25, 1.25, 0.16), rot=(rad(-36), 0, 0)))
    p.append(box("WoodDark", (0, 0, 2.62), (2.25, 0.12, 0.1)))          # ridge
    p.append(box("Stone", (-0.7, -0.45, 2.35), (0.3, 0.3, 0.95)))       # chimney
    p.append(box("Fire", (-0.7, -0.45, 2.82), (0.2, 0.2, 0.06)))        # ember
    export(p, "Cottage")

def house():
    p = [box("Stone", (0, 0, 0.25), (2.4, 2.0, 0.5))]                   # foundation
    p.append(box("WoodLight", (0, 0, 1.6), (2.2, 1.8, 2.2)))            # two-storey walls
    p.append(box("WoodDark", (0, 0.91, 1.5), (2.2, 0.06, 0.12)))        # storey band
    for sx in (-1.05, 1.05):
        p.append(box("WoodDark", (sx, 0.91, 1.6), (0.12, 0.06, 2.2)))   # corner beams
    p.append(box("WoodDark", (0, 0.92, 0.9), (0.55, 0.06, 1.1)))        # door
    p.append(box("Gold", (0, 0.94, 1.5), (0.6, 0.05, 0.07)))            # lintel
    p.append(box("Fire", (-0.65, 0.92, 2.3), (0.36, 0.05, 0.42)))       # upper windows
    p.append(box("Fire", (0.65, 0.92, 2.3), (0.36, 0.05, 0.42)))
    p.append(box("Fire", (0.7, 0.92, 1.2), (0.34, 0.05, 0.4)))          # lower window
    p.append(box("Thatch", (0, -0.62, 3.05), (2.55, 1.4, 0.16), rot=(rad(38), 0, 0)))  # roof
    p.append(box("Thatch", (0, 0.62, 3.05), (2.55, 1.4, 0.16), rot=(rad(-38), 0, 0)))
    p.append(box("WoodDark", (0, 0, 3.45), (2.55, 0.12, 0.12)))         # ridge
    p.append(box("Stone", (0.8, -0.6, 3.15), (0.34, 0.34, 1.0)))        # chimney
    p.append(box("Gold", (0.8, -0.6, 3.66), (0.4, 0.4, 0.06)))          # chimney cap
    export(p, "House")

# ─────────────────────────── Ashfields (ashen waste) ─────────────────────────
def dead_tree():
    p = [cyl("DeadWood", 0.15, 1.6, (0, 0, 0.8), v=6)]                  # trunk
    for (dx, dy, ang, ln) in [(0.3, 0.1, 55, 0.9), (-0.25, -0.05, -50, 0.8), (0.05, 0.25, 20, 0.7)]:
        p.append(cyl("DeadWood", 0.05, ln, (dx, dy, 1.5), rot=(rad(ang), 0, rad(dy * 200)), v=5))
    export(p, "DeadTree")

def boulder():
    p = [sph("StoneDark", 0.8, (0, 0, 0.45), scale=(1.2, 1.0, 0.7), seg=6, ring=4),
         sph("Stone", 0.4, (0.5, 0.3, 0.3), scale=(1, 1, 0.8), seg=5, ring=3),
         sph("Ash", 0.3, (-0.5, -0.3, 0.12), scale=(1.3, 1.3, 0.4), seg=6, ring=3)]
    export(p, "Boulder")

def bones():
    p = [sph("Bone", 0.22, (0, 0, 0.14), scale=(1, 1, 0.85), seg=6, ring=4)]   # skull
    p.append(box("Bone", (0.06, 0.16, 0.05), (0.04, 0.1, 0.04)))               # jaw
    for i, dx in enumerate((-0.35, -0.2, 0.2, 0.35)):
        p.append(cyl("Bone", 0.025, 0.5, (dx, -0.1, 0.06), rot=(rad(90), 0, rad(10 * i)), v=5))  # ribs
    export(p, "Bones")

def brazier():
    p = [cyl("Metal", 0.05, 0.9, (0, 0, 0.45), v=6)]                    # stem
    for a in (0, 120, 240):
        aa = rad(a)
        p.append(cyl("Metal", 0.03, 0.5, (0.15 * math.cos(aa), 0.15 * math.sin(aa), 0.25),
                     rot=(rad(20), 0, aa), v=4))                        # legs
    p.append(cone("Metal", 0.28, 0.16, 0.24, (0, 0, 0.92), v=8))       # bowl
    p.append(cyl("Gold", 0.29, 0.04, (0, 0, 1.04), v=8))              # gold rim
    p.append(cone("Fire", 0.2, 0.02, 0.5, (0, 0, 1.2)))               # flame
    p.append(cone("Fire", 0.1, 0.02, 0.3, (0.08, 0.05, 1.15)))
    export(p, "Brazier")

def broken_pillar():
    p = [cyl("Stone", 0.34, 0.3, (0, 0, 0.15), v=10),                   # base
         cyl("StoneDark", 0.3, 0.2, (0, 0, 0.32), v=10),               # torus-ish
         cyl("Stone", 0.26, 1.0, (0, 0, 0.9), v=10),                   # shaft (broken top)
         box("Stone", (0.1, 0, 1.45), (0.5, 0.5, 0.18), rot=(rad(12), 0, rad(8)))]  # cracked cap
    p.append(sph("StoneDark", 0.18, (0.6, 0.2, 0.14), scale=(1.2, 1, 0.6), seg=5, ring=3))  # rubble
    export(p, "BrokenPillar")

def spikes():
    p = []
    for (dx, dy, ang) in [(-0.35, 0, 18), (0.0, 0.1, -12), (0.35, -0.05, 22)]:
        p.append(cone("WoodDark", 0.08, 0.005, 1.3, (dx, dy, 0.6), rot=(rad(ang), rad(ang), 0), v=6))
    p.append(box("WoodDark", (0, 0, 0.2), (0.9, 0.14, 0.1)))           # cross-tie
    export(p, "Spikes")


if __name__ == "__main__":
    os.makedirs(OUT_DIR, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    # Homestead
    tree(); bush(); rock(); grass_tuft(); flowers(); fence(); lamppost(); well(); barrel(); crate()
    cottage(); house(); bonfire()
    # Ashfields
    dead_tree(); boulder(); bones(); brazier(); broken_pillar(); spikes()
    print("[Props] Done - environment props exported.")
