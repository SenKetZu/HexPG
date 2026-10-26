using NUnit.Framework;
using VoidHex.GameLoop.Logic;
using VoidHex.P1.Domain;
using VoidHex.P3.Domain;

namespace VoidHex.GameLoop.Tests
{
    /// <summary>
    /// Los tipos de resultado son structs y circulan como "sin resultado" (<c>default</c>).
    ///
    /// Un struct creado por defecto NO ejecuta su constructor, así que cualquier colección expuesta
    /// como auto-propiedad queda en null. Eso provocó un NullReferenceException en la pantalla del
    /// bucle al consultar la ruta antes de tener una. Estos tests fijan el invariante en los tres
    /// dominios que participan del bucle.
    /// </summary>
    public sealed class ResultStructInvariantTests
    {
        [Test]
        public void DefaultMovePreview_HasEmptyPathNotNull()
        {
            var preview = default(MovePreview);

            Assert.That(preview.Path, Is.Not.Null);
            Assert.That(preview.Path, Is.Empty);
            Assert.That(preview.IsValid, Is.False);
        }

        [Test]
        public void DefaultPathResult_HasEmptyPathNotNull()
        {
            var result = default(PathResult);

            Assert.That(result.Path, Is.Not.Null);
            Assert.That(result.Path, Is.Empty);
            Assert.That(result.IsFound, Is.False);
        }

        [Test]
        public void DefaultShipStats_HasEmptyAbilitiesNotNull()
        {
            var stats = default(ShipStats);

            Assert.That(stats.Abilities, Is.Not.Null);
            Assert.That(stats.Abilities, Is.Empty);
        }

        [Test]
        public void PreviewMove_ToTheCurrentCell_ReturnsZeroCostAndNoSteps()
        {
            var grid = new GridModel(5, 5);
            var session = new GameSession(grid, grid,
                new VoidHex.P1.Domain.HexCoordinates(1, 1),
                new VoidHex.P1.Domain.HexCoordinates(4, 4));

            // Fuera del combate no hay previsualización: primero hay que desplegar.
            session.Deploy(new ShipStats(6, 0, 3, 10, null), attackDamage: 2);

            var preview = session.PreviewMove(new VoidHex.P1.Domain.HexCoordinates(1, 1));

            // La casilla actual es un destino válido sin movimiento: dibuja el camino vacío.
            Assert.That(preview.IsValid, Is.True);
            Assert.That(preview.Cost, Is.Zero);
            Assert.That(preview.Path.Count, Is.EqualTo(1));
        }

        [Test]
        public void PreviewMove_OnBlockedCell_IsNotValid()
        {
            var grid = new GridModel(5, 5);
            grid.SetCell(HexCell.Asteroid(new VoidHex.P1.Domain.HexCoordinates(2, 2)));

            var session = new GameSession(grid, grid,
                new VoidHex.P1.Domain.HexCoordinates(1, 1),
                new VoidHex.P1.Domain.HexCoordinates(4, 4));

            session.Deploy(new ShipStats(6, 0, 3, 10, null), attackDamage: 2);

            var preview = session.PreviewMove(new VoidHex.P1.Domain.HexCoordinates(2, 2));

            Assert.That(preview.IsValid, Is.False);
            Assert.That(preview.Path, Is.Empty);
        }
    }
}
