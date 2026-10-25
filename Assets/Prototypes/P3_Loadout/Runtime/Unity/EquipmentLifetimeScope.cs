using UnityEngine;
using VContainer;
using VContainer.Unity;
using VoidHex.P3.Application;
using VoidHex.P3.Domain;

namespace VoidHex.P3.Unity
{
    /// <summary>
    /// Composition root del prototipo P3. VContainer inyecta las interfaces por constructor:
    /// el servicio no conoce al consumidor y el consumidor no busca dependencias globalmente.
    /// </summary>
    public sealed class EquipmentLifetimeScope : LifetimeScope
    {
        [SerializeField] private ShipConfigAsset shipConfig;
        [SerializeField] private ModuleCatalogAsset moduleCatalog;

        protected override void Configure(IContainerBuilder builder)
        {
            if (shipConfig == null)
            {
                throw new MissingReferenceException("EquipmentLifetimeScope requiere un ShipConfigAsset asignado.");
            }

            if (moduleCatalog == null)
            {
                throw new MissingReferenceException("EquipmentLifetimeScope requiere un ModuleCatalogAsset asignado.");
            }

            builder.RegisterInstance<IModuleCatalog>(moduleCatalog);
            builder.RegisterInstance(shipConfig.ToConfig());
            builder.Register<EquipmentService>(Lifetime.Singleton).As<IEquipmentService>();
            builder.RegisterComponentInHierarchy<LoadoutDemo>();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Asignación explícita usada por el generador de escenas. Se prefiere a SerializedObject porque
        /// no depende del nombre del campo serializado: si alguien lo renombra, esto sigue compilando.
        /// </summary>
        public void EditorAssign(ShipConfigAsset configAsset, ModuleCatalogAsset catalogAsset)
        {
            shipConfig = configAsset;
            moduleCatalog = catalogAsset;
        }

        public bool EditorIsConfigured => shipConfig != null && moduleCatalog != null;
#endif
    }
}
