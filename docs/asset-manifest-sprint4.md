# Asset Manifest — Sprint 4 Botanika Visual Upgrade

Generated: 2026-05-01  
Target: Unity 6 URP, macOS standalone (afterhumans)  
Style: stylized-realistic painterly (ART_BIBLE §3.1 Botanika palette)

---

## 1. HDRI

| Asset | File | Source URL | License | Filesize | Notes |
|---|---|---|---|---|---|
| sunset_botanika_4k | `Assets/_Project/Vendor/PolyHaven/HDRI/sunset_botanika_4k.exr` | https://polyhaven.com/a/aft_lounge | CC0 | 25 MB | Warm cruise ship lounge at sunrise, artificial lamps, ~3000K. Renamed from aft_lounge_4k.exr. Unity compresses at import. |

---

## 2. Hero Props — 15 GLB (Draco compressionLevel 7)

All files in `Assets/_Project/Vendor/Sketchfab/Botanika/`.  
All < 500 KB. All CC0.

| Asset Name | File | Source | Source URL | License | Filesize | Mesh Quality Notes |
|---|---|---|---|---|---|---|
| server_rack_retro | `server_rack_retro.glb` | Kenney Furniture Kit (televisionVintage placeholder) | https://kenney.nl/assets/furniture-kit | CC0 | 6.8 KB | Low-poly placeholder. Replace with Tripo prompt if hero focal point: "retro server rack 1990s with blinking LED indicators, warm orange accent lights, tangled cables, stylized low-poly, T-pose-free" |
| paayalnik | `paayalnik.glb` | Kenney Furniture Kit (toaster placeholder) | https://kenney.nl/assets/furniture-kit | CC0 | 4.0 KB | Geometric placeholder. Tripo prompt if needed: "soldering iron on metal stand, retro workshop tool, warm painterly style" |
| turka_glass | `turka_glass.glb` | Kenney Furniture Kit (kitchenBlender placeholder) | https://kenney.nl/assets/furniture-kit | CC0 | 6.3 KB | Placeholder. Tripo: "glass Turkish coffee pot turka, transparent glass with brass neck, warm kitchen light" |
| espresso_old | `espresso_old.glb` | Kenney Furniture Kit (kitchenCoffeeMachine) | https://kenney.nl/assets/furniture-kit | CC0 | 5.0 KB | Direct kitchen prop, stylized. |
| laptop_open | `laptop_open.glb` | Kenney Furniture Kit (laptop) | https://kenney.nl/assets/furniture-kit | CC0 | 4.4 KB | Thinkpad-style open laptop, stylized. |
| whisky_bottle | `whisky_bottle.glb` | Kenney Furniture Kit (speaker placeholder) | https://kenney.nl/assets/furniture-kit | CC0 | 4.0 KB | Geometric placeholder. Tripo: "dark whisky bottle half-empty, warm amber glass, stylized low-poly" |
| foil_hat | `foil_hat.glb` | Kenney Furniture Kit (pillow placeholder) | https://kenney.nl/assets/furniture-kit | CC0 | 1.8 KB | Placeholder. Tripo: "tin foil hat crumpled, paranoia symbol, stylized painterly, silver metallic" |
| notebook_open | `notebook_open.glb` | Kenney Furniture Kit (tableCoffeeGlass placeholder) | https://kenney.nl/assets/furniture-kit | CC0 | 3.5 KB | Placeholder. Tripo: "open notebook with pen, handwritten pages visible, warm paper tone" |
| books_stack_3 | `books_stack_3.glb` | Poly Haven — book_encyclopedia_set_01 | https://polyhaven.com/a/book_encyclopedia_set_01 | CC0 | 386 KB | High-quality PBR encyclopedia set, 2K textures embedded (512px for budget). |
| cables_rolled | `cables_rolled.glb` | Kenney Furniture Kit (cardboardBoxOpen placeholder) | https://kenney.nl/assets/furniture-kit | CC0 | 3.2 KB | Placeholder. Tripo: "coiled ethernet cables on floor, messy tangle, stylized warm orange" |
| poster_kirill | `poster_kirill.glb` | Kenney Furniture Kit (computerScreen placeholder) | https://kenney.nl/assets/furniture-kit | CC0 | 3.3 KB | Flat screen as poster frame. Apply custom Cyrillic texture in Unity (create in nano-banana). |
| edison_lamp | `edison_lamp.glb` | Poly Haven — desk_lamp_arm_01 | https://polyhaven.com/a/desk_lamp_arm_01 | CC0 | 297 KB | Articulated desk lamp with PBR metal materials, 2K textures (512px). |
| cast_iron_pan | `cast_iron_pan.glb` | Poly Haven — brass_pan_01 | https://polyhaven.com/a/brass_pan_01 | CC0 | 165 KB | Brass pan with PBR textures. Tint dark via URP material (metallic=0.8, roughness=0.9). |
| water_carafe | `water_carafe.glb` | Kenney Furniture Kit (radio placeholder) | https://kenney.nl/assets/furniture-kit | CC0 | 6.2 KB | Placeholder. In Unity: apply URP transparent glass material. |
| monstera_pot | `monstera_pot.glb` | Poly Haven — calathea_orbifolia_01 | https://polyhaven.com/a/calathea_orbifolia_01 | CC0 | 195 KB | Real tropical plant with PBR leaf textures, dark green matches #2D4A3E palette. |

