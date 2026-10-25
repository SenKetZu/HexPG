using System.Collections.Generic;

namespace VoidHex.P3.Domain
{
    /// <summary>
    /// Origen de definiciones de módulos. La implementación real lee assets (Flyweight);
    /// en los tests se inyecta un doble, sin tocar Unity (DIP).
    /// </summary>
    public interface IModuleCatalog
    {
        bool TryGet(string id, out ModuleDefinition module);

        IReadOnlyList<ModuleDefinition> All { get; }
    }
}
