using System.Collections.Generic;

namespace VoidHex.P1.Domain
{
    public enum PathFailure
    {
        None = 0,
        InvalidStart = 1,
        InvalidGoal = 2,
        Unreachable = 3,
        BudgetExceeded = 4,
    }

    /// <summary>Resultado explícito de una búsqueda. La ausencia de ruta es un estado, no una excepción.</summary>
    public readonly struct PathResult
    {
        public bool IsFound { get; }
        public IReadOnlyList<HexCoordinates> Path { get; }
        public int TotalCost { get; }
        public PathFailure Failure { get; }

        private PathResult(bool isFound, IReadOnlyList<HexCoordinates> path, int totalCost, PathFailure failure)
        {
            IsFound = isFound;
            Path = path;
            TotalCost = totalCost;
            Failure = failure;
        }

        public static PathResult Success(IReadOnlyList<HexCoordinates> path, int totalCost)
            => new PathResult(true, path, totalCost, PathFailure.None);

        public static PathResult Failed(PathFailure failure)
            => new PathResult(false, System.Array.Empty<HexCoordinates>(), 0, failure);
    }
}