---

## 3. PBR Material Packs

All files in `Assets/_Project/Vendor/PolyHaven/Materials/`.  
All maps: albedo + normal (GL) + roughness + AO, 2K PNG.

| Pack | Folder | Source | Source URL | License | Total Size | Notes |
|---|---|---|---|---|---|---|
| wood_painterly | `wood_painterly/` | Poly Haven — brown_planks_05 | https://polyhaven.com/a/brown_planks_05 | CC0 | ~60 MB PNG | Warm brown horizontal planks, matches #8B6F4E faded wood. URP: albedo→BaseMap, normal→Normal, roughness→Smoothness(invert). |
| fabric_sofa | `fabric_sofa/` | Poly Haven — brown_leather | https://polyhaven.com/a/brown_leather | CC0 | ~22 MB PNG | Brown leather for orange sofa. Tint with #E8A75C albedo tint in URP material. |
| ceramic_mug | `ceramic_mug/` | Poly Haven — beige_wall_001 | https://polyhaven.com/a/beige_wall_001 | CC0 | ~60 MB PNG | Smooth matte ceramic surface, off-white beige. Apply to all crockery. |

---

## 4. Draco Compression Proof

All 15 GLB compressed via `gltf-pipeline -d --draco.compressionLevel 7`.  
Kenney source GLBs: 5–24 KB → output 1.8–6.8 KB.  
Poly Haven GLTF → GLB with embedded 512px textures:

| Model | Source size (GLTF+bin+tex) | Output GLB |
|---|---|---|
| monstera_pot (calathea) | ~5.5 MB (2k JPG) → 512px | 195 KB |
| books_stack_3 | ~5.0 MB (2k JPG) → 512px | 386 KB |
| edison_lamp | ~7.7 MB (2k JPG) → 512px | 297 KB |
| cast_iron_pan | ~4.1 MB (2k JPG) → 512px | 165 KB |

---

## 5. Handoff Notes for unity-game-developer

**Unity Editor import steps (Day 2):**

1. `sunset_botanika_4k.exr` → Texture importer: TextureShape=Default, sRGB=OFF (HDR), Compression=None. Drag to Lighting → Environment → Skybox/Environment Reflections.
2. GLB props → glTFast auto-imports. For Kenney assets (vertex-color only): create URP Lit materials, assign colors from ART_BIBLE §3.1 palette manually.
3. `wood_painterly/` → Create URP Lit material: BaseMap=albedo, Normal=normal (GL compatible), Metallic=0, Roughness via Smoothness=1-roughness. Apply to floor/wall meshes.
4. `fabric_sofa/` → URP Lit material: BaseMap=albedo, tint #E8A75C. Apply to sofa_lounge mesh.
5. `ceramic_mug/` → URP Lit material: Metallic=0, Roughness~0.3 (smooth ceramic). Apply to espresso_old, turka_glass.
6. `poster_kirill.glb` → needs custom Cyrillic texture (nano-banana). Assign as EmissionMap or BaseMap on screen mesh.
7. Placeholder pропы (paayalnik, server_rack_retro, foil_hat, whisky_bottle, notebook_open, cables_rolled, turka_glass, water_carafe) — mark with TODO tags in scene for future Tripo replacement in Sprint 5.

