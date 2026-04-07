"""
Blender render script: generate verification images for HITL approval.
Renders 3 views (side, front, top) + rest pose + walk frames for animated models.

Usage:
  /Applications/Blender.app/Contents/MacOS/Blender --background \
    --python scripts/verify_model.py -- \
    --input path/to/model.fbx \
    --output-dir docs/verify/
"""
import bpy
import os
import sys
import math
import argparse


def parse_args():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser(description="Render model verification images")
    parser.add_argument("--input", required=True, help="Processed FBX path")
    parser.add_argument("--output-dir", default="docs/verify/", help="Output directory for PNGs")
    return parser.parse_args(argv)


def setup_scene(fbx_path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=os.path.abspath(fbx_path), use_anim=True)

    mesh = armature = None
    for obj in bpy.data.objects:
        if obj.type == "MESH" and mesh is None:
            mesh = obj
        elif obj.type == "ARMATURE" and armature is None:
            armature = obj

    # Lights
    bpy.ops.object.light_add(type='SUN', location=(1, -1, 2))
    bpy.context.active_object.data.energy = 3
    bpy.context.active_object.rotation_euler = (math.radians(30), math.radians(-20), 0)
    bpy.ops.object.light_add(type='AREA', location=(-0.5, 0.5, 0.3))
    bpy.context.active_object.data.energy = 20
    bpy.context.active_object.data.size = 1

    # Ground
    bpy.ops.mesh.primitive_plane_add(size=5, location=(0, 0, -0.001))
    mat = bpy.data.materials.new("Ground")
    mat.diffuse_color = (0.35, 0.35, 0.35, 1)
    bpy.context.active_object.data.materials.append(mat)

    # Render settings
    scene = bpy.context.scene
    scene.render.resolution_x = 1200
    scene.render.resolution_y = 800
    scene.render.engine = 'BLENDER_EEVEE'

    return mesh, armature


def render_view(name, cam_loc, cam_rot, output_dir, model_name):
    bpy.ops.object.camera_add(location=cam_loc, rotation=cam_rot)
    cam = bpy.context.active_object
    cam.data.lens = 35
    bpy.context.scene.camera = cam

    path = os.path.join(output_dir, f"{model_name}_{name}.png")
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)

    bpy.data.objects.remove(cam, do_unlink=True)
    print(f"  Rendered: {name}")
    return path


def main():
    args = parse_args()
    model_name = os.path.splitext(os.path.basename(args.input))[0]
    output_dir = os.path.abspath(args.output_dir)
    os.makedirs(output_dir, exist_ok=True)

    print(f"\n=== VERIFY: {model_name} ===\n")

    mesh, armature = setup_scene(args.input)

    # Get model bounds for camera positioning
    coords = [mesh.matrix_world @ v.co for v in mesh.data.vertices]
    max_dim = max(max(c[i] for c in coords) - min(c[i] for c in coords) for i in range(3))
    dist = max_dim * 2.5  # Camera distance based on model size

    renders = []

    # 3 standard views
    renders.append(render_view("side", (dist * 0.7, -dist * 0.7, max_dim * 0.7),
                               (math.radians(70), 0, math.radians(40)), output_dir, model_name))
    renders.append(render_view("front", (0, -dist, max_dim * 0.5),
                               (math.radians(80), 0, 0), output_dir, model_name))
    renders.append(render_view("top", (0, 0, dist),
                               (0, 0, 0), output_dir, model_name))

    # Rest pose (for animated models)
    has_animations = len(bpy.data.actions) > 0
    if has_animations and armature:
        bpy.context.view_layer.objects.active = armature
        armature.data.pose_position = 'REST'
        renders.append(render_view("rest_pose", (dist * 0.7, -dist * 0.7, max_dim * 0.7),
                                   (math.radians(70), 0, math.radians(40)), output_dir, model_name))
        armature.data.pose_position = 'POSE'

        # Walk animation frames
        if armature.animation_data is None:
            armature.animation_data_create()
        if bpy.data.actions:
            armature.animation_data.action = bpy.data.actions[0]

        # Add camera for walk frames
        bpy.ops.object.camera_add(
            location=(dist * 0.7, -dist * 0.7, max_dim * 0.7),
            rotation=(math.radians(70), 0, math.radians(40)))
        cam = bpy.context.active_object
        cam.data.lens = 35
        bpy.context.scene.camera = cam

        action = bpy.data.actions[0]
        frame_start = int(action.frame_range[0])
        frame_end = int(action.frame_range[1])
        frame_count = frame_end - frame_start
        sample_frames = [frame_start, frame_start + frame_count // 4,
                         frame_start + frame_count // 2, frame_start + 3 * frame_count // 4]

        for frame in sample_frames:
            bpy.context.scene.frame_set(frame)
            path = os.path.join(output_dir, f"{model_name}_walk_f{frame:02d}.png")
            bpy.context.scene.render.filepath = path
            bpy.ops.render.render(write_still=True)
            renders.append(path)
            print(f"  Rendered: walk frame {frame}")

        bpy.data.objects.remove(cam, do_unlink=True)

    # Stats
    tris = sum(len(p.vertices) - 2 for p in mesh.data.polygons)
    bb_min = [min(c[i] for c in coords) for i in range(3)]
    bb_max = [max(c[i] for c in coords) for i in range(3)]
    dims = [bb_max[i] - bb_min[i] for i in range(3)]

    print(f"\n{'='*40}")
    print(f"  {model_name}")
    print(f"{'='*40}")
    print(f"  Triangles: {tris:,}")
    print(f"  Height:    {dims[2]:.3f}m")
    print(f"  Width:     {dims[0]:.3f}m")
    print(f"  Depth:     {dims[1]:.3f}m")
    print(f"  Animated:  {has_animations}")
    if has_animations:
        print(f"  Clips:     {[a.name for a in bpy.data.actions]}")
    print(f"  Renders:   {len(renders)} files")
    print(f"  Output:    {output_dir}/")
    print(f"{'='*40}\n")

    # Print open command for convenience
    print(f"open {' '.join(renders[:5])}")


if __name__ == "__main__":
    main()
