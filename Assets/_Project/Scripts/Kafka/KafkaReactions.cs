using UnityEngine;
using Afterhumans.Dialogue;

namespace Afterhumans.Kafka
{
    /// <summary>
    /// Handles E-press reactions on Kafka.
    /// Random contextual response (wag tail, sniff, lie down, bark) depending on location.
    /// Dispatches subtitle text via DialogueManager for consistency.
    /// Attach to Kafka GameObject alongside KafkaFollow and an Interactable.
    /// </summary>
    public class KafkaReactions : MonoBehaviour
    {
        [Header("Location Context")]
        [Tooltip("Current scene context: 0=Botanika, 1=City, 2=Desert, 3=Cursor")]
        public int locationContext = 0;

        [Header("Animator")]
        [SerializeField] private Animator animator;
        [SerializeField] private string sitTrigger = "Sit";
        [SerializeField] private string lieTrigger = "LieDown";
        [SerializeField] private string barkTrigger = "Bark";
        [SerializeField] private string sniffTrigger = "Sniff";

        private static readonly string[] BotanikaReactions = {
            "Кафка виляет хвостом, смотрит на тебя внимательно.",
            "Кафка тыкает тебя носом в руку.",
            "Кафка тихо фыркает.",
            "Кафка ложится на бок и предлагает живот.",
            "Кафка коротко лает и убегает к ближайшему NPC.",
            "Кафка чихает. Дважды.",
            "Кафка смотрит на тебя, потом в сторону. Ты не понимаешь, на что она смотрит. Она снова смотрит на тебя.",
            "Кафка зевает, показывая все зубы, потом виляет хвостом.",
        };

        private static readonly string[] CityReactions = {
            "Кафка идёт рядом, иногда оглядывается. Ей не нравится это место, но она не отходит от тебя.",
            "Кафка принюхивается к воздуху и чихает. Пахнет чем-то чистым и ненастоящим.",
            "Кафка смотрит на downgraded-humans с интересом, как на незнакомый вид животных.",
            "Кафка тыкается тебе в колено. Проверяет, всё ли с тобой в порядке.",
        };

        private static readonly string[] DesertReactions = {
            "Кафка бежит впереди, оставляет следы на песке. Возвращается.",
            "Кафка садится, смотрит на горизонт. Ты тоже смотришь.",
            "Кафка принюхивается к ветру. В пустыне пахнет не как в Ботанике. И не как в городе. Она не знает, как именно.",
            "Кафка встряхивается всем телом — песок разлетается вокруг.",
        };

        private static readonly string[] CursorReactions = {
            "Кафка садится у твоих ног. Смотрит на курсор. Не двигается. Она готова.",
        };

        public void PlayRandomReaction()
        {
            string[] pool = locationContext switch
            {
                0 => BotanikaReactions,
                1 => CityReactions,
                2 => DesertReactions,
                3 => CursorReactions,
                _ => BotanikaReactions,
            };

            string line = pool[Random.Range(0, pool.Length)];
            DisplaySubtitle(line);
            TriggerRandomAnimation();
        }

        private void DisplaySubtitle(string text)
        {
            // Hook into DialogueManager line event so subtitles appear in the same UI
            DialogueManager.Instance?.EmitLine($"<i>{text}</i>");
        }

        private void TriggerRandomAnimation()
        {
            if (animator == null) return;
            string[] triggers = { sitTrigger, sniffTrigger, lieTrigger, barkTrigger };
            animator.SetTrigger(triggers[Random.Range(0, triggers.Length)]);
        }
    }
}
