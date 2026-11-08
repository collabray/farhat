using UnityEngine;
using UnityEngine.UI;
using Farhat.Core;
using Farhat.Systems;

namespace Farhat.UI
{
    /// <summary>Hearts of light, light meter and Shadow cooldown ring (bible section 14). Reads the live player each frame; assign the images in the HUD prefab.</summary>
    public class HudController : MonoBehaviour
    {
        [SerializeField] Image[] heartIcons = new Image[0];
        [SerializeField] Image lightFill;
        [SerializeField] Image shadowCooldownRing;
        [SerializeField] Image shieldIndicator;

        PlayerController player;
        HealthComponent health;
        LightMeter light;
        ShadowController shadow;

        void Update()
        {
            if (player == null)
            {
                if (!ServiceLocator.TryGet(out player)) return;
                health = player.GetComponent<HealthComponent>();
                light = player.GetComponent<LightMeter>();
                shadow = player.GetComponent<ShadowController>();
            }
            if (health != null)
            {
                for (int i = 0; i < heartIcons.Length; i++)
                {
                    if (heartIcons[i] == null) continue;
                    bool visible = i < health.MaxHearts;
                    heartIcons[i].enabled = visible;
                    if (visible) heartIcons[i].fillAmount = Mathf.Clamp01(health.Hearts - i);
                }
            }
            if (light != null && lightFill != null)
            {
                lightFill.fillAmount = light.Capacity <= 0f ? 0f : Mathf.Clamp01(light.Value / light.Capacity);
                if (shieldIndicator != null) shieldIndicator.enabled = light.ShieldActive;
            }
            if (shadow != null && shadowCooldownRing != null)
            {
                shadowCooldownRing.fillAmount = 1f - Mathf.Clamp01(shadow.CooldownNormalised);
            }
        }
    }
}
