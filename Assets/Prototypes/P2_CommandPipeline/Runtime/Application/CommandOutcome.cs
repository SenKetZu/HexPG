using VoidHex.P2.Domain;

namespace VoidHex.P2.Application
{
    /// <summary>
    /// Resultado de enviar un comando al pipeline: aceptado (con estado antes/después)
    /// o rechazado (con motivo y sin cambios). Viaja a la presentación por evento.
    /// </summary>
    public readonly struct CommandOutcome
    {
        public bool IsAccepted { get; }
        public CommandValidation Validation { get; }
        public ShipModel Before { get; }
        public ShipModel After { get; }

        private CommandOutcome(bool isAccepted, CommandValidation validation, ShipModel before, ShipModel after)
        {
            IsAccepted = isAccepted;
            Validation = validation;
            Before = before;
            After = after;
        }

        public static CommandOutcome Accepted(CommandValidation validation, ShipModel before, ShipModel after)
            => new CommandOutcome(true, validation, before, after);

        public static CommandOutcome Rejected(CommandValidation validation, ShipModel state)
            => new CommandOutcome(false, validation, state, state);
    }
}
