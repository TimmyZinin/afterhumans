using UnityEditor;
using UnityEngine;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// BOT-F05: Helper для замены MeshCollider на BoxCollider в Dresser pipelines.
    ///
    /// Skill `3d-games` anti-pattern: *«Mesh colliders everywhere»*.
    /// Skill `game-development` performance budget: simple colliders cheaper than mesh.
    ///
    /// На M1 8GB 500+ MeshCollider (как было до BOT-F05) = серьёзная просадка physics
    /// update. BoxCollider bounds-based даёт 95% точности для furniture/walls/props
    /// при 10× меньшей стоимости physics queries.
    ///
    /// Exception list (decorative, no collider):
    /// - grass*, flower_*, plant_small* — игрок должен проходить сквозь них
    /// - particles/VFX
    ///
    /// Usage: `ColliderHelper.AddSimpleCollider(instance)` в Dressers вместо
    /// прямого `instance.AddComponent<MeshCollider>()`.
    /// </summary>
    public static class ColliderHelper
    {
        private static readonly string[] DecorativeKeywords =
        {
            "grass", "flower_", "plant_small", "crops_leaf",
            "mushroom_small", "rock_tiny"
        };

        /// <summary>
        /// BOT-F10: Marks a static prop instance с правильными StaticEditorFlags
        /// чтобы Unity мог batch them, bake GI/occlusion, reflect в probes.
        /// Skill `3d-games`: batching = meaningful draw call reduction on M1 8GB.
        /// Skip для NPC (animated), Kafka, exit triggers, interactables.
        /// </summary>
        public static void MarkStaticProp(GameObject instance)
        {
            if (instance == null) return;
            GameObjectUtility.SetStaticEditorFlags(instance,
                StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.OccluderStatic |
                StaticEditorFlags.OccludeeStatic |
                StaticEditorFlags.ContributeGI |
                StaticEditorFlags.ReflectionProbeStatic);
        }

        /// <summary>
        /// Adds a BoxCollider sized to the combined renderer bounds of the instance.
        /// Skips decorative props (grass, flowers, small plants) — no collider at all.
        /// If instance already has a collider, does nothing.
        /// </summary>
        public static void AddSimpleCollider(GameObject instance)
        {
            if (instance == null) return;
            if (instance.GetComponent<Collider>() != null) return;

            string name = instance.name.ToLowerInvariant();
            foreach (var keyword in DecorativeKeywords)
            {
                if (name.Contains(keyword))
                {
                    return; // decorative, no collider
                }
            }

            // Compute bounds from all child renderers (handles multi-mesh props)
            Bounds? combinedBounds = null;
            foreach (var rend in instance.GetComponentsInChildren<Renderer>(includeInactive: false))
            {
                var b = rend.bounds;
                if (combinedBounds == null)
                {
                    combinedBounds = b;
                }
                else
                {
                    var cb = combinedBounds.Value;
                    cb.Encapsulate(b);
                    combinedBounds = cb;
                }
            }

            if (combinedBounds == null)
            {
                // No renderer → use mesh filter bounds in local space
                var mf = instance.GetComponentInChildren<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) return;
                var mb = mf.sharedMesh.bounds;
                var box = instance.AddComponent<BoxCollider>();
                box.center = mb.center;
                box.size = mb.size;
                return;
            }

            // World bounds → local space for BoxCollider.
            // mm-review HIGH fix: size must divide by |lossyScale| component-wise
            // (axis-aligned bounds assumption; rotation handled by InverseTransformPoint
            // on center but size is axis-scalar, not a transformed vector).
            var box2 = instance.AddComponent<BoxCollider>();
            var worldBounds = combinedBounds.Value;
            box2.center = instance.transform.InverseTransformPoint(worldBounds.center);
            var ls = instance.transform.lossyScale;
            var invScale = new Vector3(
                ls.x == 0 ? 1f : 1f / Mathf.Abs(ls.x),
                ls.y == 0 ? 1f : 1f / Mathf.Abs(ls.y),
                ls.z == 0 ? 1f : 1f / Mathf.Abs(ls.z));
            box2.size = Vector3.Scale(worldBounds.size, invScale);
        }

        /// <summary>
        /// Purge existing MeshColliders from a prop root and replace with BoxColliders.
        /// Used when re-dressing scenes after BOT-F05 patches.
        /// </summary>
        [MenuItem("Afterhumans/Setup/Purge MeshColliders In Scene")]
        public static void PurgeAndReplaceInScene()
        {
            int purged = 0;
            int added = 0;
            var meshColliders = Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None);
            foreach (var mc in meshColliders)
            {
                var go = mc.gameObject;
                Object.DestroyImmediate(mc);
                purged++;
                // Replace with BoxCollider only if instance has visible bounds
                if (go.GetComponent<Renderer>() != null)
                {
                    AddSimpleCollider(go);
                    if (go.GetComponent<BoxCollider>() != null) added++;
                }
            }
            Debug.Log($"[ColliderHelper] Purged {purged} MeshColliders, added {added} BoxColliders");
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        }
    }
}
