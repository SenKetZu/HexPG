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

        private static readonly Color OpenColor = new Color(0.15f, 0.20f, 0.28f);
        private static readonly Color SlowColor = new Color(0.26f, 0.20f, 0.45f);
        private static readonly Color OccupiedColor = new Color(0.20f, 0.45f, 0.55f);

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

        private void BuildGrid()
        {
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

            var model = PropFor(cell);

            if (_props.TryGetValue(coordinates, out var existing))
            {
                if (model == null) return;

                Destroy(existing);
                _props.Remove(coordinates);
            }

            if (model == null) return;

            var world = HexProjection.ToWorld(coordinates.Q, coordinates.R);
            var holder = new GameObject("prop" + coordinates);
            holder.transform.SetParent(_root, false);
            holder.transform.localPosition = new Vector3(world.X, tileHeight, world.Z);
            holder.transform.localRotation = Quaternion.Euler(0f, Hash(coordinates) % 360, 0f);

            // Los modelos se escalan respecto del ancho del hexágono, no de una caja cuadrada.
            var spawned = ModelLibrary.Spawn(model, holder.transform, TileRadius * 1.15f, TintFor(cell));
            if (spawned == null)
            {
                Destroy(holder);
                return;
            }

            _props[coordinates] = holder;
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

        private static Color TintFor(HexCell cell)
        {
            return cell.Terrain switch
            {
                CellTerrain.Asteroid => new Color(0.48f, 0.36f, 0.28f),
                CellTerrain.Nebula => new Color(0.42f, 0.48f, 0.78f),
                _ => Color.white,
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
