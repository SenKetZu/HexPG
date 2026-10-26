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

        /// <summary>
        /// Nunca null, ni en <c>default(ShipStats)</c>: un struct creado por defecto no ejecuta el
        /// constructor y la auto-propiedad quedaría en null.
        /// </summary>
        public IReadOnlyList<string> Abilities => _abilities ?? NoAbilities;

        private readonly IReadOnlyList<string> _abilities;

        public ShipStats(int actionPoints, int shields, int movement, int hull, IReadOnlyList<string> abilities)
        {
            ActionPoints = actionPoints;
            Shields = shields;
            Movement = movement;
            Hull = hull;
            _abilities = abilities ?? NoAbilities;
        }
    }
}
