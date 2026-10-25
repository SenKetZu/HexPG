using System;
using System.Collections.Generic;

namespace VoidHex.P1.Domain
{
    /// <summary>
    /// Único dueño del estado del mapa. Implementa los dos contratos segregados:
    /// la lectura síncrona y la publicación de cambios. Las mutaciones viven SOLO en esta clase,
    /// fuera de ambas interfaces, por lo que ningún consumidor de los contratos puede alterar el mapa.
    /// </summary>
    public sealed class GridModel : IGridQueryService, IGridStatePublisher, IGridCommands
    {
        private readonly Dictionary<HexCoordinates, HexCell> _cells;

        public event Action<GridChange> Changed;

        public GridModel(int width, int height)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

            Width = width;
            Height = height;
            _cells = new Dictionary<HexCoordinates, HexCell>(width * height);

            for (var q = 0; q < width; q++)
            {
                for (var r = 0; r < height; r++)
                {
                    var coordinates = new HexCoordinates(q, r);
                    _cells.Add(coordinates, HexCell.Open(coordinates));
                }
            }
        }

        public int Width { get; }
        public int Height { get; }

        public bool Contains(HexCoordinates coordinates) => _cells.ContainsKey(coordinates);

        public HexCell GetCell(HexCoordinates coordinates)
        {
            if (_cells.TryGetValue(coordinates, out var cell))
            {
                return cell;
            }

            throw new ArgumentOutOfRangeException(
                nameof(coordinates),
                coordinates.ToString(),
                "La celda no pertenece al mapa.");
        }

        public bool TryGetCell(HexCoordinates coordinates, out HexCell cell)
            => _cells.TryGetValue(coordinates, out cell);

        public bool IsPassable(HexCoordinates coordinates)
            => _cells.TryGetValue(coordinates, out var cell) && cell.IsPassable && !cell.IsOccupied;

        public int MovementCostToEnter(HexCoordinates coordinates)
            => _cells.TryGetValue(coordinates, out var cell) ? cell.MovementCost : int.MaxValue;

        public bool HasLineOfSight(HexCoordinates from, HexCoordinates to)
        {
            if (!Contains(from) || !Contains(to)) return false;

            var line = HexLine.Draw(from, to);
            for (var i = 1; i < line.Count - 1; i++)
            {
                if (!_cells.TryGetValue(line[i], out var cell) || cell.BlocksLineOfSight)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Mutación: solo notifica si el estado realmente cambia.</summary>
        public bool SetCell(HexCell cell)
        {
            if (!_cells.TryGetValue(cell.Coordinates, out var current))
            {
                return false;
            }

            if (current.Equals(cell))
            {
                return false;
            }

            _cells[cell.Coordinates] = cell;
            Changed?.Invoke(new GridChange(cell.Coordinates, GridChangeKind.CellChanged));
            return true;
        }

        /// <summary>Mutación: solo notifica si la ocupación realmente cambia.</summary>
        public bool SetOccupied(HexCoordinates coordinates, bool occupied)
        {
            if (!_cells.TryGetValue(coordinates, out var current) || current.IsOccupied == occupied)
            {
                return false;
            }

            _cells[coordinates] = current.WithOccupied(occupied);
            Changed?.Invoke(new GridChange(coordinates, GridChangeKind.OccupationChanged));
            return true;
        }
    }

    /// <summary>Línea de celdas entre dos extremos (Red Blob Games, interpolación cúbica).</summary>
    public static class HexLine
    {
        public static IReadOnlyList<HexCoordinates> Draw(HexCoordinates from, HexCoordinates to)
        {
            var distance = HexCoordinates.Distance(from, to);
            var result = new List<HexCoordinates>(distance + 1);

            if (distance == 0)
            {
                result.Add(from);
                return result;
            }

            for (var i = 0; i <= distance; i++)
            {
                var t = (float)i / distance;
                var q = Lerp(from.Q, to.Q, t);
                var r = Lerp(from.R, to.R, t);
                var s = Lerp(from.S, to.S, t);
                result.Add(Round(q, r, s));
            }

            return result;
        }

        private static float Lerp(int a, int b, float t) => a + (b - a) * t;

        private static HexCoordinates Round(float q, float r, float s)
        {
            var rq = (int)Math.Round(q);
            var rr = (int)Math.Round(r);
            var rs = (int)Math.Round(s);

            var dq = Math.Abs(rq - q);
            var dr = Math.Abs(rr - r);
            var ds = Math.Abs(rs - s);

            if (dq > dr && dq > ds) rq = -rr - rs;
            else if (dr > ds) rr = -rq - rs;

            return new HexCoordinates(rq, rr);
        }
    }
}
