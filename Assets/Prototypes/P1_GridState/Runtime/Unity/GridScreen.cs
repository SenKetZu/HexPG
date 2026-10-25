using UnityEngine;
using UnityEngine.UI;
using VoidHex.P1.Domain;
using VoidHex.UiKit;

namespace VoidHex.P1.Unity
{
    /// <summary>
    /// Pantalla de P1 (uGUI nativa).
    ///
    /// Es una CONSUMIDORA de los contratos agnósticos: lee con <see cref="IGridQueryService"/>,
    /// se entera de los cambios con <see cref="IGridStatePublisher"/> y provoca mutaciones de demo
    /// con <see cref="IGridCommands"/>. No conoce <c>GridModel</c> ni tiene acceso a nada más.
    ///
    /// No hay Update(): cada evento refresca solo lo que cambió.
    /// </summary>
    public sealed class GridScreen : MonoBehaviour
    {
        private const float PanelX = 32f;
        private const float PanelY = 32f;
        private const float PanelW = 436f;
        private const float PanelH = 1016f;
        private const float Pad = 24f;
        private const float ContentW = PanelW - 2f - Pad * 2f;

        [SerializeField] private GridBootstrap bootstrap;

        private readonly HexCoordinates _probe = new HexCoordinates(6, 6);
        private readonly HexCoordinates _occupiedProbe = new HexCoordinates(2, 2);

        private IGridQueryService _query;
        private IGridStatePublisher _publisher;
        private IGridCommands _commands;

        private Text _eventsValue;
        private Text _pathValue;
        private Text _pathCaption;
        private Text _originValue;
        private Text _goalValue;
        private Text _costValue;
        private Text _resultValue;
        private Text _lastEventValue;

        private void Start()
        {
            if (bootstrap == null) bootstrap = GetComponent<GridBootstrap>();
            if (bootstrap == null)
            {
                Debug.LogError("[P1] La pantalla necesita un GridBootstrap en el mismo GameObject.");
                return;
            }

            _query = bootstrap.Query;
            _publisher = bootstrap.Publisher;
            _commands = bootstrap.Commands;

            Build();

            _publisher.Changed += HandleChanged;
            bootstrap.PathRecomputed += RefreshPath;

            RefreshAll();
        }

        private void OnDestroy()
        {
            if (_publisher != null) _publisher.Changed -= HandleChanged;
            if (bootstrap != null) bootstrap.PathRecomputed -= RefreshPath;
        }

        // ------------------------------------------------------------------ construcción

