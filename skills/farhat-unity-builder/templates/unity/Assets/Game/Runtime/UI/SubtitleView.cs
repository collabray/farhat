using UnityEngine;
using UnityEngine.UI;
using Farhat.Core;
using Farhat.Save;

namespace Farhat.UI
{
    /// <summary>Subtitles with speaker names and an adjustable text size (accessibility, bible section 8).</summary>
    public class SubtitleView : MonoBehaviour
    {
        [SerializeField] Text label;
        [SerializeField] int baseFontSize = 28;

        void OnEnable() { EventBus<DialogueLineShown>.Subscribe(OnLine); Hide(); }
        void OnDisable() { EventBus<DialogueLineShown>.Unsubscribe(OnLine); }

        void OnLine(DialogueLineShown line)
        {
            if (label == null) return;
            if (!SettingsProfile.SubtitlesEnabled || string.IsNullOrEmpty(line.Text)) { Hide(); return; }
            label.fontSize = Mathf.RoundToInt(baseFontSize * SettingsProfile.SubtitleScale);
            label.text = string.IsNullOrEmpty(line.SpeakerId) ? line.Text : line.SpeakerId + ": " + line.Text;
            label.enabled = true;
        }

        void Hide()
        {
            if (label != null) { label.text = string.Empty; label.enabled = false; }
        }
    }
}
