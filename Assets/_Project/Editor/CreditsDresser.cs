using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Afterhumans.UI;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Builds the Scene_Credits UI hierarchy: background image, final text panel,
    /// credits panel, sting panel, wired to CreditsSequence component.
    /// </summary>
    public static class CreditsDresser
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scene_Credits.unity";

        [MenuItem("Afterhumans/Setup/Dress Credits")]
        public static void Dress()
        {
            Debug.Log("[CreditsDresser] Opening Scene_Credits...");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // Clear anything we added before
            var existing = GameObject.Find("CreditsRoot");
            if (existing != null) Object.DestroyImmediate(existing);

            var root = new GameObject("CreditsRoot");

            // Canvas
            var canvasGO = new GameObject("Canvas");
            canvasGO.transform.SetParent(root.transform, worldPositionStays: false);
            var canvas = Undo.AddComponent<Canvas>(canvasGO);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            Undo.AddComponent<CanvasScaler>(canvasGO).uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            Undo.AddComponent<GraphicRaycaster>(canvasGO);

            // Background full-screen black
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(canvasGO.transform, worldPositionStays: false);
            var bgRect = Undo.AddComponent<RectTransform>(bgGO);
            FullStretch(bgRect);
            var bgImg = Undo.AddComponent<Image>(bgGO);
            bgImg.color = Color.black;
            bgImg.raycastTarget = false;

            // --- Final Text Panel (center)
            var finalTextGroupGO = new GameObject("FinalTextGroup");
            finalTextGroupGO.transform.SetParent(canvasGO.transform, worldPositionStays: false);
            var ftRect = Undo.AddComponent<RectTransform>(finalTextGroupGO);
            ftRect.anchorMin = new Vector2(0.1f, 0.2f);
            ftRect.anchorMax = new Vector2(0.9f, 0.8f);
            ftRect.offsetMin = Vector2.zero;
            ftRect.offsetMax = Vector2.zero;
            var finalTextGroup = Undo.AddComponent<CanvasGroup>(finalTextGroupGO);
            finalTextGroup.alpha = 0f;

            var finalTextGO = new GameObject("FinalText");
            finalTextGO.transform.SetParent(finalTextGroupGO.transform, worldPositionStays: false);
            var finalTextRect = Undo.AddComponent<RectTransform>(finalTextGO);
            FullStretch(finalTextRect);
            var finalText = Undo.AddComponent<TextMeshProUGUI>(finalTextGO);
            finalText.text = "";
            finalText.fontSize = 32;
            finalText.color = Color.white;
            finalText.alignment = TextAlignmentOptions.Center;
            finalText.enableWordWrapping = true;

            // --- Credits Panel
            var creditsGroupGO = new GameObject("CreditsGroup");
            creditsGroupGO.transform.SetParent(canvasGO.transform, worldPositionStays: false);
            var crRect = Undo.AddComponent<RectTransform>(creditsGroupGO);
            crRect.anchorMin = new Vector2(0.15f, 0.15f);
            crRect.anchorMax = new Vector2(0.85f, 0.85f);
            crRect.offsetMin = Vector2.zero;
            crRect.offsetMax = Vector2.zero;
            var creditsGroup = Undo.AddComponent<CanvasGroup>(creditsGroupGO);
            creditsGroup.alpha = 0f;

            var creditsTextGO = new GameObject("CreditsText");
            creditsTextGO.transform.SetParent(creditsGroupGO.transform, worldPositionStays: false);
            var crTextRect = Undo.AddComponent<RectTransform>(creditsTextGO);
            FullStretch(crTextRect);
            var creditsText = Undo.AddComponent<TextMeshProUGUI>(creditsTextGO);
            creditsText.text = "";
            creditsText.fontSize = 28;
            creditsText.color = Color.black;
            creditsText.alignment = TextAlignmentOptions.Center;
            creditsText.enableWordWrapping = true;

            // --- Sting Panel
            var stingGroupGO = new GameObject("StingGroup");
            stingGroupGO.transform.SetParent(canvasGO.transform, worldPositionStays: false);
            var stRect = Undo.AddComponent<RectTransform>(stingGroupGO);
            stRect.anchorMin = new Vector2(0.3f, 0.3f);
            stRect.anchorMax = new Vector2(0.7f, 0.7f);
            stRect.offsetMin = Vector2.zero;
            stRect.offsetMax = Vector2.zero;
            var stingGroup = Undo.AddComponent<CanvasGroup>(stingGroupGO);
            stingGroup.alpha = 0f;

            var stingCursorGO = new GameObject("StingCursor");
            stingCursorGO.transform.SetParent(stingGroupGO.transform, worldPositionStays: false);
            var stCursRect = Undo.AddComponent<RectTransform>(stingCursorGO);
            stCursRect.anchorMin = new Vector2(0f, 0.55f);
            stCursRect.anchorMax = new Vector2(1f, 0.95f);
            stCursRect.offsetMin = Vector2.zero;
            stCursRect.offsetMax = Vector2.zero;
            var stingCursor = Undo.AddComponent<TextMeshProUGUI>(stingCursorGO);
            stingCursor.text = "> _";
            stingCursor.fontSize = 72;
            stingCursor.color = Color.black;
            stingCursor.alignment = TextAlignmentOptions.Center;

            var stingTagGO = new GameObject("StingTagline");
            stingTagGO.transform.SetParent(stingGroupGO.transform, worldPositionStays: false);
            var stTagRect = Undo.AddComponent<RectTransform>(stingTagGO);
            stTagRect.anchorMin = new Vector2(0f, 0.15f);
            stTagRect.anchorMax = new Vector2(1f, 0.45f);
            stTagRect.offsetMin = Vector2.zero;
            stTagRect.offsetMax = Vector2.zero;
            var stingTag = Undo.AddComponent<TextMeshProUGUI>(stingTagGO);
            stingTag.text = "";
            stingTag.fontSize = 36;
            stingTag.color = Color.black;
            stingTag.alignment = TextAlignmentOptions.Center;

            // CreditsSequence on root + wire references
            var seq = Undo.AddComponent<CreditsSequence>(root);
            var so = new SerializedObject(seq);
            Set(so, "finalTextGroup", finalTextGroup);
            Set(so, "finalText", finalText);
            Set(so, "creditsGroup", creditsGroup);
            Set(so, "creditsText", creditsText);
            Set(so, "stingGroup", stingGroup);
            Set(so, "stingCursor", stingCursor);
            Set(so, "stingTagline", stingTag);
            Set(so, "backgroundImage", bgImg);
            so.ApplyModifiedPropertiesWithoutUndo();

            // EventSystem
            if (GameObject.Find("EventSystem") == null)
            {
                var esGO = new GameObject("EventSystem");
                Undo.AddComponent<UnityEngine.EventSystems.EventSystem>(esGO);
                Undo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>(esGO);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            if (saved) Debug.Log("[CreditsDresser] Scene_Credits saved with full UI hierarchy");
            else Debug.LogError("[CreditsDresser] Failed to save Scene_Credits");

            AssetDatabase.SaveAssets();
        }

        private static void Set(SerializedObject so, string prop, Object value)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.objectReferenceValue = value;
        }

        private static void FullStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
