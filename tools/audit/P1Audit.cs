using VoidHex.P1.Domain;

namespace VoidHex.Audit
{
    /// <summary>Verifica P1 contra las aserciones de GridContractTests.</summary>
    internal static class P1Audit
    {
        internal static void Run()
        {
            var grid = new GridModel(5, 5);
            grid.SetCell(HexCell.Asteroid(new HexCoordinates(1, 0)));
            IGridQueryService query = grid;
            var pathfinder = new Pathfinder(query);

            var target = new HexCoordinates(2, 0);
            var result = pathfinder.FindPath(HexCoordinates.Origin, target, maxCost: 10);
            Program.Check("ruta encontrada", result.IsFound, true);
            Program.Note($"ruta: {string.Join(" ", result.Path)}");
            Program.Check("costo total (el test afirma 3)", result.TotalCost, 3);
            Program.Check("destino correcto", result.Path[result.Path.Count - 1], target);
            Program.Check("costo <= maxCost", result.TotalCost <= 10, true);

            // Reactividad: escritura sin cambio no notifica.
            var raised = 0;
            IGridStatePublisher publisher = grid;
            publisher.Changed += _ => raised++;
            var same = grid.GetCell(new HexCoordinates(4, 4));
            Program.Check("SetCell idéntico devuelve false", grid.SetCell(same), false);
            Program.Check("SetCell idéntico no notifica", raised, 0);

            Program.Check("SetCell distinto devuelve true",
                grid.SetCell(HexCell.Asteroid(new HexCoordinates(0, 4))), true);
            Program.Check("SetCell distinto notifica 1", raised, 1);

            // Presupuesto insuficiente.
            var tight = pathfinder.FindPath(HexCoordinates.Origin, target, maxCost: 2);
            Program.Check("presupuesto insuficiente no encuentra", tight.IsFound, false);
            Program.Check("presupuesto insuficiente -> motivo", tight.Failure, PathFailure.BudgetExceeded);
            Program.Check("ruta vacía cuando falla", tight.Path.Count, 0);

            // Objetivo bloqueado.
            var blocked = pathfinder.FindPath(HexCoordinates.Origin, new HexCoordinates(1, 0), maxCost: 10);
            Program.Check("objetivo bloqueado -> Unreachable", blocked.Failure, PathFailure.Unreachable);

            // Fuera del mapa.
            var outside = pathfinder.FindPath(HexCoordinates.Origin, new HexCoordinates(99, 99), maxCost: 10);
            Program.Check("objetivo inexistente -> InvalidGoal", outside.Failure, PathFailure.InvalidGoal);

            // Determinismo.
            var again = pathfinder.FindPath(HexCoordinates.Origin, target, maxCost: 10);
            Program.Check("determinismo (misma ruta)", string.Join(" ", again.Path), string.Join(" ", result.Path));

            // Línea de visión.
            Program.Check("LOS bloqueada por asteroide", query.HasLineOfSight(HexCoordinates.Origin, target), false);
            Program.Check("LOS libre hacia (0,2)", query.HasLineOfSight(HexCoordinates.Origin, new HexCoordinates(0, 2)), true);
            Program.Check("LOS no bloquea por el extremo",
                query.HasLineOfSight(HexCoordinates.Origin, new HexCoordinates(0, 1)), true);

            // El mapa no se muta durante la búsqueda.
            Program.Check("el mapa conserva el asteroide",
                grid.GetCell(new HexCoordinates(1, 0)).Terrain, CellTerrain.Asteroid);

            // Ocupación: solo notifica cuando cambia.
            var occupancyEvents = 0;
            publisher.Changed += _ => occupancyEvents++;
            Program.Check("SetOccupied primera vez", grid.SetOccupied(HexCoordinates.Origin, true), true);
            Program.Check("SetOccupied repetido devuelve false", grid.SetOccupied(HexCoordinates.Origin, true), false);
            Program.Check("SetOccupied notificó una sola vez", occupancyEvents, 1);
            Program.Check("celda ocupada no es transitable",
                query.IsPassable(HexCoordinates.Origin), false);

            // Desuscripción (requisito de no fuga de eventos).
            var afterUnsubscribe = 0;
            System.Action<GridChange> handler = _ => afterUnsubscribe++;
            publisher.Changed += handler;
            publisher.Changed -= handler;
            grid.SetCell(HexCell.Nebula(new HexCoordinates(4, 3)));
            Program.Check("tras desuscribirse no recibe eventos", afterUnsubscribe, 0);
        }
    }
}
