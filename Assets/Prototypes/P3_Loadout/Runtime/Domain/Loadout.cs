using System.Collections.Generic;

namespace VoidHex.P3.Domain
{
    /// <summary>
    /// Conjunto de módulos equipados. Inmutable: equipar o quitar devuelve un loadout nuevo,
    /// de modo que el anterior sigue siendo válido para comparar y deshacer.
    /// Un slot vacío se representa con ausencia explícita, no con un módulo "vacío".
    /// Un slot por tipo hace que la incompatibilidad de tipo sea imposible por construcción.
    /// </summary>
    public sealed class Loadout
    {
        public const int SlotCount = 4;

        private readonly ModuleDefinition?[] _slots;

        private Loadout(ModuleDefinition?[] slots)
        {
            _slots = slots;
        }

        public static Loadout Empty() => new Loadout(new ModuleDefinition?[SlotCount]);

        public static bool IsKnownSlot(SlotType slot) => (int)slot >= 0 && (int)slot < SlotCount;

        public bool TryGetModule(SlotType slot, out ModuleDefinition module)
        {
            if (IsKnownSlot(slot) && _slots[(int)slot].HasValue)
            {
                module = _slots[(int)slot].Value;
                return true;
            }

            module = default;
            return false;
        }

        public IReadOnlyList<ModuleDefinition> Modules
        {
            get
            {
                var result = new List<ModuleDefinition>();
                for (var i = 0; i < _slots.Length; i++)
                {
                    if (_slots[i].HasValue) result.Add(_slots[i].Value);
                }

                return result;
            }
        }

        public EquipResult TryEquip(ModuleDefinition module)
        {
            if (!module.HasIdentity) return EquipResult.Failed(EquipFailure.InvalidModule, this);
            if (!IsKnownSlot(module.Slot)) return EquipResult.Failed(EquipFailure.WrongSlotType, this);

            var copy = CopySlots();
            copy[(int)module.Slot] = module;
            return EquipResult.Success(new Loadout(copy));
        }

        public EquipResult Unequip(SlotType slot)
        {
            if (!IsKnownSlot(slot)) return EquipResult.Failed(EquipFailure.WrongSlotType, this);
            if (!_slots[(int)slot].HasValue) return EquipResult.Failed(EquipFailure.EmptySlot, this);

            var copy = CopySlots();
            copy[(int)slot] = null;
            return EquipResult.Success(new Loadout(copy));
        }

        private ModuleDefinition?[] CopySlots() => (ModuleDefinition?[])_slots.Clone();
    }
}
