using UnityEngine;
using VoidHex.GameLoop.Logic;
using VoidHex.P1.Domain;
using VoidHex.P3.Application;
using VoidHex.P3.Domain;
using VoidHex.P3.Unity;
using P1Coords = VoidHex.P1.Domain.HexCoordinates;

namespace VoidHex.GameLoop.Unity
{
    /// <summary>
    /// Composition root del bucle.
    ///
    /// Es el único punto que conoce las tres piezas a la vez: construye la grilla (P1), arma el
    /// servicio de equipamiento con los assets de P3 y crea la sesión. Nada más en el bucle
    /// referencia implementaciones: la pantalla y el driver hablan con contratos.
    ///
    /// Todo es perezoso para que la suscripción de la vista no dependa del orden Awake/OnEnable.
    /// </summary>
    public sealed class LoopBootstrap : MonoBehaviour
    {
        [Header("Mapa")]
        [SerializeField] private int width = 8;
        [SerializeField] private int height = 6;
        [SerializeField] private Vector2Int startCell = new Vector2Int(1, 1);
        [SerializeField] private Vector2Int turretCell = new Vector2Int(6, 3);

        [Header("Loadout (assets de P3)")]
        [SerializeField] private ShipConfigAsset shipConfig;
        [SerializeField] private ModuleCatalogAsset moduleCatalog;

        [Header("Combate")]
        [SerializeField] private int turretHull = 8;
        [SerializeField] private int turretDamage = 2;

        private GridModel _grid;
        private GameSession _session;
        private IEquipmentService _equipment;
        private IModuleCatalog _catalog;

        public int Width => width;

        public int Height => height;

        public P1Coords StartCell => new P1Coords(startCell.x, startCell.y);

        public P1Coords TurretCell => new P1Coords(turretCell.x, turretCell.y);

        public IGridQueryService Query => Grid;

        public IGridCommands Commands => Grid;

        public IGridStatePublisher Publisher => Grid;

        public IEquipmentService Equipment => _equipment ??= BuildEquipment();

        /// <summary>Catálogo visible para el hangar: la pantalla lista los módulos sin conocer los assets.</summary>
        public IModuleCatalog Catalog => _catalog ??= (moduleCatalog != null
            ? (IModuleCatalog)moduleCatalog
            : InMemoryCatalog.CreateDefault());

        public GameSession Session => _session ??= BuildSession();

        private GridModel Grid
        {
            get
            {
                if (_grid != null) return _grid;

                _grid = new GridModel(width, height);
                ApplyTerrain(_grid);
                return _grid;
            }
        }

        /// <summary>
        /// Terreno fijo, sin aleatoriedad: el mapa tiene que ser el mismo en cada corrida para que
        /// el test de "misma entrada, mismo resultado" siga siendo cierto en el bucle también.
        /// </summary>
        private static void ApplyTerrain(GridModel grid)
        {
            grid.SetCell(HexCell.Asteroid(new P1Coords(3, 1)));
            grid.SetCell(HexCell.Asteroid(new P1Coords(4, 4)));
            grid.SetCell(HexCell.Asteroid(new P1Coords(5, 1)));
            grid.SetCell(HexCell.Nebula(new P1Coords(2, 4)));
            grid.SetCell(HexCell.Nebula(new P1Coords(5, 4)));
        }

        private IEquipmentService BuildEquipment()
        {
            // Los assets son el camino principal; el catálogo en memoria es el respaldo para que
            // la escena siga siendo jugable si alguien mueve o borra un asset.
            var config = shipConfig != null
                ? shipConfig.ToConfig()
                : new ShipConfig(new ShipBaseStats(6, 0, 3, 10));

            var service = new EquipmentService(config, Catalog);
            service.Equip("laser-mk1");
            service.Equip("thruster-mk1");

            return service;
        }

        private GameSession BuildSession()
        {
            var session = new GameSession(Query, Commands, StartCell, TurretCell, turretHull, turretDamage);
            Debug.Log($"[Loop] Grilla {width}×{height} · nave en {StartCell} · torreta en {TurretCell}.");
            return session;
        }

        /// <summary>
        /// Daño del arma equipada. Lo resuelve quien conoce el loadout (acá), no la sesión:
        /// el bucle recibe números ya decididos.
        /// </summary>
        public int ResolveAttackDamage()
        {
            var tier = 0;

            if (Equipment.Current.TryGetModule(SlotType.Weapon, out var weapon)) tier = weapon.Tier;

            // Sin arma se puede disparar, pero débil: el bucle no se bloquea por un loadout vacío.
            return 2 + tier;
        }

        public ShipStats CurrentStats => Equipment.CurrentStats;
    }
}
