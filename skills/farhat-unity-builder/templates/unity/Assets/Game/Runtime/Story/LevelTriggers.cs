using System;
using System.Collections.Generic;
using UnityEngine;
using Farhat.Core;
using Farhat.Data;
using Farhat.Systems;

namespace Farhat.Story
{
    /// <summary>Base for trigger volumes that react to the player. Adds a trigger collider automatically.</summary>
    [RequireComponent(typeof(Collider))]
    public abstract class PlayerTrigger : MonoBehaviour
    {
        protected virtual void Reset()
        {
            Collider c = GetComponent<Collider>();
            if (c != null) c.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() != null) OnPlayerEntered();
        }

        protected abstract void OnPlayerEntered();
    }

    /// <summary>Completes a chapter objective when the player enters, once the prerequisite objectives are done.</summary>
    public class ObjectiveTrigger : PlayerTrigger
    {
        [SerializeField] string objectiveId;
        [SerializeField] string[] prerequisiteIds = new string[0];

        public string ObjectiveId { get { return objectiveId; } set { objectiveId = value; } }
        public string[] PrerequisiteIds { get { return prerequisiteIds; } set { prerequisiteIds = value; } }

        protected override void OnPlayerEntered()
        {
            ObjectiveTracker tracker;
            if (!ServiceLocator.TryGet<ObjectiveTracker>(out tracker)) tracker = RunnerTracker();
            if (tracker == null) return;
            for (int i = 0; i < prerequisiteIds.Length; i++)
            {
                if (!tracker.IsComplete(prerequisiteIds[i])) return;
            }
            tracker.Complete(objectiveId);
        }

        static ObjectiveTracker RunnerTracker()
        {
            ChapterRunner runner;
            return ServiceLocator.TryGet(out runner) ? runner.Objectives : null;
        }
    }

    /// <summary>Records a named checkpoint the first time the player enters.</summary>
    public class CheckpointTrigger : PlayerTrigger
    {
        [SerializeField] string checkpointId = "mid";
        public string CheckpointId { get { return checkpointId; } set { checkpointId = value; } }

        protected override void OnPlayerEntered()
        {
            ChapterRunner runner;
            if (!ServiceLocator.TryGet(out runner) || runner.Checkpoints == null) return;
            if (runner.Checkpoints.Has(checkpointId)) return;
            runner.RecordCheckpoint(checkpointId);
        }
    }

    /// <summary>Ends the chapter when the player enters after every tier-1 goal is complete.</summary>
    public class ChapterExitTrigger : PlayerTrigger
    {
        protected override void OnPlayerEntered()
        {
            ChapterRunner runner;
            if (!ServiceLocator.TryGet(out runner) || !runner.IsRunning) return;
            if (runner.Objectives != null && runner.Objectives.AllGoalsMet) runner.CompleteChapter();
        }
    }

    /// <summary>A Shariman fragment. Grants fragment.N, optionally completes an objective, and hides itself.</summary>
    public class FragmentPickup : PlayerTrigger
    {
        [SerializeField] int fragmentIndex = 1;
        [SerializeField] string objectiveId;
        public int FragmentIndex { get { return fragmentIndex; } set { fragmentIndex = value; } }
        public string ObjectiveId { get { return objectiveId; } set { objectiveId = value; } }

        protected override void OnPlayerEntered()
        {
            ChapterRunner runner;
            if (!ServiceLocator.TryGet(out runner) || runner.Unlocks == null) return;
            runner.Unlocks.Grant("fragment." + fragmentIndex);
            if (!string.IsNullOrEmpty(objectiveId) && runner.Objectives != null) runner.Objectives.Complete(objectiveId);
            gameObject.SetActive(false);
        }
    }

    /// <summary>Completes an objective after N enemies are defeated or freed.</summary>
    public class ObjectiveOnEnemiesDefeated : MonoBehaviour
    {
        [SerializeField] string objectiveId;
        [SerializeField] int requiredCount = 3;
        int count;

        public string ObjectiveId { get { return objectiveId; } set { objectiveId = value; } }
        public int RequiredCount { get { return requiredCount; } set { requiredCount = value; } }

        void OnEnable() { EventBus<EnemyDefeated>.Subscribe(OnDefeated); }
        void OnDisable() { EventBus<EnemyDefeated>.Unsubscribe(OnDefeated); }

        void OnDefeated(EnemyDefeated e)
        {
            count++;
            if (count < requiredCount) return;
            ChapterRunner runner;
            if (ServiceLocator.TryGet(out runner) && runner.Objectives != null) runner.Objectives.Complete(objectiveId);
            enabled = false;
        }
    }

    /// <summary>Completes an objective when a ShadowGap opens.</summary>
    public class ObjectiveOnGapOpened : MonoBehaviour
    {
        [SerializeField] ShadowGap gap;
        [SerializeField] string objectiveId;

        void OnEnable() { if (gap != null) gap.Opened += OnOpened; }
        void OnDisable() { if (gap != null) gap.Opened -= OnOpened; }

        void OnOpened()
        {
            ChapterRunner runner;
            if (ServiceLocator.TryGet(out runner) && runner.Objectives != null) runner.Objectives.Complete(objectiveId);
        }
    }

    /// <summary>Starts a countdown (for example the Shadow's dissolve meter in chapter 5). Expiry is an objective failure that the fail-forward manager handles.</summary>
    public class ObjectiveTimer : MonoBehaviour
    {
        [SerializeField] string objectiveId;
        [SerializeField] float seconds = 90f;
        [SerializeField] bool startOnChapterStart = true;
        [SerializeField] bool restartOnRestore = true;

        float remaining;
        bool running;

        public float Remaining { get { return remaining; } }
        public float Normalised { get { return seconds <= 0f ? 0f : remaining / seconds; } }
        public bool IsRunning { get { return running; } }

        void OnEnable()
        {
            EventBus<ChapterStarted>.Subscribe(OnChapterStarted);
            EventBus<PlayerRestored>.Subscribe(OnRestored);
            EventBus<ObjectiveCompleted>.Subscribe(OnObjectiveCompleted);
        }

        void OnDisable()
        {
            EventBus<ChapterStarted>.Unsubscribe(OnChapterStarted);
            EventBus<PlayerRestored>.Unsubscribe(OnRestored);
            EventBus<ObjectiveCompleted>.Unsubscribe(OnObjectiveCompleted);
        }

        void OnChapterStarted(ChapterStarted e) { if (startOnChapterStart) StartTimer(); }
        void OnRestored(PlayerRestored e) { if (restartOnRestore && startOnChapterStart) StartTimer(); }
        void OnObjectiveCompleted(ObjectiveCompleted e) { if (e.Id == objectiveId) running = false; }

        public void StartTimer()
        {
            remaining = seconds;
            running = true;
        }

        void Update()
        {
            if (!running || Time.timeScale <= 0f) return;
            remaining -= Time.deltaTime;
            if (remaining > 0f) return;
            running = false;
            remaining = 0f;
            EventBus<PlayerFailed>.Publish(new PlayerFailed(FailureType.Objective));
        }
    }

    /// <summary>
    /// One option of a story-gated choice. The cautious option costs nothing. The impulsive option applies the ImpulseTrap rule's penalty
    /// (Shadow disabled for the area, lost materials), and the story continues the same way either way, as in the series.
    /// Both options complete the linked objective.
    /// </summary>
    public class ImpulseChoiceTrigger : PlayerTrigger
    {
        [SerializeField] bool impulsive;
        [SerializeField] string objectiveId;
        bool used;

        public bool Impulsive { get { return impulsive; } set { impulsive = value; } }
        public string ObjectiveId { get { return objectiveId; } set { objectiveId = value; } }

        protected override void OnPlayerEntered()
        {
            if (used) return;
            used = true;
            FailForwardManager manager;
            if (impulsive && ServiceLocator.TryGet(out manager)) manager.ApplyPenalty(FailureType.ImpulseTrap);
            ChapterRunner runner;
            if (ServiceLocator.TryGet(out runner) && runner.Objectives != null) runner.Objectives.Complete(objectiveId);
            ImpulseChoiceTrigger[] siblings = transform.parent != null ? transform.parent.GetComponentsInChildren<ImpulseChoiceTrigger>() : null;
            if (siblings != null)
            {
                for (int i = 0; i < siblings.Length; i++) siblings[i].used = true;
            }
        }
    }

    /// <summary>Completes an objective when the player channels a light ability near this object (freeing the charmed parents in chapter 26).</summary>
    public class ObjectiveOnLightUsed : MonoBehaviour
    {
        [SerializeField] string objectiveId;
        [SerializeField] LightAbility ability = LightAbility.Flash;
        [SerializeField] float radius = 8f;

        public string ObjectiveId { get { return objectiveId; } set { objectiveId = value; } }
        public float Radius { get { return radius; } set { radius = value; } }

        void OnEnable() { EventBus<LightAbilityUsed>.Subscribe(OnLight); }
        void OnDisable() { EventBus<LightAbilityUsed>.Unsubscribe(OnLight); }

        void OnLight(LightAbilityUsed e)
        {
            if (e.Ability != ability) return;
            if (Vector3.Distance(e.Position, transform.position) > radius) return;
            ChapterRunner runner;
            if (ServiceLocator.TryGet(out runner) && runner.Objectives != null && runner.Objectives.Complete(objectiveId)) enabled = false;
        }
    }

    /// <summary>Plays a dialogue sequence the first time the player enters.</summary>
    public class DialogueTrigger : PlayerTrigger
    {
        [SerializeField] DialogueSequence sequence;
        bool played;

        public DialogueSequence Sequence { get { return sequence; } set { sequence = value; } }

        protected override void OnPlayerEntered()
        {
            if (played || sequence == null) return;
            DialogueRunner runner;
            if (!ServiceLocator.TryGet(out runner)) return;
            played = true;
            runner.Play(sequence);
        }
    }
}
