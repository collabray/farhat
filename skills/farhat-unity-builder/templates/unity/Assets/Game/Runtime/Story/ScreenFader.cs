using System.Collections;
using UnityEngine;

namespace Farhat.Story
{
    /// <summary>Full-screen black fade used for checkpoint restores and chapter changes. Uses unscaled time so it works while paused.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenFader : MonoBehaviour
    {
        CanvasGroup group;

        void Awake()
        {
            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        public IEnumerator FadeOut(float seconds) { return Fade(1f, seconds); }
        public IEnumerator FadeIn(float seconds) { return Fade(0f, seconds); }

        IEnumerator Fade(float target, float seconds)
        {
            float start = group.alpha;
            float t = 0f;
            group.blocksRaycasts = true;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(t / Mathf.Max(0.0001f, seconds)));
                yield return null;
            }
            group.alpha = target;
            group.blocksRaycasts = target > 0.5f;
        }
    }
}
