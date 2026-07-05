# Void Bound - low-poly equipment + item models.
# Run headless:
#   & "C:\Program Files\Blender Foundation\Blender 5.1\blender.exe" -b -P Tools\build_equipment_models.py
# Exports one FBX per model to Assets/Resources/Equipment/ using the
# CODING_STANDARDS.md FBX settings. Material NAMES are the contract with
# EquipmentVisuals.cs: "Main" is rarity-tinted at runtime, "Accent" stays neutral.
#
# Authoring frames (Blender Z-up, feet/ground at z=0, exported axis_up=Y so
# Blender +Z -> Unity +Y, Blender +Y -> Unity -Z):
#   Weapons + Shield: GRIP-space, grip at origin, blade/face along +Z. Parented
#     at the hand socket.
#   Armor (helm/body/legs/boots/gloves/cape/amulet): HERO-BODY space (same coords
#     as build_character_models.py's hero), parented at the character root so it
#     lands over the body. Goblin root carries a downscale to fit.
#   Materials: centered small props for world pickups.

import bpy
import math
import os

OUT_DIR = r"C:\Users\Jordon\Void Bound\Assets\Resources\Equipment"

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

# Placeholder colors. Unity restyles by material name (EquipmentVisuals):
# Main -> rarity tint, Gold -> gold trim, Gem -> glowing cyan, Accent stays ~this.
COLORS = {
    "Main":    (0.60, 0.62, 0.66, 1.0),
    "Accent":  (0.20, 0.16, 0.28, 1.0),   # dark violet lining
    "Gold":    (0.83, 0.66, 0.22, 1.0),
    "Dark":    (0.10, 0.10, 0.13, 1.0),    # obsidian plate
    "Crimson": (0.50, 0.08, 0.10, 1.0),
    "Gem":     (0.45, 0.85, 1.00, 1.0),    # cyan
    "GemV":    (0.62, 0.28, 0.95, 1.0),    # violet
    "GemR":    (0.95, 0.18, 0.20, 1.0),    # red
    "GemG":    (0.35, 0.95, 0.45, 1.0),    # green
}

def rad(d):
    return math.radians(d)

def mat(name):
    m = bpy.data.materials.get(name)
    if m is None:
        m = bpy.data.materials.new(name)
        m.diffuse_color = COLORS[name]
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
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor,
                                     location=location, rotation=rotation,
                                     major_segments=12, minor_segments=6)
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
    print(f"[Equip] Exported {path}")
    bpy.ops.wm.read_factory_settings(use_empty=True)


