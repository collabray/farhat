using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Farhat.Core;
using Farhat.Data;
using Farhat.Systems;

namespace Farhat.Story
{
    /// <summary>
    /// The only code that decides what failing means. Chapters declare FailureRules in their ChapterDefinition; this manager applies them:
    /// fade out, restore the checkpoint, apply the penalty, offer a hint after repeated failures, fade in. There is no game-over state.
    /// </summary>
    public class FailForwardManager : MonoBehaviour
    {
        [SerializeField] ChapterRunner runner;
        [SerializeField] ScreenFader fader;
        [SerializeField] float fadeSeconds = 0.4f;

        readonly Dictionary<FailureType, int> attempts = new Dictionary<FailureType, int>();
        bool restoring;

        public bool IsRestoring { get { return restoring; } }

        void OnEnable()
        {
            EventBus<PlayerFailed>.Subscribe(OnFailed);
            EventBus<ChapterStarted>.Subscribe(OnChapterStarted);
            ServiceLocator.Register<FailForwardManager>(this);
        }

        void OnDisable()
        {
            EventBus<PlayerFailed>.Unsubscribe(OnFailed);
            EventBus<ChapterStarted>.Unsubscribe(OnChapterStarted);
            ServiceLocator.Unregister<FailForwardManager>();
        }

        /// <summary>
        /// Applies only the penalty of a failure rule (Shadow lock, lost materials) without restoring a checkpoint. Used by story-gated choices such as
        /// the impulse trap in chapter 17, where the story continues as in the series whichever way the player chooses.
        /// </summary>
        public void ApplyPenalty(FailureType type)
        {
            if (runner == null || runner.Chapter == null) return;
            FailureRule rule = runner.Chapter.GetRule(type);
            if (rule == null) return;
            if (rule.lockShadowNextArea && runner.Shadow != null) runner.Shadow.LockForArea();
            if (rule.resourceLoss > 0 && runner.Inventory != null) runner.Inventory.Lose(rule.resourceLoss);
        }

        void OnChapterStarted(ChapterStarted e) { attempts.Clear(); }

        void OnFailed(PlayerFailed e)
        {
            if (restoring || runner == null || runner.Chapter == null) return;
            int count;
            attempts.TryGetValue(e.Type, out count);
            attempts[e.Type] = count + 1;
            StartCoroutine(Restore(e.Type, runner.Chapter.GetRule(e.Type), attempts[e.Type]));
        }

        IEnumerator Restore(FailureType type, FailureRule rule, int count)
        {
            restoring = true;
            if (fader != null) yield return fader.FadeOut(fadeSeconds);

            CheckpointStore store = runner.Checkpoints;
            string id = rule != null && store.Has(rule.checkpointId) ? rule.checkpointId : store.LastId;
            if (id == null || !store.Restore(id))
            {
                Debug.LogWarning("[FailForward] No checkpoint to restore for " + type + "; restarting the chapter start.");
                id = "start";
                store.Restore(id);
            }
            if (runner.PlayerHealth != null) runner.PlayerHealth.RefillAndRevive();

            if (rule != null)
            {
                if (rule.lockShadowNextArea && runner.Shadow != null) runner.Shadow.LockForArea();
                if (rule.resourceLoss > 0 && runner.Inventory != null) runner.Inventory.Lose(rule.resourceLoss);
                if (count >= rule.hintAfterRetries && runner.Hints != null) runner.Hints.Offer();
            }
            else if (count >= 2 && runner.Hints != null)
            {
                runner.Hints.Offer();
            }

            EventBus<PlayerRestored>.Publish(new PlayerRestored(id, count));
            if (fader != null) yield return fader.FadeIn(fadeSeconds);
            restoring = false;
        }
    }
}
