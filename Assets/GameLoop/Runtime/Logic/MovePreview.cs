using System;
using System.Collections.Generic;
using VoidHex.P1.Domain;
using P1Coords = VoidHex.P1.Domain.HexCoordinates;

namespace VoidHex.GameLoop.Logic
{
    /// <summary>
    /// Resultado de preguntar "¿qué pasaría si me muevo acá?" sin ejecutar nada.
    ///
    /// Existe para la previsualización: la UI necesita la ruta y el costo ANTES del clic, y no debe
    /// reconstruir esas reglas por su cuenta. Es una consulta pura: no muta el estado ni la grilla.
    /// </summary>
    public readonly struct MovePreview
    {
        /// <summary>La ruta es válida y el movimiento se podría ejecutar ahora.</summary>
        public bool IsValid { get; }

        /// <summary>
        /// Celdas desde el origen hasta el destino, inclusive.
        ///
        /// Nunca es null, ni siquiera en <c>default(MovePreview)</c>: un struct creado por defecto no
        /// ejecuta el constructor, así que la auto-propiedad quedaría en null. Al ser un tipo de valor
        /// que circula como "sin resultado", la ausencia se resuelve acá y no en cada llamador.
        /// </summary>
        public IReadOnlyList<P1Coords> Path => _path ?? Array.Empty<P1Coords>();

        private readonly IReadOnlyList<P1Coords> _path;

        public int Cost { get; }

        /// <summary>Motivo cuando la ruta no es ejecutable (<see cref="PathFailure.None"/> si lo es).</summary>
        public PathFailure Failure { get; }

        /// <summary>El destino está dentro del presupuesto de PA del turno.</summary>
        public bool CanAfford { get; }

        /// <summary>El destino está dentro del alcance de movimiento del loadout.</summary>
        public bool WithinRange { get; }

        private MovePreview(bool isValid, IReadOnlyList<P1Coords> path, int cost, PathFailure failure,
            bool canAfford, bool withinRange)
        {
            IsValid = isValid;
            _path = path ?? Array.Empty<P1Coords>();
            Cost = cost;
            Failure = failure;
            CanAfford = canAfford;
            WithinRange = withinRange;
        }

        public static MovePreview Blocked(PathFailure failure)
            => new MovePreview(false, Array.Empty<P1Coords>(), 0, failure, false, false);

        public static MovePreview Evaluate(IReadOnlyList<P1Coords> path, int cost, int steps, int maxSteps,
            int availableActionPoints)
        {
            var withinRange = steps <= maxSteps;
            var canAfford = availableActionPoints >= cost;

            var failure = withinRange ? PathFailure.None : PathFailure.BudgetExceeded;

            return new MovePreview(withinRange && canAfford, path, cost, failure, canAfford, withinRange);
        }
    }
}
