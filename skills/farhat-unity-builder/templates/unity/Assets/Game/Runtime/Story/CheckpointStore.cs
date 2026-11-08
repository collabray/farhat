using System.Collections.Generic;
using UnityEngine;
using Farhat.Core;
using Farhat.Systems;

namespace Farhat.Story
{
    /// <summary>
    /// Data-driven checkpoints. Every system that must survive a restore implements ISnapshotParticipant and registers here.
    /// Record captures all participants under a checkpoint id; Restore applies them back, so a restore equals the saved snapshot.
    /// </summary>
    public sealed class CheckpointStore
    {
        readonly List<ISnapshotParticipant> participants = new List<ISnapshotParticipant>();
        readonly Dictionary<string, Dictionary<string, string>> snapshots = new Dictionary<string, Dictionary<string, string>>();

        public string LastId { get; private set; }
        public int ParticipantCount { get { return participants.Count; } }

        public void Register(ISnapshotParticipant participant)
        {
            if (!participants.Contains(participant)) participants.Add(participant);
        }

        public void Unregister(ISnapshotParticipant participant)
        {
            participants.Remove(participant);
        }

        public bool Has(string id) { return !string.IsNullOrEmpty(id) && snapshots.ContainsKey(id); }

        public void Record(string id)
        {
            Dictionary<string, string> snapshot = new Dictionary<string, string>();
            for (int i = 0; i < participants.Count; i++)
            {
                snapshot[participants[i].SnapshotKey] = participants[i].CaptureSnapshot();
            }
            snapshots[id] = snapshot;
            LastId = id;
            EventBus<CheckpointReached>.Publish(new CheckpointReached(id));
        }

        public bool Restore(string id)
        {
            Dictionary<string, string> snapshot;
            if (!snapshots.TryGetValue(id, out snapshot)) return false;
            ISnapshotParticipant[] copy = participants.ToArray();
            for (int i = 0; i < copy.Length; i++)
            {
                string json;
                if (snapshot.TryGetValue(copy[i].SnapshotKey, out json)) copy[i].RestoreSnapshot(json);
            }
            LastId = id;
            return true;
        }

        public void Clear()
        {
            snapshots.Clear();
            LastId = null;
        }
    }
}
