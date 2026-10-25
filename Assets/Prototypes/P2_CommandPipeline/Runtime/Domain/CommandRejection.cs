namespace VoidHex.P2.Domain
{
    /// <summary>Motivo por el que un comando no puede ejecutarse. Explícito para que la UI lo explique.</summary>
    public enum CommandRejection
    {
        None = 0,
        NotEnoughActionPoints = 1,
        InvalidTarget = 2,
        OutOfRange = 3,
        TargetDestroyed = 4,
        WrongPhase = 5,

        /// <summary>
        /// Agregado por el game loop: el objetivo está en rango pero un obstáculo corta la línea de visión.
        /// Es aditivo y no cambia ninguna regla existente; el tipo de rechazo lo necesita quien extiende.
        /// </summary>
        LineOfSightBlocked = 6,
    }
}
