using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Farhat.Core;
using Farhat.Story;

namespace Farhat.UI
{
    /// <summary>Shows the episode title at the start of each chapter, as in the series' title cards.</summary>
    public class ChapterTitleCard : MonoBehaviour
    {
        [SerializeField] Text numberLabel;
        [SerializeField] Text titleLabel;
        [SerializeField] CanvasGroup group;
        [SerializeField] float showSeconds = 3f;
        [SerializeField] float fadeSeconds = 0.6f;

        void OnEnable() { EventBus<ChapterStarted>.Subscribe(OnChapterStarted); if (group != null) group.alpha = 0f; }
        void OnDisable() { EventBus<ChapterStarted>.Unsubscribe(OnChapterStarted); }

        void OnChapterStarted(ChapterStarted e)
        {
            ChapterRunner runner;
            if (!ServiceLocator.TryGet(out runner) || runner.Chapter == null || group == null) return;
            if (numberLabel != null) numberLabel.text = "Chapter " + runner.Chapter.number;
            if (titleLabel != null) titleLabel.text = runner.Chapter.episodeTitle;
            StopAllCoroutines();
            StartCoroutine(Show());
        }

        IEnumerator Show()
        {
            yield return Fade(1f);
            float t = 0f;
            while (t < showSeconds) { t += Time.unscaledDeltaTime; yield return null; }
            yield return Fade(0f);
        }

        IEnumerator Fade(float target)
        {
            float start = group.alpha;
            float t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(t / fadeSeconds));
                yield return null;
            }
            group.alpha = target;
        }
    }
}
