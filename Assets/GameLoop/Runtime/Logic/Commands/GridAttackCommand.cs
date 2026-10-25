using VoidHex.P1.Domain;
using VoidHex.P2.Domain;

namespace VoidHex.GameLoop.Logic
{
    /// <summary>
    /// Disparo sobre la torreta. Segundo comando nuevo del bucle: a diferencia del
    /// <c>AttackCommand</c> de P2 (que solo mide distancia), este consulta la grilla para validar
    /// que un asteroide no corte la línea de visión.
    ///
    /// Es el punto donde P1 y P2 se encuentran sin que ninguno dependa del otro: el comando vive
    /// en la capa de composición y es el único que conoce ambos contratos.
    /// </summary>
    public sealed class GridAttackCommand : ICommand
    {
        private readonly IGridQueryService _grid;
        private readonly int _damage;
        private readonly int _range;

        public int ActionPointCost { get; }

        public string VisualKey => "attack";

        public int Damage => _damage;

        public GridAttackCommand(IGridQueryService grid, int damage, int range, int actionPointCost)
        {
            _grid = grid;
            _damage = damage;
            _range = range;
            ActionPointCost = actionPointCost;
        }

        public CommandValidation Validate(ShipModel model)
        {
            if (model.EnemyHull <= 0)
            {
                return CommandValidation.Rejected(CommandRejection.TargetDestroyed, ActionPointCost,
                    model.ActionPoints);
            }

            var distance = VoidHex.P2.Domain.HexCoordinates.Distance(model.Position, model.EnemyPosition);
            if (distance > _range)
            {
                return CommandValidation.Rejected(CommandRejection.OutOfRange, ActionPointCost,
                    model.ActionPoints);
            }

            var from = HexCoords.ToGrid(model.Position);
            var to = HexCoords.ToGrid(model.EnemyPosition);
            if (!_grid.HasLineOfSight(from, to))
            {
                return CommandValidation.Rejected(CommandRejection.LineOfSightBlocked, ActionPointCost,
                    model.ActionPoints);
            }

            if (model.ActionPoints < ActionPointCost)
            {
                return CommandValidation.Rejected(CommandRejection.NotEnoughActionPoints, ActionPointCost,
                    model.ActionPoints);
            }

            return CommandValidation.Allowed();
        }

        public ShipModel Execute(ShipModel model)
            => model.WithEnemyHull(model.EnemyHull - _damage)
                .WithActionPoints(model.ActionPoints - ActionPointCost);
    }
}
