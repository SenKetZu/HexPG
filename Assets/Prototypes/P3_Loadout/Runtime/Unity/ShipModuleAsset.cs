using UnityEngine;
using VoidHex.P3.Domain;

namespace VoidHex.P3.Unity
{
    /// <summary>
    /// Asset data-driven de un módulo (MK I / II / III). Es el Flyweight: una sola instancia
    /// en memoria compartida por todas las naves que lo equipan.
    /// </summary>
    [CreateAssetMenu(menuName = "VoidHex/Module", fileName = "Module")]
    public sealed class ShipModuleAsset : ScriptableObject
    {
        [SerializeField] private string moduleId = "";
        [SerializeField] private string displayName = "";
        [SerializeField] private SlotType slot = SlotType.Weapon;
        [SerializeField] private int tier = 1;
        [SerializeField] private int bonusActionPoints;
        [SerializeField] private int bonusShields;
        [SerializeField] private int bonusMovement;
        [SerializeField] private string grantedAbilityId = "";

        public string ModuleId => moduleId;

        /// <summary>Autoría: usado por la herramienta de escenas y por los tests de integración.</summary>
        public void Initialize(
            string id,
            string label,
            SlotType slotType,
            int moduleTier,
            int actionPoints,
            int shields,
            int movement,
            string abilityId)
        {
            moduleId = id;
            displayName = label;
            slot = slotType;
            tier = moduleTier;
            bonusActionPoints = actionPoints;
            bonusShields = shields;
            bonusMovement = movement;
            grantedAbilityId = abilityId;
        }

        /// <summary>Traducción al tipo de dominio. Valida en el borde: nunca propaga null.</summary>
        public ModuleDefinition ToDefinition()
        {
            return new ModuleDefinition(
                moduleId,
                displayName,
                slot,
                tier,
                bonusActionPoints,
                bonusShields,
                bonusMovement,
                grantedAbilityId);
        }
    }
}
