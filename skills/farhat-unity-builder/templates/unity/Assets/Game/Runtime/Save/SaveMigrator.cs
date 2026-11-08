using System;
using System.Collections.Generic;

namespace Farhat.Save
{
    /// <summary>
    /// A save made in any released version must load in every later version.
    /// Add one migration step per version bump and one test per step.
    /// </summary>
    public static class SaveMigrator
    {
        public const int Current = 3;

        public static SaveData Migrate(SaveData data)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (data.version < 2)
            {
                data.hearts = Math.Max(data.hearts, 5);
                data.version = 2;
            }
            if (data.version < 3)
            {
                if (data.completedObjectives == null) data.completedObjectives = new List<string>();
                if (data.chapterStars == null) data.chapterStars = new List<int>();
                data.version = 3;
            }
            if (data.unlocks == null) data.unlocks = new List<string>();
            if (data.completedObjectives == null) data.completedObjectives = new List<string>();
            if (data.chapterStars == null) data.chapterStars = new List<int>();
            return data;
        }
    }
}
