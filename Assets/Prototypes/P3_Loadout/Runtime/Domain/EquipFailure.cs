namespace VoidHex.P3.Domain
{
    /// <summary>Motivo por el que una operación de equipamiento no se aplicó.</summary>
    public enum EquipFailure
    {
        None = 0,
        WrongSlotType = 1,
        UnknownModule = 2,
        InvalidModule = 3,
        EmptySlot = 4,
    }
}
