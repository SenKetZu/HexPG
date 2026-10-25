using System;
using System.Collections.Generic;
using UnityEngine;
using VoidHex.P3.Domain;

namespace VoidHex.P3.Unity
{
    /// <summary>
    /// Catálogo de módulos implementado como asset. El servicio depende de IModuleCatalog,
    /// no de este tipo, por lo que en los tests se inyecta un fake.
    /// </summary>
    [CreateAssetMenu(menuName = "VoidHex/ModuleCatalog", fileName = "ModuleCatalog")]
    public sealed class ModuleCatalogAsset : ScriptableObject, IModuleCatalog
    {
        [SerializeField] private ShipModuleAsset[] modules = Array.Empty<ShipModuleAsset>();

        private Dictionary<string, ModuleDefinition> _index;
        private ModuleDefinition[] _all;

        public void Initialize(ShipModuleAsset[] entries)
        {
            modules = entries ?? Array.Empty<ShipModuleAsset>();
            _index = null;
            _all = null;
        }

        public bool TryGet(string id, out ModuleDefinition module)
        {
            EnsureIndex();

            if (!string.IsNullOrEmpty(id) && _index.TryGetValue(id, out module))
            {
                return true;
            }

            module = default;
            return false;
        }

        public IReadOnlyList<ModuleDefinition> All
        {
            get
            {
                EnsureIndex();
                return _all;
            }
        }

        private void EnsureIndex()
        {
            if (_index != null) return;

            _index = new Dictionary<string, ModuleDefinition>();
            var definitions = new List<ModuleDefinition>();

            for (var i = 0; i < modules.Length; i++)
            {
                var asset = modules[i];
                if (asset == null) continue;

                var definition = asset.ToDefinition();
                if (!definition.HasIdentity) continue;

                _index[definition.Id] = definition;
                definitions.Add(definition);
            }

            _all = definitions.ToArray();
        }
    }
}
