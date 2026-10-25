using System;
using System.Collections.Generic;

namespace VoidHex.P3.Domain
{
    /// <summary>
    /// Composición aditiva de estadísticas. Función pura: no muta la base ni los módulos,
    /// y es idempotente (calcular dos veces da lo mismo).
    /// </summary>
    public static class ShipStatsCalculator
    {
        public static ShipStats Calculate(ShipBaseStats baseStats, IEnumerable<ModuleDefinition> modules)
        {
            if (modules == null) throw new ArgumentNullException(nameof(modules));

            var actionPoints = baseStats.ActionPoints;
            var shields = baseStats.Shields;
            var movement = baseStats.Movement;
            List<string> abilities = null;

            foreach (var module in modules)
            {
                actionPoints += module.BonusActionPoints;
                shields += module.BonusShields;
                movement += module.BonusMovement;

                if (string.IsNullOrEmpty(module.GrantedAbilityId)) continue;

                abilities ??= new List<string>();
                if (!abilities.Contains(module.GrantedAbilityId))
                {
                    abilities.Add(module.GrantedAbilityId);
                }
            }

            IReadOnlyList<string> result = abilities;
            if (result == null) result = Array.Empty<string>();
            return new ShipStats(actionPoints, shields, movement, baseStats.Hull, result);
        }
    }
}
