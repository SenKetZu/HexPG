using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VoidHex.P3.Application;
using VoidHex.P3.Domain;
using VoidHex.P3.Unity;

namespace VoidHex.P3.Tests
{
    /// <summary>
    /// Tests del prototipo P3. Cubren la composición de estadísticas (dominio), la inyección
    /// de un catálogo falso (DIP) y el mapeo del adaptador ScriptableObject (integración).
    /// </summary>
    public sealed class LoadoutTests
    {
        private static readonly ShipBaseStats BaseStats = new ShipBaseStats(
            actionPoints: 6, shields: 0, movement: 3, hull: 10);

        [Test]
        public void TryEquip_WithUnknownSlotType_ReturnsWrongSlotTypeAndKeepsLoadout()
        {
            var loadout = Loadout.Empty();
            var invalid = new ModuleDefinition("bad", "Bad", (SlotType)42, 1, 1, 0, 0, string.Empty);

            var result = loadout.TryEquip(invalid);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Failure, Is.EqualTo(EquipFailure.WrongSlotType));
            Assert.That(result.Loadout, Is.SameAs(loadout), "El loadout original no debe reemplazarse ante un fallo.");
            Assert.That(loadout.Modules, Is.Empty);
        }

        [Test]
        public void TryEquip_ReturnsNewLoadout_WithoutMutatingTheOriginal()
        {
            var empty = Loadout.Empty();

            var result = empty.TryEquip(LaserMk1);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(empty.Modules, Is.Empty, "El loadout original debe permanecer inmutable.");
            Assert.That(result.Loadout.Modules, Has.Count.EqualTo(1));
        }

        [Test]
        public void Calculate_AddsAllBonusesToBaseStats()
        {
            var loadout = Loadout.Empty()
                .TryEquip(LaserMk1).Loadout
                .TryEquip(ThrusterMk1).Loadout;

            var stats = ShipStatsCalculator.Calculate(BaseStats, loadout.Modules);

            Assert.That(stats.ActionPoints, Is.EqualTo(6));
            Assert.That(stats.Shields, Is.EqualTo(0));
            Assert.That(stats.Movement, Is.EqualTo(4));
            Assert.That(stats.Hull, Is.EqualTo(10));
            CollectionAssert.AreEqual(new[] { "beam" }, stats.Abilities);
        }

        [Test]
        public void TryEquip_ReplacingModule_DoesNotDoubleCount()
        {
            var loadout = Loadout.Empty().TryEquip(LaserMk2).Loadout;

            var stats = ShipStatsCalculator.Calculate(BaseStats, loadout.Modules);

            Assert.That(stats.Movement, Is.EqualTo(3));
            Assert.That(stats.ActionPoints, Is.EqualTo(6));
            Assert.That(stats.Shields, Is.EqualTo(2));
            Assert.That(loadout.Modules, Has.Count.EqualTo(1), "El reemplazo no debe acumular módulos.");
        }

