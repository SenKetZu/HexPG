using System;

namespace VoidHex.P2.Domain
{
    /// <summary>
    /// Habilidad adicional agregada sin tocar el pipeline ni los estados de turno.
    /// Es la demostración de OCP que se puede hacer en vivo durante la defensa.
    /// </summary>
    public sealed class ShieldCommand : ICommand
    {
        public int ActionPointCost { get; }

        public int ShieldAmount { get; }

        public string VisualKey => "shield";

        public ShieldCommand(int actionPointCost, int shieldAmount)
        {
            if (actionPointCost < 0) throw new ArgumentOutOfRangeException(nameof(actionPointCost));
            if (shieldAmount <= 0) throw new ArgumentOutOfRangeException(nameof(shieldAmount));

            ActionPointCost = actionPointCost;
            ShieldAmount = shieldAmount;
        }

        public CommandValidation Validate(ShipModel model)
        {
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
            => model.WithShields(model.Shields + ShieldAmount).WithActionPoints(model.ActionPoints - ActionPointCost);
    }
}
