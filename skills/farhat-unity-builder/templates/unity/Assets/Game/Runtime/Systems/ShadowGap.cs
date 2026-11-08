using System;
using UnityEngine;

namespace Farhat.Systems
{
    /// <summary>A narrow gap only the Shadow can pass. Slip opens it, which disables the barrier and raises the Opened event.</summary>
    public class ShadowGap : MonoBehaviour
    {
        [SerializeField] GameObject barrier;
        public event Action Opened;
        public bool IsOpen { get; private set; }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            if (barrier != null) barrier.SetActive(false);
            Action handler = Opened;
            if (handler != null) handler();
        }
    }
}
