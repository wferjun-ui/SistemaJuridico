using SistemaJuridico.Infrastructure;
using System;
using System.Collections.Generic;

namespace SistemaJuridico.Services
{
    public class ViewRegistryService
    {
        private readonly Dictionary<NavigationKey, (Type view, Type vm)> _registry = new();

        public void Register(NavigationKey key, Type viewType, Type viewModelType)
        {
            _registry[key] = (viewType, viewModelType);
        }

        public (Type view, Type vm) Resolve(NavigationKey key)
        {
            if (!_registry.ContainsKey(key))
                throw new Exception($"View não registrada: {key}");

            return _registry[key];
        }
    }
}
