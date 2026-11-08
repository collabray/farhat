using UnityEngine;

namespace Farhat.Data
{
    [CreateAssetMenu(menuName = "Farhat/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public EnemyFamily family;
        public float maxHealth = 30f;
        public float contactDamage = 0.5f;   // in hearts
        public float moveSpeed = 3.2f;
        public float attackRange = 1.8f;
        public float telegraphSeconds = 0.6f; // long enough to read, tuned in playtests
        public float attackCooldown = 1.4f;
        public bool nonLethal;                // enslaved allies are freed, never defeated
    }
}
