using System;

namespace VoidHex.P2.Domain
{
    /// <summary>Desplazamiento a una celda objetivo. El costo lo fija quien construye el comando.</summary>
    public sealed class MoveCommand : ICommand
    {
        public HexCoordinates Target { get; }

        public int ActionPointCost { get; }

        public string VisualKey => "move";

        public MoveCommand(HexCoordinates target, int actionPointCost)
        {
            if (actionPointCost < 0) throw new ArgumentOutOfRangeException(nameof(actionPointCost));

            Target = target;
            ActionPointCost = actionPointCost;
        }

        public CommandValidation Validate(ShipModel model)
        {
            if (model.Position == Target)
            {
                return CommandValidation.Rejected(CommandRejection.InvalidTarget, ActionPointCost, model.ActionPoints);
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
            => model.WithPosition(Target).WithActionPoints(model.ActionPoints - ActionPointCost);
    }
}
