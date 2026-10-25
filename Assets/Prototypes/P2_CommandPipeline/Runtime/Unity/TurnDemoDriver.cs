using System;
using System.Threading.Tasks;
using UnityEngine;
using VoidHex.P2.Application;
using VoidHex.P2.Domain;

namespace VoidHex.P2.Unity
{
    /// <summary>
    /// Composition root del prototipo P2: arma el pipeline con el adaptador de vista y ejecuta
    /// una secuencia intercalada. No hay singleton ni Service Locator: las dependencias se
    /// pasan por constructor.
    /// </summary>
    public sealed class TurnDemoDriver : MonoBehaviour
    {
        [SerializeField] private bool submitOnStart = true;

        private CommandProcessor _processor;
        private ShipView _view;

        public event Action<CommandOutcome> CommandResolved;

        public ShipModel State => _processor.State;

        public TurnPhase Phase => _processor.Phase;

        public bool HasCompletedDemo { get; private set; }

        private void Awake()
        {
            _view = GetComponent<ShipView>();
            if (_view == null) _view = gameObject.AddComponent<ShipView>();

            _processor = new CommandProcessor(ShipModel.Start(), _view);
            _processor.Resolved += outcome => CommandResolved?.Invoke(outcome);
        }

        private async void Start()
        {
            if (!submitOnStart) return;

            await RunDemoAsync();
        }

        /// <summary>Secuencia intercalada: mover → atacar → mover. Las animaciones se esperan entre pasos.</summary>
        public async Task RunDemoAsync()
        {
            await _processor.SubmitAsync(new MoveCommand(new HexCoordinates(2, 0), actionPointCost: 2));
            await _processor.SubmitAsync(new AttackCommand(actionPointCost: 2, damage: 3, range: 3));
            await _processor.SubmitAsync(new MoveCommand(new HexCoordinates(3, 0), actionPointCost: 1));

            HasCompletedDemo = true;
            Debug.Log($"[P2] Fin del turno: PA {_processor.State.ActionPoints}/{_processor.State.MaxActionPoints}, " +
                      $"hull enemigo {_processor.State.EnemyHull}.");
        }
    }
}
