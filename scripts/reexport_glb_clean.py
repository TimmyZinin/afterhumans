import bpy
import sys
import os
import glob

input_dirs = [
    os.path.expanduser("~/afterhumans/Assets/_Project/Vendor/Sketchfab/Botanika"),
    os.path.expanduser("~/afterhumans/Assets/_Project/Vendor/PolyHaven/Models"),
    os.path.expanduser("~/afterhumans/Assets/_Project/Vendor/Tripo/Botanika"),
]

processed = []
failed = []

for d in input_dirs:
    if not os.path.isdir(d):
        print(f"[SKIP] Dir not found: {d}")
        continue

    glb_files = sorted(glob.glob(os.path.join(d, "*.glb")))
    glb_files = [f for f in glb_files if not f.endswith("_clean.glb")]

    if not glb_files:
        print(f"[SKIP] No source GLBs in: {d}")
        continue

    for src in glb_files:
        name = os.path.basename(src)
        dst = src.replace(".glb", "_clean.glb")
        print(f"\n[START] {name}")

        try:
            # Reset scene to factory empty state
            bpy.ops.wm.read_factory_settings(use_empty=True)

            # Import GLB
            bpy.ops.import_scene.gltf(filepath=src)

            # Check something was imported
            if len(bpy.context.scene.objects) == 0:
                raise RuntimeError("No objects imported from GLB")

            # Export as clean GLB — Blender auto-converts SpecGloss → MetalRough
            bpy.ops.export_scene.gltf(
                filepath=dst,
                export_format='GLB',
                export_extras=False,
                export_apply=True,
                export_yup=True,
                export_materials='EXPORT',
                export_image_format='AUTO',
                export_attributes=False,
            )

            src_size = os.path.getsize(src)
            dst_size = os.path.getsize(dst)
            ratio = (1 - dst_size / src_size) * 100 if src_size > 0 else 0
            print(f"[OK] {name} → {os.path.basename(dst)} | {src_size//1024}KB → {dst_size//1024}KB ({ratio:+.1f}%)")
            processed.append((name, src_size, dst_size, dst))

        except Exception as e:
            print(f"[FAIL] {name}: {e}")
            failed.append((name, str(e)))
            # Continue with next file — do not abort
            continue

print("\n" + "="*60)
print(f"DONE: {len(processed)} processed, {len(failed)} failed")
print("="*60)

if processed:
    print("\nProcessed files:")
    for name, src_size, dst_size, dst in processed:
        print(f"  {name}: {src_size//1024}KB → {dst_size//1024}KB")

if failed:
    print("\nFailed files:")
    for name, err in failed:
        print(f"  {name}: {err}")
