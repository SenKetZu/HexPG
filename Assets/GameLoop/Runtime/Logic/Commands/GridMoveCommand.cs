using System;
using System.Collections.Generic;
using VoidHex.P2.Domain;
using P1Coords = VoidHex.P1.Domain.HexCoordinates;

namespace VoidHex.GameLoop.Logic
{
    /// <summary>
    /// Comando de movimiento sobre la grilla. Es la demostración de OCP que el bucle necesitaba:
    /// un comando NUEVO, que conoce la grilla, sin tocar <c>CommandProcessor</c> ni la FSM.
    ///
    /// Recibe la ruta ya calculada en lugar de consultar el pathfinder: un comando es inmutable y
    /// su costo se fija al construirse, así que la búsqueda ocurre en la sesión, antes de crearlo.
    /// </summary>
    public sealed class GridMoveCommand : ICommand
    {
        private readonly P1Coords _target;
        private readonly int _steps;
        private readonly int _maxSteps;

        public int ActionPointCost { get; }

        public string VisualKey => "move";

        public GridMoveCommand(P1Coords target, IReadOnlyList<P1Coords> path, int cost, int maxSteps)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (path.Count == 0) throw new ArgumentException("La ruta no puede estar vacía.", nameof(path));
            if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));

            _target = target;
            ActionPointCost = cost;
            _steps = path.Count - 1;
            _maxSteps = maxSteps;
        }

        public CommandValidation Validate(ShipModel model)
        {
            // El alcance depende del loadout (stat de movimiento); el pozo de PA, del turno.
            if (_steps > _maxSteps)
            {
                return CommandValidation.Rejected(CommandRejection.OutOfRange, ActionPointCost, model.ActionPoints);
            }

            if (model.ActionPoints < ActionPointCost)
            {
                return CommandValidation.Rejected(
                    CommandRejection.NotEnoughActionPoints,
                    ActionPointCost,
                    model.ActionPoints);
            }

            return CommandValidation.Allowed();
        }

        public ShipModel Execute(ShipModel model)
            => model.WithPosition(HexCoords.FromGrid(_target))
                .WithActionPoints(model.ActionPoints - ActionPointCost);
    }
}
