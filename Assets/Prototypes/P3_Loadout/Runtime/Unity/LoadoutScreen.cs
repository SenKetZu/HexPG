using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VoidHex.P3.Domain;
using VoidHex.UiKit;

namespace VoidHex.P3.Unity
{
    /// <summary>
    /// Pantalla de P3 (uGUI nativa).
    ///
    /// Consumidora de <see cref="IEquipmentService"/> y de <see cref="IModuleCatalog"/>, las dos
    /// interfaces que inyecta VContainer. No conoce <c>EquipmentService</c> ni el contenedor:
    /// esa es exactamente la propiedad que defiende el prototipo.
    /// </summary>
    public sealed class LoadoutScreen : MonoBehaviour
    {
        private const float PanelX = 32f;
        private const float PanelY = 32f;
        private const float PanelW = 470f;
        private const float PanelH = 1016f;
        private const float Pad = 24f;
        private const float ContentW = PanelW - 2f - Pad * 2f;

        private static readonly SlotType[] DisplayedSlots =
        {
            SlotType.Weapon, SlotType.Propulsion, SlotType.Shield, SlotType.Exotic,
        };

        [SerializeField] private LoadoutDemo demo;

        private readonly Dictionary<SlotType, Text> _slotValues = new Dictionary<SlotType, Text>();
        private readonly Dictionary<SlotType, Text> _slotCaptions = new Dictionary<SlotType, Text>();

        private Text _apValue;
        private Text _shieldsValue;
        private Text _movementValue;
        private Text _hullValue;
        private Image _apBar;
        private Image _shieldBar;

        private void Start()
        {
            if (demo == null) demo = GetComponent<LoadoutDemo>();
            if (demo == null)
            {
                Debug.LogError("[P3] La pantalla necesita un LoadoutDemo en el mismo GameObject.");
                return;
            }

            demo.Ready += OnServiceReady;
            if (demo.IsReady) OnServiceReady(demo.Service);
        }

        private void OnDestroy()
        {
            if (demo != null) demo.Ready -= OnServiceReady;
        }

        private void OnServiceReady(IEquipmentService service)
        {
            Build();
            service.StatsChanged += _ => RefreshAll();
            RefreshAll();
        }

        // ------------------------------------------------------------------ construcción

        private void Build()
        {
            var canvas = Ui.CreateCanvas("P3_Canvas");
            var panel = Ui.Panel(canvas.transform, "P3_Panel", PanelX, PanelY, PanelW, PanelH, UiTheme.Panel, 18);

            Ui.Header(panel, "P3 · Desafío 3", "Equipamiento", UiTheme.Amber, Pad, Pad, ContentW);

            const float statW = (ContentW - 20f) / 3f;
            var apStat = Ui.Stat(panel, "stat_ap", Pad, 104f, statW, "6", "PA", UiTheme.Amber);
            var shieldStat = Ui.Stat(panel, "stat_shields", Pad + statW + 10f, 104f, statW, "0", "escudos", UiTheme.Teal);
            var moveStat = Ui.Stat(panel, "stat_movement", Pad + (statW + 10f) * 2f, 104f, statW, "3", "movimiento",
                UiTheme.Violet);

            _apValue = apStat.Value;
            _shieldsValue = shieldStat.Value;
            _movementValue = moveStat.Value;

            _apBar = Ui.Bar(panel, "ap_bar", Pad, 196f, ContentW, "Energía", 6f, 10f, UiTheme.Amber);
            _shieldBar = Ui.Bar(panel, "shield_bar", Pad, 232f, ContentW, "Escudos", 0f, 12f, UiTheme.Teal);

            _hullValue = Ui.Row(panel, "row_hull", Pad, 268f, ContentW, "Casco (fijo)", "10");

            Ui.Separator(panel, Pad, 296f, ContentW);
            Ui.Label(panel, "slots_title", Pad, 308f, ContentW, 18f, "RANURAS", UiTheme.KickerSize,
                UiTheme.Muted, bold: true);

            var y = 332f;
            foreach (var slot in DisplayedSlots)
            {
                y = BuildSlotCard(panel, slot, y) + 8f;
            }

            Ui.Separator(panel, Pad, y + 4f, ContentW);
            y += 16f;
            Ui.Label(panel, "catalog_title", Pad, y, ContentW, 18f, "CATÁLOGO (ASSETS SCRIPTABLEOBJECT)",
                UiTheme.KickerSize, UiTheme.Muted, bold: true);
            y += 24f;

            foreach (var module in demo.AvailableModules)
            {
                var captured = module;
                Ui.Button(panel, "mod_" + captured.Id, Pad, y, ContentW,
                    $"{captured.DisplayName}    ·    {Describe(captured)}", UiTheme.Amber,
                    () => demo.Equip(captured.Id), 34f);
                y += 38f;
            }

            Ui.Separator(panel, Pad, y + 2f, ContentW);
            y += 10f;
            Ui.Label(panel, "remove_title", Pad, y, ContentW, 18f, "QUITAR", UiTheme.KickerSize, UiTheme.Muted,
                bold: true);
            y += 20f;

            var third = (ContentW - 16f) / 3f;
            Ui.Button(panel, "unequip_weapon", Pad, y, third, "Arma", UiTheme.Dim,
                () => demo.Unequip(SlotType.Weapon), 32f);
            Ui.Button(panel, "unequip_prop", Pad + third + 8f, y, third, "Propulsión", UiTheme.Dim,
                () => demo.Unequip(SlotType.Propulsion), 32f);
            Ui.Button(panel, "unequip_shield", Pad + (third + 8f) * 2f, y, third, "Escudo", UiTheme.Dim,
                () => demo.Unequip(SlotType.Shield), 32f);

            Ui.Label(panel, "footnote", Pad, y + 40f, ContentW, 60f,
                "Un slot por tipo: el módulo declara dónde va, así el estado inválido no existe.\n" +
                "El MK III se agregó como asset, sin recompilar código.",
                UiTheme.SmallSize, UiTheme.Dim);
        }

