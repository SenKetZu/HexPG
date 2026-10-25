using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VoidHex.P2.Application;
using VoidHex.P2.Domain;
using VoidHex.UiKit;

namespace VoidHex.P2.Unity
{
    /// <summary>
    /// Pantalla de P2 (uGUI nativa).
    ///
    /// Consumidora del pipeline: envía <see cref="ICommand"/> y se actualiza por eventos
    /// (<c>CommandResolved</c>, <c>PhaseChanged</c>, <c>TurnStarted</c>). No hay Update():
    /// el estado del turno no se consulta por frame.
    /// </summary>
    public sealed class TurnScreen : MonoBehaviour
    {
        private const float PanelX = 32f;
        private const float PanelY = 32f;
        private const float PanelW = 446f;
        private const float PanelH = 1016f;
        private const float Pad = 24f;
        private const float ContentW = PanelW - 2f - Pad * 2f;
        private const int LogLines = 5;

        [SerializeField] private TurnDemoDriver driver;

        private readonly List<Text> _logRows = new List<Text>();
        private readonly List<string> _log = new List<string>();

        private Text _phaseValue;
        private Text _apValue;
        private Text _shieldsValue;
        private Text _enemyValue;
        private Text _positionValue;
        private Text _lastValue;
        private Text _queueValue;
        private Image _apBar;

        private void Start()
        {
            if (driver == null) driver = GetComponent<TurnDemoDriver>();
            if (driver == null)
            {
                Debug.LogError("[P2] La pantalla necesita un TurnDemoDriver en el mismo GameObject.");
                return;
            }

            Build();

            driver.CommandResolved += HandleResolved;
            driver.PhaseChanged += _ => RefreshAll();
            driver.TurnStarted += _ => RefreshAll();

            RefreshAll();
        }

        private void OnDestroy()
        {
            if (driver == null) return;

            driver.CommandResolved -= HandleResolved;
        }

        // ------------------------------------------------------------------ construcción

        private void Build()
        {
            var canvas = Ui.CreateCanvas("P2_Canvas");
            var panel = Ui.Panel(canvas.transform, "P2_Panel", PanelX, PanelY, PanelW, PanelH, UiTheme.Panel, 18);

            Ui.Header(panel, "P2 · Desafío 2", "Pipeline de comandos", UiTheme.Violet, Pad, Pad, ContentW);

            const float statW = (ContentW - 20f) / 3f;
            var apStat = Ui.Stat(panel, "stat_ap", Pad, 104f, statW, "6", "PA", UiTheme.Violet);
            var shieldStat = Ui.Stat(panel, "stat_shields", Pad + statW + 10f, 104f, statW, "0", "escudos", UiTheme.Teal);
            var enemyStat = Ui.Stat(panel, "stat_enemy", Pad + (statW + 10f) * 2f, 104f, statW, "6", "hull enemigo",
                UiTheme.Danger);

            _apValue = apStat.Value;
            _shieldsValue = shieldStat.Value;
            _enemyValue = enemyStat.Value;

            _apBar = Ui.Bar(panel, "ap_bar", Pad, 196f, ContentW, "Puntos de acción", 6f, 6f, UiTheme.Violet);

            _phaseValue = Ui.Row(panel, "row_phase", Pad, 236f, ContentW, "Fase", "—");
            _positionValue = Ui.Row(panel, "row_pos", Pad, 260f, ContentW, "Posición", "—");
            _lastValue = Ui.Row(panel, "row_last", Pad, 284f, ContentW, "Último comando", "—");
            _queueValue = Ui.Row(panel, "row_queue", Pad, 308f, ContentW, "Cola", "libre");

            Ui.Separator(panel, Pad, 338f, ContentW);
            Ui.Label(panel, "actions_title", Pad, 350f, ContentW, 18f, "COMANDOS INTERCALADOS",
                UiTheme.KickerSize, UiTheme.Muted, bold: true);

            const float btnH = 44f;
            var y = 374f;

            AddCommandButton(panel, "btn_move2", y, "Mover a (2,0)  ·  2 PA", UiTheme.Violet,
                () => _ = driver.SubmitAsync(new MoveCommand(new HexCoordinates(2, 0), 2)));
            y += btnH + 6f;

            AddCommandButton(panel, "btn_move3", y, "Mover a (3,0)  ·  1 PA", UiTheme.Violet,
                () => _ = driver.SubmitAsync(new MoveCommand(new HexCoordinates(3, 0), 1)));
            y += btnH + 6f;

            AddCommandButton(panel, "btn_attack", y, "Atacar  ·  2 PA  ·  daño 3  ·  rango 3", UiTheme.Danger,
                () => _ = driver.SubmitAsync(new AttackCommand(2, 3, 3)));
            y += btnH + 6f;

            AddCommandButton(panel, "btn_shield", y, "Escudo  ·  2 PA  ·  +4", UiTheme.Teal,
                () => _ = driver.SubmitAsync(new ShieldCommand(2, 4)));
            y += btnH + 6f;

            AddCommandButton(panel, "btn_force_reject", y, "Forzar rechazo  ·  mover cuesta 20 PA", UiTheme.Amber,
                () => _ = driver.SubmitAsync(new MoveCommand(new HexCoordinates(6, 0), 20)));
            y += btnH + 6f;

            AddCommandButton(panel, "btn_end_turn", y, "Terminar turno  ·  recarga PA", UiTheme.Success,
                () => driver.EndTurn());            y += btnH + 6f;

            var half = (ContentW - 8f) / 2f;
            Ui.Button(panel, "btn_enemy_turn", Pad, y, half, "Turno enemigo", UiTheme.Dim,
                () => driver.BeginEnemyTurn(), 40f);
            Ui.Button(panel, "btn_restart", Pad + half + 8f, y, half, "Reiniciar", UiTheme.Dim,
                () => driver.Restart(), 40f);

            Ui.Separator(panel, Pad, 772f, ContentW);
            Ui.Label(panel, "log_title", Pad, 784f, ContentW, 18f, "REGISTRO DE RESOLUCIONES",
                UiTheme.KickerSize, UiTheme.Muted, bold: true);

            for (var i = 0; i < LogLines; i++)
            {
                _logRows.Add(Ui.Label(panel, "log_" + i, Pad, 808f + i * 22f, ContentW, 20f, "—",
                    UiTheme.SmallSize, UiTheme.Dim));
            }

            Ui.Label(panel, "footnote", Pad, 934f, ContentW, 60f,
                "La lógica se aplica antes de que termine la animación.\n" +
                "Un comando rechazado no dispara animación ni altera el estado.",
                UiTheme.SmallSize, UiTheme.Dim);
        }

