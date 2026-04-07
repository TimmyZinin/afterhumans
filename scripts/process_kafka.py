"""
Blender script: process Kafka corgi model from Tripo3D.
Fix origin, decimate, scale, rename animation, export FBX.

Usage:
  /Applications/Blender.app/Contents/MacOS/Blender --background --python scripts/process_kafka.py
"""
import bpy
import bmesh
import os
import sys
import mathutils

# ── Config ──────────────────────────────────────────────────────────
PROJECT_DIR = os.path.expanduser("~/afterhumans")
INPUT_FBX = os.path.join(PROJECT_DIR, "Assets/_Project/Models/kafka_tripo/kafka_animated.fbx")
OUTPUT_FBX = os.path.join(PROJECT_DIR, "Assets/_Project/Models/kafka_corgi.fbx")

TARGET_HEIGHT_M = 0.30
DECIMATE_RATIO = 0.01
MIN_TRIS = 8000
MAX_TRIS = 25000

def get_world_bounds(obj):
    """Get world-space bounding box of a mesh object."""
    coords = [obj.matrix_world @ v.co for v in obj.data.vertices]
    xs = [c.x for c in coords]
    ys = [c.y for c in coords]
    zs = [c.z for c in coords]
    return (
        mathutils.Vector((min(xs), min(ys), min(zs))),
        mathutils.Vector((max(xs), max(ys), max(zs)))
    )

# ── Step 0: Clean scene ─────────────────────────────────────────────
print("\n=== KAFKA PROCESSING START ===\n")
bpy.ops.wm.read_factory_settings(use_empty=True)

# ── Step 1: Import FBX ──────────────────────────────────────────────
print(f"[1/9] Importing FBX...")
bpy.ops.import_scene.fbx(
    filepath=INPUT_FBX,
    use_anim=True,
    ignore_leaf_bones=False,
    automatic_bone_orientation=False,
)

mesh_obj = None
armature_obj = None
for obj in bpy.data.objects:
    if obj.type == "MESH":
        mesh_obj = obj
    elif obj.type == "ARMATURE":
        armature_obj = obj

if mesh_obj is None:
    print("ERROR: No mesh found!")
    sys.exit(1)

print(f"  Mesh: {mesh_obj.name} ({len(mesh_obj.data.vertices)} verts)")
print(f"  Armature: {armature_obj.name if armature_obj else 'NONE'}")
print(f"  Mesh parent: {mesh_obj.parent.name if mesh_obj.parent else 'NONE'}")

bb_min, bb_max = get_world_bounds(mesh_obj)
print(f"  World bounds: {[round(x,3) for x in bb_min]} → {[round(x,3) for x in bb_max]}")
print(f"  World height (Z): {bb_max.z - bb_min.z:.4f}m")

# ── Step 2: Apply all transforms first ───────────────────────────────
print("[2/9] Applying all transforms...")
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

bb_min, bb_max = get_world_bounds(mesh_obj)
print(f"  After apply — world height: {bb_max.z - bb_min.z:.4f}m, feet Z: {bb_min.z:.4f}")

# ── Step 3: Decimate ────────────────────────────────────────────────
original_tris = sum(len(p.vertices) - 2 for p in mesh_obj.data.polygons)
print(f"[3/9] Decimating {original_tris:,} tris (ratio {DECIMATE_RATIO})...")

bpy.ops.object.select_all(action='DESELECT')
bpy.context.view_layer.objects.active = mesh_obj
mesh_obj.select_set(True)

mod = mesh_obj.modifiers.new(name="Decimate", type='DECIMATE')
mod.decimate_type = 'COLLAPSE'
mod.ratio = DECIMATE_RATIO
mod.use_collapse_triangulate = True
bpy.ops.object.modifier_apply(modifier=mod.name)

new_tris = sum(len(p.vertices) - 2 for p in mesh_obj.data.polygons)
new_verts = len(mesh_obj.data.vertices)
print(f"  Result: {new_verts:,} verts, {new_tris:,} tris")

bb_min, bb_max = get_world_bounds(mesh_obj)
print(f"  Post-decimate height: {bb_max.z - bb_min.z:.4f}m")

# ── Step 4: Scale to target height ──────────────────────────────────
current_height = bb_max.z - bb_min.z
print(f"[4/9] Scaling from {current_height:.4f}m to {TARGET_HEIGHT_M}m...")

if current_height < 0.001:
    print("ERROR: height is zero!")
    sys.exit(1)

scale_factor = TARGET_HEIGHT_M / current_height
print(f"  Scale factor: {scale_factor:.4f}")

# Scale only the ROOT object (armature is parent of mesh)
# If we scale both, mesh gets double-scaled
root_obj = armature_obj if armature_obj else mesh_obj
bpy.ops.object.select_all(action='DESELECT')
bpy.context.view_layer.objects.active = root_obj
root_obj.select_set(True)
root_obj.scale *= scale_factor

