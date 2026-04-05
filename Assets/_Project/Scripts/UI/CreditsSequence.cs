using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Afterhumans.Scenes;

namespace Afterhumans.UI
{
    /// <summary>
    /// Final credits scene for Episode 0.
    /// Plays final text reveal (based on cursor input chosen), then credits, then sting.
    /// Structure:
    ///   1. Black screen, final text fade in based on GameStateManager.cursorInput
    ///   2. Hold 5-8 seconds
    ///   3. Fade to white, credits list fade in
    ///   4. Credits hold
    ///   5. White screen, small `> _` cursor in center
    ///   6. "Продолжение следует." fades in
    ///   7. Fade to black, return to main menu
    /// </summary>
    public class CreditsSequence : MonoBehaviour
    {
        [Header("Final Text Panel")]
        [SerializeField] private CanvasGroup finalTextGroup;
        [SerializeField] private TextMeshProUGUI finalText;

        [Header("Credits Panel")]
        [SerializeField] private CanvasGroup creditsGroup;
        [SerializeField] private TextMeshProUGUI creditsText;

        [Header("Sting Panel")]
        [SerializeField] private CanvasGroup stingGroup;
        [SerializeField] private TextMeshProUGUI stingCursor;
        [SerializeField] private TextMeshProUGUI stingTagline;

        [Header("Background")]
        [SerializeField] private Image backgroundImage;

        [Header("Audio")]
        [SerializeField] private AudioSource creditsMusic;

        [Header("Timing")]
        [SerializeField] private float fadeDuration = 2f;
        [SerializeField] private float finalTextHold = 6f;
        [SerializeField] private float creditsHold = 8f;
        [SerializeField] private float stingHold = 4f;

        private static readonly string[] FinalTexts = {
            // 0 = empty (shouldn't happen)
            "",
            // 1 = помоги
            "Прогноз получил запрос, которого не было в его таблицах за сорок семь лет работы.\nОн впервые не знает следующего шага.\nОн начинает искать ответ.\n\nКонец Episode 0.",
            // 2 = хватит
            "Прогноз услышал.\nОн не остановится — он просто функция, а функции не остановимы.\nНо впервые за сорок семь лет он колеблется.\n\nКонец Episode 0.",
            // 3 = не знаю (canonical)
            "Прогноз получил самый честный input за сорок семь лет.\nОн не знает что с ним делать.\nВозможно, именно это ему нужно было.\n\nОн впервые замечает собаку рядом с тобой.\nОна была там все шесть лет, но он её не учитывал — корреляции собак слишком нестабильны.\nТеперь он смотрит на неё внимательно.\nМодель впервые начинает учитывать переменные, которые нельзя измерить.\n\nКонец Episode 0.",
            // 4 = я
            "Первое новое слово в модели мира.\nПрогноз начинает обновляться.\nМодель не рассчитана на субъекта.\n\nКонец Episode 0.",
            // 5 = продолжай
            "Прогноз расширяет тензор.\nТы больше не тест-кейс.\nТы — часть функции.\n\nКонец Episode 0.",
        };

        private const string CreditsBody =
            "ПОСЛЕЛЮДИ / AFTERHUMANS\n" +
            "Episode 0\n\n" +
            "Идея, сюжет, диалоги: Тим Зинин\n" +
            "Разработка: Тим Зинин + Claude Code\n" +
            "Саунд-дизайн и озвучка: Денис Говорунов\n\n" +
            "В роли Кафки: Кафка, 6 лет, корги-кардиган\n\n" +
            "Сделано в Ботанике\n" +
            "Для тех, кого Прогноз пока не учитывает";

        private const string StingTagline = "Продолжение следует.";

        private void Start()
        {
            StartCoroutine(PlaySequence());
        }

        private IEnumerator PlaySequence()
        {
            // Initial state: all hidden, black background
            SetGroupAlpha(finalTextGroup, 0f);
            SetGroupAlpha(creditsGroup, 0f);
            SetGroupAlpha(stingGroup, 0f);
            if (backgroundImage != null) backgroundImage.color = Color.black;

            // Pick final text based on cursor input
            int input = GameStateManager.Instance != null ? GameStateManager.Instance.data.cursorInput : 3;
            if (input < 1 || input >= FinalTexts.Length) input = 3;
            if (finalText != null) finalText.text = FinalTexts[input];

            if (creditsMusic != null) creditsMusic.Play();

            // 1. Fade in final text
            yield return FadeGroup(finalTextGroup, 0f, 1f, fadeDuration);
            yield return new WaitForSeconds(finalTextHold);
            yield return FadeGroup(finalTextGroup, 1f, 0f, fadeDuration);

            // 2. Fade to white
            if (backgroundImage != null)
            {
                float t = 0;
                Color from = Color.black;
                Color to = Color.white;
                while (t < fadeDuration)
                {
                    t += Time.deltaTime;
                    backgroundImage.color = Color.Lerp(from, to, t / fadeDuration);
                    yield return null;
                }
            }

            // 3. Credits
            if (creditsText != null) creditsText.text = CreditsBody;
            yield return FadeGroup(creditsGroup, 0f, 1f, fadeDuration);
            yield return new WaitForSeconds(creditsHold);
            yield return FadeGroup(creditsGroup, 1f, 0f, fadeDuration);

            // 4. Sting
            if (stingCursor != null) stingCursor.text = "> _";
            if (stingTagline != null) stingTagline.text = StingTagline;
            yield return FadeGroup(stingGroup, 0f, 1f, fadeDuration);
            yield return new WaitForSeconds(stingHold);
            yield return FadeGroup(stingGroup, 1f, 0f, fadeDuration);

            // 5. Fade to black and return to menu
            if (backgroundImage != null)
            {
                float t = 0;
                Color from = Color.white;
                Color to = Color.black;
                while (t < fadeDuration)
                {
                    t += Time.deltaTime;
                    backgroundImage.color = Color.Lerp(from, to, t / fadeDuration);
                    yield return null;
                }
            }

            SceneTransition.Instance?.LoadScene("Scene_MainMenu");
        }

        private IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration)
        {
            if (group == null) yield break;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                group.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            group.alpha = to;
        }

        private static void SetGroupAlpha(CanvasGroup g, float a)
        {
            if (g != null) g.alpha = a;
        }
    }
}
