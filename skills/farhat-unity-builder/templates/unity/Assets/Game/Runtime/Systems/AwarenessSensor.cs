using UnityEngine;
using Farhat.Core;

namespace Farhat.Systems
{
    /// <summary>A watcher with a vision cone. Reports the player to an AwarenessMeter while in view, scaled by player noise and combat.</summary>
    public class AwarenessSensor : MonoBehaviour
    {
        [SerializeField] AwarenessMeter meter;
        [SerializeField] float viewDistance = 14f;
        [SerializeField] float viewAngleDegrees = 110f;
        [SerializeField] float seenRatePerSecond = 0.2f;
        [SerializeField] float combatRatePerSecond = 0.6f;
        [SerializeField] LayerMask obstructionMask = ~0;

        PlayerController player;
        CombatController combat;

        void Update()
        {
            if (meter == null) return;
            if (player == null)
            {
                if (!ServiceLocator.TryGet(out player)) return;
                combat = player.GetComponent<CombatController>();
            }
            Vector3 toPlayer = player.transform.position - transform.position;
            if (toPlayer.magnitude > viewDistance) return;
            if (Vector3.Angle(transform.forward, toPlayer) > viewAngleDegrees * 0.5f) return;
            if (Physics.Linecast(transform.position + Vector3.up * 1.5f, player.transform.position + Vector3.up * 1f, obstructionMask)) return;
            float rate = seenRatePerSecond * Mathf.Max(0.2f, player.NoiseLevel);
            if (combat != null && combat.IsBusy) rate += combatRatePerSecond;
            meter.ReportPerceived(rate);
        }
    }
}
