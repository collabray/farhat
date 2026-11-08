using UnityEngine;

namespace Farhat.Systems
{
    /// <summary>Remembers the last safe, grounded position so falls, quicksand and kill volumes return the player to the last ledge.</summary>
    [RequireComponent(typeof(PlayerController))]
    public class SafePointRecorder : MonoBehaviour
    {
        [SerializeField] float intervalSeconds = 0.5f;
        PlayerController player;
        Vector3 lastSafe;
        float lastYaw;
        float nextRecordAt;
        bool hasPoint;

        public bool HasSafePoint { get { return hasPoint; } }
        public Vector3 LastSafePoint { get { return lastSafe; } }
        public int HazardDepth { get; private set; }

        void Awake() { player = GetComponent<PlayerController>(); }

        public void EnterHazard() { HazardDepth++; }
        public void ExitHazard() { HazardDepth = Mathf.Max(0, HazardDepth - 1); }

        void Update()
        {
            if (Time.time < nextRecordAt) return;
            nextRecordAt = Time.time + intervalSeconds;
            if (player.IsGrounded && HazardDepth == 0 && !player.IsDodging)
            {
                lastSafe = transform.position;
                lastYaw = transform.eulerAngles.y;
                hasPoint = true;
            }
        }

        public bool RestoreToSafePoint()
        {
            if (!hasPoint) return false;
            player.Teleport(lastSafe, Quaternion.Euler(0f, lastYaw, 0f));
            return true;
        }
    }
}
