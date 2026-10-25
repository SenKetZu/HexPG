using System;
using VoidHex.P3.Domain;

namespace VoidHex.P3.Application
{
    /// <summary>
    /// Implementación del servicio de equipamiento. Recibe sus dependencias por constructor,
    /// sin acceso global: el catálogo puede ser un asset o un doble de test.
    /// </summary>
    public sealed class EquipmentService : IEquipmentService
    {
        private readonly ShipConfig _config;
        private readonly IModuleCatalog _catalog;

        public EquipmentService(ShipConfig config, IModuleCatalog catalog)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Current = Loadout.Empty();
            CurrentStats = ShipStatsCalculator.Calculate(_config.BaseStats, Current.Modules);
        }

        public Loadout Current { get; private set; }

        public ShipStats CurrentStats { get; private set; }

        public event Action<ShipStats> StatsChanged;

        public EquipResult Equip(string moduleId)
        {
            if (string.IsNullOrEmpty(moduleId))
            {
                return EquipResult.Failed(EquipFailure.InvalidModule, Current);
            }

            if (!_catalog.TryGet(moduleId, out var module))
            {
                return EquipResult.Failed(EquipFailure.UnknownModule, Current);
            }

            var result = Current.TryEquip(module);
            if (result.IsSuccess) Apply(result.Loadout);
            return result;
        }

        public EquipResult Unequip(SlotType slot)
        {
            var result = Current.Unequip(slot);
            if (result.IsSuccess) Apply(result.Loadout);
            return result;
        }

        private void Apply(Loadout loadout)
        {
            var previous = CurrentStats;
            Current = loadout;
            CurrentStats = ShipStatsCalculator.Calculate(_config.BaseStats, loadout.Modules);

            if (!CurrentStats.ActionPoints.Equals(previous.ActionPoints) ||
                !CurrentStats.Shields.Equals(previous.Shields) ||
                !CurrentStats.Movement.Equals(previous.Movement) ||
                !CurrentStats.Hull.Equals(previous.Hull) ||
                !AbilitiesEqual(CurrentStats, previous))
            {
                StatsChanged?.Invoke(CurrentStats);
            }
        }

        private static bool AbilitiesEqual(ShipStats a, ShipStats b)
        {
            if (a.Abilities.Count != b.Abilities.Count) return false;

            for (var i = 0; i < a.Abilities.Count; i++)
            {
                if (!string.Equals(a.Abilities[i], b.Abilities[i], StringComparison.Ordinal)) return false;
            }

            return true;
        }
    }
}
