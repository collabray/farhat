using UnityEngine;
using Farhat.Core;
using Farhat.Save;

namespace Farhat.Systems
{
    /// <summary>Over-the-shoulder orbit camera with wall collision. Authored cinematic cameras (Cinemachine or Timeline) can take over by disabling this component.</summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] float distance = 5.5f;
        [SerializeField] float height = 1.7f;
        [SerializeField] float mouseDegreesPerPixel = 0.12f;
        [SerializeField] float stickDegreesPerSecond = 140f;
        [SerializeField] float minPitch = -20f;
        [SerializeField] float maxPitch = 65f;
        [SerializeField] float collisionRadius = 0.3f;
        [SerializeField] LayerMask collisionMask = ~0;

        PlayerInputActions input;
        bool ownsInput;
        float yaw;
        float pitch = 15f;
        float shakeUntil;
        float shakeMagnitude;

        public Transform Target { get { return target; } set { target = value; } }

        void OnEnable()
        {
            if (!ServiceLocator.TryGet(out input))
            {
                input = new PlayerInputActions();
                ownsInput = true;
            }
            yaw = transform.eulerAngles.y;
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

        /// <summary>Camera shake respects the accessibility toggle.</summary>
        public void Shake(float seconds, float magnitude)
        {
            if (!SettingsProfile.CameraShake) return;
            shakeUntil = Time.time + seconds;
            shakeMagnitude = magnitude;
        }

        void LateUpdate()
        {
            if (target == null || input == null || Time.timeScale <= 0f) return;
            Vector2 mouse = input.Look.ReadValue<Vector2>();
            Vector2 stick = input.LookStick.ReadValue<Vector2>();
            yaw += mouse.x * mouseDegreesPerPixel + stick.x * stickDegreesPerSecond * Time.deltaTime;
            pitch -= mouse.y * mouseDegreesPerPixel + stick.y * stickDegreesPerSecond * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            Vector3 pivot = target.position + Vector3.up * height;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 backwards = rotation * Vector3.back;
            float usable = distance;
            RaycastHit hit;
            if (Physics.SphereCast(pivot, collisionRadius, backwards, out hit, distance, collisionMask))
            {
                usable = Mathf.Max(0.5f, hit.distance);
            }
            Vector3 position = pivot + backwards * usable;
            if (Time.time < shakeUntil)
            {
                position += new Vector3(Mathf.Sin(Time.time * 53f), Mathf.Cos(Time.time * 47f), 0f) * shakeMagnitude;
            }
            transform.SetPositionAndRotation(position, rotation);
        }
    }
}
