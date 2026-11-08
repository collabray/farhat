using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Farhat.Core;
using Farhat.Data;
using Farhat.Story;

namespace Farhat.UI
{
    /// <summary>Three-tier quest tracker: chapter goals, season goals and the final goal.</summary>
    public class QuestLogView : MonoBehaviour
    {
        [SerializeField] Text label;

        void OnEnable()
        {
            EventBus<ChapterStarted>.Subscribe(OnChapterStarted);
            EventBus<ObjectiveCompleted>.Subscribe(OnObjectiveCompleted);
            EventBus<PlayerRestored>.Subscribe(OnRestored);
        }

        void OnDisable()
        {
            EventBus<ChapterStarted>.Unsubscribe(OnChapterStarted);
            EventBus<ObjectiveCompleted>.Unsubscribe(OnObjectiveCompleted);
            EventBus<PlayerRestored>.Unsubscribe(OnRestored);
        }

        void OnChapterStarted(ChapterStarted e) { Refresh(); }
        void OnObjectiveCompleted(ObjectiveCompleted e) { Refresh(); }
        void OnRestored(PlayerRestored e) { Refresh(); }

        void Refresh()
        {
            ChapterRunner runner;
            if (label == null || !ServiceLocator.TryGet(out runner) || runner.Chapter == null || runner.Objectives == null) return;
            StringBuilder sb = new StringBuilder();
            string[] titles = { "Chapter", "Season", "Final" };
            for (int tier = 1; tier <= 3; tier++)
            {
                bool header = false;
                for (int i = 0; i < runner.Chapter.objectives.Count; i++)
                {
                    ObjectiveDef o = runner.Chapter.objectives[i];
                    if (o.tier != tier) continue;
                    if (!header) { sb.AppendLine(titles[tier - 1]); header = true; }
                    sb.Append(runner.Objectives.IsComplete(o.id) ? "[x] " : "[ ] ");
                    sb.AppendLine(o.text);
                }
            }
            label.text = sb.ToString();
        }
    }
}
