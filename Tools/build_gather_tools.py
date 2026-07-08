# Void Bound - low-poly gather-tool models (axe / pickaxe / rod / sickle).
# Run headless:
#   & "C:\Program Files\Blender Foundation\Blender 5.1\blender.exe" -b -P Tools\build_gather_tools.py
# Exports one FBX per tool to Assets/Resources/GatherTools/ using the standard
# CODING_STANDARDS FBX settings. Modeled in GRIP-space with the SHAFT along
# Blender +Z (base at origin) so it imports with the handle along Unity +Y -
# matching the primitive layout GatherAnimator's grip pose was tuned for.
# Material names are the contract with GatherAnimator: "Wood" and "Metal".

import bpy, math, os

OUT_DIR = r"C:\Users\Jordon\Void Bound\Assets\Resources\GatherTools"

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

COLORS = {
    "Wood":  (0.40, 0.28, 0.16, 1.0),
    "Metal": (0.62, 0.64, 0.68, 1.0),
}

def rad(d): return math.radians(d)

def mat(name):
    m = bpy.data.materials.get(name)
    if m is None:
        m = bpy.data.materials.new(name)
        m.diffuse_color = COLORS[name]
    return m

def cube(mname, location=(0,0,0), scale=(1,1,1), rotation=(0,0,0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location, scale=scale, rotation=rotation)
    ob = bpy.context.active_object
    ob.data.materials.append(mat(mname))
    return ob

def cyl(mname, radius=0.1, depth=1.0, location=(0,0,0), rotation=(0,0,0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=10, radius=radius, depth=depth,
                                        location=location, rotation=rotation)
    ob = bpy.context.active_object
    ob.data.materials.append(mat(mname))
    return ob

def cone(mname, r1=0.1, r2=0.0, depth=1.0, location=(0,0,0), rotation=(0,0,0)):
    bpy.ops.mesh.primitive_cone_add(vertices=10, radius1=r1, radius2=r2, depth=depth,
                                    location=location, rotation=rotation)
    ob = bpy.context.active_object
    ob.data.materials.append(mat(mname))
    return ob

def export(parts, name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts:
        o.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    if len(parts) > 1:
        bpy.ops.object.join()
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    os.makedirs(OUT_DIR, exist_ok=True)
    path = os.path.join(OUT_DIR, name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, **FBX_SETTINGS)
    print(f"[GatherTools] Exported {path}")
    bpy.ops.wm.read_factory_settings(use_empty=True)

# ── tools (shaft along +Z, base at origin) ──────────────────────────────
def build_axe():
    p = []
    p.append(cyl("Wood", radius=0.022, depth=0.52, location=(0, 0, 0.26)))
    # head: a bevelled blade to one side of the top, wide cutting edge forward (+X)
    p.append(cube("Metal", location=(0.075, 0, 0.46), scale=(0.13, 0.03, 0.12)))
    p.append(cube("Metal", location=(0.15, 0, 0.46), scale=(0.06, 0.02, 0.16), rotation=(0, rad(20), 0)))
    return p, "axe"

def build_pickaxe():
    p = []
    p.append(cyl("Wood", radius=0.022, depth=0.52, location=(0, 0, 0.26)))
    # head: a horizontal bar across the top with two tapered points (±X)
    p.append(cube("Metal", location=(0, 0, 0.5), scale=(0.30, 0.035, 0.045)))
    p.append(cone("Metal", r1=0.035, r2=0.0, depth=0.14, location=(0.21, 0, 0.5), rotation=(0, rad(90), 0)))
    p.append(cone("Metal", r1=0.035, r2=0.0, depth=0.14, location=(-0.21, 0, 0.5), rotation=(0, rad(-90), 0)))
    return p, "pickaxe"

def build_rod():
    p = []
    # a long tapering pole + a small reel near the grip
    p.append(cone("Wood", r1=0.026, r2=0.006, depth=0.84, location=(0, 0, 0.42)))
    p.append(cyl("Metal", radius=0.03, depth=0.03, location=(0.03, 0, 0.1), rotation=(rad(90), 0, 0)))
    return p, "rod"

def build_sickle():
    p = []
    p.append(cyl("Wood", radius=0.022, depth=0.34, location=(0, 0, 0.17)))
    # curved blade approximated by three angled segments sweeping forward (+X)
    p.append(cube("Metal", location=(0.06, 0, 0.36), scale=(0.16, 0.02, 0.04), rotation=(0, rad(10), 0)))
    p.append(cube("Metal", location=(0.17, 0, 0.34), scale=(0.12, 0.02, 0.035), rotation=(0, rad(45), 0)))
    p.append(cube("Metal", location=(0.22, 0, 0.27), scale=(0.09, 0.02, 0.03), rotation=(0, rad(80), 0)))
    return p, "sickle"

def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for builder in (build_axe, build_pickaxe, build_rod, build_sickle):
        parts, name = builder()
        export(parts, name)
    print("[GatherTools] Done.")

main()
