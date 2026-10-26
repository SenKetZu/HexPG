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

        /// <summary>
        /// Nunca null, ni en <c>default(PathResult)</c>: un struct creado por defecto no ejecuta el
        /// constructor y la auto-propiedad quedaría en null. La ausencia se resuelve en el tipo.
        /// </summary>
        public IReadOnlyList<HexCoordinates> Path => _path ?? System.Array.Empty<HexCoordinates>();

        public int TotalCost { get; }
        public PathFailure Failure { get; }

        private readonly IReadOnlyList<HexCoordinates> _path;

        private PathResult(bool isFound, IReadOnlyList<HexCoordinates> path, int totalCost, PathFailure failure)
        {
            IsFound = isFound;
            _path = path ?? System.Array.Empty<HexCoordinates>();
            TotalCost = totalCost;
            Failure = failure;
        }

        public static PathResult Success(IReadOnlyList<HexCoordinates> path, int totalCost)
            => new PathResult(true, path, totalCost, PathFailure.None);

        public static PathResult Failed(PathFailure failure)
            => new PathResult(false, System.Array.Empty<HexCoordinates>(), 0, failure);
    }
}
