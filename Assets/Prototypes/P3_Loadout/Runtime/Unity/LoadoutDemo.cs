using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VoidHex.P3.Domain;

namespace VoidHex.P3.Unity
{
    /// <summary>
    /// Consumidor inyectado. No hay <c>GameManager.Instance</c> ni búsqueda de servicios:
    /// las dependencias llegan por constructor desde el LifetimeScope.
    /// </summary>
    public sealed class LoadoutDemo : MonoBehaviour
    {
        [SerializeField] private bool runDemoOnStart = true;

        private IEquipmentService _service;
        private IModuleCatalog _catalog;

        public IEquipmentService Service => _service;

        public IReadOnlyList<ModuleDefinition> AvailableModules =>
            _catalog != null ? _catalog.All : (IReadOnlyList<ModuleDefinition>)new ModuleDefinition[0];

        [Inject]
        public void Construct(IEquipmentService service, IModuleCatalog catalog)
        {
            _service = service;
            _catalog = catalog;
        }

        private void Start()
        {
            if (_service == null)
            {
                Debug.LogError("[P3] IEquipmentService no fue inyectado: revisar EquipmentLifetimeScope.");
                return;
            }

            _service.StatsChanged += LogStats;

            if (runDemoOnStart) RunDemo();
        }

        private void OnDestroy()
        {
            if (_service != null) _service.StatsChanged -= LogStats;
        }

        /// <summary>Secuencia de demostración: dos láseres (el MK II reemplaza al MK I) y un propulsor.</summary>
        public void RunDemo()
        {
            Equip("laser-mk1");
            Equip("laser-mk2");
            Equip("thruster-mk1");
            Equip("shield-mk1");
        }

        public EquipResult Equip(string moduleId) => _service.Equip(moduleId);

        public EquipResult Unequip(SlotType slot) => _service.Unequip(slot);

        public bool TryGetEquipped(SlotType slot, out ModuleDefinition module) =>
            _service.Current.TryGetModule(slot, out module);

        private static void LogStats(ShipStats stats)
        {
            Debug.Log($"[P3] Stats: PA {stats.ActionPoints}, escudos {stats.Shields}, " +
                      $"movimiento {stats.Movement}, habilidades [{string.Join(", ", stats.Abilities)}]");
        }
    }
}
