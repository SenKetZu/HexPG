using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VoidHex.GameLoop.Logic;
using VoidHex.P3.Domain;
using VoidHex.UiKit;
using P1Coords = VoidHex.P1.Domain.HexCoordinates;

namespace VoidHex.GameLoop.Unity
{
    /// <summary>
    /// Pantalla del bucle (uGUI nativa).
    ///
    /// Es consumidora: lee el estado de la sesión solo cuando el driver avisa que cambió, y no
    /// consulta nada en <c>Update</c>. En <c>Update</c> únicamente se lee el clic para mover.
    ///
    /// Dos paneles: hangar (preparación) y combate. Se alternan según la fase del bucle.
    /// </summary>
    public sealed class GameLoopScreen : MonoBehaviour
    {
        private const float PanelX = 32f;
        private const float PanelY = 32f;
        private const float PanelW = 486f;
        private const float PanelH = 1016f;
        private const float Pad = 24f;
        private const float ContentW = PanelW - 2f - Pad * 2f;
        private const int LogLines = 6;

        private static readonly SlotType[] Slots =
        {
            SlotType.Weapon, SlotType.Propulsion, SlotType.Shield, SlotType.Exotic,
        };

        [SerializeField] private LoopDriver driver;

        private readonly List<Text> _logRows = new List<Text>();
        private readonly List<string> _log = new List<string>();
        private readonly Dictionary<SlotType, Text> _slotValues = new Dictionary<SlotType, Text>();
        private readonly Dictionary<SlotType, Text> _slotCaptions = new Dictionary<SlotType, Text>();

        private RectTransform _combatPanel;
        private RectTransform _hangarPanel;

        private Ui.StatView _apStat;
        private Ui.StatView _hullStat;
        private Ui.StatView _shieldStat;
        private Ui.StatView _turretStat;

        private Text _phaseValue;
        private Text _turnValue;
        private Text _rangeValue;
        private Text _sightValue;
        private Text _statusValue;
        private Image _apBar;

        private Button _attackButton;
        private Button _endTurnButton;
        private Button _returnButton;
        private Text _combatDamage;

        private Ui.StatView _hangarAp;
        private Ui.StatView _hangarShields;
        private Ui.StatView _hangarMove;
        private Ui.StatView _hangarHull;
        private Text _damageValue;

        private void Start()
        {
            if (driver == null) driver = GetComponent<LoopDriver>();
            if (driver == null)
            {
                Debug.LogError("[Loop] La pantalla necesita un LoopDriver en el mismo GameObject.");
                return;
            }

            Build();

            driver.Refreshed += Refresh;
            PushLog("Hangar: equipá módulos y desplegá.");
            Refresh();
        }

        private void OnDestroy()
        {
            if (driver != null) driver.Refreshed -= Refresh;
        }

        private void Update()
        {
            if (driver == null) return;
            if (driver.Session.Phase != LoopPhase.Combat || driver.IsBusy) return;
            if (!Input.GetMouseButtonDown(0)) return;

            if (!GridPointer.TryPickCell(Camera.main, Input.mousePosition,
                    driver.Bootstrap.Width, driver.Bootstrap.Height, out var cell)) return;

            _ = MoveAsync(cell);
        }

        private async System.Threading.Tasks.Task MoveAsync(P1Coords cell)
        {
            var result = await driver.MoveAsync(cell);
            PushLog(result.Message);
        }

        // ------------------------------------------------------------------ construcción

        private void Build()
        {
            var canvas = Ui.CreateCanvas("GameLoop_Canvas");

            _hangarPanel = Ui.Panel(canvas.transform, "HangarPanel", PanelX, PanelY, PanelW, PanelH,
                UiTheme.Panel, 18);
            BuildHangar(_hangarPanel);

            _combatPanel = Ui.Panel(canvas.transform, "CombatPanel", PanelX, PanelY, PanelW, PanelH,
                UiTheme.Panel, 18);
            BuildCombat(_combatPanel);
        }

