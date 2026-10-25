using System;

namespace VoidHex.P2.Domain
{
    /// <summary>Disparo contra la nave enemiga dentro de un rango hexagonal.</summary>
    public sealed class AttackCommand : ICommand
    {
        public int ActionPointCost { get; }

        public int Damage { get; }

        public int Range { get; }

        public string VisualKey => "attack";

        public AttackCommand(int actionPointCost, int damage, int range)
        {
            if (actionPointCost < 0) throw new ArgumentOutOfRangeException(nameof(actionPointCost));
            if (damage <= 0) throw new ArgumentOutOfRangeException(nameof(damage));
            if (range <= 0) throw new ArgumentOutOfRangeException(nameof(range));

            ActionPointCost = actionPointCost;
            Damage = damage;
            Range = range;
        }

        public CommandValidation Validate(ShipModel model)
        {
            if (model.EnemyHull <= 0)
            {
                return CommandValidation.Rejected(
                    CommandRejection.TargetDestroyed,
                    ActionPointCost,
                    model.ActionPoints);
            }

            if (HexCoordinates.Distance(model.Position, model.EnemyPosition) > Range)
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
            => model.WithEnemyHull(model.EnemyHull - Damage).WithActionPoints(model.ActionPoints - ActionPointCost);
    }
}
