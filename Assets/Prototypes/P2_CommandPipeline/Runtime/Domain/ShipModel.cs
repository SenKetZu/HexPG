namespace VoidHex.P2.Domain
{
    /// <summary>
    /// Estado de la nave dentro de un turno. Inmutable: cada cambio produce una instancia nueva,
    /// y el único que la reemplaza es el pipeline de comandos.
    /// </summary>
    public readonly struct ShipModel
    {
        public HexCoordinates Position { get; }
        public int ActionPoints { get; }
        public int MaxActionPoints { get; }
        public int Shields { get; }
        public int Hull { get; }
        public HexCoordinates EnemyPosition { get; }
        public int EnemyHull { get; }

        public ShipModel(
            HexCoordinates position,
            int actionPoints,
            int maxActionPoints,
            int shields,
            int hull,
            HexCoordinates enemyPosition,
            int enemyHull)
        {
            Position = position;
            ActionPoints = actionPoints;
            MaxActionPoints = maxActionPoints;
            Shields = shields;
            Hull = hull;
            EnemyPosition = enemyPosition;
            EnemyHull = enemyHull;
        }

        public static ShipModel Start()
            => new ShipModel(
                HexCoordinates.Origin,
                actionPoints: 6,
                maxActionPoints: 6,
                shields: 0,
                hull: 10,
                enemyPosition: new HexCoordinates(4, 0),
                enemyHull: 6);

        public ShipModel WithActionPoints(int value)
            => new ShipModel(Position, value, MaxActionPoints, Shields, Hull, EnemyPosition, EnemyHull);

        public ShipModel WithPosition(HexCoordinates value)
            => new ShipModel(value, ActionPoints, MaxActionPoints, Shields, Hull, EnemyPosition, EnemyHull);

        public ShipModel WithShields(int value)
            => new ShipModel(Position, ActionPoints, MaxActionPoints, value, Hull, EnemyPosition, EnemyHull);

        public ShipModel WithEnemyHull(int value)
            => new ShipModel(Position, ActionPoints, MaxActionPoints, Shields, Hull, EnemyPosition, value);
    }
}
