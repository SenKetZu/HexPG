namespace VoidHex.P1.Domain
{
    /// <summary>
    /// Contrato de LECTURA SÍNCRONA del mapa. Lo consumen la IA, los comandos y el pathfinding.
    /// Intencionalmente no expone ninguna operación de mutación (ISP).
    /// </summary>
    public interface IGridQueryService
    {
        bool Contains(HexCoordinates coordinates);

        HexCell GetCell(HexCoordinates coordinates);

        bool TryGetCell(HexCoordinates coordinates, out HexCell cell);

        bool IsPassable(HexCoordinates coordinates);

        int MovementCostToEnter(HexCoordinates coordinates);

        bool HasLineOfSight(HexCoordinates from, HexCoordinates to);
    }
}
