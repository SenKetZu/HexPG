using UnityEngine;
using VoidHex.P1.Domain;

namespace VoidHex.P1.Unity
{
    /// <summary>
    /// Adaptador Unity: se suscribe a IGridStatePublisher y redibuja SOLO al recibir un evento.
    /// No consulta el modelo por frame y no tiene acceso a la interfaz de mutación (ISP).
    /// </summary>
    public sealed class GridPresenter : MonoBehaviour
    {
        [SerializeField] private GridBootstrap bootstrap;

        private readonly GridPresenterLogic _logic = new GridPresenterLogic();
        private IGridStatePublisher _publisher;

        public int RedrawCount => _logic.RedrawCount;

        private void OnEnable()
        {
            // Auto-wiring para que la escena no dependa de asignaciones manuales en el editor.
            if (bootstrap == null) bootstrap = GetComponent<GridBootstrap>();
            if (bootstrap == null) return;

            _publisher = bootstrap.Publisher;
            _publisher.Changed += HandleChanged;
        }

        private void OnDisable()
        {
            if (_publisher == null) return;

            _publisher.Changed -= HandleChanged;
            _publisher = null;
        }

        private void HandleChanged(GridChange change)
        {
            _logic.OnGridChanged(change);
            Debug.Log($"[P1][Presenter] Redibujo #{_logic.RedrawCount} por {change}");
        }
    }
}
