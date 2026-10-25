using UnityEngine;
using VoidHex.P1.Domain;

namespace VoidHex.P1.Unity
{
    /// <summary>
    /// Única fuente de verdad de los colores del mapa: la usan el renderizador 3D y la leyenda de la UI.
    /// Si divergieran, la leyenda mentiría sobre lo que se ve en pantalla.
    /// </summary>
    public static class GridPalette
    {
        public static readonly Color Open = new Color(0.14f, 0.19f, 0.25f);
        public static readonly Color Slow = new Color(0.16f, 0.30f, 0.55f);
        public static readonly Color Blocked = new Color(0.42f, 0.22f, 0.12f);
        public static readonly Color Occupied = new Color(0.65f, 0.25f, 0.85f);

        public static Color For(HexCell cell)
        {
            if (cell.IsOccupied) return Occupied;
            if (!cell.IsPassable) return Blocked;
            if (cell.MovementCost > 1) return Slow;
            return Open;
        }
    }
}
