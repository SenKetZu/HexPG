namespace VoidHex.P3.Domain
{
    /// <summary>
    /// Datos inmutables de un módulo (MK I / II / III). En Unity se materializa como
    /// ScriptableObject (Flyweight); el dominio solo conoce esta estructura.
    /// </summary>
    public readonly struct ModuleDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public SlotType Slot { get; }
        public int Tier { get; }
        public int BonusActionPoints { get; }
        public int BonusShields { get; }
        public int BonusMovement { get; }

        /// <summary>Vacío significa "no otorga habilidad". Nunca null.</summary>
        public string GrantedAbilityId { get; }

        public ModuleDefinition(
            string id,
            string displayName,
            SlotType slot,
            int tier,
            int bonusActionPoints,
            int bonusShields,
            int bonusMovement,
            string grantedAbilityId)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Slot = slot;
            Tier = tier;
            BonusActionPoints = bonusActionPoints;
            BonusShields = bonusShields;
            BonusMovement = bonusMovement;
            GrantedAbilityId = grantedAbilityId ?? string.Empty;
        }

        public bool HasIdentity => !string.IsNullOrEmpty(Id);

        public override string ToString() => DisplayName + " [" + Slot + " MK" + Tier + "]";
    }
}
