using NUnit.Framework;
using VoidHex.GameLoop.Logic;
using VoidHex.P1.Domain;
using VoidHex.P2.Domain;
using VoidHex.P3.Domain;
using P1Coords = VoidHex.P1.Domain.HexCoordinates;

namespace VoidHex.GameLoop.Tests
{
    /// <summary>
    /// Tests del bucle completo, sin escena.
    ///
    /// Lo que verifican es la composición, no los prototipos: que el loadout de P3 llegue al combate,
    /// que el movimiento use el pathfinding de P1 y cobre PA según P2, y que el bucle cierre bien
    /// en victoria, derrota y vuelta al hangar.
    /// </summary>
    public sealed class GameSessionTests
    {
        private const int Width = 8;
        private const int Height = 6;

        private static readonly P1Coords Start = new P1Coords(1, 1);
        private static readonly P1Coords Turret = new P1Coords(6, 3);

        private GridModel _grid;

        [SetUp]
        public void SetUp()
        {
            _grid = new GridModel(Width, Height);
        }

        private GameSession CreateSession(P1Coords? start = null, P1Coords? turret = null,
            int turretHull = 8, int turretDamage = 2)
        {
            return new GameSession(_grid, _grid, start ?? Start, turret ?? Turret, turretHull, turretDamage);
        }

        private static ShipStats Stats(int actionPoints = 6, int shields = 0, int movement = 3, int hull = 10)
            => new ShipStats(actionPoints, shields, movement, hull, null);

        // ------------------------------------------------------------------ fases

        [Test]
        public void NewSession_StartsInHangar()
        {
            var session = CreateSession();

            Assert.That(session.Phase, Is.EqualTo(LoopPhase.Hangar));
            Assert.That(session.Victories, Is.Zero);
        }

        [Test]
        public void Deploy_TakesStatsFromLoadoutAndEntersCombat()
        {
            var session = CreateSession();

            session.Deploy(Stats(actionPoints: 7, shields: 4, movement: 5, hull: 12), attackDamage: 3);

            Assert.That(session.Phase, Is.EqualTo(LoopPhase.Combat));
            Assert.That(session.State.ActionPoints, Is.EqualTo(7));
            Assert.That(session.State.MaxActionPoints, Is.EqualTo(7));
            Assert.That(session.State.Shields, Is.EqualTo(4));
            Assert.That(session.State.Hull, Is.EqualTo(12));
            Assert.That(session.MaxSteps, Is.EqualTo(5));
            Assert.That(session.AttackDamage, Is.EqualTo(3));
            Assert.That(session.Turn, Is.EqualTo(1));
        }

        [Test]
        public void Deploy_MarksBothOccupiedOnTheGrid()
        {
            var session = CreateSession();
            var changes = 0;
            _grid.Changed += _ => changes++;

            session.Deploy(Stats(), attackDamage: 2);

            Assert.That(changes, Is.EqualTo(2), "Cada ocupación es una mutación real de la grilla.");
            Assert.That(_grid.GetCell(Start).IsOccupied, Is.True);
            Assert.That(_grid.GetCell(Turret).IsOccupied, Is.True);
        }

        [Test]
        public void ActionsOutsideCombat_AreRejectedWithWrongPhase()
        {
            var session = CreateSession();

            var move = session.TryMove(new P1Coords(2, 1));
            var attack = session.TryAttack();

            Assert.That(move.Applied, Is.False);
            Assert.That(move.Validation.Rejection, Is.EqualTo(CommandRejection.WrongPhase));
            Assert.That(attack.Validation.Rejection, Is.EqualTo(CommandRejection.WrongPhase));
        }

        // ------------------------------------------------------------------ movimiento

        [Test]
        public void TryMove_SpendsThePathCostComputedFromTheGrid()
        {
            var session = CreateSession();
            session.Deploy(Stats(actionPoints: 6, movement: 5), attackDamage: 2);

            // Un asteroide alarga la ruta: se paga el camino real, no la distancia geométrica.
            _grid.SetCell(HexCell.Asteroid(new P1Coords(2, 1)));

            var result = session.TryMove(new P1Coords(3, 1));

            Assert.That(result.Applied, Is.True);
            Assert.That(result.HasExecution, Is.True, "La acción debe poder animarse.");
            Assert.That(result.Execution.Command.ActionPointCost, Is.EqualTo(result.Execution.Before.ActionPoints -
                result.Execution.After.ActionPoints));
            Assert.That(session.State.ActionPoints, Is.LessThan(6));
            Assert.That(session.PlayerCell, Is.EqualTo(new P1Coords(3, 1)));
        }

