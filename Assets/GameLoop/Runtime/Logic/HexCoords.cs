using P1Coords = VoidHex.P1.Domain.HexCoordinates;
using P2Coords = VoidHex.P2.Domain.HexCoordinates;

namespace VoidHex.GameLoop.Logic
{
    /// <summary>
    /// Adaptador entre las coordenadas de P1 (grilla) y las de P2 (combate).
    ///
    /// Existen dos structs idénticos porque cada prototipo es independiente: duplicar el tipo es el
    /// precio de que P2 no dependa de la grilla. El bucle paga ese precio acá, en cuatro líneas
    /// explícitas, en lugar de repartir conversiones por todo el código.
    /// </summary>
    public static class HexCoords
    {
        public static P1Coords ToGrid(P2Coords cell) => new P1Coords(cell.Q, cell.R);

        public static P2Coords FromGrid(P1Coords cell) => new P2Coords(cell.Q, cell.R);
    }
}
