using System;
using System.Collections.Generic;
using UnityEngine;
using Farhat.Core;
using Farhat.Data;
using Farhat.Systems;

namespace Farhat.Story
{
    /// <summary>Tracks the three-tier goal list of the running chapter and publishes completion events.</summary>
    public sealed class ObjectiveTracker : ISnapshotParticipant
    {
        readonly HashSet<string> done = new HashSet<string>();
        ChapterDefinition chapter;
        bool goalsMetPublished;

        [Serializable] class State { public List<string> done = new List<string>(); }

        public string SnapshotKey { get { return "objectives"; } }
        public ChapterDefinition Chapter { get { return chapter; } }
        public bool AllGoalsMet { get; private set; }

        public void Initialize(ChapterDefinition definition, IEnumerable<string> alreadyDone)
        {
            chapter = definition;
            done.Clear();
            goalsMetPublished = false;
            AllGoalsMet = false;
            if (alreadyDone != null)
            {
                foreach (string id in alreadyDone)
                {
                    if (definition.GetObjective(id) != null) done.Add(id);
                }
            }
            EvaluateGoals(false);
        }

        public bool IsComplete(string id) { return done.Contains(id); }
        public IEnumerable<string> Completed { get { return done; } }

        public bool Complete(string id)
        {
            if (chapter == null || chapter.GetObjective(id) == null)
            {
                Debug.LogWarning("[Objective] Unknown objective '" + id + "' for the running chapter.");
                return false;
            }
            if (!done.Add(id)) return false;
            EventBus<ObjectiveCompleted>.Publish(new ObjectiveCompleted(id));
            EvaluateGoals(true);
            return true;
        }

        /// <summary>The first incomplete tier-1 objective, used by hints. Null when all are complete.</summary>
        public string NextIncompleteGoal()
        {
            if (chapter == null) return null;
            for (int i = 0; i < chapter.objectives.Count; i++)
            {
                ObjectiveDef o = chapter.objectives[i];
                if (o.tier == 1 && !done.Contains(o.id)) return o.id;
            }
            return null;
        }

        void EvaluateGoals(bool publish)
        {
            bool all = true;
            bool any = false;
            for (int i = 0; i < chapter.objectives.Count; i++)
            {
                if (chapter.objectives[i].tier != 1) continue;
                any = true;
                if (!done.Contains(chapter.objectives[i].id)) { all = false; break; }
            }
            AllGoalsMet = any ? all : true;
            if (AllGoalsMet && publish && !goalsMetPublished)
            {
                goalsMetPublished = true;
                EventBus<ChapterGoalsMet>.Publish(new ChapterGoalsMet(chapter.number));
            }
        }

        public string CaptureSnapshot()
        {
            State s = new State();
            s.done.AddRange(done);
            return JsonUtility.ToJson(s);
        }

        public void RestoreSnapshot(string json)
        {
            State s = JsonUtility.FromJson<State>(json);
            done.Clear();
            for (int i = 0; i < s.done.Count; i++) done.Add(s.done[i]);
            goalsMetPublished = false;
            EvaluateGoals(false);
            goalsMetPublished = AllGoalsMet;
        }
    }
}
