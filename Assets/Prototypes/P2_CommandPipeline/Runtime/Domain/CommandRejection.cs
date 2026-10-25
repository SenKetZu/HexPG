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
    }
}