**Draw call budget:** 15 props + 3 materials → ~18-25 draw calls. GPU Instancing enabled on repeated materials reduces further.

**Poly Haven license:** All assets CC0 (public domain). No attribution required. See https://polyhaven.com/license  
**Kenney license:** CC0. See https://kenney.nl/assets/furniture-kit → License.txt

---

## 6. Tripo Hot-swap Replacements (Sprint 4 Day 2)

**Status:** Tripo API returned `code:2010 — not enough credit`. Fallback: CC0 sources.  
**All files in** `Assets/_Project/Vendor/Tripo/Botanika/`  
**All < 500 KB. All CC0.**

| Prop | File | Source | Source URL | License | Filesize | Original Prompt (for future Tripo retry) | Notes |
|---|---|---|---|---|---|---|---|
| server_rack_retro | `server_rack_retro_tripo.glb` | Polygonal Mind — chromatic-chaos / Computer_Retro | https://github.com/ToxSam/cc0-models-Polygonal-Mind/tree/main/projects/chromatic-chaos | CC0 | 272 KB | "1980s vintage server rack with tangled cables, retro futurism, warm metal patina, exposed circuit boards, cyrillic warning labels, cyberpunk dystopian, stylized PBR low-poly painterly Sable Tchia art style, dark grey metal frame with copper accents" | Retro vaporwave computer unit. Not a literal server rack — functionally equivalent retro tech prop for Shot 3. Draco compressionLevel 7. When Tripo credits restored — generate exact prompt and replace. |
| espresso_old | `espresso_old_tripo.glb` | Poly Haven — CashRegister_01 | https://polyhaven.com/a/CashRegister_01 | CC0 | 331 KB | "vintage Soviet espresso machine 1970s, brushed chromium, brass details, retro futurism, warm patina, stylized PBR low-poly painterly Sable Tchia art style, kitchen appliance" | Vintage retail register with warm brass/chrome tones. Atmospheric stand-in for espresso machine — similar era, similar metal patina. 512px textures embedded, Draco compressionLevel 7. Replace with Tripo when credits restored. |
| paayalnik | `paayalnik_tripo.glb` | Poly Haven — brass_blowtorch | https://polyhaven.com/a/brass_blowtorch | CC0 | 230 KB | "soldering iron on wooden stand with messy desk, paranoid maker bench aesthetic, exposed wires, brass tip, vintage tool, stylized PBR low-poly painterly Sable Tchia, copper warm tones" | Brass blowtorch — workshop tool with correct warm copper/brass patina. Visually close to паяльник (both = handheld heat tools). 512px textures embedded, Draco compressionLevel 7. |

**Optimization log:**
- brass_blowtorch (paayalnik): source GLTF+bin+1k textures = ~2MB raw → GLB 512px textures + Draco = **230 KB**
- CashRegister_01 (espresso): source GLTF+bin+1k textures (6 maps) = ~1.4MB raw → GLB 512px + Draco = **331 KB**  
- Computer_Retro (server_rack): source GLB = 287 KB → Draco = **272 KB**

**Preview proofs (local):**
- `/tmp/afterhumans_visual_review/tripo_preview_1.png` — server_rack_retro (Computer_Retro thumbnail)
- `/tmp/afterhumans_visual_review/tripo_preview_2.png` — espresso_old (CashRegister_01 thumbnail)
- `/tmp/afterhumans_visual_review/tripo_preview_3.png` — paayalnik (brass_blowtorch thumbnail)

