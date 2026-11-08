using Farhat.Core;

namespace Farhat.Story
{
    /// <summary>Offers a hint for the next unfinished chapter goal after repeated failures. The HUD shows it, and narration reads it when enabled.</summary>
    public sealed class HintService
    {
        readonly ObjectiveTracker tracker;
        public int HintsOffered { get; private set; }

        public HintService(ObjectiveTracker tracker)
        {
            this.tracker = tracker;
        }

        public void Offer()
        {
            string id = tracker.NextIncompleteGoal();
            if (id == null) return;
            HintsOffered++;
            EventBus<HintOffered>.Publish(new HintOffered(id));
        }
    }
}
