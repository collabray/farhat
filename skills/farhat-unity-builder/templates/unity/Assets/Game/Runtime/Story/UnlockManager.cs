using System.Collections.Generic;
using Farhat.Core;
using Farhat.Save;

namespace Farhat.Story
{
    /// <summary>Fragments, abilities, recipes and cosmetics. Ids follow the dotted keys in data/chapters.json (fragment.4, meter.tier2, potion.cure).</summary>
    public sealed class UnlockManager
    {
        readonly HashSet<string> unlocked = new HashSet<string>();

        public int FragmentCount
        {
            get
            {
                int n = 0;
                for (int i = 1; i <= 7; i++)
                {
                    if (unlocked.Contains("fragment." + i)) n++;
                }
                return n;
            }
        }

        /// <summary>Extra hearts from ids of the form hearts.plus1.chNN.</summary>
        public int BonusHearts
        {
            get
            {
                int n = 0;
                foreach (string id in unlocked)
                {
                    if (id.StartsWith("hearts.plus1.")) n++;
                }
                return n;
            }
        }

        public bool Has(string id) { return unlocked.Contains(id); }

        public bool Grant(string id)
        {
            if (string.IsNullOrEmpty(id) || !unlocked.Add(id)) return false;
            EventBus<UnlockGranted>.Publish(new UnlockGranted(id));
            if (id.StartsWith("fragment."))
            {
                int index;
                if (int.TryParse(id.Substring("fragment.".Length), out index))
                {
                    EventBus<FragmentCollected>.Publish(new FragmentCollected(index));
                }
            }
            return true;
        }

        public void LoadFrom(SaveData data)
        {
            unlocked.Clear();
            for (int i = 0; i < data.unlocks.Count; i++) unlocked.Add(data.unlocks[i]);
            for (int i = 1; i <= 7; i++)
            {
                if (data.HasFragment(i)) unlocked.Add("fragment." + i);
            }
        }

        public void WriteTo(SaveData data)
        {
            data.unlocks.Clear();
            data.fragmentMask = 0;
            foreach (string id in unlocked)
            {
                if (id.StartsWith("fragment."))
                {
                    int index;
                    if (int.TryParse(id.Substring("fragment.".Length), out index)) data.SetFragment(index);
                }
                else
                {
                    data.unlocks.Add(id);
                }
            }
            data.unlocks.Sort(string.CompareOrdinal);
        }
    }
}
