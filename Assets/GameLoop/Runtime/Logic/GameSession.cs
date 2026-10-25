using System;
using System.Collections.Generic;
using VoidHex.P1.Domain;
using VoidHex.P2.Domain;
using VoidHex.P3.Domain;
using P1Coords = VoidHex.P1.Domain.HexCoordinates;
using P2Coords = VoidHex.P2.Domain.HexCoordinates;

namespace VoidHex.GameLoop.Logic
{
    /// <summary>
    /// El bucle completo, sin Unity.
    ///
    /// Compone los tres prototipos sin que ninguno dependa de otro:
    ///   · P3 aporta el loadout: la sesión recibe <see cref="ShipStats"/> ya consolidado.
    ///   · P1 aporta la grilla: se consulta por <see cref="IGridQueryService"/> y se muta por
    ///     <see cref="IGridCommands"/> (los dos contratos segregados del desafío 1).
    ///   · P2 aporta la regla de acción: cada acción es un <see cref="ICommand"/> que se valida con
    ///     motivo y se ejecuta sobre un estado inmutable.
    ///
    /// La sesión aplica la lógica de inmediato y devuelve la <see cref="CommandExecution"/> a animar.
    /// Esperar la animación es tarea de la capa Unity: la regla nunca depende de un frame.
    /// </summary>
    public sealed class GameSession
    {
        private readonly IGridQueryService _grid;
        private readonly IGridCommands _commands;
        private readonly P1Coords _start;
        private readonly P1Coords _turret;
        private readonly int _turretDamage;

        private P1Coords _playerCell;

        public GameSession(
            IGridQueryService grid,
            IGridCommands commands,
            P1Coords start,
            P1Coords turret,
            int turretHull = 8,
            int turretDamage = 2)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _commands = commands ?? throw new ArgumentNullException(nameof(commands));

            if (turretHull <= 0) throw new ArgumentOutOfRangeException(nameof(turretHull));
            if (turretDamage < 0) throw new ArgumentOutOfRangeException(nameof(turretDamage));

            _start = start;
            _turret = turret;
            _turretDamage = turretDamage;

            TurretHullMax = turretHull;
            State = new ShipModel(HexCoords.FromGrid(start), 0, 0, 0, 1, HexCoords.FromGrid(turret), turretHull);
        }

        public LoopPhase Phase { get; private set; } = LoopPhase.Hangar;

        public ShipModel State { get; private set; }

        public int Turn { get; private set; }

        public int Victories { get; private set; }

        public int TurretHullMax { get; }

        public int TurretRange => 3;

        public int AttackCost => 2;

        /// <summary>Alcance de movimiento del turno: sale del stat Movement del loadout.</summary>
        public int MaxSteps { get; private set; } = 3;

        /// <summary>Daño del arma equipada: lo resuelve quien conoce el loadout, no la sesión.</summary>
        public int AttackDamage { get; private set; } = 2;

        public P1Coords PlayerCell => _playerCell;

        public P1Coords TurretCell => _turret;

        public int DistanceToTurret => P2Coords.Distance(State.Position, State.EnemyPosition);

        public bool TurretHasLineOfSight =>
            _grid.HasLineOfSight(_playerCell, _turret);

        public bool TurretCanFire => DistanceToTurret <= TurretRange && TurretHasLineOfSight;

        // ------------------------------------------------------------------ ciclo

        /// <summary>
        /// Sale del hangar con el loadout actual. Los números ya vienen resueltos por P3
        /// (stats) y por quien leyó el arma; la sesión no lee el catálogo.
        /// </summary>
        public void Deploy(ShipStats stats, int attackDamage)
        {
            if (attackDamage <= 0) throw new ArgumentOutOfRangeException(nameof(attackDamage));

            MaxSteps = Math.Max(1, stats.Movement);
            AttackDamage = attackDamage;
            Turn = 1;

            State = new ShipModel(
                HexCoords.FromGrid(_start),
                stats.ActionPoints,
                stats.ActionPoints,
                stats.Shields,
                stats.Hull,
                HexCoords.FromGrid(_turret),
                TurretHullMax);

            _playerCell = _start;

            // La ocupación es parte del estado de la grilla: se publica y la vista se redibuja.
            _commands.SetOccupied(_start, true);
            _commands.SetOccupied(_turret, true);

            Phase = LoopPhase.Combat;
        }

        /// <summary>Vuelve al hangar. La victoria se contabiliza al salir del combate.</summary>
        public void ReturnToHangar()
        {
            if (Phase == LoopPhase.Combat) return;

            if (Phase == LoopPhase.Victory) Victories++;

            if (_grid.TryGetCell(_playerCell, out _)) _commands.SetOccupied(_playerCell, false);

            Phase = LoopPhase.Hangar;
        }

        // ------------------------------------------------------------------ acciones

