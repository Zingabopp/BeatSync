using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace BeatSyncLib.Utilities
{
    public class LocationServiceCollection
    {
        private readonly ConcurrentDictionary<Type, Func<string, object?>> _transientServices
            = new ConcurrentDictionary<Type, Func<string, object?>>();

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<Type, object?>> _singletons
            = new ConcurrentDictionary<string, ConcurrentDictionary<Type, object?>>();

        private readonly ConcurrentDictionary<Type, Func<string, object?>> _singletonFactories
            = new ConcurrentDictionary<Type, Func<string, object?>>();

        public void AddSingleton<T>(Func<string, T> factory) where T : class
        {
            if (!_singletonFactories.TryAdd(typeof(T), s => factory(s)))
                throw new InvalidOperationException($"Cannot add service of type {typeof(T).Name} (already exists?)");
        }
        public void AddTransient<T>(Func<string, T> factory) where T : class
        {
            if (!_transientServices.TryAdd(typeof(T), s => factory(s)))
                throw new InvalidOperationException($"Cannot add service of type {typeof(T).Name} (already exists?)");
        }

        public T? GetService<T>(string path) where T : class
        {
            if (_transientServices.TryGetValue(typeof(T), out var factory))
            {
                return factory(path) as T;
            }
            else
            {
                var locationDict = _singletons.GetOrAdd(path, s => new ConcurrentDictionary<Type, object?>());
                var service = locationDict.GetOrAdd(typeof(T), s =>
                {
                    if (_singletonFactories.TryGetValue(typeof(T), out var factory))
                    {
                        return factory(path) as T;
                    }
                    else
                        throw new InvalidOperationException($"No factory for service of type {typeof(T).Name}");
                });
            }
            return null;
        }
    }
}
