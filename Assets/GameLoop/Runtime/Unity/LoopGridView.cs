using System.Collections.Generic;
using UnityEngine;
using VoidHex.GameLoop.Logic;
using VoidHex.P1.Domain;
using VoidHex.UiKit;
using P1Coords = VoidHex.P1.Domain.HexCoordinates;

namespace VoidHex.GameLoop.Unity
{
    /// <summary>
    /// Vista del tablero del bucle.
    ///
    /// Igual que en P1: se suscribe a <see cref="IGridStatePublisher"/> y solo reconstruye la casilla
    /// que cambió. No hay <c>Update()</c>. La diferencia es que acá el terreno también tiene modelos
    /// 3D y las casillas se resaltan para indicar el alcance del movimiento.
    /// </summary>
    public sealed class LoopGridView : MonoBehaviour
    {
        [SerializeField] private LoopBootstrap bootstrap;
        [SerializeField] private float tileHeight = 0.08f;

        private readonly Dictionary<P1Coords, Renderer> _tiles = new Dictionary<P1Coords, Renderer>();
        private readonly Dictionary<P1Coords, GameObject> _props = new Dictionary<P1Coords, GameObject>();

        /// <summary>
        /// Radio del hexágono. Igual a la separación entre centros (Spacing) las casillas se tocan;
        /// se deja un 4% menos para que se vea la junta y cada casilla se lea individualmente.
        /// </summary>
        public const float TileRadius = HexProjection.Spacing * 0.96f;

        private IGridStatePublisher _publisher;
        private Transform _root;
        private Material _openMaterial;
        private Material _slowMaterial;
        private Material _blockedMaterial;
        private Material _occupiedMaterial;

        // --- indicadores de interacción (puramente visuales) ---
        private Transform _hoverOutline;
        private Material _hoverMaterial;
        private Transform _selectedOutline;
        private Material _selectedMaterial;
        private readonly List<Transform> _pathMarkers = new List<Transform>();
        private Material _pathMaterial;
        private Material _pathBlockedMaterial;

        private readonly List<Transform> _rangeMarkers = new List<Transform>();
        private Material _rangeMaterial;
        private Transform _rangeRoot;

        // Aro de peligro por casilla bloqueada: persistente, porque el obstáculo también lo es.
        private readonly Dictionary<P1Coords, Transform> _hazards = new Dictionary<P1Coords, Transform>();
        private Material _hazardMaterial;
        private Transform _hazardRoot;

        private static readonly Color OpenColor = new Color(0.15f, 0.20f, 0.28f);
        private static readonly Color SlowColor = new Color(0.26f, 0.20f, 0.45f);
        private static readonly Color OccupiedColor = new Color(0.20f, 0.45f, 0.55f);
        private static readonly Color HoverColor = new Color(0.35f, 0.95f, 1f);
        private static readonly Color SelectedColor = new Color(0.98f, 0.75f, 0.15f);
        private static readonly Color PathColor = new Color(0.25f, 0.90f, 0.80f);
        private static readonly Color PathBlockedColor = new Color(0.95f, 0.42f, 0.42f);

        /// <summary>Rojo tenue: comunica "desde acá alcanzás la torreta" sin competir con el hover.</summary>
        private static readonly Color RangeColor = new Color(0.62f, 0.22f, 0.20f);

        /// <summary>Naranja de advertencia para casillas bloqueadas.</summary>
        private static readonly Color HazardColor = new Color(1f, 0.62f, 0.20f);

        /// <summary>
        /// Material de asteroide. Gris pizarra: la roca del kit viene en arena, que es el color del
        /// suelo de un planeta y sobre el tablero espacial no se lee como obstáculo.
        /// </summary>
        private static readonly Color RockColor = new Color(0.34f, 0.33f, 0.40f);

        /// <summary>Radio de los marcadores de ruta: más chicos que la casilla para no taparla.</summary>
        private const float PathMarkerRadius = TileRadius * 0.34f;

        private void OnEnable()
        {
            if (bootstrap == null) bootstrap = GetComponent<LoopBootstrap>();
            if (bootstrap == null) return;

            _publisher = bootstrap.Publisher;
            _publisher.Changed += HandleChanged;
            BuildGrid();
        }

        private void OnDisable()
        {
            if (_publisher == null) return;

            _publisher.Changed -= HandleChanged;
            _publisher = null;
        }

