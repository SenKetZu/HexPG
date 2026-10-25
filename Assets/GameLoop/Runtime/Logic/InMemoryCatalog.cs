using System;
using System.Collections.Generic;
using VoidHex.P3.Domain;

namespace VoidHex.GameLoop.Logic
{
    /// <summary>
    /// Catálogo en memoria. Sirve para dos cosas: que los tests no dependan de assets, y que la escena
    /// del bucle siga funcionando si falta el catálogo serializado (respaldo, no camino principal).
    /// </summary>
    public sealed class InMemoryCatalog : IModuleCatalog
    {
        private readonly List<ModuleDefinition> _modules = new List<ModuleDefinition>();

        public InMemoryCatalog(params ModuleDefinition[] modules)
        {
            foreach (var module in modules)
            {
                if (module.HasIdentity) _modules.Add(module);
            }
        }

        public bool TryGet(string id, out ModuleDefinition module)
        {
            for (var i = 0; i < _modules.Count; i++)
            {
                if (!string.Equals(_modules[i].Id, id, StringComparison.Ordinal)) continue;

                module = _modules[i];
                return true;
            }

            module = default;
            return false;
        }

        public IReadOnlyList<ModuleDefinition> All => _modules;

        /// <summary>Loadout mínimo con el que el bucle puede arrancar sin assets.</summary>
        public static InMemoryCatalog CreateDefault()
        {
            return new InMemoryCatalog(
                new ModuleDefinition("laser-mk1", "Láser MK I", SlotType.Weapon, 1, 0, 0, 0, "beam"),
                new ModuleDefinition("laser-mk3", "Láser MK III", SlotType.Weapon, 3, 1, 0, 0, "beam-iii"),
                new ModuleDefinition("thruster-mk1", "Propulsor MK I", SlotType.Propulsion, 1, 0, 0, 1, string.Empty),
                new ModuleDefinition("shield-mk1", "Escudo MK I", SlotType.Shield, 1, 0, 3, 0, "barrier"));
        }
    }
}