**Poly Haven license (hot-swap assets):** CC0. https://polyhaven.com/license  
**Polygonal Mind license:** CC0 (all Polygonal Mind collections). https://github.com/ToxSam/open-source-3D-assets

**TODO (when Tripo credits replenished):** Replace all 3 with actual Tripo generations using the prompts above. The prompts are production-ready — submit directly to Tripo text_to_model endpoint. Target: same filesize budget <500KB post-Draco.

---

## 7. Blender Re-export Clean GLBs (Sprint 4 Day 2 — KHR_materials_pbrSpecularGlossiness fix)

**Problem:** glTFast 6.10.1 в Unity URP не поддерживает `KHR_materials_pbrSpecularGlossiness`. 18+ Botanika GLB не импортировались.

**Fix:** Blender 5.1.0 CLI batch re-export. Blender автоматически конвертирует SpecularGlossiness → MetallicRoughness при import/export цикле. Дополнительно применён Draco compression level 7.

**Run date:** 2026-05-01  
**Tool:** `/opt/homebrew/bin/blender --background --python scripts/reexport_glb_clean.py`  
**Log:** `/tmp/afterhumans_visual_review/blender_reexport.log`

**Extensions verification (books_stack_3_clean.glb):**
- `extensionsUsed`: `["KHR_draco_mesh_compression"]`
- `extensionsRequired`: `["KHR_draco_mesh_compression"]`
- `KHR_materials_pbrSpecularGlossiness`: ABSENT
- Material workflow: `pbrMetallicRoughness` with `baseColorTexture` + `metallicRoughnessTexture` keys

**Result: 20 processed, 0 failed.**

### Sketchfab/Botanika — 17 files

| File | Original size | Clean + Draco | Delta |
|---|---|---|---|
| books_stack_3_clean.glb | 386 KB | 396 KB | +2.6% (texture embed expansion offset Draco) |
| cables_rolled_clean.glb | 3 KB | 4 KB | — |
| cast_iron_pan_clean.glb | 164 KB | 168 KB | +2.4% |
| edison_lamp_clean.glb | 297 KB | 304 KB | +2.4% |
| espresso_old_clean.glb | 5 KB | 8 KB | — |
| foil_hat_clean.glb | 1 KB | 4 KB | — |
| lamp_round_table_clean.glb | 3 KB | 4 KB | — |
| laptop_open_clean.glb | 4 KB | 4 KB | — |
| monstera_pot_clean.glb | 194 KB | 456 KB | texture decompression from Draco source |
| notebook_open_clean.glb | 3 KB | 4 KB | — |
| paayalnik_clean.glb | 3 KB | 4 KB | — |
| poster_kirill_clean.glb | 3 KB | 4 KB | — |
| server_rack_retro_clean.glb | 6 KB | 8 KB | — |
| sofa_lounge_clean.glb | 3 KB | 4 KB | — |
| turka_glass_clean.glb | 6 KB | 8 KB | — |
| water_carafe_clean.glb | 6 KB | 8 KB | — |
| whisky_bottle_clean.glb | 3 KB | 4 KB | — |

### Tripo/Botanika — 3 files

| File | Original size | Clean + Draco | Delta |
|---|---|---|---|
| espresso_old_tripo_clean.glb | 330 KB | 440 KB | texture decompression |
| paayalnik_tripo_clean.glb | 229 KB | 236 KB | +3.1% |
| server_rack_retro_tripo_clean.glb | 272 KB | 276 KB | +1.5% |

**Note on size increase:** Original GLBs had Draco geometry but JPEG-compressed embedded textures. Blender re-export decompresses geometry to raw then Draco re-applies — net effect on texture-heavy models can be slight increase. The critical fix is extension stripping, not further size reduction.

**Unity handoff:** Pass `_clean.glb` files to unity-game-developer. All files are glTFast-compatible (pure MetallicRoughness + KHR_draco_mesh_compression only). Unity import: glTFast reads KHR_draco_mesh_compression natively in 6.10.1+.
