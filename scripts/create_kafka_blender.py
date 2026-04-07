"""
Kafka — Welsh Corgi Cardigan (v2)
Breed standard: AKC, height 30cm, length:height 1.8:1
Color: black + white ONLY (no tan per CHARACTERS.md)
Target: ~1000 triangles, stylized-realistic, game-ready

Run: blender -b --python scripts/create_kafka_blender.py
"""

import bpy
import bmesh
import math
import os

def clear_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete()
    for m in list(bpy.data.materials):
        bpy.data.materials.remove(m)

def make_mat(name, color, roughness=0.5, specular=0.3):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*color, 1.0)
        bsdf.inputs["Roughness"].default_value = roughness
        bsdf.inputs["Specular IOR Level"].default_value = specular
    return mat

def add_part(name, prim, loc, scale, rot=(0,0,0), mat=None, subdiv=1):
    """Create a smooth primitive part"""
    if prim == 'sphere':
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=6, radius=1, location=loc)
    elif prim == 'cube':
        bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    elif prim == 'cylinder':
        bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=1, depth=1, location=loc)
    elif prim == 'cone':
        bpy.ops.mesh.primitive_cone_add(vertices=8, radius1=1, radius2=0.1, depth=1, location=loc)

    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.rotation_euler = tuple(math.radians(r) for r in rot)

    # Apply transforms
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

    # Subdivision for smoothness (keep low for game-ready)
    if subdiv > 0:
        mod = obj.modifiers.new("Subdiv", 'SUBSURF')
        mod.levels = subdiv
        mod.render_levels = subdiv
        bpy.ops.object.modifier_apply(modifier="Subdiv")

    bpy.ops.object.shade_smooth()

    if mat:
        obj.data.materials.clear()
        obj.data.materials.append(mat)

    return obj

