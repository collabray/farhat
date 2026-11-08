using UnityEngine;
using Farhat.Core;
using Farhat.Data;

namespace Farhat.Systems
{
    public enum HazardKind { Quicksand, Magma, Wind, Heat, Namirash }

    /// <summary>
    /// A volume that applies a continuous effect while the player is inside: quicksand sinks and slows, magma burns,
    /// wind pushes, heat drains, and Namirash zones raise the corruption meter. Effects are tuned per instance.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class HazardZone : MonoBehaviour
    {
        [SerializeField] HazardKind kind = HazardKind.Quicksand;
        [SerializeField] float heartsPerSecond = 0.15f;
        [SerializeField] Vector3 pushPerSecond = Vector3.zero;
        [SerializeField] float corruptionSecondsToFail = 8f;
        [SerializeField] float quicksandSlowFactor = 0.45f;
        [SerializeField] float quicksandSinkSecondsToFail = 6f;

        float corruption;
        float sinkTimer;
        PlayerController insidePlayer;

        public float CorruptionNormalised { get { return corruptionSecondsToFail <= 0f ? 0f : corruption / corruptionSecondsToFail; } }

        void Reset()
        {
            Collider c = GetComponent<Collider>();
            if (c != null) c.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;
            insidePlayer = player;
            SafePointRecorder recorder = player.GetComponent<SafePointRecorder>();
            if (recorder != null) recorder.EnterHazard();
        }

        void OnTriggerExit(Collider other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || player != insidePlayer) return;
            SafePointRecorder recorder = player.GetComponent<SafePointRecorder>();
            if (recorder != null) recorder.ExitHazard();
            insidePlayer = null;
            sinkTimer = 0f;
        }

        void Update()
        {
            if (insidePlayer == null)
            {
                corruption = Mathf.Max(0f, corruption - Time.deltaTime * 2f);
                return;
            }
            float dt = Time.deltaTime;
            switch (kind)
            {
                case HazardKind.Quicksand:
                    insidePlayer.ApplySlow(quicksandSlowFactor, 0.2f);
                    sinkTimer += dt;
                    if (sinkTimer >= quicksandSinkSecondsToFail)
                    {
                        sinkTimer = 0f;
                        SafePointRecorder recorder = insidePlayer.GetComponent<SafePointRecorder>();
                        if (recorder != null && !recorder.RestoreToSafePoint())
                        {
                            EventBus<PlayerFailed>.Publish(new PlayerFailed(FailureType.Health));
                        }
                    }
                    break;
                case HazardKind.Magma:
                case HazardKind.Heat:
                    Damage(heartsPerSecond * dt);
                    break;
                case HazardKind.Wind:
                    insidePlayer.transform.position += pushPerSecond * dt;
                    break;
                case HazardKind.Namirash:
                    corruption += dt;
                    if (corruption >= corruptionSecondsToFail)
                    {
                        corruption = 0f;
                        EventBus<PlayerFailed>.Publish(new PlayerFailed(FailureType.Corruption));
                    }
                    break;
            }
        }

        void Damage(float amount)
        {
            HealthComponent health = insidePlayer.GetComponent<HealthComponent>();
            if (health == null) return;
            DamageInfo info = new DamageInfo();
            info.Amount = amount;
            info.NonLethal = false;
            health.TakeDamage(info);
        }
    }
}
