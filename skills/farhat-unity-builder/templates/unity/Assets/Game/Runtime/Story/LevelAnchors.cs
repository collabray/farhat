using System.Collections.Generic;
using UnityEngine;

namespace Farhat.Story
{
    /// <summary>Where the player appears when a chapter scene loads. One per chapter scene.</summary>
    public class PlayerStart : MonoBehaviour
    {
        static readonly List<PlayerStart> all = new List<PlayerStart>();
        public static IList<PlayerStart> All { get { return all; } }

        void OnEnable() { all.Add(this); }
        void OnDisable() { all.Remove(this); }
    }

    /// <summary>A named spawn point for a checkpoint id (start, mid, pre-boss). Used when a saved game resumes inside a chapter.</summary>
    public class CheckpointAnchor : MonoBehaviour
    {
        static readonly List<CheckpointAnchor> all = new List<CheckpointAnchor>();
        [SerializeField] string checkpointId = "start";

        public string CheckpointId { get { return checkpointId; } set { checkpointId = value; } }

        void OnEnable() { all.Add(this); }
        void OnDisable() { all.Remove(this); }

        public static CheckpointAnchor Find(string id)
        {
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].checkpointId == id) return all[i];
            }
            return null;
        }
    }
}