        private void AddCommandButton(Transform panel, string name, float y, string label, Color accent,
            System.Action submit)
        {
            Ui.Button(panel, name, Pad, y, ContentW, label, accent, () =>
            {
                // El botón no espera: el pipeline serializa y la pantalla reacciona por evento.
                submit();
            });
        }

        // ------------------------------------------------------------------ eventos

        private void HandleResolved(CommandOutcome outcome)
        {
            var entry = outcome.IsAccepted
                ? $"{outcome.Before.ActionPoints} → {outcome.After.ActionPoints} PA"
                : $"rechazado · {outcome.Validation.Rejection}";

            _lastValue.text = outcome.IsAccepted
                ? entry
                : $"{outcome.Validation.Rejection} ({outcome.Validation.RequiredActionPoints} > " +
                  $"{outcome.Validation.AvailableActionPoints})";

            _lastValue.color = outcome.IsAccepted ? UiTheme.Success : UiTheme.Danger;

            PushLog(entry);
            RefreshAll();
        }

        private void PushLog(string entry)
        {
            _log.Insert(0, entry);
            while (_log.Count > LogLines) _log.RemoveAt(_log.Count - 1);

            for (var i = 0; i < _logRows.Count; i++)
            {
                _logRows[i].text = i < _log.Count ? _log[i] : "—";
                _logRows[i].color = i == 0 ? UiTheme.Text : UiTheme.Dim;
            }
        }

        private void RefreshAll()
        {
            if (driver == null) return;

            var state = driver.State;

            _phaseValue.text = driver.Phase switch
            {
                TurnPhase.PlayerTurn => "jugador",
                TurnPhase.Executing => "ejecutando",
                TurnPhase.EnemyTurn => "enemigo",
                _ => driver.Phase.ToString(),
            };

            _phaseValue.color = driver.Phase switch
            {
                TurnPhase.PlayerTurn => UiTheme.Violet,
                TurnPhase.Executing => UiTheme.Amber,
                TurnPhase.EnemyTurn => UiTheme.Danger,
                _ => UiTheme.Text,
            };

            _apValue.text = state.ActionPoints.ToString();
            _shieldsValue.text = state.Shields.ToString();
            _enemyValue.text = state.EnemyHull.ToString();
            _positionValue.text = state.Position.ToString();

            _queueValue.text = driver.Phase == TurnPhase.Executing ? "ocupada (animando)" : "libre";
            _queueValue.color = driver.Phase == TurnPhase.Executing ? UiTheme.Amber : UiTheme.Success;

            var ratio = state.MaxActionPoints <= 0 ? 0f : (float)state.ActionPoints / state.MaxActionPoints;
            var width = ContentW * Mathf.Clamp01(ratio);
            var rect = (RectTransform)_apBar.transform;
            rect.sizeDelta = new Vector2(Mathf.Max(4f, width), rect.sizeDelta.y);
        }
    }
}
