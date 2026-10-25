using UnityEngine;
using VContainer;
using VoidHex.P3.Domain;

namespace VoidHex.P3.Unity
{
    /// <summary>
    /// Consumidor inyectado. No hay GameManager.Instance ni búsqueda de servicios:
    /// la dependencia llega por constructor desde el LifetimeScope.
    /// </summary>
    public sealed class LoadoutDemo : MonoBehaviour
    {
        private IEquipmentService _service;

        [Inject]
        public void Construct(IEquipmentService service)
        {
            _service = service;
        }

        private void Start()
        {
            if (_service == null)
            {
                Debug.LogError("[P3] IEquipmentService no fue inyectado: revisar EquipmentLifetimeScope.");
                return;
            }

            _service.StatsChanged += stats => Debug.Log(
                $"[P3] Stats: PA {stats.ActionPoints}, escudos {stats.Shields}, movimiento {stats.Movement}, " +
                $"habilidades [{string.Join(", ", stats.Abilities)}]");

            _service.Equip("laser-mk1");
            _service.Equip("laser-mk2");
            _service.Equip("thruster-mk1");
            _service.Equip("shield-mk1");
        }
    }
}
