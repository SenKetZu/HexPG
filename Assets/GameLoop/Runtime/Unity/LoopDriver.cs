using System;
using System.Threading.Tasks;
using UnityEngine;
using VoidHex.GameLoop.Logic;
using VoidHex.P2.Domain;
using VoidHex.P2.Unity;
using P1Coords = VoidHex.P1.Domain.HexCoordinates;

namespace VoidHex.GameLoop.Unity
{
    /// <summary>
    /// Orquesta el bucle en Unity: pide la acción a la sesión (que resuelve al instante) y después
    /// espera la animación.
    ///
    /// La separación es la misma que defiende P2: la regla no espera frames; la presentación sí.
    /// La pantalla no conoce la sesión ni la sesión conoce Unity.
    /// </summary>
    public sealed class LoopDriver : MonoBehaviour
    {
        [SerializeField] private LoopBootstrap bootstrap;
        [SerializeField] private ShipView presentation;

        private ShipView _presentation;
        private bool _busy;

        /// <summary>Se emite después de cada acción que cambió algo, para que la UI se refresque.</summary>
        public event Action Refreshed;

        public GameSession Session => Bootstrap.Session;

        public LoopBootstrap Bootstrap
        {
            get
            {
                if (bootstrap == null) bootstrap = GetComponent<LoopBootstrap>();
                return bootstrap;
            }
        }

        /// <summary>Mientras hay una animación en curso no se acepta otra orden.</summary>
        public bool IsBusy => _busy;

        private ShipView Presentation
        {
            get
            {
                if (_presentation != null) return _presentation;

                _presentation = presentation != null ? presentation : GetComponent<ShipView>();
                if (_presentation == null) _presentation = gameObject.AddComponent<ShipView>();

                return _presentation;
            }
        }

        private void Start()
        {
            // Muestra las naves desde el hangar: sin esto el tablero aparece vacío hasta desplegar.
            Presentation.SnapTo(PreviewState(), Session.TurretHullMax);

            Refreshed?.Invoke();
        }

        /// <summary>Estado mínimo para ubicar las naves antes de desplegar (no altera la sesión).</summary>
        private ShipModel PreviewState()
        {
            var session = Session;

            return new ShipModel(
                VoidHex.GameLoop.Logic.HexCoords.FromGrid(Bootstrap.StartCell),
                0,
                0,
                0,
                1,
                VoidHex.GameLoop.Logic.HexCoords.FromGrid(Bootstrap.TurretCell),
                session.TurretHullMax);
        }

        // ------------------------------------------------------------------ fase hangar

        public void Deploy()
        {
            if (_busy) return;
            if (Session.Phase != LoopPhase.Hangar) return;

            Session.Deploy(Bootstrap.CurrentStats, Bootstrap.ResolveAttackDamage());
            Presentation.SnapTo(Session.State, Session.TurretHullMax);

            Refreshed?.Invoke();
        }

        public void ReturnToHangar()
        {
            if (_busy) return;
            if (Session.Phase == LoopPhase.Combat) return;

            Session.ReturnToHangar();
            Refreshed?.Invoke();
        }

        public EquipOutcome Equip(string moduleId)
        {
            if (_busy || Session.Phase != LoopPhase.Hangar) return EquipOutcome.Blocked;

            var result = Bootstrap.Equipment.Equip(moduleId);
            Refreshed?.Invoke();

            return result.IsSuccess ? EquipOutcome.Applied : EquipOutcome.Failed;
        }

        public EquipOutcome Unequip(VoidHex.P3.Domain.SlotType slot)
        {
            if (_busy || Session.Phase != LoopPhase.Hangar) return EquipOutcome.Blocked;

            var result = Bootstrap.Equipment.Unequip(slot);
            Refreshed?.Invoke();

            return result.IsSuccess ? EquipOutcome.Applied : EquipOutcome.Failed;
        }

        public enum EquipOutcome
        {
            Applied,
            Failed,
            Blocked,
        }

        // ------------------------------------------------------------------ acciones de combate

        public async Task<LoopActionResult> MoveAsync(P1Coords cell)
        {
            if (_busy) return LoopActionResult.NoOp("La nave está ejecutando una acción.");

            var result = Session.TryMove(cell);

            await PlayIfAnyAsync(result);
            Refreshed?.Invoke();

            return result;
        }

        public async Task<LoopActionResult> AttackAsync()
        {
            if (_busy) return LoopActionResult.NoOp("La nave está ejecutando una acción.");

            var result = Session.TryAttack();

            await PlayIfAnyAsync(result);
            Refreshed?.Invoke();

            return result;
        }

        /// <summary>Cierra el turno: la torreta actúa y la nave recarga PA. Devuelve los mensajes.</summary>
        public async Task<string[]> EndTurnAsync()
        {
            if (_busy) return new[] { "La nave está ejecutando una acción." };

            var results = Session.EndTurn();
            var messages = new string[results.Count];

            for (var i = 0; i < results.Count; i++)
            {
                messages[i] = results[i].Message;
                await PlayIfAnyAsync(results[i]);
            }

            Refreshed?.Invoke();
            return messages;
        }

        private async Task PlayIfAnyAsync(LoopActionResult result)
        {
            if (!result.HasExecution) return;

            _busy = true;
            try
            {
                await Presentation.PlayAsync(result.Execution);
            }
            finally
            {
                _busy = false;
            }
        }
    }
}
