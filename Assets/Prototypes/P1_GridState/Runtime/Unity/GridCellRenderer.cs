using System.Collections.Generic;
using UnityEngine;
using VoidHex.P1.Domain;
using VoidHex.UiKit;

namespace VoidHex.P1.Unity
{
    /// <summary>
    /// Vista 3D del mapa con modelos reales (Kenney Space Kit) en lugar de cubos.
    ///
    /// Sigue siendo una vista estrictamente reactiva: se suscribe a <see cref="IGridStatePublisher"/>
    /// y solo reconstruye la celda que cambió. No hay <c>Update()</c>, no consulta el modelo por frame
    /// y no puede mutarlo (recibe únicamente el contrato de publicación y de lectura).
    /// </summary>
    public sealed class GridCellRenderer : MonoBehaviour
    {
        [SerializeField] private GridBootstrap bootstrap;
        [SerializeField] private float tileHeight = 0.08f;

        /// <summary>Separación entre celdas. La usa también el generador de escenas para encuadrar.</summary>
        public const float Spacing = 0.7f;

        /// <summary>
        /// Radio del hexágono. Con radio = separación las casillas se tocan; 4% menos deja la junta.
        /// La malla es flat-top con ancho 2R, la misma orientación que la proyección axial.
        /// </summary>
        public const float TileRadius = Spacing * 0.96f;

        private readonly Dictionary<HexCoordinates, CellView> _views = new Dictionary<HexCoordinates, CellView>();
        private IGridStatePublisher _publisher;
        private Transform _root;

        private static readonly Color OpenColor = new Color(0.16f, 0.22f, 0.30f);
        private static readonly Color SlowColor = new Color(0.26f, 0.20f, 0.45f);
        private static readonly Color OccupiedColor = new Color(0.62f, 0.32f, 0.90f);

        private Material _openMaterial;
        private Material _slowMaterial;
        private Material _occupiedMaterial;

        /// <summary>Cada celda tiene su losa y, si corresponde, un propietario (roca, nave).</summary>
        private sealed class CellView
        {
            public Transform Tile;
            public GameObject Prop;
            public CellTerrain PropTerrain;
            public bool PropOccupied;
        }

        private void OnEnable()
        {
            if (bootstrap == null) bootstrap = GetComponent<GridBootstrap>();
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
            _root = new GameObject("Cells").transform;
            _root.SetParent(transform, false);

            var tileMaterial = ModelLibrary.CreateLitMaterial(OpenColor);

            _openMaterial = tileMaterial;
            _slowMaterial = ModelLibrary.CreateLitMaterial(SlowColor, emission: 0.55f);
            _occupiedMaterial = ModelLibrary.CreateLitMaterial(OccupiedColor, emission: 0.35f);

            for (var q = 0; q < bootstrap.Width; q++)
            {
                for (var r = 0; r < bootstrap.Height; r++)
                {
                    var cell = new HexCoordinates(q, r);

                    var tile = HexMesh.CreateTile("tile" + cell, _root, TileRadius, tileHeight, tileMaterial);
                    tile.transform.localPosition = ToWorld(cell);

                    var view = new CellView { Tile = tile.transform };
                    _views[cell] = view;

                    if (bootstrap.Query.TryGetCell(cell, out var data)) Render(view, cell, data);
                }
            }
        }

        private void HandleChanged(GridChange change)
        {
            if (!_views.TryGetValue(change.Coordinates, out var view)) return;
            if (!bootstrap.Query.TryGetCell(change.Coordinates, out var cell)) return;

            // Solo la celda afectada: ni una más.
            Render(view, change.Coordinates, cell);
        }

        /// <summary>
        /// Representa el estado de una celda: color de losa + propietario 3D.
        /// El propietario se recrea únicamente cuando cambió su tipo, no en cada evento.
        /// </summary>
        private void Render(CellView view, HexCoordinates coordinates, HexCell cell)
        {
            // La losa comunica el terreno: opaca si es libre, con emisión si es hazardous u ocupada.
            var material = cell.IsOccupied
                ? _occupiedMaterial
                : !cell.IsPassable
                    ? _openMaterial
                    : cell.MovementCost > 1
                        ? _slowMaterial
                        : _openMaterial;

            view.Tile.GetComponent<Renderer>().sharedMaterial = material;

            var desired = PropFor(cell);
            if (view.Prop != null && view.PropTerrain == cell.Terrain && view.PropOccupied == cell.IsOccupied) return;

            SafeObjects.Destroy(view.Prop);

            view.Prop = desired == null ? null : SpawnProp(desired, coordinates);
            view.PropTerrain = cell.Terrain;
            view.PropOccupied = cell.IsOccupied;
        }

        /// <summary>Qué modelo corresponde al estado. La lógica no conoce ninguno de estos nombres.</summary>
        private static string PropFor(HexCell cell)
        {
            if (cell.IsOccupied) return ModelLibrary.ShipSpeederB;

            return cell.Terrain switch
            {
                CellTerrain.Asteroid => (Hash(cell.Coordinates) % 3) switch
                {
                    0 => ModelLibrary.RockMeteor,
                    1 => ModelLibrary.RockMeteorDetailed,
                    _ => ModelLibrary.RockLargeA,
                },
                CellTerrain.Nebula => (Hash(cell.Coordinates) % 2) == 0
                    ? ModelLibrary.RockDebris
                    : ModelLibrary.RockCrystals,
                _ => null,
            };
        }

        /// <summary>
        /// Algunos bloques sobresalen de la losa: los asteroides se leen como obstáculo, no como adorno.
        /// El resto queda a la altura del tablero para no tapar las casillas vecinas.
        ///
        /// El modelo conserva la paleta del kit. La casilla ocupada ya se distingue por el color de
        /// su losa; repintar el acento del modelo (en la nave, el trim de las alas) lo único que
        /// hacía era borrar la lectura de la nave.
        /// </summary>
        private GameObject SpawnProp(string modelName, HexCoordinates coordinates)
        {
            var holder = new GameObject("prop" + coordinates);
            holder.transform.SetParent(_root, false);
            holder.transform.localPosition = ToWorld(coordinates);

            // Variación determinista: mismo mapa, misma escena. Nunca Random sin semilla.
            var seed = Hash(coordinates);
            var rotation = Quaternion.Euler(0f, seed % 360, 0f);

            var size = modelName.StartsWith("Rock_Meteor") || modelName == ModelLibrary.RockLargeA
                ? TileRadius * 1.9f
                : TileRadius * 1.6f;

            var instance = ModelLibrary.Spawn(modelName, holder.transform, size);
            if (instance == null) return holder;

            // La roca del kit viene en arena (rockTrack es el color del suelo de un planeta). Sobre
            // el tablero espacial se lee como una mancha pastel, así que se reestila a gris pizarra.
            // El cristal de la nebulosa conserva su teal: no matchea la clave "rock".
            ModelLibrary.RestyleRocks(instance, ModelLibrary.AsteroidColor);

            holder.transform.localRotation = rotation;
            return holder;
        }

        /// <summary>Hash estable por celda: variación visual sin aleatoriedad no reproducible.</summary>
        private static int Hash(HexCoordinates cell)
        {
            unchecked
            {
                return (cell.Q * 73856093) ^ (cell.R * 19349663);
            }
        }

        /// <summary>Axial a mundo: solo para dibujar, la lógica no conoce Unity.</summary>
        public static Vector3 ToWorld(HexCoordinates cell, float y = 0f)
        {
            return new Vector3(1.5f * cell.Q * Spacing, y,
                1.7320508f * (cell.R + cell.Q * 0.5f) * Spacing);
        }
    }
}
