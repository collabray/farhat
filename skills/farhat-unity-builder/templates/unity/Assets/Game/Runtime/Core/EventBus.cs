using System;
using System.Collections.Generic;
using UnityEngine;

namespace Farhat.Core
{
    /// <summary>Clears every typed bus. Used on domain-reload-off play mode starts and by tests.</summary>
    public static class EventBusRegistry
    {
        static readonly List<Action> clearers = new List<Action>();

        public static void Register(Action clear)
        {
            clearers.Add(clear);
        }

        public static void ClearAll()
        {
            for (int i = 0; i < clearers.Count; i++)
            {
                clearers[i]();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlayModeStart()
        {
            ClearAll();
        }
    }

    /// <summary>Typed publish/subscribe bus. Subscribers must unsubscribe in OnDisable.</summary>
    public static class EventBus<T> where T : struct
    {
        static Action<T> handlers;

        static EventBus()
        {
            EventBusRegistry.Register(Clear);
        }

        public static int SubscriberCount
        {
            get { return handlers == null ? 0 : handlers.GetInvocationList().Length; }
        }

        public static void Subscribe(Action<T> handler)
        {
            handlers += handler;
        }

        public static void Unsubscribe(Action<T> handler)
        {
            handlers -= handler;
        }

        public static void Publish(T evt)
        {
            Action<T> h = handlers;
            if (h != null)
            {
                h(evt);
            }
        }

        public static void Clear()
        {
            handlers = null;
        }
    }
}
