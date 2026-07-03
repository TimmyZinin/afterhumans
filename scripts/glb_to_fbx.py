import bpy, glob, os
input_dirs = [
    os.path.expanduser("~/afterhumans/Assets/_Project/Vendor/Sketchfab/Botanika"),
    os.path.expanduser("~/afterhumans/Assets/_Project/Vendor/PolyHaven/Models"),
    os.path.expanduser("~/afterhumans/Assets/_Project/Vendor/Tripo/Botanika"),
]
processed = 0
failed = []
for d in input_dirs:
    if not os.path.isdir(d): continue
    for src in glob.glob(os.path.join(d, "*_clean.glb")):
        base = os.path.basename(src).replace("_clean.glb", "")
        dst = os.path.join(d, base + ".fbx")
        try:
            bpy.ops.wm.read_factory_settings(use_empty=True)
            bpy.ops.import_scene.gltf(filepath=src)
            bpy.ops.export_scene.fbx(
                filepath=dst,
                use_selection=False,
                apply_unit_scale=True,
                bake_space_transform=True,
                object_types={'MESH', 'EMPTY'},
                use_mesh_modifiers=True,
                mesh_smooth_type='OFF',
                path_mode='COPY',
                embed_textures=True,
                axis_forward='-Z',
                axis_up='Y',
            )
            processed += 1
            print(f"[OK] {base}")
        except Exception as e:
            failed.append((base, str(e)[:100]))
            print(f"[FAIL] {base}: {e}")
print(f"DONE: {processed} ok, {len(failed)} failed")
for f in failed: print(f"  - {f[0]}: {f[1]}")
