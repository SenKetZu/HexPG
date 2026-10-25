using System;

namespace VoidHex.P3.Domain
{
    /// <summary>
    /// Servicio de equipamiento. El consumidor depende de esta interfaz, no de la implementación,
    /// lo que permite inyectar el servicio real en la escena y un doble en los tests (DIP).
    /// </summary>
    public interface IEquipmentService
    {
        Loadout Current { get; }

        ShipStats CurrentStats { get; }

        EquipResult Equip(string moduleId);

        EquipResult Unequip(SlotType slot);

        /// <summary>Se emite solo cuando las estadísticas cambian realmente.</summary>
        event Action<ShipStats> StatsChanged;
    }
}
