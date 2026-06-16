"""Headless Blender: import a GLB human, render front+side+back to catch missing/half heads.
Run: blender --background --python blender_render_glb.py -- <input.glb> <out_prefix>"""
import bpy, sys, math, mathutils

argv = sys.argv[sys.argv.index("--") + 1:]
glb, out_prefix = argv[0], argv[1]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=glb)

meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
if not meshes:
    print("NO_MESH"); sys.exit(0)

# combined bounds (world space)
mins = mathutils.Vector(( 1e9,  1e9,  1e9))
maxs = mathutils.Vector((-1e9, -1e9, -1e9))
for o in meshes:
    for c in o.bound_box:
        w = o.matrix_world @ mathutils.Vector(c)
        for i in range(3):
            mins[i] = min(mins[i], w[i]); maxs[i] = max(maxs[i], w[i])
center = (mins + maxs) * 0.5
size = maxs - mins
height = max(size.z, size.y, 0.1)
# head target = near the top (upper 12%)
top = mathutils.Vector((center.x, center.y, maxs[2] - height * 0.12))

# world: GLB usually Y-up imported as Z-up by Blender; use bounding box extents generically
radius = max(size.x, size.y, size.z) * 1.6 + 0.5

# lighting: bright sun so we SEE the head clearly
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", 'SUN'))
sun.data.energy = 4.0
bpy.context.scene.collection.objects.link(sun)
sun.rotation_euler = (math.radians(55), 0, math.radians(30))
bpy.context.scene.world = bpy.data.worlds.new("W")
bpy.context.scene.world.use_nodes = True
bpy.context.scene.world.node_tree.nodes["Background"].inputs[0].default_value = (0.5, 0.55, 0.6, 1)
bpy.context.scene.world.node_tree.nodes["Background"].inputs[1].default_value = 1.2

scene = bpy.context.scene
# Cycles on CPU — reliable headless (no GPU/display needed); low samples for speed.
scene.render.engine = 'CYCLES'
try:
    scene.cycles.device = 'CPU'
    scene.cycles.samples = 24
except Exception:
    pass
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.film_transparent = False

cam_data = bpy.data.cameras.new("Cam"); cam = bpy.data.objects.new("Cam", cam_data)
scene.collection.objects.link(cam); scene.camera = cam

def look_at(obj, target):
    d = (obj.location - target)
    obj.rotation_euler = d.to_track_quat('Z', 'Y').to_euler()

# frame the WHOLE figure but aim a bit high so the head is centred-ish
aim = mathutils.Vector((center.x, center.y, center.z + height * 0.25))
views = {
    "front": mathutils.Vector(( 0,        -radius, center.z + height*0.15)),
    "side":  mathutils.Vector(( radius,    0,      center.z + height*0.15)),
    "back":  mathutils.Vector(( 0,         radius, center.z + height*0.15)),
}
for name, pos in views.items():
    cam.location = center + (pos - center)
    cam.location = pos
    look_at(cam, aim)
    scene.render.filepath = f"{out_prefix}_{name}.png"
    bpy.ops.render.render(write_still=True)
    print(f"RENDERED {scene.render.filepath}")
print("DONE")