        private void BuildHangar(Transform panel)
        {
            Ui.Header(panel, "Game loop · 1/3", "Hangar", UiTheme.Amber, Pad, Pad, ContentW);

            write_hint(panel, 104f,
                "Equipá la nave. El loadout define PA, escudos, alcance de movimiento y daño del arma.");

            const float statW = (ContentW - 30f) / 4f;
            _hangarAp = Ui.Stat(panel, "h_ap", Pad, 158f, statW, "6", "PA", UiTheme.Amber);
            _hangarShields = Ui.Stat(panel, "h_sh", Pad + (statW + 10f), 158f, statW, "0", "escudos", UiTheme.Teal);
            _hangarMove = Ui.Stat(panel, "h_mv", Pad + (statW + 10f) * 2f, 158f, statW, "3", "mov", UiTheme.Violet);
            _hangarHull = Ui.Stat(panel, "h_hl", Pad + (statW + 10f) * 3f, 158f, statW, "10", "casco", UiTheme.Text);

            Ui.Separator(panel, Pad, 252f, ContentW);
            Ui.Label(panel, "slots_title", Pad, 264f, ContentW, 18f, "RANURAS", UiTheme.KickerSize,
                UiTheme.Muted, bold: true);

            var y = 288f;
            foreach (var slot in Slots) y = BuildSlotCard(panel, slot, y) + 6f;

            Ui.Separator(panel, Pad, y + 6f, ContentW);
            Ui.Label(panel, "catalog_title", Pad, y + 18f, ContentW, 18f, "CATÁLOGO", UiTheme.KickerSize,
                UiTheme.Muted, bold: true);
            y += 42f;

            var modules = driver.Bootstrap.Catalog.All;
            for (var i = 0; i < modules.Count; i++)
            {
                var id = modules[i].Id;
                var label = modules[i].DisplayName;

                Ui.Button(panel, "mod_" + id, Pad, y, ContentW, label, UiTheme.Amber,
                    () => Equip(id), 34f, Describe(modules[i]));
                y += 38f;
            }

            Ui.Separator(panel, Pad, y + 4f, ContentW);
            Ui.Label(panel, "damage_title", Pad, y + 16f, ContentW, 18f, "POTENCIA DE FUEGO",
                UiTheme.KickerSize, UiTheme.Muted, bold: true);
            _damageValue = Ui.Label(panel, "damage", Pad, y + 40f, ContentW, 22f, "—", UiTheme.BodySize,
                UiTheme.Text, bold: true);

            Ui.Button(panel, "deploy", Pad, PanelH - 92f, ContentW, "DESPLEGAR  →", UiTheme.Success,
                () => driver.Deploy(), 52f, "Pasa al combate con este loadout");
        }

