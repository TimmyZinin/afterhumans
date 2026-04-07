"""
Universal Blender processing script for Tripo3D models.
Decimate, scale, fix origin, rename animations, export FBX.

Usage:
  /Applications/Blender.app/Contents/MacOS/Blender --background \
    --python scripts/process_model.py -- \
    --input path/to/raw.fbx \
    --output path/to/processed.fbx \
    --height 0.9 \
    --max-tris 15000 \
    --animated true \
    --rotation-fix 90
"""
import bpy
import os
import sys
import mathutils
import argparse


def get_world_bounds(obj):
    coords = [obj.matrix_world @ v.co for v in obj.data.vertices]
    xs = [c.x for c in coords]
    ys = [c.y for c in coords]
    zs = [c.z for c in coords]
    return (
        mathutils.Vector((min(xs), min(ys), min(zs))),
        mathutils.Vector((max(xs), max(ys), max(zs)))
    )


def parse_args():
    # Blender passes everything after "--" to the script
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser(description="Process 3D model for Unity")
    parser.add_argument("--input", required=True, help="Input FBX/GLB path")
    parser.add_argument("--output", required=True, help="Output FBX path")
    parser.add_argument("--height", type=float, default=1.0, help="Target height in meters")
    parser.add_argument("--max-tris", type=int, default=15000, help="Max triangles")
    parser.add_argument("--animated", default="false", help="Keep armature+animations (true/false)")
    parser.add_argument("--origin", default="bottom", choices=["bottom", "center"], help="Origin placement")
    parser.add_argument("--rotation-fix", type=float, default=90, help="Y-rotation offset degrees (Tripo default: 90)")
    return parser.parse_args(argv)


