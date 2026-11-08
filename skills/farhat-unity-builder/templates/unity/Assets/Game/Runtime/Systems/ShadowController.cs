using System;
using System.Collections;
using UnityEngine;
using Farhat.Core;
using Farhat.Data;
using Farhat.Story;

namespace Farhat.Systems
{
    /// <summary>
    /// Farhat's Shadow (bible 6.4). Four cooldown abilities: Scout reveals hidden paths, Slip opens narrow gaps, Swap trades places
    /// with the proxy, Shape turns the proxy into an object. States: Available, Captured (chapter 5, abilities locked),
    /// Absent (story removal) and Evolved (chapter 48). Slip unlocks together with Scout, Swap with shadow.swap and Shape with shadow.shape.
    /// </summary>
    public class ShadowController : MonoBehaviour, ISnapshotParticipant
    {
        [SerializeField] Transform proxy;
        [SerializeField] GameObject[] shapeObjects = new GameObject[0];
        [SerializeField] float cooldownSeconds = 8f;
        [SerializeField] float scoutSeconds = 4f;
        [SerializeField] float scoutSpeed = 6f;
        [SerializeField] float revealRadius = 8f;
        [SerializeField] float revealSeconds = 6f;
        [SerializeField] float slipRange = 3.5f;
        [SerializeField] float shapeSeconds = 10f;
        [SerializeField] float defaultAreaLockSeconds = 120f;

        float readyAt;
        float lockedUntil;
        int shapeIndex;
        ShadowAbility selected = ShadowAbility.Scout;
        UnlockManager unlocks;
        PlayerController player;

        [Serializable]
        class Snapshot { public int state; public float cooldownRemaining; public float lockRemaining; }

        public string SnapshotKey { get { return "shadow"; } }
        public ShadowState State { get; private set; }
        public ShadowAbility Selected { get { return selected; } }
        public float CooldownRemaining { get { return Mathf.Max(0f, readyAt - Time.time); } }
        public float CooldownNormalised { get { return cooldownSeconds <= 0f ? 0f : CooldownRemaining / cooldownSeconds; } }
        public bool ProxyActive { get { return proxy != null && proxy.gameObject.activeSelf; } }

        public bool CanUse
        {
            get
            {
                return (State == ShadowState.Available || State == ShadowState.Evolved)
                    && Time.time >= readyAt && Time.time >= lockedUntil;
            }
        }

        void Awake()
        {
            State = ShadowState.Available;
            player = GetComponent<PlayerController>();
            if (proxy != null) proxy.gameObject.SetActive(false);
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

        public void SetState(ShadowState next)
        {
            State = next;
            EventBus<ShadowStateChanged>.Publish(new ShadowStateChanged(next));
        }

        /// <summary>Impulse-trap penalty from the failure rules: the Shadow is disabled for the rest of the area.</summary>
        public void LockForArea()
        {
            lockedUntil = Time.time + defaultAreaLockSeconds;
        }

        public void LockForArea(float seconds)
        {
            lockedUntil = Time.time + seconds;
        }

        public bool IsAbilityUnlocked(ShadowAbility ability)
        {
            if (unlocks == null) return false;
            switch (ability)
            {
                case ShadowAbility.Scout:
                case ShadowAbility.Slip: return unlocks.Has("shadow.scout");
                case ShadowAbility.Swap: return unlocks.Has("shadow.swap");
                default: return unlocks.Has("shadow.shape");
            }
        }

        public void SelectNext()
        {
            for (int i = 1; i <= 4; i++)
            {
                ShadowAbility candidate = (ShadowAbility)(((int)selected + i) % 4);
                if (IsAbilityUnlocked(candidate)) { selected = candidate; return; }
            }
        }

        public bool TryUse()
        {
            return TryUse(selected);
        }

        public bool TryUse(ShadowAbility ability)
        {
            if (!CanUse || !IsAbilityUnlocked(ability)) return false;
            readyAt = Time.time + cooldownSeconds;
            EventBus<ShadowAbilityUsed>.Publish(new ShadowAbilityUsed(ability));
            switch (ability)
            {
                case ShadowAbility.Scout: StartCoroutine(Scout()); break;
                case ShadowAbility.Slip: Slip(); break;
                case ShadowAbility.Swap: Swap(); break;
                default: StartCoroutine(Shape()); break;
            }
            return true;
        }

        IEnumerator Scout()
        {
            if (proxy == null) yield break;
            proxy.position = transform.position + Vector3.up * 0.2f;
            proxy.gameObject.SetActive(true);
            Vector3 forward = transform.forward;
            float t = 0f;
            while (t < scoutSeconds)
            {
                t += Time.deltaTime;
                proxy.position += forward * scoutSpeed * Time.deltaTime;
                RevealAround(proxy.position);
                yield return null;
            }
            proxy.gameObject.SetActive(false);
        }

        void RevealAround(Vector3 center)
        {
            Collider[] hits = Physics.OverlapSphere(center, revealRadius);
            for (int i = 0; i < hits.Length; i++)
            {
                ShadowRevealable revealable = hits[i].GetComponentInParent<ShadowRevealable>();
                if (revealable != null) revealable.Reveal(revealSeconds);
            }
        }

        void Slip()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, slipRange);
            ShadowGap best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
            {
                ShadowGap gap = hits[i].GetComponentInParent<ShadowGap>();
                if (gap == null || gap.IsOpen) continue;
                float d = Vector3.Distance(transform.position, gap.transform.position);
                if (d < bestDistance) { bestDistance = d; best = gap; }
            }
            if (best != null) best.Open();
        }

        void Swap()
        {
            if (proxy == null || !proxy.gameObject.activeSelf || player == null) return;
            Vector3 playerPosition = transform.position;
            Vector3 proxyPosition = proxy.position;
            player.Teleport(proxyPosition, transform.rotation);
            proxy.position = playerPosition;
        }

        IEnumerator Shape()
        {
            if (proxy == null || shapeObjects == null || shapeObjects.Length == 0) yield break;
            proxy.position = transform.position + transform.forward * 1.5f;
            proxy.gameObject.SetActive(true);
            shapeIndex = (shapeIndex + 1) % shapeObjects.Length;
            for (int i = 0; i < shapeObjects.Length; i++)
            {
                if (shapeObjects[i] != null) shapeObjects[i].SetActive(i == shapeIndex);
            }
            yield return new WaitForSeconds(shapeSeconds);
            proxy.gameObject.SetActive(false);
        }

        public string CaptureSnapshot()
        {
            Snapshot s = new Snapshot();
            s.state = (int)State;
            s.cooldownRemaining = CooldownRemaining;
            s.lockRemaining = Mathf.Max(0f, lockedUntil - Time.time);
            return JsonUtility.ToJson(s);
        }

        public void RestoreSnapshot(string json)
        {
            Snapshot s = JsonUtility.FromJson<Snapshot>(json);
            State = (ShadowState)s.state;
            readyAt = Time.time + s.cooldownRemaining;
            lockedUntil = 0f; // a restore clears any impulse-trap lock taken after the checkpoint
            if (proxy != null) proxy.gameObject.SetActive(false);
        }
    }
}
