using System;
using System.Collections.Generic;

namespace SistemaJuridico.Services
{
    public class ViewRegistryService
    {
        private readonly Dictionary<string, (Type view, Type vm)> _registry = new();

        public void Register(string key, Type viewType, Type viewModelType)
        {
            _registry[key] = (viewType, viewModelType);
        }

        public (Type view, Type vm) Resolve(string key)
        {
            if (!_registry.ContainsKey(key))
                throw new Exception($"View não registrada: {key}");

            return _registry[key];
        }
    }
}
