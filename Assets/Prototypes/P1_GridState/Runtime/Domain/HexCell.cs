namespace VoidHex.P1.Domain
{
    public enum CellTerrain
    {
        Void = 0,
        Open = 1,
        Asteroid = 2,
        Nebula = 3,
    }

    /// <summary>
    /// Estado lógico de una celda. Inmutable: toda mutación produce una celda nueva,
    /// lo que permite comparar antes/después y detectar cambios reales.
    /// </summary>
    public readonly struct HexCell
    {
        public HexCoordinates Coordinates { get; }
        public CellTerrain Terrain { get; }
        public int MovementCost { get; }
        public bool BlocksLineOfSight { get; }
        public bool IsOccupied { get; }

        public HexCell(
            HexCoordinates coordinates,
            CellTerrain terrain,
            int movementCost,
            bool blocksLineOfSight,
            bool isOccupied = false)
        {
            Coordinates = coordinates;
            Terrain = terrain;
            MovementCost = movementCost;
            BlocksLineOfSight = blocksLineOfSight;
            IsOccupied = isOccupied;
        }

        public static HexCell Open(HexCoordinates coordinates)
            => new HexCell(coordinates, CellTerrain.Open, 1, false);

        public static HexCell Asteroid(HexCoordinates coordinates)
            => new HexCell(coordinates, CellTerrain.Asteroid, int.MaxValue, true);

        public static HexCell Nebula(HexCoordinates coordinates, int movementCost = 2)
            => new HexCell(coordinates, CellTerrain.Nebula, movementCost, false);

        public bool IsPassable => Terrain != CellTerrain.Void && MovementCost < int.MaxValue;

        public HexCell WithOccupied(bool occupied)
            => new HexCell(Coordinates, Terrain, MovementCost, BlocksLineOfSight, occupied);
    }

    /// <summary>Tipo de mutación real que dispara una notificación.</summary>
    public enum GridChangeKind
    {
        CellChanged = 0,
        OccupationChanged = 1,
    }

    /// <summary>Notificación inmutable de una mutación concreta del mapa.</summary>
    public readonly struct GridChange
    {
        public HexCoordinates Coordinates { get; }
        public GridChangeKind Kind { get; }

        public GridChange(HexCoordinates coordinates, GridChangeKind kind)
        {
            Coordinates = coordinates;
            Kind = kind;
        }

        public override string ToString() => Kind + " " + Coordinates;
    }
}
