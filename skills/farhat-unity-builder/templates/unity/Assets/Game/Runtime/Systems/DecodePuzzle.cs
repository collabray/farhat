using System;
using UnityEngine;
using Farhat.Core;
using Farhat.Data;
using Farhat.Story;

namespace Farhat.Systems
{
    /// <summary>
    /// Tilda's decoding puzzle (bible 6.6): enter the symbols of a map or inscription in order. A wrong symbol resets the progress,
    /// never the player's health. After enough failures a hint becomes available. Solving completes the linked objective.
    /// </summary>
    public class DecodePuzzle : MonoBehaviour
    {
        [SerializeField] string[] solution = new string[0];
        [SerializeField] string objectiveId;
        [SerializeField] int failuresBeforeHint = 3;

        int progress;
        int failures;

        public event Action Solved;
        public event Action<int> Progressed;
        public event Action Failed;

        public bool IsSolved { get; private set; }
        public int Progress { get { return progress; } }
        public int Length { get { return solution.Length; } }
        public int Failures { get { return failures; } }
        public bool HintAvailable { get { return !IsSolved && failures >= failuresBeforeHint; } }

        public void Configure(string[] symbols, string objective)
        {
            solution = symbols;
            objectiveId = objective;
            ResetPuzzle();
        }

        /// <summary>The next symbol the player should enter, available only when the hint threshold is reached.</summary>
        public string Hint()
        {
            return HintAvailable && progress < solution.Length ? solution[progress] : null;
        }

        public bool TryInput(string symbol)
        {
            if (IsSolved || solution.Length == 0) return false;
            if (symbol == solution[progress])
            {
                progress++;
                Action<int> p = Progressed;
                if (p != null) p(progress);
                if (progress >= solution.Length) Complete();
                return true;
            }
            failures++;
            progress = 0;
            Action f = Failed;
            if (f != null) f();
            return false;
        }

        public void ResetPuzzle()
        {
            progress = 0;
            failures = 0;
            IsSolved = false;
        }

        void Complete()
        {
            IsSolved = true;
            ObjectiveTracker tracker;
            if (!string.IsNullOrEmpty(objectiveId) && ServiceLocator.TryGet(out tracker)) tracker.Complete(objectiveId);
            Action s = Solved;
            if (s != null) s();
        }
    }
}
