using System.Collections.Generic;
using UnityEngine;
using VoidHex.P1.Domain;

namespace VoidHex.P1.Unity
{
    /// <summary>
    /// Composition root del prototipo P1. Construye el modelo, expone los dos contratos segregados
    /// y ejecuta una demo guionada para que la escena muestre el comportamiento sin depender de input.
    /// </summary>
    public sealed class GridBootstrap : MonoBehaviour
    {
        [SerializeField] private int width = 12;
        [SerializeField] private int height = 12;
        [SerializeField] private bool runDemoOnStart = true;

        private GridModel _grid;

        /// <summary>
        /// Se crea de forma perezosa para que la suscripción de la vista no dependa del orden
        /// en que Unity invoca Awake/OnEnable entre componentes del mismo GameObject.
        /// </summary>
        private GridModel Grid => _grid ??= new GridModel(width, height);

        /// <summary>La vista recibe únicamente el contrato de publicación.</summary>
        public IGridStatePublisher Publisher => Grid;

        /// <summary>La IA y el pathfinding reciben únicamente el contrato de lectura.</summary>
        public IGridQueryService Query => Grid;

        private readonly List<HexCoordinates> _demoPath = new List<HexCoordinates>();

        private void Start()
        {
            if (!runDemoOnStart) return;

            var start = HexCoordinates.Origin;
            var goal = new HexCoordinates(width - 1, height - 3);

            // Dos mutaciones reales: la vista debe registrar exactamente dos redibujos.
            Grid.SetCell(HexCell.Asteroid(new HexCoordinates(3, 3)));
            Grid.SetCell(HexCell.Nebula(new HexCoordinates(4, 4)));

            // Escritura sin cambio: NO debe notificar (demostración de reactividad real).
            var unchanged = Grid.GetCell(new HexCoordinates(4, 4));
            var mutated = Grid.SetCell(unchanged);
            Debug.Log($"[P1] Escritura idéntica -> mutó: {mutated} (se espera False y ningún redibujo).");

            var pathfinder = new Pathfinder(Query);
            var result = pathfinder.FindPath(start, goal, maxCost: 40);

            if (!result.IsFound)
            {
                Debug.LogWarning($"[P1] Sin ruta: {result.Failure}");
                return;
            }

            _demoPath.Clear();
            _demoPath.AddRange(result.Path);
            Debug.Log($"[P1] Ruta encontrada: {result.Path.Count} celdas, costo {result.TotalCost}.");
        }

        private void OnDrawGizmos()
        {
            var size = 0.9f;
            for (var q = 0; q < width; q++)
            {
                for (var r = 0; r < height; r++)
                {
                    var coordinates = new HexCoordinates(q, r);
                    if (!Grid.TryGetCell(coordinates, out var cell)) continue;

                    Gizmos.color = ColorFor(cell);
                    Gizmos.DrawCube(ToWorld(coordinates), new Vector3(size, 0.05f, size));
                }
            }

            Gizmos.color = Color.cyan;
            for (var i = 1; i < _demoPath.Count; i++)
            {
                Gizmos.DrawLine(ToWorld(_demoPath[i - 1]) + Vector3.up * 0.1f,
                    ToWorld(_demoPath[i]) + Vector3.up * 0.1f);
            }
        }

        private static Color ColorFor(HexCell cell)
        {
            if (cell.IsOccupied) return Color.magenta;
            if (!cell.IsPassable) return new Color(0.4f, 0.2f, 0.1f);
            if (cell.MovementCost > 1) return new Color(0.2f, 0.3f, 0.6f);
            return new Color(0.15f, 0.2f, 0.25f);
        }

        /// <summary>Conversión axial a mundo solo para dibujar: la lógica no conoce Unity.</summary>
        private static Vector3 ToWorld(HexCoordinates coordinates)
        {
            var x = 1.5f * coordinates.Q;
            var z = 1.7320508f * (coordinates.R + coordinates.Q * 0.5f);
            return new Vector3(x, 0f, z);
        }
    }
}
