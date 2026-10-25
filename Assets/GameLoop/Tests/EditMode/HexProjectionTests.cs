using NUnit.Framework;
using VoidHex.GameLoop.Logic;

namespace VoidHex.GameLoop.Tests
{
    /// <summary>
    /// La proyección decide qué casilla se clickeó. Si se desalinea del render, el juego "no responde"
    /// y el síntoma no apunta a la causa. Estos tests fijan la ida y vuelta.
    /// </summary>
    public sealed class HexProjectionTests
    {
        [Test]
        public void ToWorld_ThenTryToCell_ReturnsTheSameCell(
            [Values(0, 1, 3, 5, 7)] int q,
            [Values(0, 1, 2, 4, 5)] int r)
        {
            var world = HexProjection.ToWorld(q, r);

            var found = HexProjection.TryToCell(world.X, world.Z, 8, 6, out var cellQ, out var cellR);

            Assert.That(found, Is.True);
            Assert.That(cellQ, Is.EqualTo(q));
            Assert.That(cellR, Is.EqualTo(r));
        }

        [Test]
        public void TryToCell_NearTheCenterOfACell_StillResolvesToThatCell()
        {
            var world = HexProjection.ToWorld(2, 3);

            // Un clic 25 cm al costado sigue dentro de la misma casilla (radio ~0.6).
            var found = HexProjection.TryToCell(world.X + 0.25f, world.Z - 0.2f, 8, 6, out var q, out var r);

            Assert.That(found, Is.True);
            Assert.That(q, Is.EqualTo(2));
            Assert.That(r, Is.EqualTo(3));
        }

        [Test]
        public void TryToCell_OutsideTheMap_ReturnsFalse()
        {
            var world = HexProjection.ToWorld(40, 40);

            var found = HexProjection.TryToCell(world.X, world.Z, 8, 6, out _, out _);

            Assert.That(found, Is.False, "Un clic en el vacío no debe producir una casilla válida.");
        }

        [Test]
        public void TryToCell_WithDegenerateMap_ReturnsFalse()
        {
            Assert.That(HexProjection.TryToCell(0f, 0f, 0, 0, out _, out _), Is.False);
        }
    }
}
