using UnityEngine;

namespace Farhat.Systems
{
    /// <summary>Hidden geometry or markers that appear while the Shadow's Scout is near. Assign the object to show.</summary>
    public class ShadowRevealable : MonoBehaviour
    {
        [SerializeField] GameObject revealTarget;
        float hideAt;
        bool shown;

        void Awake()
        {
            if (revealTarget != null) revealTarget.SetActive(false);
        }

        public void Reveal(float seconds)
        {
            hideAt = Time.time + seconds;
            if (!shown && revealTarget != null)
            {
                revealTarget.SetActive(true);
                shown = true;
            }
        }

        void Update()
        {
            if (shown && Time.time >= hideAt)
            {
                if (revealTarget != null) revealTarget.SetActive(false);
                shown = false;
            }
        }
    }
}