        private void BuildCombat(Transform panel)
        {
            Ui.Header(panel, "Game loop · 2/3", "Combate", UiTheme.Teal, Pad, Pad, ContentW);

            const float statW = (ContentW - 30f) / 4f;
            _apStat = Ui.Stat(panel, "c_ap", Pad, 104f, statW, "6", "PA", UiTheme.Teal);
            _hullStat = Ui.Stat(panel, "c_hull", Pad + (statW + 10f), 104f, statW, "10", "casco", UiTheme.Text);
            _shieldStat = Ui.Stat(panel, "c_sh", Pad + (statW + 10f) * 2f, 104f, statW, "0", "escudos", UiTheme.Violet);
            _turretStat = Ui.Stat(panel, "c_turret", Pad + (statW + 10f) * 3f, 104f, statW, "8", "torreta", UiTheme.Danger);

            _apBar = Ui.Bar(panel, "ap_bar", Pad, 196f, ContentW, "Puntos de acción", 6f, 6f, UiTheme.Teal);

            _phaseValue = Ui.Row(panel, "r_phase", Pad, 236f, ContentW, "Fase", "—");
            _turnValue = Ui.Row(panel, "r_turn", Pad, 260f, ContentW, "Turno", "1");
            _rangeValue = Ui.Row(panel, "r_range", Pad, 284f, ContentW, "Distancia a la torreta", "—");
            _sightValue = Ui.Row(panel, "r_sight", Pad, 308f, ContentW, "Línea de visión", "—");
            _combatDamage = Ui.Row(panel, "r_damage", Pad, 332f, ContentW, "Daño por disparo", "—");

            Ui.Separator(panel, Pad, 360f, ContentW);
            Ui.Label(panel, "act_title", Pad, 372f, ContentW, 18f, "ACCIONES", UiTheme.KickerSize,
                UiTheme.Muted, bold: true);

            _attackButton = Ui.Button(panel, "attack", Pad, 396f, ContentW, "Atacar torreta  ·  2 PA",
                UiTheme.Danger, () => _ = AttackAsync(), 46f, "Necesita rango y línea de visión");

            _endTurnButton = Ui.Button(panel, "end_turn", Pad, 448f, ContentW, "Terminar turno",
                UiTheme.Success, () => _ = EndTurnAsync(), 46f, "La torreta responde y recargás PA");

            write_hint(panel, 508f, "Clic en una casilla para moverte. El costo depende del camino.");

            _statusValue = Ui.Label(panel, "status", Pad, 550f, ContentW, 46f, string.Empty,
                UiTheme.SmallSize, UiTheme.Amber);

            Ui.Separator(panel, Pad, 602f, ContentW);
            Ui.Label(panel, "log_title", Pad, 614f, ContentW, 18f, "REGISTRO", UiTheme.KickerSize,
                UiTheme.Muted, bold: true);

            for (var i = 0; i < LogLines; i++)
            {
                _logRows.Add(Ui.Label(panel, "log_" + i, Pad, 638f + i * 22f, ContentW, 20f, "—",
                    UiTheme.SmallSize, UiTheme.Dim));
            }

            Ui.Label(panel, "loop_note", Pad, 782f, ContentW, 90f,
                "Hangar (P3) → despliegue → combate (P1 + P2) → resultado → hangar.\n" +
                "Mover consulta la grilla; atacar valida rango y visión; el daño viene del loadout.",
                UiTheme.SmallSize, UiTheme.Dim);

            _returnButton = Ui.Button(panel, "return", Pad, PanelH - 92f, ContentW, "Volver al hangar",
                UiTheme.Teal, () => driver.ReturnToHangar(), 52f, "Reinicia el ciclo con el mismo loadout");
        }

        private void write_hint(Transform panel, float y, string text)
        {
            Ui.Label(panel, "hint_" + Mathf.RoundToInt(y), Pad, y, ContentW, 48f, text,
                UiTheme.SmallSize, UiTheme.Muted);
        }

        private float BuildSlotCard(Transform panel, SlotType slot, float y)
        {
            const float h = 44f;
            var card = Ui.Panel(panel, "slot_" + slot, Pad, y, ContentW, h, UiTheme.PanelAlt, 10, UiTheme.Border);

            Ui.Label(card, "type", 14f, 5f, 130f, 16f, slot.ToString().ToUpperInvariant(), UiTheme.SmallSize,
                UiTheme.Dim, bold: true);

            _slotValues[slot] = Ui.Label(card, "value", 14f, 20f, ContentW - 30f, 20f, "— vacío —",
                UiTheme.SmallSize, UiTheme.Muted, bold: true);

            _slotCaptions[slot] = Ui.Label(card, "caption", 150f, 5f, ContentW - 166f, 16f, string.Empty,
                UiTheme.SmallSize, UiTheme.Dim, TextAnchor.UpperRight);

            return y + h;
        }

        // ------------------------------------------------------------------ acciones de UI

        private void Equip(string moduleId)
        {
            var outcome = driver.Equip(moduleId);

            PushLog(outcome switch
            {
                LoopDriver.EquipOutcome.Applied => $"Equipado: {moduleId}",
                LoopDriver.EquipOutcome.Blocked => "No se puede equipar en combate.",
                _ => $"No se pudo equipar {moduleId}.",
            });
        }

        private async System.Threading.Tasks.Task AttackAsync()
        {
            var result = await driver.AttackAsync();
            PushLog(result.Message);
        }

        private async System.Threading.Tasks.Task EndTurnAsync()
        {
            var messages = await driver.EndTurnAsync();
            foreach (var message in messages) PushLog(message);

            if (driver.Session.Phase == LoopPhase.Victory) PushLog("¡Victoria! Volvé al hangar.");
            else if (driver.Session.Phase == LoopPhase.Defeat) PushLog("Casco destruido. Remolque al hangar.");
        }

        // ------------------------------------------------------------------ refresco

