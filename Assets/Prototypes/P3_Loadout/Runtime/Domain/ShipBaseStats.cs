namespace VoidHex.P3.Domain
{
    /// <summary>Estadísticas base de la nave, antes de equipar módulos.</summary>
    public readonly struct ShipBaseStats
    {
        public int ActionPoints { get; }
        public int Shields { get; }
        public int Movement { get; }
        public int Hull { get; }

        public ShipBaseStats(int actionPoints, int shields, int movement, int hull)
        {
            ActionPoints = actionPoints;
            Shields = shields;
            Movement = movement;
            Hull = hull;
        }
    }
}