        public LoopActionResult TryMove(P1Coords target)
        {
            if (Phase != LoopPhase.Combat)
            {
                return LoopActionResult.Rejected(
                    CommandValidation.Rejected(CommandRejection.WrongPhase, 0, State.ActionPoints),
                    "No es el turno de la nave.");
            }

            if (!_grid.Contains(target))
            {
                return LoopActionResult.Rejected(
                    CommandValidation.Rejected(CommandRejection.InvalidTarget, 0, State.ActionPoints),
                    "Esa casilla no existe.");
            }

            if (target.Equals(_playerCell))
            {
                return LoopActionResult.NoOp("Ya estás en esa casilla.");
            }

            var pathfinder = new Pathfinder(_grid);
            var path = pathfinder.FindPath(_playerCell, target, maxCost: 999);

            if (!path.IsFound)
            {
                return LoopActionResult.Rejected(
                    CommandValidation.Rejected(CommandRejection.InvalidTarget, 0, State.ActionPoints),
                    path.Failure == PathFailure.Unreachable
                        ? "El camino está bloqueado."
                        : "No se encontró un camino.");
            }

            var command = new GridMoveCommand(target, path.Path, path.TotalCost, MaxSteps);
            var validation = command.Validate(State);

            if (!validation.IsAllowed)
            {
                return LoopActionResult.Rejected(validation, DescribeMoveRejection(validation, path.TotalCost));
            }

            var before = State;
            State = command.Execute(State);

            _commands.SetOccupied(_playerCell, false);
            _commands.SetOccupied(target, true);
            _playerCell = target;

            return LoopActionResult.Success(validation,
                $"Movimiento a {target} · {path.TotalCost} PA",
                new CommandExecution(command, before, State));
        }

        public LoopActionResult TryAttack()
        {
            if (Phase != LoopPhase.Combat)
            {
                return LoopActionResult.Rejected(
                    CommandValidation.Rejected(CommandRejection.WrongPhase, AttackCost, State.ActionPoints),
                    "No es el turno de la nave.");
            }

            var command = new GridAttackCommand(_grid, AttackDamage, TurretRange, AttackCost);
            var validation = command.Validate(State);

            if (!validation.IsAllowed)
            {
                return LoopActionResult.Rejected(validation, DescribeAttackRejection(validation));
            }

            var before = State;
            State = command.Execute(State);

            var message = $"Disparo · {AttackDamage} de daño · torreta {State.EnemyHull}/{TurretHullMax}";

            if (State.EnemyHull <= 0)
            {
                Phase = LoopPhase.Victory;
                message = "Torreta destruida.";
            }

            return LoopActionResult.Success(validation, message, new CommandExecution(command, before, State));
        }

        /// <summary>
        /// Cierra el turno: la torreta actúa (si puede) y la nave recarga PA.
        /// Devuelve cada acción de la torreta para poder animarlas en orden.
        /// </summary>
        public IReadOnlyList<LoopActionResult> EndTurn()
        {
            var results = new List<LoopActionResult>();

            if (Phase != LoopPhase.Combat) return results;

            if (TurretCanFire)
            {
                var command = new EnemyStrikeCommand(_turretDamage);
                var before = State;
                State = command.Execute(State);

                var absorbed = before.Shields - State.Shields;
                var toHull = before.Hull - State.Hull;
                var message = toHull > 0
                    ? $"La torreta dispara · -{toHull} casco"
                    : $"La torreta dispara · escudos absorben {absorbed}";

                results.Add(LoopActionResult.Success(CommandValidation.Allowed(), message,
                    new CommandExecution(command, before, State)));

                if (State.Hull <= 0)
                {
                    Phase = LoopPhase.Defeat;
                    return results;
                }
            }
            else
            {
                results.Add(LoopActionResult.NoOp("La torreta no tiene línea de tiro."));
            }

            State = State.WithActionPoints(State.MaxActionPoints);
            Turn++;

            return results;
        }

        // ------------------------------------------------------------------ mensajes

        private string DescribeMoveRejection(CommandValidation validation, int cost)
        {
            return validation.Rejection == CommandRejection.OutOfRange
                ? $"Fuera de alcance: el movimiento permite {MaxSteps} casillas"
                : $"PA insuficiente: necesita {cost}, quedan {State.ActionPoints}";
        }

        private string DescribeAttackRejection(CommandValidation validation)
        {
            switch (validation.Rejection)
            {
                case CommandRejection.LineOfSightBlocked:
                    return "Un asteroide corta la línea de visión.";
                case CommandRejection.OutOfRange:
                    return $"Fuera de rango: la torreta está a {DistanceToTurret}, el arma llega a {TurretRange}.";
                case CommandRejection.TargetDestroyed:
                    return "La torreta ya está destruida.";
                case CommandRejection.NotEnoughActionPoints:
                    return $"PA insuficiente: el disparo cuesta {AttackCost}, quedan {State.ActionPoints}.";
                default:
                    return "El disparo no es válido.";
            }
        }
    }
}
