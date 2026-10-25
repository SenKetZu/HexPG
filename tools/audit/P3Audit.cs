using System.Collections.Generic;
using VoidHex.P3.Application;
using VoidHex.P3.Domain;

namespace VoidHex.Audit
{
    /// <summary>Verifica P3 contra las aserciones de LoadoutTests y la demo de la escena.</summary>
    internal static class P3Audit
    {
        internal static void Run()
        {
            var baseStats = new ShipBaseStats(6, 0, 3, 10);

            // Slot inválido.
            var empty = Loadout.Empty();
            var invalid = new ModuleDefinition("bad", "Bad", (SlotType)42, 1, 1, 0, 0, string.Empty);
            var wrong = empty.TryEquip(invalid);
            Program.Check("slot inválido -> WrongSlotType", wrong.Failure, EquipFailure.WrongSlotType);
            Program.Check("el loadout original se conserva", ReferenceEquals(wrong.Loadout, empty), true);
            Program.Check("el original sigue vacío", empty.Modules.Count, 0);

            // Inmutabilidad.
            var equipped = empty.TryEquip(LaserMk1);
            Program.Check("equipar no muta el original", empty.Modules.Count, 0);
            Program.Check("equipar devuelve 1 módulo", equipped.Loadout.Modules.Count, 1);

            // Suma de bonificaciones.
            var two = Loadout.Empty().TryEquip(LaserMk1).Loadout.TryEquip(Thruster).Loadout;
            var stats = ShipStatsCalculator.Calculate(baseStats, two.Modules);
            Program.Check("movement = 4", stats.Movement, 4);
            Program.Check("escudos = 0", stats.Shields, 0);
            Program.Check("hull = 10", stats.Hull, 10);
            Program.Check("AP = 6", stats.ActionPoints, 6);
            Program.Check("habilidades = 1", stats.Abilities.Count, 1);
            Program.Check("habilidad = beam", stats.Abilities[0], "beam");

            // Reemplazo sin doble conteo.
            var replaced = Loadout.Empty().TryEquip(LaserMk1).Loadout.TryEquip(LaserMk2).Loadout;
            var replacedStats = ShipStatsCalculator.Calculate(baseStats, replaced.Modules);
            Program.Check("reemplazo: módulos = 1", replaced.Modules.Count, 1);
            Program.Check("reemplazo: escudos = 2", replacedStats.Shields, 2);
            Program.Check("reemplazo: movement = 3", replacedStats.Movement, 3);
            Program.Check("reemplazo: AP = 6", replacedStats.ActionPoints, 6);

            // Quitar y ausencia explícita.
            var removed = replaced.Unequip(SlotType.Weapon);
            var removedStats = ShipStatsCalculator.Calculate(baseStats, removed.Loadout.Modules);
            Program.Check("quitar -> habilidades vacías", removedStats.Abilities.Count, 0);
            Program.Check("quitar -> escudos 0", removedStats.Shields, 0);
            Program.Check("slot vacío -> TryGetModule false", removed.Loadout.TryGetModule(SlotType.Weapon, out _), false);
            Program.Check("quitar un slot vacío -> EmptySlot",
                removed.Loadout.Unequip(SlotType.Weapon).Failure, EquipFailure.EmptySlot);

            // Idempotencia y no mutación de la entrada.
            var modules = new List<ModuleDefinition> { LaserMk1, Thruster };
            var a = ShipStatsCalculator.Calculate(baseStats, modules);
            var b = ShipStatsCalculator.Calculate(baseStats, modules);
            Program.Check("idempotente (AP/esc/mov/hab)",
                a.ActionPoints == b.ActionPoints && a.Shields == b.Shields
                && a.Movement == b.Movement && a.Abilities.Count == b.Abilities.Count, true);
            Program.Check("no muta la colección de entrada", modules.Count, 2);
            Program.Check("no muta las stats base", baseStats.ActionPoints, 6);

            // Habilidades compartidas no se duplican.
            var shared = new List<ModuleDefinition> { LaserMk1, LaserMk2 };
            var sharedStats = ShipStatsCalculator.Calculate(baseStats, shared);
            Program.Check("habilidad compartida no se duplica", sharedStats.Abilities.Count, 1);

            // Servicio con catálogo inyectado (DIP).
            var catalog = new FakeCatalog(LaserMk1, LaserMk2, Thruster, Shield);
            var service = new EquipmentService(new ShipConfig(baseStats), catalog);
            var byId = service.Equip("laser-mk1");
            Program.Check("servicio equipa por id", byId.IsSuccess, true);
            Program.Check("servicio: habilidades = 1", service.CurrentStats.Abilities.Count, 1);
            Program.Check("servicio consultó el catálogo", catalog.Queries, 1);

            var unknown = service.Equip("does-not-exist");
            Program.Check("módulo desconocido -> UnknownModule", unknown.Failure, EquipFailure.UnknownModule);
            Program.Check("fallo no altera el loadout", service.Current.Modules.Count, 1);

            var idEmpty = service.Equip(string.Empty);
            Program.Check("id vacío -> InvalidModule", idEmpty.Failure, EquipFailure.InvalidModule);

            // Secuencia real de la escena (LoadoutDemo): 4 equipamientos, pero los dos láseres
            // comparten el slot Weapon, así que el segundo reemplaza al primero: 3 módulos.
            var demo = new EquipmentService(new ShipConfig(baseStats), catalog);
            var events = 0;
            demo.StatsChanged += _ => events++;
            demo.Equip("laser-mk1");
            demo.Equip("laser-mk2");
            demo.Equip("thruster-mk1");
            demo.Equip("shield-mk1");
            Program.Check("demo: 3 módulos (el láser MK II reemplaza al MK I)", demo.Current.Modules.Count, 3);
            Program.Check("demo: 4 eventos (uno por cambio real)", events, 4);
            Program.Note($"stats demo: PA={demo.CurrentStats.ActionPoints} esc={demo.CurrentStats.Shields} " +
                         $"mov={demo.CurrentStats.Movement} hab=[{string.Join(",", demo.CurrentStats.Abilities)}]");

            // Evento no se emite cuando el cambio no altera stats (equipar el mismo módulo dos veces).
            var repeated = new EquipmentService(new ShipConfig(baseStats), catalog);
            var repeatedEvents = 0;
            repeated.StatsChanged += _ => repeatedEvents++;
            repeated.Equip("laser-mk1");
            repeated.Equip("laser-mk1");
            Program.Check("re-equipar lo mismo no emite evento", repeatedEvents, 1);
        }