        [Test]
        public void TryMove_MovesTheOccupancyToTheNewCell()
        {
            var session = CreateSession();
            session.Deploy(Stats(), attackDamage: 2);

            session.TryMove(new P1Coords(2, 1));

            Assert.That(_grid.GetCell(Start).IsOccupied, Is.False);
            Assert.That(_grid.GetCell(new P1Coords(2, 1)).IsOccupied, Is.True);
        }

        [Test]
        public void TryMove_BeyondMovementRange_RejectsWithOutOfRange()
        {
            var session = CreateSession();
            session.Deploy(Stats(actionPoints: 9, movement: 1), attackDamage: 2);

            var result = session.TryMove(new P1Coords(5, 1));

            Assert.That(result.Applied, Is.False);
            Assert.That(result.Validation.Rejection, Is.EqualTo(CommandRejection.OutOfRange));
            Assert.That(session.PlayerCell, Is.EqualTo(Start), "Un rechazo no mueve la nave.");
        }

        [Test]
        public void TryMove_WithoutEnoughActionPoints_RejectsAndKeepsState()
        {
            var session = CreateSession();
            session.Deploy(Stats(actionPoints: 1, movement: 5), attackDamage: 2);

            var result = session.TryMove(new P1Coords(4, 1));

            Assert.That(result.Applied, Is.False);
            Assert.That(result.Validation.Rejection, Is.EqualTo(CommandRejection.NotEnoughActionPoints));
            Assert.That(session.State.ActionPoints, Is.EqualTo(1));
            Assert.That(session.PlayerCell, Is.EqualTo(Start));
        }

        [Test]
        public void TryMove_OntoBlockedCell_IsRejected()
        {
            var session = CreateSession();
            session.Deploy(Stats(), attackDamage: 2);
            _grid.SetCell(HexCell.Asteroid(new P1Coords(2, 1)));

            var result = session.TryMove(new P1Coords(2, 1));

            Assert.That(result.Applied, Is.False);
            Assert.That(result.Validation.Rejection, Is.EqualTo(CommandRejection.InvalidTarget));
        }

        // ------------------------------------------------------------------ ataque

        [Test]
        public void TryAttack_WithoutLineOfSight_RejectsWithItsOwnReason()
        {
            var session = CreateSession(start: new P1Coords(4, 3));
            session.Deploy(Stats(), attackDamage: 3);

            // La única casilla entre ambas queda bloqueada por un asteroide.
            _grid.SetCell(HexCell.Asteroid(new P1Coords(5, 3)));

            var result = session.TryAttack();

            Assert.That(result.Applied, Is.False);
            Assert.That(result.Validation.Rejection, Is.EqualTo(CommandRejection.LineOfSightBlocked));
            Assert.That(session.State.EnemyHull, Is.EqualTo(session.TurretHullMax));
        }

        [Test]
        public void TryAttack_OutOfRange_RejectsWithDistance()
        {
            var session = CreateSession();
            session.Deploy(Stats(), attackDamage: 3);

            var result = session.TryAttack();

            Assert.That(result.Applied, Is.False);
            Assert.That(result.Validation.Rejection, Is.EqualTo(CommandRejection.OutOfRange));
            Assert.That(result.Message, Does.Contain("rango"));
        }

        [Test]
        public void TryAttack_WhenTurretHullReachesZero_EndsInVictory()
        {
            var session = CreateSession(start: new P1Coords(5, 3), turretHull: 8);
            session.Deploy(Stats(), attackDamage: 8);

            var result = session.TryAttack();

            Assert.That(result.Applied, Is.True);
            Assert.That(session.State.EnemyHull, Is.Zero);
            Assert.That(session.Phase, Is.EqualTo(LoopPhase.Victory));
        }