        private float BuildSlotCard(Transform panel, SlotType slot, float y)
        {
            const float h = 46f;
            var card = Ui.Panel(panel, "slot_" + slot, Pad, y, ContentW, h, UiTheme.PanelAlt, 10, UiTheme.Border);

            Ui.Label(card, "type", 14f, 5f, 120f, 16f, slot.ToString().ToUpperInvariant(), UiTheme.SmallSize,
                UiTheme.Dim, bold: true);

            var value = Ui.Label(card, "value", 14f, 21f, ContentW - 30f, 20f, "— vacío —", UiTheme.BodySize,
                UiTheme.Muted, bold: true);
            _slotValues[slot] = value;

            var caption = Ui.Label(card, "caption", 140f, 5f, ContentW - 156f, 16f, string.Empty,
                UiTheme.SmallSize, UiTheme.Dim, TextAnchor.UpperRight);
            _slotCaptions[slot] = caption;

            return y + h;
        }

        // ------------------------------------------------------------------ refresco

        private void RefreshAll()
        {
            var service = demo != null ? demo.Service : null;
            if (service == null) return;

            var stats = service.CurrentStats;

            _apValue.text = stats.ActionPoints.ToString();
            _shieldsValue.text = stats.Shields.ToString();
            _movementValue.text = stats.Movement.ToString();
            _hullValue.text = stats.Hull.ToString();

            SetBar(_apBar, stats.ActionPoints, 10f);
            SetBar(_shieldBar, stats.Shields, 12f);

            foreach (var slot in DisplayedSlots)
            {
                var has = service.Current.TryGetModule(slot, out var module);

                _slotValues[slot].text = has ? module.DisplayName : "— vacío —";
                _slotValues[slot].color = has ? UiTheme.Text : UiTheme.Dim;
                _slotCaptions[slot].text = has ? Describe(module) : string.Empty;
            }
        }

        private static void SetBar(Image fill, float value, float max)
        {
            var rect = (RectTransform)fill.transform;
            var ratio = max <= 0f ? 0f : Mathf.Clamp01(value / max);
            rect.sizeDelta = new Vector2(Mathf.Max(4f, ContentW * ratio), rect.sizeDelta.y);
        }

        private static string Describe(ModuleDefinition module)
        {
            var parts = new List<string>();
            if (module.BonusActionPoints != 0) parts.Add($"+{module.BonusActionPoints} PA");
            if (module.BonusShields != 0) parts.Add($"+{module.BonusShields} esc");
            if (module.BonusMovement != 0) parts.Add($"+{module.BonusMovement} mov");
            if (!string.IsNullOrEmpty(module.GrantedAbilityId)) parts.Add(module.GrantedAbilityId);

            return parts.Count == 0 ? "sin bonificaciones" : string.Join(" · ", parts);
        }
    }
}
