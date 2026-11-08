using UnityEngine;
using UnityEngine.AI;
using Farhat.Core;
using Farhat.Data;
using Farhat.Save;

namespace Farhat.Systems
{
    /// <summary>
    /// Chase, telegraph, attack, recover. Defeated enemies dissolve (no blood). Enemies marked nonLethal in their definition
    /// (Wings, Rashid, Ben under Laylah's control) are freed rather than defeated and never deal more than half a heart.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyBrain : MonoBehaviour, IDamageable
    {
        enum Mode { Idle, Chase, Telegraph, Recover, Stunned, Done }

        [SerializeField] EnemyDefinition definition;
        [SerializeField] float sightRange = 18f;
        [SerializeField] float dissolveSeconds = 0.6f;
        [SerializeField] Renderer telegraphRenderer;
        [SerializeField] Color telegraphColor = new Color(1f, 0.25f, 0.15f);

        readonly StateMachine<Mode> machine = new StateMachine<Mode>();
        NavMeshAgent agent;
        PlayerController player;
        HealthComponent playerHealth;
        float health;
        float timer;
        float stunnedUntil;
        Color baseColor = Color.white;

        public bool IsPlayer { get { return false; } }
        public bool IsDone { get { return machine.Current == Mode.Done; } }
        public EnemyDefinition Definition { get { return definition; } }
        public float Health { get { return health; } }

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (telegraphRenderer != null) baseColor = telegraphRenderer.material.color;
        }

        void OnEnable()
        {
            float multiplier = DifficultyModifiers.For(SettingsProfile.Difficulty).EnemyHealthMultiplier;
            health = definition != null ? definition.maxHealth * multiplier : 30f;
            if (definition != null) agent.speed = definition.moveSpeed;
            machine.Configure(Mode.Idle, null, TickIdle, null)
                   .Configure(Mode.Chase, null, TickChase, null)
                   .Configure(Mode.Telegraph, BeginTelegraph, TickTelegraph, EndTelegraph)
                   .Configure(Mode.Recover, BeginRecover, TickRecover, null)
                   .Configure(Mode.Stunned, BeginStun, TickStun, null);
            machine.Set(Mode.Idle);
        }

        void Update()
        {
            if (Time.timeScale <= 0f) return;
            machine.Tick(Time.deltaTime);
        }

        bool FindPlayer()
        {
            if (player == null)
            {
                ServiceLocator.TryGet(out player);
                playerHealth = player != null ? player.GetComponent<HealthComponent>() : null;
            }
            return player != null;
        }

        float DistanceToPlayer()
        {
            return Vector3.Distance(transform.position, player.transform.position);
        }

        void TickIdle(float dt)
        {
            if (FindPlayer() && DistanceToPlayer() <= sightRange) machine.Set(Mode.Chase);
        }

        void TickChase(float dt)
        {
            if (!FindPlayer()) { machine.Set(Mode.Idle); return; }
            float distance = DistanceToPlayer();
            float range = definition != null ? definition.attackRange : 1.8f;
            if (distance > sightRange * 1.5f) { machine.Set(Mode.Idle); return; }
            if (distance <= range)
            {
                agent.isStopped = true;
                machine.Set(Mode.Telegraph);
                return;
            }
            agent.isStopped = false;
            agent.SetDestination(player.transform.position);
        }

        void BeginTelegraph()
        {
            timer = definition != null ? definition.telegraphSeconds : 0.6f;
            if (telegraphRenderer != null) telegraphRenderer.material.color = telegraphColor;
        }

        void TickTelegraph(float dt)
        {
            timer -= dt;
            if (timer > 0f) return;
            if (FindPlayer() && playerHealth != null)
            {
                float range = definition != null ? definition.attackRange : 1.8f;
                if (DistanceToPlayer() <= range + 0.4f)
                {
                    DamageInfo info = new DamageInfo();
                    info.Amount = definition != null ? definition.contactDamage : 0.5f;
                    info.Source = gameObject;
                    info.Direction = (player.transform.position - transform.position).normalized;
                    info.NonLethal = definition != null && definition.nonLethal;
                    playerHealth.TakeDamage(info);
                }
            }
            machine.Set(Mode.Recover);
        }

        void EndTelegraph()
        {
            if (telegraphRenderer != null) telegraphRenderer.material.color = baseColor;
        }

        void BeginRecover()
        {
            timer = definition != null ? definition.attackCooldown : 1.4f;
        }

        void TickRecover(float dt)
        {
            timer -= dt;
            if (timer <= 0f) { agent.isStopped = false; machine.Set(Mode.Chase); }
        }

        public void Stun(float seconds)
        {
            if (IsDone) return;
            stunnedUntil = Time.time + seconds;
            machine.Set(Mode.Stunned);
        }

        void BeginStun()
        {
            agent.isStopped = true;
        }

        void TickStun(float dt)
        {
            if (Time.time >= stunnedUntil) { agent.isStopped = false; machine.Set(Mode.Chase); }
        }

        public void TakeDamage(DamageInfo info)
        {
            if (IsDone) return;
            health -= info.Amount;
            EventBus<DamageDealt>.Publish(new DamageDealt(info.Amount, false));
            if (health <= 0f) StartCoroutine(Dissolve());
        }

        System.Collections.IEnumerator Dissolve()
        {
            machine.Set(Mode.Done);
            agent.isStopped = true;
            Vector3 start = transform.localScale;
            float t = 0f;
            while (t < dissolveSeconds)
            {
                t += Time.deltaTime;
                transform.localScale = start * Mathf.Clamp01(1f - t / dissolveSeconds);
                yield return null;
            }
            EventBus<EnemyDefeated>.Publish(new EnemyDefeated(definition != null ? definition.id : name));
            gameObject.SetActive(false);
            transform.localScale = start;
        }
    }
}