def create_kafka():
    clear_scene()

    # Materials
    mat_black = make_mat("Black", (0.015, 0.015, 0.02), roughness=0.7, specular=0.2)
    mat_white = make_mat("White", (0.93, 0.91, 0.87), roughness=0.6, specular=0.3)
    mat_nose = make_mat("Nose", (0.005, 0.005, 0.005), roughness=0.15, specular=0.8)  # wet
    mat_eye = make_mat("Eye", (0.04, 0.025, 0.015), roughness=0.1, specular=0.9)

    # === BREED STANDARD DIMENSIONS ===
    # Height at withers: 0.30m, Body length: 0.55m (1.8:1 ratio)
    # All coordinates in meters, centered at origin

    H = 0.30   # height at withers
    L = 0.55   # body length
    chest_depth = 0.14  # deep chest

    parts = []

    # === BODY (main torso — elongated, deep chest) ===
    body = add_part("Body", 'sphere', (0, 0, H * 0.5),
        (L * 0.48, 0.09, chest_depth * 0.7), mat=mat_black, subdiv=1)
    parts.append(body)

    # White belly/chest
    chest = add_part("Chest", 'sphere', (L * 0.15, 0, H * 0.35),
        (0.08, 0.07, 0.09), mat=mat_white, subdiv=1)
    parts.append(chest)

    belly = add_part("Belly", 'sphere', (0, 0, H * 0.30),
        (L * 0.30, 0.06, 0.06), mat=mat_white, subdiv=0)
    parts.append(belly)

    # === HEAD ===
    # Skull wider than muzzle, ratio 5:3
    head = add_part("Head", 'sphere', (L * 0.35, 0, H * 0.75),
        (0.065, 0.055, 0.06), mat=mat_black, subdiv=1)
    parts.append(head)

    # White muzzle (lower face)
    muzzle = add_part("Muzzle", 'sphere', (L * 0.42, 0, H * 0.68),
        (0.045, 0.035, 0.035), mat=mat_white, subdiv=1)
    parts.append(muzzle)

    # White blaze (stripe between eyes up to forehead)
    blaze = add_part("Blaze", 'sphere', (L * 0.37, 0, H * 0.82),
        (0.02, 0.015, 0.025), mat=mat_white, subdiv=0)
    parts.append(blaze)

    # Nose (black, wet/shiny)
    nose = add_part("Nose", 'sphere', (L * 0.46, 0, H * 0.70),
        (0.012, 0.01, 0.01), mat=mat_nose, subdiv=0)
    parts.append(nose)

    # Eyes (dark brown, almond-shaped)
    for side in [-1, 1]:
        eye = add_part(f"Eye{'L' if side<0 else 'R'}", 'sphere',
            (L * 0.38, side * 0.035, H * 0.78),
            (0.01, 0.008, 0.009), mat=mat_eye, subdiv=0)
        parts.append(eye)

    # === EARS (BIG! — corgi signature) ===
    # Stiff, upright, rounded tips, wide set
    for side in [-1, 1]:
        ear = add_part(f"Ear{'L' if side<0 else 'R'}", 'cone',
            (L * 0.30, side * 0.045, H * 1.05),
            (0.022, 0.018, 0.05),
            rot=(0, side * 12, 0), mat=mat_black, subdiv=1)
        parts.append(ear)

    # === LEGS (4x — SHORT! defining feature) ===
    # Front legs slightly bowed outward
    leg_h = H * 0.35  # very short legs
    leg_r = 0.018

    front_x = L * 0.22
    back_x = -L * 0.22
    leg_spread = 0.055

    leg_positions = [
        (front_x, -leg_spread, leg_h * 0.5, "FL", -3),   # front left, slight bow
        (front_x,  leg_spread, leg_h * 0.5, "FR",  3),   # front right
        (back_x,  -leg_spread, leg_h * 0.5, "BL",  0),   # back left
        (back_x,   leg_spread, leg_h * 0.5, "BR",  0),   # back right
    ]

    for x, y, z, label, bow in leg_positions:
        leg = add_part(f"Leg_{label}", 'cylinder',
            (x, y, z), (leg_r, leg_r, leg_h * 0.5),
            rot=(0, bow, 0), mat=mat_black, subdiv=0)
        parts.append(leg)

        # White paws
        paw = add_part(f"Paw_{label}", 'sphere',
            (x, y, 0.008), (0.022, 0.02, 0.012),
            mat=mat_white, subdiv=0)
        parts.append(paw)

    # === TAIL (FULL — Cardigan, NOT Pembroke!) ===
    # Fox-like brush, low set, hangs down at rest
    tail = add_part("Tail", 'sphere',
        (-L * 0.35, 0, H * 0.45), (0.09, 0.025, 0.025),
        rot=(0, 0, -25), mat=mat_black, subdiv=1)
    parts.append(tail)

    # White tail tip
    tail_tip = add_part("TailTip", 'sphere',
        (-L * 0.42, 0, H * 0.38), (0.03, 0.018, 0.018),
        mat=mat_white, subdiv=0)
    parts.append(tail_tip)

    # === JOIN ALL INTO ONE MESH ===
    bpy.ops.object.select_all(action='DESELECT')
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.join()

    kafka = bpy.context.object
    kafka.name = "Kafka"

    # Final smooth
    bpy.ops.object.shade_smooth()

    # Set origin to bottom center (feet level)
    bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='BOUNDS')
    # Adjust so bottom is at z=0
    bbox = kafka.bound_box
    min_z = min(v[2] for v in bbox)
    kafka.location.z -= min_z

    # Apply all transforms
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

    verts = len(kafka.data.vertices)
    faces = len(kafka.data.polygons)
    # Estimate triangles (each quad = 2 tris)
    tris = sum(len(f.vertices) - 2 for f in kafka.data.polygons)

    print(f"[Kafka] Model: {verts} verts, {faces} faces, ~{tris} triangles")
    print(f"[Kafka] Dimensions: {kafka.dimensions.x:.3f} x {kafka.dimensions.y:.3f} x {kafka.dimensions.z:.3f} m")

    return kafka

def export_fbx(filepath):
    bpy.ops.export_scene.fbx(
        filepath=filepath,
        use_selection=True,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        object_types={'MESH'},
        use_mesh_modifiers=True,
        mesh_smooth_type='FACE',
        bake_space_transform=True,
        path_mode='COPY',
        embed_textures=False
    )
    print(f"[Kafka] FBX exported: {filepath}")

# === MAIN ===
kafka = create_kafka()

# Select for export
bpy.ops.object.select_all(action='DESELECT')
kafka.select_set(True)
bpy.context.view_layer.objects.active = kafka

# Export
output_dir = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
    "Assets", "_Project", "Models")
os.makedirs(output_dir, exist_ok=True)

export_fbx(os.path.join(output_dir, "kafka_corgi.fbx"))
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(output_dir, "kafka_corgi.blend"))

print("[Kafka] DONE — Welsh Corgi Cardigan v2")