        // ------------------------------------------------------------------ indicadores

        /// <summary>
        /// Marca la casilla bajo el mouse. Pasar <c>null</c> lo oculta.
        ///
        /// Es un contorno y no un relleno: el jugador necesita ver el terreno y el propietario de la
        /// casilla, y un hexágono opaco encima los taparía.
        /// </summary>
        public void SetHover(P1Coords? cell, bool reachable)
        {
            if (_hoverOutline == null) BuildIndicators();

            if (!cell.HasValue)
            {
                _hoverOutline.gameObject.SetActive(false);
                return;
            }

            var world = HexProjection.ToWorld(cell.Value.Q, cell.Value.R);
            _hoverOutline.localPosition = new Vector3(world.X, tileHeight + 0.006f, world.Z);
            _hoverOutline.gameObject.SetActive(true);

            _hoverOutline.GetComponent<OutlineTint>()?.Apply(reachable ? HoverColor : PathBlockedColor);
        }

        /// <summary>Dibuja la ruta previsualizada con marcadores sobre cada casilla intermedia.</summary>
        public void SetPath(IReadOnlyList<P1Coords> path, bool reachable)
        {
            if (_hoverOutline == null) BuildIndicators();

            var material = reachable ? _pathMaterial : _pathBlockedMaterial;
            var used = 0;

            if (path != null)
            {
                // Se omite el primer elemento: es la casilla donde ya está la nave.
                for (var i = 1; i < path.Count; i++)
                {
                    var marker = GetMarker(used++);
                    var world = HexProjection.ToWorld(path[i].Q, path[i].R);

                    marker.localPosition = new Vector3(world.X, tileHeight + 0.01f, world.Z);
                    marker.gameObject.SetActive(true);
                    marker.GetComponent<MeshRenderer>().sharedMaterial = material;
                }
            }

            for (var i = used; i < _pathMarkers.Count; i++) _pathMarkers[i].gameObject.SetActive(false);
        }

        private Transform GetMarker(int index)
        {
            while (_pathMarkers.Count <= index)
            {
                var marker = HexMesh.CreateTile("path" + _pathMarkers.Count, transform, PathMarkerRadius,
                    0.01f, _pathMaterial);

                var collider = marker.GetComponent<Collider>();
                SafeObjects.Destroy(collider);

                _pathMarkers.Add(marker.transform);
            }

            return _pathMarkers[index];
        }

        /// <summary>
        /// Marca la casilla que ocupa la nave: es el "seleccionado" del turno.
        /// Persistente, de otro color que el hover, para que ambos se distingan.
        /// </summary>
        public void SetSelectedCell(P1Coords? cell)
        {
            if (_hoverOutline == null) BuildIndicators();

            if (!cell.HasValue)
            {
                _selectedOutline.gameObject.SetActive(false);
                return;
            }

            var world = HexProjection.ToWorld(cell.Value.Q, cell.Value.R);
            _selectedOutline.localPosition = new Vector3(world.X, tileHeight + 0.004f, world.Z);
            _selectedOutline.gameObject.SetActive(true);
        }

        /// <summary>
        /// Pinta la zona desde la que se puede disparar a la torreta.
        ///
        /// Solo se dibuja el PERÍMETRO del área, no cada casilla. Marcar las ~35 casillas del alcance
        /// llenaba el tablero de aros rojos y tapaba justamente el terreno que hay que leer; el borde
        /// comunica el área igual y deja respirar las casillas del medio.
        ///
        /// Las casillas vienen calculadas por la sesión (rango + LOS), la vista no decide nada.
        /// </summary>
        public void SetAttackArea(IReadOnlyList<P1Coords> cells, bool visible)
        {
            if (_hoverOutline == null) BuildIndicators();

            var used = 0;

            if (visible && cells != null && cells.Count > 0)
            {
                var inside = new HashSet<P1Coords>(cells);

                for (var i = 0; i < cells.Count; i++)
                {
                    if (!IsFrontier(cells[i], inside)) continue;

                    var marker = GetRangeMarker(used++);
                    var world = HexProjection.ToWorld(cells[i].Q, cells[i].R);

                    marker.localPosition = new Vector3(world.X, tileHeight + 0.002f, world.Z);
                    marker.gameObject.SetActive(true);
                }
            }

            for (var i = used; i < _rangeMarkers.Count; i++) _rangeMarkers[i].gameObject.SetActive(false);
        }

