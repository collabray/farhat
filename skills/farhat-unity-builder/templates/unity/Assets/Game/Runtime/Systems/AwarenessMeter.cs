using System;
using UnityEngine;
using Farhat.Core;
using Farhat.Data;
using Farhat.Story;

namespace Farhat.Systems
{
    /// <summary>
    /// Cover-up and disguise meter (chapters 8, 11, 29). Rises with player noise, visible magic and combat in the watcher's sight and
    /// decays when nothing is reported. A full meter publishes PlayerFailed(Stealth) once.
    /// </summary>
    public class AwarenessMeter : MonoBehaviour, ISnapshotParticipant
    {
        [SerializeField] float noiseRatePerSecond = 0.25f;
        [SerializeField] float decayPerSecond = 0.12f;
        [SerializeField] float publishThreshold = 0.01f;

        float value;
        float reportedThisFrame;
        float lastPublished;
        bool failed;

        [Serializable] class State { public float value; }

        public string SnapshotKey { get { return "awareness"; } }
        public float Value { get { return value; } }

        void OnEnable()
        {
            EventBus<PlayerRestored>.Subscribe(OnRestored);
            CheckpointStore store;
            if (ServiceLocator.TryGet(out store)) store.Register(this);
        }

        void OnDisable()
        {
            EventBus<PlayerRestored>.Unsubscribe(OnRestored);
            CheckpointStore store;
            if (ServiceLocator.TryGet(out store)) store.Unregister(this);
        }

        void OnRestored(PlayerRestored e)
        {
            failed = false;
        }

        /// <summary>Amount is per second at full intensity. Sensors call this every frame while they perceive something.</summary>
        public void ReportPerceived(float ratePerSecond)
        {
            reportedThisFrame += Mathf.Max(0f, ratePerSecond);
        }

        public void ReportNoise(float noiseLevel)
        {
            ReportPerceived(noiseLevel * noiseRatePerSecond);
        }

        void Update()
        {
            if (Time.timeScale <= 0f) return;
            float dt = Time.deltaTime;
            if (reportedThisFrame > 0f) value += reportedThisFrame * dt;
            else value -= decayPerSecond * dt;
            reportedThisFrame = 0f;
            value = Mathf.Clamp01(value);
            if (Mathf.Abs(value - lastPublished) >= publishThreshold)
            {
                lastPublished = value;
                EventBus<AwarenessChanged>.Publish(new AwarenessChanged(value));
            }
            if (value >= 1f && !failed)
            {
                failed = true;
                EventBus<PlayerFailed>.Publish(new PlayerFailed(FailureType.Stealth));
            }
        }

        public string CaptureSnapshot()
        {
            State s = new State();
            s.value = value;
            return JsonUtility.ToJson(s);
        }

        public void RestoreSnapshot(string json)
        {
            // A restore always returns the watcher to calm, whatever the captured value was.
            value = 0f;
            lastPublished = 0f;
            failed = false;
        }
    }
}
