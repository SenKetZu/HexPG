namespace VoidHex.P3.Domain
{
    public readonly struct EquipResult
    {
        public bool IsSuccess { get; }
        public EquipFailure Failure { get; }
        /// <summary>Nunca es null: en caso de fallo devuelve el loadout vigente.</summary>
        public Loadout Loadout { get; }

        private EquipResult(bool isSuccess, EquipFailure failure, Loadout loadout)
        {
            IsSuccess = isSuccess;
            Failure = failure;
            Loadout = loadout;
        }

        public static EquipResult Success(Loadout loadout) => new EquipResult(true, EquipFailure.None, loadout);

        public static EquipResult Failed(EquipFailure failure, Loadout current)
            => new EquipResult(false, failure, current);
    }
}
