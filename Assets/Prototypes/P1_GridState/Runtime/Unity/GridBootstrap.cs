using System.Collections.Generic;
using UnityEngine;
using VoidHex.P1.Domain;

namespace VoidHex.P1.Unity
{
    /// <summary>
    /// Composition root del prototipo P1. Construye el modelo y expone los dos contratos segregados.
    ///
    /// Las mutaciones de demo viven acá y no en las interfaces: en el juego real las dispara un comando
    /// (capa de aplicación). La escena las usa como si fueran la entrada del jugador.
    /// </summary>
    public sealed class GridBootstrap : MonoBehaviour
    {
        [SerializeField] private int width = 12;
        [SerializeField] private int height = 12;
        [SerializeField] private bool runDemoOnStart = true;

        private GridModel _grid;

        /// <summary>
        /// Creación perezosa: la suscripción de la vista no depende del orden
        /// en que Unity invoca Awake/OnEnable entre componentes del mismo GameObject.
        /// </summary>
        private GridModel Grid => _grid ??= new GridModel(width, height);

        public int Width => width;

        public int Height => height;

        /// <summary>La vista recibe únicamente el contrato de publicación.</summary>
        public IGridStatePublisher Publisher => Grid;

        /// <summary>La IA y el pathfinding reciben únicamente el contrato de lectura.</summary>
        public IGridQueryService Query => Grid;

        public HexCoordinates StartCell { get; private set; } = HexCoordinates.Origin;

        public HexCoordinates GoalCell { get; private set; }

        public bool LastPathFound { get; private set; }

        public int LastPathCost { get; private set; }

        public PathFailure LastFailure { get; private set; } = PathFailure.None;

        public IReadOnlyList<HexCoordinates> LastPath { get; private set; } = new HexCoordinates[0];

        /// <summary>Eventos recibidos por la vista. Demuestra que sin mutación real no hay trabajo.</summary>
        public int ChangeCount { get; private set; }

        private void Start()
        {
            GoalCell = new HexCoordinates(width - 1, height - 3);
            ApplyDefaultTerrain();
            RecomputePath();

            if (runDemoOnStart)
            {
                Debug.Log($"[P1] Ruta {(LastPathFound ? "encontrada" : "fallida: " + LastFailure)} " +
                          $"({LastPath.Count} celdas, costo {LastPathCost}).");
            }
        }

        private void ApplyDefaultTerrain()
        {
            Grid.SetCell(HexCell.Asteroid(new HexCoordinates(3, 3)));
            Grid.SetCell(HexCell.Asteroid(new HexCoordinates(3, 4)));
            Grid.SetCell(HexCell.Nebula(new HexCoordinates(4, 4)));
            Grid.SetCell(HexCell.Nebula(new HexCoordinates(5, 3)));
        }

        /// <summary>Bloquea una celda (simula el efecto de un comando de terreno).</summary>
        public bool DemoBlock(HexCoordinates cell) => Grid.SetCell(HexCell.Asteroid(cell));

        /// <summary>Libera una celda bloqueada.</summary>
        public bool DemoOpen(HexCoordinates cell) => Grid.SetCell(HexCell.Open(cell));

        /// <summary>
        /// Reescribe una celda con su mismo valor. Debe devolver false y NO emitir evento:
        /// es la demostración en vivo de que el render es reactivo y no polling.
        /// </summary>
        public bool DemoRewriteSame(HexCoordinates cell)
        {
            if (!Grid.TryGetCell(cell, out var current)) return false;
            return Grid.SetCell(current);
        }

        /// <summary>Ocupa o libera una celda, para mostrar que la ocupación también notifica.</summary>
        public bool DemoToggleOccupied(HexCoordinates cell)
        {
            if (!Grid.TryGetCell(cell, out var current)) return false;
            return Grid.SetOccupied(cell, !current.IsOccupied);
        }

        public void RecomputePath()
        {
            var pathfinder = new Pathfinder(Query);
            var result = pathfinder.FindPath(StartCell, GoalCell, maxCost: 60);

            LastPathFound = result.IsFound;
            LastPathCost = result.TotalCost;
            LastFailure = result.Failure;
            LastPath = result.Path;
        }

        /// <summary>Lo llama la vista al recibir un evento; así el HUD lo muestra sin consultar el modelo.</summary>
        internal void RegisterChange() => ChangeCount++;
    }
}
