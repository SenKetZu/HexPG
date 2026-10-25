using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VoidHex.P3.Domain;

namespace VoidHex.P3.Unity
{
    /// <summary>
    /// Consumidor inyectado. No hay <c>GameManager.Instance</c> ni búsqueda de servicios:
    /// las dependencias llegan por constructor desde el LifetimeScope.
    ///
    /// Expone <see cref="Ready"/> porque la inyección ocurre cuando el LifetimeScope construye el
    /// contenedor, y eso puede pasar antes o después del <c>Start</c> de otras vistas. Un evento
    /// elimina la dependencia del orden de inicialización.
    /// </summary>
    public sealed class LoadoutDemo : MonoBehaviour
    {
        [SerializeField] private bool runDemoOnStart = true;

        private IEquipmentService _service;
        private IModuleCatalog _catalog;

        public IEquipmentService Service => _service;

        public IModuleCatalog Catalog => _catalog;

        public bool IsReady => _service != null;

        /// <summary>Se emite cuando el contenedor inyecta las dependencias.</summary>
        public event Action<IEquipmentService> Ready;

        public IReadOnlyList<ModuleDefinition> AvailableModules =>
            _catalog != null ? _catalog.All : (IReadOnlyList<ModuleDefinition>)Array.Empty<ModuleDefinition>();

        [Inject]
        public void Construct(IEquipmentService service, IModuleCatalog catalog)
        {
            _service = service;
            _catalog = catalog;
            Ready?.Invoke(service);
        }

        private void Start()
        {
            if (_service == null)
            {
                Debug.LogError("[P3] IEquipmentService no fue inyectado: revisar EquipmentLifetimeScope.");
                return;
            }

            if (runDemoOnStart) RunDemo();
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
    }
}
