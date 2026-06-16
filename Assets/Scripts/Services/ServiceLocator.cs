using System;
using System.Collections.Generic;

public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

    public static void Register<T>(T service)
    {
        Type type = typeof(T);
        if (_services.ContainsKey(type))
        {
            _services[type] = service;
        }
        else
        {
            _services.Add(type, service);
        }
    }

    public static T Get<T>()
    {
        Type type = typeof(T);
        if (_services.TryGetValue(type, out var service))
        {
            return (T)service;
        }
        throw new Exception($"Service {type} not found in ServiceLocator.");
    }

    public static void Clear()
    {
        _services.Clear();
    }
}
