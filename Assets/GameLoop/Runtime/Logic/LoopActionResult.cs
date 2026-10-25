using VoidHex.P2.Domain;

namespace VoidHex.GameLoop.Logic
{
    /// <summary>
    /// Resultado de una acción del bucle.
    ///
    /// Lleva la validación completa (con motivo) para que la UI explique el rechazo, y la
    /// <see cref="CommandExecution"/> para que la presentación anime el cambio. La sesión aplica la
    /// lógica al instante y devuelve esto; esperar la animación es responsabilidad de la capa Unity.
    /// </summary>
    public readonly struct LoopActionResult
    {
        public bool Applied { get; }
        public CommandValidation Validation { get; }
        public string Message { get; }

        /// <summary>Par antes/después a animar. Solo es válido si <see cref="HasExecution"/>.</summary>
        public CommandExecution Execution { get; }

        public bool HasExecution { get; }

        private LoopActionResult(bool applied, CommandValidation validation, string message,
            CommandExecution execution, bool hasExecution)
        {
            Applied = applied;
            Validation = validation;
            Message = message;
            Execution = execution;
            HasExecution = hasExecution;
        }

        public static LoopActionResult Success(CommandValidation validation, string message,
            CommandExecution execution)
            => new LoopActionResult(true, validation, message, execution, true);

        public static LoopActionResult Rejected(CommandValidation validation, string message)
            => new LoopActionResult(false, validation, message, default, false);

        /// <summary>Acción sin efecto observable (por ejemplo, la torreta no tiene a quién disparar).</summary>
        public static LoopActionResult NoOp(string message)
            => new LoopActionResult(false, CommandValidation.Allowed(), message, default, false);
    }
}
