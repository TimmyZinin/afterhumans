"""Blender headless: GLB -> FBX with textures, + dump albedo PNGs as fallback.
Run on Contabo host: /opt/blender/blender --background --python reexport_textured.py
In:  /opt/blender-work/in/*.glb
Out: /opt/blender-work/out/<name>.fbx  (+ embedded/copied textures)
     /opt/blender-work/out/tex/<name>__<image>.png  (dumped albedos for composer)
     /opt/blender-work/out/manifest.txt  (name -> material -> base color image)
"""
import bpy, glob, os, json

IN = "/opt/blender-work/in"
OUT = "/opt/blender-work/out"
TEX = os.path.join(OUT, "tex")
os.makedirs(OUT, exist_ok=True)
os.makedirs(TEX, exist_ok=True)

manifest = {}

for src in sorted(glob.glob(os.path.join(IN, "*.glb"))):
    name = os.path.splitext(os.path.basename(src))[0]
    bpy.ops.wm.read_factory_settings(use_empty=True)
    try:
        bpy.ops.import_scene.gltf(filepath=src)
    except Exception as e:
        print(f"IMPORT_FAIL {name}: {e}")
        continue

    # Map each material -> its Base Color image (follow Principled BSDF input).
    mat_to_img = {}
    for mat in bpy.data.materials:
        if not mat.use_nodes:
            continue
        bsdf = next((n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
        if not bsdf:
            continue
        base_in = bsdf.inputs.get('Base Color')
        if base_in and base_in.is_linked:
            src_node = base_in.links[0].from_node
            if src_node.type == 'TEX_IMAGE' and src_node.image:
                mat_to_img[mat.name] = src_node.image.name

    # Dump every image to PNG (so the Unity composer can load albedos directly).
    saved = []
    for img in bpy.data.images:
        if img.size[0] == 0:
            continue
        safe = "".join(c if c.isalnum() or c in "._-" else "_" for c in img.name)
        p = os.path.join(TEX, f"{name}__{safe}.png")
        try:
            img.filepath_raw = p
            img.file_format = 'PNG'
            img.save()
            saved.append((img.name, os.path.basename(p)))
            print(f"TEX {name}: {img.name} -> {os.path.basename(p)}")
        except Exception as e:
            print(f"TEX_FAIL {name} {img.name}: {e}")

    # Export FBX with textures embedded (Unity ModelImporter can extract).
    dst = os.path.join(OUT, f"{name}.fbx")
    try:
        bpy.ops.export_scene.fbx(
            filepath=dst, use_selection=False, apply_unit_scale=True,
            bake_space_transform=True, path_mode='COPY', embed_textures=True,
            object_types={'MESH', 'EMPTY'}, use_mesh_modifiers=True,
            axis_forward='-Z', axis_up='Y')
        print(f"FBX_OK {name} -> {dst}")
    except Exception as e:
        print(f"FBX_FAIL {name}: {e}")

    manifest[name] = {
        "materials": mat_to_img,
        "images": dict(saved),
    }

with open(os.path.join(OUT, "manifest.json"), "w") as f:
    json.dump(manifest, f, indent=2, ensure_ascii=False)
print("REEXPORT_DONE", len(manifest), "assets")
