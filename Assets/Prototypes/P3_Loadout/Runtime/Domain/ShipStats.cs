using System;
using System.Collections.Generic;

namespace VoidHex.P3.Domain
{
    /// <summary>Estadísticas consolidadas. Resultado puro del cálculo, nunca se muta.</summary>
    public readonly struct ShipStats
    {
        private static readonly IReadOnlyList<string> NoAbilities = Array.Empty<string>();

        public int ActionPoints { get; }
        public int Shields { get; }
        public int Movement { get; }
        public int Hull { get; }
        public IReadOnlyList<string> Abilities { get; }

        public ShipStats(int actionPoints, int shields, int movement, int hull, IReadOnlyList<string> abilities)
        {
            ActionPoints = actionPoints;
            Shields = shields;
            Movement = movement;
            Hull = hull;
            Abilities = abilities ?? NoAbilities;
        }
    }
}
