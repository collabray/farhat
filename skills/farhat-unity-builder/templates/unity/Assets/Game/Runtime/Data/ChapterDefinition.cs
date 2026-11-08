using System;
using System.Collections.Generic;
using UnityEngine;

namespace Farhat.Data
{
    [Serializable]
    public class ObjectiveDef
    {
        public string id;      // stable key such as ch05.rescue-shadow, used by events and saves
        public string text;    // fallback display text; the localised string is looked up by id
        public int tier;       // 1 chapter goal, 2 season goal, 3 final goal
    }

    [Serializable]
    public class FailureRule
    {
        public FailureType type;
        public string checkpointId;       // where to return; empty means the last recorded checkpoint
        public bool lockShadowNextArea;   // impulse-trap penalty
        public int resourceLoss;          // herbs or potions lost
        public int hintAfterRetries = 2;  // offer a hint after N failures
    }

    /// <summary>One asset per episode. The single source of truth that connects the bible to the build.</summary>
    [CreateAssetMenu(menuName = "Farhat/Chapter Definition")]
    public class ChapterDefinition : ScriptableObject
    {
        public int number;
        public string id;
        public string episodeTitle;
        public int season;
        public int part;
        public SourceTag sourceTag;
        public string bibleTag;
        public CostTier costTier;
        public ChapterType chapterType;
        public string location;
        public string coreMechanic;
        public string setPiece;
        [TextArea(3, 12)] public string storyBeats;
        public string sceneName;
        public string locPrefix;
        public bool includedInBuild = true;
        public List<ObjectiveDef> objectives = new List<ObjectiveDef>();
        public List<string> checkpointIds = new List<string>();
        public List<FailureRule> failureRules = new List<FailureRule>();
        public List<string> unlocks = new List<string>();

        public FailureRule GetRule(FailureType type)
        {
            for (int i = 0; i < failureRules.Count; i++)
            {
                if (failureRules[i].type == type) return failureRules[i];
            }
            return null;
        }

        public bool HasCheckpoint(string checkpointId)
        {
            return checkpointIds.Contains(checkpointId);
        }

        public ObjectiveDef GetObjective(string objectiveId)
        {
            for (int i = 0; i < objectives.Count; i++)
            {
                if (objectives[i].id == objectiveId) return objectives[i];
            }
            return null;
        }

        public int CountObjectives(int tier)
        {
            int n = 0;
            for (int i = 0; i < objectives.Count; i++)
            {
                if (objectives[i].tier == tier) n++;
            }
            return n;
        }
    }
}
