using UnityEngine;
using Farhat.Core;
using Farhat.Data;

namespace Farhat.Systems
{
    /// <summary>
    /// A character who must never be hurt: the charmed parents in chapter 26, or any ally under a spell. Damage is ignored, and an attempt to strike
    /// resets the section through the fail-forward manager, as the bible requires.
    /// </summary>
    public class ProtectedAlly : MonoBehaviour, IDamageable
    {
        [SerializeField] float cooldownSeconds = 1.5f;
        float nextAllowedAt;

        public bool IsPlayer { get { return false; } }

        public void TakeDamage(DamageInfo info)
        {
            if (Time.time < nextAllowedAt) return;
            nextAllowedAt = Time.time + cooldownSeconds;
            EventBus<PlayerFailed>.Publish(new PlayerFailed(FailureType.Objective));
        }
    }
}