        /// <summary>
        /// Una casilla es borde si alguno de sus seis vecinos queda fuera del área.
        ///
        /// Con el área completa (todas las casillas interiores) no se dibuja nada, así que se fuerza
        /// el trazo cuando el área es tan chica que no tiene interior.
        /// </summary>
        private static bool IsFrontier(P1Coords cell, HashSet<P1Coords> inside)
        {
            for (var i = 0; i < 6; i++)
            {
                var neighbour = new P1Coords(cell.Q + NeighbourQ[i], cell.R + NeighbourR[i]);
                if (!inside.Contains(neighbour)) return true;
            }

            return false;
        }

        // Direcciones axiales de un hexágono (las mismas seis de la ruta).
        private static readonly int[] NeighbourQ = { 1, 1, 0, -1, -1, 0 };
        private static readonly int[] NeighbourR = { 0, -1, -1, 0, 1, 1 };

        private Transform GetRangeMarker(int index)
        {
            while (_rangeMarkers.Count <= index)
            {
                var marker = HexMesh.CreateOutline("range" + _rangeMarkers.Count, _rangeRoot,
                    TileRadius * 0.90f, 0.02f, 0f, _rangeMaterial);

                _rangeMarkers.Add(marker);
            }

            return _rangeMarkers[index];
        }

        private void BuildIndicators()
        {
            _hoverMaterial = ModelLibrary.CreateLitMaterial(HoverColor, emission: 1.6f);
            _selectedMaterial = ModelLibrary.CreateLitMaterial(SelectedColor, emission: 1.4f);
            _pathMaterial = ModelLibrary.CreateLitMaterial(PathColor, emission: 1.2f);
            _pathBlockedMaterial = ModelLibrary.CreateLitMaterial(PathBlockedColor, emission: 1.0f);

            // Emisión moderada a propósito: por encima de ~0.9 el tonemap ACES lava el naranja hacia
            // rosa, y el peligro terminaba confundiéndose con el área de ataque.
            _rangeMaterial = ModelLibrary.CreateLitMaterial(RangeColor, emission: 0.25f);
            _hazardMaterial = ModelLibrary.CreateLitMaterial(HazardColor, emission: 0.55f);

            _rangeRoot = new GameObject("AttackArea").transform;
            _rangeRoot.SetParent(transform, false);

            _hazardRoot = new GameObject("Hazards").transform;
            _hazardRoot.SetParent(transform, false);

            // El seleccionado va debajo del hover: si coinciden, el hover queda visible encima.
            _selectedOutline = HexMesh.CreateOutline("Selected", transform, TileRadius * 1.04f, 0.05f, 0.004f,
                _selectedMaterial);
            _selectedOutline.gameObject.AddComponent<OutlineTint>().Apply(SelectedColor);
            _selectedOutline.gameObject.SetActive(false);

            _hoverOutline = HexMesh.CreateOutline("Hover", transform, TileRadius * 1.02f, 0.035f, 0.01f,
                _hoverMaterial);
            _hoverOutline.gameObject.AddComponent<OutlineTint>();
            _hoverOutline.gameObject.SetActive(false);
        }

        private void BuildGrid()
        {
            // Los indicadores se construyen ANTES de las losas: Render() corre durante este mismo
            // bucle y necesita el material de peligro. Si se construyen tarde, los aros quedan sin
            // material y Unity los pinta con el magenta de shader faltante.
            if (_hoverOutline == null) BuildIndicators();

            _openMaterial = ModelLibrary.CreateLitMaterial(OpenColor);
            _slowMaterial = ModelLibrary.CreateLitMaterial(SlowColor, emission: 0.5f);
            _blockedMaterial = ModelLibrary.CreateLitMaterial(new Color(0.36f, 0.26f, 0.20f));
            _occupiedMaterial = ModelLibrary.CreateLitMaterial(OccupiedColor, emission: 0.35f);

            _root = new GameObject("Cells").transform;
            _root.SetParent(transform, false);

            for (var q = 0; q < bootstrap.Width; q++)
            {
                for (var r = 0; r < bootstrap.Height; r++)
                {
                    var cell = new P1Coords(q, r);
                    var world = HexProjection.ToWorld(q, r);

                    var tile = HexMesh.CreateTile("tile" + cell, _root, TileRadius, tileHeight, _openMaterial);
                    tile.transform.localPosition = new Vector3(world.X, 0f, world.Z);

                    _tiles[cell] = tile.GetComponent<Renderer>();

                    if (bootstrap.Query.TryGetCell(cell, out var data)) Render(cell, data);
                }
            }
        }