        [Test]
        public void TryAttack_AfterVictory_IsRejected()
        {
            var session = CreateSession(start: new P1Coords(5, 3), turretHull: 4);
            session.Deploy(Stats(), attackDamage: 4);
            session.TryAttack();

            var again = session.TryAttack();

            Assert.That(again.Applied, Is.False);
            Assert.That(again.Validation.Rejection, Is.EqualTo(CommandRejection.WrongPhase));
        }

        // ------------------------------------------------------------------ turno

        [Test]
        public void EndTurn_WhenTurretCanFire_DamagesShieldsBeforeHull()
        {
            var session = CreateSession(start: new P1Coords(5, 3), turretDamage: 2);
            session.Deploy(Stats(shields: 3, hull: 10), attackDamage: 1);

            var results = session.EndTurn();

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Applied, Is.True);
            Assert.That(session.State.Shields, Is.EqualTo(1), "Dos puntos se fueron en escudos.");
            Assert.That(session.State.Hull, Is.EqualTo(10), "El casco no se toca mientras haya escudos.");
        }

        [Test]
        public void EndTurn_WhenShieldsAreGone_DamagesHull()
        {
            var session = CreateSession(start: new P1Coords(5, 3), turretDamage: 2);
            session.Deploy(Stats(shields: 1, hull: 10), attackDamage: 1);

            session.EndTurn();

            Assert.That(session.State.Shields, Is.Zero);
            Assert.That(session.State.Hull, Is.EqualTo(9));
        }

        [Test]
        public void EndTurn_RefillsActionPointsAndAdvancesTheTurn()
        {
            var session = CreateSession();
            session.Deploy(Stats(actionPoints: 6, movement: 5), attackDamage: 2);

            session.TryMove(new P1Coords(2, 1));
            var afterMove = session.State.ActionPoints;

            session.EndTurn();

            Assert.That(afterMove, Is.LessThan(6));
            Assert.That(session.State.ActionPoints, Is.EqualTo(6));
            Assert.That(session.Turn, Is.EqualTo(2));
        }

        [Test]
        public void EndTurn_WhenOutOfSight_DoesNotDamageAndReportsIt()
        {
            var session = CreateSession();
            session.Deploy(Stats(shields: 3, hull: 10), attackDamage: 2);

            var results = session.EndTurn();

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Applied, Is.False);
            Assert.That(session.State.Hull, Is.EqualTo(10));
            Assert.That(session.State.Shields, Is.EqualTo(3));
        }

        [Test]
        public void EndTurn_WhenHullReachesZero_EndsInDefeat()
        {
            var session = CreateSession(start: new P1Coords(5, 3), turretDamage: 5);
            session.Deploy(Stats(shields: 0, hull: 4), attackDamage: 1);

            session.EndTurn();

            Assert.That(session.State.Hull, Is.Zero);
            Assert.That(session.Phase, Is.EqualTo(LoopPhase.Defeat));
        }

        // ------------------------------------------------------------------ vuelta al hangar

        [Test]
        public void ReturnToHangar_AfterVictory_AwardsOneVictoryAndReleasesTheCell()
        {
            var session = CreateSession(start: new P1Coords(5, 3), turretHull: 3);
            session.Deploy(Stats(), attackDamage: 3);
            session.TryAttack();

            session.ReturnToHangar();

            Assert.That(session.Phase, Is.EqualTo(LoopPhase.Hangar));
            Assert.That(session.Victories, Is.EqualTo(1));
            Assert.That(_grid.GetCell(new P1Coords(5, 3)).IsOccupied, Is.False);
        }

        [Test]
        public void FullLoop_HangarCombatVictoryHangar_CanStartAgain()
        {
            var session = CreateSession(start: new P1Coords(5, 3), turretHull: 2);

            session.Deploy(Stats(), attackDamage: 2);
            session.TryAttack();
            session.ReturnToHangar();

            // Segundo combate: el estado se reinicia y la torreta vuelve a estar entera.
            session.Deploy(Stats(actionPoints: 4), attackDamage: 2);

            Assert.That(session.Phase, Is.EqualTo(LoopPhase.Combat));
            Assert.That(session.State.EnemyHull, Is.EqualTo(2));
            Assert.That(session.State.ActionPoints, Is.EqualTo(4));
            Assert.That(session.Victories, Is.EqualTo(1));
        }
    }
}
