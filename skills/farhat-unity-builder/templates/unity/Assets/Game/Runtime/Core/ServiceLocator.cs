using System;
using System.Collections.Generic;

namespace Farhat.Core
{
    /// <summary>Services are registered once at start-up (by GameBootstrap). No scene searches at runtime.</summary>
    public static class ServiceLocator
    {
        static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

        public static void Register<T>(T service) where T : class
        {
            services[typeof(T)] = service;
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            object value;
            if (services.TryGetValue(typeof(T), out value))
            {
                service = (T)value;
                return service != null;
            }
            service = null;
            return false;
        }

        public static T Get<T>() where T : class
        {
            T service;
            if (!TryGet(out service))
            {
                throw new InvalidOperationException("Service not registered: " + typeof(T).Name);
            }
            return service;
        }

        public static void Unregister<T>() where T : class
        {
            services.Remove(typeof(T));
        }

        public static void Clear()
        {
            services.Clear();
        }
    }
}
