using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Afterhumans.UI;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Builds the Scene_MainMenu UI: title, subtitle, and three buttons
    /// (Start / Continue / Quit), wired to MainMenuController.
    /// </summary>
    public static class MenuDresser
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scene_MainMenu.unity";

        [MenuItem("Afterhumans/Setup/Dress Menu")]
        public static void Dress()
        {
            Debug.Log("[MenuDresser] Opening Scene_MainMenu...");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var existing = GameObject.Find("MenuRoot");
            if (existing != null) Object.DestroyImmediate(existing);

            var root = new GameObject("MenuRoot");

            // Canvas
            var canvasGO = new GameObject("Canvas");
            canvasGO.transform.SetParent(root.transform, worldPositionStays: false);
            var canvas = Undo.AddComponent<Canvas>(canvasGO);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Undo.AddComponent<CanvasScaler>(canvasGO).uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            Undo.AddComponent<GraphicRaycaster>(canvasGO);

            // Background — warm sunset gradient approximation (solid orange for now)
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(canvasGO.transform, worldPositionStays: false);
            var bgRect = Undo.AddComponent<RectTransform>(bgGO);
            FullStretch(bgRect);
            var bgImg = Undo.AddComponent<Image>(bgGO);
            bgImg.color = new Color(0.88f, 0.42f, 0.18f);
            bgImg.raycastTarget = false;

            // Title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(canvasGO.transform, worldPositionStays: false);
            var titleRect = Undo.AddComponent<RectTransform>(titleGO);
            titleRect.anchorMin = new Vector2(0f, 0.65f);
            titleRect.anchorMax = new Vector2(1f, 0.85f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            var titleText = Undo.AddComponent<TextMeshProUGUI>(titleGO);
            titleText.text = "ПОСЛЕЛЮДИ";
            titleText.fontSize = 120;
            titleText.color = new Color(1f, 0.94f, 0.85f);
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;

            // Subtitle
            var subGO = new GameObject("Subtitle");
            subGO.transform.SetParent(canvasGO.transform, worldPositionStays: false);
            var subRect = Undo.AddComponent<RectTransform>(subGO);
            subRect.anchorMin = new Vector2(0f, 0.55f);
            subRect.anchorMax = new Vector2(1f, 0.62f);
            subRect.offsetMin = Vector2.zero;
            subRect.offsetMax = Vector2.zero;
            var subText = Undo.AddComponent<TextMeshProUGUI>(subGO);
            subText.text = "Episode 0 — Баг в алгоритме";
            subText.fontSize = 32;
            subText.color = new Color(0.92f, 0.85f, 0.72f);
            subText.alignment = TextAlignmentOptions.Center;
            subText.fontStyle = FontStyles.Italic;

            // Buttons container
            var buttonsGO = new GameObject("Buttons");
            buttonsGO.transform.SetParent(canvasGO.transform, worldPositionStays: false);
            var btnsRect = Undo.AddComponent<RectTransform>(buttonsGO);
            btnsRect.anchorMin = new Vector2(0.35f, 0.15f);
            btnsRect.anchorMax = new Vector2(0.65f, 0.48f);
            btnsRect.offsetMin = Vector2.zero;
            btnsRect.offsetMax = Vector2.zero;
            var vlg = Undo.AddComponent<VerticalLayoutGroup>(buttonsGO);
            vlg.spacing = 20;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.MiddleCenter;

            var startButton = MakeButton(buttonsGO, "StartButton", "Начать");
            var continueButton = MakeButton(buttonsGO, "ContinueButton", "Продолжить");
            var quitButton = MakeButton(buttonsGO, "QuitButton", "Выход");

            // Controller + wire
            var controller = Undo.AddComponent<MainMenuController>(root);
            var so = new SerializedObject(controller);
            Set(so, "startButton", startButton);
            Set(so, "continueButton", continueButton);
            Set(so, "quitButton", quitButton);
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
            if (saved) Debug.Log("[MenuDresser] Scene_MainMenu saved");
            else Debug.LogError("[MenuDresser] Failed to save");

            AssetDatabase.SaveAssets();
        }

        private static Button MakeButton(GameObject parent, string name, string label)
        {
            var btnGO = new GameObject(name);
            btnGO.transform.SetParent(parent.transform, worldPositionStays: false);
            var rect = Undo.AddComponent<RectTransform>(btnGO);
            rect.sizeDelta = new Vector2(0, 80);
            var img = Undo.AddComponent<Image>(btnGO);
            img.color = new Color(0.20f, 0.10f, 0.05f, 0.85f);
            var btn = Undo.AddComponent<Button>(btnGO);
            btn.targetGraphic = img;

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(btnGO.transform, worldPositionStays: false);
            var lrect = Undo.AddComponent<RectTransform>(labelGO);
            lrect.anchorMin = Vector2.zero;
            lrect.anchorMax = Vector2.one;
            lrect.offsetMin = Vector2.zero;
            lrect.offsetMax = Vector2.zero;
            var ltext = Undo.AddComponent<TextMeshProUGUI>(labelGO);
            ltext.text = label;
            ltext.fontSize = 40;
            ltext.color = new Color(1f, 0.94f, 0.85f);
            ltext.alignment = TextAlignmentOptions.Center;

            return btn;
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