        private void Build()
        {
            var canvas = Ui.CreateCanvas("P1_Canvas");
            var panel = Ui.Panel(canvas.transform, "P1_Panel", PanelX, PanelY, PanelW, PanelH, UiTheme.Panel, 18);

            Ui.Header(panel, "P1 · Desafío 1", "Estado de la grilla", UiTheme.Teal, Pad, Pad, ContentW);

            // Tres cifras: eventos, tamaño del mapa y ruta activa.
            const float statW = (ContentW - 20f) / 3f;
            var eventsStat = Ui.Stat(panel, "stat_events", Pad, 104f, statW, "0", "eventos", UiTheme.Teal);
            Ui.Stat(panel, "stat_cells", Pad + statW + 10f, 104f, statW,
                (_query != null ? bootstrap.Width * bootstrap.Height : 0).ToString(), "celdas", UiTheme.Text);
            var pathStat = Ui.Stat(panel, "stat_path", Pad + (statW + 10f) * 2f, 104f, statW, "—", "ruta", UiTheme.Amber);

            _eventsValue = eventsStat.Value;
            _pathValue = pathStat.Value;
            _pathCaption = pathStat.Caption;

            // Lecturas del mapa y del resultado del pathfinding.
            const float rowW = ContentW;
            _originValue = Ui.Row(panel, "row_origin", Pad, 196f, rowW, "Origen", bootstrap.StartCell.ToString());
            _goalValue = Ui.Row(panel, "row_goal", Pad, 220f, rowW, "Destino", bootstrap.GoalCell.ToString());
            _costValue = Ui.Row(panel, "row_cost", Pad, 244f, rowW, "Costo total", "—");
            _resultValue = Ui.Row(panel, "row_result", Pad, 268f, rowW, "Resultado", "—");
            _lastEventValue = Ui.Row(panel, "row_last", Pad, 292f, rowW, "Último evento", "—");

            Ui.Separator(panel, Pad, 322f, rowW);
            Ui.Label(panel, "actions_title", Pad, 334f, rowW, 18f, "ACCIONES DE DEMOSTRACIÓN",
                UiTheme.KickerSize, UiTheme.Muted, bold: true);

            const float btnH = 50f;
            var y = 360f;
            Ui.Button(panel, "btn_block", Pad, y, rowW, "Bloquear celda (6,6)", UiTheme.Danger,
                () => Mutate(() => _commands.SetCell(HexCell.Asteroid(_probe))), btnH,
                "Asteroid · bloquea también la visión");
            y += btnH + 8f;

            Ui.Button(panel, "btn_free", Pad, y, rowW, "Liberar celda (6,6)", UiTheme.Teal,
                () => Mutate(() => _commands.SetCell(HexCell.Open(_probe))), btnH, "Open · costo de movimiento 1");
            y += btnH + 8f;

            Ui.Button(panel, "btn_occupy", Pad, y, rowW, "Ocupar / liberar (2,2)", UiTheme.Violet,
                () => Mutate(ToggleOccupation), btnH, "SetOccupied notifica sólo si cambia");
            y += btnH + 8f;

            Ui.Button(panel, "btn_same", Pad, y, rowW, "Reescribir igual (6,6)", UiTheme.Amber,
                () => Mutate(RewriteUnchanged), btnH, "Escritura sin cambio: no debe notificar");
            y += btnH + 8f;

            Ui.Button(panel, "btn_path", Pad, y, rowW, "Recalcular ruta", UiTheme.Violet,
                () => bootstrap.RecomputePath(), btnH, "Pathfinder sobre IGridQueryService");

            Ui.Separator(panel, Pad, 664f, rowW);
            Ui.Label(panel, "legend_title", Pad, 678f, rowW, 18f, "LEYENDA DEL MAPA",
                UiTheme.KickerSize, UiTheme.Muted, bold: true);

            Legend(panel, "legend_open", 704f, GridPalette.Open, "Vacío · costo 1");
            Legend(panel, "legend_slow", 730f, new Color(0.26f, 0.20f, 0.45f), "Nebulosa · costo 2 (anomalía)");
            Legend(panel, "legend_blocked", 756f, GridPalette.Blocked, "Asteroide · bloquea y tapa la visión");
            Legend(panel, "legend_occupied", 782f, GridPalette.Occupied, "Ocupada por una nave");

            Ui.Label(panel, "footnote", Pad, 890f, rowW, 100f,
                "El contador de eventos sólo sube cuando la mutación es real.\n" +
                "La vista no tiene Update(): no hay polling en ningún frame.\n" +
                "La ruta se calcula con el contrato de lectura, no con el modelo.",
                UiTheme.SmallSize, UiTheme.Dim);
        }

        private static void Legend(Transform panel, string name, float y, Color color, string text)
        {
            Ui.Dot(panel, name + "_dot", Pad, y, 12f, color);
            Ui.Label(panel, name + "_label", Pad + 22f, y - 2f, ContentW - 22f, 20f, text, UiTheme.SmallSize,
                UiTheme.Muted);
        }

        // ------------------------------------------------------------------ eventos

        private void Mutate(System.Func<bool> action)
        {
            var mutated = action();
            Debug.Log($"[P1] Acción de demo → mutó: {mutated} (eventos emitidos: {bootstrap.ChangeCount}).");

            if (!mutated)
            {
                // Sin mutación no hay evento: el refresco lo hace la pantalla, no el publisher.
                RefreshAll();
            }
        }

        /// <summary>Invierte la ocupación leyendo con el contrato de lectura y escribiendo con el de escritura.</summary>
        private bool ToggleOccupation()
        {
            var occupied = _query.TryGetCell(_occupiedProbe, out var cell) && cell.IsOccupied;
            return _commands.SetOccupied(_occupiedProbe, !occupied);
        }

        /// <summary>Reescribe la celda con su mismo valor: el contrato de escritura debe rechazarlo.</summary>
        private bool RewriteUnchanged()
        {
            if (!_query.TryGetCell(_probe, out var cell)) return false;
            return _commands.SetCell(cell);
        }

        private void HandleChanged(GridChange change)
        {
            RefreshAll();
        }

        private void RefreshPath()
        {
            RefreshAll();
        }

        private void RefreshAll()
        {
            if (_query == null) return;

            _eventsValue.text = bootstrap.ChangeCount.ToString();

            if (bootstrap.LastPathFound)
            {
                _pathValue.text = bootstrap.LastPath.Count.ToString();
                _pathCaption.text = $"costo {bootstrap.LastPathCost}";
                _costValue.text = bootstrap.LastPathCost.ToString();
                _resultValue.text = "Ruta encontrada";
            }
            else
            {
                _pathValue.text = "—";
                _pathCaption.text = bootstrap.LastFailure.ToString();
                _costValue.text = "—";
                _resultValue.text = bootstrap.LastFailure.ToString();
            }

            _originValue.text = bootstrap.StartCell.ToString();
            _goalValue.text = bootstrap.GoalCell.ToString();
            _lastEventValue.text = bootstrap.ChangeCount == 0 ? "—" : bootstrap.LastChange.ToString();
        }
    }
}
