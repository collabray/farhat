using UnityEngine;
using UnityEngine.AI;
using Farhat.Core;

namespace Farhat.Systems
{
    /// <summary>Follows the player on the NavMesh. A stall timer and a teleport fallback keep companions from getting stuck or lost.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class CompanionFollower : MonoBehaviour
    {
        [SerializeField] float followDistance = 2.5f;
        [SerializeField] float catchUpDistance = 9f;
        [SerializeField] float teleportDistance = 28f;
        [SerializeField] float stallSeconds = 4f;
        [SerializeField] float stallSpeedThreshold = 0.1f;
        [SerializeField] float walkSpeed = 4.2f;
        [SerializeField] float runSpeed = 7f;

        NavMeshAgent agent;
        PlayerController player;
        float stalledFor;

        void Awake() { agent = GetComponent<NavMeshAgent>(); }

        void Update()
        {
            if (Time.timeScale <= 0f) return;
            if (player == null && !ServiceLocator.TryGet(out player)) return;

            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance > teleportDistance)
            {
                TeleportNearPlayer();
                return;
            }
            agent.speed = distance > catchUpDistance ? runSpeed : walkSpeed;
            agent.stoppingDistance = followDistance;
            if (distance > followDistance) agent.SetDestination(player.transform.position);

            bool shouldMove = distance > followDistance + 0.5f;
            if (shouldMove && agent.velocity.magnitude < stallSpeedThreshold) stalledFor += Time.deltaTime;
            else stalledFor = 0f;
            if (stalledFor >= stallSeconds)
            {
                stalledFor = 0f;
                TeleportNearPlayer();
            }
        }

        void TeleportNearPlayer()
        {
            Vector3 target = player.transform.position - player.transform.forward * followDistance;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(target, out hit, 4f, NavMesh.AllAreas)) agent.Warp(hit.position);
            else agent.Warp(player.transform.position);
        }
    }
}
