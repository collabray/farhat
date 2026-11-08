using System;
using UnityEngine;
using Farhat.Core;
using Farhat.Data;
using Farhat.Story;

namespace Farhat.Systems
{
    /// <summary>
    /// Shariman energy. Fills as Farhat parries and defeats enemies. Capacity grows with each fragment, and the three
    /// abilities unlock with the light tiers granted in chapters 4, 15 and 18 (see data/chapters.json).
    /// </summary>
    public class LightMeter : MonoBehaviour, ISnapshotParticipant
    {
        [SerializeField] float baseCapacity = 100f;
        [SerializeField] float capacityPerFragment = 20f;
        [SerializeField] float maxFormCapacity = 260f;
        [SerializeField] float flashCost = 35f;
        [SerializeField] float flashRadius = 6f;
        [SerializeField] float flashStunSeconds = 2.5f;
        [SerializeField] float bladeCost = 50f;
        [SerializeField] float bladeSeconds = 12f;
        [SerializeField] float bladeMultiplier = 1.5f;
        [SerializeField] float shieldCost = 40f;
        [SerializeField] float shieldSeconds = 8f;
        [SerializeField] LayerMask enemyMask = ~0;

        float value;
        float bladeUntil;
        float shieldUntil;
        UnlockManager unlocks;

        [Serializable]
        class State { public float value; }

        public string SnapshotKey { get { return "light"; } }
        public float Value { get { return value; } }
        public bool BladeActive { get { return Time.time < bladeUntil; } }
        public float DamageMultiplier { get { return BladeActive ? bladeMultiplier : 1f; } }
        public bool ShieldActive { get { return Time.time < shieldUntil; } }

        public float Capacity
        {
            get
            {
                if (unlocks == null) return baseCapacity;
                if (unlocks.Has("meter.max")) return maxFormCapacity;
                return baseCapacity + capacityPerFragment * unlocks.FragmentCount;
            }
        }

        void OnEnable()
        {
            ServiceLocator.TryGet(out unlocks);
            CheckpointStore store;
            if (ServiceLocator.TryGet(out store)) store.Register(this);
        }

        void OnDisable()
        {
            CheckpointStore store;
            if (ServiceLocator.TryGet(out store)) store.Unregister(this);
        }

        public void Gain(float amount)
        {
            float gain = unlocks != null && unlocks.Has("meter.max") ? amount * 1.5f : amount;
            value = Mathf.Min(Capacity, value + Mathf.Max(0f, gain));
        }

        public bool IsAbilityUnlocked(LightAbility ability)
        {
            if (unlocks == null) return false;
            switch (ability)
            {
                case LightAbility.Flash: return unlocks.Has("meter.tier1");
                case LightAbility.LightBlade: return unlocks.Has("meter.tier2");
                default: return unlocks.Has("meter.tier3");
            }
        }

        public bool TrySpend(LightAbility ability)
        {
            if (!IsAbilityUnlocked(ability)) return false;
            float cost = ability == LightAbility.Flash ? flashCost : (ability == LightAbility.LightBlade ? bladeCost : shieldCost);
            if (value < cost) return false;
            value -= cost;
            switch (ability)
            {
                case LightAbility.Flash: DoFlash(); break;
                case LightAbility.LightBlade: bladeUntil = Time.time + bladeSeconds; break;
                default: shieldUntil = Time.time + shieldSeconds; break;
            }
            EventBus<LightAbilityUsed>.Publish(new LightAbilityUsed(ability, transform.position));
            return true;
        }

        public bool ConsumeShield()
        {
            if (!ShieldActive) return false;
            shieldUntil = 0f;
            return true;
        }

        void DoFlash()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, flashRadius, enemyMask);
            for (int i = 0; i < hits.Length; i++)
            {
                EnemyBrain enemy = hits[i].GetComponentInParent<EnemyBrain>();
                if (enemy != null) enemy.Stun(flashStunSeconds);
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
            State s = JsonUtility.FromJson<State>(json);
            value = Mathf.Min(Capacity, s.value);
            bladeUntil = 0f;
            shieldUntil = 0f;
        }
    }
}