# Apply scale on root
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

bb_min, bb_max = get_world_bounds(mesh_obj)
print(f"  After scale — height: {bb_max.z - bb_min.z:.4f}m")

# ── Step 5: Move feet to Z=0 ────────────────────────────────────────
print("[5/9] Moving feet to Z=0...")
feet_z = bb_min.z
print(f"  Feet currently at Z={feet_z:.4f}")

# Move root object up so feet touch Z=0
root_obj.location.z -= feet_z
bpy.ops.object.select_all(action='DESELECT')
bpy.context.view_layer.objects.active = root_obj
root_obj.select_set(True)
bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)

bb_min, bb_max = get_world_bounds(mesh_obj)
print(f"  After move — feet Z: {bb_min.z:.4f}, top Z: {bb_max.z:.4f}")
print(f"  Final height: {bb_max.z - bb_min.z:.4f}m")

# ── Step 6: Set mesh origin to feet ──────────────────────────────────
print("[6/9] Setting mesh origin to feet...")
bpy.ops.object.select_all(action='DESELECT')
bpy.context.view_layer.objects.active = mesh_obj
mesh_obj.select_set(True)

# Cursor to world origin (feet should be there now)
bpy.context.scene.cursor.location = (0, 0, 0)
bpy.ops.object.origin_set(type='ORIGIN_CURSOR')

# ── Step 7: Rename animation actions ─────────────────────────────────
print("[7/9] Renaming animation actions...")
for i, action in enumerate(bpy.data.actions):
    old_name = action.name
    if i == 0:
        action.name = "Walk"
    else:
        action.name = f"Walk_{i}"
    print(f"  {old_name} → {action.name}")

# ── Step 8: Print bone hierarchy ─────────────────────────────────────
print("[8/9] Bone hierarchy:")
if armature_obj:
    for bone in armature_obj.data.bones:
        parent = bone.parent.name if bone.parent else "ROOT"
        depth = 0
        b = bone
        while b.parent:
            depth += 1
            b = b.parent
        indent = "  " * depth
        print(f"  {indent}{bone.name} ← {parent}")

# ── Step 9: Export FBX ───────────────────────────────────────────────
print(f"[9/9] Exporting to {OUTPUT_FBX}...")
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(
    filepath=OUTPUT_FBX,
    use_selection=True,
    apply_scale_options='FBX_SCALE_ALL',
    axis_forward='-Z',
    axis_up='Y',
    use_mesh_modifiers=True,
    mesh_smooth_type='FACE',
    add_leaf_bones=False,
    bake_anim=True,
    bake_anim_use_all_bones=True,
    bake_anim_use_nla_strips=False,
    bake_anim_use_all_actions=True,
    bake_anim_simplify_factor=0.1,
    path_mode='COPY',
    embed_textures=True,
)

# ── Final report ─────────────────────────────────────────────────────
output_size = os.path.getsize(OUTPUT_FBX) / 1024 / 1024
bb_min, bb_max = get_world_bounds(mesh_obj)
dims = bb_max - bb_min
final_verts = len(mesh_obj.data.vertices)
final_tris = sum(len(p.vertices) - 2 for p in mesh_obj.data.polygons)

print("\n" + "=" * 50)
print("  KAFKA PROCESSING — FINAL REPORT")
print("=" * 50)
print(f"  Output:     {OUTPUT_FBX}")
print(f"  Size:       {output_size:.1f} MB")
print(f"  Vertices:   {final_verts:,}")
print(f"  Triangles:  {final_tris:,}")
print(f"  Height:     {dims.z:.3f} m (target: {TARGET_HEIGHT_M}m)")
print(f"  Length:     {dims.x:.3f} m")
print(f"  Width:      {dims.y:.3f} m")
print(f"  L:H ratio:  {dims.x/dims.z:.1f}:1 (corgi standard: 1.8:1)")
print(f"  Feet at:    Z={bb_min.z:.4f}")
print(f"  Top at:     Z={bb_max.z:.4f}")
print(f"  Bones:      {len(armature_obj.data.bones) if armature_obj else 0}")
print(f"  Animations: {[a.name for a in bpy.data.actions]}")
print("=" * 50)

# Assertions
assert final_tris >= MIN_TRIS, f"Too few tris: {final_tris} < {MIN_TRIS}"
assert final_tris <= MAX_TRIS, f"Too many tris: {final_tris} > {MAX_TRIS}"
assert 0.20 <= dims.z <= 0.40, f"Height out of range: {dims.z:.3f}m (expected 0.20-0.40)"
assert abs(bb_min.z) < 0.02, f"Feet not at ground: Z={bb_min.z:.4f}"

print("\n  ✅ ALL CHECKS PASSED")
print("  === KAFKA PROCESSING DONE ===\n")
