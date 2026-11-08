using UnityEngine;
using Farhat.Core;
using Farhat.Data;

namespace Farhat.Systems
{
    /// <summary>Trigger below levels and over bottomless hazards. Returns the player to the last safe point, or fails the chapter section when none exists.</summary>
    [RequireComponent(typeof(Collider))]
    public class KillVolume : MonoBehaviour
    {
        [SerializeField] float heartCost = 0f;

        void Reset()
        {
            Collider c = GetComponent<Collider>();
            if (c != null) c.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            SafePointRecorder recorder = other.GetComponentInParent<SafePointRecorder>();
            if (recorder == null) return;
            if (!recorder.RestoreToSafePoint())
            {
                EventBus<PlayerFailed>.Publish(new PlayerFailed(FailureType.Health));
                return;
            }
            if (heartCost > 0f)
            {
                HealthComponent health = recorder.GetComponent<HealthComponent>();
                if (health != null)
                {
                    DamageInfo info = new DamageInfo();
                    info.Amount = heartCost;
                    info.NonLethal = true;
                    health.TakeDamage(info);
                }
            }
        }
    }
}
