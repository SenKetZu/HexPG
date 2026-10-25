using System.Collections.Generic;
using NUnit.Framework;
using VoidHex.P1.Domain;

namespace VoidHex.P1.Tests
{
    /// <summary>
    /// Tests de contrato del scaffolding: verifican que la segregación de interfaces (ISP)
    /// sea real y no una convención. Si alguien agrega una mutación a un contrato de lectura
    /// o a uno de publicación, estos tests fallan.
    /// </summary>
    public sealed class GridContractTests
    {
        [Test]
        public void IGridStatePublisher_ExposesOnlyEvents()
        {
            var members = typeof(IGridStatePublisher).GetMembers();

            foreach (var member in members)
            {
                Assert.That(
                    member.MemberType,
                    Is.EqualTo(System.Reflection.MemberTypes.Event),
                    "IGridStatePublisher solo debe exponer eventos, pero encontró: " + member.Name);
            }
        }

        [Test]
        public void IGridQueryService_ExposesNoMutatingMembers()
        {
            var members = typeof(IGridQueryService).GetMembers();

            foreach (var member in members)
            {
                Assert.That(member.MemberType, Is.EqualTo(System.Reflection.MemberTypes.Method),
                    "IGridQueryService debe exponer solo métodos de consulta: " + member.Name);
                Assert.That(member.Name.StartsWith("Set") || member.Name.StartsWith("Add") || member.Name.StartsWith("Remove"),
                    Is.False,
                    "IGridQueryService no puede exponer mutaciones: " + member.Name);
            }
        }

        [Test]
        public void SetCell_WithSameValue_DoesNotRaiseChange()
        {
            var grid = new GridModel(4, 4);
            var raised = 0;
            IGridStatePublisher publisher = grid;
            publisher.Changed += _ => raised++;

            var unchanged = grid.GetCell(HexCoordinates.Origin);
            var mutated = grid.SetCell(unchanged);

            Assert.That(mutated, Is.False);
            Assert.That(raised, Is.Zero, "Una escritura sin cambio real no debe notificar a la vista.");
        }

        [Test]
        public void SetCell_WithDifferentValue_RaisesExactlyOneChange()
        {
            var grid = new GridModel(4, 4);
            var changes = new List<GridChange>();
            IGridStatePublisher publisher = grid;
            publisher.Changed += changes.Add;

            var coordinate = new HexCoordinates(1, 1);
            grid.SetCell(HexCell.Asteroid(coordinate));

            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes[0].Coordinates, Is.EqualTo(coordinate));
            Assert.That(changes[0].Kind, Is.EqualTo(GridChangeKind.CellChanged));
        }

        [Test]
        public void FindPath_AvoidsBlockedCells_AndReturnsMinimumCost()
        {
            var grid = new GridModel(5, 5);
            grid.SetCell(HexCell.Asteroid(new HexCoordinates(1, 0)));

            IGridQueryService query = grid;
            var pathfinder = new Pathfinder(query);

            var result = pathfinder.FindPath(HexCoordinates.Origin, new HexCoordinates(2, 0), maxCost: 10);

            Assert.That(result.IsFound, Is.True);
            Assert.That(result.Path[0], Is.EqualTo(HexCoordinates.Origin));
            Assert.That(result.Path[result.Path.Count - 1], Is.EqualTo(new HexCoordinates(2, 0)));
            // El asteroide en (1,0) obliga a rodearlo por (0,1) y (1,1): 3 pasos de costo 1.
            Assert.That(result.TotalCost, Is.EqualTo(3));
            foreach (var step in result.Path)
            {
                Assert.That(query.IsPassable(step), Is.True, "La ruta no puede atravesar una celda bloqueada.");
            }
        }
    }
}
