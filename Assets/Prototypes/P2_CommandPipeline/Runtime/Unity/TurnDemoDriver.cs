using System;
using System.Threading.Tasks;
using UnityEngine;
using VoidHex.P2.Application;
using VoidHex.P2.Domain;

namespace VoidHex.P2.Unity
{
    /// <summary>
    /// Composition root del prototipo P2: arma el pipeline con el adaptador de vista y ejecuta una
    /// secuencia intercalada. Sin singleton ni Service Locator: las dependencias se pasan por constructor.
    /// </summary>
    public sealed class TurnDemoDriver : MonoBehaviour
    {
        [SerializeField] private bool submitOnStart = true;

        private CommandProcessor _processor;
        private ShipView _view;

        public event Action<CommandOutcome> CommandResolved;

        /// <summary>Reexpone la transición de fase para que la pantalla no consulte el estado por frame.</summary>
        public event Action<TurnPhase> PhaseChanged
        {
            add => _processor.PhaseChanged += value;
            remove => _processor.PhaseChanged -= value;
        }

        /// <summary>Reexpone la recarga de PA al iniciar turno.</summary>
        public event Action<ShipModel> TurnStarted
        {
            add => _processor.TurnStarted += value;
            remove => _processor.TurnStarted -= value;
        }

        public ShipModel State => _processor.State;

        public TurnPhase Phase => _processor.Phase;

        public bool HasCompletedDemo { get; private set; }

        public CommandOutcome LastOutcome { get; private set; }

        private void Awake()
        {
            _view = GetComponent<ShipView>();
            if (_view == null) _view = gameObject.AddComponent<ShipView>();

            CreateProcessor();
        }

        private void CreateProcessor()
        {
            if (_processor != null) _processor.Resolved -= HandleResolved;

            _processor = new CommandProcessor(ShipModel.Start(), _view);
            _processor.Resolved += HandleResolved;
            HasCompletedDemo = false;
            LastOutcome = default;
        }

        private void HandleResolved(CommandOutcome outcome)
        {
            LastOutcome = outcome;
            CommandResolved?.Invoke(outcome);
        }

        private async void Start()
        {
            if (!submitOnStart) return;

            await RunDemoAsync();
        }

        /// <summary>
        /// Secuencia intercalada: mover → atacar → mover. Cada paso espera la animación,
        /// pero la lógica ya se aplicó al volver de SubmitAsync.
        /// </summary>
        public async Task RunDemoAsync()
        {
            await SubmitAsync(new MoveCommand(new HexCoordinates(2, 0), actionPointCost: 2));
            await SubmitAsync(new AttackCommand(actionPointCost: 2, damage: 3, range: 3));
            await SubmitAsync(new MoveCommand(new HexCoordinates(3, 0), actionPointCost: 1));

            HasCompletedDemo = true;
            Debug.Log($"[P2] Fin del turno: PA {_processor.State.ActionPoints}/{_processor.State.MaxActionPoints}, " +
                      $"hull enemigo {_processor.State.EnemyHull}.");
        }

        /// <summary>Entrada del HUD/escena. Devuelve el resultado para poder mostrarlo en pantalla.</summary>
        public Task<CommandOutcome> SubmitAsync(ICommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            return _processor.SubmitAsync(command);
        }

        /// <summary>Vuelve al estado inicial sin recargar la escena.</summary>
        public void Restart()
        {
            CreateProcessor();
            if (_view != null) _view.ResetVisuals();
        }

        public void EndTurn() => _processor.EndTurn();

        public void BeginEnemyTurn() => _processor.BeginEnemyTurn();
    }
}