def export_armor(pairs, name):
    # pairs: list of (object, bone_name). Armor spans several limbs, so it can't
    # ride a single bone — split it into one merged mesh per bone, each object
    # NAMED for its bone, and export them as separate objects. EquipmentVisuals
    # then parents each sub-part to the matching skeleton bone so it follows that
    # limb (matches the character's rigid one-bone-per-part deformation).
    groups = {}
    order = []
    for ob, bone in pairs:
        if bone not in groups:
            groups[bone] = []
            order.append(bone)
        groups[bone].append(ob)
    roots = []
    for bone in order:
        objs = groups[bone]
        if len(objs) > 1:
            bpy.ops.object.select_all(action='DESELECT')
            for o in objs:
                o.select_set(True)
            bpy.context.view_layer.objects.active = objs[0]
            bpy.ops.object.join()
            ob = bpy.context.active_object
        else:
            ob = objs[0]
        ob.name = bone
        roots.append(ob)
    bpy.ops.object.select_all(action='DESELECT')
    for o in roots:
        o.select_set(True)
    bpy.context.view_layer.objects.active = roots[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    path = os.path.join(OUT_DIR, name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, **FBX_SETTINGS)
    print(f"[Equip] Exported {path} (per-bone parts: {', '.join(order)})")
    bpy.ops.wm.read_factory_settings(use_empty=True)


# ── WEAPONS (grip at origin, blade along +Z) ──────────────────
def sword():
    p = []
    p.append(box("Main", (0, 0, 0.34), (0.05, 0.015, 0.58)))     # blade
    p.append(cone("Main", 0.035, 0.002, 0.10, (0, 0, 0.68)))     # tip
    p.append(box("Accent", (0, 0, 0.04), (0.20, 0.05, 0.04)))    # guard
    p.append(cyl("Accent", 0.028, 0.16, (0, 0, -0.05)))          # grip
    p.append(sphere("Accent", 0.04, (0, 0, -0.15)))              # pommel
    export(p, "Sword")

def sword2h():
    p = []
    p.append(box("Main", (0, 0, 0.50), (0.07, 0.02, 0.85)))
    p.append(cone("Main", 0.05, 0.002, 0.14, (0, 0, 0.99)))
    p.append(box("Accent", (0, 0, 0.05), (0.30, 0.06, 0.05)))
    p.append(cyl("Accent", 0.032, 0.30, (0, 0, -0.12)))
    p.append(sphere("Accent", 0.05, (0, 0, -0.29)))
    export(p, "Sword2H")

def dagger():
    p = []
    p.append(box("Main", (0, 0, 0.20), (0.045, 0.014, 0.30)))
    p.append(cone("Main", 0.03, 0.002, 0.08, (0, 0, 0.38)))
    p.append(box("Accent", (0, 0, 0.03), (0.13, 0.04, 0.03)))
    p.append(cyl("Accent", 0.025, 0.12, (0, 0, -0.05)))
    export(p, "Dagger")

def mace():
    p = []
    p.append(cyl("Accent", 0.03, 0.42, (0, 0, 0.10)))            # haft
    p.append(sphere("Main", 0.10, (0, 0, 0.36)))                 # head
    for i in range(4):
        a = rad(i * 90)
        p.append(cone("Main", 0.04, 0.005, 0.09,
                      (0.11 * math.cos(a), 0.11 * math.sin(a), 0.36),
                      rotation=(rad(90) if i % 2 else -rad(90), 0, 0) if False else (0, 0, 0)))
    export(p, "Mace")

def bow():
    # Grip at origin (like the other weapons) so it seats in the hand.
    p = []
    p.append(torus("Main", 0.34, 0.02, (0, 0.05, 0.0), rotation=(rad(90), 0, 0)))  # arc (front half visible)
    p.append(cyl("Accent", 0.025, 0.14, (0, 0, 0.0)))          # grip wrap
    p.append(box("Accent", (0, 0.02, 0.0), (0.006, 0.02, 0.66)))  # string
    export(p, "Bow")

def crossbow():
    p = []
    p.append(box("Accent", (0, 0.14, 0.14), (0.06, 0.42, 0.06)))   # stock
    p.append(box("Main", (0, 0.30, 0.14), (0.60, 0.05, 0.05)))     # limbs
    p.append(cyl("Accent", 0.03, 0.14, (0, -0.02, 0.06), rotation=(rad(90), 0, 0)))  # grip
    export(p, "Crossbow")

def staff():
    p = []
    p.append(cyl("Accent", 0.028, 1.05, (0, 0, 0.45)))          # pole
    p.append(cone("Main", 0.10, 0.01, 0.22, (0, 0, 1.05), vertices=6))   # crystal top
    p.append(cone("Main", 0.10, 0.01, 0.22, (0, 0, 0.83), rotation=(rad(180), 0, 0), vertices=6))
    export(p, "Staff")

def wand():
    p = []
    p.append(cyl("Accent", 0.02, 0.34, (0, 0, 0.15)))
    p.append(sphere("Main", 0.05, (0, 0, 0.36)))
    export(p, "Wand")

def shield():
    # GRIP-space (attaches at off-hand socket): boss at origin, face toward -Y
    p = []
    p.append(cyl("Main", 0.26, 0.05, (0, 0, 0), rotation=(rad(90), 0, 0), vertices=12))  # round face
    p.append(torus("Accent", 0.26, 0.025, (0, 0, 0), rotation=(rad(90), 0, 0)))          # rim
    p.append(sphere("Accent", 0.06, (0, -0.04, 0)))             # central boss
    export(p, "Shield")


# ── ARMOR (hero-body space; each part tagged with the bone it follows) ──
# Bone names match build_character_models.py's hero skeleton; +X = _R side.
def helm():
    p = [
        (sphere("Main", 0.155, (0, 0, 1.63), scale=(1.05, 1.05, 0.85)), "Head"),  # dome
        (box("Accent", (0, 0.12, 1.56), (0.30, 0.06, 0.06)), "Head"),             # brow band
        (cone("Main", 0.03, 0.005, 0.12, (0, 0, 1.80), vertices=6), "Head"),      # spike
    ]
    export_armor(p, "Helm")

def body_armor():
    p = [
        (box("Main", (0, 0.16, 1.14), (0.34, 0.10, 0.42)), "Chest"),   # chest plate
        (box("Accent", (0, 0.20, 1.30), (0.10, 0.04, 0.12)), "Chest"),  # emblem
        (sphere("Main", 0.13, (0.28, 0, 1.38)), "UpperArm_R"),          # shoulder R (follows the arm)
        (sphere("Main", 0.13, (-0.28, 0, 1.38)), "UpperArm_L"),         # shoulder L
    ]
    export_armor(p, "Body")

def legs_armor():
    p = [
        (box("Main", (0.12, 0.10, 0.55), (0.14, 0.16, 0.36)), "UpperLeg_R"),
        (box("Main", (-0.12, 0.10, 0.55), (0.14, 0.16, 0.36)), "UpperLeg_L"),
        (box("Accent", (0, 0.12, 0.80), (0.34, 0.14, 0.10)), "Hips"),  # belt
    ]
    export_armor(p, "Legs")

def boots():
    p = [
        (box("Main", (0.12, 0.05, 0.10), (0.15, 0.26, 0.20)), "Foot_R"),   # shin/ankle
        (box("Accent", (0.12, 0.10, 0.03), (0.15, 0.34, 0.08)), "Foot_R"),  # foot
        (box("Main", (-0.12, 0.05, 0.10), (0.15, 0.26, 0.20)), "Foot_L"),
        (box("Accent", (-0.12, 0.10, 0.03), (0.15, 0.34, 0.08)), "Foot_L"),
    ]
    export_armor(p, "Boots")

def gloves():
    p = [
        (sphere("Main", 0.075, (0.30, 0, 0.74)), "Hand_R"),        # hand
        (cyl("Main", 0.07, 0.16, (0.30, 0, 0.90)), "Hand_R"),      # bracer
        (sphere("Main", 0.075, (-0.30, 0, 0.74)), "Hand_L"),
        (cyl("Main", 0.07, 0.16, (-0.30, 0, 0.90)), "Hand_L"),
    ]
    export_armor(p, "Gloves")

def cape():
    p = [
        (box("Main", (0, -0.17, 1.30), (0.44, 0.04, 0.16)), "Chest"),   # collar
        (box("Main", (0, -0.20, 0.92), (0.50, 0.03, 0.72), rotation=(rad(-6), 0, 0)), "Chest"),  # drape
    ]
    export_armor(p, "Cape")

def amulet():
    p = [
        (torus("Accent", 0.09, 0.012, (0, 0.06, 1.44), rotation=(rad(90), 0, 0)), "Neck"),  # cord
        (sphere("Main", 0.05, (0, 0.13, 1.36)), "Neck"),              # pendant
    ]
    export_armor(p, "Amulet")


# ── CLASS ARMOR (ranger leather, mage cloth) — distinct silhouettes ──
def ranger_hood():
    # Hood over the head with a forward peak; reads as a leather hood.
    p = [
        (sphere("Main", 0.175, (0, -0.02, 1.60), scale=(1.0, 1.12, 1.05)), "Head"),  # hood dome (back-weighted)
        (cone("Main", 0.11, 0.02, 0.18, (0, 0.15, 1.63), rotation=(rad(-38), 0, 0), vertices=6), "Head"),  # peak forward
        (box("Accent", (0, 0.13, 1.51), (0.22, 0.06, 0.09)), "Head"),   # brow trim
    ]
    export_armor(p, "RangerHood")

def ranger_vest():
    # Leather vest with straps and small shoulder caps (lighter than plate).
    p = [
        (box("Main", (0, 0.14, 1.12), (0.30, 0.13, 0.42)), "Chest"),       # torso vest
        (box("Accent", (0.10, 0.03, 1.10), (0.05, 0.14, 0.46)), "Chest"),  # strap R
        (box("Accent", (-0.10, 0.03, 1.10), (0.05, 0.14, 0.46)), "Chest"), # strap L
        (sphere("Main", 0.095, (0.27, 0, 1.37), scale=(1, 1, 0.6)), "UpperArm_R"),  # shoulder cap
        (sphere("Main", 0.095, (-0.27, 0, 1.37), scale=(1, 1, 0.6)), "UpperArm_L"),
    ]
    export_armor(p, "RangerVest")

def mage_hat():
    # Grand wizard hat: wide gold-rimmed brim, a tall cone that bends and droops
    # forward in three segments, a jeweled hatband, and a gem-tipped point.
    p = []
    # brim — wide, gently domed, gold rim
    p.append((cone("Main", 0.33, 0.30, 0.05, (0, 0.03, 1.585), vertices=16), "Head"))          # wide brim
    p.append((torus("Gold", 0.325, 0.018, (0, 0.03, 1.585), rotation=(rad(90), 0, 0)), "Head")) # gold rim
    p.append((cone("Main", 0.23, 0.195, 0.07, (0, 0.03, 1.635), vertices=14), "Head"))          # domed crown of brim
    # hatband + faceted gem at the front
    p.append((cyl("Accent", 0.20, 0.075, (0, 0.03, 1.70), vertices=14), "Head"))                # band
    p.append((torus("Gold", 0.205, 0.01, (0, 0.03, 1.735), rotation=(rad(90), 0, 0)), "Head"))  # band top trim
    p.append((sphere("Gem", 0.055, (0, 0.205, 1.70), scale=(1.0, 0.6, 1.4), segments=4, rings=3), "Head"))  # gem
    # cone — three bending segments drooping forward (+Y)
    p.append((cone("Main", 0.19, 0.14, 0.30, (0, 0.03, 1.88), vertices=12), "Head"))            # base (straight)
    p.append((cone("Main", 0.14, 0.09, 0.30, (0, 0.086, 2.17), rotation=(rad(-22), 0, 0), vertices=10), "Head"))  # mid bend
    p.append((cone("Main", 0.09, 0.02, 0.28, (0, 0.253, 2.40), rotation=(rad(-52), 0, 0), vertices=8), "Head"))   # tip droop
    # gem star at the tip
    p.append((sphere("Gem", 0.04, (0, 0.40, 2.49), segments=4, rings=3), "Head"))               # tip gem
    p.append((cone("Gold", 0.02, 0.002, 0.05, (0, 0.42, 2.53), rotation=(rad(-58), 0, 0), vertices=6), "Head"))  # tip cap
    export_armor(p, "MageHat")

def mage_robe_top():
    # Fitted upper robe: tapered torso, gold-trimmed front placket, a high collar,
    # a jewelled chest brooch, and layered gem-tipped shoulder pauldrons.
    p = []
    p.append((cone("Main", 0.25, 0.205, 0.52, (0, 0.03, 1.14), vertices=10), "Chest"))          # body
    p.append((box("Accent", (0, 0.185, 1.10), (0.11, 0.05, 0.46)), "Chest"))                    # dark placket
    p.append((box("Gold", (0.06, 0.205, 1.10), (0.02, 0.02, 0.46)), "Chest"))                   # gold trim R
    p.append((box("Gold", (-0.06, 0.205, 1.10), (0.02, 0.02, 0.46)), "Chest"))                  # gold trim L
    p.append((cone("Main", 0.11, 0.16, 0.17, (0, -0.03, 1.45), vertices=10), "Chest"))          # high collar (flares up/back)
    p.append((torus("Gold", 0.135, 0.012, (0, -0.03, 1.52), rotation=(rad(70), 0, 0)), "Chest"))# collar rim
    p.append((torus("Gold", 0.052, 0.014, (0, 0.225, 1.24), rotation=(rad(90), 0, 0)), "Chest"))# brooch ring
    p.append((sphere("Gem", 0.04, (0, 0.245, 1.24), segments=4, rings=3), "Chest"))             # brooch gem
    # pauldrons
    p.append((sphere("Main", 0.12, (0.28, 0, 1.38), scale=(1.05, 1.05, 0.8)), "UpperArm_R"))
    p.append((torus("Gold", 0.11, 0.016, (0.28, 0, 1.34), rotation=(rad(22), 0, 0)), "UpperArm_R"))
    p.append((cone("Gold", 0.035, 0.005, 0.13, (0.285, 0, 1.51), vertices=6), "UpperArm_R"))
    p.append((sphere("Gem", 0.03, (0.285, 0.09, 1.41), segments=4, rings=2), "UpperArm_R"))
    p.append((sphere("Main", 0.12, (-0.28, 0, 1.38), scale=(1.05, 1.05, 0.8)), "UpperArm_L"))
    p.append((torus("Gold", 0.11, 0.016, (-0.28, 0, 1.34), rotation=(rad(-22), 0, 0)), "UpperArm_L"))
    p.append((cone("Gold", 0.035, 0.005, 0.13, (-0.285, 0, 1.51), vertices=6), "UpperArm_L"))
    p.append((sphere("Gem", 0.03, (-0.285, 0.09, 1.41), segments=4, rings=2), "UpperArm_L"))
    export_armor(p, "MageRobeTop")

def mage_robe_bottom():
    # Flowing lower robe: a jewelled belt, a hanging front sash with a tassel, and
    # two elegant skirt panels (one per leg) with gold hems and a dark lining.
    p = []
    p.append((cyl("Gold", 0.235, 0.075, (0, 0.03, 0.86), vertices=14), "Hips"))                 # belt
    p.append((sphere("Gem", 0.055, (0, 0.235, 0.86), scale=(1.3, 0.6, 1.0), segments=4, rings=3), "Hips"))  # buckle gem
    p.append((box("Main", (0, 0.215, 0.55), (0.10, 0.03, 0.52)), "Hips"))                       # front sash
    p.append((box("Gold", (0, 0.232, 0.55), (0.035, 0.02, 0.52)), "Hips"))                      # sash trim
    p.append((sphere("Gold", 0.045, (0, 0.22, 0.28), scale=(1, 1, 1.3), segments=6, rings=4), "Hips"))  # tassel bead
    for sx, bone in ((0.11, "UpperLeg_R"), (-0.11, "UpperLeg_L")):
        p.append((cone("Main", 0.25, 0.175, 0.88, (sx, 0.02, 0.46), vertices=8), bone))         # panel
        p.append((cone("Accent", 0.205, 0.14, 0.82, (sx, 0.05, 0.44), vertices=7), bone))       # dark lining (peeks)
        p.append((torus("Gold", 0.245, 0.02, (sx, 0.02, 0.06), rotation=(rad(90), 0, 0)), bone))# gold hem
    export_armor(p, "MageRobeBottom")


# ── DREAD KNIGHT (obsidian plate, dark gold, violet gem-glow) ──
def dread_helm():
    # Menacing horned great-helm with a glowing violet eye-slit.
    p = []
    p.append((sphere("Dark", 0.165, (0, 0, 1.60), scale=(1.0, 1.05, 1.08)), "Head"))            # skull dome
    p.append((box("Dark", (0, 0.11, 1.55), (0.27, 0.13, 0.22)), "Head"))                        # faceplate
    p.append((box("Dark", (0, 0.15, 1.47), (0.20, 0.06, 0.10), rotation=(rad(20), 0, 0)), "Head"))  # jaw/chin bevel
    p.append((box("GemV", (0, 0.185, 1.60), (0.20, 0.028, 0.032)), "Head"))                     # glowing eye slit
    p.append((box("Gold", (0, 0.17, 1.665), (0.24, 0.05, 0.045)), "Head"))                      # brow ridge
    p.append((box("Dark", (0, 0.20, 1.55), (0.03, 0.05, 0.12)), "Head"))                        # nasal bar
    # horns — two segments each, curling up and back
    for sx in (0.13, -0.13):
        z = rad(22 if sx > 0 else -22)
        p.append((cone("Dark", 0.05, 0.032, 0.15, (sx, 0.03, 1.71), rotation=(rad(12), 0, z)), "Head"))
        p.append((cone("Dark", 0.032, 0.004, 0.18, (sx * 1.35, -0.02, 1.86), rotation=(rad(40), 0, z * 1.4)), "Head"))
        p.append((torus("Gold", 0.05, 0.009, (sx, 0.03, 1.70), rotation=(rad(90), 0, 0)), "Head"))
    p.append((cone("Dark", 0.035, 0.004, 0.14, (0, -0.02, 1.82), rotation=(rad(-8), 0, 0)), "Head"))  # crown spike
    export_armor(p, "DreadHelm")

def dread_chest():
    # Blackened breastplate with a violet core gem and sharp spiked pauldrons.
    p = []
    p.append((box("Dark", (0, 0.16, 1.14), (0.34, 0.14, 0.44)), "Chest"))                       # breastplate
    p.append((cone("Dark", 0.21, 0.145, 0.22, (0, 0.18, 1.31), rotation=(rad(90), 0, 0), vertices=6), "Chest"))  # chest bevel
    p.append((sphere("GemV", 0.06, (0, 0.245, 1.15), segments=4, rings=3), "Chest"))            # core gem
    p.append((torus("Gold", 0.085, 0.014, (0, 0.24, 1.15), rotation=(rad(90), 0, 0)), "Chest")) # gem ring
    p.append((box("Gold", (0, 0.185, 1.35), (0.30, 0.05, 0.045)), "Chest"))                     # collar edge
    p.append((box("Gold", (0, 0.15, 0.94), (0.34, 0.05, 0.045)), "Chest"))                      # waist edge
    p.append((box("Crimson", (0, 0.235, 1.05), (0.03, 0.03, 0.30)), "Chest"))                   # crimson runnel
    for sx, bone in ((0.28, "UpperArm_R"), (-0.28, "UpperArm_L")):
        z = rad(40 if sx > 0 else -40)
        p.append((sphere("Dark", 0.135, (sx, 0, 1.38), scale=(1.1, 1.1, 0.85)), bone))          # pauldron
        p.append((torus("Gold", 0.125, 0.016, (sx, 0, 1.335), rotation=(rad(25 if sx > 0 else -25), 0, 0)), bone))
        p.append((cone("Dark", 0.05, 0.004, 0.20, (sx * 1.02, -0.03, 1.50), rotation=(rad(-30), 0, 0), vertices=6), bone))  # back spike
        p.append((cone("Dark", 0.035, 0.004, 0.13, (sx * 1.18, 0.05, 1.41), rotation=(rad(18), 0, z), vertices=6), bone))   # side spike
    export_armor(p, "DreadChest")

def dread_legs():
    # Dark tassets + angular thigh plates with a knee spike each.
    p = []
    p.append((box("Dark", (0, 0.10, 0.80), (0.35, 0.16, 0.15)), "Hips"))                        # waist guard
    p.append((box("Gold", (0, 0.185, 0.80), (0.31, 0.045, 0.03)), "Hips"))                      # belt trim
    p.append((sphere("GemV", 0.035, (0, 0.21, 0.80), segments=4, rings=2), "Hips"))             # belt gem
    for sx, bone in ((0.12, "UpperLeg_R"), (-0.12, "UpperLeg_L")):
        p.append((box("Dark", (sx, 0.10, 0.55), (0.17, 0.16, 0.42)), bone))                     # thigh plate
        p.append((box("Gold", (sx, 0.195, 0.55), (0.14, 0.02, 0.36)), bone))                    # plate trim
        p.append((cone("Dark", 0.045, 0.004, 0.11, (sx, 0.22, 0.40), rotation=(rad(-95), 0, 0), vertices=6), bone))  # knee spike
    export_armor(p, "DreadLegs")

def dread_shield():
    # GRIP-space off-hand shield: dark hexagonal face, gold rim/spine, gem boss,
    # top+bottom spikes.
    p = []
    p.append(cyl("Dark", 0.27, 0.05, (0, 0, 0), rotation=(rad(90), 0, 0), vertices=6))          # hex face
    p.append(torus("Gold", 0.27, 0.022, (0, 0, 0), rotation=(rad(90), 0, 0)))                   # gold rim
    p.append(box("Gold", (0, -0.03, 0), (0.05, 0.05, 0.50)))                                    # vertical spine
    p.append(sphere("GemV", 0.075, (0, -0.06, 0), segments=4, rings=3))                         # gem boss
    p.append(cone("Dark", 0.05, 0.005, 0.20, (0, -0.03, 0.32), vertices=6))                     # top spike (+Z)
    p.append(cone("Dark", 0.05, 0.005, 0.20, (0, -0.03, -0.32), rotation=(rad(180), 0, 0), vertices=6))  # bottom spike
    export(p, "DreadShield")

def dread_gauntlets():
    # Obsidian gauntlets: armoured fist, gold-ringed bracer, knuckle spike.
    p = []
    for sx, bone in ((0.30, "Hand_R"), (-0.30, "Hand_L")):
        p.append((box("Dark", (sx, 0, 0.74), (0.12, 0.12, 0.13)), bone))                        # fist
        p.append((cyl("Dark", 0.078, 0.22, (sx, 0, 0.92)), bone))                               # forearm bracer
        p.append((torus("Gold", 0.084, 0.013, (sx, 0, 1.01)), bone))                            # rim top
        p.append((torus("Gold", 0.084, 0.013, (sx, 0, 0.83)), bone))                            # rim bottom
        p.append((cone("Dark", 0.024, 0.003, 0.08, (sx, 0.08, 0.74), rotation=(rad(-90), 0, 0), vertices=5), bone))  # knuckle spike
    export_armor(p, "DreadGauntlets")

def dread_boots():
    # Obsidian sabatons with gold trim and a back spur.
    p = []
    for sx, bone in ((0.12, "Foot_R"), (-0.12, "Foot_L")):
        p.append((box("Dark", (sx, 0.05, 0.11), (0.16, 0.27, 0.22)), bone))                     # shin/ankle
        p.append((box("Dark", (sx, 0.12, 0.035), (0.16, 0.36, 0.10)), bone))                    # foot
        p.append((box("Gold", (sx, 0.05, 0.20), (0.16, 0.05, 0.035)), bone))                    # shin trim
        p.append((cone("Dark", 0.026, 0.003, 0.09, (sx, -0.10, 0.15), rotation=(rad(90), 0, 0), vertices=5), bone))  # back spur
    export_armor(p, "DreadBoots")


# ── HOODED ASSASSIN (charcoal + crimson wraps, cyan gem) — sleek/minimal ──
def assassin_hood():
    # Deep pointed hood shadowing the face, glowing cyan eye-slit, crimson mask.
    p = []
    p.append((sphere("Dark", 0.175, (0, -0.03, 1.60), scale=(1.0, 1.16, 1.08)), "Head"))        # hood
    p.append((cone("Dark", 0.145, 0.02, 0.22, (0, 0.15, 1.66), rotation=(rad(-44), 0, 0), vertices=6), "Head"))  # forward peak
    p.append((box("Dark", (0, 0.185, 1.58), (0.22, 0.07, 0.14)), "Head"))                       # brow shadow
    p.append((box("Gem", (0, 0.225, 1.585), (0.15, 0.022, 0.022)), "Head"))                     # cyan eye-slit
    p.append((box("Crimson", (0, 0.185, 1.47), (0.19, 0.06, 0.11)), "Head"))                    # face wrap/mask
    export_armor(p, "AssassinHood")

def assassin_garb():
    # Fitted charcoal wrap, a crimson sash + strap, cyan clasp, minimal shoulders.
    p = []
    p.append((cone("Dark", 0.215, 0.185, 0.50, (0, 0.03, 1.14), vertices=8), "Chest"))          # torso wrap
    p.append((box("Crimson", (0, 0.195, 1.08), (0.09, 0.025, 0.42), rotation=(0, rad(20), 0)), "Chest"))  # diagonal sash
    p.append((box("Dark", (0, 0.20, 1.12), (0.05, 0.02, 0.40), rotation=(0, rad(-16), 0)), "Chest"))      # counter strap
    p.append((sphere("Gem", 0.028, (0.075, 0.215, 1.18), segments=4, rings=2), "Chest"))        # clasp gem
    p.append((cyl("Crimson", 0.20, 0.05, (0, 0.03, 0.92)), "Chest"))                            # waist wrap
    for sx, bone in ((0.27, "UpperArm_R"), (-0.27, "UpperArm_L")):
        p.append((sphere("Dark", 0.092, (sx, 0, 1.38), scale=(1, 1, 0.7)), bone))               # small shoulder
        p.append((cyl("Crimson", 0.078, 0.045, (sx, 0, 1.30)), bone))                           # crimson band
    export_armor(p, "AssassinGarb")

def assassin_legs():
    # Wrapped charcoal leggings with crimson binding bands (per-leg).
    p = []
    p.append((box("Dark", (0, 0.07, 0.80), (0.31, 0.13, 0.13)), "Hips"))                        # hip wrap
    p.append((box("Crimson", (0, 0.14, 0.80), (0.27, 0.03, 0.035)), "Hips"))                    # crimson belt line
    for sx, bone in ((0.12, "UpperLeg_R"), (-0.12, "UpperLeg_L")):
        p.append((cyl("Dark", 0.088, 0.62, (sx, 0.02, 0.46)), bone))                            # legging
        p.append((cyl("Crimson", 0.092, 0.035, (sx, 0.02, 0.24)), bone))                        # binding band
        p.append((cyl("Crimson", 0.092, 0.035, (sx, 0.02, 0.56)), bone))                        # binding band
    export_armor(p, "AssassinLegs")

def assassin_gauntlets():
    # Wrapped forearms with crimson bands.
    p = []
    for sx, bone in ((0.30, "Hand_R"), (-0.30, "Hand_L")):
        p.append((box("Dark", (sx, 0, 0.74), (0.10, 0.10, 0.11)), bone))                        # wrapped fist
        p.append((cyl("Dark", 0.072, 0.20, (sx, 0, 0.91)), bone))                               # forearm wrap
        p.append((cyl("Crimson", 0.075, 0.03, (sx, 0, 0.99)), bone))                            # band
        p.append((cyl("Crimson", 0.075, 0.03, (sx, 0, 0.85)), bone))                            # band
    export_armor(p, "AssassinGauntlets")

def assassin_boots():
    # Wrapped charcoal boots with a crimson ankle wrap.
    p = []
    for sx, bone in ((0.12, "Foot_R"), (-0.12, "Foot_L")):
        p.append((box("Dark", (sx, 0.05, 0.10), (0.14, 0.25, 0.20)), bone))                     # shin
        p.append((box("Dark", (sx, 0.11, 0.03), (0.14, 0.33, 0.09)), bone))                     # foot
        p.append((cyl("Crimson", 0.082, 0.035, (sx, 0.02, 0.18)), bone))                        # crimson ankle wrap
    export_armor(p, "AssassinBoots")


# ── MATERIALS (centered props for world pickups) ──────────────
def ore_chunk():
    p = []
    p.append(sphere("Main", 0.16, (0, 0, 0.14), scale=(1.1, 0.9, 0.8), segments=6, rings=5))
    p.append(box("Accent", (0.08, 0.05, 0.20), (0.06, 0.06, 0.06), rotation=(0, 0, rad(20))))
    p.append(box("Accent", (-0.06, -0.04, 0.16), (0.05, 0.05, 0.05)))
    export(p, "OreChunk")

def ingot():
    p = []
    p.append(cone("Main", 0.20, 0.12, 0.12, (0, 0, 0.06), vertices=4, rotation=(0, 0, rad(45))))
    export(p, "Ingot")

def herb():
    p = []
    p.append(cyl("Accent", 0.012, 0.20, (0, 0, 0.10)))
    for a in (0, 120, 240):
        p.append(cone("Main", 0.05, 0.005, 0.16,
                      (0.05 * math.cos(rad(a)), 0.05 * math.sin(rad(a)), 0.20),
                      rotation=(rad(35), 0, rad(a)), vertices=5))
    export(p, "Herb")

def fish():
    p = []
    p.append(sphere("Main", 0.10, (0, 0, 0.10), scale=(2.2, 0.9, 0.8)))
    p.append(cone("Main", 0.09, 0.005, 0.12, (0.24, 0, 0.10), rotation=(0, rad(90), 0), vertices=4))
    export(p, "Fish")


if __name__ == "__main__":
    os.makedirs(OUT_DIR, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for fn in (sword, sword2h, dagger, mace, bow, crossbow, staff, wand, shield,
               helm, body_armor, legs_armor, boots, gloves, cape, amulet,
               ranger_hood, ranger_vest, mage_hat, mage_robe_top, mage_robe_bottom,
               dread_helm, dread_chest, dread_legs, dread_shield, dread_gauntlets, dread_boots,
               assassin_hood, assassin_garb, assassin_legs, assassin_gauntlets, assassin_boots,
               ore_chunk, ingot, herb, fish):
        fn()
    print("[Equip] Done - all equipment/item models exported.")
