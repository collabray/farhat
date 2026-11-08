using System;
using UnityEngine;
using Farhat.Core;
using Farhat.Data;
using Farhat.Save;
using Farhat.Story;

namespace Farhat.Systems
{
    /// <summary>Hearts of light. The player never dies: zero hearts publishes PlayerFailed(Health) and the fail-forward manager restores a checkpoint.</summary>
    public class HealthComponent : MonoBehaviour, IDamageable, ISnapshotParticipant
    {
        [SerializeField] int maxHearts = 5;
        [SerializeField] float invulnerabilitySeconds = 0.6f;

        float hearts;
        float invulnerableUntil;
        bool downed;
        CombatController combat;
        LightMeter lightMeter;

        public bool IsPlayer { get { return true; } }
        public float Hearts { get { return hearts; } }
        public int MaxHearts { get { return maxHearts; } }
        public bool IsInvulnerable { get { return Time.time < invulnerableUntil; } }
        public float HeartsLostThisChapter { get; private set; }

        [Serializable]
        class State { public float hearts; public int maxHearts; }

        public string SnapshotKey { get { return "health"; } }

        void Awake()
        {
            hearts = maxHearts;
            combat = GetComponent<CombatController>();
            lightMeter = GetComponent<LightMeter>();
        }

        void OnEnable()
        {
            CheckpointStore store;
            if (ServiceLocator.TryGet(out store)) store.Register(this);
        }

        void OnDisable()
        {
            CheckpointStore store;
            if (ServiceLocator.TryGet(out store)) store.Unregister(this);
        }

        public void SetMaxHearts(int value, bool refill)
        {
            maxHearts = Mathf.Max(1, value);
            hearts = refill ? maxHearts : Mathf.Min(hearts, maxHearts);
        }

        public void SetInvulnerable(float seconds)
        {
            invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + seconds);
        }

        public void ResetChapterStats() { HeartsLostThisChapter = 0f; }

        public void Heal(float amount)
        {
            hearts = Mathf.Min(maxHearts, hearts + Mathf.Max(0f, amount));
        }

        public void RefillAndRevive()
        {
            hearts = maxHearts;
            downed = false;
        }

        public void TakeDamage(DamageInfo info)
        {
            if (downed || IsInvulnerable) return;
            if (combat != null && combat.TryParry(info)) return;
            if (lightMeter != null && lightMeter.ConsumeShield()) return;
            float amount = info.Amount * DifficultyModifiers.For(SettingsProfile.Difficulty).DamageTakenMultiplier;
            if (info.NonLethal) amount = Mathf.Min(amount, Mathf.Max(0f, hearts - 0.5f));
            hearts = Mathf.Max(0f, hearts - amount);
            HeartsLostThisChapter += amount;
            invulnerableUntil = Time.time + invulnerabilitySeconds;
            EventBus<DamageDealt>.Publish(new DamageDealt(amount, true));
            if (hearts <= 0f)
            {
                downed = true;
                EventBus<PlayerFailed>.Publish(new PlayerFailed(FailureType.Health));
            }
        }

        public string CaptureSnapshot()
        {
            State s = new State();
            s.hearts = hearts;
            s.maxHearts = maxHearts;
            return JsonUtility.ToJson(s);
        }

        public void RestoreSnapshot(string json)
        {
            State s = JsonUtility.FromJson<State>(json);
            maxHearts = Mathf.Max(1, s.maxHearts);
            hearts = maxHearts; // a restored player returns at full health
            downed = false;
        }
    }
}