        private static ModuleDefinition LaserMk1 => new ModuleDefinition(
            "laser-mk1", "Láser MK I", SlotType.Weapon, 1, 0, 0, 0, "beam");

        private static ModuleDefinition LaserMk2 => new ModuleDefinition(
            "laser-mk2", "Láser MK II", SlotType.Weapon, 2, 0, 2, 0, "beam");

        private static ModuleDefinition Thruster => new ModuleDefinition(
            "thruster-mk1", "Propulsor MK I", SlotType.Propulsion, 1, 0, 0, 1, string.Empty);

        private static ModuleDefinition Shield => new ModuleDefinition(
            "shield-mk1", "Escudo MK I", SlotType.Shield, 1, 0, 3, 0, "barrier");

        private sealed class FakeCatalog : IModuleCatalog
        {
            private readonly Dictionary<string, ModuleDefinition> _modules = new Dictionary<string, ModuleDefinition>();

            public FakeCatalog(params ModuleDefinition[] modules)
            {
                foreach (var module in modules) _modules[module.Id] = module;
            }

            public int Queries { get; private set; }

            public bool TryGet(string id, out ModuleDefinition module)
            {
                Queries++;
                return _modules.TryGetValue(id, out module);
            }

            public IReadOnlyList<ModuleDefinition> All
            {
                get
                {
                    var result = new List<ModuleDefinition>(_modules.Values);
                    return result;
                }
            }
        }
    }
}
