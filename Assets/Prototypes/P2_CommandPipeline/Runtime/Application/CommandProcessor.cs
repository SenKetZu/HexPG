using System;
using System.Threading;
using System.Threading.Tasks;
using VoidHex.P2.Domain;

namespace VoidHex.P2.Application
{
    /// <summary>
    /// Pipeline de comandos. Orden por comando: validar → ejecutar (instantáneo) → notificar
    /// → esperar la animación. Los comandos se serializan con una cola asíncrona, por lo que
    /// no pueden intercalarse a mitad de una animación.
    ///
    /// Único dueño de <see cref="State"/>: nadie más lo reemplaza.
    /// </summary>
    public sealed class CommandProcessor
    {
        private readonly ICommandPresentation _presentation;
        private readonly SemaphoreSlim _queue = new SemaphoreSlim(1, 1);
        private ShipModel _state;

        public CommandProcessor(ShipModel initialState, ICommandPresentation presentation)
        {
            _state = initialState;
            _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            Phase = TurnPhase.PlayerTurn;
        }

        public ShipModel State => _state;

        public TurnPhase Phase { get; private set; }

        /// <summary>Se emite en cada transición de fase: la vista no necesita consultar por frame.</summary>
        public event Action<TurnPhase> PhaseChanged;

        private void SetPhase(TurnPhase phase)
        {
            if (Phase == phase) return;

            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }

        /// <summary>Se emite para comandos aceptados y rechazados; la UI distingue con IsAccepted.</summary>
        public event Action<CommandOutcome> Resolved;

        public async Task<CommandOutcome> SubmitAsync(ICommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            if (Phase == TurnPhase.EnemyTurn)
            {
                var rejected = CommandOutcome.Rejected(
                    CommandValidation.Rejected(
                        CommandRejection.WrongPhase,
                        command.ActionPointCost,
                        _state.ActionPoints),
                    _state);

                Resolved?.Invoke(rejected);
                return rejected;
            }

            await _queue.WaitAsync();
            try
            {
                var before = _state;
                var validation = command.Validate(before);

                if (!validation.IsAllowed)
                {
                    var rejected = CommandOutcome.Rejected(validation, before);
                    Resolved?.Invoke(rejected);
                    return rejected;
                }

                SetPhase(TurnPhase.Executing);
                var after = command.Execute(before);
                _state = after;
                var outcome = CommandOutcome.Accepted(validation, before, after);
                Resolved?.Invoke(outcome);

                // La regla ya está aplicada; la animación ocurre después y no la condiciona.
                await _presentation.PlayAsync(new CommandExecution(command, before, after));
                return outcome;
            }
            finally
            {
                SetPhase(TurnPhase.PlayerTurn);
                _queue.Release();
            }
        }

        /// <summary>Cierra el turno: recarga PA al máximo (no acumula) y avisa a la presentación.</summary>
        public void EndTurn()
        {
            if (Phase == TurnPhase.Executing)
            {
                throw new InvalidOperationException("No se puede terminar el turno durante una animación.");
            }

            SetPhase(TurnPhase.PlayerTurn);
            _state = _state.WithActionPoints(_state.MaxActionPoints);
            TurnStarted?.Invoke(_state);
        }

        /// <summary>Durante el turno enemigo la intención del jugador se rechaza con motivo.</summary>
        public void BeginEnemyTurn()
        {
            if (Phase == TurnPhase.Executing)
            {
                throw new InvalidOperationException("No se puede ceder el turno durante una animación.");
            }

            SetPhase(TurnPhase.EnemyTurn);
        }

        public event Action<ShipModel> TurnStarted;
    }
}