        private void Refresh()
        {
            if (driver == null) return;

            var session = driver.Session;
            var inHangar = session.Phase == LoopPhase.Hangar;

            _hangarPanel.gameObject.SetActive(inHangar);
            _combatPanel.gameObject.SetActive(!inHangar);

            if (inHangar) RefreshHangar();
            else RefreshCombat(session);
        }

        private void RefreshHangar()
        {
            var stats = driver.Bootstrap.CurrentStats;

            _hangarAp.Value.text = stats.ActionPoints.ToString();
            _hangarShields.Value.text = stats.Shields.ToString();
            _hangarMove.Value.text = stats.Movement.ToString();
            _hangarHull.Value.text = stats.Hull.ToString();

            var damage = driver.Bootstrap.ResolveAttackDamage();
            _damageValue.text = $"{damage} de daño por disparo  ·  alcance 3  ·  victorias: " +
                                $"{driver.Session.Victories}";

            foreach (var slot in Slots)
            {
                var has = driver.Bootstrap.Equipment.Current.TryGetModule(slot, out var module);

                _slotValues[slot].text = has ? module.DisplayName : "— vacío —";
                _slotValues[slot].color = has ? UiTheme.Text : UiTheme.Dim;
                _slotCaptions[slot].text = has ? $"MK {module.Tier}" : string.Empty;
            }
        }

        private void RefreshCombat(GameSession session)
        {
            var state = session.State;

            _apStat.Value.text = state.ActionPoints.ToString();
            _hullStat.Value.text = state.Hull.ToString();
            _shieldStat.Value.text = state.Shields.ToString();
            _turretStat.Value.text = state.EnemyHull.ToString();

            SetBar(_apBar, state.ActionPoints, state.MaxActionPoints);

            _phaseValue.text = session.Phase switch
            {
                LoopPhase.Combat => "combate",
                LoopPhase.Victory => "victoria",
                LoopPhase.Defeat => "derrota",
                _ => session.Phase.ToString(),
            };

            _phaseValue.color = session.Phase switch
            {
                LoopPhase.Combat => UiTheme.Teal,
                LoopPhase.Victory => UiTheme.Success,
                LoopPhase.Defeat => UiTheme.Danger,
                _ => UiTheme.Text,
            };

            _turnValue.text = session.Turn.ToString();
            _rangeValue.text = $"{session.DistanceToTurret}  (alcance de la torreta: {session.TurretRange})";
            _sightValue.text = session.TurretHasLineOfSight ? "libre" : "bloqueada";
            _sightValue.color = session.TurretHasLineOfSight ? UiTheme.Danger : UiTheme.Success;

            var inCombat = session.Phase == LoopPhase.Combat;
            _attackButton.interactable = inCombat;
            _endTurnButton.interactable = inCombat;
            _returnButton.gameObject.SetActive(!inCombat);

            _combatDamage.text = $"{session.AttackDamage}  ·  rango 3  ·  victorias: {session.Victories}";

            _statusValue.text = inCombat
                ? "Tu turno: elegí una casilla o atacá."
                : "El combate terminó. Volvé al hangar para reequipar y repetir el ciclo.";
        }

        private static void SetBar(Image fill, float value, float max)
        {
            var rect = (RectTransform)fill.transform;
            var ratio = max <= 0f ? 0f : Mathf.Clamp01(value / max);
            rect.sizeDelta = new Vector2(Mathf.Max(4f, ContentW * ratio), rect.sizeDelta.y);
        }

        private void PushLog(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            _log.Insert(0, message);
            while (_log.Count > LogLines) _log.RemoveAt(_log.Count - 1);

            for (var i = 0; i < _logRows.Count; i++)
            {
                _logRows[i].text = i < _log.Count ? _log[i] : "—";
                _logRows[i].color = i == 0 ? UiTheme.Text : UiTheme.Dim;
            }
        }

        private static string Describe(ModuleDefinition module)
        {
            var parts = new List<string>();

            if (module.BonusActionPoints != 0) parts.Add($"+{module.BonusActionPoints} PA");
            if (module.BonusShields != 0) parts.Add($"+{module.BonusShields} esc");
            if (module.BonusMovement != 0) parts.Add($"+{module.BonusMovement} mov");

            parts.Add($"MK {module.Tier}");
            return string.Join(" · ", parts);
        }
    }
}
