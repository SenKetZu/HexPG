using System;
using System.Collections.Generic;

namespace VoidHex.P1.Domain
{
    /// <summary>
    /// A* sobre una grilla hexagonal de costo variable.
    /// Depende solo de <see cref="IGridQueryService"/>: no puede mutar el mapa ni notificar a la vista (ISP).
    /// El desempate de la frontera es determinista (f, luego orden de inserción).
    /// </summary>
    public sealed class Pathfinder
    {
        private readonly IGridQueryService _grid;

        public Pathfinder(IGridQueryService grid)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
        }

        public PathResult FindPath(HexCoordinates start, HexCoordinates goal, int maxCost)
        {
            if (maxCost < 0) throw new ArgumentOutOfRangeException(nameof(maxCost));

            if (!_grid.Contains(start)) return PathResult.Failed(PathFailure.InvalidStart);
            if (!_grid.Contains(goal)) return PathResult.Failed(PathFailure.InvalidGoal);

            if (start.Equals(goal)) return PathResult.Success(new[] { start }, 0);
            if (!_grid.IsPassable(goal)) return PathResult.Failed(PathFailure.Unreachable);

            var open = new List<FrontierNode>();
            var bestCost = new Dictionary<HexCoordinates, int> { [start] = 0 };
            var cameFrom = new Dictionary<HexCoordinates, HexCoordinates>();
            var closed = new HashSet<HexCoordinates>();
            var insertionOrder = 0;

            open.Add(new FrontierNode(start, 0, HexCoordinates.Distance(start, goal), insertionOrder++));

            while (open.Count > 0)
            {
                var current = TakeBest(open);
                if (current.Coordinates.Equals(goal))
                {
                    return BuildResult(current.Coordinates, cameFrom, current.Cost, maxCost);
                }

                if (!closed.Add(current.Coordinates)) continue;

                for (var i = 0; i < HexCoordinates.Directions.Count; i++)
                {
                    var neighbor = current.Coordinates + HexCoordinates.Directions[i];
                    if (closed.Contains(neighbor) || !_grid.IsPassable(neighbor)) continue;

                    var tentative = current.Cost + _grid.MovementCostToEnter(neighbor);
                    if (bestCost.TryGetValue(neighbor, out var known) && tentative >= known) continue;

                    bestCost[neighbor] = tentative;
                    cameFrom[neighbor] = current.Coordinates;
                    open.Add(new FrontierNode(
                        neighbor,
                        tentative,
                        HexCoordinates.Distance(neighbor, goal),
                        insertionOrder++));
                }
            }

            return PathResult.Failed(PathFailure.Unreachable);
        }

        private static PathResult BuildResult(
            HexCoordinates goal,
            Dictionary<HexCoordinates, HexCoordinates> cameFrom,
            int totalCost,
            int maxCost)
        {
            if (totalCost > maxCost) return PathResult.Failed(PathFailure.BudgetExceeded);

            var path = new List<HexCoordinates> { goal };
            var cursor = goal;
            while (cameFrom.TryGetValue(cursor, out var previous))
            {
                path.Add(previous);
                cursor = previous;
            }

            path.Reverse();
            return PathResult.Success(path, totalCost);
        }

        private static FrontierNode TakeBest(List<FrontierNode> open)
        {
            var bestIndex = 0;
            for (var i = 1; i < open.Count; i++)
            {
                var candidate = open[i];
                var best = open[bestIndex];
                if (candidate.Estimated < best.Estimated ||
                    (candidate.Estimated == best.Estimated && candidate.Order < best.Order))
                {
                    bestIndex = i;
                }
            }

            var result = open[bestIndex];
            open.RemoveAt(bestIndex);
            return result;
        }

        private readonly struct FrontierNode
        {
            public HexCoordinates Coordinates { get; }
            public int Cost { get; }
            public int Estimated { get; }
            public int Order { get; }

            public FrontierNode(HexCoordinates coordinates, int cost, int heuristic, int order)
            {
                Coordinates = coordinates;
                Cost = cost;
                Estimated = cost + heuristic;
                Order = order;
            }
        }
    }
}
