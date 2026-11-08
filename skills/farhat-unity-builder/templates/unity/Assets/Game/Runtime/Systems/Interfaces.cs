using UnityEngine;

namespace Farhat.Systems
{
    public struct DamageInfo
    {
        public float Amount;          // in hearts for the player, in health points for enemies
        public GameObject Source;
        public Vector3 Direction;
        public bool IsHeavy;
        public bool NonLethal;
    }

    public interface IDamageable
    {
        bool IsPlayer { get; }
        void TakeDamage(DamageInfo info);
    }

    /// <summary>Every system whose state must be restored at a checkpoint implements this and registers with the CheckpointStore.</summary>
    public interface ISnapshotParticipant
    {
        string SnapshotKey { get; }
        string CaptureSnapshot();
        void RestoreSnapshot(string json);
    }
}
