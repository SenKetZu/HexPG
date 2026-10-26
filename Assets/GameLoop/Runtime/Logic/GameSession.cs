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

        /// <summary>
        /// Alcance del arma de la nave. Es una regla distinta de <see cref="TurretRange"/>, aunque
        /// hoy compartan valor: equilibrar una no debería mover la otra.
        /// </summary>
        public int AttackRange => 3;

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

        // ------------------------------------------------------------------ alcance de ataque

        /// <summary>
        /// Celdas que el arma alcanza <b>desde</b> <paramref name="origin"/>.
        ///
        /// Es el alcance del arma, no el área del enemigo: sirve para responder "si me paro acá,
        /// ¿a qué llego?". El centro lo elige la presentación (la casilla bajo el mouse), no la sesión.
        ///
        /// No es solo "distancia ≤ alcance": un asteroide corta la línea de visión desde ese origen.
        /// Se usa la misma pareja (rango + LOS) que valida <see cref="GridAttackCommand"/>, así lo que
        /// se pinta y lo que se acepta al disparar no pueden divergir.
        /// </summary>
        public IReadOnlyList<P1Coords> AttackReach(P1Coords origin)
        {
            var cells = new List<P1Coords>();

            var originP2 = HexCoords.FromGrid(origin);

            for (var dq = -TurretRange; dq <= TurretRange; dq++)
            {
                for (var dr = -TurretRange; dr <= TurretRange; dr++)
                {
                    var cell = new P1Coords(origin.Q + dq, origin.R + dr);

                    if (cell.Equals(origin)) continue;
                    if (!IsOpenGround(cell)) continue;

                    var distance = P2Coords.Distance(originP2, HexCoords.FromGrid(cell));
                    if (distance > TurretRange) continue;

                    if (!_grid.HasLineOfSight(origin, cell)) continue;

                    cells.Add(cell);
                }
            }

            return cells;
        }

        /// <summary>True si desde <paramref name="origin"/> se alcanza la torreta ahora.</summary>
        public bool CanAttackFrom(P1Coords origin)
        {
            // Parado sobre la torreta no hay disparo: es el propio objetivo, y <see cref="AttackReach"/>
            // nunca incluye el origen, así que sin esta guarda ambas respuestas se contradecían.
            if (origin.Equals(_turret)) return false;

            if (!IsOpenGround(origin)) return false;

            var distance = P2Coords.Distance(HexCoords.FromGrid(origin), State.EnemyPosition);
            if (distance > TurretRange) return false;

            return _grid.HasLineOfSight(origin, _turret);
        }

        /// <summary>True si desde la posición actual de la nave un disparo sería válido.</summary>
        public bool IsInAttackRange(P1Coords cell) => CanAttackFrom(cell);

        /// <summary>
        /// Terreno donde una nave puede estar: existe y no está bloqueado.
        ///
        /// Deliberadamente NO usa <c>IGridQueryService.IsPassable</c>: ese contrato también descarta
        /// las celdas occupadas, y la nave ocupa su propia celda. Usarlo hacía que el área de ataque
        /// y el rango nunca incluyeran la posición actual del jugador.
        /// </summary>
        private bool IsOpenGround(P1Coords cell)
            => _grid.TryGetCell(cell, out var data) && data.IsPassable;

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

        /// <summary>
        /// Calcula qué pasaría al intentar moverse a <paramref name="target"/>, sin mutar nada.
        ///
        /// La usa la previsualización: la pantalla pide la ruta al pasar el mouse y la dibuja.
        /// Devuelve la misma evaluación que usaría <see cref="TryMove"/>, así lo que se muestra
        /// y lo que ocurre al hacer clic no pueden divergir.
        /// </summary>
        public MovePreview PreviewMove(P1Coords target)
        {
            if (Phase != LoopPhase.Combat) return MovePreview.Blocked(PathFailure.None);

            // La casilla actual se evalúa ANTES de la transitabilidad: la nave la ocupa, así que
            // `IsPassable` es false para ella. Comparar después hacía que apuntar a la propia
            // casilla se reportara como "camino bloqueado".
            if (target.Equals(_playerCell))
            {
                return MovePreview.Evaluate(new[] { target }, 0, 0, MaxSteps, State.ActionPoints);
            }

            if (!_grid.Contains(target) || !_grid.IsPassable(target))
            {
                return MovePreview.Blocked(PathFailure.InvalidGoal);
            }

            var pathfinder = new Pathfinder(_grid);
            var path = pathfinder.FindPath(_playerCell, target, maxCost: 999);

            if (!path.IsFound) return MovePreview.Blocked(path.Failure);

            return MovePreview.Evaluate(path.Path, path.TotalCost, path.Path.Count - 1, MaxSteps,
                State.ActionPoints);
        }

        public LoopActionResult TryMove(P1Coords target)
        {            if (Phase != LoopPhase.Combat)
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

            var command = new GridAttackCommand(_grid, AttackDamage, AttackRange, AttackCost);
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
                    return $"Fuera de rango: la torreta está a {DistanceToTurret}, el arma llega a {AttackRange}.";
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
