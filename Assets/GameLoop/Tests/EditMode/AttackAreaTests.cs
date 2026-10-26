using System.Linq;
using NUnit.Framework;
using VoidHex.GameLoop.Logic;
using VoidHex.P1.Domain;
using VoidHex.P3.Domain;
using P1Coords = VoidHex.P1.Domain.HexCoordinates;

namespace VoidHex.GameLoop.Tests
{
    /// <summary>
    /// El alcance de ataque es una promesa visual: si se pinta una casilla que el arma no alcanza
    /// desde el origen, el jugador se guía por una mentira. Estos tests fijan que lo dibujado y lo
    /// validado coincidan, y que el centro del alcance sea el origen que elige la presentación
    /// (la casilla bajo el mouse), no el enemigo.
    /// </summary>
    public sealed class AttackAreaTests
    {
        private GridModel _grid;

        [SetUp]
        public void SetUp() => _grid = new GridModel(8, 6);

        private GameSession CreateSession(P1Coords start, P1Coords turret)
            => new GameSession(_grid, _grid, start, turret);

        private static ShipStats Stats(int actionPoints = 9) => new ShipStats(actionPoints, 0, 5, 10, null);

        [Test]
        public void AttackReach_IsCentredOnTheOriginNotTheTurret()
        {
            var turret = new P1Coords(6, 4);
            var session = CreateSession(new P1Coords(1, 1), turret);

            var origin = new P1Coords(2, 3);
            var reach = session.AttackReach(origin);

            Assert.That(reach, Is.Not.Empty);

            foreach (var cell in reach)
            {
                var distance = VoidHex.P2.Domain.HexCoordinates.Distance(
                    new VoidHex.P2.Domain.HexCoordinates(origin.Q, origin.R),
                    new VoidHex.P2.Domain.HexCoordinates(cell.Q, cell.R));

                Assert.That(distance, Is.LessThanOrEqualTo(session.AttackRange),
                    $"La casilla {cell} está a {distance} de {origin}, fuera del alcance {session.AttackRange}.");
            }
        }

        [Test]
        public void AttackReach_NeverContainsTheOriginItself()
        {
            var session = CreateSession(new P1Coords(1, 1), new P1Coords(6, 4));
            var origin = new P1Coords(3, 3);

            Assert.That(session.AttackReach(origin).Any(c => c.Equals(origin)), Is.False,
                "El origen no es un destino de disparo: es donde estás parado.");
        }

        [Test]
        public void AttackReach_ShrinksWhenABlockerCutsLineOfSight()
        {
            var session = CreateSession(new P1Coords(1, 1), new P1Coords(6, 4));
            var origin = new P1Coords(4, 3);

            var before = session.AttackReach(origin).Count;

            // Asteroides alrededor del origen: cortan la visión hacia un lado.
            _grid.SetCell(HexCell.Asteroid(new P1Coords(5, 3)));
            _grid.SetCell(HexCell.Asteroid(new P1Coords(5, 2)));
            _grid.SetCell(HexCell.Asteroid(new P1Coords(4, 2)));

            var after = session.AttackReach(origin);

            Assert.That(after.Count, Is.LessThan(before),
                "Bloquear la visión debe reducir el alcance, no solo la distancia.");

            foreach (var cell in after)
            {
                Assert.That(_grid.HasLineOfSight(origin, cell), Is.True,
                    $"La casilla {cell} está en el alcance pero no tiene línea de visión desde {origin}.");
            }
        }

        [Test]
        public void AttackReach_ExcludesBlockedCells()
        {
            var session = CreateSession(new P1Coords(1, 1), new P1Coords(6, 4));

            _grid.SetCell(HexCell.Asteroid(new P1Coords(3, 3)));

            Assert.That(session.AttackReach(new P1Coords(2, 2)).Any(c => c.Equals(new P1Coords(3, 3))), Is.False,
                "No se dispara hacia una casilla bloqueada.");
        }

        [Test]
        public void CanAttackFrom_AgreesWithTheReachContainingTheTurret()
        {
            var turret = new P1Coords(5, 3);
            var session = CreateSession(new P1Coords(0, 3), turret);

            // Recorre todo el tablero: el invariante debe valer en cada casilla.
            for (var q = 0; q < 8; q++)
            {
                for (var r = 0; r < 6; r++)
                {
                    var origin = new P1Coords(q, r);
                    if (!_grid.TryGetCell(origin, out var cell) || !cell.IsPassable) continue;

                    var reachable = session.AttackReach(origin).Any(c => c.Equals(turret));
                    var canShoot = session.CanAttackFrom(origin);

                    Assert.That(canShoot, Is.EqualTo(reachable),
                        $"En {origin}: CanAttackFrom={canShoot} pero la torreta " +
                        $"{(reachable ? "está" : "no está")} en el alcance dibujado.");
                }
            }
        }

        [Test]
        public void MovingIntoTheAttackArea_EnablesTheShot()
        {
            var turret = new P1Coords(5, 3);
            var session = CreateSession(new P1Coords(0, 3), turret);

            // PA holgado: el objetivo del test es la relación alcance → disparo, no la economía de PA.
            session.Deploy(Stats(), attackDamage: 2);

            Assert.That(session.CanAttackFrom(session.PlayerCell), Is.False,
                "La posición inicial está fuera de alcance.");

            // El alcance marca exactamente a dónde ir para poder disparar.
            var candidates = session.AttackReach(session.PlayerCell)
                .Where(session.CanAttackFrom)
                .OrderBy(c => P1Coords.Distance(session.PlayerCell, c))
                .ToList();

            Assert.That(candidates, Is.Not.Empty, "Debe existir al menos una casilla alcanzable y de disparo.");

            var move = session.TryMove(candidates[0]);
            Assert.That(move.Applied, Is.True, $"El movimiento debe aplicarse: {move.Message}");

            Assert.That(session.CanAttackFrom(session.PlayerCell), Is.True,
                $"Tras moverse a {session.PlayerCell}: distancia={session.DistanceToTurret} " +
                $"alcance={session.AttackRange}");

            Assert.That(session.TryAttack().Applied, Is.True,
                "Entrar al área debe alcanzar para que el disparo se acepte.");
        }
    }
}
