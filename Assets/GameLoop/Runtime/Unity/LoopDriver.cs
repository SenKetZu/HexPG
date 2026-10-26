using System;
using System.Threading.Tasks;
using UnityEngine;
using VoidHex.GameLoop.Logic;
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
        [SerializeField] private LoopGridView gridView;
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

        private LoopGridView GridView => gridView != null ? gridView : gridView = GetComponent<LoopGridView>();

        // ------------------------------------------------------------------ previsualización

        /// <summary>
        /// Informa qué casilla está bajo el mouse y dibuja la ruta que se recorrería.
        ///
        /// La consulta la resuelve la sesión (<see cref="GameSession.PreviewMove"/>), no la vista:
        /// lo que se muestra y lo que ocurre al hacer clic salen de la misma evaluación.
        /// </summary>
        public MovePreview Hover(P1Coords? cell)
        {
            var canPreview = Session.Phase == LoopPhase.Combat && !_busy;

            if (!canPreview)
            {
                GridView?.SetHover(null, false);
                GridView?.SetPath(null, false);
                GridView?.SetAttackArea(null, false);
                return default;
            }

            if (!cell.HasValue)
            {
                GridView?.SetHover(null, false);
                GridView?.SetPath(null, false);

                // Sin casilla apuntada, la celda resaltada es la de la nave: muestra el alcance actual.
                // Así el jugador ve su alcance real antes de mover el mouse, y el área nunca "desaparece".
                GridView?.SetAttackArea(Session.AttackReach(Session.PlayerCell), true);
                return default;
            }

            var preview = Session.PreviewMove(cell.Value);

            GridView?.SetHover(cell.Value, preview.IsValid);
            GridView?.SetPath(preview.Path, preview.IsValid);

            // El alcance se centra en la casilla apuntada: responde "si me paro acá, a qué llego".
            GridView?.SetAttackArea(Session.AttackReach(cell.Value), true);

            return preview;
        }

        /// <summary>Marca la casilla de la nave mientras sea su turno.</summary>
        private void RefreshSelection()
        {
            var active = Session.Phase == LoopPhase.Combat;
            GridView?.SetSelectedCell(active ? Session.PlayerCell : (P1Coords?)null);

            // El alcance sigue a la nave cuando no hay casilla apuntada; se recalcula tras cada acción.
            if (active) GridView?.SetAttackArea(Session.AttackReach(Session.PlayerCell), true);
            else GridView?.SetAttackArea(null, false);
        }

        private void Start()
        {
            // Muestra las naves desde el hangar: sin esto el tablero aparece vacío hasta desplegar.
            // La sesión ya nace en su posición inicial, así que no hace falta fabricar estado acá.
            Presentation.SnapTo(Session.State, Session.TurretHullMax);

            RefreshSelection();
            Refreshed?.Invoke();
        }

        // ------------------------------------------------------------------ fase hangar

        public void Deploy()
        {
            if (_busy) return;
            if (Session.Phase != LoopPhase.Hangar) return;

            Session.Deploy(Bootstrap.CurrentStats, Bootstrap.ResolveAttackDamage());
            Presentation.SnapTo(Session.State, Session.TurretHullMax);

            RefreshSelection();
            Refreshed?.Invoke();
        }

        public void ReturnToHangar()
        {
            if (_busy) return;
            if (Session.Phase == LoopPhase.Combat) return;

            Session.ReturnToHangar();
            GridView?.SetHover(null, false);
            GridView?.SetPath(null, false);
            GridView?.SetAttackArea(null, false);
            RefreshSelection();
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

            // Moverse cambia el área de ataque: la vista se refresca después de la animación.
            RefreshSelection();
            Refreshed?.Invoke();

            return result;
        }

        public async Task<LoopActionResult> AttackAsync()
        {
            if (_busy) return LoopActionResult.NoOp("La nave está ejecutando una acción.");

            var result = Session.TryAttack();

            await PlayIfAnyAsync(result);
            RefreshSelection();
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

            RefreshSelection();
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
