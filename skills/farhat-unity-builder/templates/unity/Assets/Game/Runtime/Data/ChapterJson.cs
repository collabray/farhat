using System;
using System.Collections.Generic;

namespace Farhat.Data
{
    // Data-transfer classes that mirror data/chapters.json. JsonUtility ignores fields it does not know (such as "spec").
    [Serializable]
    public class ObjectiveRecord
    {
        public string id;
        public int tier;
        public string text;
    }

    [Serializable]
    public class FailureRuleRecord
    {
        public string type;
        public string checkpointId;
        public bool lockShadowNextArea;
        public int resourceLoss;
        public int hintAfterRetries;
    }

    [Serializable]
    public class ChapterRecord
    {
        public int number;
        public string id;
        public string episodeTitle;
        public int season;
        public int part;
        public string sourceTag;
        public string bibleTag;
        public string costTier;
        public string chapterType;
        public string location;
        public string coreMechanic;
        public string setPiece;
        public string storyBeats;
        public string sceneName;
        public string locPrefix;
        public List<ObjectiveRecord> objectives;
        public List<string> checkpointIds;
        public List<FailureRuleRecord> failureRules;
        public List<string> unlocks;
    }

    [Serializable]
    public class ChapterFile
    {
        public int schemaVersion;
        public List<ChapterRecord> chapters;
    }
}
