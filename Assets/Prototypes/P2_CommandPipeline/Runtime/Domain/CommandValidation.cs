namespace VoidHex.P2.Domain
{
    /// <summary>
    /// Resultado de validar un comando contra el estado actual.
    /// Nunca es un bool suelto: el motivo y los números viajan con el resultado para que
    /// la presentación pueda explicar el rechazo y el test pueda verificarlo.
    /// </summary>
    public readonly struct CommandValidation
    {
        public bool IsAllowed { get; }
        public CommandRejection Rejection { get; }
        public int RequiredActionPoints { get; }
        public int AvailableActionPoints { get; }

        private CommandValidation(
            bool isAllowed,
            CommandRejection rejection,
            int requiredActionPoints,
            int availableActionPoints)
        {
            IsAllowed = isAllowed;
            Rejection = rejection;
            RequiredActionPoints = requiredActionPoints;
            AvailableActionPoints = availableActionPoints;
        }

        public static CommandValidation Allowed()
            => new CommandValidation(true, CommandRejection.None, 0, 0);

        public static CommandValidation Rejected(CommandRejection rejection, int required, int available)
            => new CommandValidation(false, rejection, required, available);

        public override string ToString()
            => IsAllowed ? "Allowed" : Rejection + " (" + RequiredActionPoints + " > " + AvailableActionPoints + ")";
    }
}
