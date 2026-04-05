using UnityEditor;
using UnityEngine;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// BOT-A04: Volumetric atmosphere layer for Scene_Botanika.
    ///
    /// Adds the "god rays through glass ceiling" feeling that turns a lit room
    /// into a Sable/Firewatch oasis:
    /// - Glass ceiling quad (semi-transparent URP/Lit, high smoothness) over the
    ///   entire 11×11 floor at y=3.2
    /// - SunRayDust ParticleSystem: 50 lazy dust motes under the east window,
    ///   falling slowly through the beam, simulation space World, warm tint
    /// - 2 supplemental Point Lights near the east/north windows for "warmth
    ///   bleeding in" hint (complements BOT-A06 accent lights)
    ///
    /// Skill references:
    /// - `3d-web-experience`: volumetric atmosphere is the single biggest lift
    ///   from Minecraft-cube to Journey-interior feel
    /// - `scroll-experience`: god rays are the first visual the player sees on
    ///   wake-up, sets the entire tone for the 30-second test (DES-1)
    /// - `game-art` ART_BIBLE §4.1: golden hour diffuse sun through glass
    ///
    /// Scope: ONLY atmosphere. Collider/static flags skipped for quad
    /// (transparent, non-interactive). Particle system is dynamic (runtime
    /// simulation). Lights set to Baked + BlinkingLight NOT attached here
    /// (accent lights are steady, not pulsing).
    ///
    /// Idempotent: finds existing Botanika_Atmosphere group, destroys, recreates.
    /// </summary>
    public static class BotanikaAtmosphere
    {
        private const string GroupName = "Botanika_Atmosphere";
        private const string GlassMatPath = "Assets/_Project/Materials/Skyboxes/Glass_Greenhouse.mat";
        private const string DustMatPath = "Assets/_Project/Materials/Skyboxes/DustMote.mat";

        public static void Apply(GameObject propsRoot)
        {
            if (propsRoot == null)
            {
                Debug.LogError("[BotanikaAtmosphere] propsRoot is null — aborting.");
                return;
            }

            // Idempotent cleanup
            var existing = propsRoot.transform.Find(GroupName);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var root = new GameObject(GroupName);
            root.transform.SetParent(propsRoot.transform, worldPositionStays: false);

            BuildGlassCeiling(root);
            BuildSunRayDust(root);
            BuildWindowAccentLights(root);
            BuildInteriorAccentLights(root);   // BOT-A06
            BuildWindowGlassOverlays(root);    // BOT-A07

            Debug.Log("[BotanikaAtmosphere] DONE — glass ceiling + dust motes + 2 window accents + 3 interior accents + 11 window overlays.");
        }

        // ---------- BOT-A06: interior warm accent lights ----------
        private static void BuildInteriorAccentLights(GameObject parent)
        {
            // Three focused warm point lights на ключевые NPC stations для
            // «islands of warmth» эффекта (ART_BIBLE §4.1 — warm pools of light
            // между cool shadows). Server rack уже имеет 3 cool blinking LEDs
            // как contrasting accent, поэтому здесь только warm interior pools.
            //
            // mm-review MEDIUM fix: all light colors now palette-driven from
            // Botanika SceneTheme instead of hardcoded (BOT-A08 discipline).
            var theme = AssetDatabase.LoadAssetAtPath<Afterhumans.Art.SceneTheme>(
                "Assets/_Project/Art/Themes/Botanika.asset");
            Color warmColor = theme != null ? theme.accent1 : new Color(1.0f, 0.82f, 0.52f);
            // Kitchen slightly warmer/orange — blend accent1 with accent2
            Color kitchenColor = theme != null
                ? Color.Lerp(theme.accent1, theme.accent2, 0.35f)
                : new Color(1.0f, 0.78f, 0.48f);

            CreatePointLight(parent, "Accent_CoffeeTable", new Vector3(0f, 2.0f, 1.8f),
                warmColor, 1.8f, 5f);
            CreatePointLight(parent, "Accent_NikolaiCorner", new Vector3(-4.2f, 2.5f, 4.3f),
                warmColor, 1.4f, 4f);
            CreatePointLight(parent, "Accent_KirillKitchen", new Vector3(3.8f, 2.3f, 2.2f),
                kitchenColor, 1.3f, 3.5f);
        }

        // ---------- BOT-A07: window glass overlays ----------
        private static void BuildWindowGlassOverlays(GameObject parent)
        {
            // Place thin glass quads in front of each wallWindow tile so the
            // greenhouse has actual visible transparent glass, not just opaque
            // walls with window-shaped openings. Mirrors greenhouseShell window
            // slots from BotanikaDresser.BuildGreenhouseShell.
            //
            // mm-review CRITICAL fix: rotation scheme was inverted. Quad default
            // normal is +Z; each wall's overlay must face INTO the room:
            //   North wall z=+5.5 → player looks at quad from -Z → normal = -Z
            //     → Euler(0, 180, 0) (+Z rotated Y=180° → -Z)
            //   East wall  x=+5.5 → player looks from -X → normal = -X
            //     → Euler(0, -90, 0) (+Z rotated Y=-90° → -X)
            //   West wall  x=-5.5 → player looks from +X → normal = +X
            //     → Euler(0, 90, 0) (+Z rotated Y=+90° → +X)
            //
            // mm-review MEDIUM fix: WestWindow_z5 was missing per pattern
            // (z+5)%3==1 → z=5 is a valid match.
            var glassMat = GetOrCreateGlassMaterial();

            // North wall (z=+5.5), normal must face -Z (into room)
            PlaceWindowOverlay(parent, glassMat, "NorthWindow_xneg4", new Vector3(-4f, 1.5f, 5.4f), Quaternion.Euler(0f, 180f, 0f));
            PlaceWindowOverlay(parent, glassMat, "NorthWindow_xneg1", new Vector3(-1f, 1.5f, 5.4f), Quaternion.Euler(0f, 180f, 0f));
            PlaceWindowOverlay(parent, glassMat, "NorthWindow_x2",    new Vector3(2f,  1.5f, 5.4f), Quaternion.Euler(0f, 180f, 0f));
            PlaceWindowOverlay(parent, glassMat, "NorthWindow_x5",    new Vector3(5f,  1.5f, 5.4f), Quaternion.Euler(0f, 180f, 0f));

            // East wall (x=+5.5), normal must face -X (into room)
            PlaceWindowOverlay(parent, glassMat, "EastWindow_zneg3", new Vector3(5.4f, 1.5f, -3f), Quaternion.Euler(0f, -90f, 0f));
            PlaceWindowOverlay(parent, glassMat, "EastWindow_z0",    new Vector3(5.4f, 1.5f,  0f), Quaternion.Euler(0f, -90f, 0f));
            PlaceWindowOverlay(parent, glassMat, "EastWindow_z3",    new Vector3(5.4f, 1.5f,  3f), Quaternion.Euler(0f, -90f, 0f));

            // West wall (x=-5.5), normal must face +X (into room)
            PlaceWindowOverlay(parent, glassMat, "WestWindow_zneg4", new Vector3(-5.4f, 1.5f, -4f), Quaternion.Euler(0f, 90f, 0f));
            PlaceWindowOverlay(parent, glassMat, "WestWindow_zneg1", new Vector3(-5.4f, 1.5f, -1f), Quaternion.Euler(0f, 90f, 0f));
            PlaceWindowOverlay(parent, glassMat, "WestWindow_z2",    new Vector3(-5.4f, 1.5f,  2f), Quaternion.Euler(0f, 90f, 0f));
            PlaceWindowOverlay(parent, glassMat, "WestWindow_z5",    new Vector3(-5.4f, 1.5f,  5f), Quaternion.Euler(0f, 90f, 0f));
        }

        private static void PlaceWindowOverlay(GameObject parent, Material glassMat, string name,
            Vector3 worldPos, Quaternion worldRot)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(parent.transform, false);
            go.transform.position = worldPos;
            go.transform.rotation = worldRot;
            go.transform.localScale = new Vector3(0.9f, 1.6f, 1f);  // window pane dims
            var rend = go.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = glassMat;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }

        private static Material GetOrCreateGlassMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(GlassMatPath);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogWarning("[BotanikaAtmosphere] URP/Lit not found — falling back to Standard transparent");
                shader = Shader.Find("Standard");
            }
            if (mat == null)
            {
                mat = new Material(shader) { name = "Glass_Greenhouse" };
                AssetDatabase.CreateAsset(mat, GlassMatPath);
            }
            else
            {
                mat.shader = shader;
            }

            // URP/Lit transparent configuration
            // _Surface 0=Opaque 1=Transparent
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f); // Alpha
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f); // Off — both sides visible
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);

            // Appearance: cool-tinted glass, high smoothness
            var glassColor = new Color(0.85f, 0.92f, 0.98f, 0.18f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", glassColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", glassColor);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.92f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);

            // URP rendering queue for transparent
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            // Keywords to force alpha blend in URP
            mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        private static void BuildGlassCeiling(GameObject parent)
        {
            var glass = GameObject.CreatePrimitive(PrimitiveType.Quad);
            glass.name = "GlassCeiling";
            glass.transform.SetParent(parent.transform, false);
            // mm-review HIGH fix: Unity Quad default normal +Z. Rotation Euler(90,0,0)
            // in Unity's left-handed system tilts it face-up (floor orientation —
            // normal +Y). For a ceiling visible from BELOW (player looking up), we
            // need face-down (normal -Y), which is Euler(-90, 0, 0). With Cull=Off
            // the back side would render too but lighting on the wrong-facing side
            // would be broken. Correct rotation is -90°.
            glass.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            glass.transform.position = new Vector3(0f, 3.2f, 0f);
            glass.transform.localScale = new Vector3(12f, 12f, 1f);  // 11x11 floor + overhang

            var mat = GetOrCreateGlassMaterial();
            var rend = glass.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = mat;

            // Drop collider — player never touches ceiling
            var col = glass.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            // Not static — transparent/shadow-receive interaction simpler with dynamic flag
        }

        private static void BuildSunRayDust(GameObject parent)
        {
            var dust = new GameObject("SunRayDust");
            dust.transform.SetParent(parent.transform, false);
            // Position dust volume under the east wall windows (room east is x=+5)
            // so the beam appears as if entering through the east-facing panes.
            dust.transform.position = new Vector3(3.5f, 1.8f, 0f);

            var ps = dust.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // Main module
            var main = ps.main;
            main.startLifetime = 9f;
            main.startSize = 0.025f;
            main.startSpeed = 0.05f;
            main.startColor = new Color(0.96f, 0.85f, 0.64f, 0.35f);  // warm sunlight motes
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 120;
            main.loop = true;
            main.prewarm = true;
            main.gravityModifier = 0f;

            // Emission
            var emission = ps.emission;
            emission.rateOverTime = 6f;

            // Shape — box under east window
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(2.0f, 2.5f, 6f);  // stretches along Z (north-south)

            // Velocity over lifetime — slow drift down + slightly toward camera
            var vol = ps.velocityOverLifetime;
            vol.enabled = true;
            vol.space = ParticleSystemSimulationSpace.World;
            vol.x = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);
            vol.y = new ParticleSystem.MinMaxCurve(-0.08f, -0.02f);
            vol.z = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);

            // Color over lifetime — fade in and out for breath effect
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] {
                    new GradientColorKey(new Color(0.96f, 0.85f, 0.64f), 0.0f),
                    new GradientColorKey(new Color(1.0f, 0.92f, 0.72f), 0.5f),
                    new GradientColorKey(new Color(0.96f, 0.85f, 0.64f), 1.0f),
                },
                new[] {
                    new GradientAlphaKey(0f, 0.0f),
                    new GradientAlphaKey(0.55f, 0.3f),
                    new GradientAlphaKey(0.55f, 0.7f),
                    new GradientAlphaKey(0f, 1.0f),
                }
            );
            col.color = new ParticleSystem.MinMaxGradient(gradient);

            // Size over lifetime — subtle breath
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            var sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.5f),
                new Keyframe(0.5f, 1.0f),
                new Keyframe(1f, 0.5f)
            );
            sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Renderer — unlit particle material persisted as asset to prevent
            // memory leak on repeat Dress() runs (mm-review MEDIUM fix).
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.sharedMaterial = GetOrCreateDustMoteMaterial();
                renderer.sortingFudge = -4f;  // render behind transparent glass
            }

            ps.Play();
        }

        /// <summary>
        /// mm-review MEDIUM fix: persist DustMote particle material as project
        /// asset so repeat Dress() runs reuse it instead of leaking fresh runtime
        /// Materials into scene serialization.
        /// </summary>
        private static Material GetOrCreateDustMoteMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(DustMatPath);
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogError("[BotanikaAtmosphere] No particle shader available for DustMote.");
                return null;
            }
            if (mat == null)
            {
                mat = new Material(shader) { name = "DustMote" };
                AssetDatabase.CreateAsset(mat, DustMatPath);
            }
            else
            {
                mat.shader = shader;
            }
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", new Color(1f, 0.9f, 0.7f, 0.5f));
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", new Color(1f, 0.9f, 0.7f, 0.5f));
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        private static void BuildWindowAccentLights(GameObject parent)
        {
            // Two soft warm point lights just inside the east and north windows
            // to suggest afternoon sun bleeding through glass. Complements but
            // doesn't replace BOT-A06 focused interior accents.
            CreatePointLight(parent, "WindowAccent_East", new Vector3(4.8f, 2.4f, 0f),
                new Color(1.0f, 0.86f, 0.64f), 1.8f, 8f);
            CreatePointLight(parent, "WindowAccent_North", new Vector3(0f, 2.4f, 4.8f),
                new Color(1.0f, 0.86f, 0.64f), 1.6f, 8f);
        }

        private static void CreatePointLight(GameObject parent, string name, Vector3 position,
            Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;  // perf on M1 8GB
            light.lightmapBakeType = LightmapBakeType.Baked;  // baked for GI (BOT-A09)
        }

        /// <summary>
        /// BOT-T01 integration: verifies atmosphere layer objects exist.
        /// Includes A04 (glass, dust, window accents) + A06 (interior accents)
        /// + A07 (window overlays).
        /// </summary>
        public static bool Verify(out string reason)
        {
            var glass = GameObject.Find("GlassCeiling");
            if (glass == null) { reason = "GlassCeiling missing"; return false; }
            var rend = glass.GetComponent<Renderer>();
            if (rend == null || rend.sharedMaterial == null)
            {
                reason = "GlassCeiling has no material";
                return false;
            }

            var dust = GameObject.Find("SunRayDust");
            if (dust == null) { reason = "SunRayDust missing"; return false; }
            var ps = dust.GetComponent<ParticleSystem>();
            if (ps == null) { reason = "SunRayDust has no ParticleSystem"; return false; }

            // A04 window accent lights
            string[] lightNames = { "WindowAccent_East", "WindowAccent_North",
                // A06 interior accents
                "Accent_CoffeeTable", "Accent_NikolaiCorner", "Accent_KirillKitchen" };
            foreach (var n in lightNames)
            {
                var go = GameObject.Find(n);
                if (go == null) { reason = $"{n} missing"; return false; }
                var l = go.GetComponent<Light>();
                if (l == null) { reason = $"{n} has no Light component"; return false; }
            }

            // A07 window overlays — at least 10 overlay quads expected
            int overlayCount = 0;
            string[] overlayPrefixes = { "NorthWindow_", "EastWindow_", "WestWindow_" };
            var allGos = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in allGos)
            {
                foreach (var p in overlayPrefixes)
                {
                    if (go.name.StartsWith(p)) { overlayCount++; break; }
                }
            }
            if (overlayCount < 11)
            {
                reason = $"Window glass overlays count={overlayCount} expected >=11 (4 north + 3 east + 4 west)";
                return false;
            }

            reason = "OK";
            return true;
        }
    }
}