def main():
    args = parse_args()
    is_animated = args.animated.lower() == "true"

    print(f"\n{'='*50}")
    print(f"  3D MODEL PROCESSING")
    print(f"{'='*50}")
    print(f"  Input:    {args.input}")
    print(f"  Output:   {args.output}")
    print(f"  Height:   {args.height}m")
    print(f"  Max tris: {args.max_tris}")
    print(f"  Animated: {is_animated}")
    print(f"  Origin:   {args.origin}")
    print(f"  Rot fix:  {args.rotation_fix} deg")
    print(f"{'='*50}\n")

    # Step 0: Clean scene
    bpy.ops.wm.read_factory_settings(use_empty=True)

    # Step 1: Import
    input_path = os.path.abspath(args.input)
    ext = input_path.split(".")[-1].lower()
    print(f"[1/10] Importing {ext.upper()}...")
    if ext == "fbx":
        bpy.ops.import_scene.fbx(filepath=input_path, use_anim=is_animated, ignore_leaf_bones=False)
    elif ext in ("glb", "gltf"):
        bpy.ops.import_scene.gltf(filepath=input_path)
    else:
        print(f"ERROR: Unsupported format: {ext}")
        sys.exit(1)

    mesh_obj = None
    armature_obj = None
    for obj in bpy.data.objects:
        if obj.type == "MESH" and mesh_obj is None:
            mesh_obj = obj
        elif obj.type == "ARMATURE" and armature_obj is None:
            armature_obj = obj

    if mesh_obj is None:
        print("ERROR: No mesh found in file!")
        sys.exit(1)

    orig_tris = sum(len(p.vertices) - 2 for p in mesh_obj.data.polygons)
    bb_min, bb_max = get_world_bounds(mesh_obj)
    print(f"  Mesh: {mesh_obj.name} ({len(mesh_obj.data.vertices):,} verts, {orig_tris:,} tris)")
    print(f"  Height: {bb_max.z - bb_min.z:.3f}m")

    # Step 2: Apply transforms
    print("[2/10] Applying transforms...")
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

    # Step 3: Decimate
    current_tris = sum(len(p.vertices) - 2 for p in mesh_obj.data.polygons)
    if current_tris > args.max_tris:
        ratio = args.max_tris / current_tris
        print(f"[3/10] Decimating {current_tris:,} → ~{args.max_tris:,} tris (ratio {ratio:.4f})...")
        bpy.ops.object.select_all(action='DESELECT')
        bpy.context.view_layer.objects.active = mesh_obj
        mesh_obj.select_set(True)

        mod = mesh_obj.modifiers.new(name="Decimate", type='DECIMATE')
        mod.decimate_type = 'COLLAPSE'
        mod.ratio = ratio
        mod.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=mod.name)

        new_tris = sum(len(p.vertices) - 2 for p in mesh_obj.data.polygons)
        print(f"  Result: {len(mesh_obj.data.vertices):,} verts, {new_tris:,} tris")
    else:
        print(f"[3/10] Skipping decimate ({current_tris:,} tris ≤ {args.max_tris:,})")

    # Step 4: Scale to target height
    bb_min, bb_max = get_world_bounds(mesh_obj)
    current_height = bb_max.z - bb_min.z
    print(f"[4/10] Scaling from {current_height:.3f}m to {args.height}m...")

    if current_height > 0.001:
        scale_factor = args.height / current_height
        root_obj = armature_obj if armature_obj else mesh_obj
        bpy.ops.object.select_all(action='DESELECT')
        bpy.context.view_layer.objects.active = root_obj
        root_obj.select_set(True)
        root_obj.scale *= scale_factor
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    # Step 5: Move origin to bottom or center
    bb_min, bb_max = get_world_bounds(mesh_obj)
    print(f"[5/10] Setting origin to {args.origin}...")

    root_obj = armature_obj if armature_obj else mesh_obj
    if args.origin == "bottom":
        root_obj.location.z -= bb_min.z  # feet at Z=0
    else:
        center_z = (bb_min.z + bb_max.z) / 2
        root_obj.location.z -= center_z  # center at Z=0

    bpy.ops.object.select_all(action='DESELECT')
    bpy.context.view_layer.objects.active = root_obj
    root_obj.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)

    # Step 6: Set mesh origin to world origin
    print("[6/10] Setting mesh origin...")
    bpy.ops.object.select_all(action='DESELECT')
    bpy.context.view_layer.objects.active = mesh_obj
    mesh_obj.select_set(True)
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')

    # Step 7: Rotation fix (Tripo models face wrong direction)
    if abs(args.rotation_fix) > 0.1:
        print(f"[7/10] Applying rotation fix: {args.rotation_fix} deg Y...")
        # Apply rotation to armature/root so mesh children inherit it
        root_obj = armature_obj if armature_obj else mesh_obj
        import math
        root_obj.rotation_euler.z += math.radians(args.rotation_fix)  # Blender Z = Unity Y
        bpy.ops.object.select_all(action='DESELECT')
        bpy.context.view_layer.objects.active = root_obj
        root_obj.select_set(True)
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    else:
        print("[7/10] No rotation fix needed")

    # Step 8: Rename animation clips
    print("[8/10] Renaming animation clips...")
    presets = ["Walk", "Run", "Idle", "Jump"]
    for i, action in enumerate(bpy.data.actions):
        old = action.name
        new_name = presets[i] if i < len(presets) else f"Clip_{i}"
        action.name = new_name
        print(f"  {old} → {new_name}")

    # Step 9: Export FBX
    output_path = os.path.abspath(args.output)
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    print(f"[9/10] Exporting to {output_path}...")
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(
        filepath=output_path,
        use_selection=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        use_mesh_modifiers=True,
        mesh_smooth_type='FACE',
        add_leaf_bones=False,
        bake_anim=is_animated,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=is_animated,
        bake_anim_simplify_factor=0.1,
        path_mode='COPY',
        embed_textures=True,
    )

    # Step 10: Final report
    bb_min, bb_max = get_world_bounds(mesh_obj)
    dims = bb_max - bb_min
    final_tris = sum(len(p.vertices) - 2 for p in mesh_obj.data.polygons)
    output_size = os.path.getsize(output_path) / 1024 / 1024

    print(f"\n{'='*50}")
    print(f"  PROCESSING COMPLETE")
    print(f"{'='*50}")
    print(f"  Output:     {output_path}")
    print(f"  Size:       {output_size:.1f} MB")
    print(f"  Vertices:   {len(mesh_obj.data.vertices):,}")
    print(f"  Triangles:  {final_tris:,} (from {orig_tris:,})")
    print(f"  Height:     {dims.z:.3f}m (target: {args.height}m)")
    print(f"  Dimensions: {dims.x:.3f} x {dims.y:.3f} x {dims.z:.3f}m")
    print(f"  Bones:      {len(armature_obj.data.bones) if armature_obj else 0}")
    print(f"  Animations: {[a.name for a in bpy.data.actions]}")
    print(f"{'='*50}")

    # Assertions
    assert final_tris <= args.max_tris * 1.1, f"Tris {final_tris} > max {args.max_tris}"
    height_tolerance = args.height * 0.15
    assert abs(dims.z - args.height) <= height_tolerance, f"Height {dims.z:.3f} != target {args.height}"

    print(f"\n  OK — ready for verify_model.py\n")


if __name__ == "__main__":
    main()
