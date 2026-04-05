using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using Afterhumans.Art;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// BOT-A03: Environmental props layer for Scene_Botanika.
    /// Adds diegetic storytelling objects on top of base BotanikaDresser greenhouse:
    ///
    /// - Server rack in NW corner (tableCross + 2 blinking LEDs + 3 runtime point lights)
    ///   diegetic signal: «The Forecast runs on compute — even the oasis is tethered»
    /// - Graffiti «segfault == freedom» 3D TMP text on east wall (narrative Easter egg)
    /// - Николай's corner table + bottle cylinder (alcohol prop, STORY §3.2 data-жрец ломается)
    /// - Мила's desk + laptop (manifest writing station)
    /// - Кирилл's kitchen stove + coffee machine + turka cylinder (grounds him in ritual)
    /// - Стас's foil hat (capsule, silver metal, placed at expected Stas spawn —
    ///   BOT-N03 re-parents it to the NPC head once character prefab is spawned)
    /// - 6 additional scattered books on bookcase shelves
    ///
    /// Skill references:
    /// - `game-art`: environmental storytelling via props, palette discipline
    /// - `game-design`: diegetic signals carry narrative without dialogue
    /// - `3d-games`: BoxColliders + static flags + URP/Lit (per BOT-F05/F10)
    /// - `game-development`: runtime components (BlinkingLight) separated from editor setup
    ///
    /// Scope discipline: this file only adds props, never moves NPCs or dressers the
    /// base greenhouse. Called from BotanikaDresser.Dress() tail as last pass.
    /// Idempotent: re-run safe via propsRoot parent reuse.
    /// </summary>
    public static class BotanikaEnvProps
    {
        private const string FurnitureFbx = "Assets/_Project/Vendor/Kenney/furniture-kit/Models/FBX format";
        private const string MaterialsDir = "Assets/_Project/Materials/Tints";

        // ART_BIBLE accent materials (added by this layer — base dresser has warm palette)
        private static Material _matMetalCool;   // Server rack, electronics
        private static Material _matGlass;       // Laptop screen emissive hint
        private static Material _matFoilSilver;  // Stas hat
        private static Material _matBottleDark;  // Nikolai bottle
        private static Material _matTurka;       // Kirill copper coffee pot
        private static Material _matGraffiti;    // East wall accent red text

        public static void Apply(GameObject propsRoot)
        {
            if (propsRoot == null)
            {
                Debug.LogError("[BotanikaEnvProps] propsRoot is null — aborting.");
                return;
            }

            InitAccentMaterials();

            // mm-review CRITICAL fix: idempotent parent — if Env_StoryProps
            // exists from a previous Apply call without BotanikaDresser cleanup,
            // destroy it first to prevent duplicate "ServerRack (1)" siblings.
            // Defensive: BotanikaDresser already clears Botanika_Props root each
            // Dress() call, but direct calls to Apply() or cleanup failures
            // would still leak without this check.
            var existingEnv = propsRoot.transform.Find("Env_StoryProps");
            if (existingEnv != null)
            {
                Object.DestroyImmediate(existingEnv.gameObject);
            }

            var envRoot = new GameObject("Env_StoryProps");
            envRoot.transform.SetParent(propsRoot.transform, worldPositionStays: false);

            BuildServerRack(envRoot);
            BuildGraffiti(envRoot);
            BuildMilaDesk(envRoot);
            BuildKirillKitchen(envRoot);
            BuildNikolaiCorner(envRoot);
            BuildStasFoilHat(envRoot);
            ScatterExtraBooks(envRoot);

            Debug.Log("[BotanikaEnvProps] DONE — 7 diegetic prop groups added.");
        }

        private static void InitAccentMaterials()
        {
            if (!Directory.Exists(MaterialsDir))
            {
                Directory.CreateDirectory(MaterialsDir);
                AssetDatabase.Refresh();
            }

            _matMetalCool = LoadOrCreate("Tint_MetalCool", new Color(0.34f, 0.38f, 0.44f), 0.75f);
            _matGlass = LoadOrCreate("Tint_LaptopScreen", new Color(0.12f, 0.18f, 0.22f), 0.9f);
            _matFoilSilver = LoadOrCreate("Tint_FoilSilver", new Color(0.85f, 0.87f, 0.90f), 0.85f);
            _matBottleDark = LoadOrCreate("Tint_BottleDark", new Color(0.18f, 0.12f, 0.09f), 0.65f);
            _matTurka = LoadOrCreate("Tint_TurkaCopper", new Color(0.72f, 0.42f, 0.22f), 0.7f);
            _matGraffiti = LoadOrCreate("Tint_GraffitiRed", new Color(0.78f, 0.20f, 0.15f), 0.1f);
            AssetDatabase.SaveAssets();
        }

        private static Material LoadOrCreate(string name, Color color, float smoothness)
        {
            string path = $"{MaterialsDir}/{name}.mat";
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogWarning("[BotanikaEnvProps] URP/Lit shader not found — falling back to Standard");
                shader = Shader.Find("Standard");
            }
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing == null)
            {
                var mat = new Material(shader) { name = name };
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", smoothness > 0.6f ? 0.85f : 0f);
                AssetDatabase.CreateAsset(mat, path);
                return mat;
            }
            existing.shader = shader;
            if (existing.HasProperty("_BaseColor")) existing.SetColor("_BaseColor", color);
            if (existing.HasProperty("_Smoothness")) existing.SetFloat("_Smoothness", smoothness);
            if (existing.HasProperty("_Metallic")) existing.SetFloat("_Metallic", smoothness > 0.6f ? 0.85f : 0f);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        // ---------- SERVER RACK ----------
        private static void BuildServerRack(GameObject parent)
        {
            var rackRoot = new GameObject("ServerRack");
            rackRoot.transform.SetParent(parent.transform, false);
            rackRoot.transform.position = new Vector3(-4.8f, 0f, -4.2f);
            rackRoot.transform.rotation = Quaternion.Euler(0f, 45f, 0f);  // angled toward player path

            // Base: cross table as rack body
            PlaceKenney(rackRoot, "tableCross.fbx", Vector3.zero, Quaternion.identity, Vector3.one, _matMetalCool);

            // Two square table lamps stacked to suggest rack units
            PlaceKenney(rackRoot, "lampSquareTable.fbx", new Vector3(0f, 0.8f, 0f),
                Quaternion.identity, Vector3.one * 0.8f, _matMetalCool);
            PlaceKenney(rackRoot, "lampSquareTable.fbx", new Vector3(0f, 1.4f, 0f),
                Quaternion.identity, Vector3.one * 0.8f, _matMetalCool);

            // 3 blinking point lights (LED indicators)
            var ledPositions = new[]
            {
                new Vector3(0.15f, 0.85f, 0.25f),
                new Vector3(-0.15f, 0.90f, 0.25f),
                new Vector3(0.0f, 1.45f, 0.25f)
            };
            var ledColors = new[]
            {
                new Color(0.6f, 0.85f, 1.0f),  // cool cyan
                new Color(0.9f, 0.95f, 1.0f),  // white-blue
                new Color(0.5f, 0.9f, 0.95f)   // teal
            };
            for (int i = 0; i < ledPositions.Length; i++)
            {
                var ledGo = new GameObject($"LED_{i}");
                ledGo.transform.SetParent(rackRoot.transform, false);
                ledGo.transform.localPosition = ledPositions[i];
                var light = ledGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = ledColors[i];
                light.intensity = 0.4f;
                light.range = 2.5f;
                light.shadows = LightShadows.None;  // perf: M1 8GB shadow budget tight
                var blink = ledGo.AddComponent<BlinkingLight>();
                blink.minIntensity = 0.08f;
                blink.maxIntensity = 0.9f;
                blink.cyclePeriod = 1.2f + i * 0.4f;  // stagger periods so not synchronized
                blink.phase = i * 0.33f;
                blink.smooth = i != 1;  // middle one hard-pingpong for contrast
            }
        }

        // ---------- GRAFFITI «segfault == freedom» ----------
        private static void BuildGraffiti(GameObject parent)
        {
            var go = new GameObject("Graffiti_Segfault");
            go.transform.SetParent(parent.transform, false);
            // East wall is at x=5.5, quads face -X. Place slightly inside wall.
            go.transform.position = new Vector3(5.35f, 1.70f, 0f);
            go.transform.rotation = Quaternion.Euler(0f, -90f, 0f);  // face -X

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = "segfault == freedom";
            tmp.fontSize = 1.8f;
            tmp.color = new Color(0.78f, 0.20f, 0.15f);  // accent2 from ART_BIBLE §3.1
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            // mm-review MEDIUM fix: explicitly bind default TMP font asset so
            // the graffiti mesh renders even if TMP_Settings.defaultFontAsset
            // is unset or Essentials missing. LiberationSans SDF ships with TMP.
            if (tmp.font == null)
            {
                var defaultFont = TMP_Settings.defaultFontAsset;
                if (defaultFont == null)
                {
                    defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                }
                if (defaultFont != null) tmp.font = defaultFont;
                else Debug.LogWarning("[BotanikaEnvProps] No TMP font asset available for Graffiti_Segfault — text may not render.");
            }
            // Bounding rect so text has width — TMP 3D sizes by rect transform
            var rect = go.GetComponent<RectTransform>();
            if (rect != null) rect.sizeDelta = new Vector2(3.5f, 1.0f);

            // Mark as non-static (TMP meshes don't combine with static batching cleanly)
            // and skip collider — graffiti is non-interactable.
        }

        // ---------- МИЛА desk + laptop ----------
        private static void BuildMilaDesk(GameObject parent)
        {
            var root = new GameObject("Mila_Desk");
            root.transform.SetParent(parent.transform, false);
            root.transform.position = new Vector3(-3.5f, 0f, 1.4f);
            root.transform.rotation = Quaternion.Euler(0f, 45f, 0f);

            // Small desk
            PlaceKenney(root, "sideTableDrawers.fbx", Vector3.zero, Quaternion.identity, Vector3.one, null);
            // Laptop on top
            PlaceKenney(root, "laptop.fbx", new Vector3(0f, 0.82f, 0f),
                Quaternion.Euler(0f, 15f, 0f), Vector3.one, null);
        }

        // ---------- КИРИЛЛ kitchen counter ----------
        private static void BuildKirillKitchen(GameObject parent)
        {
            var root = new GameObject("Kirill_Kitchen");
            root.transform.SetParent(parent.transform, false);
            root.transform.position = new Vector3(3.5f, 0f, 2.0f);
            root.transform.rotation = Quaternion.Euler(0f, -45f, 0f);

            // Stove (grounded in ritual — he grows mushrooms + brews coffee)
            PlaceKenney(root, "kitchenStove.fbx", Vector3.zero, Quaternion.identity, Vector3.one, null);
            // Coffee machine beside stove
            PlaceKenney(root, "kitchenCoffeeMachine.fbx", new Vector3(0.8f, 0f, 0f),
                Quaternion.identity, Vector3.one, null);

            // Turka primitive (small cylinder with handle bump) on stove top
            var turka = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            turka.name = "Kirill_Turka";
            turka.transform.SetParent(root.transform, false);
            turka.transform.localPosition = new Vector3(-0.05f, 0.95f, 0f);
            turka.transform.localScale = new Vector3(0.18f, 0.12f, 0.18f);
            AssignMaterial(turka, _matTurka);
            // Remove default capsule collider (added by CreatePrimitive) + replace via helper
            Object.DestroyImmediate(turka.GetComponent<Collider>());
            ColliderHelper.AddSimpleCollider(turka);
            ColliderHelper.MarkStaticProp(turka);
        }

        // ---------- НИКОЛАЙ corner table + bottle ----------
        private static void BuildNikolaiCorner(GameObject parent)
        {
            var root = new GameObject("Nikolai_Corner");
            root.transform.SetParent(parent.transform, false);
            root.transform.position = new Vector3(-4.5f, 0f, 4.5f);
            root.transform.rotation = Quaternion.Euler(0f, 135f, 0f);

            // Corner side-table
            PlaceKenney(root, "sideTable.fbx", Vector3.zero, Quaternion.identity, Vector3.one, null);

            // Bottle primitive (dark wine)
            var bottle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bottle.name = "Nikolai_Bottle";
            bottle.transform.SetParent(root.transform, false);
            bottle.transform.localPosition = new Vector3(0.15f, 1.02f, 0.05f);
            bottle.transform.localScale = new Vector3(0.09f, 0.18f, 0.09f);
            AssignMaterial(bottle, _matBottleDark);
            Object.DestroyImmediate(bottle.GetComponent<Collider>());
            ColliderHelper.AddSimpleCollider(bottle);
            ColliderHelper.MarkStaticProp(bottle);

            // Books (data-жрец тексты) stacked beside bottle
            PlaceKenney(root, "books.fbx", new Vector3(-0.25f, 0.82f, 0.0f),
                Quaternion.Euler(0f, 30f, 0f), Vector3.one * 0.8f, null);
        }

        // ---------- СТАС foil hat ----------
        private static void BuildStasFoilHat(GameObject parent)
        {
            var hat = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            hat.name = "Stas_FoilHat";
            hat.transform.SetParent(parent.transform, false);
            // Stas expected spawn (0, 0, -3.5) per plan BOT-N03. Hat sits where his head
            // will be. BOT-N08 will re-parent to NPC head bone when Quaternius model lands.
            hat.transform.position = new Vector3(0f, 1.95f, -3.5f);
            hat.transform.localScale = new Vector3(0.38f, 0.22f, 0.38f);
            AssignMaterial(hat, _matFoilSilver);
            Object.DestroyImmediate(hat.GetComponent<Collider>());
            // No collider — player walks through it (decorative only).
        }

        // ---------- SCATTER extra books on bookcases ----------
        private static void ScatterExtraBooks(GameObject parent)
        {
            var root = new GameObject("ExtraBooks");
            root.transform.SetParent(parent.transform, false);

            // Left bookcase (at -4.5, 0, 4.8 in base dresser) — put 3 book stacks on shelves
            var leftStacks = new[]
            {
                new Vector3(-4.6f, 0.55f, 4.6f),
                new Vector3(-4.4f, 1.15f, 4.6f),
                new Vector3(-4.55f, 1.75f, 4.6f),
            };
            for (int i = 0; i < leftStacks.Length; i++)
            {
                PlaceKenney(root, "books.fbx", leftStacks[i],
                    Quaternion.Euler(0f, i * 27f - 15f, 0f), Vector3.one * 0.85f, null);
            }

            // Right low bookcase (at 4.5, 0, 4.8) — 3 more stacks
            var rightStacks = new[]
            {
                new Vector3(4.4f, 0.55f, 4.6f),
                new Vector3(4.55f, 0.95f, 4.6f),
                new Vector3(4.45f, 1.35f, 4.6f),
            };
            for (int i = 0; i < rightStacks.Length; i++)
            {
                PlaceKenney(root, "books.fbx", rightStacks[i],
                    Quaternion.Euler(0f, i * -33f + 18f, 0f), Vector3.one * 0.85f, null);
            }
        }

        // ---------- HELPERS ----------
        private static void PlaceKenney(GameObject parent, string assetFile,
            Vector3 localPos, Quaternion localRot, Vector3 scale, Material forcedMat)
        {
            string path = $"{FurnitureFbx}/{assetFile}";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"[BotanikaEnvProps] Kenney asset not found: {path}");
                return;
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (instance == null) return;
            instance.transform.SetParent(parent.transform, worldPositionStays: false);
            instance.transform.localPosition = localPos;
            instance.transform.localRotation = localRot;
            instance.transform.localScale = scale;
            if (forcedMat != null) AssignMaterial(instance, forcedMat);
            ColliderHelper.AddSimpleCollider(instance);
            ColliderHelper.MarkStaticProp(instance);
        }

        /// <summary>
        /// Uniform-tint material assignment (matches base BotanikaDresser pattern).
        ///
        /// mm-review MEDIUM context: this overwrites ALL submesh material slots
        /// with a single material. This is INTENTIONAL for our stylized look —
        /// Kenney FBX don't bundle textures, so multi-submesh models need uniform
        /// tint to appear as one coherent material rather than hot-pink missing-mat.
        /// If a future prop needs per-submesh materials, add a dedicated assignment
        /// helper alongside this one rather than extending AssignMaterial.
        /// </summary>
        private static void AssignMaterial(GameObject go, Material mat)
        {
            if (go == null || mat == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                var count = r.sharedMaterials.Length;
                var mats = new Material[count];
                for (int i = 0; i < count; i++) mats[i] = mat;
                r.sharedMaterials = mats;
            }
        }

        /// <summary>
        /// Verification helper called by BotanikaVerification.RunAll (BOT-T01).
        /// Checks that all expected environmental prop GameObjects exist in the scene.
        /// </summary>
        public static bool Verify(out string reason)
        {
            string[] expected = {
                "ServerRack",
                "Graffiti_Segfault",
                "Mila_Desk",
                "Kirill_Kitchen",
                "Nikolai_Corner",
                "Stas_FoilHat",
                "ExtraBooks",
            };
            foreach (var name in expected)
            {
                var go = GameObject.Find(name);
                if (go == null)
                {
                    reason = $"Missing env prop: {name}";
                    return false;
                }
            }

            var rack = GameObject.Find("ServerRack");
            if (rack != null)
            {
                int leds = rack.GetComponentsInChildren<BlinkingLight>().Length;
                if (leds < 3)
                {
                    reason = $"ServerRack has {leds} BlinkingLight components — expected 3";
                    return false;
                }
            }

            reason = "OK";
            return true;
        }
    }
}
