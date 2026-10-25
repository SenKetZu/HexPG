namespace VoidHex.GameLoop.Logic
{
    /// <summary>
    /// Fases del bucle completo. No son las fases del pipeline de P2: acá hablamos del juego entero
    /// (hangar → combate → resultado), no de un turno.
    /// </summary>
    public enum LoopPhase
    {
        /// <summary>Preparación: se equipa la nave y se lee el resultado del loadout.</summary>
        Hangar = 0,

        /// <summary>Combate por turnos sobre la grilla.</summary>
        Combat = 1,

        /// <summary>La torreta quedó destruida.</summary>
        Victory = 2,

        /// <summary>El casco de la nave llegó a cero.</summary>
        Defeat = 3,
    }
}