        [Test]
        public void Unequip_RemovesExactBonuses()
        {
            var equipped = Loadout.Empty().TryEquip(LaserMk1).Loadout;

            var result = equipped.Unequip(SlotType.Weapon);
            var stats = ShipStatsCalculator.Calculate(BaseStats, result.Loadout.Modules);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Loadout.TryGetModule(SlotType.Weapon, out _), Is.False);
            CollectionAssert.AreEqual(new[] { "beam" }, ShipStatsCalculator.Calculate(BaseStats, equipped.Modules).Abilities);
            CollectionAssert.IsEmpty(stats.Abilities);
            Assert.That(stats.Movement, Is.EqualTo(BaseStats.Movement));
        }

        [Test]
        public void Calculate_IsIdempotent_AndDoesNotMutateInputs()
        {
            var modules = new List<ModuleDefinition> { LaserMk1, ThrusterMk1 };

            var first = ShipStatsCalculator.Calculate(BaseStats, modules);
            var second = ShipStatsCalculator.Calculate(BaseStats, modules);

            Assert.That(second.ActionPoints, Is.EqualTo(first.ActionPoints));
            Assert.That(second.Shields, Is.EqualTo(first.Shields));
            Assert.That(second.Movement, Is.EqualTo(first.Movement));
            CollectionAssert.AreEqual(first.Abilities, second.Abilities);
            Assert.That(modules, Has.Count.EqualTo(2), "El cálculo no debe alterar la colección de entrada.");
            Assert.That(BaseStats.ActionPoints, Is.EqualTo(6));
        }

        [Test]
        public void EquipmentService_WithInjectedFakeCatalog_ResolvesModulesById()
        {
            var catalog = new FakeCatalog(LaserMk1);
            var service = new EquipmentService(new ShipConfig(BaseStats), catalog);

            var result = service.Equip("laser-mk1");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(service.CurrentStats.Abilities, Has.Count.EqualTo(1));
            Assert.That(catalog.Queries, Is.EqualTo(1), "El servicio debe consultar el catálogo inyectado.");
        }

        [Test]
        public void EquipmentService_UnknownModule_KeepsStatsUnchanged()
        {
            var service = new EquipmentService(new ShipConfig(BaseStats), new FakeCatalog(LaserMk1));
            var before = service.CurrentStats;

            var result = service.Equip("does-not-exist");

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Failure, Is.EqualTo(EquipFailure.UnknownModule));
            Assert.That(service.CurrentStats.ActionPoints, Is.EqualTo(before.ActionPoints));
            Assert.That(service.Current.Modules, Is.Empty);
        }

        [Test]
        public void ShipModuleAsset_MapsToDefinition_AllFields()
        {
            var asset = ScriptableObject.CreateInstance<ShipModuleAsset>();
            try
            {
                asset.Initialize("laser-mk3", "Láser MK III", SlotType.Weapon, 3, 1, 2, 0, "beam-iii");

                var definition = asset.ToDefinition();

                Assert.That(definition.Id, Is.EqualTo("laser-mk3"));
                Assert.That(definition.DisplayName, Is.EqualTo("Láser MK III"));
                Assert.That(definition.Slot, Is.EqualTo(SlotType.Weapon));
                Assert.That(definition.Tier, Is.EqualTo(3));
                Assert.That(definition.BonusActionPoints, Is.EqualTo(1));
                Assert.That(definition.BonusShields, Is.EqualTo(2));
                Assert.That(definition.BonusMovement, Is.EqualTo(0));
                Assert.That(definition.GrantedAbilityId, Is.EqualTo("beam-iii"));
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        private static ModuleDefinition LaserMk1 => new ModuleDefinition(
            "laser-mk1", "Láser MK I", SlotType.Weapon, 1, 0, 0, 0, "beam");

        private static ModuleDefinition LaserMk2 => new ModuleDefinition(
            "laser-mk2", "Láser MK II", SlotType.Weapon, 2, 0, 2, 0, "beam");

        private static ModuleDefinition ThrusterMk1 => new ModuleDefinition(
            "thruster-mk1", "Propulsor MK I", SlotType.Propulsion, 1, 0, 0, 1, string.Empty);

        private sealed class FakeCatalog : IModuleCatalog
        {
            private readonly Dictionary<string, ModuleDefinition> _modules;

            public FakeCatalog(params ModuleDefinition[] modules)
            {
                _modules = new Dictionary<string, ModuleDefinition>();
                foreach (var module in modules)
                {
                    _modules[module.Id] = module;
                }
            }

            public int Queries { get; private set; }

            public bool TryGet(string id, out ModuleDefinition module)
            {
                Queries++;
                return _modules.TryGetValue(id, out module);
            }

            public IReadOnlyList<ModuleDefinition> All => new List<ModuleDefinition>(_modules.Values);
        }
    }
}