        private void HandleChanged(GridChange change)
        {
            if (!bootstrap.Query.TryGetCell(change.Coordinates, out var cell)) return;

            Render(change.Coordinates, cell);
        }

        private void Render(P1Coords coordinates, HexCell cell)
        {
            if (_tiles.TryGetValue(coordinates, out var renderer))
            {
                renderer.sharedMaterial = cell.IsOccupied
                    ? _occupiedMaterial
                    : !cell.IsPassable
                        ? _blockedMaterial
                        : cell.MovementCost > 1
                            ? _slowMaterial
                            : _openMaterial;
            }

            // Aro naranja: el obstáculo se ve de un vistazo, incluso antes de leer el modelo.
            UpdateHazard(coordinates, !cell.IsPassable);

            var model = PropFor(cell);

            if (_props.TryGetValue(coordinates, out var existing))
            {
                if (model == null) return;

                SafeObjects.Destroy(existing);
                _props.Remove(coordinates);
            }

            if (model == null) return;

            var world = HexProjection.ToWorld(coordinates.Q, coordinates.R);
            var holder = new GameObject("prop" + coordinates);
            holder.transform.SetParent(_root, false);
            holder.transform.localPosition = new Vector3(world.X, tileHeight, world.Z);
            holder.transform.localRotation = Quaternion.Euler(0f, Hash(coordinates) % 360, 0f);

            // Escala: los asteroides son más anchos que una casilla a propósito. Con el tamaño de la
            // casilla quedaban como losas planas de 0,26 de alto y no se leían como obstáculo.
            var isAsteroid = cell.Terrain == CellTerrain.Asteroid;
            var size = isAsteroid ? TileRadius * 1.60f : TileRadius * 1.35f;

            var spawned = ModelLibrary.Spawn(model, holder.transform, size);
            if (spawned == null)
            {
                SafeObjects.Destroy(holder);
                return;
            }

            // La roca se reestila, no se tiñe entera: el kit la pinta en arena (color de suelo de
            // planeta) y sobre el tablero espacial se leía como una mancha salmón. Los cristales de
            // la nebulosa conservan su teal, que sí funciona.
            ModelLibrary.RestyleRocks(spawned, RockColor);

            _props[coordinates] = holder;
        }

        /// <summary>
        /// Crea o destruye el aro de advertencia de una casilla bloqueada.
        ///
        /// Se mantiene por casilla (no se recrea en cada evento) porque el obstáculo es permanente:
        /// un aro por frame sería trabajo gratuito y parpadeo visual.
        /// </summary>
        private void UpdateHazard(P1Coords coordinates, bool hazard)
        {
            if (_hazardMaterial == null) BuildIndicators();

            if (hazard)
            {
                if (_hazards.ContainsKey(coordinates)) return;

                var world = HexProjection.ToWorld(coordinates.Q, coordinates.R);
                var ring = HexMesh.CreateOutline("hazard" + coordinates, _hazardRoot,
                    TileRadius * 0.99f, 0.06f, 0.008f, _hazardMaterial);
                ring.localPosition = new Vector3(world.X, 0f, world.Z);

                _hazards[coordinates] = ring;
                return;
            }

            if (!_hazards.TryGetValue(coordinates, out var existing)) return;

            SafeObjects.Destroy(existing.gameObject);
            _hazards.Remove(coordinates);
        }

        private static string PropFor(HexCell cell)
        {
            // El jugador y la torreta los dibuja ShipView; acá solo el terreno.
            if (cell.IsOccupied) return null;

            return cell.Terrain switch
            {
                CellTerrain.Asteroid => (Hash(cell.Coordinates) % 2) == 0
                    ? ModelLibrary.RockMeteor
                    : ModelLibrary.RockLargeA,
                CellTerrain.Nebula => ModelLibrary.RockCrystals,
                _ => null,
            };
        }

        private static int Hash(P1Coords cell)
        {
            unchecked
            {
                return (cell.Q * 73856093) ^ (cell.R * 19349663);
            }
        }
    }
}
