using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Sprint 4 Day 2 / fix #5: Compose hero props into Scene_Botanika.
    ///
    /// Strategy: instantiate 15 Botanika props in a 5×3 grid at Y=0.85
    /// (table height) so they're visible in any cardinal-direction probe
    /// shot. Day 1 placed lights in Vec3 hard-coded positions outside
    /// scene geometry — this composer brings the props *into* the visible
    /// volume first; hero-shot focal alignment is a Day 3 task.
    ///
    /// Tripo replacements: 3d-artist may drop server_rack_retro_tripo.glb,
    /// espresso_old_tripo.glb, paayalnik_tripo.glb into Vendor/Tripo/Botanika/
    /// during this sprint. Composer try-loads Tripo first, falls back to
    /// Sketchfab if Tripo path doesn't exist. Either path works graceful.
    ///
    /// Per-prop Point Light: warm 2800K, intensity 0.6, range 2m. This
    /// replaces hard-coded Vec3 hero lights from Day 1 lighting director.
    ///
    /// Idempotent: removes existing AH_HeroProps_Botanika root before
    /// recomposition.
    /// </summary>
    public static class BotanikaHeroComposer
    {
        private const string BotanikaScenePath =
            "Assets/_Project/Scenes/Scene_Botanika.unity";
        private const string RootName = "AH_HeroProps_Botanika";

        private const float Spacing = 1.6f;       // grid cell width
        private const float Y = 0.85f;             // table-top height
        private const int Cols = 5;
        private const int Rows = 3;

        private static readonly Color WarmPoint =
            new Color(1.0f, 0.78f, 0.5f, 1f); // ~2800K

        private struct PropEntry
        {
            public string label;
            public string[] candidates; // try in order

            public PropEntry(string label, params string[] candidates)
            {
                this.label = label;
                this.candidates = candidates;
            }
        }

        private static readonly PropEntry[] Props = BuildProps();

        private static PropEntry[] BuildProps()
        {
            // Sprint 4 Day 2 final pass: every prop tries _clean.glb first
            // (Blender CLI re-export, removed KHR_materials_pbrSpecularGlossiness),
            // then falls back to original *.glb in same vendor dir, then to
            // alternative vendors. PolyHaven path is reserved for future
            // drops (currently empty but probed for forward-compat).
            string[][] mapping = new[]
            {
                // 3 critical props with Tripo variants — Tripo _clean first.
                new[] { "server_rack_retro",
                    "Tripo/Botanika/server_rack_retro_tripo_clean.glb",
                    "Tripo/Botanika/server_rack_retro_tripo.glb",
                    "Sketchfab/Botanika/server_rack_retro_clean.glb",
                    "Sketchfab/Botanika/server_rack_retro.glb" },
                new[] { "espresso_old",
                    "Tripo/Botanika/espresso_old_tripo_clean.glb",
                    "Tripo/Botanika/espresso_old_tripo.glb",
                    "Sketchfab/Botanika/espresso_old_clean.glb",
                    "Sketchfab/Botanika/espresso_old.glb" },
                new[] { "paayalnik",
                    "Tripo/Botanika/paayalnik_tripo_clean.glb",
                    "Tripo/Botanika/paayalnik_tripo.glb",
                    "Sketchfab/Botanika/paayalnik_clean.glb",
                    "Sketchfab/Botanika/paayalnik.glb" },

                // 4 PBR PolyHaven/Sketchfab props (real geometry).
                new[] { "monstera_pot",
                    "Sketchfab/Botanika/monstera_pot_clean.glb",
                    "PolyHaven/Models/monstera_pot_clean.glb",
                    "Sketchfab/Botanika/monstera_pot.glb",
                    "PolyHaven/Models/monstera_pot.glb" },
                new[] { "books_stack_3",
                    "Sketchfab/Botanika/books_stack_3_clean.glb",
                    "PolyHaven/Models/books_stack_3_clean.glb",
                    "Sketchfab/Botanika/books_stack_3.glb",
                    "PolyHaven/Models/books_stack_3.glb" },
                new[] { "edison_lamp",
                    "Sketchfab/Botanika/edison_lamp_clean.glb",
                    "PolyHaven/Models/edison_lamp_clean.glb",
                    "Sketchfab/Botanika/edison_lamp.glb",
                    "PolyHaven/Models/edison_lamp.glb" },
                new[] { "cast_iron_pan",
                    "Sketchfab/Botanika/cast_iron_pan_clean.glb",
                    "PolyHaven/Models/cast_iron_pan_clean.glb",
                    "Sketchfab/Botanika/cast_iron_pan.glb",
                    "PolyHaven/Models/cast_iron_pan.glb" },

                // 8 Kenney/PolyHaven placeholders.
                new[] { "turka_glass",
                    "Sketchfab/Botanika/turka_glass_clean.glb",
                    "Sketchfab/Botanika/turka_glass.glb" },
                new[] { "laptop_open",
                    "Sketchfab/Botanika/laptop_open_clean.glb",
                    "Sketchfab/Botanika/laptop_open.glb" },
                new[] { "whisky_bottle",
                    "Sketchfab/Botanika/whisky_bottle_clean.glb",
                    "Sketchfab/Botanika/whisky_bottle.glb" },
                new[] { "foil_hat",
                    "Sketchfab/Botanika/foil_hat_clean.glb",
                    "Sketchfab/Botanika/foil_hat.glb" },
                new[] { "notebook_open",
                    "Sketchfab/Botanika/notebook_open_clean.glb",
                    "Sketchfab/Botanika/notebook_open.glb" },
                new[] { "cables_rolled",
                    "Sketchfab/Botanika/cables_rolled_clean.glb",
                    "Sketchfab/Botanika/cables_rolled.glb" },
                new[] { "poster_kirill",
                    "Sketchfab/Botanika/poster_kirill_clean.glb",
                    "Sketchfab/Botanika/poster_kirill.glb" },
                new[] { "water_carafe",
                    "Sketchfab/Botanika/water_carafe_clean.glb",
                    "Sketchfab/Botanika/water_carafe.glb" },
            };

            var result = new PropEntry[mapping.Length];
            for (int i = 0; i < mapping.Length; i++)
            {
                string label = mapping[i][0];
                int n = mapping[i].Length - 1;
                string[] paths = new string[n];
                for (int j = 0; j < n; j++)
                {
                    paths[j] = "Assets/_Project/Vendor/" + mapping[i][j + 1];
                }
                // Sprint 4 final: prepend .fbx variants (Blender re-export
                // converted .glb -> .fbx to bypass gltfast importer bug).
                // FBX uses native Unity ModelImporter which works in batchmode.
                var promoted = new System.Collections.Generic.List<string>();
                foreach (var p in paths)
                {
                    var fbx = p.Replace("_tripo_clean.glb", "_tripo.fbx")
                                .Replace("_clean.glb", ".fbx")
                                .Replace(".glb", ".fbx");
                    if (!promoted.Contains(fbx)) promoted.Add(fbx);
                }
                foreach (var p in paths)
                {
                    if (!promoted.Contains(p)) promoted.Add(p);
                }
                result[i] = new PropEntry(label, promoted.ToArray());
            }
            return result;
        }

        [MenuItem("Afterhumans/Sprint4/Compose Hero Props")]
        public static void ComposeMenu()
        {
            Compose();
        }

        public static void Compose()
        {
            // Force refresh in case 3d-artist dropped Tripo GLBs after the
            // last batch-mode run. AssetDatabase needs to know about them
            // before LoadAssetAtPath can resolve.
            AssetDatabase.Refresh();
            Debug.Log($"[HeroComposer] DEBUG candidates[0] for '{Props[0].label}': " +
                      string.Join(" | ", Props[0].candidates));

            // Day 2 fix: 3d-artist dropped GLBs alongside .meta files
            // generated by another tool. Those .meta entries say
            // "DefaultImporter" — Unity treats the .glb as a binary blob
            // instead of importing it through ModelImporter. Force-reimport
            // each candidate so AfterhumansGLBImporter (ModelImporter hook)
            // picks them up and produces a GameObject.
            EnsureModelImported();

            var active = EditorSceneManager.GetActiveScene();
            if (!active.IsValid() || active.path != BotanikaScenePath)
            {
                if (active.IsValid() && active.isDirty)
                {
                    EditorSceneManager.SaveOpenScenes();
                }
                EditorSceneManager.OpenScene(
                    BotanikaScenePath, OpenSceneMode.Single);
            }

            var scene = EditorSceneManager.GetActiveScene();

            // Idempotent: blow away previous root.
            var existing = GameObject.Find(RootName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            var root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);

            // Sprint 4 final: manual placement aligned to Sprint1_Greybox
            // furniture coords (see BotanikaBuilder.cs PosSofa/PosCoffeeTable/
            // PosKitchen/PosTableNikolai/PosServerRack/etc). Each prop
            // belongs to a specific NPC zone per CHARACTERS.md / STORY.md
            // (Sasha/Mila/Kirill/Nikolai/Stas + Kafka).
            //   X: ±6 walls; Z: ±5 walls; Y: floor=0, table tops ~0.55-0.85m.
            var placement = new System.Collections.Generic.Dictionary<string, (Vector3 pos, Vector3 rot, float scale)>
            {
                // Kirill kitchen zone (X=4.5, Z=-2.5, table top Y≈0.85)
                { "espresso_old",   (new Vector3( 4.4f, 0.85f, -2.2f), new Vector3(0,  -25, 0), 0.6f) },
                { "turka_glass",    (new Vector3( 3.7f, 0.85f, -2.0f), new Vector3(0,   45, 0), 0.7f) },
                { "cast_iron_pan",  (new Vector3( 4.7f, 0.85f, -3.0f), new Vector3(0,    0, 0), 1.0f) },

                // Sasha sofa+coffee-table zone (sofa Z=3.8, coffee Z=2.5)
                { "edison_lamp",    (new Vector3(-0.7f, 0.55f,  2.5f), new Vector3(0,   15, 0), 0.5f) },
                { "notebook_open",  (new Vector3( 0.4f, 0.55f,  2.6f), new Vector3(0,  -20, 0), 1.0f) },
                { "laptop_open",    (new Vector3(-0.2f, 0.55f,  2.4f), new Vector3(0,   10, 0), 0.7f) },
                { "whisky_bottle",  (new Vector3( 0.8f, 0.55f,  2.7f), new Vector3(0,    0, 0), 1.0f) },
                { "water_carafe",   (new Vector3( 1.2f, 0.55f,  2.4f), new Vector3(0,    0, 0), 1.0f) },

                // Mila chair+desk zone (-2.8..-4.0 X, Z=1.5)
                { "books_stack_3",  (new Vector3(-4.0f, 0.75f,  1.5f), new Vector3(0,    0, 0), 0.8f) },

                // Nikolai NW corner desk (-4.5, 0, 4.2) — серверный жрец
                { "paayalnik",      (new Vector3(-4.4f, 0.85f,  4.0f), new Vector3(0,  -45, 0), 0.7f) },

                // Server rack zone (5.2, 0, -3.5) — холодный угол у двери
                { "server_rack_retro",(new Vector3(5.0f, 0.0f, -3.7f), new Vector3(0,  180, 0), 0.9f) },
                { "cables_rolled",  (new Vector3( 4.6f, 0.0f, -3.8f), new Vector3(0,    0, 0), 0.6f) },

                // Stas paranoid corner у двери (1.5, 0, -4)
                { "foil_hat",       (new Vector3( 2.1f, 0.1f, -4.0f), new Vector3(0,    0, 0), 0.5f) },

                // Wall poster — на западной стене напротив дивана
                { "poster_kirill",  (new Vector3(-5.85f, 1.6f, 1.0f), new Vector3(0,   90, 0), 1.5f) },

                // Plants — растения по углам Botanika oasis
                { "monstera_pot",   (new Vector3(-5.3f, 0.0f, -3.5f), new Vector3(0,    0, 0), 1.2f) },
            };

            int placed = 0;
            int tripoLoaded = 0;
            int sketchfabFallback = 0;

            for (int i = 0; i < Props.Length; i++)
            {
                var entry = Props[i];

                if (!placement.TryGetValue(entry.label, out var place))
                {
                    Debug.LogWarning(
                        $"[HeroComposer] No placement for '{entry.label}', skipping.");
                    continue;
                }
                var pos = place.pos;

                GameObject loaded = LoadFirstAvailable(entry, out string usedPath);
                GameObject go;
                if (loaded == null)
                {
                    // Day 2 final pass: 3d-artist re-exported GLBs via Blender
                    // CLI (removed KHR_materials_pbrSpecularGlossiness); if a
                    // _clean.glb still fails to resolve to a GameObject,
                    // gltfast import is the suspect. Cube placeholder keeps
                    // the pipeline running end-to-end.
                    Debug.LogWarning(
                        $"[HeroComposer] WARN: {entry.label} fell back to " +
                        $"cube placeholder (no candidate produced GameObject).");
                    go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.transform.SetParent(root.transform);
                    go.transform.localScale = new Vector3(
                        0.6f, 0.6f, 0.6f);
                    // Tint placeholder with warm-cream so it's not magenta.
                    var rend = go.GetComponent<MeshRenderer>();
                    if (rend != null)
                    {
                        var lit = Shader.Find("Universal Render Pipeline/Lit");
                        if (lit != null)
                        {
                            var mat = new Material(lit);
                            mat.SetColor("_BaseColor",
                                new Color(0.85f, 0.72f, 0.54f, 1f));
                            mat.SetFloat("_Smoothness", 0.3f);
                            rend.sharedMaterial = mat;
                        }
                    }
                    usedPath = "<placeholder>";
                }
                else
                {
                    if (usedPath.Contains("/Tripo/"))
                    {
                        tripoLoaded++;
                    }
                    else if (usedPath.Contains("/Sketchfab/"))
                    {
                        sketchfabFallback++;
                    }

                    Debug.Log($"[HeroComposer] {entry.label} -> {usedPath}");

                    go = (GameObject)PrefabUtility.InstantiatePrefab(
                        loaded, root.transform);
                    if (go == null)
                    {
                        go = Object.Instantiate(loaded, root.transform);
                    }
                }

                go.name = entry.label;
                go.transform.position = pos;
                go.transform.rotation = Quaternion.Euler(place.rot);
                go.transform.localScale = Vector3.one * place.scale;

                // Add point light hovering 0.5m above prop.
                var lightGo = new GameObject($"PL_{entry.label}");
                lightGo.transform.SetParent(root.transform, false);
                lightGo.transform.position = pos + new Vector3(0f, 0.5f, 0f);
                var pl = lightGo.AddComponent<Light>();
                pl.type = LightType.Point;
                pl.color = WarmPoint;
                pl.intensity = 0.6f;
                pl.range = 2.0f;
                pl.shadows = LightShadows.Soft;
                pl.shadowStrength = 0.4f;
                pl.shadowResolution = LightShadowResolution.Low;
                pl.lightmapBakeType = LightmapBakeType.Mixed;

                placed++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log(
                $"[HeroComposer] Placed {placed}/{Props.Length} props in " +
                $"{Cols}x{Rows} grid @ Y={Y}. Tripo loaded: {tripoLoaded}, " +
                $"Sketchfab fallback: {sketchfabFallback}.");
        }

        private static void EnsureModelImported()
        {
            // .glb requires com.unity.cloud.gltfast package which registers
            // GltfImporter. If LoadAssetAtPath<GameObject> returns null,
            // either the package isn't loaded yet or the .meta says
            // DefaultImporter (created by 3d-artist before package install).
            // Fix: delete .meta and force-reimport — AssetDatabase will pick
            // GltfImporter for .glb extension and produce a GameObject.
            int reimported = 0;
            foreach (var entry in Props)
            {
                foreach (var path in entry.candidates)
                {
                    if (!File.Exists(path)) continue;

                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (asset != null)
                    {
                        continue; // already importable as GameObject — done.
                    }
                    // Also check sub-assets (GltfImporter sometimes nests).
                    var allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                    bool hasGameObject = false;
                    if (allAssets != null)
                    {
                        foreach (var a in allAssets)
                        {
                            if (a is GameObject) { hasGameObject = true; break; }
                        }
                    }
                    if (hasGameObject) continue;

                    string metaPath = path + ".meta";
                    Debug.Log(
                        $"[HeroComposer] No GameObject at '{path}'; " +
                        $"deleting .meta + force reimport.");
                    if (File.Exists(metaPath))
                    {
                        File.Delete(metaPath);
                    }
                    AssetDatabase.ImportAsset(
                        path,
                        ImportAssetOptions.ForceUpdate |
                        ImportAssetOptions.ForceSynchronousImport);
                    reimported++;
                }
            }
            if (reimported > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                Debug.Log(
                    $"[HeroComposer] Force-reimported {reimported} GLB files.");
            }
        }

        private static GameObject LoadFirstAvailable(
            PropEntry entry, out string usedPath)
        {
            foreach (var path in entry.candidates)
            {
                bool fileExists = File.Exists(path);
                if (!fileExists) continue;

                // GltfImporter (com.unity.cloud.gltfast) puts the prefab as
                // the main asset OR as a sub-asset depending on Unity
                // version. Try both: LoadAssetAtPath<GameObject>, then
                // LoadAllAssetsAtPath → first GameObject.
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset != null)
                {
                    usedPath = path;
                    return asset;
                }

                var allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                if (allAssets != null)
                {
                    foreach (var a in allAssets)
                    {
                        if (a is GameObject go)
                        {
                            Debug.Log(
                                $"[HeroComposer] Resolved GameObject as " +
                                $"sub-asset for '{path}' (name: {go.name})");
                            usedPath = path;
                            return go;
                        }
                    }
                }

                Debug.LogWarning(
                    $"[HeroComposer] No GameObject (main or sub) for " +
                    $"'{path}'. Sub-asset count: " +
                    $"{(allAssets == null ? 0 : allAssets.Length)}");
            }
            usedPath = null;
            return null;
        }
    }
}
