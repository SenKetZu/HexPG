using System;
using System.Collections.Generic;
using UnityEngine;
using VoidHex.P1.Domain;

namespace VoidHex.P1.Unity
{
    /// <summary>
    /// Composition root del prototipo P1. Construye el modelo y expone TRES contratos segregados:
    /// lectura (<see cref="IGridQueryService"/>), notificación (<see cref="IGridStatePublisher"/>)
    /// y escritura (<see cref="IGridCommands"/>).
    ///
    /// La pantalla recibe los tres; el pathfinding recibe solo el de lectura. Esa es la garantía de ISP.
    /// </summary>
    public sealed class GridBootstrap : MonoBehaviour
    {
        [SerializeField] private int width = 12;
        [SerializeField] private int height = 12;
        [SerializeField] private bool runDemoOnStart = true;

        private GridModel _grid;

        /// <summary>
        /// Creación perezosa: la suscripción no depende del orden Awake/OnEnable entre componentes.
        /// </summary>
        private GridModel Grid
        {
            get
            {
                if (_grid != null) return _grid;

                _grid = new GridModel(width, height);
                _grid.Changed += HandleChanged;
                return _grid;
            }
        }

        public int Width => width;

        public int Height => height;

        public IGridQueryService Query => Grid;

        public IGridStatePublisher Publisher => Grid;

        public IGridCommands Commands => Grid;

        public HexCoordinates StartCell { get; private set; } = HexCoordinates.Origin;

        public HexCoordinates GoalCell { get; private set; }

        public bool LastPathFound { get; private set; }

        public int LastPathCost { get; private set; }

        public PathFailure LastFailure { get; private set; } = PathFailure.None;

        public IReadOnlyList<HexCoordinates> LastPath { get; private set; } = new HexCoordinates[0];

        /// <summary>Eventos que emitió el modelo. Si no hubo mutación real, no sube.</summary>
        public int ChangeCount { get; private set; }

        public GridChange LastChange { get; private set; }

        /// <summary>Se reemite para que la pantalla refresque sin consultar por frame.</summary>
        public event Action<GridChange> Changed;

        /// <summary>Se emite cuando cambia la ruta (recalculo explícito).</summary>
        public event Action PathRecomputed;

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

        private void HandleChanged(GridChange change)
        {
            ChangeCount++;
            LastChange = change;
            Changed?.Invoke(change);
        }

        private void ApplyDefaultTerrain()
        {
            Commands.SetCell(HexCell.Asteroid(new HexCoordinates(3, 3)));
            Commands.SetCell(HexCell.Asteroid(new HexCoordinates(3, 4)));
            Commands.SetCell(HexCell.Nebula(new HexCoordinates(4, 4)));
            Commands.SetCell(HexCell.Nebula(new HexCoordinates(5, 3)));
        }

        // ------------------------------------------------------------------ demo

        public bool Block(HexCoordinates cell) => Commands.SetCell(HexCell.Asteroid(cell));

        public bool Free(HexCoordinates cell) => Commands.SetCell(HexCell.Open(cell));

        public bool ToggleOccupied(HexCoordinates cell)
        {
            if (!Query.TryGetCell(cell, out var current)) return false;
            return Commands.SetOccupied(cell, !current.IsOccupied);
        }

        /// <summary>
        /// Reescribe una celda con su mismo valor. Devuelve false y NO emite evento:
        /// es la prueba en vivo de que el render es reactivo y no polling.
        /// </summary>
        public bool RewriteSame(HexCoordinates cell)
        {
            if (!Query.TryGetCell(cell, out var current)) return false;
            return Commands.SetCell(current);
        }

        public void RecomputePath()
        {
            var pathfinder = new Pathfinder(Query);
            var result = pathfinder.FindPath(StartCell, GoalCell, maxCost: 60);

            LastPathFound = result.IsFound;
            LastPathCost = result.TotalCost;
            LastFailure = result.Failure;
            LastPath = result.Path;

            PathRecomputed?.Invoke();
        }
    }
}
