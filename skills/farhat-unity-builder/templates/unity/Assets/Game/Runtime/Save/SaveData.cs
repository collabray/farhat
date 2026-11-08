using System;
using System.Collections.Generic;

namespace Farhat.Save
{
    [Serializable]
    public class SaveData
    {
        public int version = SaveMigrator.Current;
        public int chapter = 1;
        public string checkpointId = "start";
        public int fragmentMask;                 // one bit per fragment, bits 0 to 6 used
        public int hearts = 5;
        public List<string> unlocks = new List<string>();
        public List<string> completedObjectives = new List<string>();
        public List<int> chapterStars = new List<int>();   // index 0 is chapter 1, values 0 to 3
        public float playSeconds;

        public bool HasFragment(int index)
        {
            return index >= 1 && index <= 7 && (fragmentMask & (1 << (index - 1))) != 0;
        }

        public void SetFragment(int index)
        {
            if (index >= 1 && index <= 7) fragmentMask |= 1 << (index - 1);
        }

        public int FragmentCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < 7; i++)
                {
                    if ((fragmentMask & (1 << i)) != 0) n++;
                }
                return n;
            }
        }

        public void SetStars(int chapterNumber, int stars)
        {
            while (chapterStars.Count < chapterNumber) chapterStars.Add(0);
            int clamped = stars < 0 ? 0 : (stars > 3 ? 3 : stars);
            if (clamped > chapterStars[chapterNumber - 1]) chapterStars[chapterNumber - 1] = clamped;
        }
    }
}
