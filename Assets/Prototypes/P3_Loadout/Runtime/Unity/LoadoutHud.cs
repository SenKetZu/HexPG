using UnityEngine;
using VoidHex.P3.Domain;

namespace VoidHex.P3.Unity
{
    /// <summary>
    /// Panel de control del prototipo P3 en Play Mode (IMGUI).
    /// Muestra slots, estadísticas recalculadas y permite equipar/quitar sin tocar la escena.
    /// </summary>
    public sealed class LoadoutHud : MonoBehaviour
    {
        [SerializeField] private LoadoutDemo demo;

        private void OnEnable()
        {
            if (demo == null) demo = GetComponent<LoadoutDemo>();
        }

        private void OnGUI()
        {
            if (demo == null || demo.Service == null) return;

            var stats = demo.Service.CurrentStats;

            GUILayout.BeginArea(new Rect(12, 12, 460, 520), GUI.skin.box);

            GUILayout.Label("<b>P3 · VContainer + ScriptableObjects</b>", Label(15));
            GUILayout.Label($"PA <b>{stats.ActionPoints}</b>   Escudos <b>{stats.Shields}</b>   " +
                            $"Movimiento <b>{stats.Movement}</b>   Hull <b>{stats.Hull}</b>", Label(12));

            var abilities = stats.Abilities.Count == 0
                ? "—"
                : string.Join(", ", stats.Abilities);
            GUILayout.Label($"Habilidades: <b>{abilities}</b>", Label(12));

            GUILayout.Space(8);
            GUILayout.Label("Slots (un tipo por slot)", Label(11));
            foreach (SlotType slot in new[] { SlotType.Weapon, SlotType.Propulsion, SlotType.Shield, SlotType.Exotic })
            {
                var text = demo.TryGetEquipped(slot, out var module) ? module.ToString() : "— vacío —";
                GUILayout.Label($"{slot,-11} : {text}", Label(12));
            }

            GUILayout.Space(10);
            GUILayout.Label("Equipar (reemplaza el slot del mismo tipo)", Label(11));

            foreach (var module in demo.AvailableModules)
            {
                var label = $"{module.DisplayName}  ·  {module.Slot}  ·  " +
                            $"{Describe(module)}";
                if (GUILayout.Button(label)) demo.Equip(module.Id);
            }

            GUILayout.Space(10);
            GUILayout.Label("Quitar", Label(11));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Weapon")) demo.Unequip(SlotType.Weapon);
            if (GUILayout.Button("Propulsion")) demo.Unequip(SlotType.Propulsion);
            if (GUILayout.Button("Shield")) demo.Unequip(SlotType.Shield);
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            if (GUILayout.Button("Repetir demo (MK I → MK II → propulsor → escudo)")) demo.RunDemo();

            GUILayout.EndArea();
        }

        private static string Describe(ModuleDefinition module)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (module.BonusActionPoints != 0) parts.Add($"+{module.BonusActionPoints} PA");
            if (module.BonusShields != 0) parts.Add($"+{module.BonusShields} esc");
            if (module.BonusMovement != 0) parts.Add($"+{module.BonusMovement} mov");
            if (!string.IsNullOrEmpty(module.GrantedAbilityId)) parts.Add($"habilidad {module.GrantedAbilityId}");
            parts.Add($"MK {module.Tier}");
            return string.Join(" · ", parts);
        }

        private static GUIStyle Label(int size)
        {
            return new GUIStyle(GUI.skin.label) { richText = true, fontSize = size };
        }
    }
}
