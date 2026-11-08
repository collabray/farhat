using UnityEngine;
using Farhat.Core;
using Farhat.Data;
using Farhat.Save;

namespace Farhat.Systems
{
    /// <summary>
    /// Sword combat from the bible: three-hit light combo, heavy strike, a perfect parry window that staggers the attacker,
    /// and the three light abilities. Story difficulty adds an automatic parry on a cooldown.
    /// </summary>
    public class CombatController : MonoBehaviour
    {
        [SerializeField] float[] comboDamage = { 10f, 10f, 15f };
        [SerializeField] float heavyDamage = 25f;
        [SerializeField] float attackReach = 1.4f;
        [SerializeField] float attackRadius = 1.3f;
        [SerializeField] float lightAttackSeconds = 0.35f;
        [SerializeField] float heavyWindupSeconds = 0.5f;
        [SerializeField] float heavyRecoverySeconds = 0.5f;
        [SerializeField] float comboWindowSeconds = 0.6f;
        [SerializeField] float parryCooldownSeconds = 0.8f;
        [SerializeField] float autoParryCooldownSeconds = 3f;
        [SerializeField] float staggerSeconds = 1.5f;
        [SerializeField] float lightGainPerHit = 6f;
        [SerializeField] float lightGainPerParry = 20f;
        [SerializeField] LayerMask hitMask = ~0;

        PlayerInputActions input;
        bool ownsInput;
        LightMeter lightMeter;
        int comboIndex;
        float busyUntil;
        float comboExpiresAt;
        float parryOpenUntil;
        float nextParryAt;
        float nextAutoParryAt;
        bool heavyPending;
        float heavyFiresAt;

        public bool IsBusy { get { return Time.time < busyUntil; } }
        public bool ParryOpen { get { return Time.time < parryOpenUntil; } }

        void Awake()
        {
            lightMeter = GetComponent<LightMeter>();
        }

        void OnEnable()
        {
            if (!ServiceLocator.TryGet(out input))
            {
                input = new PlayerInputActions();
                ownsInput = true;
            }
        }

        void OnDisable()
        {
            if (ownsInput && input != null)
            {
                input.Dispose();
                input = null;
                ownsInput = false;
            }
        }

        void Update()
        {
            if (input == null || Time.timeScale <= 0f) return;
            if (heavyPending && Time.time >= heavyFiresAt)
            {
                heavyPending = false;
                Strike(heavyDamage, true);
            }
            if (IsBusy) return;

            if (input.Parry.WasPressedThisFrame() && Time.time >= nextParryAt)
            {
                parryOpenUntil = Time.time + DifficultyModifiers.For(SettingsProfile.Difficulty).ParryWindowSeconds;
                nextParryAt = Time.time + parryCooldownSeconds;
                busyUntil = Time.time + 0.2f;
                return;
            }
            if (input.HeavyAttack.WasPressedThisFrame())
            {
                heavyPending = true;
                heavyFiresAt = Time.time + heavyWindupSeconds;
                busyUntil = heavyFiresAt + heavyRecoverySeconds;
                comboIndex = 0;
                return;
            }
            if (input.LightAttack.WasPressedThisFrame())
            {
                if (Time.time > comboExpiresAt) comboIndex = 0;
                float damage = comboDamage[Mathf.Clamp(comboIndex, 0, comboDamage.Length - 1)];
                Strike(damage, false);
                comboIndex = (comboIndex + 1) % comboDamage.Length;
                busyUntil = Time.time + lightAttackSeconds;
                comboExpiresAt = Time.time + comboWindowSeconds;
            }
            if (lightMeter != null)
            {
                if (input.ChannelFlash.WasPressedThisFrame()) lightMeter.TrySpend(LightAbility.Flash);
                if (input.ChannelBlade.WasPressedThisFrame()) lightMeter.TrySpend(LightAbility.LightBlade);
                if (input.ChannelShield.WasPressedThisFrame()) lightMeter.TrySpend(LightAbility.Shield);
            }
        }

        void Strike(float baseDamage, bool heavy)
        {
            float multiplier = lightMeter != null ? lightMeter.DamageMultiplier : 1f;
            Vector3 center = transform.position + Vector3.up * 1f + transform.forward * attackReach;
            Collider[] hits = Physics.OverlapSphere(center, attackRadius, hitMask);
            for (int i = 0; i < hits.Length; i++)
            {
                IDamageable target = hits[i].GetComponentInParent<IDamageable>();
                if (target == null || target.IsPlayer) continue;
                DamageInfo info = new DamageInfo();
                info.Amount = baseDamage * multiplier;
                info.Source = gameObject;
                info.Direction = transform.forward;
                info.IsHeavy = heavy;
                target.TakeDamage(info);
                if (lightMeter != null) lightMeter.Gain(lightGainPerHit);
            }
        }

        /// <summary>Called by HealthComponent before damage lands. Returns true when the hit was parried.</summary>
        public bool TryParry(DamageInfo info)
        {
            bool parried = ParryOpen;
            if (!parried)
            {
                DifficultyModifiers mods = DifficultyModifiers.For(SettingsProfile.Difficulty);
                if (mods.AutoParry && Time.time >= nextAutoParryAt)
                {
                    nextAutoParryAt = Time.time + autoParryCooldownSeconds;
                    parried = true;
                }
            }
            if (!parried) return false;
            parryOpenUntil = 0f;
            if (info.Source != null)
            {
                EnemyBrain attacker = info.Source.GetComponentInParent<EnemyBrain>();
                if (attacker != null) attacker.Stun(staggerSeconds);
            }
            if (lightMeter != null) lightMeter.Gain(lightGainPerParry);
            return true;
        }
    }
}
