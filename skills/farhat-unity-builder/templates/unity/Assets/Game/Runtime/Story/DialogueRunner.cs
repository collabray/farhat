using System.Collections;
using UnityEngine;
using Farhat.Core;
using Farhat.Data;

namespace Farhat.Story
{
    /// <summary>Plays dialogue sequences line by line and publishes each line for the subtitle view and for voice playback.</summary>
    public class DialogueRunner : MonoBehaviour
    {
        Coroutine current;
        bool skipRequested;

        public bool IsPlaying { get; private set; }

        void OnEnable() { ServiceLocator.Register<DialogueRunner>(this); }
        void OnDisable() { ServiceLocator.Unregister<DialogueRunner>(); }

        public void Play(DialogueSequence sequence)
        {
            if (sequence == null) return;
            if (current != null) StopCoroutine(current);
            current = StartCoroutine(Run(sequence));
        }

        public void Skip() { skipRequested = true; }

        IEnumerator Run(DialogueSequence sequence)
        {
            IsPlaying = true;
            for (int i = 0; i < sequence.lines.Count; i++)
            {
                DialogueLine line = sequence.lines[i];
                skipRequested = false;
                EventBus<DialogueLineShown>.Publish(new DialogueLineShown(line.speakerId, line.locKey, line.fallbackText, line.tone));
                float t = 0f;
                while (t < line.seconds && !skipRequested)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            EventBus<DialogueLineShown>.Publish(new DialogueLineShown(string.Empty, string.Empty, string.Empty, DialogueTone.Neutral));
            IsPlaying = false;
            current = null;
        }
    }
}
