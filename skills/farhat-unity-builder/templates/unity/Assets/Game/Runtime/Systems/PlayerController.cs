using System;
using UnityEngine;
using Farhat.Core;
using Farhat.Story;

namespace Farhat.Systems
{
    /// <summary>Camera-relative third-person movement with sprint, jump and a dodge roll that grants brief invulnerability.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour, ISnapshotParticipant
    {
        [SerializeField] float walkSpeed = 4.5f;
        [SerializeField] float sprintSpeed = 7f;
        [SerializeField] float turnSpeedDegrees = 720f;
        [SerializeField] float jumpHeight = 1.4f;
        [SerializeField] float gravity = -24f;
        [SerializeField] float dodgeSpeed = 11f;
        [SerializeField] float dodgeSeconds = 0.35f;
        [SerializeField] float dodgeCooldown = 0.8f;
        [SerializeField] float dodgeInvulnerability = 0.4f;
        [SerializeField] Transform cameraTransform;

        CharacterController controller;
        HealthComponent health;
        PlayerInputActions input;
        bool ownsInput;
        float verticalVelocity;
        float dodgeUntil;
        float nextDodgeAt;
        Vector3 dodgeDirection;
        float boostUntil;
        float boostMultiplier = 1f;
        float slowUntil;
        float slowFactor = 1f;

        [Serializable]
        class State { public Vector3 position; public float yaw; }

        public string SnapshotKey { get { return "player"; } }
        public bool IsGrounded { get { return controller != null && controller.isGrounded; } }
        public bool IsDodging { get { return Time.time < dodgeUntil; } }
        public float SpeedMultiplier
        {
            get
            {
                float boost = Time.time < boostUntil ? boostMultiplier : 1f;
                float slow = Time.time < slowUntil ? slowFactor : 1f;
                return boost * slow;
            }
        }
        /// <summary>0 when still, 0.4 when walking, 1 when sprinting or dodging. Read by the cover-up system.</summary>
        public float NoiseLevel { get; private set; }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<HealthComponent>();
        }

        void OnEnable()
        {
            ServiceLocator.Register<PlayerController>(this);
            if (!ServiceLocator.TryGet(out input))
            {
                input = new PlayerInputActions();
                ownsInput = true;
            }
            CheckpointStore store;
            if (ServiceLocator.TryGet(out store)) store.Register(this);
        }

        void OnDisable()
        {
            ServiceLocator.Unregister<PlayerController>();
            CheckpointStore store;
            if (ServiceLocator.TryGet(out store)) store.Unregister(this);
            if (ownsInput && input != null)
            {
                input.Dispose();
                input = null;
                ownsInput = false;
            }
        }

        void Update()
        {
            if (Time.timeScale <= 0f || input == null) return;
            Transform cam = cameraTransform != null ? cameraTransform : (Camera.main != null ? Camera.main.transform : transform);

            Vector2 move = input.Move.ReadValue<Vector2>();
            Vector3 forward = cam.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = cam.right;
            right.y = 0f;
            right.Normalize();
            Vector3 direction = forward * move.y + right * move.x;
            if (direction.sqrMagnitude > 1f) direction.Normalize();

            bool sprinting = input.Sprint.IsPressed() && direction.sqrMagnitude > 0.01f;
            if (input.Dodge.WasPressedThisFrame() && Time.time >= nextDodgeAt && controller.isGrounded)
            {
                dodgeDirection = direction.sqrMagnitude > 0.01f ? direction.normalized : transform.forward;
                dodgeUntil = Time.time + dodgeSeconds;
                nextDodgeAt = Time.time + dodgeCooldown;
                if (health != null) health.SetInvulnerable(dodgeInvulnerability);
            }

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            if (input.Jump.WasPressedThisFrame() && controller.isGrounded)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            verticalVelocity += gravity * Time.deltaTime;

            Vector3 horizontal;
            if (IsDodging)
            {
                horizontal = dodgeDirection * dodgeSpeed;
                NoiseLevel = 1f;
            }
            else
            {
                float speed = (sprinting ? sprintSpeed : walkSpeed) * SpeedMultiplier;
                horizontal = direction * speed;
                NoiseLevel = direction.sqrMagnitude < 0.01f ? 0f : (sprinting ? 1f : 0.4f);
                if (direction.sqrMagnitude > 0.01f)
                {
                    Quaternion target = Quaternion.LookRotation(direction, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeedDegrees * Time.deltaTime);
                }
            }

            Vector3 velocity = horizontal;
            velocity.y = verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            controller.enabled = true;
            verticalVelocity = 0f;
        }

        public void AddSpeedBoost(float multiplier, float seconds)
        {
            boostMultiplier = Mathf.Max(1f, multiplier);
            boostUntil = Time.time + Mathf.Max(0f, seconds);
        }

        /// <summary>Temporary slow, used by quicksand and similar terrain. Refreshed every frame while the effect applies.</summary>
        public void ApplySlow(float factor, float seconds)
        {
            slowFactor = Mathf.Clamp(factor, 0.1f, 1f);
            slowUntil = Time.time + Mathf.Max(0f, seconds);
        }

        public string CaptureSnapshot()
        {
            State s = new State();
            s.position = transform.position;
            s.yaw = transform.eulerAngles.y;
            return JsonUtility.ToJson(s);
        }

        public void RestoreSnapshot(string json)
        {
            State s = JsonUtility.FromJson<State>(json);
            Teleport(s.position, Quaternion.Euler(0f, s.yaw, 0f));
        }
    }
}
